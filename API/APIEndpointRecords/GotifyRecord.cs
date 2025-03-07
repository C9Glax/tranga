namespace API.APIEndpointRecords;

public record GotifyRecord(string endpoint, string appToken, int priority)
{
    public bool Validate()
    {
        if (endpoint == string.Empty)
            return false;
        if (appToken == string.Empty)
            return false;
        if (priority < 0 || priority > 10)
            return false;

        return true;
    }
}