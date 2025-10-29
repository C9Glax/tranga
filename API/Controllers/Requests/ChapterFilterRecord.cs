namespace API.Controllers.Requests;

public record ChapterFilterRecord(bool? Downloaded, string? Name, int? VolumeNumber, string? ChapterNumber);