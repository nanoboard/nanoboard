using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using NDB;
using nboard;

namespace captcha
{

    static class ByteStringExt
    {
        public static string Stringify(this byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static byte[] Bytify(this string @string)
        {
            var bytes = new byte[@string.Length / 2];

            for (int i = 0; i < @string.Length / 2; i++)
            {
                bytes[i] = byte.Parse(@string[i * 2] + "" + @string[i * 2 + 1], System.Globalization.NumberStyles.HexNumber);
            }

            return bytes;
        }

        public static int MaxLeadingZeros(this byte[] bytes)
        {
            int len = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    len += 1;
                }

                else
                {
                    return len;
                }
            }

            return len;
        }

        public static int MaxConsecZeros(this byte[] bytes, int startFrom = 0, int limit = 0)
        {
            int len = 0;
            int max_len = 0;

            for (int i = startFrom; i < bytes.Length; i++)
            {
                if (bytes[i] <= limit)
                {
                    len += 1;
                }

                else
                {
                    if (max_len < len)
                    {
                        max_len = len;
                    }

                    len = 0;
                }
            }

            return Math.Max(max_len, len);
        }
    }
}