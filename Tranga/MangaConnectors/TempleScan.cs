namespace Tranga.MangaConnectors;

public class TempleScan : HeanCms
{
    protected override string hostname => "https://api.templescan.net";
    public TempleScan (GlobalBase clone) : base(clone, "TempleScan")
    {
    }
}