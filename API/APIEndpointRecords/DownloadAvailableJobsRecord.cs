using System.ComponentModel.DataAnnotations;

namespace API.APIEndpointRecords;

public record DownloadAvailableJobsRecord([Required]ulong recurrenceTimeMs, [Required]string localLibraryId);