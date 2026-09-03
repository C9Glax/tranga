#!/usr/bin/env python3
"""Best-effort migration of a Tranga `main` database into the new multi-service schema.

`main` stored everything in a single database written by one `API` service (contexts
`MangaContext`, `LibraryContext`, `NotificationsContext`, `ActionsContext`). The new stack
splits that across `Services.Manga`, `Services.Libraries`, `Services.Notifications` and
`Services.Tasks`, with a different model: manga are identified by a Guid, titles/descriptions
live in per-source `MetadataEntries` rows rather than on the manga itself, download sources are
extensions addressed by Guid, and downloaded files are tracked as `Files` rows.

This script reads the old database and writes the new one. It never modifies the old database.

What it migrates
    - Manga, with their old name/description/year/status/tags/authors as a metadata entry
    - Chapters, including volume/number/title
    - Downloaded chapter archives: relinked on disk into the new layout and registered as
      `Files`, so the new instance does not re-download your library
    - MangaDex download- and metadata-links (identifiers carry over unchanged)
    - AniList / MyAnimeList metadata identifiers, recovered from the old `Link` and
      `MetadataEntries` tables
    - Gotify and Ntfy notification connectors, rewritten as Naprise service URLs

What it cannot migrate (reported, so you can redo it in the UI)
    - Non-MangaDex download sources. `main` had built-in scrapers; the new stack gets its
      sources from the Suwayomi sidecar, whose extension ids have no relation to the old
      connector names. Affected manga keep their chapters and files but need a source
      re-matched under the manga's "Download links".
    - Komga/Kavita library connections. Registering a library performs an API call against the
      server to create the Tranga library, so it has to be redone through the UI.
    - Alt-titles, external links, `IgnoreChaptersBefore`, download history and cover images.
      None of these have a home in the new schema (covers are re-fetched automatically).

Usage
    pip install "psycopg[binary]"
    ./migrate-from-main.py \
        --source-dsn "postgresql://postgres:postgres@localhost:5432/postgres" \
        --target-dsn "postgresql://postgres:postgres@localhost:5433/tranga" \
        --map /Manga=/srv/tranga/Manga \
        --new-manga-root /srv/tranga-new/Manga

Nothing is written without `--apply`; the default run is a dry run that prints the plan.
"""

from __future__ import annotations

import argparse
import os
import re
import shutil
import sys
import uuid
from collections import defaultdict
from dataclasses import dataclass, field
from typing import Any

try:
    import psycopg
    from psycopg.rows import dict_row
except ImportError:  # pragma: no cover - import guard
    sys.exit('This script needs psycopg 3. Install it with:  pip install "psycopg[binary]"')

# --------------------------------------------------------------------------------------
# Extension identifiers
# --------------------------------------------------------------------------------------

# Identifier of the built-in MangaDex extension (Extensions/Extensions/MangaDex.cs). It is both
# a download and a metadata extension, so old MangaDex ids stay usable on both sides.
MANGADEX = uuid.UUID('019ce521-deaf-7739-9e14-eb6f4afc86e2')
# Metadata-only extensions (Extensions/Extensions/AniList.cs, MyAnimeList.cs).
ANILIST = uuid.UUID('914c3e45-27f4-45ec-b7e2-88d3827713ce')
MYANIMELIST = uuid.UUID('69ade113-7c3c-4ef8-a575-e5082edb5585')

# Namespace for placeholder ids standing in for `main` connectors that have no counterpart in
# the new stack. Rows carrying one are inert: nothing resolves the id to an extension, so the
# download/metadata code paths skip them (they check `GetExtension(...) is not { } extension`),
# and the frontend falls back to displaying the raw Guid. They exist only so a downloaded file
# has a link row to hang off.
LEGACY_NAMESPACE = uuid.UUID('0f9e2a13-8f5c-5d4e-9a6b-7c8d9e0f1a2b')


def legacy_extension(connector_name: str) -> uuid.UUID:
    """Stable placeholder extension id for a `main` connector with no new-stack equivalent."""
    return uuid.uuid5(LEGACY_NAMESPACE, connector_name)


# `main`'s MangaReleaseStatus (Continuing, Completed, OnHiatus, Cancelled, Unreleased) against
# the new ReleaseStatus (Ongoing, Complete, Hiatus, Cancelled). "Unreleased" is dropped.
RELEASE_STATUS = {0: 0, 1: 1, 2: 2, 3: 3, 4: None}

