namespace Extensions.Data;

public interface IBook<TSeriesIdentifier> where TSeriesIdentifier : IIdentifier
{
    public TSeriesIdentifier Id { get; init; }
    
    public string Title { get; init; }
}