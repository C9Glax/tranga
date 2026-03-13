namespace DownloadExtensions.Data;

public sealed class MangaSearchResult<T> : List<Manga<T>> where T : IExtension<T>;