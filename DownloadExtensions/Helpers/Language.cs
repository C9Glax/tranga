using System.Globalization;

namespace DownloadExtensions.Helpers;

public sealed class Language(string name) : CultureInfo(name)
{
    public static implicit operator string(Language l) => l.Name;
    public static implicit operator Language(string s) => new(s);

    public override string ToString() => Name;
}