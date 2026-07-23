using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Shortly.Application.Services;

public static class ShortCodeGenerator
{
    private const string Base62Alphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private const int ShortCodeLength = 12;

    public static string Generate()
    {
        var ulidBytes = Ulid.NewUlid().ToByteArray();

        var hash = SHA256.HashData(ulidBytes);

        return EncodeBase62(hash)[..ShortCodeLength];
    }

    private static string EncodeBase62(byte[] data)
    {
        var value = new BigInteger(data, isUnsigned: true, isBigEndian: true);
        if (value == 0) return new string('0', ShortCodeLength);

        var sb = new StringBuilder();
        while (value > 0)
        {
            value = BigInteger.DivRem(value, 62, out var remainder);
            sb.Insert(0, Base62Alphabet[(int)remainder]);
        }

        while (sb.Length < ShortCodeLength)
            sb.Insert(0, '0');

        return sb.ToString();
    }
}