using Logging;

namespace Tranga.LibraryManagers;

public class Kavita : LibraryManager
{
    public Kavita(string baseUrl, string auth, Logger? logger) : base(baseUrl, auth, logger)
    {
    }

    public override void UpdateLibrary()
    {
        throw new NotImplementedException();
    }
}