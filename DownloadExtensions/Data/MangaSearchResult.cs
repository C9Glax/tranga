namespace DownloadExtensions.Data;

public sealed class MangaSearchResult<T> : List<MangaInfo<T>> where T : IDownloadExtension<T>;