namespace Extensions.Extensions.Suwayomi;

/// <summary>
/// How a Suwayomi source or extension is classified by its extension store.
/// <para>
/// Suwayomi also exposes a deprecated <c>isNsfw</c> boolean, but that collapses <see cref="Mixed"/> into
/// <see cref="Nsfw"/> — which would wrongly lock out sites that merely allow adult content alongside everything else
/// (MangaDex and Weeb Central are both <see cref="Mixed"/>).
/// </para>
/// </summary>
public enum SuwayomiContentWarning
{
    /// <summary>The source carries no adult content.</summary>
    Safe,

    /// <summary>The source carries both regular and adult content; individual entries have to be judged on their own.</summary>
    Mixed,

    /// <summary>The source exists for adult content.</summary>
    Nsfw
}
