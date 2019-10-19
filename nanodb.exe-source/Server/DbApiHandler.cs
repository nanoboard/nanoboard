using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using NDB;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using nboard;
using nbpack;
using System.Drawing.Imaging;		//make ImageFormat.Png working, to save bmp as PNG...
using fractalgen;

namespace NServer
{
    /*
        Gives access to various DB and server functions, mainly related to reading/writing posts.
    */
    class DbApiHandler : IRequestHandler
    {
        private PostDb _db;
        private Dictionary<string, Func<string,string,HttpResponse>> _handlers;

        public DbApiHandler(PostDb db)
        {
            _db = db;
            _handlers = new Dictionary<string, Func<string, string, HttpResponse>>();
            // filling handlers dictionary with actions that will be called with (request address, request content) args:
            _handlers["get"] = GetPostByHash;
            _handlers["getlastn"] = GetLastNPosts;	//from client 3.1	//get last n posts in thread, or last 3 threads in category, to show this.
            _handlers["delete"] = DeletePost;
            _handlers["add"] = AddPost;
			_handlers["download-posts"] = Download_Posts;		//download posts by url, from another nanodb server.
			_handlers["upload-posts"] = Upload_Posts; 			//upload posts to another nanodb server, like Bitmessage-retranslation.
            _handlers["addmany"] = AddPosts;
            _handlers["readd"] = ReAddPost;
            _handlers["replies"] = GetReplies;
			
			//get posts by list with hashes.
			//posts can be parsed from responce
			//GET-query (bytesize limit): http://127.0.0.1:7346/api/getposts/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
			//POST-query: http://127.0.0.1:7346/api/getposts/POST/ with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
            _handlers["getposts"] = GetPosts;		//get list of specified posts. Return array with posts in JSON.
			
            _handlers["count"] = GetPostCount;
            _handlers["nget"] = GetNthPost;
			
			//GET: http://127.0.0.1:7346/api/prange/?fromto=0-20 							- JSON response with array of posts
			//POST: http://127.0.0.1:7346/api/prange/0-20/									- array with posts
			//GET: http://127.0.0.1:7346/api/prange/?fromto=0-20&only_hashes=only_hashes	- array with hashes only.
			//POST: http://127.0.0.1:7346/api/prange/0-20/	string="only_hashes"			- array with hashes only.
            _handlers["prange"] = GetPresentRange;
			
            _handlers["pcount"] = GetPresentCount;
            _handlers["search"] = Search;
            _handlers["paramset"] = ParamSet;
            _handlers["paramget"] = ParamGet;
            _handlers["params"] = Params;
            _handlers["find-thread"] = FindThread;
            _handlers["threadsize"] = ThreadSize;
			
			//GET and POST parameters available. http://127.0.0.1:7346/api/getposts/POST/OnlyRAM|save_files|10 (last value - is limit for connectoins)
            _handlers["png-collect"] = PngCollect;
			_handlers["download-png"] = DownloadPNG;
			
			
			//posts from queue can be packed, using link:
			//http://127.0.0.1:7346/api/png-create/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
			//or using POST-query to http://127.0.0.1:7346/api/png-create/
			//with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
            _handlers["png-create"] = PngCreate;	//create png with post from queue or not (depending from parameters).
			
            _handlers["run-nbpack"] = Run_nbpack_with_params;	//Run NBPack.cs with parameters, specified in url.
																//URL must contains parameters, joined with comma, after EncodeURIComponent();
			_handlers["convert-to-PNG"] = ConvertToPNG;	//Upload custom picture, convert this to PNG and save in the folder "containers"
            _handlers["png-collect-avail"] = (a,b)=>new HttpResponse(_collectAvail ? StatusCode.Ok : StatusCode.NotFound, _collectAvail ? "Finished." : "Collect...");
            _handlers["png-create-avail"] = (a,b)=>new HttpResponse(_createAvail ? StatusCode.Ok : StatusCode.NotFound, _createAvail ? "Finished." : "Creating PNG...");
        }

