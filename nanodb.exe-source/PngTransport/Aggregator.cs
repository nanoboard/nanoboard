using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using NServer;
using nbpack;

namespace nboard
{
    public class AggregatorMain 
    {
      private static float DOWNLOAD_TIMEOUT_SEC
        {
            get
            { 
                var timeoutStr = Configurator.Instance.GetValue("Download_Timeout_Sec", "30");
                float timeout = 0;
                if (float.TryParse(timeoutStr, out timeout))
                {
                    return timeout;
                }
                return 30f;
            }
      }
      public static bool Running { get; private set; }
      public static void Run()
      {
        if (Running) return;
        _Main();
      }
      public static void _Main() {
        bool running = true;
        Running = true;
        var agg = new Aggregator();
        agg.ProgressChanged += () => { if (agg.InProgress == 0) running = false; else running = true; };
        agg.Aggregate();
        int lastProg = 0;
        float progStaySec = 0;
        while(running) {
          Thread.Sleep(100);
          if (agg.InProgress == lastProg) progStaySec += 0.1f;
          else {
            progStaySec = 0;
            lastProg = agg.InProgress;
          }
          if (progStaySec > DOWNLOAD_TIMEOUT_SEC)
          {
            WebClientX.Interrupt();
            agg.InProgress = 0;
          }
        }
        Running = false;
        Console.WriteLine("Finished.");
      }
    }

    public class Aggregator
    {
        private const string UserAgentConfig = "useragent.config";
        private const string Downloaded = "downloaded.txt";
        private const string Config = "places.txt";
        private const string ImgPattern = "href=\"[:A-z0-9/\\-\\.]*\\.png\"";

        private int _inProgress = 0;
        public int InProgress
        { 
            get
            {
                return _inProgress;
            }

            set
            {
                if (value < 0) value = 0;
                _inProgress = value;
                ProgressChanged();
            }
        }

        public event Action ProgressChanged = delegate {};

        private static List<string> _places;

        private readonly HashSet<string> _downloaded;

        private readonly WebHeaderCollection _headers;

        public static void CheckUpdatePlacesConfig()
        {
            var places = Configurator.Instance.GetValue("places", File.Exists(Config)?File.ReadAllText(Config):"# put urls to threads here, each at new line:\n");
            File.Delete(Config);
            _places = places.Split('\n').Where(p => !p.StartsWith("#")).ToList();
        }

        public Aggregator()
        {
            try
            {
                if (!File.Exists(UserAgentConfig))
                {
                    File.WriteAllText(UserAgentConfig, "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2227.1 Safari/537.36");
                }

                string userAgent = File.ReadAllLines(UserAgentConfig).First(l => !l.StartsWith("#")).Trim();
                _headers = new WebHeaderCollection();
                _headers[HttpRequestHeader.UserAgent] = userAgent;
                _headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                _headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.8";
                _headers[HttpRequestHeader.CacheControl] = "max-age=0";

                if (File.Exists(Downloaded))
                {
                    _downloaded = new HashSet<string>(File.ReadAllLines(Downloaded));
                }
                else
                {
                    _downloaded = new HashSet<string>();
                }

                CheckUpdatePlacesConfig();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while creating containers aggregator:\n" + e.ToString());
            }
        }

