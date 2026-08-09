using System.Text.RegularExpressions;

namespace Common.Helpers;

public static partial class StringExtensions
{
    /// <summary>
    /// Removes "illegal" characters
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string SafeFilesystemString(this string str) => SafeCharacters().Replace(str, string.Empty);
    [GeneratedRegex(@"[^0-9a-zA-Z-._\ ]")]
    private static partial Regex SafeCharacters();
}
