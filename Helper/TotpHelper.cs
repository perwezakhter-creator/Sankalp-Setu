using System;
using System.Security.Cryptography;
using System.Text;

namespace SankalpAPI.Helper
{
    public static class TotpHelper
    {
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>
        /// Generates a cryptographically secure Base32 secret string.
        /// Standard TOTP secret is typically 20 bytes (160 bits), giving 32 Base32 characters.
        /// </summary>
        public static string GenerateBase32Secret(int byteLength = 20)
        {
            byte[] bytes = new byte[byteLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return ToBase32String(bytes);
        }

        /// <summary>
        /// Generates a 6-digit TOTP code for the given Base32 secret at the specified UTC time (defaults to now).
        /// </summary>
        public static string GenerateTotp(string base32Secret, DateTime? utcTime = null, int digits = 6, int period = 30)
        {
            if (string.IsNullOrWhiteSpace(base32Secret))
                throw new ArgumentException("Secret cannot be empty.", nameof(base32Secret));

            byte[] key = FromBase32String(base32Secret);
            DateTime time = utcTime ?? DateTime.UtcNow;
            long unixTime = new DateTimeOffset(time).ToUnixTimeSeconds();
            long counter = unixTime / period;

            return ComputeHmacTotp(key, counter, digits);
        }

        /// <summary>
        /// Validates a TOTP code against the Base32 secret allowing clock drift within +/- window steps (default window = 1).
        /// </summary>
        public static bool ValidateTotp(string base32Secret, string code, int window = 1, int digits = 6, int period = 30)
        {
            if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
                return false;

            code = code.Trim();
            if (code.Length != digits)
                return false;

            byte[] key;
            try
            {
                key = FromBase32String(base32Secret);
            }
            catch
            {
                return false;
            }

            long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long currentCounter = currentUnixTime / period;

            for (int i = -window; i <= window; i++)
            {
                long checkCounter = currentCounter + i;
                string generated = ComputeHmacTotp(key, checkCounter, digits);
                if (CryptographicEquals(generated, code))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates standard otpauth:// URL for QR code generation or mobile authenticator apps.
        /// </summary>
        public static string GenerateOtpauthUri(string issuer, string accountName, string base32Secret, int digits = 6, int period = 30)
        {
            string cleanIssuer = issuer?.Trim() ?? "Sankalp";
            string cleanAccount = accountName?.Trim() ?? "User";
            string label = $"{Uri.EscapeDataString(cleanIssuer)}:{Uri.EscapeDataString(cleanAccount)}";
            return $"otpauth://totp/{label}?secret={base32Secret}&issuer={Uri.EscapeDataString(cleanIssuer)}&algorithm=SHA1&digits={digits}&period={period}";
        }

        private static string ComputeHmacTotp(byte[] key, long counter, int digits)
        {
            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using var hmac = new HMACSHA1(key);
            byte[] hash = hmac.ComputeHash(counterBytes);

            int offset = hash[hash.Length - 1] & 0x0F;
            int binaryCode = ((hash[offset] & 0x7F) << 24)
                           | ((hash[offset + 1] & 0xFF) << 16)
                           | ((hash[offset + 2] & 0xFF) << 8)
                           | (hash[offset + 3] & 0xFF);

            int modulus = (int)Math.Pow(10, digits);
            int otp = binaryCode % modulus;

            return otp.ToString(new string('0', digits));
        }

        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        public static string ToBase32String(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            var result = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;

            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[next++] & 0xFF;
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = 0x1F & (buffer >> (bitsLeft - 5));
                bitsLeft -= 5;
                result.Append(Base32Chars[index]);
            }

            return result.ToString();
        }

        public static byte[] FromBase32String(string base32)
        {
            if (string.IsNullOrWhiteSpace(base32))
                return Array.Empty<byte>();

            string cleaned = base32.Trim().TrimEnd('=').ToUpperInvariant();
            var output = new byte[cleaned.Length * 5 / 8];

            int buffer = 0;
            int bitsLeft = 0;
            int outputIndex = 0;

            foreach (char c in cleaned)
            {
                int val = Base32Chars.IndexOf(c);
                if (val < 0)
                    throw new ArgumentException($"Invalid Base32 character '{c}'.");

                buffer <<= 5;
                buffer |= val;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    output[outputIndex++] = (byte)((buffer >> (bitsLeft - 8)) & 0xFF);
                    bitsLeft -= 8;
                }
            }

            return output;
        }
    }
}
