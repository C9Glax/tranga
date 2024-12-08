namespace Tranga.MangaConnectors;

public class TempleScan : HeanCms
{
    protected override string hostname => "https://api.templescan.net";
    protected override string mangaUrlPrefix { get; } = "/comic/";
    public TempleScan (GlobalBase clone) : base(clone, "TempleScan", ["en"])
    {
    }
}