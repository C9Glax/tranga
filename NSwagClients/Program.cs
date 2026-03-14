using NSwagClients;

CancellationTokenSource ctSource = new ();

await Generator.GenerateFromUrl("https://api.mangaupdates.com/openapi.yaml", "MangaUpdates", ctSource.Token, Generator.Type.Yaml);
await Generator.GenerateFromUrl("https://api.mangadex.org/docs/static/api.yaml", "MangaDex", ctSource.Token, Generator.Type.Yaml);