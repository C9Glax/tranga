using Microsoft.AspNetCore.Http.HttpResults;

namespace Services.Manga.Features.Settings;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetBehaviorSettingsEndpoint
{
    /// <summary>
    /// Get the Behavior-Settings
    /// </summary>
    /// <returns>The current Behavior-Settings</returns>
    /// <response code="200">The current Behavior-Settings</response>
    public static Ok<BehaviorSettings> Handle() =>
        TypedResults.Ok(new BehaviorSettings(Common.Settings.Settings.AllowNSFW, Common.Settings.Settings.AllowNSFWLocked));

    /// <summary>
    /// Behavior-Settings
    /// </summary>
    /// <param name="AllowNsfw">Whether NSFW content is allowed in search results</param>
    /// <param name="AllowNsfwLocked">Whether <paramref name="AllowNsfw"/> is fixed by the <c>AllowNSFW</c> environment variable and cannot be changed</param>
    public sealed record BehaviorSettings(bool AllowNsfw, bool AllowNsfwLocked);
}
