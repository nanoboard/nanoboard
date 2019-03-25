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
using System.Drawing;	//for using Image type.

namespace nboard
{
    public class AggregatorMain 
    {
      private static float DOWNLOAD_TIMEOUT_SEC
        {
            get
            { 
                var timeoutStr = Configurator.Instance.GetValue("download_timeout_sec", "30");
                float timeout = 0;
                if (float.TryParse(timeoutStr, out timeout))
                {
                    return timeout;
                }
                return 30f;
            }
      }
      public static bool Running { get; private set; }
      public static void Run(string [] p = null, string url="", string save_to_filename="")
      {
        if (Running) return;
        _Main(p, url, save_to_filename);
      }
	  
      public static void _Main(string [] _params_ = null, string url="", string save_to_filename="") {
        bool running = true;
        Running = true;
        var agg = new Aggregator(_params_);
        agg.ProgressChanged += () => { if (agg.InProgress == 0) running = false; else running = true; };
        agg.Aggregate(url, save_to_filename);
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

		public static bool 	only_RAM 		= 	true;
		public static bool 	save_files 		= 	false;
		public static int 	max_connections = 	6;
		
	  
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

        private static List<string> proxies;

        private readonly HashSet<string> _downloaded;

        private readonly WebHeaderCollection _headers;

        public static void CheckUpdatePlacesConfig()
        {
            var places = Configurator.Instance.GetValue("places", File.Exists(Config)?File.ReadAllText(Config):"# put urls to threads here, each at new line:\n");
            File.Delete(Config);
            _places = places.Split('\n').Where(p => !p.StartsWith("#")).ToList();
			return;
        }

        public Aggregator(string [] _params_ = null)
        {
            try
            {
				if(_params_.Length!=0){
					for(int item=0; item<_params_.Length;item++)
					{
						Console.WriteLine("_params_[i] = "+_params_[item]);
						if(_params_[item] == "collect_using_files"){
							Console.WriteLine("ParseImage: collect_using_files, only_RAM = false now");
							only_RAM = false;
						}else if(_params_[item] == "save_files"){
							Console.WriteLine("ParseImage: save_files save_files = true now");
							save_files = true;
						}else if(_params_[item] == "delete_files"){
							Console.WriteLine("ParseImage: delete_files, save_files = false now");
							save_files = false;
						}else if(_params_[item] == "collect_using_RAM"){
							Console.WriteLine("ParseImage: collect_using_RAM, only_RAM = true now");
							only_RAM = true;
						}else{
							max_connections = nbpack.NBPackMain.parse_number(_params_[item]);
						}
					}
				}
				Console.WriteLine("only_RAM = "+only_RAM+", save_files = "+save_files+", max_connections = "+max_connections);
				

				//delete all files in "download" folder
				if(save_files==false){
					Console.WriteLine("Delete files in \"download//\" folder...");
					System.IO.DirectoryInfo di = new DirectoryInfo("download"+Path.DirectorySeparatorChar);
					foreach (FileInfo file in di.GetFiles()){file.Delete();}
					foreach (DirectoryInfo dir in di.GetDirectories()){dir.Delete(true);}
					// Create a file to write to.
					using (StreamWriter sw = File.CreateText("download"+Path.DirectorySeparatorChar+"Files_will_deleted.txt")) 
					{
						sw.WriteLine("Files in this folder will be deleted, if option for saving this not specified in GET-query.");
						sw.WriteLine("Don't store any files here or enable \"save_files\". See the parameters of the query, in /scripts/init.js");
					}	
				}

                if (!File.Exists(UserAgentConfig))
                {
                    File.WriteAllText(UserAgentConfig, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246");
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
			return;
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

        public void Aggregate(string url = "", string save_to_filename="")
        {
            try
            {
				if(url=="" && save_to_filename==""){
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
				}else if(url!="" && save_to_filename!=""){
					if (IsUriValid(url))
					{
						ParseImage(url, save_to_filename);
					}
				}
            }

            catch (Exception e)
            {
                Console.WriteLine("Error while parsing from places.txt:\n" + e.ToString());
            }
			return;
        }

/*
        private static void AddProxy(WebClientX client)
        {
            if (File.Exists("proxy.txt"))
            {
                //Console.WriteLine("PROXY USAGE IS DISABLED IN THIS VERSION (UNABLE TO SUPPORT PROXY FOR EVERY OCCASION)");
				var proxyUrl = File.ReadAllText("proxy.txt");
                WebProxy proxy = new WebProxy();
                proxy.Address = new Uri(proxyUrl);
				//proxy.Credentials = new NetworkCredential("usernameHere", "pa****rdHere");  //These can be replaced by user input
				//proxy.UseDefaultCredentials = false;										//this false, in this case...
				//proxy.BypassProxyOnLocal = true;
				proxy.BypassProxyOnLocal = false;  					//still use the proxy for local addresses
                //client._wc.Proxy = proxy;							//WebClient, not WebclientX, working when _wc is public in WebClientX.cs
                client = WebClientX(proxy);						//update client, using extended method
            }
        }//+ PNGTransport/WebClientX.cs: "private WebClient _wc;" -> "public WebClient _wc;"
*/
		
		private static bool Ping(string url)
		{
			try
			{
				//HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				WebRequest request = WebRequest.Create(url);	//"http:", "https:", "ftp:", and "file:"
				request.Timeout = 3000;
				//request.AllowAutoRedirect = false; // find out if this site is up and don't follow a redirector	//for HttpWebRequest
				request.Method = "HEAD";

				using (var response = request.GetResponse())
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

        private static WebProxy AddProxy(WebProxy proxy)
        {
            proxy = new WebProxy();
			
            if (File.Exists("proxy.txt"))
            {
				string proxyUrl = "";
				string proxy_file = File.Exists("proxy.txt")?File.ReadAllText("proxy.txt"):"";
				proxies = proxy_file.Split('\n').Where(p => !p.StartsWith("#")).ToList();
				foreach (var proxy_URL in proxies) {
					Console.WriteLine("Try ping proxy: "+proxy_URL);
					if(Ping(proxy_URL)==true){
						proxyUrl = proxy_URL;
						
						Console.WriteLine("Using proxy: "+proxy_URL);
						//Console.WriteLine("PROXY USAGE IS DISABLED IN THIS VERSION (UNABLE TO SUPPORT PROXY FOR EVERY OCCASION)");
						//var proxyUrl = File.ReadAllText("proxy.txt");
						proxy.Address = new Uri(proxyUrl);
						//proxy.Credentials = new NetworkCredential("usernameHere", "pa****rdHere");  //These can be replaced by user input
						//proxy.UseDefaultCredentials = false;										//this false, in this case...
						//proxy.BypassProxyOnLocal = true;
						proxy.BypassProxyOnLocal = false;  					//still use the proxy for local addresses
						//client._wc.Proxy = proxy;							//WebClient, not WebclientX, working when _wc is public in WebClientX.cs
						//client = WebClientX(proxy);						//update client, using extended method
						return proxy;
						
					}else{
						proxyUrl = "";
					}
				}
				if(proxyUrl == ""){Console.WriteLine("No responce from any proxy.");}
            }
			Console.WriteLine("Do not using proxy...");
			return proxy;
        }//+ PNGTransport/WebClientX.cs: "private WebClient _wc;" -> "public WebClient _wc;"

		WebProxy proxy = AddProxy(new WebProxy());
		//and change everywhere in this file:
		//var client = new WebClientX();
		//to var client = new WebClientX(proxy);

        private void ParseText(string address)
        {
            var client = new WebClientX(proxy);
            //AddProxy(client);
            client.Headers = _headers;

			//calculate host once from address, not for each picture
			//this code calculating host with folder for images relative pathways.
			//		__BEGIN__
			string[] items = address.Split('/');	//split address from settings by slash
			string host_and_folder = "";			//empty string
			string item = "";						//empty temp item
			int len = items.Length;					//array length
			int last = len-1;						//last element index.
			string host = "";						//host (IP:PORT only)
			string picture_host = "";				//temp empty variable to save picture host.
			string protocol_host_port = "";         //protocol://(domain/IP):PORT - without slash in the end
			
			for(int i = 0; i<len; i++){				//check all items
//				Console.WriteLine("item: {0}, value: '{1}'", i, items[i]);	//show item, value
				item = items[i];											//write item to temp string

				if(
						(i==0 && item.IndexOf(".")!=-1)						//if first item contains dot ("www.domain.com:1234/folder")
					|| 	i==2												//or if this is next item, after protocol
				){
					host = item;											//save this item as host.
				}

				if(
						i!=last												//if this is not last item
					|| 	(i==2 && len==3)									//or if domain/IP(+port) only, without slash
				){
					item = item+'/';										//add slash
				}
				if(
						(len!=3 && i==last)									//if last item
					&& 	item.IndexOf('.')!=-1								//and if dot found
				){//maybe, this is filename. If not - add slash in the end...
					//do not add this file to path
				}else{
					host_and_folder = String.Concat(host_and_folder, item);	//if not last item or if dot not found, add this as folder.
				}
			}
            protocol_host_port = Regex.Match(address, @"^(?<proto>\w+)://+?(?<host>[A-Za-z0-9\-\.]+)+?(?<port>:\d+)?/", RegexOptions.None).Result("${proto}://${host}${port}");
			//		__END__

            client.DownloadDataCompleted += bytes => 
            {
                if (bytes == null) {
                    Console.WriteLine("Null bytes received from " + address);
                    return;
                }
                Console.WriteLine("Finished: thread " + address);
                NotificationHandler.Instance.Messages.Enqueue("Downloaded: thread " + address);
                //_downloaded.Add(address);

                string imageAddress = "";
                try
                {
                    string text = Encoding.UTF8.GetString(bytes);
                    //string host = Regex.Match(address, "https?://[A-z\\.0-9-]*").Value;	//original code, commented.
					//return protocol://(IP/domain) only, without PORT, folder, and slash.

                    var images = Regex.Matches(text, ImgPattern);

                    foreach (Match im in images)
                    {
                        imageAddress = im.Value.Replace("href=", "").Trim('"');

                        if (imageAddress.Contains("http://") || imageAddress.Contains("https://"))//if link in href not relative and contains "http://" or "https://"
                        {
							//get host for picture
							//Warning, IP-loggers can working on another PORTS and PROTOCOLS!

							//picture_host = Regex.Match(imageAddress, "[^https?://][A-z\\.0-9:0-9]*").Value; //host+port - without protocol

							//another regular expressions:
								//protocol://(ip/domain):port
							//picture_host = Regex.Match(imageAddress, @"^(?<proto>\w+)://+?(?<host>[A-Za-z0-9\-\.]+)+?(?<port>:\d+)?/", RegexOptions.None, TimeSpan.FromMilliseconds(150)).Result("${proto}://${host}${port}");
								//(ip/domain):port only - without protocol.
							picture_host = Regex.Match(imageAddress, @"^(?<proto>\w+)://+?(?<host>[A-Za-z0-9\-\.]+)+?(?<port>:\d+)?/", RegexOptions.None).Result("${host}${port}");
								//If HTTPS board loading images from HTTP host no any difference in protocol...
								//But... IP-loggers on another protocol not excluded.

							//Console.WriteLine("---> picture_host:{0}", picture_host); //just write this
						}
						/*
						else if("ftp://" and another protocols...){}//do something...
						*/
						else if(imageAddress.Contains("://")){//if not http or https, but something://blah-blah...
							Console.WriteLine("Starting download: {0}\nUnknown protocol.",imageAddress); //just write this
							_downloaded.Add(imageAddress);
							continue;
						}else if(imageAddress[0]=='/'){										//if relative path "/img/pic.png"
							imageAddress = protocol_host_port+imageAddress;						//add to protocol_host_port
						}else if(imageAddress[0]=='.' && imageAddress[1]=='/'){				//if relative path "./img/pic.png"
							imageAddress = protocol_host_port+imageAddress.Substring(1); 		//add to protocol_host_port without dot.
						}else{
                            imageAddress = host_and_folder + imageAddress; 						//add relative pathway for picture, to the board host_and_folder.
                        }

                        if (IsUriValid(imageAddress))
                        {
                            //if (InProgress > 16)
                            if (InProgress > max_connections)
                            //if (InProgress > 4)
                            {
                                Thread.Sleep(4000);
                            }
							picture_host = Regex.Match(imageAddress, @"^(?<proto>\w+)://+?(?<host>[A-Za-z0-9\-\.]+)+?(?<port>:\d+)?/", RegexOptions.None).Result("${host}${port}");

							//Console.WriteLine("address: "+address+"\nimageAddress: "+imageAddress+"\n"); //show image address and picture address.
                            
							//IP-logging attempt prevention
							if (
									imageAddress.Contains("logger")		//iploggers by keyword "logger"
								||	imageAddress.Contains("bit.ly")		//some url shorters
								||	imageAddress.Contains("goo.gl")
							)
							{
								Console.WriteLine(	"Starting download: {0}\n"+	//show imageURL
													"Logging attempt prevented.",
													imageAddress
								);
								//and do nothing...
								_downloaded.Add(imageAddress);
							}else if( //block all pictures from another hosts, if...
									host_and_folder.IndexOf(picture_host)==-1				//picture_host not founded in host_and_folder
									//(https://someIPLOGGER.com/collectIP.png) loading from (https://wowchan.net/thread/res/1565/?search=png&timesorting=50)
									//true in this case								
								&&	picture_host.IndexOf(host)==-1
									//and if host not founded in picture_host (https://media.8ch.net/folder/pic.png loaded from https://8ch.net/)
									//false in this case, and true in prefious case.
							){
								//don't download this picture.
								//show message in console:
								Console.WriteLine(
													"Starting download: {0}\n"+
													"IP-log block: picHost('{1}') != boardHost('{2}')",
													imageAddress,
													picture_host,
													host
													//host is the part of host_and_folder and this must be founded (host_and_folder.indexOf(host)!=-1)
								);
								_downloaded.Add(imageAddress);
								//and do nothing...
							}else{//else, download and parse image from this imageAddress.
								//Console.WriteLine("_params_: "+_params_);
								ParseImage(imageAddress);
								_downloaded.Add(imageAddress);
							}
                        }else{
							Console.WriteLine("Invalid URI: "+imageAddress);
							_downloaded.Add(imageAddress);
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

        private void ParseImage(string address, string save_to_filename="")
        {
            if (_downloaded.Contains(address))
                return;
			
            var client = new WebClientX(proxy);
            //AddProxy(client);
            client.Headers = _headers;

            client.DownloadDataCompleted += bytes => 
            {
                if (bytes == null) {
                    Console.WriteLine("Ignoring null bytes");
                    return;
                }
                //InProgress -= 1;
                //NotificationHandler.Instance.Messages.Enqueue(InProgress + " items left to download");
				//Console.WriteLine("InProgress after -1: "+InProgress);

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

                    MemoryStream ms = null;
					Image RAM_container = null;
					string name = null;
					if(only_RAM==false){
						//Old code. 																				file pathway
						name = Guid.NewGuid().ToString().Trim('{', '}')+".png";
						File.WriteAllBytes("temp" + Path.DirectorySeparatorChar + name, bytes);
						File.Move("temp" + Path.DirectorySeparatorChar + name, "download" + Path.DirectorySeparatorChar + name);
						if(save_files==true){
							Console.WriteLine("\n"+address+"\nsaved as "+"download" + Path.DirectorySeparatorChar + name+"\n");
						}
                    }
					else{
						ms = new MemoryStream(bytes);
						RAM_container = Image.FromStream(ms);														//image in RAM
						if(save_files==true){
							var name_ms = "download" + Path.DirectorySeparatorChar + Guid.NewGuid().ToString().Trim('{', '}')+".png";
							File.WriteAllBytes(name_ms, ms.ToArray());
							Console.WriteLine("\n"+address+"\nsaved as "+name_ms+"\n");
						}
					}
					//Console.WriteLine("only_RAM = "+only_RAM+", save_files = "+save_files+", max_connections = "+max_connections);
					
					GC.Collect();
                    
					Console.WriteLine("Downloaded: " + address);
                    NotificationHandler.Instance.Messages.Enqueue("Downloaded: " + address);
					
                    if (only_RAM==true){
						nbpack.NBPackMain.ParseFile("http://" 
							+ Configurator.Instance.GetValue("ip", "127.0.0.1") 
							+ ":"
							+ Configurator.Instance.GetValue("port", "7346"),
							Configurator.Instance.GetValue("password", Configurator.DefaultPass),
							RAM_container																//Image in RAM
						);
					}else{
						nbpack.NBPackMain.ParseFile("http://" 
							+ Configurator.Instance.GetValue("ip", "127.0.0.1") 
							+ ":"
							+ Configurator.Instance.GetValue("port", "7346"),
							Configurator.Instance.GetValue("password", Configurator.DefaultPass),
							"download" + Path.DirectorySeparatorChar + name								//file pathway
							, save_files
						);						
					}

					if(only_RAM!=false){
						ms.Dispose();
						RAM_container.Dispose();																		//Image in RAM. Try flush RAM, after parsing.
					}
					
                    //_downloaded.Add(address);
                    
                    try
                    {
                        NDB.FileUtil.Append(Downloaded, address + "\n");
                    }
                    catch
                    {
						Console.WriteLine("Cannt append to downloaded.");
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
                InProgress -= 1;
                NotificationHandler.Instance.Messages.Enqueue(InProgress + " items left to download");
				//Console.WriteLine("InProgress after -1: "+InProgress);
            };

            InProgress += 1;
            //address = address.Replace("2ch.hk", "m2-ch.ru");		//m2-ch.ru not working.
            address = address.Replace("2ch.hk", "m2ch.hk");			//m2ch.hk working. See also the exception at line 265 with condition (picture_host!="2ch.hk" && host!="m2ch.hk")
            address = address.Replace("mm2ch.hk", "m2ch.hk");		//m2ch.hk contains 2ch.hk, and replaced to mm2ch.hk. Turn it back.
            Console.WriteLine("Starting download: " + address);
            if(save_to_filename==""){
				client.DownloadDataAsync(new Uri(address));
			}else{
				client.DownloadDataAsync(new Uri(address), save_to_filename);
			}
			return;
        }
    }
}
