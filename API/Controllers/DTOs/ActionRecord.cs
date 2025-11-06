using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Schema.ActionsContext.Actions;
using API.Schema.ActionsContext.Actions.Generic;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace API.Controllers.DTOs;

public sealed record ActionRecord : Identifiable
{
    public ActionRecord(Schema.ActionsContext.ActionRecord actionRecord) : base(actionRecord.Key)
    {
        Action = actionRecord.Action;
        PerformedAt = actionRecord.PerformedAt;
        MangaId = actionRecord is IActionWithMangaRecord manga ? manga.MangaId : null;
        ChapterId = actionRecord is IActionWithChapterRecord chapter ? chapter.ChapterId : null;
        From = actionRecord is DataMovedActionRecord from ? from.From : null;
        To = actionRecord is DataMovedActionRecord to ? to.To : null;
        Filename = actionRecord is CoverDownloadedActionRecord filename ? filename.Filename : null;
        MetadataFetcher = actionRecord is MetadataUpdatedActionRecord metadata ? metadata.MetadataFetcher : null;
    }
    
    /// <summary>
    /// <inheritdoc cref="Schema.ActionsContext.ActionRecord.Action" />
    /// </summary>
    [Required]
    public Actions Action { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="Schema.ActionsContext.ActionRecord.PerformedAt" />
    /// </summary>
    [Required]
    public DateTime PerformedAt { get; init; }
    
    /// <summary>
    /// MangaId if Record is <see cref="IActionWithMangaRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MangaId { get; init; }
    
    /// <summary>
    /// ChapterId if Record is <see cref="IActionWithMangaRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChapterId { get; init; }
    
    /// <summary>
    /// FromPath if Record is <see cref="Schema.ActionsContext.Actions.DataMovedActionRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; init; }
    
    /// <summary>
    /// ToPath if Record is <see cref="Schema.ActionsContext.Actions.DataMovedActionRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? To { get; init; }
    
    /// <summary>
    /// Filename if Record is <see cref="Schema.ActionsContext.Actions.CoverDownloadedActionRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filename { get; init; }
    
    /// <summary>
    /// <see cref="Schema.MangaContext.MetadataFetchers.MetadataFetcher"/> if Record is <see cref="Schema.ActionsContext.Actions.MetadataUpdatedActionRecord"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetadataFetcher { get; init; }
}