namespace Common.Datatypes;

public enum ReleaseStatus
{
    Ongoing,
    Complete,
    Hiatus,
    Cancelled
}

public static class ReleaseStatusHelpers
{
    public static ReleaseStatus? ParseStatus(this string? status) => status?.ToLowerInvariant() switch
    {
        "ongoing" => ReleaseStatus.Ongoing,
        "releasing" => ReleaseStatus.Ongoing,
        // MyAnimeList spells "ongoing" as "currently_publishing"
        "currently_publishing" => ReleaseStatus.Ongoing,
        "hiatus" => ReleaseStatus.Hiatus,
        // MyAnimeList spells "hiatus" as "on_hiatus"
        "on_hiatus" => ReleaseStatus.Hiatus,
        "complete" => ReleaseStatus.Complete,
        "completed" => ReleaseStatus.Complete,
        // AniList spells "completed" as "finished"
        "finished" => ReleaseStatus.Complete,
        "cancelled" => ReleaseStatus.Cancelled,
        "canceled" => ReleaseStatus.Cancelled,
        // MyAnimeList spells "cancelled" as "discontinued"
        "discontinued" => ReleaseStatus.Cancelled,
        _ => null
    };
}