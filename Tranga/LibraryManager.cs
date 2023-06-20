using System.Net;
using System.Net.Http.Headers;
using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.LibraryManagers;

namespace Tranga;

public abstract class LibraryManager
{
    public enum LibraryType : byte
    {
        Komga = 0,
        Kavita = 1
    }

    public LibraryType libraryType { get; }
    public string baseUrl { get; }
    public string auth { get; } //Base64 encoded, if you use your password everywhere, you have problems
    protected Logger? logger;
    
    /// <param name="baseUrl">Base-URL of Komga instance, no trailing slashes(/)</param>
    /// <param name="auth">Base64 string of username and password (username):(password)</param>
    /// <param name="logger"></param>
    /// <param name="libraryType"></param>
    protected LibraryManager(string baseUrl, string auth, Logger? logger, LibraryType libraryType)
    {
        this.baseUrl = baseUrl;
        this.auth = auth;
        this.logger = logger;
        this.libraryType = libraryType;
    }
    public abstract void UpdateLibrary();

    public void AddLogger(Logger newLogger)
    {
        this.logger = newLogger;
    }

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
            HttpResponseMessage response = client.Send(requestMessage);
            logger?.WriteLine("LibraryManager", $"GET {url} -> {(int)response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakeRequest(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth, logger);
            else if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStream();
            else
                return Stream.Null;
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
            logger?.WriteLine("LibraryManager", $"POST {url} -> {(int)response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakePost(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth, logger);
            else if (response.IsSuccessStatusCode)
                return true;
            else 
                return false;
        }
    }
    
    public class LibraryManagerJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(LibraryManager));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["libraryType"]!.Value<Int64>() == (Int64)LibraryType.Komga)
                return jo.ToObject<Komga>(serializer)!;

            if (jo["libraryType"]!.Value<Int64>() == (Int64)LibraryType.Kavita)
                return jo.ToObject<Kavita>(serializer)!;

            throw new Exception();
        }

        public override bool CanWrite => false;

        /// <summary>
        /// Don't call this
        /// </summary>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new Exception("Dont call this");
        }
    }
}