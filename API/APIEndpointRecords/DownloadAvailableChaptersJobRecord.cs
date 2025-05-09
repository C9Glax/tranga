using System.ComponentModel.DataAnnotations;

namespace API.APIEndpointRecords;

public record DownloadAvailableChaptersJobRecord([Required]string language, [Required]ulong recurrenceTimeMs, [Required]string localLibraryId);