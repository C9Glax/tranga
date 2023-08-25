using Logging;
using Tranga.Jobs;
using Tranga.MangaConnectors;

namespace Tranga;

public partial class Tranga : GlobalBase
{
    public bool keepRunning;
    private JobBoss _jobBoss;
    private Server server;
    private HashSet<MangaConnector> connectors;

    public Tranga(Logger? logger, TrangaSettings settings) : base(logger, settings)
    {
        keepRunning = true;
        _jobBoss = new(this);
        connectors = new HashSet<MangaConnector>()
        {  
            new Manganato(this),
            new Mangasee(this),
            new MangaDex(this),
            new MangaKatana(this)
        };
        StartJobBoss();
        this.server = new Server(this);
    }

    public MangaConnector? GetConnector(string name)
    {
        foreach(MangaConnector mc in connectors)
            if (mc.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return mc;
        return null;
    }

    public IEnumerable<MangaConnector> GetConnectors()
    {
        return connectors;
    }

    private void StartJobBoss()
    {
        Thread t = new (() =>
        {
            while (keepRunning)
            {
                _jobBoss.CheckJobs();
                Thread.Sleep(1000);
            }
        });
        t.Start();
    }
}