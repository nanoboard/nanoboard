using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using NServer;

namespace nbpack
{
    public class WebClientX
    {
        private static List<WebClient> _clients = new List<WebClient>();

        public static void Interrupt()
        {
            try
            {
                _clients.ToArray().ToList().ForEach(c => {
                    try{c.CancelAsync();}catch{}
                 });
                _clients.Clear();
            }
            catch
            {
            }
        }

        public event Action<byte[]> DownloadDataCompleted = delegate{};

        public void DownloadDataAsync(Uri uri)
        {
            _wc.DownloadDataCompleted += (object sender, DownloadDataCompletedEventArgs e) =>
            {
                try{_clients.Remove(_wc);}catch{}
                try
                {
                    try
                    {
                    var bytes = e.Result;

                    if (bytes.Length < 8192)
                    {
                        var str = System.Text.Encoding.UTF8.GetString(bytes);

                        if (str.Contains("<a href=\"https://www.cloudflare.com/5xx-error-landing"))
                        {
                            NotificationHandler.Instance.Messages.Enqueue("Error! CloudFlare on " + uri.Host);
                            Console.WriteLine("CloudFlare Exception\n" + uri + " is under CloudFlare protection, cannot parse!");
                        }
                    }} catch {}
                    if (e.Result == null) {
                        throw new Exception("SendFailure");
                    }
                    DownloadDataCompleted(e.Result);
                } catch  (Exception e1)
                {
                    var exceptionStr = e1.ToString();
                    bool tlsErr = exceptionStr.Contains("SendFailure");
                    if (tlsErr)
                    {
                        Console.WriteLine("Mono TLS Exception\n" + uri + " will use cURL for this URL");
                        try
                        {
                            var eResult = _cwc.DownloadBytes(uri.AbsoluteUri);
                            try
                            {
                                if (eResult.Length < 8192)
                            {
                                var str = System.Text.Encoding.UTF8.GetString(eResult);

                                if (str.Contains("<a href=\"https://www.cloudflare.com/5xx-error-landing"))
                                {
                                    NotificationHandler.Instance.Messages.Enqueue("Error! CloudFlare on " + uri.Host);
                                    Console.WriteLine("CloudFlare Exception\n" + uri + " is under CloudFlare protection, cannot parse!");
                                }
                            }} catch {}

                            DownloadDataCompleted(eResult);
                        } 
                        catch (Exception e3)
                        {
                            Console.WriteLine(e3.ToString());
                        }
                    }
                }
            };
            try{_clients.Add(_wc);}catch{}
            //_wc.DownloadDataAsync(new Uri(uri.AbsoluteUri.Replace("https://", "http://")));
            _wc.DownloadDataAsync(new Uri(uri.AbsoluteUri));	//delete replace.
        }
		
        public void DownloadFile(Uri uri, string fileName)	//download large file.
        {
            _wc.DownloadFile(new Uri(uri.AbsoluteUri), fileName);
        }

        public void CancelAsync()	//download large file.
        {
            _wc.CancelAsync();
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return _wc.Headers;
            }
            set
            {
                _cwc.Headers = value;
                _wc.Headers = value;
            }
        }

/*
        public WebClientX()
        {
            _cwc = new CurlWebClient();
            _wc = new WebClient();
        }
		//old code
*/

        public WebClientX(WebProxy proxy = null)
        {
			//working with string in Aggregator.cs
			//client = new WebClientX(proxy);	//WebClient, not WebclientX
			//or without proxy
            _cwc = new CurlWebClient();
            _wc = new WebClient();
			if(proxy!=null){
				//Console.WriteLine("Add proxy..."+proxy);
				_wc.Proxy = proxy;
			}
        }

        private WebClient _wc;			//Old code
//        public WebClient _wc;			//woking with old code when Aggregator.cs contains client._wc.Proxy = proxy; //WebClient, not WebclientX
        private CurlWebClient _cwc;
    }

    // for the future use:

    public enum Platform
    {
        Windows,
        Linux,
        Mac
    }

    public static class Env
    {
        private static Platform? _detected;

        public static Platform GetPlatform()
        {
            if (_detected.HasValue)
                return _detected.Value;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        _detected = Platform.Mac;
                    else
                        _detected = Platform.Linux;
                    break;
                case PlatformID.MacOSX:
                    _detected = Platform.Mac;
                    break;
                default:
                    _detected = Platform.Windows;
                    break;
            }

            return _detected.Value;
        }
    }
}
