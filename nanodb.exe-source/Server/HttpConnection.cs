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
    /*
        Encapsulates live HTTP connection:
            raw request,
            response callbacks
    */
    class HttpConnection
    {
        public readonly string Request;
        private Action<string,string> _callback;
        private Action<string,byte[]> _binaryCallback;
        public readonly byte[] Raw;

        public HttpConnection(byte[] raw, string request, 
            Action<string,string> asciiUtf8callback,
            Action<string,byte[]> asciiBytesCallback = null)
        {
            this.Raw = raw;
            this.Request = request;
            this._callback = asciiUtf8callback;
            this._binaryCallback = asciiBytesCallback;
        }

        public void Response(HttpResponse response)
        {
            if (response.IsBinary)
                _binaryCallback(response.GetResponse(), response.GetBinaryContent());
            else
                _callback(response.GetResponse(), response.GetContent());
        }
    }
}
