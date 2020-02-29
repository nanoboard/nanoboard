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
	[System.Runtime.InteropServices.ComVisible(true)]
	public interface ICloneable
	{
		object Clone();	//Need to Clone Ndb.Post (Post). See comment at line 95.
	}
	
    public class Post : ICloneable
    {
#pragma warning disable 0649
        [JsonProperty("hash")]
        public string hash;												//three			//can't
        [JsonProperty("message")]
        public string message;      // is Base64 string of UTF-8 bytes	//warnings		//fix
        [JsonProperty("replyTo")]
        public string replyto;											//here			//this
#pragma warning restore 0649

		object _lock = new object();	//lock datetime_result
		//from client 3.1
        public DateTime date
        {
            get	//get date and time from [g][/g] tag in the message of the post.
            {
				lock(_lock){	//_lock datetime_result value
					DateTime datetime_result = new DateTime();					//new DateTime() == DateTime.Parse("01.01.0001 0:00:00")

					string[] splitted =
								message								//get post message
								.Replace("post_was_deleted", "")	//remove "post_was_deleted", if this was been "deleted_once"
//								.Replace("post_is_reported", "")	//remove "post_is_reported", if this was been "reported"
								.FromB64()							//decode base64-encoded post-message to string
								.Split(								//split this string
									new string[] { "[g]" },			//by "[g]"
									StringSplitOptions.None
								);
					;

					if(splitted.Length>1){							//if "[g]" was been found and splitted-array length > 1
						splitted = splitted[1]						//continue to split the second part splitted[1]
							.Split(									//split it
								new string[] { ", client:" },		//by ", client: "
								StringSplitOptions.None
							);
					}else{											//else if "[g]" not found
						return datetime_result;										//return default date-time
					}
					
					//[g]Mon, 11/Feb/2019, 20:35:20.535 (Europe/Helsinki), client: karasiq-nanoboard v1.3.2[/g]
					if(splitted[0].Contains("(")){	//DateTime.TryParse("Mon, 11/Feb/2019, 20:35:20.535 (Europe/Helsinki)", out datetime_result); //not working
						splitted = splitted[0].Split('(');	//DateTime.TryParse("Mon, 11/Feb/2019, 20:35:20.535", out datetime_result); // return 11.02.2019 20:35:20 - working
					}

					try{
						DateTime.TryParse(splitted[0], out datetime_result);	//after all, in splitted[0] must to be DateTime.
																				//Try to parse this and save DateTime in datetime_result.
					}
					catch(Exception ex){
						Console.WriteLine("Post.cs. date. Post hash: "+hash+", post message: "+message.Substring(0, 100)+", Exception: "+ex);
					}
					
					return datetime_result; //return parsed DateTime or just return default.
				}
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
		
		/**
			Need to clone Post,
			because when
			Post newPost = Post oldPost;
			newPost.message += "post was deleted"+newPost.message; //oldPost.message changing too.
		*/
		public object Clone()
		{
			return this.MemberwiseClone();	//now, clonning is available by command: Post newPost = (NDB.Post)oldPost.Clone();
		}
    }
}
