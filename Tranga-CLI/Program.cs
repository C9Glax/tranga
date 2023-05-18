// See https://aka.ms/new-console-template for more information
using Tranga;
using Tranga.Connectors;

public class Program
{
    public static void Main(string[] args)
    {
        MangaDex mangaDexConnector = new MangaDex();
        Publication[] publications = mangaDexConnector.GetPublications();
        Console.ReadKey();
    }

}