using API.Schema;
using API.Schema.Jobs;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class UpdateMetadata(UpdateMetadataJob data) : Job<UpdateMetadataJob>(data)
{
    
    private const string UpdateMetadataEndpoint = "v2/Manga/{0}";
    
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(UpdateMetadataJob data)
    {
        Manga manga = data.Manga;
        MangaConnector mangaConnector = GetConnector(manga);
        
        //Retrieve new Metadata
        Manga? retManga = mangaConnector.GetMangaFromManga(manga)?.Item1;
        if (retManga is null)
            return [];

        Monitor.MakePatchRequestApi(string.Format(UpdateMetadataEndpoint, manga.MangaId), manga, out object? _);

        return [];
    }
}