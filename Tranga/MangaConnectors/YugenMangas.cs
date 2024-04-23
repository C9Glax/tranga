namespace Tranga.MangaConnectors;

public class YugenMangas : HeanCms
{
    protected override string hostname => "https://api.yugenmangas.net";
    public YugenMangas (GlobalBase clone) : base(clone, "YugenMangas")
    {
    }
}