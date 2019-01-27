using Newtonsoft.Json;
using NDB;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text;

namespace NDB
{
    /*
        Reads array of posts from json file.
        Not used, probably will delete this.
    */
    class PostsReader
    {
        public Post[] Read(string pathToJson)
        {
            var json = File.ReadAllText(pathToJson);
            var posts = JsonConvert.DeserializeObject<Post[]>(json);
            return PostsValidator.Validate(posts);
        }
    }
}
