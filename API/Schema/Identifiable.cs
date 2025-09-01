using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Key")]
public abstract class Identifiable
{
    protected Identifiable()
    {
        this.Key = TokenGen.CreateToken(this.GetType());
    }

    protected Identifiable(string key)
    {
        this.Key = key;
    }
    
    [Required]
    [StringLength(TokenGen.MaximumLength, MinimumLength = TokenGen.MinimumLength)]
    public string Key { get; init; }

    public override string ToString() => Key;
}