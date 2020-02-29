using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Linq;

namespace NServer
{
    class HttpServer
    {
        public HttpServer Instance;

        private readonly 	TcpServer 	_tcp;
        private readonly 	Dictionary<string,IRequestHandler>		_handlers;
        private 			IRequestHandler							_root;
		private readonly 	bool	lite_server 		= 		false;		//by default, lite-server is disabled, when enabled - this will be true.

        public HttpServer(NDB.PostDb db, string ip, int port, bool enable_lite_server = false, string large_POST_mode = "0", bool allowReput = false, bool bypassValidation = false)	//by default, lite-server is disabled.
        {
			lite_server = enable_lite_server;
//			Console.WriteLine("HttpServer.cs. HttpServer. lite_server: "+lite_server+", large_POST_mode: "+large_POST_mode);
            Instance = this;
            _handlers = new Dictionary<string, IRequestHandler>();
            _tcp = new TcpServer(db, ip, port, lite_server, large_POST_mode, allowReput, bypassValidation);
            _tcp.ConnectionAdded += OnConnectionAdded;
            _tcp.ConnectionAdded_lite += OnConnectionAdded_lite;
        }

        public void SetRootHandler(IRequestHandler handler)
        {
            _root = handler;   
        }

        public void AddHandler(string endpoint, IRequestHandler handler)
        {
            _handlers[endpoint] = handler;
        }

        public void Run()
        {
            _tcp.Run();
        }

        private void OnConnectionAdded(HttpConnection connection)
        {
//			Console.WriteLine("HttpServer.cs. OnConnectionAdded. connection.Request.Length: "+connection.Request.Length);
            var request = new HttpRequest(connection, connection.Request);
            if (request.Method == "GET" || request.Method == "POST"){
				Process(connection, request, false);
            }
			else if (request.Method!=null){
				connection.Response(new ErrorHandler(StatusCode.MethodNotAllowed, "Server only supports GET and POST").Handle(request));
			}
        }

        private void OnConnectionAdded_lite(HttpConnection connection)
        {
//			Console.WriteLine("HttpServer.cs. OnConnectionAdded_lite. connection.Request.Length: "+connection.Request.Length);
            var request = new HttpRequest(connection, connection.Request);
            if (request.Method == "GET" || request.Method == "POST"){
				Process(connection, request, true);
            }
			else if (request.Method!=null){
				connection.Response(new ErrorHandler(StatusCode.MethodNotAllowed, "Server only supports GET and POST").Handle(request));
			}
        }

        private void Process(HttpConnection connection, HttpRequest request, bool is_lite_server)
        {
			try{
//				if(is_lite_server == true){
//
//					string[] often_requests = { /*"notif",*/ "threadsize", "scripts", "styles", "getlastn", "count"};	//don't show this often requests
//		            
//					if(!often_requests.Any(request.Address.Contains)){			//if request.Address not contains all often requests
//
//						Console.WriteLine(
//							((is_lite_server)?"Lite":"Full")+", "+DateTime.Now+	//show server, show time,
//							": http://"+request.Host+request.Address			//and just show this request.
//						);
//
//						//Console.Write("\""+request.Address+"\", ");
//
//					}//else don't show request
//				}

				if (request.Address == "/" || request.Address == "" || request.Address.Contains("captcha.nbc"))
				{
					connection.Response(_root.Handle(request));
					return;
				}

				var splitted = request.Address.Split(new char[]{ '/' }, StringSplitOptions.RemoveEmptyEntries);

				if (splitted.Length == 0)
				{
					connection.Response(new ErrorHandler(StatusCode.BadRequest, "Invalid address").Handle(request));
					return;
				}

				var endpoint = splitted[0];
				if(endpoint == "download" && (splitted.Length>1 && (splitted[1] == "generated" || splitted[1] == "created"))){endpoint = "download/"+splitted[1];}
				else if( lite_server == true && is_lite_server == true){

//					Console.WriteLine("HttpServer.cs. Lite server: "+( lite_server == true && is_lite_server == true)+"Process. Replace endpoint");

					//Change endpoint for lite-server, if port of request is corresponding of default port for lite-server:
					//	/pages 		-> 		/pages_lite
					//	/scripts 	->		/scripts_lite
					//	/api		->		/api_lite
					//	etc...

					if(
							endpoint == "pages"
						|| 	endpoint == "scripts"
						|| 	endpoint == "api"
						||	endpoint == "shutdown"
						||	endpoint == "restart"
						||	endpoint == "reports"
						||	endpoint == "notif"		||	endpoint == "containers"
					){
						endpoint = endpoint + "_lite";
					}
				}
				if(request.Address == "/favicon.ico"){endpoint = "images";}				
				if (_handlers.ContainsKey(endpoint))
				{
					connection.Response(_handlers[endpoint].Handle(request));
				}
				else if(endpoint == "shutdown"){
					using (StreamWriter sw = File.AppendText("shutdown_requests.log")) 
					{
						sw.WriteLine(is_lite_server+", "+DateTime.Now+": http://"+request.Host+request.Address);
						sw.WriteLine("connection.Request: "+connection.Request);
						sw.WriteLine("\n");
					}
					connection.Response(new ErrorHandler(StatusCode.Ok, "Server was shutdown.").Handle(request));
					
					System.Threading.Thread.Sleep(500);
				
					Stop();
				}
				else{
					connection.Response(
						new ErrorHandler(StatusCode.BadRequest, 
						"Unknown endpoint: " + endpoint + ". Supported: " + JsonConvert.SerializeObject(_handlers.Keys.ToArray())).Handle(request));
				}
			}
			catch(Exception ex){
				Console.WriteLine("HttpServer.cs. Process. Exception: "+ex);
			}
        }

        public void Stop()
        {
            _tcp.Stop();
        }
    }
}