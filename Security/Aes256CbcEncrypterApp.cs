using System.Security.Cryptography;
using System.Text;
using System.Web;
using Nancy.Json;
using Newtonsoft.Json;


namespace SankalpAPI.Security
{
    public class Aes256CbcEncrypterApp
    {
        public Aes256CbcEncrypterApp()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private static readonly Encoding encoding = Encoding.UTF8;

        public static string Encrypt(string plainText, string key)
        {
            try
            {
                int p = plainText.Length;
                char[] padding = { '=' };
                RijndaelManaged aes = new RijndaelManaged();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                aes.Key = encoding.GetBytes(key);
                aes.GenerateIV();

                ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] buffer = encoding.GetBytes(plainText);

                string encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length)).TrimEnd(padding).Replace('+', '-').Replace('/', '_');

                String mac = "";

                mac = BitConverter.ToString(HmacSHA256(Convert.ToBase64String(aes.IV) + encryptedText, key)).Replace("-", "").ToLower();

                var keyValues = new Dictionary<string, object>
                {
                    { "iv", Convert.ToBase64String(aes.IV) },
                    { "value", encryptedText },
                    { "mac", mac },
                    { "ts", System.DateTime.UtcNow.ToString() },
                };

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                // serializer.MaxJsonLength = 2147483644; ;
                //var b64= Convert.ToBase64String(encoding.GetBytes(serializer.Serialize(keyValues)));
                //b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
                //return b64;
                return Convert.ToBase64String(encoding.GetBytes(serializer.Serialize(keyValues))).TrimEnd(padding).Replace('+', '-').Replace('/', '_');


            }
            catch (Exception e)
            {
                //throw new Exception("Error encrypting: " + e.Message);
                //This code is newly implemented in aesencrypter file by ashish on 12/11/2024
                return ("Your Token have been expired...! Please login to continue" + e.Message);

            }
        }

        private static byte[] NormalizeKey(string key)
        {
            byte[] rawKey = encoding.GetBytes(key);
            byte[] keyBytes;
            if (rawKey.Length <= 16)
            {
                keyBytes = new byte[16];
                Array.Copy(rawKey, keyBytes, rawKey.Length);
            }
            else if (rawKey.Length <= 24)
            {
                keyBytes = new byte[24];
                Array.Copy(rawKey, keyBytes, rawKey.Length);
            }
            else
            {
                keyBytes = new byte[32];
                Array.Copy(rawKey, keyBytes, Math.Min(rawKey.Length, 32));
            }
            return keyBytes;
        }

        private static bool IsPrintable(string s)
        {
            return !string.IsNullOrWhiteSpace(s) && s.All(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t');
        }

        public static string Decrypt(string plainText, string key)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                return plainText;

            byte[] keyBytes = NormalizeKey(key);

            // 1. Try Laravel-style base64-encoded JSON payload: {"iv":"...","value":"..."}
            try
            {
                byte[] base64Decoded = DecodeUrlBase64(plainText);
                string base64DecodedStr = encoding.GetString(base64Decoded);

                if (base64DecodedStr.TrimStart().StartsWith("{"))
                {
                    var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(base64DecodedStr);
                    if (payload != null && payload.ContainsKey("iv") && payload.ContainsKey("value"))
                    {
                        using var aes = Aes.Create();
                        aes.KeySize = keyBytes.Length * 8;
                        aes.BlockSize = 128;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Mode = CipherMode.CBC;
                        aes.Key = keyBytes;
                        aes.IV = Convert.FromBase64String(payload["iv"]);

                        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                        byte[] buffer = DecodeUrlBase64(payload["value"]);
                        byte[] decrypted = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);
                        string res = encoding.GetString(decrypted).Trim();
                        if (IsPrintable(res))
                            return res;
                    }
                }
            }
            catch
            {
                // Try next format
            }

            // 2. Try raw JSON payload: {"iv":"...","value":"..."} (not base64 encoded)
            try
            {
                if (plainText.TrimStart().StartsWith("{"))
                {
                    var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(plainText);
                    if (payload != null && payload.ContainsKey("iv") && payload.ContainsKey("value"))
                    {
                        using var aes = Aes.Create();
                        aes.KeySize = keyBytes.Length * 8;
                        aes.BlockSize = 128;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Mode = CipherMode.CBC;
                        aes.Key = keyBytes;
                        aes.IV = Convert.FromBase64String(payload["iv"]);

                        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                        byte[] buffer = DecodeUrlBase64(payload["value"]);
                        byte[] decrypted = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);
                        string res = encoding.GetString(decrypted).Trim();
                        if (IsPrintable(res))
                            return res;
                    }
                }
            }
            catch
            {
                // Try next format
            }

            // 3. Try standard AES decryption using Encryptor logic (16 bytes key & IV = key)
            try
            {
                var enc = new Helper.Encryptor();
                string decrypted = enc.Decrypt(plainText, key);
                if (IsPrintable(decrypted))
                {
                    return decrypted;
                }
            }
            catch
            {
                // Try next format
            }

            // 4. Try direct AES-CBC with IV as the first 16 bytes of the decoded buffer
            try
            {
                byte[] buffer = DecodeUrlBase64(plainText);
                if (buffer.Length > 16)
                {
                    byte[] iv = new byte[16];
                    Array.Copy(buffer, 0, iv, 0, 16);
                    byte[] cipherBytes = new byte[buffer.Length - 16];
                    Array.Copy(buffer, 16, cipherBytes, 0, cipherBytes.Length);

                    using var aes = Aes.Create();
                    aes.KeySize = keyBytes.Length * 8;
                    aes.BlockSize = 128;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;
                    aes.Key = keyBytes;
                    aes.IV = iv;

                    using var decryptor = aes.CreateDecryptor();
                    byte[] decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    string res = encoding.GetString(decrypted).Trim();
                    if (IsPrintable(res))
                    {
                        return res;
                    }
                }
            }
            catch
            {
                // Try next format
            }

            // 5. Try direct AES-CBC with IV = first 16 bytes of key
            try
            {
                byte[] buffer = DecodeUrlBase64(plainText);
                byte[] iv = new byte[16];
                Array.Copy(keyBytes, iv, 16);

                using var aes = Aes.Create();
                aes.KeySize = keyBytes.Length * 8;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                aes.Key = keyBytes;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);
                string res = encoding.GetString(decrypted).Trim();
                if (IsPrintable(res))
                {
                    return res;
                }
            }
            catch
            {
                // Try next format
            }

            // 6. If all decryption attempts fail or input was plain text, return plainText
            return plainText;
        }

        public static byte[] HmacSHA256(String data, String key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(key)))
            {
                return hmac.ComputeHash(encoding.GetBytes(data));
            }
        }


        public static string Base64ForUrlEncode(string str)
        {
            byte[] encbuff = Encoding.UTF8.GetBytes(str);
            // Use Convert.ToBase64String and make it URL-safe
            string base64 = Convert.ToBase64String(encbuff);
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        ///<summary>
        /// Decode Base64 encoded string with URL and Filename Safe Alphabet using UTF-8.
        ///</summary>
        ///<param name="str">Base64 code</param>
        ///<returns>The decoded string.</returns>


        public static byte[] DecodeUrlBase64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/').PadRight(4 * ((s.Length + 3) / 4), '=');
            //base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            return Convert.FromBase64String(s);
        }
    }
}
