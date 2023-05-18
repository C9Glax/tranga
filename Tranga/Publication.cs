namespace Tranga;

public struct Publication
{
    public string sortName { get; }
    public string[,] altTitles { get;  }
    public string? description { get; }
    public string[] tags { get; }
    public string posterUrl { get; } //maybe there is a better way?
    public string[,] links { get; }
    public int? year { get; }
    public string? originalLanguage { get; }
    public string status { get; }
    
    public Connector connector { get; }
    public string downloadUrl { get; }

    public Publication(string sortName, string? description, string[,] altTitles, string[] tags, string posterUrl, string[,] links, int? year, string? originalLanguage, string status, Connector connector, string downloadUrl)
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
        this.connector = connector;
        this.downloadUrl = downloadUrl;
    }
}