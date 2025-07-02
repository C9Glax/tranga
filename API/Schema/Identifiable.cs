using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Key")]
public abstract class Identifiable
{
    public Identifiable()
    {
        this.Key = TokenGen.CreateToken(this.GetType());
    }
    
    public Identifiable(string key)
    {
        this.Key = key;
    }
    
    public string Key { get; init; }

    public override string ToString() => Key;
}