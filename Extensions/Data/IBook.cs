namespace Extensions.Data;

public interface IBook<TSeriesIdentifier> where TSeriesIdentifier : IIdentifier<TSeriesIdentifier>
{
    public TSeriesIdentifier Id { get; init; }
    
    public string Title { get; init; }
}