using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace nbpack
{
    // Uses cURL process to retreive data from URLs without Mono's http api
    public class CurlWebClient
    {
        public CookieContainer container = new CookieContainer();
        public WebHeaderCollection Headers = new WebHeaderCollection();

        public byte[] DownloadBytes(string url)
        {
            var args = "";

            for (int i = 0; i < Headers.Count; i++)
            {
                args += "-H '" + Headers.GetKey(i) + ": " + Headers.Get(i) + "' ";
            }

            var cc = container.GetCookieHeader(new Uri(url));

            if (!string.IsNullOrEmpty(cc))
            {
                args += "-H '" + cc + "'";
            }

            if (args == "-H '") args = "";

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "curl";
            start.Arguments = args + url;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.RedirectStandardInput = false;
            start.CreateNoWindow = true;

            Process p = Process.Start(start);
			p.WaitForExit();

            FileStream baseStream = p.StandardOutput.BaseStream as FileStream;
            byte[] bytes = null;
            int lastRead = 0;

            using (MemoryStream ms = new MemoryStream())
            {            
                byte[] buffer = new byte[4096];
                do
                {
                    lastRead = baseStream.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, lastRead);
                } while (lastRead > 0);

                bytes = ms.ToArray();
				baseStream.Close();
				baseStream.Dispose();
            }

            p.WaitForExit();
            return bytes;
        }


        public WebHeaderCollection DownloadHeaders(string url)
        {
            var args = "";

            for (int i = 0; i < Headers.Count; i++)
            {
                args += "-H '" + Headers.GetKey(i) + ": " + Headers.Get(i) + "' ";
            }

            var cc = container.GetCookieHeader(new Uri(url));

            if (!string.IsNullOrEmpty(cc))
            {
                args += "-H '" + cc + "'";
            }

            if (args == "-H '")
                args = "";

            Console.WriteLine("CURL REQUEST: -I " + args + url);

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "curl";
            start.Arguments = "-I " + args + url;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.RedirectStandardInput = false;
            start.CreateNoWindow = true;

            Process p = Process.Start(start);
			p.WaitForExit();
            Console.WriteLine("CURL PROCESS STARTED");


            FileStream baseStream = p.StandardOutput.BaseStream as FileStream;
            byte[] bytes = null;
            int lastRead = 0;

            using (MemoryStream ms = new MemoryStream())
            {            
                byte[] buffer = new byte[4096];
                do
                {
                    lastRead = baseStream.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, lastRead);
                } while (lastRead > 0);

                bytes = ms.ToArray();
				baseStream.Close();
				baseStream.Dispose();
            }

            p.WaitForExit();
            Console.WriteLine(bytes.Length + " Downloaded");

            var str = Encoding.UTF8.GetString(bytes);

            var hs = new WebHeaderCollection();

            try
            {
                var lines = str.Split('\n');
                bool first = true;
                foreach (var line in lines)
                {
                    if (first)
                    {
                        var status = line.Split(' ');
                        hs.Add("Status", status[1]);
                        first = false;
                        continue;
                    }

                    if (line.Length > 2)
                    {
                        var header = line.Split(new char[]{':'}, 2, StringSplitOptions.None);
                        hs.Add(header[0], header[1]);
                        Console.WriteLine("GOT HEADER " + header[0] + ": " + header[1]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return hs;
        }

        public string DownloadString(string url)
        {
            return Encoding.UTF8.GetString(DownloadBytes(url));
        }
    }
}