# Default of Common/Settings/Settings.cs ChapterNamingScheme.
DEFAULT_NAMING_SCHEME = '?V(Vol. %V ) Ch. %C?T( - %T)'

# Mirrors Common/Helpers/StringExtensions.SafeFilesystemString.
UNSAFE_CHARACTERS = re.compile(r'[^0-9a-zA-Z\-._ ]')

# Column length limits from the new schema, applied so a long `main` value cannot abort the
# insert halfway through.
MAX_SERIES = 1024
MAX_SUMMARY = 4096
MAX_LANGUAGE = 8
MAX_CHAPTER_TITLE = 2048
MAX_CHAPTER_NUMBER = 16
MAX_CHAPTER_VOLUME = 16
MAX_GENRE = 128
MAX_PERSON = 128

NEW_TABLES = [
    'Mangas', 'MetadataEntries', 'MangaMetadataEntries', 'Chapters', 'ChapterDownloadLinks',
    'DownloadLinks', 'MangaDownloadLinks', 'Files', 'Genres', 'DbMangaGenres', 'DbPerson',
    'DbMangaAuthors',
]


def safe_filesystem_string(value: str) -> str:
    return UNSAFE_CHARACTERS.sub('', value)


def chapter_file_name(volume: str | None, number: str, title: str | None, scheme: str) -> str:
    """Port of Services.Tasks/Helpers/ChapterFileHelper.CreateFileName."""
    values = {'V': volume, 'C': number, 'T': title}
    for parameter, value in values.items():
        optional = re.compile(rf'\?{parameter}\((.*?)\)')
        scheme = optional.sub(lambda m: '' if value is None else m.group(1), scheme)
    for parameter, value in values.items():
        scheme = re.sub(f'%{parameter}', value or '', scheme)
    return safe_filesystem_string(f'{scheme}.cbz')


def truncate(value: str | None, limit: int) -> str | None:
    if value is None:
        return None
    return value if len(value) <= limit else value[:limit]


# --------------------------------------------------------------------------------------
# Plan
# --------------------------------------------------------------------------------------


@dataclass
class FileMove:
    """One downloaded archive to place into the new `Mangas` layout."""
    source: str
    target: str
    relative_path: str      # value for Files.Path, e.g. "Mangas/One Piece"
    name: str               # value for Files.Name


@dataclass
class Plan:
    mangas: list[tuple] = field(default_factory=list)
    metadata: list[tuple] = field(default_factory=list)
    manga_metadata: list[tuple] = field(default_factory=list)
    chapters: list[tuple] = field(default_factory=list)
    chapter_links: list[tuple] = field(default_factory=list)
    download_links: list[tuple] = field(default_factory=list)
    manga_download_links: list[tuple] = field(default_factory=list)
    genres: set[str] = field(default_factory=set)
    manga_genres: list[tuple] = field(default_factory=list)
    people: set[str] = field(default_factory=set)
    manga_authors: list[tuple] = field(default_factory=list)
    notifications: list[tuple] = field(default_factory=list)
    files: list[tuple] = field(default_factory=list)     # (file_id, path, name, mime)
    file_moves: list[FileMove] = field(default_factory=list)
    # file_id -> (chapter_id, extension) so the link can be updated once the file is placed
    file_owner: dict[uuid.UUID, tuple] = field(default_factory=dict)
    warnings: list[str] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)


# --------------------------------------------------------------------------------------
# Reading the old database
# --------------------------------------------------------------------------------------


def fetch(cur, sql: str) -> list[dict[str, Any]]:
    cur.execute(sql)
    return cur.fetchall()


def table_exists(cur, name: str) -> bool:
    cur.execute('SELECT to_regclass(%s) IS NOT NULL AS present', (f'public."{name}"',))
    return cur.fetchone()['present']


