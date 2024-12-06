using log4net;
using log4net.Config;

namespace Tranga.LibraryConnectors;

public abstract class LibraryConnector
{
    protected readonly ILog log;
    protected API.Schema.LibraryConnectors.LibraryConnector info;

    protected LibraryConnector(API.Schema.LibraryConnectors.LibraryConnector info)
    {
        log = LogManager.GetLogger(this.GetType());
        BasicConfigurator.Configure();
        this.info = info;
    }

    public abstract void UpdateLibrary();
    public abstract bool Test();
}