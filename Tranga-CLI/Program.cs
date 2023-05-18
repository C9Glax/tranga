// See https://aka.ms/new-console-template for more information
using Tranga;
using Tranga.Connectors;

public class Program
{
    public static void Main(string[] args)
    {
        MangaDex mangaDexConnector = new MangaDex("D:");
        Publication[] publications = mangaDexConnector.GetPublications("test");
        Chapter[] chapters = mangaDexConnector.GetChapters(publications[1]);
        mangaDexConnector.DownloadChapter(publications[1], chapters[0]);
    }

}