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
    class NotificationHandler : IRequestHandler
    {
        public static NotificationHandler Instance;

        public Queue<string> Messages = new Queue<string>();

        public NotificationHandler()
        {
            Instance = this;
        }

        public HttpResponse Handle(HttpRequest request)
        //public async Task<HttpResponse> Handle(HttpRequest request)
        {
			//while(true){
				try
				{
					if (Messages.Count > 0)
					{
						return new HttpResponse(StatusCode.Ok, Messages.Dequeue());
					}
				}
				catch
				{
					Thread.Sleep(10); // Wait.
				}
			//}

            return new HttpResponse(StatusCode.Ok, "");
            //return new HttpResponse(StatusCode.BadRequest, "false");
        }
    }

    /*
        Does no more than invoking prefedined action and returning predefined reply to the client.
    */
    
}
