using SixLabors.ImageSharp;

namespace API;

public struct Constants
{
    public const string TRANGA = 
        "\n\n" +
        " _______                                 v2\n" +
        "|_     _|.----..---.-..-----..-----..---.-.\n" +
        "  |   |  |   _||  _  ||     ||  _  ||  _  |\n" +
        "  |___|  |__|  |___._||__|__||___  ||___._|\n" +
        "                             |_____|       \n\n";

    public static readonly Size ImageSmSize = new (225, 320);
    public static readonly Size ImageMdSize = new (450, 640);
    public static readonly Size ImageLgSize = new (900, 1280);
}