using Newtonsoft.Json;

namespace NDB
{
    /*
        It's a class that is used to serialize post DB entry.
        Such short strings are used to occupy less space in json.index file.
    */
    class DbPostRef
    {
        [JsonProperty("h")]
        public string hash;
        [JsonProperty("r")]
        public string replyTo; // hash of parent post
        [JsonProperty("o")]
        public int offset;   // offset in bytes from start of file (filename below)
        [JsonProperty("l")]
        public int length;   // length of the post message in bytes
        [JsonProperty("d")]
        public bool deleted;
        [JsonProperty("f")]
        public string file;  // filename, for example: 0.db
    }
}
