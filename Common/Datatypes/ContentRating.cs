namespace Common.Datatypes;

public enum ContentRating
{
    Safe,
    Suggestive,
    Erotica,
    Pornographic
}

public static class ContentRatingExtensions
{
    public static ContentRating TryParseContentRating(this string str)
    {
        if (Enum.TryParse(str, true, out ContentRating result))
            return result;
        throw new NotImplementedException();
        // TODO more parsing methods
    }

    public static bool IsNsfw(this ContentRating rating) => rating switch
    {
        ContentRating.Safe => false,
        ContentRating.Suggestive => false,
        ContentRating.Erotica => true,
        ContentRating.Pornographic => true,
        _ => throw new NotImplementedException()
    };
}