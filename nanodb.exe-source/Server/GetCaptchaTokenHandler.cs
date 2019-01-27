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
        Content: toBase64(post.replyTo + post.message)
        Response: captcha token string
    */
    class GetCaptchaTokenHandler : IRequestHandler
    {
        public HttpResponse Handle(HttpRequest request)
        {
            var token = Guid.NewGuid().ToString();
            CaptchaTracker.Posts[token] = Captcha.AddPow(request.Content.FromB64());
            var captcha = Captcha.GetCaptchaForPost(CaptchaTracker.Posts[token]);
            CaptchaTracker.Captchas[token] = captcha;
            return new HttpResponse(StatusCode.Ok, token);
        }
    }
}
