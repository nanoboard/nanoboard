using System;
using System.IO;
using Newtonsoft.Json;
using nboard;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace nbpack
{
    class Posts
    {
        [JsonProperty("posts")]
        public Post[] posts;
    }
}