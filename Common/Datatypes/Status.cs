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
        "hiatus" => ReleaseStatus.Hiatus,
        "complete" => ReleaseStatus.Complete,
        "completed" => ReleaseStatus.Complete,
        _ => null
    };
}