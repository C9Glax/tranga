using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("MangaConnectorId")]
public class MangaConnector
{
    [MaxLength(64)]
    public string MangaConnectorId { get; init; } = TokenGen.CreateToken(typeof(MangaConnector), 64);
}