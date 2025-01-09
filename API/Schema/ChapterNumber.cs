using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace API.Schema;

public readonly struct ChapterNumber : INumber<ChapterNumber>
{
    private readonly uint[] _numbers;
    private readonly bool _naN;

    private ChapterNumber(uint[] numbers, bool naN = false)
    {
        this._numbers = numbers;
        this._naN = naN;
    }

    public ChapterNumber(string number)
    {
        if (!CanParse(number))
        {
            this._numbers = [];
            this._naN = true;
        }
        this._numbers = number.Split('.').Select(uint.Parse).ToArray();
    }

    public ChapterNumber(float number) : this(number.ToString("F")) {}

    public ChapterNumber(double number) : this((float)number) {}
    
    public ChapterNumber(uint number)
    { 
        this._numbers = [number];
        this._naN = false;
    }

    public ChapterNumber(int number)
    {
        if (int.IsNegative(number))
        {
            this._numbers = [];
            this._naN = true;
        }
        this._numbers = [(uint)number];
        this._naN = false;
    }
    
    public int CompareTo(ChapterNumber other)
    {
        byte index = 0;
        do
        {
            if (this._numbers[index] < other._numbers[index])
                return -1;
            else if (this._numbers[index] > other._numbers[index])
                return 1;
        }while(index < this._numbers.Length && index < other._numbers.Length);

        if (index >= this._numbers.Length && index >= other._numbers.Length)
            return 0;
        else if (index >= this._numbers.Length)
            return -1;
        else if (index >= other._numbers.Length)
            return 1;
        throw new UnreachableException();
    }

    private static readonly Regex Pattern = new(@"[0-9]+(?:\.[0-9]+)*");
    public static bool CanParse(string? number) => number is not null && Pattern.Match(number).Length == number.Length && number.Length > 0;

    public bool Equals(ChapterNumber other) => CompareTo(other) == 0;

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Join('.', _numbers);
    }
    
    public override bool Equals(object? obj)
    {
        return obj is ChapterNumber other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_numbers, _naN);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public int CompareTo(object? obj)
    {
        if(obj is ChapterNumber other)
            return CompareTo(other);
        throw new ArgumentException();
    }

    public static ChapterNumber Parse(string s, IFormatProvider? provider)
    {
        if(!CanParse(s))
            throw new FormatException($"Invalid ChapterNumber-String: {s}");
        return new ChapterNumber(s);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ChapterNumber result)
    {
        result = new ChapterNumber([], true);;
        if (!CanParse(s))
            return false;
        if (s is null)
            return false;
        result = new ChapterNumber(s);
        return true;
    }

    public static ChapterNumber Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s.ToString(), provider);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ChapterNumber result) => TryParse(s.ToString(), provider, out result);

    public static ChapterNumber operator +(ChapterNumber left, ChapterNumber right)
    {
        if (IsNaN(left) || IsNaN(right))
            return new ChapterNumber([], true);
        int size = left._numbers.Length > right._numbers.Length ? left._numbers.Length : right._numbers.Length;
        uint[] numbers = new uint[size];
        for (int i = 0; i < size; i++)
        {
            if(left._numbers.Length < i)
                numbers[i] = right._numbers[i];
            else if(right._numbers.Length < i)
                numbers[i] = left._numbers[i];
            else
                numbers[i] = left._numbers[i] + right._numbers[i];
        }
        return new ChapterNumber(numbers);
    }
    
    private static bool BothNotNaN(ChapterNumber left, ChapterNumber right) => !IsNaN(left) && !IsNaN(right);

    public static ChapterNumber AdditiveIdentity => Zero;
    
    public static bool operator ==(ChapterNumber left, ChapterNumber right) => BothNotNaN(left, right) && left.Equals(right);

    public static bool operator !=(ChapterNumber left, ChapterNumber right) => !(left == right);

    public static bool operator >(ChapterNumber left, ChapterNumber right) => BothNotNaN(left, right) && left.CompareTo(right) > 0;

    public static bool operator >=(ChapterNumber left, ChapterNumber right) => BothNotNaN(left, right) && left.CompareTo(right) >= 0;

    public static bool operator <(ChapterNumber left, ChapterNumber right) => BothNotNaN(left, right) && left.CompareTo(right) < 0;

    public static bool operator <=(ChapterNumber left, ChapterNumber right) => BothNotNaN(left, right) && left.CompareTo(right) <= 0;

    public static ChapterNumber operator %(ChapterNumber left, ChapterNumber right) => throw new ArithmeticException();

    public static ChapterNumber operator +(ChapterNumber value) => throw new InvalidOperationException();

    public static ChapterNumber operator --(ChapterNumber value)
    {
        if (IsNaN(value))
            return value;
        uint[] numbers = value._numbers;
        numbers[0]--;
        return new ChapterNumber(numbers);
    }

    public static ChapterNumber operator /(ChapterNumber left, ChapterNumber right) => throw new InvalidOperationException();

    public static ChapterNumber operator ++(ChapterNumber value)
    {
        if (IsNaN(value))
            return value;
        uint[] numbers = value._numbers;
        numbers[0]++;
        return new ChapterNumber(numbers);
    }

    public static ChapterNumber MultiplicativeIdentity => One;
    public static ChapterNumber operator *(ChapterNumber left, ChapterNumber right) => throw new InvalidOperationException();

    public static ChapterNumber operator -(ChapterNumber left, ChapterNumber right) => throw new InvalidOperationException();

    public static ChapterNumber operator -(ChapterNumber value) => throw new InvalidOperationException();

    public static ChapterNumber Abs(ChapterNumber value) => value;

    public static bool IsCanonical(ChapterNumber value) => true;

    public static bool IsComplexNumber(ChapterNumber value) => false;

    public static bool IsEvenInteger(ChapterNumber value) => IsInteger(value) && uint.IsEvenInteger(value._numbers[0]);

    public static bool IsFinite(ChapterNumber value) => true;

    public static bool IsImaginaryNumber(ChapterNumber value) => false;

    public static bool IsInfinity(ChapterNumber value) => false;

    public static bool IsInteger(ChapterNumber value) => !IsNaN(value) && value._numbers.Length == 1;

    public static bool IsNaN(ChapterNumber value) => value._naN;

    public static bool IsNegative(ChapterNumber value) => false;

    public static bool IsNegativeInfinity(ChapterNumber value) => false;

    public static bool IsNormal(ChapterNumber value) => true;
    
    public static bool IsOddInteger(ChapterNumber value) => false;

    public static bool IsPositive(ChapterNumber value) => true;

    public static bool IsPositiveInfinity(ChapterNumber value) => false;

    public static bool IsRealNumber(ChapterNumber value) => false;

    public static bool IsSubnormal(ChapterNumber value) => false;

    public static bool IsZero(ChapterNumber value) => value._numbers.All(n => n == 0);

    public static ChapterNumber MaxMagnitude(ChapterNumber x, ChapterNumber y)
    {
        if(IsNaN(x))
            return new ChapterNumber([], true);
        if (IsNaN(y))
            return new ChapterNumber([], true);
        return x >= y ? x : y;
    }

    public static ChapterNumber MaxMagnitudeNumber(ChapterNumber x, ChapterNumber y)
    {
        if (IsNaN(x))
            return y;
        if (IsNaN(y))
            return x;
        return x >= y ? x : y;
    }

    public static ChapterNumber MinMagnitude(ChapterNumber x, ChapterNumber y)
    {
        if(IsNaN(x))
            return new ChapterNumber([], true);
        if (IsNaN(y))
            return new ChapterNumber([], true);
        return x <= y ? x : y;
    }

    public static ChapterNumber MinMagnitudeNumber(ChapterNumber x, ChapterNumber y)
    {
        if (IsNaN(x))
            return y;
        if (IsNaN(y))
            return x;
        return x <= y ? x : y;
    }

    public static ChapterNumber Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();

    public static ChapterNumber Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();

    public static bool TryConvertFromChecked<TOther>(TOther value, out ChapterNumber result) where TOther : INumberBase<TOther>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertFromSaturating<TOther>(TOther value, out ChapterNumber result) where TOther : INumberBase<TOther>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertFromTruncating<TOther>(TOther value, out ChapterNumber result) where TOther : INumberBase<TOther>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertToChecked<TOther>(ChapterNumber value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertToSaturating<TOther>(ChapterNumber value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        throw new NotImplementedException();
    }

    public static bool TryConvertToTruncating<TOther>(ChapterNumber value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        throw new NotImplementedException();
    }

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ChapterNumber result)
        => TryParse(s.ToString(), style, provider, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ChapterNumber result)
        => TryParse(s, provider, out result);

    public static ChapterNumber One => new(1);
    public static int Radix => 10;
    public static ChapterNumber Zero => new(0);
}