def read_source(cur) -> dict[str, Any]:
    for required in ('Mangas', 'Chapters', 'MangaConnectorToManga'):
        if not table_exists(cur, required):
            sys.exit(
                f'The source database has no "{required}" table — this does not look like a '
                'Tranga `main` database. Check --source-dsn (on the stock docker-compose the '
                'database is named "postgres").'
            )

    data = {
        'mangas': fetch(cur, 'SELECT * FROM "Mangas"'),
        'chapters': fetch(cur, 'SELECT * FROM "Chapters"'),
        'manga_connectors': fetch(cur, 'SELECT * FROM "MangaConnectorToManga"'),
        'chapter_connectors': fetch(cur, 'SELECT * FROM "MangaConnectorToChapter"'),
        'authors': fetch(cur, 'SELECT * FROM "Authors"'),
        'author_links': fetch(cur, 'SELECT * FROM "AuthorToManga"'),
        'tags': fetch(cur, 'SELECT * FROM "MangaTagToManga"'),
        'libraries': fetch(cur, 'SELECT * FROM "FileLibraries"'),
        'links': fetch(cur, 'SELECT * FROM "Link"') if table_exists(cur, 'Link') else [],
        'metadata': (
            fetch(cur, 'SELECT * FROM "MetadataEntries"')
            if table_exists(cur, 'MetadataEntries') else []
        ),
        'library_connectors': (
            fetch(cur, 'SELECT * FROM "LibraryConnectors"')
            if table_exists(cur, 'LibraryConnectors') else []
        ),
        'notification_connectors': (
            fetch(cur, 'SELECT * FROM "NotificationConnectors"')
            if table_exists(cur, 'NotificationConnectors') else []
        ),
    }
    return data


# --------------------------------------------------------------------------------------
# Notification connector translation
# --------------------------------------------------------------------------------------


def parse_headers(raw: Any) -> dict[str, str]:
    """`main` stored headers as hstore; psycopg may hand them back as dict or as text."""
    if isinstance(raw, dict):
        return raw
    if not raw:
        return {}
    headers = {}
    for match in re.finditer(r'"((?:[^"\\]|\\.)*)"\s*=>\s*"((?:[^"\\]|\\.)*)"', str(raw)):
        key = match.group(1).replace('\\"', '"').replace('\\\\', '\\')
        value = match.group(2).replace('\\"', '"').replace('\\\\', '\\')
        headers[key] = value
    return headers


def split_host_port(url: str) -> tuple[bool, str, int]:
    from urllib.parse import urlparse
    parsed = urlparse(url)
    https = parsed.scheme == 'https'
    host = parsed.hostname or ''
    port = parsed.port or (443 if https else 80)
    return https, host, port


def naprise_url(connector: dict[str, Any]) -> tuple[str | None, str | None]:
    """Translate a `main` REST notification connector into a Naprise service URL.

    Returns (service_url, reason_if_unsupported).
    """
    import base64
    import json
    from urllib.parse import urlparse

    headers = parse_headers(connector.get('Headers'))
    url = connector.get('Url') or ''
    body = connector.get('Body') or ''

    # Gotify: NotificationConnectorController appended "/message" and put the app token in the
    # X-Gotify-Key header.
    token = headers.get('X-Gotify-Key')
    if token:
        https, host, port = split_host_port(url)
        return f'gotify{"s" if https else ""}://{host}:{port}/{token}', None

    # Ntfy: the auth query parameter is base64(base64("Basic user:pass")) with padding stripped,
    # and the topic was written into the JSON body.
    if 'Authorization' in headers or 'auth=' in url:
        parsed = urlparse(url)
        https = parsed.scheme == 'https'
        host = parsed.hostname or ''
        port = parsed.port or (443 if https else 80)

        user = password = None
        encoded = headers.get('Authorization') or ''
        if not encoded and 'auth=' in url:
            encoded = url.split('auth=', 1)[1].split('&', 1)[0]
        if encoded:
            try:
                padded = encoded + '=' * (-len(encoded) % 4)
                decoded = base64.b64decode(padded).decode('utf-8', 'replace')
                if decoded.startswith('Basic '):
                    credentials = base64.b64decode(
                        decoded[6:] + '=' * (-len(decoded[6:]) % 4)
                    ).decode('utf-8', 'replace')
                    user, _, password = credentials.partition(':')
            except Exception:
                user = password = None

        topic = None
        match = re.search(r'"Topic"\s*:\s*"([^"]*)"', body)
        if match:
            topic = match.group(1)
        if not topic:
            try:
                topic = json.loads(body).get('Topic')
            except Exception:
                topic = None
        if not topic:
            topic = (parsed.path or '').strip('/') or None
        if not topic:
            return None, 'ntfy topic could not be determined from the stored body or url'

        scheme = 'ntfys' if https else 'ntfy'
        if user and password:
            return f'{scheme}://{user}:{password}@{host}:{port}/{topic}', None
        return f'{scheme}://{host}:{port}/{topic}', None

    if 'pushover.net' in url:
        return None, 'Pushover is not one of the shipped Naprise extensions; re-add it manually'

    return None, 'generic REST connector — no Naprise equivalent could be derived'


