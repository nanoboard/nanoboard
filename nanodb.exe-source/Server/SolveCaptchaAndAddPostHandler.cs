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
using Newtonsoft.Json;

namespace NServer
{
    /*
        Address: captcha token
        Content: toBase64(captcha answer)
        Response: 200 OK in case of success
    */
    class SolveCaptchaAndAddPostHandler : IRequestHandler
    {
        private readonly PostDb _db;

        public SolveCaptchaAndAddPostHandler(PostDb db)
        {
            _db = db;
        }

        public HttpResponse Handle(HttpRequest request)
        {
            var token = request.Address.Split('/').Last();

            if (!CaptchaTracker.Captchas.ContainsKey(token))
            {
                return new HttpResponse(StatusCode.BadRequest, "Not existing token");
            }

            var captcha = CaptchaTracker.Captchas[token];
            var post = CaptchaTracker.Posts[token];
            var guess = request.Content.FromB64();

            if (!captcha.CheckGuess(guess))
            {
                return new HttpResponse(StatusCode.BadRequest, "Wrong answer");
            }

            post = captcha.AddSignatureToThePost(post, guess);

            var post1 = new Post(post.Substring(0, 32), post.Substring(32).ToB64());

            if (_db.PutPost(post1))
            {
                return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post1));
            }

            CaptchaTracker.Captchas.Remove(token);
            CaptchaTracker.Posts.Remove(post);

            return new HttpResponse(StatusCode.BadRequest, "Unable to add post.");
        }
    }
}
