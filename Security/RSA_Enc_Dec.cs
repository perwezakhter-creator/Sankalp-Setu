using System.Security.Cryptography;
using System.Text;

namespace SankalpAPI.Security
{
    public static class RSA_Enc_Dec
    {
        private static readonly string PublicKey = "<RSAKeyValue><Modulus>eS70WCy6BUEKlzE1OvLmKqf1tyijhjrGXoz4yAZBUaQXTd3mZqDVTeDcG8kvSDloJ5+KuxTN1edOZIhyK7G8Lo6Yi92c4aENNEqCrkI6btooGFfkLyk9eNao5R87pg8v5ft/K9tj4Yn4lvSPZY2PSjORk3VF8HOPzFAIRPEk1TE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        private static readonly string PrivateKey = "<RSAKeyValue><Modulus>eS70WCy6BUEKlzE1OvLmKqf1tyijhjrGXoz4yAZBUaQXTd3mZqDVTeDcG8kvSDloJ5+KuxTN1edOZIhyK7G8Lo6Yi92c4aENNEqCrkI6btooGFfkLyk9eNao5R87pg8v5ft/K9tj4Yn4lvSPZY2PSjORk3VF8HOPzFAIRPEk1TE=</Modulus><Exponent>AQAB</Exponent><P>7S/sHNRgNdbXIU36OxMPfF3DA6+jg3wLEBwrkhddz0zUBinEFwWqWYiu19htoOAMa4v2nZOoI387AKgi3MKGEQ==</P><Q>gsuUWBJk+fs2/TWLC+7sPELSGSpNVjIlcbCLJVdim5ga7+098oWsoCMyIDtAsHmhJBAFOn1boj8igfBcyqe9IQ==</Q><DP>KFp041rGm6JXq5Tms9JEWjZhjWj+espJ343LY+mRzkkV8Y59jCUsnFsC1DpAPJShyFJhvOF4Q8HLugrhE4oM4Q==</DP><DQ>VIGCA/FGinj0C19XyksBGOI3aoj5VLKrnKvrCyJNlKgO52uJ9bLGWOTEkyyVXDAJnAo5xpON6+MMwZVx7oG34Q==</DQ><InverseQ>BoVay/PxoLWTykHGGy1TyTsXjR9UP691Aa1ftuMGQnuVAKo/vBmV1D4E71Ewi725KgWPcOrus6n3koDuClQung==</InverseQ><D>B5PaD5FsaFuyz9Vx4KfDoLMz+CoAX6pZMRxbihZqCzLZijLv70brT51Z5hrka/26949QW3ZnkJKQIlUt3nGUtLWPWGFJ0el6q15g7C0ZUImuMZSFhXPwH7O+uOAgvdNSJLQf2U2X3sI7Kb4J9mE7cPoHp11bK9mKmQUbkhiUJAE=</D></RSAKeyValue>";

        public static string Encryption(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new Exception("Input is empty");

            using (var rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.FromXmlString(PublicKey);

                var data = Encoding.UTF8.GetBytes(plainText);
                var encrypted = rsa.Encrypt(data, false);

                return Convert.ToBase64String(encrypted);
            }
        }

        public static string Decryption(string base64Text)
        {
            if (string.IsNullOrWhiteSpace(base64Text))
                throw new Exception("Input is empty");

            using (var rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.FromXmlString(PrivateKey);

                var data = Convert.FromBase64String(base64Text);
                var decrypted = rsa.Decrypt(data, false);

                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
}
