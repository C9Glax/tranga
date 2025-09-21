using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Workers;

namespace API.Controllers.Requests;

public record PatchLibraryRefreshRecord
{
    /// <summary>
    /// When to refresh the Library
    /// </summary>
    [Required]
    [Description("When to refresh the Library")]
    public required LibraryRefreshSetting Setting { get; init; }
    
    /// <summary>
    /// When <see cref="LibraryRefreshSetting.WhileDownloading"/> is selected, update the time between refreshes
    /// </summary>
    [Description("When WhileDownloadingis selected, update the time between refreshes")]
    public int? RefreshLibraryWhileDownloadingEveryMinutes { get; init; }
}