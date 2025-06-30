using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Key")]
public abstract class Identifiable(string key)
{
    public string Key { get; init; } = key;

    public override string ToString() => Key;
}