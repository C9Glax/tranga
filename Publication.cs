namespace Tranga;

public struct Publication
{
    public string sortName { get; }
    public string[] titles { get;  }
    public string description { get; }
    public string[] tags { get; }
    public string posterUrl { get; } //maybe there is a better way?
    

    public Publication(string sortName, string description, string[] titles, string[] tags, string posterUrl)
    {
        this.sortName = sortName;
        this.description = description;
        this.titles = titles;
        this.tags = tags;
        this.posterUrl = posterUrl;
    }
}