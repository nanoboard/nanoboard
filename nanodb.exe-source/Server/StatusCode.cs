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
    static class StatusCode
    {
        public const string Ok = "200 OK";
        public const string NotModified = "304 Not Modified";
        public const string NotFound = "404 Not Found";
        public const string BadRequest = "400 Bad Request";
        public const string MethodNotAllowed = "405 Method Not Allowed";
        public const string InternalServerError = "500 Internal Server Error";
    }
}