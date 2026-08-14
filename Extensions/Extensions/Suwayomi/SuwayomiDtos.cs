// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Extensions.Extensions.Suwayomi;

/// <summary>A GraphQL request body. Suwayomi's endpoint only ever receives fully-inlined documents from us, so there are no variables.</summary>
internal sealed record GraphQlRequest(string Query);

/// <summary>A GraphQL response envelope. <c>Data</c> is only trustworthy when <c>Errors</c> is empty.</summary>
internal sealed record GraphQlResponse<TData>(TData? Data, GraphQlError[]? Errors);

/// <summary>A single GraphQL error entry.</summary>
internal sealed record GraphQlError(string? Message);

/// <summary>
/// A source exposed by an installed Suwayomi extension. One of these becomes one Tranga <see cref="SuwayomiSource"/>.
/// <para>
/// <c>Id</c> is the Tachiyomi source id: a 64-bit value transported as a string by Suwayomi's <c>LongString</c> scalar.
/// <c>ContentWarning</c> arrives as the GraphQL enum name (<c>SAFE</c>, <c>MIXED</c> or <c>NSFW</c>).
/// </para>
/// </summary>
internal sealed record SuwayomiSourceDto(
    string Id,
    string Name,
    string Lang,
    string? DisplayName,
    string? IconUrl,
    string? HomeUrl,
    string? ContentWarning,
    bool SupportsLatest);

/// <summary>An extension as known to Suwayomi, whether installed or merely listed by a configured extension store.</summary>
internal sealed record SuwayomiExtensionDto(
    string PkgName,
    string Name,
    string Lang,
    string? IconUrl,
    string VersionName,
    string? ContentWarning,
    bool IsInstalled,
    bool IsObsolete,
    bool HasUpdate);

/// <summary>A manga row in Suwayomi's database. <c>Id</c> is a local row id and is not stable across a wiped data volume; <c>Url</c> is.</summary>
internal sealed record SuwayomiMangaDto(
    int Id,
    string SourceId,
    string Url,
    string Title,
    string? ThumbnailUrl,
    string? Description,
    string? Author,
    string? Artist,
    string[]? Genre,
    string? RealUrl);

/// <summary>A chapter row in Suwayomi's database. As with manga, <c>Url</c> is the stable identity and <c>Id</c> is not.</summary>
internal sealed record SuwayomiChapterDto(
    int Id,
    string Url,
    string? Name,
    double ChapterNumber,
    string? Scanlator,
    int SourceOrder);

internal sealed record SourcesData(SourceNodeList? Sources);

internal sealed record SourceNodeList(SuwayomiSourceDto[]? Nodes);

internal sealed record ExtensionsData(ExtensionNodeList? Extensions);

internal sealed record ExtensionNodeList(SuwayomiExtensionDto[]? Nodes);

internal sealed record FetchExtensionsData(FetchExtensionsPayload? FetchExtensions);

internal sealed record FetchExtensionsPayload(SuwayomiExtensionDto[]? Extensions);

internal sealed record UpdateExtensionData(UpdateExtensionPayload? UpdateExtension);

internal sealed record UpdateExtensionPayload(SuwayomiExtensionDto? Extension);

internal sealed record FetchSourceMangaData(FetchSourceMangaPayload? FetchSourceManga);

internal sealed record FetchSourceMangaPayload(bool HasNextPage, SuwayomiMangaDto[]? Mangas);

internal sealed record MangasData(MangaNodeList? Mangas);

internal sealed record MangaNodeList(SuwayomiMangaDto[]? Nodes);

internal sealed record ChaptersData(ChapterNodeList? Chapters);

internal sealed record ChapterNodeList(SuwayomiChapterDto[]? Nodes);

internal sealed record FetchMangaAndChaptersData(FetchMangaAndChaptersPayload? FetchMangaAndChapters);

internal sealed record FetchMangaAndChaptersPayload(SuwayomiMangaDto? Manga, SuwayomiChapterDto[]? Chapters);

internal sealed record FetchChapterPagesData(FetchChapterPagesPayload? FetchChapterPages);

internal sealed record FetchChapterPagesPayload(string[]? Pages);

internal sealed record AboutServerData(AboutServerPayload? AboutServer);

internal sealed record AboutServerPayload(string? Name, string? Version, string? BuildType);
