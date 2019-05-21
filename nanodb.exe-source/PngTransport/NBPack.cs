using System;
using System.IO;
using Newtonsoft.Json;
using nboard;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;	//regex to check dataURL
using System.Drawing.Imaging;		//ImageFormat.Png to save bmp as PNG...

namespace nbpack
{
    public class NBPackMain
    {
        public static NDB.PostDb PostDatabase;

        public static void Main_(string[] args)
        {
            if (!Directory.Exists("upload"))
                Directory.CreateDirectory("upload");
            if (!Directory.Exists("download"))
                Directory.CreateDirectory("download");
            if (!Directory.Exists("containers"))
                Directory.CreateDirectory("containers");

            if (args.Length <= 2)
            {
                Console.WriteLine(@"Simple tool that can pack/unpack png containers of 1.x nanoboard format,
also can talk to 2.0 server to: 
    1) feed posts from containers (download folder) to 2.0
    2) extract posts from 2.0 to form a container
This is the main intent of this tool - to help 2.0 to use transport 
compatible with 1.x nanoboard PNG containers format.

Example usages:
    nbpack -g http://127.0.0.1:7346 nano
        (creates PNG container using random picture from containers folder
         and puts result into upload folder)
    nbpack -a http://127.0.0.1:7346 nano
        (for each picture from download folder, tries to unpack it as a
         container, then sends it's posts to the 2.0 server and deletes it)
Other usages (may be useful if you're developing your own client):
    nbpack -v posts.json output.json                         (rewrite hashes)
    nbpack -p posts.json template.png crypto_key output.png  (pack container)
    nbpack -u container.png crypto_key output.json           (unpack container)
Sample JSON (note that message contains utf-8 BYTES converted to base64 string) 
 { ""posts"" : [ { ""hash"" : "".."", ""replyTo"" : "".."", ""message"" : ""base64"" }, .. ] }");
                return;
            }

            switch (args[0])
            {
                case "-g":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Insufficient parameters count");
                        return;
                    }
                    if (args.Length == 4){//-g + 3 agrs
						Create(args[1], args[2], args[3]);
					}
					else{
						Create(args[1], args[2], "");	//add empty string as third arg.
					}
                    break;
                case "-a":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Insufficient parameters count");
                        return;
                    }
                    AutoParse(args[1], args[2]);
                    break;
                case "-v": // validate
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Insufficient parameters count");
                        return;
                    }
                    Validate(args[1], args[2]);
                    break;
                case "-p": // pack
                    if (args.Length < 5)
                    {
                        Console.WriteLine("Insufficient parameters count");
                        return;
                    }
                    Pack(args[1], args[2], args[3], args[4]);
                break;
                case "-u": // unpack
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Insufficient parameters count");
                        return;
                    }else if(args.Length==3){
						//run Unpack with two parameters, from previous function, because this have type NDB.Post[], not void
						return;
					}
                    Unpack(args[1], args[2], args[3]); //void
                break;
            }
			return;
        }

		//int.Parse and Int32.Parse working bad for me.		See issue: https://github.com/nanoboard/nanoboard/issues/5
		//So this function was been writed, to make this code more independent...
        public static int parse_number(string string_number)//this function return (int)number from (string)"number". Negative numbers supporting too.
        {
			string test = (new Regex(@"\D")).Replace(string_number, "");
            int test_length = test.Length;
            int number = 0;
            for(int i = ((char)test[0]=='-')?1:0; i < test_length; i++){
                number += ((int)Char.GetNumericValue((char)test[i])*(int)Math.Pow(10,test_length-i-1));
			}
            number = ((char)test[0]=='-'?(0-number):(number));
            return number;
        }

		//check is base64 encoded.
		public static bool IsBase64Encoded(string str)
		{
			try
			{
				// If no exception is caught, then it is possibly a base64 encoded string
				byte[] data = Convert.FromBase64String(str);
				// The part that checks if the string was properly padded to the
				// correct length was borrowed from d@anish's solution
				return (str.Replace(" ","").Length % 4 == 0);
			}
			catch
			{
				// If exception is caught, then it is not a base64 encoded string
				return false;
			}
		}

        private static bool ByteCountUnder(List<NDB.Post> posts, int limit)
        {
            int byteCount = 0;

            foreach (var p in posts)
            {
                byteCount += Convert.FromBase64String(p.message).Length + 32;
                if (byteCount > limit) return false;
            }

            return true;
        }

        private static int ByteCount(NDB.Post p)
        {
            return Convert.FromBase64String(p.message).Length + 32;
        }

        /*
            Takes 50 or less last posts (up to 150000 bytes max total),
            adds  50 or less random posts (up to 150000 bytes max total),
            random is shifted towards latest posts.
        */
        private static void Create(string address, string key, string queue_image_params = "")
        {
			int from_last_posts = -1;
			int start_post_number = -1;
			string dataURL = "";
			string queue = "";															//string with hashes of posts, separated with comma.
			int from_queue = 0;
			int random_posts = -1;
			int max_bytelength = 150000;
			//Console.WriteLine("queue_image_params {0}", queue_image_params);
			
			var splitted = queue_image_params.Split('\n');

			for(int i=0;i<splitted.Length;i++){
				//Console.WriteLine("splitted[i] = {0}", splitted[i]);
				if(splitted[i]==""){
					//Console.WriteLine("Splitted[i] == \"\". Continue...");		//if empty string - continue. String is empty if '\n' at last of post-content.
					continue;
				}
				if(
						splitted[i].IndexOf("data:") != -1
					&& 	splitted[i].IndexOf("base64,")!= -1
					&&	nbpack.NBPackMain.IsBase64Encoded(splitted[i].Split(',')[1])
				){
					dataURL = splitted[i];
				}else if(splitted[i]=="No_dataURL_specified_for_source_image."){
					//Console.WriteLine("DataURL not specified...");
					dataURL = splitted[i];
				}else if(splitted[i].StartsWith("max_bytelength=")){
						//max_bytelength=
						int parsed_value = nbpack.NBPackMain.parse_number(splitted[i].Substring(14));
						max_bytelength = (parsed_value!=0) ? parsed_value : max_bytelength;
					//	Console.WriteLine(
					//		"Set max_bytelength = "+max_bytelength+", parsed_value = "+parsed_value+","
					//		+"\n(splitted[i]==\"\") = "+(splitted[i]=="")+", splitted[i] = "+splitted[i]
					//	);
				}else if(splitted[i].Length>=32){
					//Console.WriteLine("splitted.Length = 1, splitted[i] = {0}, maybe this is queue in GET-query...", splitted[i]);
					queue = splitted[i];
				}else if(splitted[i].Length<32){
					//Console.WriteLine("i = {0}, splitted[i] = {1}, splitted[i].Length = {2} < 32", i, splitted[i], splitted[i].Length);
					if(i==0){
						//Console.WriteLine("splitted[i], i==0, this is first value with length lesser than 32, maybe number = from_last_posts{0}", splitted[i]);
						if(splitted[i].IndexOf('-')!=-1){											//if range specified
							string [] from_to = splitted[i].Split('-');								//split this
							if(from_to.Length==2){													//if array length ==2 - set two numbers
								start_post_number = nbpack.NBPackMain.parse_number(from_to[0]);			//start post
								from_last_posts = nbpack.NBPackMain.parse_number(from_to[1]);			//posts count to add
							}else{																	//else
								Console.WriteLine(	"Negative numbers not supporting in parameters! \n"+							//maybe negative number was been sent
													"First parameter contains - and not a range: "+splitted[i]
								);
								return;																//error
							}
						}
						else{from_last_posts = nbpack.NBPackMain.parse_number(splitted[i]);}
					}else if (i==1){
						//Console.WriteLine("splitted[i], i>0, this is not first value with length lesser than 32, maybe number = from_queue{0}", splitted[i]);
						from_queue = nbpack.NBPackMain.parse_number(splitted[i]);
					}else/*if (i==2)*/{
						random_posts = nbpack.NBPackMain.parse_number(splitted[i]);
					}
				}
				//Console.WriteLine("End of iteration...");
			}
			//Console.WriteLine("End of cycle...");
			
			if(dataURL==""){//if string is null, this is empty string
				//Console.WriteLine("dataURL still empty...");
				dataURL="No_dataURL_specified_for_source_image.";
			}
			if(from_queue==0){from_queue = queue.Split(',').Length;}
			//Console.WriteLine("Queue {0}", queue);

			//Console.WriteLine(
			//	"from_last_posts = {0}\n"
			//	+"dataURL = {1}\n"
			//	+"queue = {2}\n"
			//	+"from_queue = {3}\n"
			//	,
			//	from_last_posts
			//	,dataURL.Substring(0, (	(dataURL.Length>40) ?40 :dataURL.Length )	)
			//	,queue
			//	,from_queue
			//);
			
			List<NDB.Post> list = new List<NDB.Post>();										//define empty list
			var count = 0;																	//define count variable

			int take = -1;																	//number of posts to taking (default value -1)
			if(																				//if queue hashes not defined here
					queue==null									//and this is default value, null
				|| 	queue == ""									//or if this is defined, but empty string
			){																			//pack last from_last_posts
					//Console.WriteLine("Queue is empty. Pack last {0} posts...", from_last_posts);					//Show message about this
				count = PostDatabase.GetPresentCount();											//get posts count (total posts)
				if(from_last_posts!=-1 && from_last_posts!=0){
					Console.WriteLine("Pack last {0} posts...", from_last_posts);
					take = from_last_posts;														//from_last_posts posts to taking
				}else if(from_last_posts==0){
					take = 0;
				}else{
					Console.WriteLine(	"Number of last posts to pack - not specified!\n"+
										"Pack last 50 posts, by default..."
					);
					take = 50;														//from_last_posts posts to taking
				}
				Console.WriteLine("start_post_number = "+start_post_number+", (count - take) = "+(count - take)+
				"\n( (start_post_number!=-1) ? start_post_number : (count - take) )"+( (start_post_number!=-1) ? start_post_number : (count - take) )+
				"\n(Math.Max( ( (start_post_number!=-1) ? start_post_number : (count - take) ), 0))"+Math.Max( ( (start_post_number!=-1) ? start_post_number : (count - take) ), 0)+
				"\ntake = "+take
				);
				var last50s = PostDatabase.RangePresent(Math.Max( ( (start_post_number!=-1) ? start_post_number : (count - take) ), 0), take);		//take last 50 posts
				list = last50s.ToList();														//push this to list.			
			}else{																			//if queue hashes defined
				
				var posts = PostDatabase.GetPosts(queue, from_queue);							//get array with posts
				//count = posts.Length;															//get post count in queue
				count = PostDatabase.GetPresentCount();											//get posts count in database (total posts)
					//Console.WriteLine("Queue accepted. Pack {0} posts from {1} posts in queue...", from_queue, count);	//show message about pack posts from queue
				list = posts.ToList();															//push posts to list
			}

			//select random container file
            var files = Directory.GetFiles("containers", "*.png").ToList();
            files.AddRange(Directory.GetFiles("containers", "*.jpg"));
            files.AddRange(Directory.GetFiles("containers", "*.jpeg"));

            if (files.Count == 0)
            {
                NServer.NotificationHandler.Instance.Messages.Enqueue("Your containers dir is empty! Add container(s)");
                return;
            }

            var r = new Random();

            var file = files[r.Next(files.Count)];
            var name = "upload/" + Guid.NewGuid().ToString() + ".png";

		//begin calculate capacity			
			Image bmp = null;
			if(dataURL=="No_dataURL_specified_for_source_image."){
				bmp = Bitmap.FromFile(file);
			}
			else if(
					dataURL.IndexOf("data:") != -1
				&& 	dataURL.IndexOf("base64,")!= -1
				&&	nbpack.NBPackMain.IsBase64Encoded(dataURL.Split(',')[1])
			){
				Console.WriteLine("Image uploaded and dataURL found. Create bitmap from dataURL.");
				
				//create bitmap from dataURL, and save this as PNG-file to Upload folder.
				var base64Data = Regex.Match(dataURL, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
				var binData = Convert.FromBase64String(base64Data);

				using (var stream = new MemoryStream(binData))
				{
					bmp = new Bitmap(stream);		//create image from dataURL
				}
			}else{
				Console.WriteLine("NBPack.cs - Create method: No DataURL found.");
            }
			var capacity = ((bmp.Width * bmp.Height * 3) >> 3) - 4;							//each bit in RGB subpixel byte - 32. Result = total bytes can be packed.
			capacity = (capacity>max_bytelength) ? capacity : max_bytelength;				//if capacity < 150000 bytes, resize image.
			
			//Console.WriteLine("bmp.Width = "+bmp.Width+", bmp.Height = "+bmp.Height+", capacity = "+capacity+", max_bytelength = "+max_bytelength);
			
			bmp.Dispose();
		//end calculate capacity


			int deleted_post_number = 0;
            while (!ByteCountUnder(list, capacity))		//remove old static limit, and check bytecount - up to capacity, to do not do resize image.
            {
				Console.WriteLine(
					"removed_post_hash: "+list[0].hash
					+"! Deleted post nubmer = "+(++deleted_post_number)
				);
                list.RemoveAt(0);
            }
			
            if(random_posts==-1){
				Console.WriteLine(	"Number of random posts to pack not specified!"+
									"Pack random 50 posts, by default..."
				);
				random_posts = 50;
			}else if(random_posts!=0){
				Console.WriteLine(	"Add {0} random posts to container", random_posts);				
			}else{
				//don't show nothing if 0 posts added.
			}
			
			//add random posts
            int rbytes = 0;
			for (int i = 0; i < random_posts; i++)
            {
                int index = (int)Math.Min(Math.Pow(r.NextDouble(), 0.3) * count, count - 1);
                var p = PostDatabase.RangePresent(index, 1)[0];
                var bc = ByteCount(p);
                if (rbytes + bc > capacity){
					Console.WriteLine("{0} posts selected from last {1}, according of bytelimit", i, random_posts);
					break;
                }
				rbytes += bc;
                if(!list.Contains(p)){list.Add(p);}
            }
			
			//Shuffle all elements in list - to anonymize container creator.
			list = list.OrderBy(a => Guid.NewGuid()).ToList();

			//pack from file or from specified dataURL
			if(dataURL=="No_dataURL_specified_for_source_image."){
				Pack(list.ToArray(), file, key, name);
			}else{
				Pack(list.ToArray(), dataURL, key, name);
			}
			
			List<string> packed_posts_hashes = new List<string>();
            foreach (var p in list)
            {
				packed_posts_hashes.Add(p.hash);
            }
			
			NServer.NotificationHandler.Instance.Messages.Enqueue("Saved PNG to /"	+	name);
			NServer.NotificationHandler.Instance.Messages.Enqueue("Hashes of posts, packed into "+name+": "	+ JsonConvert.SerializeObject(packed_posts_hashes));

			return;
        }

        public static void ParseFile(string address, string key, string filename, bool save_files=false)		//filename - is pathway for container file.
        {
			//Console.WriteLine("ParseFile: save_files = "+save_files);
            var posts = Unpack(filename, key);											//here catch for some files.

            GC.Collect();
            try
            {
                foreach (var p in posts)
                {
                    bool added = PostDatabase.PutPost(p);

                    if (added)
                    {
                        NServer.NotificationHandler.Instance.
                            Messages.Enqueue(	"[b][g]Extracted post:[/g][/b] "
											+	Encoding.UTF8.GetString(Convert.FromBase64String(p.message)));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("NBPack.cs: ParseFile. Try add posts - catch: "+e.Message);
            }

//			while(true){		//wanted to use infinite cycle with repeat this... fail, because validate();
			if(!save_files){
				try
				{
					File.Delete(filename);
					//Console.WriteLine("Delete: "+filename);
//					break;
				}
				catch(Exception e)
				{
					Console.WriteLine("NBPack.cs: ParseFile(pathway). Try to delete file - catch: "+e.Message);	//from here is error message for some files.
					//System.Threading.Thread.Sleep(10);
				}
			}
			return;
        }
		
		public static bool bypassValidation = !captcha.Captcha.captcha_found;

        public static void ParseFile(string address, string key, Image container)				//here Image from RAM
        {
            var posts = Unpack(container, key);													//here can be catch for some Images

            GC.Collect();
            try
            {
				int posts_added = 0;
                foreach (var p in posts)
                {
					//if(bypassValidation){ Console.WriteLine("Captcha file not found. bypassValidation = "+bypassValidation+"now..."); }
                    bool added = false;
					try{
						added = PostDatabase.PutPost(p, true, bypassValidation);
					}catch(Exception ex){
						Console.WriteLine("Try to PutPost: "+ex);
					}

                    if (added)
                    {
					/*
						try{
							NServer.NotificationHandler.Instance.
								Messages.Enqueue(
									//"[b][g]Extracted post:[/g][/b] "
									//+	Encoding.UTF8.GetString(Convert.FromBase64String(p.message))	//display post

									"[b][g]Extracted post:[/g][/b] "
									+	p.hash															//only hash
								);
						}
						catch(Exception ex){	//here sometimes catch srcIndex for fast collect
							Console.WriteLine("Try to add notification: "+ex + "\nstring.Length" + Encoding.UTF8.GetString(Convert.FromBase64String(p.message)).Length);
						}
					*/
						posts_added++;
                    }
/*
				//or don't display posts and just display added posts:
					try{
						NServer.NotificationHandler.Instance.
							Messages.Enqueue(
										"[b][g]Contains posts:[/g][/b] "			//Display contains posts
									+	posts.Length +								//posts in container
										". Added: "
							//		+	posts_added									//added posts
							//		+	"posts."
							
							//or this notification
									+
									(
										(posts_added==posts.Length)					//if all posts added
											? "All"									//display "All"
											:	posts_added +						//or display added posts
												", Not added: " +					//and not added
												(posts.Length - posts_added)		//value, then.
												+(
													( ( posts.Length - posts_added ) != 0 )
														? " (maybe already exist)"
														: ""
												)
									)
							);
					}
					catch(Exception ex){
						Console.WriteLine("Try to add notification: "+ex);	//sometimes error: srcIndex
					}
*/
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("NBPack.cs: ParseFile(Image RAM) - try to add posts: "+e.Message);	//here catch "OutOfMemoryException" for notifs.
            }
			return;
        }

        private static void AutoParse(string address, string key)
        {
            var files = Directory.GetFiles("download");

            foreach (var f in files)
            {
                var posts = Unpack(f, key);
                GC.Collect();
                try
                {
                    foreach (var p in posts)
                    {
                        PostDatabase.PutPost(p);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                try
                {
                    File.Delete(f);
                }

                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
			return;
        }

        private static void Validate(NDB.Post[] posts)
        {
            foreach (var p in posts)
            {
                p.hash = HashCalculator.Calculate(p.replyto + Encoding.UTF8.GetString(Convert.FromBase64String(p.message)));
            }
			return;
        }

        private static void Validate(string postsPath, string outputPath)
        {
            var json = File.ReadAllText(postsPath);
            var posts = JsonConvert.DeserializeObject<NDB.Post[]>(json);
            Validate(posts);
            var result = JsonConvert.SerializeObject(posts, Formatting.Indented);
            File.WriteAllText(outputPath, result);
			return;
        }

        private static void Pack(NDB.Post[] posts, string templatePath, string key, string outputPath)
        {
            var @set = new HashSet<string>();

            Validate(posts);
            var nposts = new List<NanoPost>();

            NServer.NotificationHandler.Instance.Messages.Enqueue("Showing posts that will go to the container:");

            foreach (var p in posts)
            {
                var mess = Encoding.UTF8.GetString(Convert.FromBase64String(p.message));
                var hash = p.hash;

                if (!@set.Contains(hash))
                {
                    @set.Add(hash);
                    NServer.NotificationHandler.Instance.Messages.Enqueue(mess);
                }

                nposts.Add(new NanoPost(p.replyto + mess));
            }
            var packed = NanoPostPackUtil.Pack(nposts.ToArray());
            var encrypted = ByteEncryptionUtil.EncryptSalsa20(packed, key);

			Image bmp;
			if(
					templatePath.IndexOf("data:") != -1
				&& 	templatePath.IndexOf("base64,")!= -1
				&&	nbpack.NBPackMain.IsBase64Encoded(templatePath.Split(',')[1])
			){
				Console.WriteLine("Image uploaded and dataURL found. Create bitmap from dataURL.");
				
				//create bitmap from dataURL, and save this as PNG-file to Upload folder.
				var base64Data = Regex.Match(templatePath, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
				var binData = Convert.FromBase64String(base64Data);

				using (var stream = new MemoryStream(binData))
				{
					bmp = new Bitmap(stream);		//create image from dataURL

						//save this image as PNG-file to the folder "upload"
					//Console.WriteLine(bmp);
					//bmp.Save("upload/" + Guid.NewGuid().ToString() + ".png", ImageFormat.Png);
					//bmp.Dispose();
					//Console.WriteLine("saved to \"upload\"");
				}
				//working...
			}else{
				//Console.WriteLine("DataURL not found. Create bitmap from templatePath = {0}", templatePath);
				bmp = Bitmap.FromFile(templatePath);
            }
			var capacity = (bmp.Width * bmp.Height * 3) / 8 - 32;

            if (encrypted.Length > capacity)
            {
                float scale = (encrypted.Length / (float)capacity);
                Console.WriteLine("Warning: scaling image to increase capacity: " + scale.ToString("n2") + "x");
                scale = (float)Math.Sqrt(scale);
                bmp = new Bitmap(bmp, (int) (bmp.Width * scale + 1), (int) (bmp.Height * scale + 1));
            }

            new PngStegoUtil().HideBytesInPng(bmp, outputPath, encrypted);
			return;
        }

        private static void Pack(string postsPath, string templatePath, string key, string outputPath)
        {
            var json = File.ReadAllText(postsPath);
            var posts = JsonConvert.DeserializeObject<NDB.Post[]>(json);
            Validate(posts);
            var nposts = new List<NanoPost>();
            foreach (var p in posts)
            {
                nposts.Add(new NanoPost(p.replyto + Encoding.UTF8.GetString(Convert.FromBase64String(p.message))));
            }
            var packed = NanoPostPackUtil.Pack(nposts.ToArray());
            var encrypted = ByteEncryptionUtil.EncryptSalsa20(packed, key);

			Image bmp;
			if(
					templatePath.IndexOf("data:") != -1
				&&	templatePath.IndexOf("base64,")!= -1
				&&	nbpack.NBPackMain.IsBase64Encoded(templatePath.Split(',')[1])
			){
				Console.WriteLine("Image uploaded and dataURL found. Create bitmap from dataURL.");
				
				//create bitmap from dataURL, and save this as PNG-file to Upload folder.
				var base64Data = Regex.Match(templatePath, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
				var binData = Convert.FromBase64String(base64Data);

				using (var stream = new MemoryStream(binData))
				{
					bmp = new Bitmap(stream);		//create image from dataURL

					//save this image as PNG-file to the folder "upload"
					//Console.WriteLine(bmp);
					//bmp.Save("upload/" + Guid.NewGuid().ToString() + ".png", ImageFormat.Png);
					//bmp.Dispose();
					//Console.WriteLine("saved to \"upload\"");
				}
				//working...
			}else{
				Console.WriteLine("DataURL not found. Create bitmap from templatePath = {0}", templatePath);
				bmp = Bitmap.FromFile(templatePath);
            }
			
            var capacity = (bmp.Width * bmp.Height * 3) / 8 - 32;

            if (encrypted.Length > capacity)
            {
                float scale = (encrypted.Length / (float)capacity);
                Console.WriteLine("Warning: scaling image to increase capacity: " + scale.ToString("n2") + "x");
                scale = (float)Math.Sqrt(scale);
                bmp = new Bitmap(bmp, (int) (bmp.Width * scale + 1), (int) (bmp.Height * scale + 1));
            }

            new PngStegoUtil().HideBytesInPng(bmp, outputPath, encrypted);
			return;
        }
		//GET-query to http://127.0.0.1:7346/api/run-nbpack/-u|upload%2FMORE-secrets-inside.png|nano3
		//or POST-query to http://127.0.0.1:7346/api/run-nbpack/ with three two parameters - return JSON.
		//dataURL of PNG is supporting in containerPath.
        public static NDB.Post[] Unpack(string containerPath, string key)				//containerPath - pathway to container, as file
        {
            try
            {
				//Console.WriteLine("Start read hidden bytes");
                var encrypted = new PngStegoUtil().ReadHiddenBytesFromPng(containerPath);
				//Console.WriteLine("readhiddenbytesFromPNG..........");
                var decrypted = ByteEncryptionUtil.DecryptSalsa20(encrypted, key);		//here is reason of catch for some files
				//Console.WriteLine("decrypted...........");
                var nposts = NanoPostPackUtil.Unpack(decrypted);						//or here
				//Console.WriteLine("nposts...........");
                var posts = nposts.Select(												//or here
                    np => new NDB.Post
                    {
                        replyto = np.SerializedString().Substring(0, 32),
                        message = Convert.ToBase64String(Encoding.UTF8.GetBytes(np.SerializedString().Substring(32)))
                    }).ToArray();
				//Console.WriteLine("posts...........");
                Validate(posts);														//in the most cases this not working, then catch
				//Console.WriteLine("Validate...........");
                return posts;
            }
            catch (Exception e)
            {
				Console.WriteLine("Unpack catch: "+containerPath);										//here catch for some files.
                return new NDB.Post[0];
            }
        }

        public static NDB.Post[] Unpack(Image container, string key)					//this Unpack using Image from RAM, as parameter.
        {
            try
            {
				//Console.WriteLine("Start read hidden bytes");
                var encrypted = new PngStegoUtil().ReadHiddenBytesFromPng(container);	//here can be catch
				//Console.WriteLine("readhiddenbytesFromPNG..........");
                var decrypted = ByteEncryptionUtil.DecryptSalsa20(encrypted, key);		//here can be catch
				//Console.WriteLine("decrypted...........");
                var nposts = NanoPostPackUtil.Unpack(decrypted);						//here can be catch
				//Console.WriteLine("nposts...........");
                var posts = nposts.Select(												//here can be catch
                    np => new NDB.Post
                    {
                        replyto = np.SerializedString().Substring(0, 32),
                        message = Convert.ToBase64String(Encoding.UTF8.GetBytes(np.SerializedString().Substring(32)))
                    }).ToArray();
				//Console.WriteLine("posts...........");
                Validate(posts);														//AND HERE reason of for lagging containers
				//Console.WriteLine("Validate...........");
                return posts;
            }
            catch (Exception e)
            {
				//Console.WriteLine("Unpack catch");									//here can be catch.
                return new NDB.Post[0];
            }
        }
		

		//GET-query to http://127.0.0.1:7346/api/run-nbpack/-u|upload%2FMORE-secrets-inside.png|nano3|upload%2Fposts.json
		//or POST-query to http://127.0.0.1:7346/api/run-nbpack/ with four parameters - saving the JSON-file.
		//dataURL of PNG is supporting in containerPath.
        private static void Unpack(string containerPath, string key, string outputPath)
        {
            try
            {
                var encrypted = new PngStegoUtil().ReadHiddenBytesFromPng(containerPath);
                var decrypted = ByteEncryptionUtil.DecryptSalsa20(encrypted, key);
                var nposts = NanoPostPackUtil.Unpack(decrypted);
                var posts = nposts.Select(
                    np => new NDB.Post
                    {
                        replyto = np.SerializedString().Substring(0, 32),
                        message = Convert.ToBase64String(Encoding.UTF8.GetBytes(np.SerializedString().Substring(32)))
                    }).ToArray();
                Validate(posts);
				
				//from the sources of client 3.1
                for (int i = 0; i < posts.Length; i++)
                    posts[i].message = NDB.PostsValidator.FromB64(posts[i].message);
                
				var result = JsonConvert.SerializeObject(posts, Formatting.Indented);
                File.WriteAllText(outputPath, result);
            }
            catch
            {
                var posts = new Posts();
                posts.posts = new Post[0];
                var result = JsonConvert.SerializeObject(posts);
                File.WriteAllText(outputPath, result);
            }
			return;
        }
    }
}