        private string SearchUp(string hash, string categoriesHash, string previousHash)
        {
            var p = _db.GetPost(hash);

            if (p == null)
            {
                return null;
            }

            if (p.replyto == categoriesHash)
            {
                return previousHash;
            }

            previousHash = p.hash;
            hash = p.replyto;
            return SearchUp(hash, categoriesHash, previousHash);
        }

        private HttpResponse FindThread(string postHash, string categoriesHash)
        {
            var prev = postHash;
            var result = SearchUp(postHash, categoriesHash, prev);
            return new HttpResponse(result == null ? StatusCode.NotFound : StatusCode.Ok, result ?? "");
        }

        private HttpResponse DownloadPNG(string GET = "", string POST = "") //default queue value is null
        {
			string params_ = "";		//string with queue_and_image, joined with comma. Default value is null, and this string is empty.
			if(POST==""){				//if string with second value is empty
				params_ = GET;			//then get queue_and_image from get query
			}else{						//else, if contens was been send, using POST-query
				params_ = POST;			//get queue_and_image from second parameter
			}
			string [] p = params_.Split('|');
			var obj_Aggregator = new Aggregator(new string [] {p[0], "collect_using_RAM", "save_files", "1" /*max_connections*/});
            return new HttpResponse(StatusCode.Ok, obj_Aggregator.DownloadPNG(p));
        }

        private bool _collectAvail = true;
        private bool _createAvail = true;

        private HttpResponse PngCollect(string GET="", string POST = "")
        {
			string parameters = "";
			if(POST==""){				//if string with second value is empty
				parameters = Uri.UnescapeDataString(GET);			//then get queue_and_image from get query
			}else{						//else, if contens was been send, using POST-query
				parameters = Uri.UnescapeDataString(POST);			//get queue_and_image from second parameter
			}
			Console.WriteLine("parameters: " + parameters);
			string [] _params_ = (parameters!="") ? parameters.Split('|') : new string[0];
			
            _collectAvail = false;
            AggregatorMain.Run(_params_);
            ThreadPool.QueueUserWorkItem(o => 
            {
                while(AggregatorMain.Running) 
                {
                    Thread.Sleep(1000);
                }

                _collectAvail = true;
            });
            return new HttpResponse(StatusCode.Ok, "");
        }

        /*
        private void ParseContainers()
        {
            NBPackMain.Main_(new []{"-a", 
                "http://" 
                + Configurator.Instance.GetValue("ip", "127.0.0.1") 
                + ":"
                + Configurator.Instance.GetValue("port", "7346"),
                Configurator.Instance.GetValue("password", "nano")});
        }
        */

		//params in one string, joined with comma, after JavaScript EncodeURIComponent().
		//See NBPack.cs guide, about it.
        private HttpResponse Run_nbpack_with_params(string GET="", string POST = "")
        {
			//Console.WriteLine("GET {0}, POST {1}", GET, POST);
			string parameters = "";		//string with queue_and_image, joined with comma. Default value is null, and this string is empty.
			
			if(POST==""){				//if string with second value is empty
				parameters = Uri.UnescapeDataString(GET);			//then get queue_and_image from get query
			}else{						//else, if contens was been send, using POST-query
				parameters = Uri.UnescapeDataString(POST);			//get queue_and_image from second parameter
			}
			
			string[] p = parameters.Split('|');
			Console.Write("Start: nbpack");
			for(int i=0; i<p.Length;i++){
				if(																					//if dataURL
							p[i].IndexOf("data:") != -1
						&& 	p[i].IndexOf("base64,")!= -1
						&&	nbpack.NBPackMain.IsBase64Encoded(p[i].Split(',')[1])
				){
					Console.Write(" {0}", p[i].Substring(0, 40) + "...");
				}else{
					Console.Write(" {0}", p[i]);
				}
			}
			Console.Write("\n");
			
			_createAvail = false;
			ThreadPool.QueueUserWorkItem(o => 
			{
				NBPackMain.Main_(p);
				_createAvail = true;
			});
			
			string status;
			if(p.Length<=2)	{status = 	"Specify parameters, after encodeURIcomponent, in the end of URL.\n" + 
										"Example: http://127.0.0.1:7346/api/run-nbpack/-u%2Cdownload%2Fcontainer_PNG.png%2Cnano3%2Cdownload%2Fcontainer_posts.json";}
			else if(p[0]=="-u" && p.Length == 3){
				status = JsonConvert.SerializeObject(nbpack.NBPackMain.Unpack(p[1], p[2]));	//return posts in JSON-response.
				//e.g.: http://127.0.0.1:7346/api/run-nbpack/-u%2Cdownload%2Fcontainer_PNG.png%2Cnano3
			}
			else{status = "OK";}
			Console.WriteLine("Done.");
			return new HttpResponse(StatusCode.Ok, status);
        }

