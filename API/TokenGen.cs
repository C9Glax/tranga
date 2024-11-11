using System.Security.Cryptography;

namespace API;

public static class TokenGen
{
    private const uint MinimumLength = 8;
    private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string CreateToken(Type t, uint fullLength) => CreateToken(t.Name, fullLength);
    
    public static string CreateToken(string prefix, uint fullLength)
    {
        if (prefix.Length + 1 <= fullLength - MinimumLength)
            throw new ArgumentException("Prefix to long to create Token of meaningful length.");
        long l = fullLength - prefix.Length - 1;
        byte[] rng = new byte[l];
        RandomNumberGenerator.Create().GetBytes(rng);
        string key = new (rng.Select(b => Chars[b % Chars.Length]).ToArray());
        key = string.Join('-', prefix, key);
        return key;
    }
}