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
        Does no more than invoking prefedined action and returning predefined reply to the client.
    */
    class ActionHandler : IRequestHandler
    {
        private readonly string _okMessage;
        private readonly Action _action;

        public ActionHandler(string okMessage, Action action)
        {
            _okMessage = okMessage;
            _action = action;
        }

        public HttpResponse Handle(HttpRequest request)
        {
            _action();
            return new HttpResponse(StatusCode.Ok, _okMessage);
        }
    }
    
}
