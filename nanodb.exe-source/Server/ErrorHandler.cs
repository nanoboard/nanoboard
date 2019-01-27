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

namespace NServer
{
    /*
        Template for page response that shows some server status code and message.
    */
    class ErrorHandler : IRequestHandler
    {
        private readonly string _statusLine;
        private readonly string _description;

        public ErrorHandler(string statusLine, string description)
        {
            _statusLine = statusLine;
            _description = description;
        }

        public HttpResponse Handle(HttpRequest request)
        {
            return new HttpResponse(
                _statusLine, 
                ("<big><strong>" + _statusLine + "</strong></big><br/>" + _description + "<br/><small><i>nboard-2.0</i></small>"),
                MimeType.Html);
        }
    }
}
