namespace Tranga.MangaConnectors;

public class OmegaScans : HeanCms
{
    protected override string hostname => "https://api.omegascans.org";
    public OmegaScans(GlobalBase clone) : base(clone, "OmegaScans", ["en"])
    {
    }
}