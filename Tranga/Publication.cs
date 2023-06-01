using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using static System.IO.UnixFileMode;

namespace Tranga;

/// <summary>
/// Contains information on a Publication (Manga)
/// </summary>
public readonly struct Publication
{
    public string sortName { get; }
    public string? author { get; }
    public Dictionary<string,string> altTitles { get; }
    // ReSharper disable trice MemberCanBePrivate.Global, trust
    public string? description { get; }
    public string[] tags { get; }
    public string? posterUrl { get; }
    public string? coverFileNameInCache { get; }
    public Dictionary<string,string> links { get; }
    public int? year { get; }
    public string? originalLanguage { get; }
    public string status { get; }
    public string folderName { get; }
    public string publicationId { get; }
    public string internalId { get; }

    private static readonly Regex LegalCharacters = new Regex(@"([A-z]*[0-9]* *\.*-*,*\]*\[*'*\'*\)*\(*~*!*)*");

    public Publication(string sortName, string? author, string? description, Dictionary<string,string> altTitles, string[] tags, string? posterUrl, string? coverFileNameInCache, Dictionary<string,string>? links, int? year, string? originalLanguage, string status, string publicationId)
    {
        this.sortName = sortName;
        this.author = author;
        this.description = description;
        this.altTitles = altTitles;
        this.tags = tags;
        this.coverFileNameInCache = coverFileNameInCache;
        this.posterUrl = posterUrl;
        this.links = links ?? new Dictionary<string, string>();
        this.year = year;
        this.originalLanguage = originalLanguage;
        this.status = status;
        this.publicationId = publicationId;
        this.folderName = string.Concat(LegalCharacters.Matches(sortName));
        while (this.folderName.EndsWith('.'))
            this.folderName = this.folderName.Substring(0, this.folderName.Length - 1);
        string onlyLowerLetters = string.Concat(this.sortName.ToLower().Where(Char.IsLetter));
        this.internalId = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{onlyLowerLetters}{this.year}"));
    }

    public string CreatePublicationFolder(string downloadDirectory)
    {
        string publicationFolder = Path.Join(downloadDirectory, this.folderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(publicationFolder, GroupRead | GroupWrite | GroupExecute | OtherRead | OtherWrite | OtherExecute | UserRead | UserWrite | UserExecute);
        return publicationFolder;
    }

    public void SaveSeriesInfoJson(string downloadDirectory)
    {
        string publicationFolder = CreatePublicationFolder(downloadDirectory);
        string seriesInfoPath = Path.Join(publicationFolder, "series.json");
        if(!File.Exists(seriesInfoPath))
            File.WriteAllText(seriesInfoPath,this.GetSeriesInfoJson());
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(seriesInfoPath, GroupRead | GroupWrite | OtherRead | OtherWrite | UserRead | UserWrite);
    }
    
    /// <returns>Serialized JSON String for series.json</returns>
    public string GetSeriesInfoJson()
    {
        SeriesInfo si = new (new Metadata(this.sortName, this.year.ToString() ?? string.Empty, this.status, this.description ?? ""));
        return System.Text.Json.JsonSerializer.Serialize(si);
    }

    //Only for series.json
    private struct SeriesInfo
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local we need it, trust
        [JsonRequired]public Metadata metadata { get; }
        public SeriesInfo(Metadata metadata) => this.metadata = metadata;
    }

    //Only for series.json what an abomination, why are all the fields not-null????
    private struct Metadata
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local we need them all, trust me
        [JsonRequired] public string type { get; }
        [JsonRequired] public string publisher { get; }
        // ReSharper disable twice IdentifierTypo
        [JsonRequired] public int comicid  { get; }
        [JsonRequired] public string booktype { get; }
        // ReSharper disable InconsistentNaming This one property is capitalized. Why?
        [JsonRequired] public string ComicImage { get; }
        [JsonRequired] public int total_issues { get; }
        [JsonRequired] public string publication_run { get; }
        [JsonRequired]public string name { get; }
        [JsonRequired]public string year { get; }
        [JsonRequired]public string status { get; }
        [JsonRequired]public string description_text { get; }

        public Metadata(string name, string year, string status, string description_text)
        {
            this.name = name;
            this.year = year;
            if(status.ToLower() == "ongoing" || status.ToLower() == "hiatus")
                this.status = "Continuing";
            else if (status.ToLower() == "completed" || status.ToLower() == "cancelled")
                this.status = "Ended";
            else
                this.status = status;
            this.description_text = description_text;
            
            //kill it with fire, but otherwise Komga will not parse
            type = "Manga";
            publisher = "";
            comicid = 0;
            booktype = "";
            ComicImage = "";
            total_issues = 0;
            publication_run = "";
        }
    }
}