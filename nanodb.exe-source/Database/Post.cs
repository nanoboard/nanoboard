using Newtonsoft.Json;
using NDB;

//from client 3.1
using System;
using System.Collections.Generic;

namespace NDB
{
    /*
        Post entity used by DB class and API handlers for read/write.
    */
    public class Post
    {
#pragma warning disable 0649
        [JsonProperty("hash")]
        public string hash;												//three			//can't
        [JsonProperty("message")]
        public string message;      // is Base64 string of UTF-8 bytes	//warnings		//fix
        [JsonProperty("replyTo")]
        public string replyto;											//here			//this
#pragma warning restore 0649

		//from client 3.1
        public DateTime date
        {
            get
            {
                DateTime res;
                try
                {
                    res = Convert.ToDateTime(message.FromB64().Split(new string[] { "[g]" }, StringSplitOptions.None)[1].
                        Split(new string[] { ", client:" }, StringSplitOptions.None)[0]);
                    
                }
                catch//(Exception e) //fix compile warning
                {
                    res = new DateTime();
                }
                return res;
            }
        }

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
