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
    class CaptchaTracker
    {
        public static Dictionary<string, captcha.Captcha> Captchas = new Dictionary<string, captcha.Captcha>();
        public static Dictionary<string, string> Posts = new Dictionary<string, string>();
    }
}
