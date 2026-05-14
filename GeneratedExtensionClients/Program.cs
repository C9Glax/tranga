using GeneratedExtensionClients;

CancellationTokenSource ctSource = new ();

await NswagGenerator.GenerateFromFile("Definitions/MangaUpdates.yaml", "MangaUpdates", ctSource.Token, NswagGenerator.Type.Yaml);
await NswagGenerator.GenerateFromUrl("https://api.mangadex.org/docs/static/api.yaml", "MangaDex", ctSource.Token, NswagGenerator.Type.Yaml);