using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using NDB;
using Newtonsoft.Json;

namespace NServer
{
    /*
        config param: skin - if empty ("") - root of styles folder used
        if contains name of skin - styles/skin_name/ folder is used
        config param: skins contains JSON array with available skin names
    */
    class StylesHandler : IRequestHandler
    {
        private readonly FileHandler _filehandler;
		public static string currentSkin = "";
        public StylesHandler()
        {
            _filehandler = new FileHandler("styles", MimeType.Css, false);
            var skinsDir = "styles" + Path.DirectorySeparatorChar + "skins";

            if (Directory.Exists(skinsDir))
            {
                var skins = Directory.GetDirectories(skinsDir);
                skins = skins.Select(s => s.Split('\\', '/').Last()).ToArray();
                Configurator.Instance.SetValue("skins", JsonConvert.SerializeObject(skins));
            }
			Update_currentSkin();
        }

		public static void Update_currentSkin(){
            currentSkin = Configurator.Instance.GetValue("skin", "");//Console.WriteLine("currentSkin: "+currentSkin);
			string styles_folder = ( ( currentSkin == "" || currentSkin == "None") ? "styles/" : ( ( "styles/skins/" + currentSkin + "/" ) ) ).Replace('/', Path.DirectorySeparatorChar);
			if(!File.Exists(styles_folder+"notif.css") || !File.Exists(styles_folder+"bootstrap.min.css") || !File.Exists(styles_folder+"nano.css")){
				Console.WriteLine("StylesHandler.cs. Some files does not exists in "+styles_folder+"! Using the default style...");
				Configurator.Instance.SetValue("skin", "");
				currentSkin = "";
			}
		}
        public HttpResponse Handle(HttpRequest request)
        {
            if (currentSkin == "" || currentSkin == "None"){
				return _filehandler.Handle(request);
			}
            var file = request.Address.Replace("styles/", "styles/skins/" + currentSkin + "/").TrimStart('/');
            return new HttpResponse(StatusCode.Ok, File.ReadAllText(file.Replace('/', Path.DirectorySeparatorChar)), MimeType.Css);
        }
    }
}