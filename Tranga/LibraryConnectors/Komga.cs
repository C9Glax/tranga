﻿using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.LibraryConnectors;

/// <summary>
/// Provides connectivity to Komga-API
/// Can fetch and update libraries
/// </summary>
public class Komga : LibraryConnector
{
    public Komga(GlobalBase clone, string baseUrl, string username, string password)
        : base(clone, baseUrl, Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")), LibraryType.Komga)
    {
    }

    [JsonConstructor]
    public Komga(GlobalBase clone, string baseUrl, string auth) : base(clone, baseUrl, auth, LibraryType.Komga)
    {
    }

    public override string ToString()
    {
        return $"Komga {baseUrl}";
    }

    public override void UpdateLibrary()
    {
        logger?.LogInformation("Updating libraries.");
        foreach (KomgaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", auth, logger);
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private IEnumerable<KomgaLibrary> GetLibraries()
    {
        logger?.LogDebug("Getting Libraries");
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/v1/libraries", "Basic", auth, logger);
        if (data == Stream.Null)
        {
            logger?.LogDebug("No libraries returned");
            return Array.Empty<KomgaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            logger?.LogDebug("No libraries returned");
            return Array.Empty<KomgaLibrary>();
        }

        HashSet<KomgaLibrary> ret = new();

        foreach (JsonNode? jsonNode in result)
        {
            var jObject = (JsonObject?)jsonNode;
            string libraryId = jObject!["id"]!.GetValue<string>();
            string libraryName = jObject["name"]!.GetValue<string>();
            ret.Add(new KomgaLibrary(libraryId, libraryName));
        }

        return ret;
    }

    private struct KomgaLibrary
    {
        public string id { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string name { get; }

        public KomgaLibrary(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}