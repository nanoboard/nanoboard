using System;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using Logos.Utility.Security.Cryptography;

namespace nboard
{
    static class ByteEncryptionUtil
    {
        private static byte[] Crop(this byte[] input, int len)
        {
            byte[] result = new byte[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = input[i];
            }

            return result;
        }

        public static byte[] EncryptSalsa20(byte[] input, string key)
        {
            byte[] initKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
            byte[] initVec = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key).Reverse().ToArray());
            initKey = initKey.Crop(32);
            initVec = initVec.Crop(8);
            var enc = new Salsa20().CreateEncryptor(initKey, initVec);
            byte[] output = enc.TransformFinalBlock(input, 0, input.Length);
            return output;
        }

        public static byte[] DecryptSalsa20(byte[] input, string key)
        {
            byte[] initKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
            byte[] initVec = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key).Reverse().ToArray());
            initKey = initKey.Crop(32);
            initVec = initVec.Crop(8);
            var enc = new Salsa20().CreateDecryptor(initKey, initVec);
            byte[] output = enc.TransformFinalBlock(input, 0, input.Length);
            return output;
        }

        public static byte[] WrappedXor(byte[] input, string key)
        {
            byte[] sha = SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
            byte[] output = new byte[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (byte) (input[i] ^ sha[i & 63]);
            }

            return output;
        }
    }
}