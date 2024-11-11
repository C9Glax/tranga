namespace API.Schema.LibraryConnectors;

public class Kavita(string baseUrl, string auth)
    : LibraryConnector(TokenGen.CreateToken(typeof(Kavita), 64), Tranga.LibraryConnectors.LibraryConnector.LibraryType.Kavita, baseUrl, auth)
{
    
}