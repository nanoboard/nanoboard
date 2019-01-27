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
    class HttpResponse
    {
        private readonly string _response;
        private readonly string _content;
        private readonly byte[] _bytes;

        public bool IsBinary
        {
            get
            {
                return _bytes != null;
            }
        }

        public HttpResponse(string code, byte[] bytes, string contentType)
        {
            _response = "HTTP/1.1 " + code + "\r\nContent-type: " + contentType + (bytes == null ? "\r\n" : ("\r\n\r\n"));
            _bytes = bytes;
        }

        public HttpResponse(string code, string content, string contentType)
        {
            _response = "HTTP/1.1 " + code + "\r\nContent-type: " + contentType + (string.IsNullOrEmpty(content) ? "\r\n" : ("\r\n\r\n"));
            _content = content;
        }

        public HttpResponse(string code, string content)
            : this(code, content, MimeType.Plain)
        {
            //_response = "HTTP/1.1 " + code + (string.IsNullOrEmpty(content) ? "\r\n" : ("\r\n\r\n"));
            //_content = content;
        }

        public string GetResponse()
        {
            return _response;
        }

        public string GetContent()
        {
            return _content;
        }

        public byte[] GetBinaryContent()
        {
            return _bytes;
        }
    }
}