# --------------------------------------------------------------------------------------
# Building the plan
# --------------------------------------------------------------------------------------


def build_plan(data: dict[str, Any], args: argparse.Namespace) -> Plan:
    plan = Plan()

    libraries = {row['Key']: row for row in data['libraries']}
    path_map = dict(entry.split('=', 1) for entry in args.map)

    connectors_by_manga: dict[str, list[dict]] = defaultdict(list)
    for row in data['manga_connectors']:
        connectors_by_manga[row['ObjId']].append(row)

    connectors_by_chapter: dict[str, list[dict]] = defaultdict(list)
    for row in data['chapter_connectors']:
        connectors_by_chapter[row['ObjId']].append(row)

    chapters_by_manga: dict[str, list[dict]] = defaultdict(list)
    for row in data['chapters']:
        chapters_by_manga[row['ParentMangaId']].append(row)

    author_names = {row['Key']: row['AuthorName'] for row in data['authors']}
    authors_by_manga: dict[str, list[str]] = defaultdict(list)
    for row in data['author_links']:
        name = author_names.get(row['AuthorIds'])
        if name:
            authors_by_manga[row['MangaIds']].append(name)

    tags_by_manga: dict[str, list[str]] = defaultdict(list)
    for row in data['tags']:
        tags_by_manga[row['MangaIds']].append(row['MangaTagIds'])

    links_by_manga: dict[str, list[dict]] = defaultdict(list)
    for row in data['links']:
        links_by_manga[row['MangaKey']].append(row)

    old_metadata_by_manga: dict[str, list[dict]] = defaultdict(list)
    for row in data['metadata']:
        old_metadata_by_manga[row['MangaId']].append(row)

    # DownloadLinks are deduplicated across manga: two manga pointing at the same source series
    # should share one row, the way the new stack creates them.
    download_link_ids: dict[tuple[uuid.UUID, str], uuid.UUID] = {}

    unmatched_sources: list[str] = []
    missing_files: list[str] = []

    for manga in data['mangas']:
        manga_id = uuid.uuid4()
        name = manga['Name']
        connectors = connectors_by_manga.get(manga['Key'], [])
        monitored = any(c['UseForDownload'] for c in connectors)

        plan.mangas.append((manga_id, monitored))

        # ---------- metadata ----------
        extension, identifier = metadata_source(manga, connectors, links_by_manga, old_metadata_by_manga)
        metadata_id = uuid.uuid4()
        year = manga['Year']
        if year is not None and not (0 < year < 2_147_483_647):
            year = None
        plan.metadata.append((
            metadata_id,
            extension,
            identifier,
            truncate(name, MAX_SERIES),
            truncate(manga['Description'], MAX_SUMMARY),
            year,
            truncate(manga['OriginalLanguage'], MAX_LANGUAGE),
            None,                                        # ChaptersNumber: not tracked on main
            RELEASE_STATUS.get(manga['ReleaseStatus']),
            None,                                        # CoverId: covers are re-fetched
            None,                                        # Url
            None,                                        # NSFW: not tracked on main
        ))
        plan.manga_metadata.append((manga_id, metadata_id, True))

        for tag in tags_by_manga.get(manga['Key'], []):
            tag = truncate(tag, MAX_GENRE)
            plan.genres.add(tag)
            plan.manga_genres.append((metadata_id, tag))

        for author in authors_by_manga.get(manga['Key'], []):
            author = truncate(author, MAX_PERSON)
            plan.people.add(author)
            plan.manga_authors.append((author, metadata_id))

        # ---------- download links ----------
        has_usable_source = False
        for priority, connector in enumerate(
            sorted(connectors, key=lambda c: not c['UseForDownload'])
        ):
            connector_name = connector['MangaConnectorName']
            if connector_name == 'Global':
                # A meta-connector on main that fanned out to the others; nothing to point at.
                continue
            if connector_name == 'MangaDex':
                extension_id = MANGADEX
                has_usable_source = True
            else:
                extension_id = legacy_extension(connector_name)
                unmatched_sources.append(f'{name} (source "{connector_name}")')

            key = (extension_id, connector['IdOnConnectorSite'])
            download_link_id = download_link_ids.get(key)
            if download_link_id is None:
                download_link_id = uuid.uuid4()
                download_link_ids[key] = download_link_id
                plan.download_links.append((
                    download_link_id,
                    extension_id,
                    connector['IdOnConnectorSite'],
                    truncate(name, MAX_SERIES),
                    truncate(manga['Description'], MAX_SUMMARY),
                    truncate(manga['OriginalLanguage'], MAX_LANGUAGE),
                    connector['WebsiteUrl'],
                    None,                                # CoverId
                    None,                                # NSFW
                ))
            plan.manga_download_links.append((
                manga_id, download_link_id, bool(connector['UseForDownload']), priority,
            ))

        if monitored and not has_usable_source:
            plan.warnings.append(
                f'"{name}" is monitored but has no MangaDex source; pick a download source for '
                'it in the UI or it will never fetch new chapters.'
            )

        # ---------- chapters ----------
        series_directory = safe_filesystem_string(name)
        relative_path = os.path.join('Mangas', series_directory)
        host_directory = os.path.join(args.new_manga_root, series_directory) if args.new_manga_root else None

        for chapter in chapters_by_manga.get(manga['Key'], []):
            chapter_id = uuid.uuid4()
            volume = str(chapter['VolumeNumber']) if chapter['VolumeNumber'] is not None else None
            plan.chapters.append((
                chapter_id,
                manga_id,
                truncate(chapter['Title'], MAX_CHAPTER_TITLE),
                truncate(volume, MAX_CHAPTER_VOLUME),
                truncate(chapter['ChapterNumber'], MAX_CHAPTER_NUMBER),
                None,                                    # ReleaseDate: not tracked on main
            ))

            chapter_connectors = connectors_by_chapter.get(chapter['Key'], [])
            link_extensions: list[uuid.UUID] = []
            for priority, connector in enumerate(
                sorted(chapter_connectors, key=lambda c: not c['UseForDownload'])
            ):
                connector_name = connector['MangaConnectorName']
                if connector_name == 'Global':
                    continue
                extension_id = (
                    MANGADEX if connector_name == 'MangaDex' else legacy_extension(connector_name)
                )
                if extension_id in link_extensions:
                    continue    # (ChapterId, DownloadExtension) is the primary key
                link_extensions.append(extension_id)
                plan.chapter_links.append((
                    chapter_id,
                    extension_id,
                    connector['IdOnConnectorSite'],
                    priority,
                    None,                                # FileId, filled in below
                    connector['WebsiteUrl'],
                ))

            # ---------- downloaded archive ----------
            if not chapter['Downloaded'] or args.files == 'none':
                continue
            source_file = old_file_path(manga, chapter, libraries, path_map)
            if source_file is None:
                continue
            if not os.path.isfile(source_file):
                missing_files.append(source_file)
                continue
            if host_directory is None:
                continue

            file_name = (
                chapter['FileName'] if args.keep_file_names and chapter['FileName']
                else chapter_file_name(volume, chapter['ChapterNumber'], chapter['Title'], args.naming_scheme)
            )
            file_name = safe_filesystem_string(file_name)
            file_id = uuid.uuid4()
            plan.files.append((file_id, relative_path, file_name, 'application/zip'))
            plan.file_moves.append(FileMove(
                source=source_file,
                target=os.path.join(host_directory, file_name),
                relative_path=relative_path,
                name=file_name,
            ))

            # The file has to hang off a chapter download link. Prefer a real one; otherwise
            # invent a placeholder so the chapter still counts as downloaded.
            if link_extensions:
                owner_extension = link_extensions[0]
            else:
                owner_extension = legacy_extension('Unknown')
                plan.chapter_links.append(
                    (chapter_id, owner_extension, chapter['Key'], 0, None, None)
                )
                link_extensions.append(owner_extension)
            plan.file_owner[file_id] = (chapter_id, owner_extension)

    # ---------- notifications ----------
    for connector in data['notification_connectors']:
        service_url, reason = naprise_url(connector)
        if service_url is None:
            plan.warnings.append(
                f'Notification connector "{connector["Name"]}" was not migrated: {reason}.'
            )
            continue
        plan.notifications.append((uuid.uuid4(), connector['Name'], 0, service_url))

    # ---------- things that need doing by hand ----------
    for connector in data['library_connectors']:
        kind = 'Komga' if connector['LibraryType'] == 0 else 'Kavita'
        if kind == 'Kavita':
            plan.notes.append(
                f'Kavita library at {connector["BaseUrl"]} has no equivalent — the new stack '
                'only supports Komga.'
            )
        else:
            plan.notes.append(
                f'Re-add the Komga library under Settings → Libraries: URL {connector["BaseUrl"]}, '
                f'API key {connector["Auth"]}. Registering it calls the Komga API to create the '
                'Tranga library, which this script cannot do.'
            )

    if unmatched_sources:
        unique = sorted(set(unmatched_sources))
        plan.notes.append(
            f'{len(unique)} manga use a `main` scraper with no new-stack equivalent. Their '
            'chapters and downloaded files carry over, but a Suwayomi source has to be matched '
            'to them before new chapters are fetched:\n  - ' + '\n  - '.join(unique[:50])
            + ('\n  - ...' if len(unique) > 50 else '')
        )
    if missing_files:
        plan.notes.append(
            f'{len(missing_files)} chapters are flagged as downloaded but their archive was not '
            'found on disk; they will be re-downloaded. First few:\n  - '
            + '\n  - '.join(missing_files[:10])
        )

    return plan


