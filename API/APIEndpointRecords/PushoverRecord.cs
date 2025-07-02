namespace API.APIEndpointRecords;

public record PushoverRecord(string Name, string AppToken, string User)
{
    public bool Validate()
    {
        if (AppToken == string.Empty)
            return false;
        if (User == string.Empty)
            return false;
        return true;
    }
}