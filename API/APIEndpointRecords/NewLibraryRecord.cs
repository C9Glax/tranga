namespace API.APIEndpointRecords;

public record NewLibraryRecord(string path, string name)
{
    public bool Validate()
    {
        if (path.Length < 1) //TODO Better Path validation
            return false;
        if (name.Length < 1)
            return false;
        return true;
    }
}