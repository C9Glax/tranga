using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Extensions.Data;

namespace Extensions.Extensions;

/// <summary>
/// Ported from https://github.com/keiyoushi/extensions-source/tree/main/src/en/asurascans
/// </summary>
public sealed class AsuraScans : IDownloadExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("0199a6e4-2b7a-7f1e-9c4a-5e2d8b6c1a30");

    public string Name { get; init; } = "AsuraScans";

    public Language[] SupportedLanguages { get; init; } = ["en"!];

    public string BaseUrl { get; init; } = "https://asurascans.com";

    public string IconUrl { get; init; } = "https://asurascans.com/images/logo.webp";

    private const string ApiUrl = "https://api.asurascans.com/api";

    private const int SearchLimit = 32;

    private static readonly RequestClient RequestClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 8,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    #region Search

    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        UriBuilder builder = new($"{ApiUrl}/series");
        builder.AddQueryParameter("offset", "0")
            .AddQueryParameter("limit", SearchLimit.ToString());
        if (!string.IsNullOrWhiteSpace(query.Title))
            builder.AddQueryParameter("search", Uri.EscapeDataString(query.Title));
        if (query.Author is { } author)
            builder.AddQueryParameter("author", Uri.EscapeDataString(author));
        if (query.Artist is { } artist)
            builder.AddQueryParameter("artist", Uri.EscapeDataString(artist));

        if (await GetJson<SeriesSearchResponseDto>(builder.Uri, ct) is not { Data: { } series })
            return null;

        List<Task<MangaInfo?>> tasks = series.Select(s => ParseSeries(s, ct)).ToList();
        await Task.WhenAll(tasks);

        return tasks
            .Where(t => t is { IsCompletedSuccessfully: true, Result: not null })
            .Select(t => t.Result!)
            .ToList();
    }

    private async Task<MangaInfo?> ParseSeries(SeriesSummaryDto series, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(series.Slug) || string.IsNullOrEmpty(series.Title))
            return null;
        if (await FetchImage(series.Cover, ct) is not { } cover)
            return null;

        string url = $"{BaseUrl}{series.PublicUrl ?? $"/comics/{series.Slug}"}";
        return new MangaInfo(this.Identifier, series.Title, url, series.Slug, cover);
    }

    #endregion

    #region Identifier

    public string? ParseIdentifierFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Segments.Length < 3)
            return null;
        if (!uri.Segments[1].Equals("comics/", StringComparison.OrdinalIgnoreCase))
            return null;
        string slug = uri.Segments[2].Trim('/');
        return string.IsNullOrEmpty(slug) ? null : slug;
    }

    #endregion

    #region Chapters

    public async Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct)
    {
        string slug = mangaInfo.Identifier;
        Uri url = new($"{ApiUrl}/series/{slug}/chapters");

        if (await GetJson<ChapterListResponseDto>(url, ct) is not { Data: { } chapters })
            return null;

        List<ChapterInfo> result = [];
        foreach (ChapterDto chapter in chapters)
        {
            // Premium chapters require an account/subscription to unlock, which Tranga does not support.
            if (chapter.IsLocked)
                continue;
            if (string.IsNullOrEmpty(chapter.Slug))
                continue;

            string number = FormatChapterNumber(chapter.Number);
            string chapterUrl = $"{BaseUrl}/comics/{slug}/chapter/{number}";
            result.Add(new ChapterInfo(this.Identifier, number, chapterUrl, chapter.Slug, Title: chapter.Title));
        }

        return result;
    }

    private static string FormatChapterNumber(float number) => number.ToString("0.####", CultureInfo.InvariantCulture);

    #endregion

    #region Images

    public async Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        Uri chapterUri = new(chapterInfo.Url);
        if (chapterUri.Segments.Length < 5)
            return null;
        string slug = chapterUri.Segments[2].Trim('/');
        string number = chapterUri.Segments[^1].Trim('/');
        Uri url = new($"{ApiUrl}/series/{slug}/chapters/{number}");

        if (await GetJson<ChapterPagesResponseDto>(url, ct) is not { Data.Chapter.Pages: { Count: > 0 } pages })
            return null;

        List<Task<TrangaImage?>> tasks = pages.Select(p => FetchImage(p.Url, ct)).ToList();
        await Task.WhenAll(tasks);

        if (tasks.Any(t => t is not { IsCompletedSuccessfully: true, Result: not null }))
            return null;

        return tasks.Select((t, index) => new ChapterImage(this.Identifier, chapterInfo.Identifier, index, t.Result!))
            .ToList();
    }

    #endregion

    #region Utilities

    private static async Task<T?> GetJson<T>(Uri url, CancellationToken ct)
    {
        HttpResponseMessage response = await RequestClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return default;
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    private static async Task<TrangaImage?> FetchImage(string? url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        HttpResponseMessage response = await RequestClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        TrangaImage image = new();
        Stream data = await response.Content.ReadAsStreamAsync(ct);
        await data.CopyToAsync(image, ct);
        return image;
    }

    #endregion

    #region Data

    private sealed class SeriesSearchResponseDto
    {
        [JsonPropertyName("data")] public List<SeriesSummaryDto>? Data { get; init; }
    }

    private sealed class SeriesSummaryDto
    {
        [JsonPropertyName("slug")] public string? Slug { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("cover")] public string? Cover { get; init; }
        [JsonPropertyName("public_url")] public string? PublicUrl { get; init; }
    }

    private sealed class ChapterListResponseDto
    {
        [JsonPropertyName("data")] public List<ChapterDto>? Data { get; init; }
    }

    private sealed class ChapterDto
    {
        [JsonPropertyName("number")] public float Number { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("slug")] public string? Slug { get; init; }
        [JsonPropertyName("is_locked")] public bool IsLocked { get; init; }
    }

    private sealed class ChapterPagesResponseDto
    {
        [JsonPropertyName("data")] public ChapterPagesDataDto? Data { get; init; }
    }

    private sealed class ChapterPagesDataDto
    {
        [JsonPropertyName("chapter")] public ChapterPagesDto? Chapter { get; init; }
    }

    private sealed class ChapterPagesDto
    {
        [JsonPropertyName("pages")] public List<PageDto>? Pages { get; init; }
    }

    private sealed class PageDto
    {
        [JsonPropertyName("url")] public string? Url { get; init; }
    }

    #endregion
}
