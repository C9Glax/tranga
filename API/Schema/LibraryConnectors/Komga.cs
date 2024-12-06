namespace API.Schema.LibraryConnectors;

public class Komga(string baseUrl, string auth)
    : LibraryConnector(TokenGen.CreateToken(typeof(Komga), 64), LibraryType.Komga, baseUrl, auth)
{
    
}