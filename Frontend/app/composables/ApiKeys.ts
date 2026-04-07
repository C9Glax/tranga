export default class ApiKeys {
    static MangaList = 'MangaList';
    static Manga = (id: string) => `Mangas/${id}`;
    static MangaMetadataEntries = (id: string) => `Mangas/${id}/MetadataEntries`;
    static MetadataList = 'MetadataList';
    static Metadata = (id: string) => `Metadata/${id}`;
    static MetadataManga = (id: string) => `Metadata/${id}/Manga`;
    static MetadataRelatedMangas = (id: string) => `Metadata/${id}/RelatedMangas`;
    static DownloadExtensions = 'DownloadExtensions';
    static MetadataExtensions = 'MetadataExtensions';
}
