export default class ApiKeys {
    static MangaList = 'MangaList';
    static Manga = (id: string) => `Mangas/${id}`;
    static MangaMetadataEntries = (id: string) => `Mangas/${id}/MetadataEntries`;
    static MetadataList = 'MetadataList';
    static Metadata = (id: string) => `Metadata/${id}`;
    static MetadataManga = (id: string) => `Metadata/${id}/Manga`;
    static MetadataRelatedMangas = (id: string) => `Metadata/${id}/RelatedMangas`;
    static MangaDownloadLinks = (id: string) => `Mangas/${id}/DownloadLinks`;
    static DownloadExtensions = 'DownloadExtensions';
    static MetadataExtensions = 'MetadataExtensions';
    static MangaTasks = (id: string) => `Mangas/${id}/tasks`;
    static MangaDownloadTasks = (id: string) => `Mangas/${id}/tasks/downloads`;
    static DownloadTasks = 'tasks/downloads';
    static Tasks = 'tasks';
}
