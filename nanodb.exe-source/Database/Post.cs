using Newtonsoft.Json;
using NDB;

namespace NDB
{
    /*
        Post entity used by DB class and API handlers for read/write.
    */
    public class Post
    {
        [JsonProperty("hash")]
        public string hash;
        [JsonProperty("message")]
        public string message;      // is Base64 string of UTF-8 bytes
        [JsonProperty("replyTo")]
        public string replyto;

        public Post()
        {
        }

        /*
            r - replyTo hash,
            m - message
            hash is calculated inside this constructor
        */
        public Post(string r, string m)
        {
            replyto = r;
            message = m;
            hash = HashCalculator.Calculate(r + m.FromB64());
        }
    }
}
