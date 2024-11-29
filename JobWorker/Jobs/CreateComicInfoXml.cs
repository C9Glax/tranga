using System.Xml.Linq;
using API.Schema;
using API.Schema.Jobs;

namespace JobWorker.Jobs;

public class CreateComicInfoXml : Job<Chapter, string>
{
    protected override (IEnumerable<Job>, string) ExecuteReturnSubTasksInternal(Chapter chapter, Job[] relatedJobs)
    {
        string path = Path.GetTempFileName();
        XElement comicInfo = new("ComicInfo",
            new XElement("Tags", string.Join(',', chapter.ParentManga.Tags.Select(t => t.Tag))),
            new XElement("LanguageISO", chapter.ParentManga.OriginalLanguage),
            new XElement("Title", chapter.Title),
            new XElement("Writer", string.Join(',', chapter.ParentManga.Authors.Select(a => a.AuthorName))),
            new XElement("Volume", chapter.VolumeNumber),
            new XElement("Number", chapter.ChapterNumber)
        );
        File.WriteAllText(path, comicInfo.ToString());
        return ([], path);
    }
}