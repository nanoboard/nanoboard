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
using captcha;

namespace NServer
{
    /*
        Address: captcha token
        Response: data uri png
    */
    class GetCaptchaImageHandler : IRequestHandler
    {
        public HttpResponse Handle(HttpRequest request)
        {
            var token = request.Address.Split('/').Last();

            if (!CaptchaTracker.Captchas.ContainsKey(token))
            {
                return new HttpResponse(StatusCode.BadRequest, "Not existing token");
            }

            var captcha = CaptchaTracker.Captchas[token];
            return new HttpResponse(StatusCode.Ok, captcha.ImageDataUri);
        }
    }
}
