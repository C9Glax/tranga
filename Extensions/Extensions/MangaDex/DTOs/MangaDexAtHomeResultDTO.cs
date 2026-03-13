namespace Extensions.Extensions.MangaDex.DTOs;
// ReSharper disable ClassNeverInstantiated.Global

internal record MangaDexAtHomeResultDTO(string Result, string BaseUrl, MangaDexAtHomeChapterDTO Chapter) : MangaDexResponse(Result);

internal record MangaDexAtHomeChapterDTO(string Hash, string[] Data, string[] DataSaver);