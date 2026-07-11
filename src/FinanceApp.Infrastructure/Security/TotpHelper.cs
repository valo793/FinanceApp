using System;
using System.Security.Cryptography;
using System.Text;

namespace FinanceApp.Infrastructure.Security;

public static class TotpHelper
{
    public static string GenerateSecretKey()
    {
        // 160 bits (20 bytes) is the standard size for Google Authenticator TOTP secrets
        var bytes = new byte[20];
        RandomNumberGenerator.Fill(bytes);
        return ToBase32(bytes);
    }

    public static string GenerateCode(string secret, DateTimeOffset time)
    {
        var key = FromBase32(secret);
        var counter = time.ToUnixTimeSeconds() / 30;
        return GenerateCodeInternal(key, counter);
    }

    public static bool VerifyCode(string secret, string code, DateTimeOffset time, int driftWindow = 1)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim().Replace(" ", "").Replace("-", "");
        if (code.Length != 6) return false;

        var key = FromBase32(secret);
        var currentCounter = time.ToUnixTimeSeconds() / 30;

        // Check drift window (-driftWindow to +driftWindow) to handle client-server clock desync
        for (int i = -driftWindow; i <= driftWindow; i++)
        {
            var calculatedCode = GenerateCodeInternal(key, currentCounter + i);
            if (calculatedCode == code) return true;
        }

        return false;
    }

    public static string GetQrCodeUri(string issuer, string accountName, string secret)
    {
        // Uri encode issuer and account name
        string escapedIssuer = Uri.EscapeDataString(issuer);
        string escapedAccount = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{escapedIssuer}:{escapedAccount}?secret={secret.Trim().ToUpperInvariant()}&issuer={escapedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    private static string GenerateCodeInternal(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24) |
                     ((hash[offset + 1] & 0xFF) << 16) |
                     ((hash[offset + 2] & 0xFF) << 8) |
                     (hash[offset + 3] & 0xFF);

        int otp = binary % 1000000;
        return otp.ToString("D6");
    }

    private static byte[] FromBase32(string input)
    {
        if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();
        input = input.Trim().ToUpperInvariant().Replace("=", "");
        int byteCount = input.Length * 5 / 8;
        byte[] returnArray = new byte[byteCount];
        byte curByte = 0, bitsRemaining = 8;
        int mask = 0, arrayIndex = 0;

        foreach (char c in input)
        {
            int value = c switch
            {
                >= 'A' and <= 'Z' => c - 'A',
                >= '2' and <= '7' => c - '2' + 26,
                _ => throw new ArgumentException("Caracteres base32 inválidos.")
            };

            if (bitsRemaining > 5)
            {
                mask = value << (bitsRemaining - 5);
                curByte = (byte)(curByte | mask);
                bitsRemaining -= 5;
            }
            else
            {
                mask = value >> (5 - bitsRemaining);
                curByte = (byte)(curByte | mask);
                returnArray[arrayIndex++] = curByte;
                curByte = (byte)(value << (3 + bitsRemaining));
                bitsRemaining += 3;
            }
        }
        return returnArray;
    }

    private static string ToBase32(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new StringBuilder();
        int byteCount = data.Length;
        for (int i = 0; i < byteCount; i += 5)
        {
            int limit = Math.Min(5, byteCount - i);
            ulong buffer = 0;
            for (int j = 0; j < limit; j++)
            {
                buffer |= ((ulong)data[i + j]) << (8 * (4 - j));
            }
            int steps = (limit * 8 + 4) / 5;
            for (int j = 0; j < steps; j++)
            {
                int index = (int)((buffer >> (5 * (7 - j))) & 0x1F);
                sb.Append(alphabet[index]);
            }
        }
        return sb.ToString();
    }
}
