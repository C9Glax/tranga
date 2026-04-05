export default class ApiKeys {
    static MangaList = 'MangaList';
    static Manga = (id: string) => `Mangas/${id}`;
    static Metadata = (id: string) => `Metadata/${id}`;
}
