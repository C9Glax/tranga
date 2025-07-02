namespace API.APIEndpointRecords;

public record NtfyRecord(string Name, string Endpoint, string Username, string Password, string Topic, int Priority)
{
    public bool Validate()
    {
        if (Endpoint == string.Empty)
            return false;
        if (Username == string.Empty)
            return false;
        if (Password == string.Empty)
            return false;
        if (Priority < 1 || Priority > 5)
            return false;
        return true;
    }
}