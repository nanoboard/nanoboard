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
//			Console.WriteLine("request.Content: "+request.Content);
			if(request.Content == null){
				return new HttpResponse(StatusCode.BadRequest, "GetCaptchaTokenHandler.cs. GetCaptchaTokenHandler. Handle. (request.Content == null): "+(request.Content == null));
			}

			if( Captcha.captcha_checked == false ){
				Captcha.IsCaptchaValid = Captcha.verify_captcha_hash();
//				Captcha.IsCaptchaValid = true;								//temporary disable verification of captcha-file hash.
				Captcha.captcha_checked = true;
				Console.Write("IsCaptchaValid? "+Captcha.IsCaptchaValid+". ");
				if( Captcha.IsCaptchaValid == true ){
					Console.Write("Captcha file ready.\n\n");
				}else{
					Console.WriteLine(
						"\n\nInvailid captcha! "+
						"You may download it here: \n\""+Captcha.captcha_downloading_url+"\"!\n"
					);
				}
			}

			if(Captcha.IsCaptchaValid==false)
			{
				return new HttpResponse(StatusCode.BadRequest, "Captcha file is not correct!\nYou may download it here: <a href=\""+Captcha.captcha_downloading_url+"\">"+Captcha.captcha_downloading_url+"</a>"+((Captcha.bypassValidation == true)?"<div style=\"display: none;\">bypassValidation = true, accepting posts without solved captcha enabled, but posts will be without pow and sign!</div>":"")); //append this text if bypassValidation is enabled, or don't append.
			}
            var token = Guid.NewGuid().ToString();
            CaptchaTracker.Posts[token] = Captcha.AddPow(request.Content.FromB64());
            var captcha = Captcha.GetCaptchaForPost(CaptchaTracker.Posts[token]);
            CaptchaTracker.Captchas[token] = captcha;
            return new HttpResponse(StatusCode.Ok, token);
        }
    }
}
