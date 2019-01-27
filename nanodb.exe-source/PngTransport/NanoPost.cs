using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nboard
{
    class NanoPost
    {
        private string _raw;

        public string SerializedString()
        {
            return _raw;
        }

        public byte[] SerializedBytes()
        {
            return Encoding.UTF8.GetBytes(_raw);
        }

        public NanoPost(string raw)
        {
            _raw = raw;
        }
    }
}