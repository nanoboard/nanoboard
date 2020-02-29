using System;
using System.IO;
using Newtonsoft.Json;
using nboard;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace nbpack
{
    class Post
    {
#pragma warning disable 0649
        [JsonProperty("hash")]
        public string hash;
        [JsonProperty("message")]
        public string message;
        [JsonProperty("replyTo")]
        public string replyto;
#pragma warning restore 0649
    }
}