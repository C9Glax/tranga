namespace API.APIEndpointRecords;

public record PushoverRecord(string apptoken, string user)
{
    public bool Validate()
    {
        if (apptoken == string.Empty)
            return false;
        if (user == string.Empty)
            return false;
        return true;
    }
}