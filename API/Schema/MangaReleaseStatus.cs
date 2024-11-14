namespace API.Schema;

public enum MangaReleaseStatus : byte
{
    Continuing = 0,
    Completed = 1,
    OnHiatus = 2,
    Cancelled = 3,
    Unreleased = 4
}