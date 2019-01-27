using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace NServer
{
    static class MimeType
    {
        public const string Html = "text/html; charset=utf-8";
        public const string Json = "application/json; charset=utf-8";
        public const string Js = "application/javascript; charset=utf-8";
        public const string Css = "text/css; charset=utf-8";
        public const string Plain = "text/plain; charset=utf-8";
        public const string Image = "image/png";
    }
    
}