using Logging;
using Tranga.Jobs;
using Tranga.MangaConnectors;

namespace Tranga;

public partial class Tranga : GlobalBase
{
    public bool keepRunning;
    public JobBoss jobBoss;
    private Server _server;
    private HashSet<MangaConnector> _connectors;

    public Tranga(Logger? logger, TrangaSettings settings) : base(logger, settings)
    {
        keepRunning = true;
        _connectors = new HashSet<MangaConnector>()
        {  
            new Manganato(this),
            new Mangasee(this),
            new MangaDex(this),
            new MangaKatana(this)
        };
        jobBoss = new(this, this._connectors);
        StartJobBoss();
        this._server = new Server(this);
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

    public IEnumerable<MangaConnector> GetConnectors()
    {
        return _connectors;
    }

    public Manga? GetPublicationById(string internalId)
    {
        if (cachedPublications.Exists(publication => publication.internalId == internalId))
            return cachedPublications.First(publication => publication.internalId == internalId);
        return null;
    }

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
                jobBoss.CheckJobs();
                Thread.Sleep(100);
            }

            foreach (MangaConnector connector in _connectors)
            {
                
            }
        });
        t.Start();
    }
}