using System.Text.RegularExpressions;

namespace API.APIEndpointRecords;

public record LunaseaRecord(string id)
{
    private static Regex validateRex = new(@"(?:device|user)\/[0-9a-zA-Z\-]+");
    public bool Validate()
    {
        if (id == string.Empty)
            return false;
        if (!validateRex.IsMatch(id))
            return false;
        return true;
    }
}