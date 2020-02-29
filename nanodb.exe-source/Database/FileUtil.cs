using System;
using System.IO;

namespace NDB
{
    static class FileUtil
    {
        private static object _lock = new object();

        /* Appends bytes to the end of file */
        public static int Append(string path, string @string)
        {
            lock (_lock) //sometimes .db3 file busy by another process, when program try to append string there. lock it.
            {
				return Append(path, System.Text.Encoding.UTF8.GetBytes(@string));
			}
        }

        /* Appends bytes to the end of file */
        public static int Append(string path, byte[] bytes)
        {
            lock (_lock)
            {
                long pos = 0;

                using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    pos = stream.Position;
                    stream.Write(bytes, 0, bytes.Length);
					stream.Close();
					stream.Dispose();
                }

                return (int)pos;
            }
        }
        
        /* Writes bytes at specific file offset, overwrites existing bytes */
        public static void Write(string path, byte[] bytes, int offset)
        {
            lock (_lock)
            {
                using (Stream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    stream.Write(bytes, 0, bytes.Length);
					stream.Close();
					stream.Dispose();
                }
            }
        }

        /* Reads bytes from file using specific offset and length */
        public static byte[] Read(string path, int offset, int length)
        {
            var bytes = new byte[length];
            using (Stream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Read(bytes, 0, length);
				stream.Close();
				stream.Dispose();
            }
            return bytes;
        }
    }    
}
