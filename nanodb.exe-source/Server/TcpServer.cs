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
        Low-level limited implementation of HTTP server based on TCP socket.
    */
    class TcpServer
    {
        private TcpListener _server;
        private Boolean _isRunning;

        public event Action<HttpConnection> ConnectionAdded;

        public TcpServer(string ip, int port)
        {
            Console.WriteLine("Listening on port " + port);

            if (ip == "localhost")
            {
                IPAddress ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
                Console.WriteLine(ipAddress.ToString() + ":" + port);
                IPEndPoint ipLocalEndPoint = new IPEndPoint(ipAddress, port);
                _server = new TcpListener(ipLocalEndPoint);
            }
                
            else
            {
                _server = new TcpListener(IPAddress.Parse(ip), port);
            }
        }

        public void Run()
        {
            _server.Start();
            _isRunning = true;
            LoopClients();
        }

        public void Stop()
        {
            Console.WriteLine("Server was shut down");
            _isRunning = false;
            _server.Stop();
        }

        private void LoopClients()
        {
            while (_isRunning)
            {
                if (!_server.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }

                try
                {
                    TcpClient newClient = _server.AcceptTcpClient();
                    Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                    t.Start(newClient);
                }
                catch
                {
                }
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            String readData = "";
            bool noTcpDelay = Configurator.Instance.GetValue("no_tcp_delay", "true") == "true";
            stream.ReadTimeout = noTcpDelay ? 15 : 100;
            var buffer = new byte[16384];
            int len = -1;
            int contentLength = 0;
            List<byte> raw = new List<byte>();

            // try read all the data to the end
            do
            {
                try
                {
                    if (!noTcpDelay)
                    if (raw.Count == 0 || raw[0] == (byte)'P')
                        Thread.Sleep(50);
                    len = stream.Read(buffer, 0, buffer.Length);
                    var block = System.Text.Encoding.UTF8.GetString(buffer, 0, len);
                    readData += block;
                    for (int i = 0; i < len; i++)
                        raw.Add(buffer[i]);
                }
                catch
                {
                    if (!noTcpDelay)
                    if (raw.Count == 0 || raw[0] == (byte)'P')
                        Thread.Sleep(50);
                    if (contentLength == 0 && readData.Contains("Content-Length"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(readData, "Content-Length: [0-9]+");
                        if (match.Success)
                        {
//                            contentLength = int.Parse(match.Value.Split(' ')[1]);
                            contentLength = nbpack.NBPackMain.parse_number(match.Value.Split(' ')[1]);
                            contentLength += readData.Split(new[]{ "\r\n\r\n" }, StringSplitOptions.None)[0].Length;
                        }
                    }
                    len = -1;
                }
            }
            while (len > 0 || raw.Count < contentLength);

            try
            {
                // notify about pending connection
                if (ConnectionAdded != null)
                {
                    ConnectionAdded(new HttpConnection(raw.ToArray(), readData, (ascii, utf8) =>
                    {
                        byte[] ba = ascii == null ? new byte[0] : Encoding.ASCII.GetBytes(ascii);
                        byte[] bu = utf8 == null ? new byte[0] : Encoding.UTF8.GetBytes(utf8);
                        try
                        {
                            stream.Write(ba, 0, ba.Length);
                            stream.Write(bu, 0, bu.Length);
                            stream.Flush();
                        }
                        catch
                        {
                        }
                        finally
                        {
                            client.Close();
                        }
                    }, (ascii, bytes) =>
                    {
                        byte[] ba = Encoding.ASCII.GetBytes(ascii);
                        byte[] bu = bytes;
                        try
                        {
                            stream.Write(ba, 0, ba.Length);
                            stream.Write(bu, 0, bu.Length);
                            stream.Flush();
                        }
                        catch
                        {
                        }
                        finally
                        {
                            client.Close();
                        }
                    }));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Something is wrong with connection between your browser and nanoboard client...");
                Console.WriteLine("But that's ok, don't worry.");
				Console.WriteLine("Exception: "+ex);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