def metadata_source(
    manga: dict[str, Any],
    connectors: list[dict],
    links_by_manga: dict[str, list[dict]],
    old_metadata_by_manga: dict[str, list[dict]],
) -> tuple[uuid.UUID, str]:
    """Pick the metadata extension + identifier a migrated manga should be attributed to.

    MangaDex first: on `main` the stored metadata was scraped from whichever connector the manga
    came from, and MangaDex is the only one that survives as an extension. Otherwise fall back to
    the cross-links MangaDex itself recorded (AniList / MyAnimeList), which are exact ids.
    """
    for connector in connectors:
        if connector['MangaConnectorName'] == 'MangaDex':
            return MANGADEX, connector['IdOnConnectorSite']

    for entry in old_metadata_by_manga.get(manga['Key'], []):
        if entry['MetadataFetcherName'] == 'MyAnimeList':
            return MYANIMELIST, entry['Identifier']

    for link in links_by_manga.get(manga['Key'], []):
        provider, url = link['LinkProvider'], link['LinkUrl'] or ''
        if provider == 'AniList':
            match = re.search(r'anilist\.co/manga/(\d+)', url)
            if match:
                return ANILIST, match.group(1)
        if provider == 'MyAnimeList':
            match = re.search(r'myanimelist\.net/manga/(\d+)', url)
            if match:
                return MYANIMELIST, match.group(1)

    # Nothing to attribute it to. A placeholder keeps the title/description visible in the UI
    # without pointing a refresh at the wrong series.
    return legacy_extension('MigratedFromMain'), manga['Key']


