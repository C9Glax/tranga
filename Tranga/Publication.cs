using Newtonsoft.Json;

namespace Tranga;

/// <summary>
/// Contains information on a Publication (Manga)
/// </summary>
public readonly struct Publication
{
    public string sortName { get; }
    // ReSharper disable UnusedAutoPropertyAccessor.Global we need it, trust
    [JsonIgnore]public string[,] altTitles { get; }
    // ReSharper disable trice MemberCanBePrivate.Global, trust
    public string? description { get; }
    public string[] tags { get; }
    public string? posterUrl { get; }
    [JsonIgnore]public string[,]? links { get; }
    public int? year { get; }
    public string? originalLanguage { get; }
    public string status { get; }
    public string folderName { get; }
    public string downloadUrl { get; }

    public Publication(string sortName, string? description, string[,] altTitles, string[] tags, string? posterUrl, string[,]? links, int? year, string? originalLanguage, string status, string downloadUrl)
    {
        this.sortName = sortName;
        this.description = description;
        this.altTitles = altTitles;
        this.tags = tags;
        this.posterUrl = posterUrl;
        this.links = links;
        this.year = year;
        this.originalLanguage = originalLanguage;
        this.status = status;
        this.downloadUrl = downloadUrl;
        this.folderName = string.Concat(sortName.Split(Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray()));
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
            if(status == "ongoing" || status == "hiatus")
                this.status = "Continuing";
            else if (status == "completed" || status == "cancelled")
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