export default class ApiKeys {
    static MangaList = (includeUnmonitored: boolean) => `MangaList/${includeUnmonitored}`;
    static Manga = (id: string, includes?: ('DownloadLinks' | 'MetadataLinks')[]) => `Manga/${id}?${includes?.join(',')}`;
    static MangaMatched = (id: string) => `Manga/${id}/Matched}`;

    static Metadata = (id: string) => `Metadata/${id}`;

    static Match = (id: string) => `Match/${id}`;

    static Chapters = (id: string) => `Chapters/${id}`;
}
