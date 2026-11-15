namespace Services.Manga.Database;

/// <summary>
/// Database entity holding a single runtime-configurable setting as a key/value pair.
/// </summary>
public sealed record DbSetting
{
    /// <summary>Well-known key for <see cref="Common.Settings.Settings.AllowNSFW"/>.</summary>
    public const string AllowNsfwKey = "AllowNSFW";

    /// <summary>The unique name of the setting.</summary>
    public required string Key { get; init; }

    /// <summary>The serialized value of the setting.</summary>
    public required string Value { get; set; }
}
