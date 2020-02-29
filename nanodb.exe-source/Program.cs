using System;
using NDB;
using System.Linq;
using NServer;
using nboard;

namespace NDB
{
    class MainClass
    {
        /*
            Application's entry point. 
            Builds server, assigns new DB instance to it and runs it;
        */
        public static void Main(string[] args)
        {
			try{
//				bool 	lite_server 		= 	false;						//lite-server is disabled, by default. Run nanodb as "nanodb lite" to enable it.
				bool	lite_server 		= 	true;						//lite-server is enabled, by default. Run nanodb as "nanodb old" to disable it.
				string	large_POST_mode 	= 	"1";						//by defalt, Upload_Post TCP stream, one by one from large POST data, when this data received. 0 - disable, 2 - use file cache.
				string	notif_mode 			= 	"2";						//0 - old mode (empty response for each request, if no any notifs)
																			//1 - wait response from recursive function,
				//by default, 2 - enable async-await, using IAsyncResult with BeginInvoke, and wait HttpResponse from async_method().
				bool allowReput = false;	bool bypassValidation = false;
				for(var i = 0; i<args.Length; i++){
					//Console.WriteLine("args[i]: "+args[i]);
					if(		lite_server == false
						&&	new string[]{
								//keywords to run lite-server.
								"lite",
								"share",
								"public",
								"anon",
								"anonymous",
								"extended",
								"open"
							}
							.Contains(args[i])		//is corresponding of run-parameter
					){
						lite_server = true;				//enable lite-server.
					}
					else if(	lite_server == true
							&&	new string[]{
									"no_lite",
									"one",
									"old",
									"disable",
									"false"
								}.Contains(args[i])
					){
						lite_server = false;			//disable lite-server.
					}
					else if(
						(args[i]).StartsWith("large_POST_mode")
					){
						large_POST_mode = (args[i]).Split(new string[]{"large_POST_mode"}, StringSplitOptions.None)[1];	//one digit, as string "0", "1", or "2";
					}
					else if(args[i]=="allowReput"){allowReput = true;}else if(args[i]=="bypassValidation"){bypassValidation = true;}else if(
						(args[i]).StartsWith("notif_mode")
					){
						notif_mode = (args[i]).Split(new string[]{"notif_mode"}, StringSplitOptions.None)[1];			//one digit, as string "0", "1", or "2";
					}else if((args[i]).StartsWith("lite_images_timeout")){NServer.DbApiHandler.milliseconds_to_delete_generated_images = (double)nbpack.NBPackMain.parse_number((args[i]).Split(new string[]{"lite_images_timeout"}, StringSplitOptions.None)[1]); /*milliseconds_to_delete_generated_images*/}
				}

				//Console.WriteLine(DateTime.Now.ToString("R"));
				//Console.WriteLine("Program.cs: Aggregator.CheckUpdatePlacesConfig();");
				Aggregator.CheckUpdatePlacesConfig();			//Update, if places was been updated, and contains in places.txt
				//Console.WriteLine("Program.cs: Aggregator.CheckUpdateProxyList();");
				Aggregator.CheckUpdateProxyList();				//Update, if proxies was been updated, and contains in proxy.txt
				//Console.WriteLine("Program.cs: Aggregator.CheckUpdateIPServicesConfig();");
				Aggregator.CheckUpdateIPServicesConfig();		//Update, if IP_services was been updated in the "Settings" or externalIPservices.txt. See /pages/params.html
				var db = new PostDb(allowReput);
				nbpack.NBPackMain.PostDatabase = db;
				var serv = new HttpServerBuilder(db).Build(lite_server, large_POST_mode, notif_mode, allowReput, bypassValidation); //with lite server or not (true/false) + modes.
				serv.Run();            
			}
			catch(Exception ex){
				Console.WriteLine("Program.cs. catch. Exception1: "+ex);
			}
        }
    }
}
