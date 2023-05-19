using Newtonsoft.Json;

namespace Tranga;

public struct Publication
{
    public string sortName { get; }
    [JsonIgnore]public string[,] altTitles { get; }
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
        this.folderName = string.Concat(sortName.Split(Path.GetInvalidPathChars()));
    }

    public string GetSeriesInfo()
    {
        SeriesInfo si = new (new Metadata(this.sortName, this.year.ToString() ?? string.Empty, this.status, this.description ?? ""));
        return System.Text.Json.JsonSerializer.Serialize(si);
    }
    
    internal struct SeriesInfo
    {
        [JsonRequired]public Metadata metadata { get; }
        public SeriesInfo(Metadata metadata) => this.metadata = metadata;
    }
        
    internal struct Metadata
    {
        [JsonRequired]public string name { get; }
        [JsonRequired]public string year { get; }
        [JsonRequired]public string status { get; }
        // ReSharper disable twice InconsistentNaming
        [JsonRequired]public string description_text { get; }

        public Metadata(string name, string year, string status, string description_text)
        {
            this.name = name;
            this.year = year;
            this.status = status;
            this.description_text = description_text;
        }
    }
}