using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Collections.Generic;

namespace captcha
{
    /*
        Converts 1-bit headerless images to Bitmap instance and back.
    */
    class BitmapConvert
    {
        public static byte[] Convert(Bitmap bmp)
        {
            var bytes = new byte[bmp.Width * bmp.Height / 8];
            int bii = 0;
            int byi = 0;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    var pix = bmp.GetPixel(x, y);

                    if (pix.R < 128)
                    {
                        bytes[byi] |= (byte)(1 << bii);
                    }

                    bii += 1;

                    if (bii >= 8)
                    {   
                        bii = 0;
                        byi += 1;
                    }
                }
            }

            return bytes;
        }

        public static Bitmap Convert(byte[] bytes, int width = 50, int height = 20)
        {
            var bmp = new Bitmap(width, height);

            int bii = 0;
            int byi = 0;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    var color = Color.White;

                    if ((bytes[byi] & (byte)(1 << bii)) != 0)
                    {
                        color = Color.Black;
                    }

                    bii += 1;

                    if (bii >= 8)
                    {   
                        bii = 0;
                        byi += 1;
                    }

                    bmp.SetPixel(x, y, color);
                }
            }

            return bmp;
        }
    }
}