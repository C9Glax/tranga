using System.Text.RegularExpressions;
using Common.Settings;
using Services.Manga.Database;

namespace Services.Tasks.Helpers;

public static partial class ChapterFileHelper
{
    /// <summary>
    /// Creates the chapter filename for a given chapter.
    /// </summary>
    /// <returns>Chapter archive filename ("xxx.cbz")</returns>
    /**
     * Regex formatting
     *
     * Parameters:
     * - %V Volume-number
     * - %C Chapter-number
     * - %T Title
     *
     * Optional: Only include parameters if not null
     * ?V(xxx %V) will only be present if %V has a value.
     */
    public static string CreateFileName(this DbChapter chapter)
    {
        string scheme = Settings.ChapterNamingScheme;
        
        string optionalFormattersApplied = ApplyOptionalFormatters(scheme, chapter, ["V", "C", "T"]);
        string chapterName = ReplaceParameters(optionalFormattersApplied, chapter, ["V", "C", "T"]);

        return $"{chapterName}.cbz".SafeString();
    }

    private static string ApplyOptionalFormatters(string scheme, DbChapter chapter, params string[] parameters)
    {
        foreach (string parameter in parameters)
            scheme = OptionalFormatting(parameter).Replace(scheme,
                match => ParameterValue(parameter, chapter) is null ? string.Empty : match.Groups[1].Value);
        return scheme;
    }

    private static string ReplaceParameters(string scheme, DbChapter chapter, params string[] parameters)
    {
        foreach (string parameter in parameters)
            scheme = new Regex($"%{parameter}").Replace(scheme, ParameterValue(parameter, chapter) ?? string.Empty);
        return scheme;
    }

    private static Regex OptionalFormatting(string parameter) => new($@"\?{parameter}\((.*)\)");

    private static string? ParameterValue(string parameter, DbChapter chapter) => parameter switch
    {
        "V" => chapter.Volume,
        "C" => chapter.Number,
        "T" => chapter.Title,
        _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter, null)
    };

    private static string SafeString(this string str) => SafeCharacters().Replace(str, string.Empty);
    [GeneratedRegex(@"[^0-9a-zA-Z-._\ ]")]
    private static partial Regex SafeCharacters();
}