// See https://aka.ms/new-console-template for more information
using Tranga;
using Tranga.Connectors;

public class Program
{
    public static void Main(string[] args)
    {
        MangaDex mangaDexConnector = new MangaDex("D:");
        Console.WriteLine("Search query (leave empty for all):");
        string? query = Console.ReadLine();
        Publication[] publications = mangaDexConnector.GetPublications(query ?? "");
        
        int pIndex = 0;
        foreach(Publication publication in publications)
            Console.WriteLine($"{pIndex++}: {publication.sortName}");
        Console.WriteLine($"Select publication to Download (0-{publications.Length - 1}):");
        
        string? selected = Console.ReadLine();
        while(selected is null || selected.Length < 1)
            selected = Console.ReadLine();
        Publication selectedPub = publications[Convert.ToInt32(selected)];
        
        Chapter[] chapters = mangaDexConnector.GetChapters(selectedPub);

        int cIndex = 0;
        foreach (Chapter ch in chapters)
        {
            string name = cIndex.ToString();
            if (ch.name is not null && ch.name.Length > 0)
                name = ch.name;
            else if (ch.chapterNumber is not null && ch.chapterNumber.Length > 0)
                name = ch.chapterNumber;
            Console.WriteLine($"{cIndex++}: {name}");
        }
        Console.WriteLine($"Select Chapters to download (0-{chapters.Length - 1}) [range x-y or 'a' for all]: ");
        selected = Console.ReadLine();
        while(selected is null || selected.Length < 1)
            selected = Console.ReadLine();

        int start = 0;
        int end = 0;
        if (selected == "a")
            end = chapters.Length - 1;
        else if (selected.Contains('-'))
        {
            string[] split = selected.Split('-');
            start = Convert.ToInt32(split[0]);
            end = Convert.ToInt32(split[1]);
        }
        else
        {
            start = Convert.ToInt32(selected);
            end = Convert.ToInt32(selected);
        }

        for (int i = start; i < end + 1; i++)
        {
            Console.WriteLine($"Downloading {selectedPub.sortName} Chapter {i}");
            mangaDexConnector.DownloadChapter(selectedPub, chapters[i]);
        }
    }

}