using Newtonsoft.Json;
using NDB;
using System.Collections.Generic;
using System;

namespace NDB
{
    /*
        Just a wrapper for the collection of DB index entries.
        To be serialized into index.json
    */
    class Index
    {
        [JsonProperty("indexes")]
        public DbPostRef[] indexes;
    }
}
