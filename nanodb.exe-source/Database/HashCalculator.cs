using System.Security.Cryptography;
using System.Text;

namespace NDB
{
    static class HashCalculator
    {
        public const int HashCrop = 16;

        /*
            raw string is post's replyTo hash + post's message (concatenated without any separators)
            and calculates hash of such post (similar to replyTo hash). 
            The hash is first 16 bytes of SHA-256 of raw string converted to a hexadecimal string (32 characters as result),
            example hash (not real): 1234567890abcdefgh1234567890abcdefgh
        */
        public static string Calculate(string raw)
        {
            byte[] bhash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(raw));
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < HashCrop; i++)
            {
                sb.Append(bhash[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