def old_file_path(
    manga: dict[str, Any],
    chapter: dict[str, Any],
    libraries: dict[str, dict],
    path_map: dict[str, str],
) -> str | None:
    """Reconstruct where `main` put a downloaded chapter, translated to a host path."""
    if not chapter['FileName']:
        return None
    library = libraries.get(manga['LibraryId'])
    if library is None:
        return None
    base = library['BasePath']
    for container_path, host_path in path_map.items():
        if base == container_path or base.startswith(container_path.rstrip('/') + '/'):
            base = host_path + base[len(container_path.rstrip('/')):]
            break
    return os.path.join(base, manga['DirectoryName'], chapter['FileName'])


# --------------------------------------------------------------------------------------
# Writing the new database
# --------------------------------------------------------------------------------------


def check_target(cur, allow_nonempty: bool) -> None:
    missing = [name for name in NEW_TABLES if not table_exists(cur, name)]
    if missing:
        sys.exit(
            'The target database is missing tables: ' + ', '.join(missing) + '.\n'
            'Start the new stack once so it applies its EF migrations, then run this script '
            'against the database it created (named "tranga" by default).'
        )
    cur.execute('SELECT COUNT(*) AS count FROM "Mangas"')
    count = cur.fetchone()['count']
    if count and not allow_nonempty:
        sys.exit(
            f'The target database already holds {count} manga. This script is meant for a fresh '
            'install; pass --allow-nonempty if you really want to add to it.'
        )


