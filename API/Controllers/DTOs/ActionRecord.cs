using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Schema.ActionsContext.Actions;
using API.Schema.ActionsContext.Actions.Generic;

namespace API.Controllers.DTOs;

public sealed record ActionRecord : Identifiable
{
    public ActionRecord(Schema.ActionsContext.ActionRecord actionRecord) : base(actionRecord.Key)
    {
        Action = actionRecord.Action;
        PerformedAt = actionRecord.PerformedAt;
        MangaId = actionRecord is ActionWithMangaRecord m ? m.MangaId : null;
        ChapterId = actionRecord is ActionWithChapterRecord c ? c.ChapterId : null;
    }
    
    /// <summary>
    /// <inheritdoc cref="Schema.ActionsContext.ActionRecord.Action" />
    /// </summary>
    [Required]
    public ActionsEnum Action { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="Schema.ActionsContext.ActionRecord.PerformedAt" />
    /// </summary>
    [Required]
    public DateTime PerformedAt { get; init; }
    
    /// <summary>
    /// MangaId if Record is <see cref="ActionWithMangaRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MangaId { get; init; }
    
    /// <summary>
    /// ChapterId if Record is <see cref="ActionWithMangaRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChapterId { get; init; }
}