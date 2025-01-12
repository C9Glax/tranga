using System.Security.Cryptography;
using System.Text;

namespace API;

public static class TokenGen
{
    private const uint MinimumLength = 8;
    private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string CreateToken(Type t, uint fullLength) => CreateToken(t.Name, fullLength);
    
    public static string CreateToken(string prefix, uint fullLength)
    {
        if (prefix.Length + 1 >= fullLength - MinimumLength)
            throw new ArgumentException("Prefix to long to create Token of meaningful length.");
        long l = fullLength - prefix.Length - 1;
        byte[] rng = new byte[l];
        RandomNumberGenerator.Create().GetBytes(rng);
        string key = new (rng.Select(b => Chars[b % Chars.Length]).ToArray());
        key = string.Join('-', prefix, key);
        return key;
    }

    public static string CreateTokenHash(string prefix, uint fullLength, string[] keys)
    {
        if (prefix.Length + 1 >= fullLength - MinimumLength)
            throw new ArgumentException("Prefix to long to create Token of meaningful length.");
        int l = (int)(fullLength - prefix.Length - 1);
        MD5 md5 = MD5.Create();
        byte[][] hashes = keys.Select(key => md5.ComputeHash(Encoding.UTF8.GetBytes(key))).ToArray();
        byte[] xOrHash = new byte[l];
        foreach (byte[] hash in hashes)
            for(int i = 0; i < hash.Length; i++)
                xOrHash[i] = (byte)(xOrHash[i] ^ (i >= hash.Length ? 0 : hash[i]));
        string key = new (xOrHash.Select(b => Chars[b % Chars.Length]).ToArray());
        key = string.Join('-', prefix, key);
        return key;
    }
}