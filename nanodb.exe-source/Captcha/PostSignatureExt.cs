using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using NDB;
using nboard;

namespace captcha
{
    static class PostSignatureExt
    {
        public static string ExceptSignature(this string post)
        {
            if (post.IndexOf("["+Captcha.SignatureTag+"=") == -1) return post;
            return post.Substring(0, post.LastIndexOf("["+Captcha.SignatureTag+"="));
        }

        public static byte[] Signature(this string post)
        {
            if (post.IndexOf("["+Captcha.SignatureTag+"=") == -1) return new byte[64];
            var offset = post.LastIndexOf("["+Captcha.SignatureTag+"=") + ("["+Captcha.SignatureTag+"=").Length;
            return post.Substring(offset).TrimEnd(']').Bytify();
        }
    }
    
}