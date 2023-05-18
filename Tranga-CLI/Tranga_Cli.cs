using Tranga;
using Tranga.Connectors;

namespace Tranga_CLI;

public static class Tranga_Cli
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Output folder path (D:):");
        string? folderPath = Console.ReadLine();
        while(folderPath is null )
            folderPath = Console.ReadLine();
        if (folderPath.Length < 1)
            folderPath = "D:";
        
        Console.WriteLine("Select Connector:");
        Console.WriteLine("0: MangaDex");

        string? selectedConnector = Console.ReadLine();
        while(selectedConnector is null || selectedConnector.Length < 1)
            selectedConnector = Console.ReadLine();
        int selectedConnectorIndex = Convert.ToInt32(selectedConnector);

        Connector connector;
        switch (selectedConnectorIndex)
        {
            case 0:
                connector = new MangaDex(folderPath);
                break;
            default:
                connector = new MangaDex(folderPath);
                break;
        }

        Console.WriteLine("Search query (leave empty for all):");
        string? query = Console.ReadLine();
        Publication[] publications = connector.GetPublications(query ?? "");
        
        int pIndex = 0;
        foreach(Publication publication in publications)
            Console.WriteLine($"{pIndex++}: {publication.sortName}");
        Console.WriteLine($"Select publication to Download (0-{publications.Length - 1}):");
        
        string? selected = Console.ReadLine();
        while(selected is null || selected.Length < 1)
            selected = Console.ReadLine();
        Publication selectedPub = publications[Convert.ToInt32(selected)];
        
        Chapter[] chapters = connector.GetChapters(selectedPub, "en");

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
            connector.DownloadChapter(selectedPub, chapters[i]);
        }
    }

}