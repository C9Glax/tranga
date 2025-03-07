namespace API.APIEndpointRecords;

public record NtfyRecord(string endpoint, string username, string password, string topic, int priority)
{
    public bool Validate()
    {
        if (endpoint == string.Empty)
            return false;
        if (username == string.Empty)
            return false;
        if (password == string.Empty)
            return false;
        if (priority < 1 || priority > 5)
            return false;
        return true;
    }
}