using System.Security.Cryptography;

namespace Marketplace.Orders.Domain;

// ULID: 48 bits de timestamp + 80 bits aleatórios, em Crockford base32 (26 chars).
// Implementação própria para não puxar um pacote por 30 linhas.
public static class Ulid
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string New(string prefix)
    {
        Span<byte> bytes = stackalloc byte[16];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (var i = 5; i >= 0; i--)
        {
            bytes[i] = (byte)(timestamp & 0xFF);
            timestamp >>= 8;
        }

        RandomNumberGenerator.Fill(bytes[6..]);

        return prefix + Encode(bytes);
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        Span<char> chars = stackalloc char[26];
        var index = 0;
        var buffer = 0;
        var bitsInBuffer = 0;

        // 128 bits não fecham em múltiplo de 5; o primeiro caractere usa só 3 bits.
        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bitsInBuffer += 8;
            while (bitsInBuffer >= 5 && index < 25)
            {
                bitsInBuffer -= 5;
                chars[index++] = Alphabet[(buffer >> bitsInBuffer) & 0x1F];
            }
        }

        chars[25] = Alphabet[(buffer << (5 - bitsInBuffer)) & 0x1F];

        return new string(chars);
    }
}
