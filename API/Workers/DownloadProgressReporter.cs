using API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace API.Workers;

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum DownloadPhase
{
    FetchingUrls,
    DownloadingImages,
    PackagingArchive,
    Completed,
    Failed
}

public record DownloadProgressData(
    string WorkerId,
    string ChapterId,
    string MangaId,
    string MangaName,
    string ChapterNumber,
    int CurrentImage,
    int TotalImages,
    float Progress,
    DownloadPhase Phase
);

public class DownloadProgressReporter(IHubContext<DownloadProgressHub> hubContext)
{
    public async Task ReportProgress(BaseWorker worker, string chapterId, string mangaId, string mangaName,
        string chapterNumber, int currentImage, int totalImages, DownloadPhase phase)
    {
        float progress = totalImages > 0 ? (float)currentImage / totalImages : 0f;

        worker.Progress = progress;
        worker.CurrentStep = currentImage;
        worker.TotalSteps = totalImages;
        worker.ProgressDescription = phase switch
        {
            DownloadPhase.FetchingUrls => "Fetching image URLs",
            DownloadPhase.DownloadingImages => $"{currentImage} / {totalImages} images ({progress:P0})",
            DownloadPhase.PackagingArchive => "Creating archive",
            DownloadPhase.Completed => "Download complete",
            DownloadPhase.Failed => "Download failed",
            _ => null
        };

        DownloadProgressData data = new(
            worker.Key, chapterId, mangaId, mangaName, chapterNumber,
            currentImage, totalImages, progress, phase
        );

        await Task.WhenAll(
            hubContext.Clients.Group($"worker-{worker.Key}").SendAsync("DownloadProgress", data),
            hubContext.Clients.Group($"manga-{mangaId}").SendAsync("DownloadProgress", data),
            hubContext.Clients.Group("all-downloads").SendAsync("DownloadProgress", data)
        );
    }

    public Task ReportPhaseChanged(BaseWorker worker, string chapterId, string mangaId, string mangaName,
        string chapterNumber, DownloadPhase phase)
    {
        return ReportProgress(worker, chapterId, mangaId, mangaName, chapterNumber, 0, 0, phase);
    }

    public Task ReportCompleted(BaseWorker worker, string chapterId, string mangaId, string mangaName,
        string chapterNumber, int totalImages)
    {
        return ReportProgress(worker, chapterId, mangaId, mangaName, chapterNumber, totalImages, totalImages,
            DownloadPhase.Completed);
    }

    public Task ReportFailed(BaseWorker worker, string chapterId, string mangaId, string mangaName,
        string chapterNumber)
    {
        return ReportProgress(worker, chapterId, mangaId, mangaName, chapterNumber, 0, 0, DownloadPhase.Failed);
    }
}