        private bool IsUriValid(string uri)
        {
            try
            {
                var u = new Uri(uri);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Aggregate()
        {
            try
            {
                CheckUpdatePlacesConfig();
                bool empty = true;

                foreach (string place in _places)
                {
                    if (!place.StartsWith("#"))
                    {
                        empty = false;

                        if (IsUriValid(place))
                        {
                            ParseText(place);
                        }
                    }
                }

                if (empty)
                {
                    InProgress = 0;
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Error while parsing from places.txt:\n" + e.ToString());
            }
        }

        private static void AddProxy(WebClientX client)
        {
            if (File.Exists("proxy.txt"))
            {
                Console.WriteLine("PROXY USAGE IS DISABLED IN THIS VERSION (UNABLE TO SUPPORT PROXY FOR EVERY OCCASION)");
                /*var proxyUrl = File.ReadAllText("proxy.txt");
                WebProxy proxy = new WebProxy();
                proxy.Address = new Uri(proxyUrl);
                proxy.BypassProxyOnLocal = true;
                client.Proxy = proxy;*/
            }
        }

        private void ParseText(string address)
        {
            var client = new WebClientX();
            AddProxy(client);
            client.Headers = _headers;

            client.DownloadDataCompleted += bytes => 
            {
                if (bytes == null) {
                    Console.WriteLine("Null bytes received from " + address);
                    return;
                }
                Console.WriteLine("Finished: " + address);
                NotificationHandler.Instance.Messages.Enqueue("Downloaded: " + address);
                //_downloaded.Add(address);

                string imageAddress = "";
                try
                {
                    string text = Encoding.UTF8.GetString(bytes);
                    string host = Regex.Match(address, "https?://[A-z\\.0-9-]*").Value;

                    var images = Regex.Matches(text, ImgPattern);

                    foreach (Match im in images)
                    {
                        imageAddress = im.Value.Replace("href=", "").Trim('"');

                        if (imageAddress.Contains("http://") || imageAddress.Contains("https://"))
                        {
                        }

                        else
                        {
                            imageAddress = host + imageAddress;
                        }

                        if (IsUriValid(imageAddress))
                        {
                            if (InProgress > 16)
                            {
                                Thread.Sleep(4000);
                            }

                            ParseImage(imageAddress);
                        }
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading image:" + imageAddress);
//                    if (e.Error != null)
//                        Console.WriteLine(e.Error.Message);
                    Console.WriteLine(ex.Message);
                }
                InProgress -= 1;
                NotificationHandler.Instance.Messages.Enqueue(InProgress + " items left to download");
            };

            InProgress += 1;
            Console.WriteLine("Starting download: " + address);
            client.DownloadDataAsync(new Uri(address));
        }

        private void ParseImage(string address)
        {
            if (_downloaded.Contains(address))
                return;

            var client = new WebClientX();
            AddProxy(client);
            client.Headers = _headers;

            client.DownloadDataCompleted += bytes => 
            {
                if (bytes == null) {
                    Console.WriteLine("Ignoring null bytes");
                    return;
                }
                InProgress -= 1;
                NotificationHandler.Instance.Messages.Enqueue(InProgress + " items left to download");

                try
                {
                    if (!Directory.Exists("temp"))
                    {
                        Directory.CreateDirectory("temp");
                    }

                    if (!Directory.Exists("download"))
                    {
                        Directory.CreateDirectory("download");
                    }

                    var name = Guid.NewGuid().ToString().Trim('{', '}');
                    File.WriteAllBytes("temp" + Path.DirectorySeparatorChar + name, bytes);
                    File.Move("temp" + Path.DirectorySeparatorChar + name, "download" + Path.DirectorySeparatorChar + name);
                    GC.Collect();
                    Console.WriteLine("Downloaded: " + address);
                    NotificationHandler.Instance.Messages.Enqueue("Downloaded: " + address);

                    nbpack.NBPackMain.ParseFile("http://" 
                        + Configurator.Instance.GetValue("ip", "127.0.0.1") 
                        + ":"
                        + Configurator.Instance.GetValue("port", "7346"),
                        Configurator.Instance.GetValue("password", Configurator.DefaultPass), "download" + Path.DirectorySeparatorChar + name);
                    _downloaded.Add(address);
                    
                    try
                    {
                        NDB.FileUtil.Append(Downloaded, address + "\n");
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(1000);

                        try
                        {
                            NDB.FileUtil.Append(Downloaded, address + "\n");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("downloaded.txt appending error:\n" + e.ToString());
                        }
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading url: " + address);
//                    if (e.Error != null)
//                        Console.WriteLine(e.Error.Message);
                    Console.WriteLine(ex.Message);
                }
            };

            InProgress += 1;
            address = address.Replace("2ch.hk", "m2-ch.ru");
            Console.WriteLine("Starting download: " + address);
            client.DownloadDataAsync(new Uri(address));
        }
    }
}
