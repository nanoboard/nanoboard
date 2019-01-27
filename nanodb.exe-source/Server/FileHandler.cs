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

namespace NServer
{
    /*
        Serves files from specified folder.
        At the root (/) always serves small page with a link to pages/index.html
    */
    class FileHandler : IRequestHandler
    {
        private readonly string _mime;
        private readonly string _folder;
        private readonly bool _binary;

        public FileHandler(string folder, string mime, bool binary = false)
        {
            _mime = mime;
            _folder = folder;
            _binary = binary;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        public HttpResponse Handle(HttpRequest request)
        {
            try
            {
                if (request.Address == "" || request.Address == "/") 
                {
                    string redirect = "<meta http-equiv='refresh' content='0; url=/pages/index.html' />";
                    return new HttpResponse(StatusCode.Ok, redirect + "<a href='/pages/index.html'>[Enter]</a>", _mime);
                }

                var name = request.Address.Split('/').Last();
                var path = _folder + Path.DirectorySeparatorChar + name;

                if (!File.Exists(path))
                {
                    return new ErrorHandler(StatusCode.NotFound, "").Handle(request);
                }

                if (_binary)
                {
                    return new HttpResponse(StatusCode.Ok, File.ReadAllBytes(path), _mime);
                }

                return new HttpResponse(StatusCode.Ok, File.ReadAllText(path), _mime);
            }

            catch
            {
                return new ErrorHandler(StatusCode.InternalServerError, "").Handle(request);
            }
        }
    }
    
}
