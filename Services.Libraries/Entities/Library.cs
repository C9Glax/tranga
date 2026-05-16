namespace Services.Libraries.Entities;

public sealed record Library(Guid Id, string BaseUrl)
{
    public Guid Id { get; init; } = Id;
    public string BaseUrl { get; init; } = BaseUrl;
}