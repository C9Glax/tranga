using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.ActionsContext;

/// <summary>
/// Persists connector enabled/disabled state across restarts.
/// </summary>
[PrimaryKey("Name")]
public class ConnectorSettings(string name, bool enabled)
{
    [StringLength(32)]
    public string Name { get; init; } = name;

    public bool Enabled { get; set; } = enabled;
}
