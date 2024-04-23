namespace Tranga.MangaConnectors;

public class PerfScan : HeanCms
{
    protected override string hostname => "https://api.perf-scan.fr";
    public PerfScan (GlobalBase clone) : base(clone, "PerfScan", ["en"])
    {
    }
}