def place_files(plan: Plan, mode: str, apply: bool) -> set[uuid.UUID]:
    """Put the downloaded archives where the new stack expects them.

    Returns the ids of the files that are actually in place; anything that failed is left out of
    the database so the chapter is re-downloaded rather than pointing at nothing.
    """
    placed: set[uuid.UUID] = set()
    for (file_id, _, _, _), move in zip(plan.files, plan.file_moves):
        if not apply:
            placed.add(file_id)
            continue
        try:
            os.makedirs(os.path.dirname(move.target), exist_ok=True)
            if os.path.exists(move.target):
                placed.add(file_id)
                continue
            if mode == 'hardlink':
                try:
                    os.link(move.source, move.target)
                except OSError:
                    shutil.copy2(move.source, move.target)   # different filesystems
            elif mode == 'symlink':
                os.symlink(os.path.abspath(move.source), move.target)
            elif mode == 'copy':
                shutil.copy2(move.source, move.target)
            elif mode == 'move':
                shutil.move(move.source, move.target)
            placed.add(file_id)
        except OSError as error:
            plan.warnings.append(f'Could not place {move.target}: {error}')
    return placed


def write_plan(cur, plan: Plan, placed: set[uuid.UUID]) -> None:
    cur.executemany('INSERT INTO "Mangas" ("MangaId", "Monitored") VALUES (%s, %s)', plan.mangas)
    cur.executemany(
        'INSERT INTO "MetadataEntries" ("MetadataId", "MetadataExtension", "Identifier", "Series",'
        ' "Summary", "Year", "Language", "ChaptersNumber", "Status", "CoverId", "Url", "NSFW")'
        ' VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)',
        plan.metadata,
    )
    cur.executemany(
        'INSERT INTO "MangaMetadataEntries" ("MangaId", "MetadataId", "Chosen") VALUES (%s, %s, %s)',
        plan.manga_metadata,
    )
    cur.executemany(
        'INSERT INTO "Genres" ("Genre") VALUES (%s) ON CONFLICT DO NOTHING',
        [(genre,) for genre in sorted(plan.genres)],
    )
    cur.executemany(
        'INSERT INTO "DbMangaGenres" ("MetadataId", "GenreId") VALUES (%s, %s) ON CONFLICT DO NOTHING',
        plan.manga_genres,
    )
    cur.executemany(
        'INSERT INTO "DbPerson" ("Name") VALUES (%s) ON CONFLICT DO NOTHING',
        [(person,) for person in sorted(plan.people)],
    )
    cur.executemany(
        'INSERT INTO "DbMangaAuthors" ("AuthorId", "MetadataId") VALUES (%s, %s) ON CONFLICT DO NOTHING',
        plan.manga_authors,
    )
    cur.executemany(
        'INSERT INTO "DownloadLinks" ("DownloadLinkId", "DownloadExtension", "Identifier", "Series",'
        ' "Summary", "Language", "Url", "CoverId", "NSFW") VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)',
        plan.download_links,
    )
    cur.executemany(
        'INSERT INTO "MangaDownloadLinks" ("MangaId", "DownloadLinkId", "Matched", "Priority")'
        ' VALUES (%s, %s, %s, %s) ON CONFLICT DO NOTHING',
        plan.manga_download_links,
    )
    cur.executemany(
        'INSERT INTO "Chapters" ("ChapterId", "MangaId", "Title", "Volume", "Number", "ReleaseDate")'
        ' VALUES (%s, %s, %s, %s, %s, %s)',
        plan.chapters,
    )

    files = [row for row in plan.files if row[0] in placed]
    cur.executemany(
        'INSERT INTO "Files" ("FileId", "Path", "Name", "MimeType") VALUES (%s, %s, %s, %s)',
        files,
    )

    owners = {plan.file_owner[file_id]: file_id for file_id, *_ in files if file_id in plan.file_owner}
    links = [
        (chapter_id, extension, identifier, priority, owners.get((chapter_id, extension)), url)
        for chapter_id, extension, identifier, priority, _, url in plan.chapter_links
    ]
    cur.executemany(
        'INSERT INTO "ChapterDownloadLinks" ("ChapterId", "DownloadExtension", "Identifier",'
        ' "Priority", "FileId", "Url") VALUES (%s, %s, %s, %s, %s, %s)',
        links,
    )

    cur.executemany(
        'INSERT INTO "NotificationExtensions" ("Id", "Name", "Type", "ServiceUrl") VALUES (%s, %s, %s, %s)',
        plan.notifications,
    )


