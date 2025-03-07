using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace API;

public record CoverFormatRequestRecord(Size size)
{
    public bool Validate()
    {
        if (size.Height <= 0 || size.Width <= 0 || size.Height > 65535 || size.Width > 65535) //JPEG max size
            return false;
        return true;
    }
}