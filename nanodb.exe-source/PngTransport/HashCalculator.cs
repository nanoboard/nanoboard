using System;
using System.Security.Cryptography;
using System.Text;

namespace nboard
{
    static class HashCalculator
    {
        public const int HashCrop = 16;

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