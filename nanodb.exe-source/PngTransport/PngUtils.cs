using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
 
namespace nboard
{
    class PngUtils
    {
        public static byte[] BitmapToByteArray(Image img)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
 
        public static Image ByteArrayToBitmap(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return Image.FromStream(ms);
            }
        }
 
        public static byte[] Combine(byte[] left, byte[] right)
        {
            byte[] combined = new byte[left.Length + right.Length];
            Buffer.BlockCopy(left, 0, combined, 0, left.Length);
            Buffer.BlockCopy(right, 0, combined, left.Length, right.Length);
            return combined;
        }
 
        public static byte[] RgbComponentsToBytes(Image innocuousImg)
        {
            Bitmap innocuousBmp = new Bitmap(innocuousImg);
            int counter = 0;
            byte[] components = new byte[3 * innocuousBmp.Width * innocuousBmp.Height];
            for (int y = 0; y < innocuousBmp.Height; y++)
            {
                for (int x = 0; x < innocuousBmp.Width; x++)
                {
                    Color c = innocuousBmp.GetPixel(x, y);
                    components[counter++] = c.R;
                    components[counter++] = c.G;
                    components[counter++] = c.B;
                }
            }
            return components;
        }
 
        public static Bitmap ByteArrayToBitmap(byte[] rgbComponents, int width, int hight)
        {
            Queue<byte> rgbComponentQueue = new Queue<byte>(rgbComponents);
            Bitmap bitmap = new Bitmap(width, hight);
            for (int y = 0; y < hight; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bitmap.SetPixel(x, y, Color.FromArgb(rgbComponentQueue.Dequeue(), rgbComponentQueue.Dequeue(), rgbComponentQueue.Dequeue()));
                }
            }
            return bitmap;
        }
    }
}