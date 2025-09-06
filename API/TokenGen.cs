using System.Security.Cryptography;
using System.Text;

namespace API;

public static class TokenGen
{
    public const int MinimumLength = 16;
    public const int MaximumLength = 64;
    private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string CreateToken(Type t, params string[] identifiers) => CreateToken(t.Name, identifiers);
    
    public static string CreateToken(string prefix, params string[] identifiers)
    {
        if (prefix.Length + 1 >= MaximumLength - MinimumLength)
            throw new ArgumentException("Prefix to long to create Token of meaningful length.");
        
        int tokenLength = MaximumLength - prefix.Length - 1;
        
        if (identifiers.Length == 0)
        {
            // No identifier, just create a random token
            byte[] rng = new byte[tokenLength];
            RandomNumberGenerator.Create().GetBytes(rng);
            string key = new(rng.Select(b => Chars[b % Chars.Length]).ToArray());
            key = string.Join('-', prefix, key);
            return key;
        }

        // Identifier provided, create a token based on the identifier hashed
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(string.Join("", identifiers)));
        string token = Convert.ToHexStringLower(hash);
        
        return string.Join('-', prefix, token);
    }
}