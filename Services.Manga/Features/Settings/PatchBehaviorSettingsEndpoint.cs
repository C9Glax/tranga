using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Settings;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PatchBehaviorSettingsEndpoint
{
    /// <summary>
    /// Update the Behavior-Settings
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">Settings have been updated</response>
    /// <response code="409">The setting is fixed by an environment variable and cannot be changed</response>
    public static async Task<Results<Ok, Conflict<string>>> Handle([FromServices]MangaContext mangaContext, [FromBody]PatchBehaviorSettingsRequest req, CancellationToken ct)
    {
        if (req.AllowNsfw is not { } allowNsfw)
            return TypedResults.Ok();

        if (Common.Settings.Settings.AllowNSFWLocked)
            return TypedResults.Conflict("AllowNSFW is set through the AllowNSFW environment variable and cannot be changed.");

        string value = allowNsfw.ToString();
        if (await mangaContext.Settings.FirstOrDefaultAsync(s => s.Key == DbSetting.AllowNsfwKey, ct) is { } existing)
            existing.Value = value;
        else
            await mangaContext.Settings.AddAsync(new DbSetting { Key = DbSetting.AllowNsfwKey, Value = value }, ct);
        await mangaContext.SaveChangesAsync(ct);

        Common.Settings.Settings.AllowNSFW = allowNsfw;
        return TypedResults.Ok();
    }

    /// <summary>
    /// Used in <see cref="PatchBehaviorSettingsEndpoint"/>
    /// </summary>
    /// <param name="AllowNsfw">Whether NSFW content is allowed in search results. Leave out to keep the current value</param>
    public sealed record PatchBehaviorSettingsRequest(bool? AllowNsfw);
}
