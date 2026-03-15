using System.Globalization;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Common.Datatypes;

public sealed class Language(string name) : CultureInfo(name)
{
    public static implicit operator string?(Language? l) => l?.Name;
    public static implicit operator Language?(string? s) => s is null ? null : new Language(s);

    public override string ToString() => Name;
}