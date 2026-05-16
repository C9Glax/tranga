namespace Services.Libraries.Database;

public sealed record DbLibrary(string BaseUrl, string ApiKey)
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string BaseUrl { get; init; } = BaseUrl;
    public string ApiKey { get; init; } = ApiKey;
}