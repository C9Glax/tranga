namespace API.APIEndpointRecords;

public record GotifyRecord(string Name, string Endpoint, string AppToken, int Priority)
{
    public bool Validate()
    {
        if (Endpoint == string.Empty)
            return false;
        if (AppToken == string.Empty)
            return false;
        if (Priority < 0 || Priority > 10)
            return false;

        return true;
    }
}