		//posts from queue can be packed, using link:
		//http://127.0.0.1:7346/api/png-create/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
		//or using POST-query to http://127.0.0.1:7346/api/png-create/
		//with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
        private HttpResponse PngCreate(string GET = "", string POST = "") //default queue value is null
        {
			string queue_and_image = "";		//string with queue_and_image, joined with comma. Default value is null, and this string is empty.
			
			if(POST==""){				//if string with second value is empty
				queue_and_image = GET;			//then get queue_and_image from get query
			}else{						//else, if contens was been send, using POST-query
				queue_and_image = POST;			//get queue_and_image from second parameter
			}

            _createAvail = false;
            ThreadPool.QueueUserWorkItem(o => 
            {
                NBPackMain.Main_(new []
                {"-g", 
                    "http://"
                    + Configurator.Instance.GetValue("ip", "127.0.0.1")
                    + ":"
                    + Configurator.Instance.GetValue("port", "7346"),
                    Configurator.Instance.GetValue("password", Configurator.DefaultPass),
					queue_and_image	//add queue_and_image as parameter
                });
                _createAvail = true;
            });
			
			while(_createAvail!=true)
				Thread.Sleep(1000);
				
			return new HttpResponse(StatusCode.Ok, "PNG successfully created.");
			
        }

		private HttpResponse generate_PNG(string query)//?generate,1920,1080
		{
			//Console.WriteLine("args[0] = {0}, == \"\"", args[0], (args[0]==""));
			int width = 0; int height = 0; int bitlength = 0;
			string pathway = "";
			
			string [] splitted;
			splitted = query.Split('|');
			string error = 	"Too many parameters.\nUse comma, as delimiter" + 
							"\"../api/convert-to-PNG/?generate_PNG,PNG_WIDTH,PNG_HEIGHT\"," + 
							"or \"../api/convert-to-PNG/generate_PNG/generate_PNG,bitlength_to_pack\"";
			if(splitted.Length>5){
				return new HttpResponse(StatusCode.BadRequest, error);
			}
			else if(splitted.Length==5 && splitted[1]=="fractalgen"){
				string fractalgen_result = fractalgen.Program.Main_(new string[]{splitted[2], splitted[3], splitted[4]});
				return new HttpResponse(StatusCode.Ok, fractalgen_result);
			}
			else if(splitted.Length==4 && splitted[1]=="fractalgen"){
				string fractalgen_result = fractalgen.Program.Main_(new string[]{splitted[2], splitted[3]});
				return new HttpResponse(StatusCode.Ok, fractalgen_result);
			}
			else if(splitted.Length==3 && splitted[1]=="fractalgen"){
				string fractalgen_result = fractalgen.Program.Main_(new string[]{splitted[2]});
				return new HttpResponse(StatusCode.Ok, fractalgen_result);
			}
			else if(splitted.Length==2 && splitted[1]=="fractalgen"){
				string fractalgen_result = fractalgen.Program.Main_(new string[]{});
				return new HttpResponse(StatusCode.Ok, fractalgen_result);
			}
			else if(splitted.Length==4){pathway = splitted[1]; width=nbpack.NBPackMain.parse_number(splitted[2]); height=nbpack.NBPackMain.parse_number(splitted[3]);}
			else if(splitted.Length==3){width=nbpack.NBPackMain.parse_number(splitted[1]); height=nbpack.NBPackMain.parse_number(splitted[2]);}
			else if(splitted.Length==2){
				bitlength = nbpack.NBPackMain.parse_number(splitted[1]);
				int side = 0;
				if(bitlength!=0){
					side = (int)Math.Ceiling((decimal)Math.Sqrt(((bitlength*8)/3)+32));	//side of square, from bitlength of message to pack
					width = side; height = side;
				}
			}else{
				return new HttpResponse(StatusCode.Ok, ("Specify PNG size to generate.\nUse comma, as delimiter: \n" + 
														"../api/convert-to-PNG/generate_PNG,PNG_WIDTH,PNG_HEIGHT\"," + 
														"or ../api/convert-to-PNG/?generate_PNG,bitlength_to_pack")
				);
			}
			Console.WriteLine("Start generate PNG: width = " + width + ", heigth = " + height + ", bitlength = " + bitlength);
			
			//after width and height values are defined...
			Bitmap bmp = new Bitmap(width, height);						//bitmap
			Random rand = new Random();									//random number
			for (int y = 0; y < height; y++)							//create random pixels
			{
				for (int x = 0; x < width; x++)
				{
					//int a = rand.Next(256);							//generate random ARGB value
					int r = rand.Next(256);								//Just generate RGB
					int g = rand.Next(256);
					int b = rand.Next(256);

					//bmp.SetPixel(x, y, Color.FromArgb(a, r, g, b));	//set ARGB value
					bmp.SetPixel(x, y, Color.FromArgb(r, g, b));		//Just RGB.
				}
			}
			var random_guid = Guid.NewGuid().ToString();
			bmp.Save( pathway + Path.DirectorySeparatorChar + random_guid + ".png", ImageFormat.Png);					//png ImageFormat
			return new HttpResponse(StatusCode.Ok, ("Random PNG generated successfully. Width = " + width + ", height = " + height + ".\n" +
													"Generated image saved to: \\containers\\" + random_guid + ".png"));
		}
		
