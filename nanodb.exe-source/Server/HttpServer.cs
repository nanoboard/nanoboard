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

        private readonly TcpServer _tcp;
        private readonly Dictionary<string,IRequestHandler> _handlers;
        private IRequestHandler _root;

        public HttpServer(string ip, int port)
        {
            Instance = this;
            _handlers = new Dictionary<string, IRequestHandler>();
            _tcp = new TcpServer(ip, port);
            _tcp.ConnectionAdded += OnConnectionAdded;
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
            var request = new HttpRequest(connection, connection.Request);
            if (request.Method == "GET" || request.Method == "POST") Process(connection, request);
            else if (request.Method!=null) connection.Response(new ErrorHandler(StatusCode.MethodNotAllowed, "Server only supports GET and POST").Handle(request));
        }

        private void Process(HttpConnection connection, HttpRequest request)
        {
            if (request.Address == "/" || request.Address == "")
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

            if (_handlers.ContainsKey(endpoint))
            {
                connection.Response(_handlers[endpoint].Handle(request));
            }

            else
            {
                connection.Response(new ErrorHandler(StatusCode.BadRequest, 
                    "Unknown endpoint: " + endpoint + ". Supported: " + JsonConvert.SerializeObject(_handlers.Keys.ToArray())).Handle(request));
            }
        }

        public void Stop()
        {
            _tcp.Stop();
        }
    }
}