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
        [JsonProperty("hash")]
        public string hash;
        [JsonProperty("message")]
        public string message;
        [JsonProperty("replyTo")]
        public string replyto;
    }
}