		private HttpResponse ConvertToPNG(string dataURL_GET = "", string dataURL_POST = "") //default queue value is null
        {
			//Console.WriteLine("first {0}, second {1}", dataURL_GET.Substring(0, (dataURL_GET.Length>40)?40:dataURL_GET.Length), dataURL_POST.Substring(0, (dataURL_POST.Length>40)?40:dataURL_POST.Length));
//test split string
//input string with parameters for png-create
//output - splitted and extracted values...
			int from_last_posts = 0;
			string dataURL = "";
			string queue = "";															//string with hashes of posts, separated with comma.
			int from_queue = 0;

			string queue_image_params = "";
			if(			dataURL_GET.Trim()=="" 		&& 	dataURL_POST.Trim()==""		)	{queue_image_params = "";}
			else if(	dataURL_POST.Trim()=="" 	&& 	dataURL_GET.Trim()!=""		)	{queue_image_params = dataURL_GET;}
			else if(	dataURL_POST.Trim()!="" 	&& 	dataURL_GET.Trim()==""		)	{queue_image_params = dataURL_POST;}
			else if(	dataURL_POST.Trim()!="" 	&& 	dataURL_GET.Trim()!=""		)	{
				if(			dataURL_GET.IndexOf("%2F")	!=	-1 || dataURL_GET.IndexOf("%2f") != -1	)	{queue_image_params = dataURL_GET;}
				else if(
							dataURL_POST.IndexOf("%2F")	!=	-1 || dataURL_POST.IndexOf("%2f") != -1 ||
							dataURL_POST.IndexOf("\n")	!=	-1 || dataURL_POST.IndexOf("%2Fn")!=-1 	||
							dataURL_POST.IndexOf("%2fn")!=-1
				)																						{queue_image_params = dataURL_POST;}
				else	{
							//Console.WriteLine("ConvertToPNG: 1. another case...");
				}
			}else	{
						//Console.WriteLine("ConvertToPNG: 2. another case...{0}");
			}

																						//if string send using GET-query this must to be escaped.
			queue_image_params = Uri.UnescapeDataString(queue_image_params); 			//Unescape this. "%2F" or "%2f" replaced to "/".
			
			if(queue_image_params.IndexOf("generate_PNG")!=-1){return generate_PNG((string)queue_image_params);}
			
			var splitted = queue_image_params.Split('|');

			for(int i=0;i<splitted.Length;i++){
				if(splitted[i]==""){continue;}						//if empty string - continue. String is empty if '\n' at last of post-content.
				if(		splitted[i].IndexOf("data:") != -1
					&& 	splitted[i].IndexOf("base64,")!= -1
					&& 	nbpack.NBPackMain.IsBase64Encoded(splitted[i].Split(',')[1])
				)	{dataURL = splitted[i];}		//if dataURL specified
				else if(splitted[i]=="No_dataURL_specified_for_source_image.")					{dataURL = splitted[i];}		//if dataURL not specified
				else if(splitted[i].Length>=32)													{queue = splitted[i];}			//if not a number, seems like a queue string
				else if(splitted[i].Length<32){																					//if lesser than 32 symbols, this is not hash(es) of posts
					if(i==0)	{from_last_posts = nbpack.NBPackMain.parse_number(splitted[i]);}		//for first value this variable
					else		{from_queue = nbpack.NBPackMain.parse_number(splitted[i]);}			//or this
				}
			}

			if(dataURL==""){dataURL="No_dataURL_specified_for_source_image";}		//if string is null, this is empty string
			if(queue.Trim().Length == 0){queue = "";}								//if trimmed queue value length == 0, leave this empty

			//Console.WriteLine(
			//	"from_last_posts = {0}\n"
			//	 + "dataURL = {1}\n"
			//	 + "queue = {2}\n"
			//	 + "from_queue = {3}\n"
			//	,
			//	from_last_posts
			//	,dataURL.Substring(0, (	(dataURL.Length>40) ?40: dataURL.Length )	)
			//	,queue
			//	,from_queue
			//);
			
			if(
					dataURL.IndexOf("data:") != -1
				&& dataURL.IndexOf("base64,") != -1
				&& nbpack.NBPackMain.IsBase64Encoded(dataURL.Split(',')[1])
			){
				Console.WriteLine("Custom image uploaded and dataURL found.");
				//create bitmap from dataURL, and save this as PNG-file to Upload folder.
				var base64Data = Regex.Match(dataURL, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
				var binData = Convert.FromBase64String(base64Data);

				string random_guid = Guid.NewGuid().ToString();
				using (var stream = new MemoryStream(binData))
				{
					Image bmp = new Bitmap(stream);
					bmp.Save("containers/" + random_guid + ".png", ImageFormat.Png);
					Console.WriteLine("Saved to \"containers\" as " + random_guid + ".png");
					bmp.Dispose();
				}
				return new HttpResponse(StatusCode.Ok, ("Saved to: \\containers\\" + random_guid + ".png"));
			}
			else{
				Console.WriteLine("DataURL not found. dataURL = {0}", dataURL.Substring(0, (dataURL.Length>40)?40:dataURL.Length));
				return new HttpResponse(StatusCode.Ok, ("dataURL: " + dataURL + "\n\nAfter encodeURIComponent(dataURL), you can send this HERE \nas GET parameter or POST (for big images);"));
			}
        }

        // example: prange/90-10 - gets not deleted posts from 91 to 100 (skip 90, take 10)
        private HttpResponse GetPresentRange(string fromto, string only_hashes = null)
		{
			//Console.WriteLine("fromto {0}, only_hashes {1}", fromto, only_hashes); 
            if(fromto==""){		//if POST-query
				fromto = only_hashes;	//try to get fromto from second parameter.
			}
			var spl = fromto.Split('-');
            //int skip = int.Parse(spl[0]);
            //int count = int.Parse(spl[1]);					//<--- THIS OLD CODE INCORRECT FOR MY "Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
				//replace this, using previous function
            int skip = nbpack.NBPackMain.parse_number(spl[0]);
            int count = nbpack.NBPackMain.parse_number(spl[1]);					//NOW - OK...

			NDB.Post[] posts;
			string[] posts_hashes;

			//Console.WriteLine("DbApiHandler.cs: GetPresentRange. Only hashes: "+only_hashes);
			
			//if second parameter with string "only_hashes" sent, using POST-query
			//or sent, using GET-query: http://127.0.0.1:7346/api/prange/?fromto=0-50&only_hashes=only_hashes
			if(only_hashes=="only_hashes"){
				//then, return hashes only
				posts_hashes = _db.RangePresent(skip, count, only_hashes);
				return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts_hashes));
			}else if(fromto.IndexOf("only_hashes")!=-1){
				string value_only_hashes = "only_hashes"+fromto.Split(new string[] { "only_hashes" }, StringSplitOptions.None)[1];
				//Console.WriteLine("value_only_hashes: "+value_only_hashes);
				
				//then, return hashes only
				posts_hashes = _db.RangePresent(skip, count, value_only_hashes);
				return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts_hashes));
			}else{
				//else, return array with posts...
				posts = _db.RangePresent(skip, count);
				return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts));
			}
            //return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts));
        }

        private HttpResponse ParamGet(string addr, string content)
        {
            if (!Configurator.Instance.HasValue(addr))
            {
                return new ErrorHandler(StatusCode.NotFound, "No such param.").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, Configurator.Instance.GetValue(addr, ""));
        }

        // returns available params in a JSON array
        private HttpResponse Params(string addr, string content)
        {
            var @params = Configurator.Instance.GetParams();
            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(@params));
        }

        private HttpResponse ParamSet(string addr, string content)
        {
            Configurator.Instance.SetValue(addr, content);
            return new HttpResponse(StatusCode.Ok, "Ok");
        }

        private HttpResponse GetPostByHash(string hash, string notUsed = null)
        {
            var post = _db.GetPost(hash);

            if (post == null)
            {
                return new ErrorHandler(StatusCode.NotFound, "No such post.").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post));
        }

        private HttpResponse GetLastNPosts(string hash, string n)	//from client 3.1
        {
            List<NDB.Post> posts;
            try
            {
                //posts = _db.GetLastNAnswers(hash, Convert.ToInt32(n)); //not always correct. Return error when value = 5.
                posts = _db.GetLastNAnswers(hash, nbpack.NBPackMain.parse_number(n));	//now ok.
                //posts = _db.GetLastNAnswers(hash, nbpack.NBPackMain.parse_number(n), true);		//fast GetLastNAnswers()
            }
            catch(Exception e)
            {
                return new ErrorHandler(StatusCode.BadRequest, e.Message).Handle(null);
            }

            if (posts == null)
            {
                return new ErrorHandler(StatusCode.NotFound, "No such post.").Handle(null);
            }
                        
            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts));
        }

        // includes deleted
        private HttpResponse GetNthPost(string n, string notUsed = null)
        {
            //var post = _db.GetNthPost(int.Parse(n));
            var post = _db.GetNthPost(nbpack.NBPackMain.parse_number(n));

            if (post == null)
            {
                return new ErrorHandler(StatusCode.NotFound, "No such post.").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post));
        }

        // returns count of all posts including deleted
        private HttpResponse GetPostCount(string notUsed1, string notUsed = null)
        {
            return new HttpResponse(StatusCode.Ok, _db.GetPostCount().ToString());
        }

        // returns recursive replies count for specified post
        private HttpResponse ThreadSize(string hash, string notUsed = null)
        {
            return new HttpResponse(StatusCode.Ok, _db.GetThreadSize(hash).ToString());
        }

        // returns count of not deleted posts
        private HttpResponse GetPresentCount(string notUsed1, string notUsed = null)
        {
            return new HttpResponse(StatusCode.Ok, _db.GetPresentCount().ToString());
        }

        // returns array of posts with messages including searchString, search avoids [img=..] tag contents.
        private HttpResponse Search(string searchString = null, string notUsed = null)	//notUsed contains base64 encoded searchString, when POST-query sent.
        {

			//Console.WriteLine("before searchString: "+searchString+" , notUsed: "+notUsed);

            if(searchString=="" && notUsed!=""){
				searchString = notUsed;
            }else{
				//searchString = searchString;	//fix compile warning
			}
			string [] splitted = searchString.Split('|');
			searchString = splitted[0].FromB64();

			//Console.WriteLine("after searchString: "+searchString+" , notUsed: "+notUsed);

			var found = new List<NDB.Post>();
			
            int limit = 500;
			if(splitted.Length==2){
				limit = nbpack.NBPackMain.parse_number(splitted[1]);
			}

            for (int i = _db.GetPostCount() - 1; i >= 0; i--)
            {
                var post = _db.GetNthPost(i);

                if (post == null)
                    continue;

                var msg = post.message.FromB64();

                if (msg.Contains("[img="))
                {
                    //msg = Regex.Replace(msg, "\\[img=[A-Za-z0-9+=/]{4,64512}\\]", "");
                    msg = Regex.Replace(msg, "\\[img=[A-Za-z0-9+=/]{4,}\\]", "");			//remove limit, because pictures from Karasiq nanoboard can be larger.
                }

                //if (msg.Contains(searchString))
                if (msg.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase)!=-1)
                {
                    found.Add(post);

                    if (found.Count >= limit) break;
                }
            }

            return new HttpResponse(StatusCode.Ok, found.Count == 0 ? "[]" : JsonConvert.SerializeObject(found.ToArray()));
        }

        private HttpResponse GetReplies(string hash, string notUsed = null)
        {
            var replies = _db.GetReplies(hash);
            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(replies));
        }

		//GET-query: http://127.0.0.1:7346/api/getposts/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
		//or POST-query to http://127.0.0.1:7346/api/getposts/POST/
		//with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
		//return JSON-array with posts, corresponding this hashes.
        private HttpResponse GetPosts(string GET = null, string POST = null)						//GET-query or POST query
        {
			string hashes = null;		//string with hashes, joined with comma. Default value is null, and this string is empty.
			
			if(POST==""){				//if string with second value is empty
				hashes = GET;			//then get hashes from get query
			}else{						//else, if contens was been send, using POST-query
				hashes = POST;			//get hashes from second parameter
			}
            var posts = _db.GetPosts(hashes); //get array with posts, by hash-list in the string.
            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts));
        }

        private HttpResponse AddPost(string replyTo, string content)
        {
            var post = new NDB.Post(replyTo, content);
            var added = _db.PutPost(post);

            if (!added)
            {
                return new ErrorHandler(StatusCode.BadRequest, "Can't add post, probably already exists").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post));
        }

		//Download posts from another nanodb api, by URL for fast syncronization.
        private HttpResponse Download_Posts(string GET = null, string POST = null)						//GET-query or POST query
        {
			string url = null;		//string with hashes, joined with comma. Default value is null, and this string is empty.
			
			if(POST==""){									//if string with second value is empty
				url = Uri.UnescapeDataString(GET);			//then get hashes from get query
			}else{											//else, if contens was been send, using POST-query
				url = Uri.UnescapeDataString(POST);			//get hashes from second parameter
			}
			
			//Console.WriteLine("url {0}", url);
			
            var status = _db.DownloadPosts(url); //download posts by URL
			
            if (!status)
            {
                return new ErrorHandler(StatusCode.BadRequest, "Can't download posts from url.").Handle(null);
            }
            return new HttpResponse(StatusCode.Ok, "Posts downloaded without any proxy: "+JsonConvert.SerializeObject(status));
        }

		//accept posts in JSON from anywhere, and add this to DataBase, after validation.
        private HttpResponse Upload_Posts(string GET = null, string POST = null) //GET-query or POST query
        {
			//for GET-query, need to use encodeURIComponent (Warning! There is limited data-size!)
			//for POST-query can be sent clear JSON.
			string JSON = null;		//string with hashes, joined with comma. Default value is null, and this string is empty.
			if(POST==""){									//if string with second value is empty
				JSON = Uri.UnescapeDataString(GET);			//then get hashes from get query
			}else{											//else, if contens was been send, using POST-query
				JSON = Uri.UnescapeDataString(POST);			//get hashes from second parameter
			}
			var status = _db.UploadPosts(JSON); //download posts by URL
			if (status<=0){
                return new ErrorHandler(StatusCode.BadRequest, "Can't download posts from url.").Handle(null);
            }
            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(status)+" posts was been accepted.");
        }

        private HttpResponse AddPosts(string none, string content)
        {
            try
            {
                var posts = JsonConvert.DeserializeObject<NDB.Post[]>(content);
                foreach (var p in posts)
                    _db.PutPost(p);
            }
            catch
            {
                return new HttpResponse(StatusCode.InternalServerError, "Error");
            }
            return new HttpResponse(StatusCode.Ok, "Ok");
        }

        // same as add but allows for putting deleted post back
        private HttpResponse ReAddPost(string replyTo, string content)
        {
            var post = new NDB.Post(replyTo, content);
            var added = _db.PutPost(post, true);

            if (!added)
            {
                return new ErrorHandler(StatusCode.BadRequest, "Can't add post, probably already exists").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post));
        }

        private object _lockObj = new object();

        private HttpResponse DeletePost(string hash, string notUsed = null)
        {
            int deleted = 0;
        
            lock (_lockObj)
            {
                for (int i = 0; i < 999; i++)
                {
                    try
                    {
                        deleted = _db.DeletePost(hash);
                        i = 1000;
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(i*15);
                    }
                }
            }
			if 			( deleted == 0 )	{
                return new ErrorHandler(StatusCode.BadRequest, "No such post.").Handle(null);
            }else if	( deleted == 1 )	{
			    return new HttpResponse(StatusCode.Ok, "Post "+hash+" was been deleted once.");
            }else if	( deleted == 2 )	{
			    return new HttpResponse(StatusCode.Ok, "Post "+hash+" was been deleted forever.");
            }
			return new HttpResponse(StatusCode.Ok, "Post "+hash+" just deleted.");
        }

        public HttpResponse Handle(HttpRequest request)
        {
            try
            {
                var splitted = request.Address.Split(new char[]{'/'}, StringSplitOptions.RemoveEmptyEntries);
                var cmd = splitted.Length < 2 ? "" : splitted[1];
                var arg = splitted.Length < 3 ? "" : splitted[2];

                if (_handlers.ContainsKey(cmd))
                {
                    return _handlers[cmd](arg, request.Content);
                }

                else
                {
                    return new ErrorHandler(StatusCode.BadRequest, "No such command: " + cmd + ". Available commands: " + JsonConvert.SerializeObject(_handlers.Keys.ToArray())).Handle(request);
                }
            }

            catch (Exception e)
            {
                return new ErrorHandler(StatusCode.InternalServerError, e.Message).Handle(request);
            }
        }
    }
}
