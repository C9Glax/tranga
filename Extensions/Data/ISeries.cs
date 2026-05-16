namespace Extensions.Data;

public interface ISeries<TSeriesIdentifier> where TSeriesIdentifier : IIdentifier
{
    public TSeriesIdentifier Id { get; init; }
    
    public string Name { get; init; }

    public string Summary { get; init; }
}