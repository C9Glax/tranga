using System.Net;
using System.Net.Http.Headers;
using Logging;

namespace Tranga.LibraryConnectors;

public abstract class LibraryConnector : GlobalBase
{
    public enum LibraryType : byte
    {
        Komga = 0,
        Kavita = 1
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public LibraryType libraryType { get; }
    public string baseUrl { get; }
    // ReSharper disable once MemberCanBeProtected.Global
    public string auth { get; } //Base64 encoded, if you use your password everywhere, you have problems
    private DateTime? _updateLibraryRequested = null;
    private readonly Thread? _libraryBufferThread = null;
    private const int NoChangeTimeout = 2, BiggestInterval = 20;
    
    protected LibraryConnector(GlobalBase clone, string baseUrl, string auth, LibraryType libraryType) : base(clone)
    {
        Log($"Creating libraryConnector {Enum.GetName(libraryType)}");
        if (!baseUrlRex.IsMatch(baseUrl))
            throw new ArgumentException("Base url does not match pattern");
        if(auth == "")
            throw new ArgumentNullException(nameof(auth), "Auth can not be empty");
        this.baseUrl = baseUrlRex.Match(baseUrl).Value;
        this.auth = auth;
        this.libraryType = libraryType;

        if (TrangaSettings.bufferLibraryUpdates)
        {
            _libraryBufferThread = new(CheckLibraryBuffer);
            _libraryBufferThread.Start();
        }
    }

    private void CheckLibraryBuffer()
    {
        while (true)
        {
            if (_updateLibraryRequested is not null && DateTime.Now.Subtract((DateTime)_updateLibraryRequested) > TimeSpan.FromMinutes(NoChangeTimeout)) //If no updates have been requested for NoChangeTimeout minutes, update library
            {
                UpdateLibraryInternal();
                _updateLibraryRequested = null;
            }
            Thread.Sleep(100);
        }
    }

    public void UpdateLibrary()
    {
        _updateLibraryRequested ??= DateTime.Now;
        if (!TrangaSettings.bufferLibraryUpdates)
        {
            UpdateLibraryInternal();
            return;
        }else if (_updateLibraryRequested is not null &&
                  DateTime.Now.Subtract((DateTime)_updateLibraryRequested) > TimeSpan.FromMinutes(BiggestInterval)) //If the last update has been more than BiggestInterval minutes ago, update library
        {
            UpdateLibraryInternal();
            _updateLibraryRequested = null;
        }
        else if(_updateLibraryRequested is not null)
        {
            Log($"Buffering Library Updates (Updates in latest {((DateTime)_updateLibraryRequested).Add(TimeSpan.FromMinutes(BiggestInterval)).Subtract(DateTime.Now)} or {((DateTime)_updateLibraryRequested).Add(TimeSpan.FromMinutes(NoChangeTimeout)).Subtract(DateTime.Now)})");
        }
    }
    
    protected abstract void UpdateLibraryInternal();
    internal abstract bool Test();

    protected static class NetClient
    {
        public static Stream MakeRequest(string url, string authScheme, string auth, Logger? logger)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authScheme, auth);
            
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };
            try
            {

                HttpResponseMessage response = client.Send(requestMessage);
                logger?.WriteLine("LibraryManager.NetClient",
                    $"GET {url} -> {(int)response.StatusCode}: {response.ReasonPhrase}");

                if (response.StatusCode is HttpStatusCode.Unauthorized &&
                    response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                    return MakeRequest(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth, logger);
                else if (response.IsSuccessStatusCode)
                    return response.Content.ReadAsStream();
                else
                    return Stream.Null;
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case HttpRequestException:
                        logger?.WriteLine("LibraryManager.NetClient", $"Failed to make Request:\n\r{e}\n\rContinuing.");
                        break;
                    default:
                        throw;
                }
                return Stream.Null;
            }
        }

        public static bool MakePost(string url, string authScheme, string auth, Logger? logger)
        {
            HttpClient client = new()
            {
                DefaultRequestHeaders =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue(authScheme, auth).ToString() }
                }
            };
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            HttpResponseMessage response = client.Send(requestMessage);
            logger?.WriteLine("LibraryManager.NetClient", $"POST {url} -> {(int)response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakePost(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth, logger);
            else if (response.IsSuccessStatusCode)
                return true;
            else 
                return false;
        }
    }
}