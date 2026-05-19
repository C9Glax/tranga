namespace Extensions.Data;

public sealed class StringIdentifier(string id) : IIdentifier<StringIdentifier>
{
    private string Id { get; set; } = id;
    
    public static implicit operator string(StringIdentifier id) => id.Id;
    public static implicit operator StringIdentifier(string str) => new (str);

    public StringIdentifier FromString(string str) => new(str);
    public string StringRepresentation() => Id;
}