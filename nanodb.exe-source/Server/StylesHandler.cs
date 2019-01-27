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
        }

        public HttpResponse Handle(HttpRequest request)
        {
            var currentSkin = Configurator.Instance.GetValue("skin", "");

            if (currentSkin == "")
            {
                return _filehandler.Handle(request);
            }

            var file = request.Address.Replace("styles/", "styles/skins/" + currentSkin + "/").TrimStart('/');
            return new HttpResponse(StatusCode.Ok, File.ReadAllText(file.Replace('/', Path.DirectorySeparatorChar)), MimeType.Css);
        }
    }
}