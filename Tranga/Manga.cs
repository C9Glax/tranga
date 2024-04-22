using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using static System.IO.UnixFileMode;

namespace Tranga;

/// <summary>
/// Contains information on a Publication (Manga)
/// </summary>
public struct Manga
{
    public string sortName { get; private set; }
    public List<string> authors { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Dictionary<string,string> altTitles { get; private set; }
    // ReSharper disable once MemberCanBePrivate.Global
    public string? description { get; private set; }
    public string[] tags { get; private set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? coverUrl { get; private set; }
    public string? coverFileNameInCache { get; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Dictionary<string,string> links { get; }
    // ReSharper disable once MemberCanBePrivate.Global
    public int? year { get; private set; }
    public string? originalLanguage { get; }
    // ReSharper disable twice MemberCanBePrivate.Global
    public string status { get; private set; }
    public ReleaseStatusByte releaseStatus { get; private set; }
    public enum ReleaseStatusByte : byte
    {
        Continuing = 0,
        Completed = 1,
        OnHiatus = 2,
        Cancelled = 3,
        Unreleased = 4
    };
    public string folderName { get; private set; }
    public string publicationId { get; }
    public string internalId { get; }
    public float ignoreChaptersBelow { get; set; }
    public float latestChapterDownloaded { get; set; }
    public float latestChapterAvailable { get; set; }
    
    public string websiteUrl { get; private set; }

    private static readonly Regex LegalCharacters = new (@"[A-Za-zÀ-ÖØ-öø-ÿ0-9 \.\-,'\'\)\(~!\+]*");

    [JsonConstructor]
    public Manga(string sortName, List<string> authors, string? description, Dictionary<string,string> altTitles, string[] tags, string? coverUrl, string? coverFileNameInCache, Dictionary<string,string>? links, int? year, string? originalLanguage, string publicationId, ReleaseStatusByte releaseStatus, string? websiteUrl, string? folderName = null, float? ignoreChaptersBelow = 0)
    {
        this.sortName = sortName;
        this.authors = authors;
        this.description = description;
        this.altTitles = altTitles;
        this.tags = tags;
        this.coverFileNameInCache = coverFileNameInCache;
        this.coverUrl = coverUrl;
        this.links = links ?? new Dictionary<string, string>();
        this.year = year;
        this.originalLanguage = originalLanguage;
        this.publicationId = publicationId;
        this.folderName = folderName ?? string.Concat(LegalCharacters.Matches(sortName));
        while (this.folderName.EndsWith('.'))
            this.folderName = this.folderName.Substring(0, this.folderName.Length - 1);
        string onlyLowerLetters = string.Concat(this.sortName.ToLower().Where(Char.IsLetter));
        this.internalId = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{onlyLowerLetters}{this.year}"));
        this.ignoreChaptersBelow = ignoreChaptersBelow ?? 0f;
        this.latestChapterDownloaded = 0;
        this.latestChapterAvailable = 0;
        this.releaseStatus = releaseStatus;
        this.websiteUrl = websiteUrl;
    }

    public void UpdateMetadata(Manga newManga)
    {
        this.sortName = newManga.sortName;
        this.description = newManga.description;
        this.coverUrl = newManga.coverUrl;
        this.authors = authors.Union(newManga.authors).ToList();
        this.altTitles = altTitles.UnionBy(newManga.altTitles, kv => kv.Key).ToDictionary(x => x.Key, x => x.Value);
        this.tags = tags.Union(newManga.tags).ToArray();
        this.status = newManga.status;
        this.releaseStatus = newManga.releaseStatus;
        this.websiteUrl = newManga.websiteUrl;
        this.year = newManga.year;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Manga compareManga)
            return false;
        return this.description == compareManga.description &&
               this.year == compareManga.year &&
               this.status == compareManga.status &&
               this.releaseStatus == compareManga.releaseStatus &&
               this.sortName == compareManga.sortName &&
               this.latestChapterAvailable.Equals(compareManga.latestChapterAvailable) &&
               this.authors.All(a => compareManga.authors.Contains(a)) &&
               this.websiteUrl.Equals(compareManga.websiteUrl) &&
               this.tags.All(t => compareManga.tags.Contains(t));
    }

    public override string ToString()
    {
        return $"Publication {sortName} {internalId}";
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

    public void MovePublicationFolder(string downloadDirectory, string newFolderName)
    {
        string oldPath = Path.Join(downloadDirectory, this.folderName);
        this.folderName = newFolderName;
        string newPath = CreatePublicationFolder(downloadDirectory);
        if(Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);
    }

    public void UpdateLatestDownloadedChapter(Chapter chapter)//TODO check files if chapters are all downloaded
    {
        float chapterNumber = Convert.ToSingle(chapter.chapterNumber, GlobalBase.numberFormatDecimalPoint);
        latestChapterDownloaded = latestChapterDownloaded < chapterNumber ? chapterNumber : latestChapterDownloaded;
    }

    public void SaveSeriesInfoJson(string downloadDirectory, bool overwrite = false)
    {
        string publicationFolder = CreatePublicationFolder(downloadDirectory);
        string seriesInfoPath = Path.Join(publicationFolder, "series.json");
        if(overwrite || (!overwrite && !File.Exists(seriesInfoPath)))
            File.WriteAllText(seriesInfoPath,this.GetSeriesInfoJson());
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(seriesInfoPath, GroupRead | GroupWrite | OtherRead | OtherWrite | UserRead | UserWrite);
    }
    
    /// <returns>Serialized JSON String for series.json</returns>
    private string GetSeriesInfoJson()
    {
        SeriesInfo si = new (new Metadata(this));
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

        public Metadata(Manga manga) : this(manga.sortName, manga.year.ToString() ?? string.Empty, manga.releaseStatus, manga.description ?? "")
        {
            
        }
        
        public Metadata(string name, string year, ReleaseStatusByte status, string description_text)
        {
            this.name = name;
            this.year = year;
            this.status = status switch
            {
                ReleaseStatusByte.Continuing => "Continuing",
                ReleaseStatusByte.Completed => "Ended",
                _ => Enum.GetName(status) ?? "Ended"
            };
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