# --------------------------------------------------------------------------------------
# Reporting
# --------------------------------------------------------------------------------------


def render_report(plan: Plan, placed: set[uuid.UUID], apply: bool) -> str:
    lines = [
        '# Tranga migration report',
        '',
        ('Applied.' if apply else 'Dry run — nothing was written. Re-run with --apply to commit.'),
        '',
        '## Migrated',
        '',
        f'- {len(plan.mangas)} manga',
        f'- {len(plan.metadata)} metadata entries ({len(plan.genres)} genres, {len(plan.people)} authors)',
        f'- {len(plan.chapters)} chapters',
        f'- {len(plan.download_links)} download links',
        f'- {len(placed)} downloaded chapter archives relinked into the new layout',
        f'- {len(plan.notifications)} notification connectors',
        '',
    ]
    if plan.notes:
        lines += ['## Needs your attention', '']
        lines += [f'- {note}' for note in plan.notes] + ['']
    if plan.warnings:
        lines += ['## Warnings', '']
        lines += [f'- {warning}' for warning in plan.warnings] + ['']
    return '\n'.join(lines)


# --------------------------------------------------------------------------------------


def main() -> int:
    parser = argparse.ArgumentParser(
        description='Best-effort migration of a Tranga `main` database into the new schema.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument('--source-dsn', required=True,
                        help='Connection string for the `main` database (usually .../postgres)')
    parser.add_argument('--target-dsn', required=True,
                        help='Connection string for the new database (usually .../tranga)')
    parser.add_argument('--map', action='append', default=[], metavar='CONTAINER=HOST',
                        help='Translate a FileLibrary base path to a host path, e.g. /Manga=/srv/tranga/Manga. Repeatable.')
    parser.add_argument('--new-manga-root', default=None, metavar='PATH',
                        help='Host path bind-mounted at /app/Mangas in the new stack (the MangaDirectory from .env)')
    parser.add_argument('--files', choices=['hardlink', 'copy', 'move', 'symlink', 'none'],
                        default='hardlink',
                        help='How to place existing archives into the new layout (default: hardlink)')
    parser.add_argument('--keep-file-names', action='store_true',
                        help="Keep each archive's existing filename instead of renaming it to the new naming scheme")
    parser.add_argument('--naming-scheme', default=DEFAULT_NAMING_SCHEME,
                        help=f'Chapter naming scheme to rename archives to (default: {DEFAULT_NAMING_SCHEME!r})')
    parser.add_argument('--allow-nonempty', action='store_true',
                        help='Write into a target database that already contains manga')
    parser.add_argument('--report', default=None, metavar='PATH',
                        help='Write the report to a file as well as stdout')
    parser.add_argument('--apply', action='store_true',
                        help='Actually write. Without it the script only prints what it would do.')
    args = parser.parse_args()

    for entry in args.map:
        if '=' not in entry:
            parser.error(f'--map expects CONTAINER=HOST, got {entry!r}')
    if args.files != 'none' and not args.new_manga_root:
        parser.error('--new-manga-root is required unless --files none')

    with psycopg.connect(args.source_dsn, row_factory=dict_row) as source:
        with source.cursor() as cur:
            try:
                from psycopg.types.hstore import register_hstore
                register_hstore(cur.connection.info, cur.connection)
            except Exception:
                pass    # headers are parsed from text instead
            data = read_source(cur)

    plan = build_plan(data, args)

    with psycopg.connect(args.target_dsn, row_factory=dict_row) as target:
        with target.cursor() as cur:
            check_target(cur, args.allow_nonempty)
            placed = place_files(plan, args.files, args.apply)
            if args.apply:
                write_plan(cur, plan, placed)
                target.commit()
            else:
                target.rollback()

    report = render_report(plan, placed, args.apply)
    print(report)
    if args.report:
        with open(args.report, 'w', encoding='utf-8') as handle:
            handle.write(report)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
