using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using NDB;
using captcha;
namespace NServer
{
    class HttpServerBuilder
    {
        private readonly PostDb _db;

        public HttpServerBuilder(PostDb db)
        {
            _db = db;
        }

        /*
            Reads params needed for server to run, assigns handlers to endpoints, 
            returns server instance (without running it).
        */
        public HttpServer Build(bool enable_lite_server = false, string large_POST_mode = "0", string notif_mode = "0", bool allowReput = false, bool bypassValidation = false)	//by default, lite-server is disabled. run nanodb as "nanodb lite" to enable this.
        {
            string ip = Configurator.Instance.GetValue("ip", "127.0.0.1");  // pass default params in case if they are missing
//            int port = int.Parse(Configurator.Instance.GetValue("port", "7346"));
            int port = nbpack.NBPackMain.parse_number(Configurator.Instance.GetValue("port", "7346"));
            Configurator.Instance.GetValue("password", Configurator.DefaultPass);
            var server = new HttpServer(_db, ip, port, enable_lite_server, large_POST_mode, allowReput, bypassValidation);
            var pagesHandler = new FileHandler("pages", MimeType.Html);
            server.SetRootHandler(pagesHandler);
            server.AddHandler("api", new DbApiHandler(_db, false, allowReput, bypassValidation));
            server.AddHandler("solve", new SolveCaptchaAndAddPostHandler(_db));
            server.AddHandler("captcha", new GetCaptchaImageHandler());
            server.AddHandler("pow", new GetCaptchaTokenHandler());	Captcha.bypassValidation = bypassValidation;
            server.AddHandler("pages", pagesHandler);
            server.AddHandler("scripts", new FileHandler("scripts", MimeType.Js));
            //server.AddHandler("styles", new FileHandler("styles", MimeType.Css));
            server.AddHandler("styles", new StylesHandler());
            server.AddHandler("notif", new NotificationHandler(notif_mode));	//Notifs available only for full-server
            server.AddHandler("images", new FileHandler("images", MimeType.Image, true));
            server.AddHandler("reports", new FileHandler("reports", MimeType.Plain));

            server.AddHandler("containers", 				new FileHandler("containers", 			MimeType.Dynamic, 	true)	);		//any files in "containers"-folder, will opened with dynamic mime-type.
            server.AddHandler("download", 					new FileHandler("download", 			MimeType.Dynamic, 	true)	);		//any files in "download"-folder, will open with dynamic mime-type.
            server.AddHandler("download/generated", 		new FileHandler("download/generated", 	MimeType.Dynamic, 	true)	);		//any files in "download/generated"-folder, will open with dynamic mime-type.
            server.AddHandler("download/created", 			new FileHandler("download/created", 	MimeType.Dynamic, 	true)	);		//any files in "download/created"-folder, will open with dynamic mime-type.
            server.AddHandler("upload", 					new FileHandler("upload", 				MimeType.Dynamic, 	true)	);		//any files in "upload"-folder, will open with dynamic mime-type.
			//http://127.0.0.1:7346/download/Files_will_deleted.txt.	-	(dot in the end) application/octet-stream
			//http://127.0.0.1:7346/download/index.html.				-	(dot in the end) application/octet-stream
			//http://127.0.0.1:7346/download/index.html					-	text/html
			//http://127.0.0.1:7346/download/image.png					-	image/png
			//http://127.0.0.1:7346/download/Files_will_deleted.txt		-	text/plain
			//etc...
			
            server.AddHandler("fonts", new FileHandler("fonts", "application/font",true));

			if(enable_lite_server == true){
//____________________________________________________
//						lite-server handlers:
            var pagesHandler_lite = new FileHandler("pages_lite", MimeType.Html);
            server.AddHandler("api_lite", new DbApiHandler(_db, true, allowReput, bypassValidation));				//separate API for lite-server there is in DbApiHandler.cs
//            server.AddHandler("solve", new SolveCaptchaAndAddPostHandler(_db));
//            server.AddHandler("captcha", new GetCaptchaImageHandler());
//            server.AddHandler("pow", new GetCaptchaTokenHandler());
            server.AddHandler("pages_lite", pagesHandler_lite);
            server.AddHandler("scripts_lite", new FileHandler("scripts_lite", MimeType.Js));
            //server.AddHandler("styles", new FileHandler("styles", MimeType.Css));
//            server.AddHandler("styles", new StylesHandler());
//            server.AddHandler("notif_lite", new NotificationHandler());
//            server.AddHandler("images_lite", new FileHandler("images_lite", MimeType.Image, true));
//            server.AddHandler("download", new FileHandler("download", MimeType.Dynamic, true));	//files to download (not opened as images or html)
//            server.AddHandler("fonts", new FileHandler("fonts", "application/font",true));
//____________________________________________________
			}
			
//            server.AddHandler("shutdown", new ActionHandler("Server was shut down.", ()=>server.Stop()));		//temporary disable /shutdown to write log of requests. See HttpServer.cs. Process()

            server.AddHandler("restart", new ActionHandler("Server was restarted.", ()=>{
                server.Stop();
                server = Build();
                server.Run();
            }));
            return server;
        }
    }
}
