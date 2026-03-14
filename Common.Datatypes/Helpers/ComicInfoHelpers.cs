using Data;

namespace Common.Datatypes.Helpers;

public static class ComicInfoHelpers
{
    public static ComicInfo Merge(this ComicInfo comicInfo, ComicInfo other)
    {
        return comicInfo with
        {
            Title = comicInfo.Title
            // TODO
        };
    }
}