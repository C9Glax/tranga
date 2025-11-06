namespace API.Controllers.DTOs;

public sealed record Stats(int NumberManga, int NumberChapters, int MissingChapters, int DownloadedChapters, int SentNotifications, int ActionsTaken, int NumberAuthors, int NumberTags);