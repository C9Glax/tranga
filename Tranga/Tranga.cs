using Logging;
using Tranga.Jobs;
using Tranga.MangaConnectors;

namespace Tranga;

public partial class Tranga : GlobalBase
{
    public bool keepRunning;
    public JobBoss jobBoss;
    private Server.Server _server;

    public Tranga(Logger? logger) : base(logger)
    {
        Log("\n\n _______                                   \n|_     _|.----..---.-..-----..-----..---.-.\n  |   |  |   _||  _  ||     ||  _  ||  _  |\n  |___|  |__|  |___._||__|__||___  ||___._|\n                             |_____|       \n\n");
        keepRunning = true;
        _connectors = new HashSet<MangaConnector>()
        {  
            new Manganato(this),
            new Mangasee(this),
            new MangaDex(this),
            new MangaKatana(this),
            new Mangaworld(this),
            new Bato(this),
            new MangaLife(this),
            new ManhuaPlus(this),
            new MangaHere(this),
        };
        foreach(DirectoryInfo dir in new DirectoryInfo(Path.GetTempPath()).GetDirectories("trangatemp"))//Cleanup old temp folders
            dir.Delete();
        jobBoss = new(this, this._connectors);
        StartJobBoss();
        this._server = new Server.Server(this);
        string[] emojis = { "(•‿•)", "(づ \u25d5‿\u25d5 )づ", "( \u02d8\u25bd\u02d8)っ\u2668", "=\uff3e\u25cf \u22cf \u25cf\uff3e=", "（ΦωΦ）", "(\u272a\u3268\u272a)", "( ﾉ･o･ )ﾉ", "（〜^\u2207^ )〜", "~(\u2267ω\u2266)~","૮ \u00b4• ﻌ \u00b4• ა", "(\u02c3ᆺ\u02c2)", "(=\ud83d\udf66 \u0f1d \ud83d\udf66=)"};
        SendNotifications("Tranga Started", emojis[Random.Shared.Next(0,emojis.Length-1)]);
    }

    public MangaConnector? GetConnector(string name)
    {
        foreach(MangaConnector mc in _connectors)
            if (mc.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return mc;
        return null;
    }

    public bool TryGetConnector(string name, out MangaConnector? connector)
    {
        connector = GetConnector(name);
        return connector is not null;
    }

    public Dictionary<string, string[]> GetConnectors()
    {
        return _connectors.ToDictionary(c => c.name, c => c.SupportedLanguages);
    }

    public Manga? GetPublicationById(string internalId) => GetCachedManga(internalId);

    public bool TryGetPublicationById(string internalId, out Manga? manga)
    {
        manga = GetPublicationById(internalId);
        return manga is not null;
    }

    private void StartJobBoss()
    {
        Thread t = new (() =>
        {
            while (keepRunning)
            {
                if(!TrangaSettings.aprilFoolsMode || !IsAprilFirst())
                    jobBoss.CheckJobs();
                else
                    Log("April Fools Mode in Effect");
                Thread.Sleep(100);
            }
        });
        t.Start();
    }

    private bool IsAprilFirst()
    {
        //UTC 01 Apr +-12hrs
        DateTime start = new DateTime(DateTime.Now.Year, 03, 31, 12, 0, 0, DateTimeKind.Utc);
        DateTime end = new DateTime(DateTime.Now.Year, 04, 02, 12, 0, 0, DateTimeKind.Utc);
        if (DateTime.UtcNow > start && DateTime.UtcNow < end)
            return true;
        return false;
    }
}