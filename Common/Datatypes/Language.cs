using System.Globalization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Common.Datatypes;

public sealed class Language(string name) : CultureInfo(name), IEquatable<Language>, IEquatable<string>
{
    public static implicit operator string?(Language? l) => l?.Name;
    public static implicit operator Language?(string? s) => s is null ? null : new Language(s);

    public override string ToString() => Name;

    /// <summary>
    /// Compares this language against another locale code, treating a bare ISO 639 language code
    /// (e.g. "en") as equal to any of its sub-locales (e.g. "en-us", "en-gb"), while two different
    /// sub-locales of the same language (e.g. "en-us" vs "en-gb") are not considered equal.
    /// Returns false if <paramref name="other"/> is null, empty, or not a parseable culture name.
    /// </summary>
    public bool Equals(string? other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        Language otherLanguage;
        try
        {
            otherLanguage = new Language(other);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }

        return Equals(otherLanguage);
    }

    public bool Equals(Language? other)
    {
        if (other is null)
            return false;
        if (Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase))
            return true;
        if (!TwoLetterISOLanguageName.Equals(other.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
            return false;

        bool thisIsLanguageOnly = Name.Equals(TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase);
        bool otherIsLanguageOnly = other.Name.Equals(other.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase);
        return thisIsLanguageOnly || otherIsLanguageOnly;
    }
}