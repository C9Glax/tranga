export default class ApiKeys {
    static MangaList = (includeUnmonitored: boolean) => `MangaList/${includeUnmonitored}`;
    static Manga = (id: string) => `Mangas/${id}`;
}
