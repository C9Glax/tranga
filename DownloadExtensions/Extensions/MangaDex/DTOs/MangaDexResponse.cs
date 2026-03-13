namespace DownloadExtensions.Extensions.MangaDex.DTOs;

public abstract record MangaDexResponse(string Result);

internal abstract record MangaDexEntityResponse<T>(string Result, string Response, T Data) : MangaDexResponse(Result);

internal abstract record MangaDexCollectionResponse<T>(string Result, string Response, T[] Data, int Limit, int Offset, int Total) : MangaDexEntityResponse<T[]>(Result, Response, Data);