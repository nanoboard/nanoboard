using System.IO.Compression;
using System.IO;

namespace nboard
{
    static class GZipUtil
    {
        // Only useful before .NET 4
        public static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        public static byte[] Compress(byte[] input)
        {
            try
            {
                using (var output = new MemoryStream())
                {
                    using (var gz = new GZipStream(output, CompressionMode.Compress))
                    using (var ms = new MemoryStream(input))
                        ms.CopyTo(gz);
                    return output.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        public static byte[] Decompress(byte[] input)
        {
            try
            {
                using (var output = new MemoryStream())
                {
                    using (var ms = new MemoryStream(input))
                    using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                        gz.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}