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
		private bool is_lite = false;					//True, when this is request on lite-server, or false, if this is request on full-server.
        public DbApiHandler(PostDb db, bool lite = false, bool SetAllowReput = false, bool SetByPassValidation = false)
        {
            _db = db;
			_handlers = new Dictionary<string, Func<string, string, HttpResponse>>();
			is_lite = lite;	allowReput = SetAllowReput;	bypassValidation = SetByPassValidation;		//set this once, for current Instance.
            if(lite == false){	//all actions allowed for full server
				// filling handlers dictionary with actions that will be called with (request address, request content) args:
				_handlers["get"] 				= 	GetPostByHash;
				_handlers["getlastn"] 			= 	GetLastNPosts;	//from client 3.1	//get last n posts in thread, or last 3 threads in category, to show this.
				_handlers["delete"] 			= 	DeletePost;
				_handlers["add"] 				= 	AddPost;
				_handlers["download-posts"] 	= 	Download_Posts;		//download posts by url, from another nanodb server.
				_handlers["upload-posts"] 		= 	Upload_Posts; 			//upload posts to another nanodb server, like Bitmessage-retranslation.
				_handlers["upload-post"] 		= 	Upload_Post; 			//upload posts to another nanodb server, like Bitmessage-retranslation.
				_handlers["addmany"] 			= 	AddPosts;
				_handlers["readd"] 				= 	ReAddPost;
				_handlers["replies"] 			= 	GetReplies;
			
				//get posts by list with hashes.
				//posts can be parsed from responce
				//GET-query (bytesize limit): http://127.0.0.1:7346/api/getposts/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
				//POST-query: http://127.0.0.1:7346/api/getposts/POST/ with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
				_handlers["getposts"] 			= 	GetPosts;		//get list of specified posts. Return array with posts in JSON.
			
				_handlers["count"]				= 	GetPostCount;
			//	_handlers["count"]				= 	GetPostCount;			//on lite-server, show number of posts, without "deleted_once"-posts.
				_handlers["nget"] 				= 	GetNthPost;
			
				//GET: http://127.0.0.1:7346/api/prange/?fromto=0-20 							- JSON response with array of posts
				//POST: http://127.0.0.1:7346/api/prange/0-20/									- array with posts
				//GET: http://127.0.0.1:7346/api/prange/?fromto=0-20&only_hashes=only_hashes	- array with hashes only.
				//POST: http://127.0.0.1:7346/api/prange/0-20/	string="only_hashes"			- array with hashes only.
				_handlers["prange"] 			= 	GetPresentRange;
			
				_handlers["pcount"] 			= 	GetPresentCount;
				_handlers["search"] 			= 	Search;
				_handlers["paramset"] 			= 	ParamSet;
				_handlers["paramget"] 			= 	ParamGet;		//get-set (if uncommented). On lite-server get only. And need to make not all params to get.
				_handlers["params"] 			= 	Params;
				_handlers["find-thread"] 		= 	FindThread;
				_handlers["threadsize"] 		= 	ThreadSize;
			
				//GET and POST parameters available. http://127.0.0.1:7346/api/getposts/POST/OnlyRAM|save_files|10 (last value - is limit for connectoins)
				_handlers["png-collect"] 		= 	PngCollect;
				_handlers["download-png"] 		= 	DownloadPNG;
			
			
				//posts from queue can be packed, using link:
				//http://127.0.0.1:7346/api/png-create/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
				//or using POST-query to http://127.0.0.1:7346/api/png-create/
				//with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
				_handlers["png-create"] 		= 	PngCreate;	//create png with post from queue or not (depending from parameters).
			
				_handlers["run-nbpack"] 		= 	Run_nbpack_with_params;	//Run NBPack.cs with parameters, specified in url.
																	//URL must contains parameters, joined with comma, after EncodeURIComponent();
				_handlers["convert-to-PNG"] 	= 	ConvertToPNG;	//Upload custom picture, convert this to PNG and save in the folder default folder "download/generated" or "containers"
				_handlers["png-collect-avail"] 	= 	(a,b)=>new HttpResponse(_collectAvail ? StatusCode.Ok : StatusCode.NotFound, _collectAvail ? "Finished." : "Collect...");
				_handlers["png-create-avail"] 	= 	(a,b)=>new HttpResponse(_createAvail ? StatusCode.Ok : StatusCode.NotFound, _createAvail ? "Finished." : "Creating PNG...");
				
				_handlers["get_reports"] 		= 	GetReports;
				_handlers["delete_reports"] 	= 	DeleteReportsForPostHash;
				_handlers["undelete_post"] 		= 	UndeletePost;	//undelete post, after this was been "deleted_once" on full-server: http://127.0.0.1:7346/api/undelete_post/{POST_HASH}
			}
			else if(lite == true){	//not all actions allowed for lite-server to run this by another anonymous users.

/*
				to make able anonymous to create PNG, need to enable params, paramget (places), createPNG, and pnt-create-avail.
				and also generate fractal, and make able to save this locally, using base64 dataURL, or blob-object.
				Now, this all just is not available, and maybe this will be available in future.
*/
				// filling handlers dictionary with actions that will be called with (request address, request content) args:
				_handlers["get"] 				= 	GetPostByHash;
				_handlers["getlastn"] 			= 	GetLastNPosts;	//from client 3.1	//get last n posts in thread, or last 3 threads in category, to show this.
//				_handlers["delete"] 			= 	DeletePost;

				//Another anonymous can delete all another posts, locally, and send reports for the posts to delete this from the server, by admin.
				//This is strongly not recommended, because Article 19 of "Universal Declaration of Human Rights (UDHR)",
				//but sometimes need to delete posts, for example, after attack server by using wipe-attack by using intensive shitposting.
				//	Warning!
				//	If anyone is not happy with the perversed level of censorship with some admin,
				//	anyone can raise his own server, and talk about anything there, himself, and with his friends.
				_handlers["report"] 			= 	ReportPost;

				_handlers["add"] 				= 	AddPost;				//when bypassValidation == true, posts without captcha can be accepted on lite-server. enable allowReput to use this instead "readd"
//				_handlers["download-posts"] 	= 	Download_Posts;			//disabled for lite-server, because this posts downoloaded in database on full-server.
				_handlers["upload-posts"] 		= 	Upload_Posts; 			//This option is enabled for lite-server. Posts can be uploaded. PostDb.cs: public bool PutPost(Post p, bool allowReput = false, bool bypassValidation = false).
				_handlers["upload-post"] 		= 	Upload_Post; 			//This option is enabled for lite-server. Post can be uploaded.  PostDb.cs: public bool PutPost(Post p, bool allowReput = false, bool bypassValidation = false).
				_handlers["addmany"] 			= 	AddPosts;				//posts without captcha added only when bypassValidation == true;
//				_handlers["readd"] 				= 	ReAddPost;				//disable this on lite-server to don't allow add deleted posts, and restore the posts with wipe. Use just add, when allowReput enabled on the full-server.
				_handlers["replies"] 			= 	GetReplies;
			
				//get posts by list with hashes.
				//posts can be parsed from responce
				//GET-query (bytesize limit): http://127.0.0.1:7346/api/getposts/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
				//POST-query: http://127.0.0.1:7346/api/getposts/POST/ with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
				_handlers["getposts"] 			= 	GetPosts;		//get list of specified posts. Return array with posts in JSON.
			
				_handlers["count"] 				= 	GetPostCount;
				_handlers["nget"] 				= 	GetNthPost;
			
				//GET: http://127.0.0.1:7346/api/prange/?fromto=0-20 							- JSON response with array of posts
				//POST: http://127.0.0.1:7346/api/prange/0-20/									- array with posts
				//GET: http://127.0.0.1:7346/api/prange/?fromto=0-20&only_hashes=only_hashes	- array with hashes only.
				//POST: http://127.0.0.1:7346/api/prange/0-20/	string="only_hashes"			- array with hashes only.
				_handlers["prange"] 			= 	GetPresentRange;
			
				_handlers["pcount"] 			= 	GetPresentCount;
				_handlers["search"] 			= 	Search;
//				_handlers["paramset"] 			= 	ParamSet;
				_handlers["paramget"] 			= 	ParamGet_lite;		//this can be available for lite-server, to see places and send container there.
//				_handlers["params"] 			= 	Params;				//some params can be available on lite-server, for example, params and paramget. To see places and send container there.
				_handlers["find-thread"] 		= 	FindThread;
				_handlers["threadsize"] 		= 	ThreadSize;
			
				//GET and POST parameters available. http://127.0.0.1:7346/api/getposts/POST/OnlyRAM|save_files|10 (last value - is limit for connectoins)
//				_handlers["png-collect"] 		= 	PngCollect;
				_handlers["download-png"] 		= 	DownloadPNG;
			
				//posts from queue can be packed, using link:
				//http://127.0.0.1:7346/api/png-create/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
				//or using POST-query to http://127.0.0.1:7346/api/png-create/
				//with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
				_handlers["png-create"] 		= 	PngCreate;	//create png with post from queue or not (depending from parameters).	//This can be available on lite-server to create PNG, but need to make able to save this not in the server-folder, but in browser.
	
				_handlers["run-nbpack"] 		= 	Run_nbpack_with_params;	//Run NBPack.cs with parameters, specified in url.
																			//URL must contains parameters, joined with comma, after EncodeURIComponent();
				_handlers["convert-to-PNG"] 	= 	ConvertToPNG;	//Upload custom picture, convert this to PNG and save it the "download/generated/" folder.
//				_handlers["png-collect-avail"] 	= 	(a,b)=>new HttpResponse(_collectAvail ? StatusCode.Ok : StatusCode.NotFound, _collectAvail ? "Finished." : "Collect...");
				_handlers["png-create-avail"] 	= 	(a,b)=>new HttpResponse(_createAvail ? StatusCode.Ok : StatusCode.NotFound, _createAvail ? "Finished." : "Creating PNG...");
			} Set_Timer_To_Delete_Generated_Images();
        }private static bool allowReput = false;	private static bool bypassValidation = false;	/*//true if need to make bypassValidation for posts in containers, while png-collect is processing.*/
		private static bool GET_POST_busy = false;	//true - when busy, false - when ready.
		
		/*
			This method return string with parameters, from request parameters.
			It return first or second parameter of request query (if second is available).
		*/
		private string param_GET_POST(string GET = null, string POST = null){
			while(GET_POST_busy){System.Threading.Thread.Sleep(1);}
			
			GET_POST_busy = true;
			if( (POST == null) || (POST == "")){	//if second parameter is empty
				GET_POST_busy = false;
//				return GET;								//return first parameter from GET-query, 	as string with parameters.
				return Uri.UnescapeDataString(GET);		//Try to unescape (make decodeURIComponent), if this param was been escaped by encodeURIComponent. Else return GET as is.
			}
			else{
				GET_POST_busy = false;
				return POST;							//return second parameter from POST-query, 	as string with parameters.
			}
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
			string params_ = param_GET_POST(GET, POST);		//string with queue_and_image, joined with comma. Default value is null, and this string is empty.
			string [] p = params_.Split('|');
			var obj_Aggregator = new Aggregator(new string [] {p[0], "collect_using_RAM", "save_files", "1" /*max_connections*/});
            return new HttpResponse(StatusCode.Ok, obj_Aggregator.DownloadPNG(p, is_lite));
        }

        private bool _collectAvail = true;
        private bool _createAvail = true;

        private HttpResponse PngCollect(string GET="", string POST = "")
        {
			string parameters = param_GET_POST(GET, POST);			//			string parameters = Uri.UnescapeDataString(param_GET_POST(GET, POST));
			Console.WriteLine("\nPngCollect was been runned, wait the end of collect images...");
			Console.WriteLine("parameters: " + parameters);
			string [] _params_ = (parameters!="") ? parameters.Split('|') : new string[0];
			if(allowReput == true){_params_ = _params_.Concat(new string[] { "allowReput" }).ToArray();}if(bypassValidation == true){_params_ = _params_.Concat(new string[] { "bypassValidation" }).ToArray();}
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
        private HttpResponse Run_nbpack_with_params(string GET = "", string POST = "")
        {
			//Console.WriteLine("GET {0}, POST {1}", GET, POST);
			string parameters = param_GET_POST(GET, POST);			//string parameters = Uri.UnescapeDataString(param_GET_POST(GET, POST));		//string with queue_and_image, joined with comma. Default value is null, and this string is empty.
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
		public static string notifications_with_filename = "";
		//posts from queue can be packed, using link:
		//http://127.0.0.1:7346/api/png-create/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
		//or using POST-query to http://127.0.0.1:7346/api/png-create/
		//with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
        private HttpResponse PngCreate(string GET = "", string POST = "") //default queue value is null
        {
			string queue_and_image = param_GET_POST(GET, POST);		//string with queue_and_image, joined with comma. Default value is null, and this string is empty.
            _createAvail = false;
            ThreadPool.QueueUserWorkItem(o => 
            {
                NBPackMain.Main_(new []
                {"-g", 
                    ((is_lite == true) ? "CreatePNG_on_lite_server" : ("http://"
                    + Configurator.Instance.GetValue("ip", "127.0.0.1")
                    + ":"
                    + Configurator.Instance.GetValue("port", "7346"))),
                    Configurator.Instance.GetValue("password", Configurator.DefaultPass),
					queue_and_image	//add queue_and_image as parameter
                });
                _createAvail = true;
            });
			
			while(_createAvail!=true)
				Thread.Sleep(1000);
			Thread.Sleep(5000);
			HttpResponse result = new HttpResponse(StatusCode.Ok, notifications_with_filename);//return new HttpResponse(StatusCode.Ok, "PNG successfully created.");
			notifications_with_filename = "";			return result;
        }
		public static Dictionary<string, DateTime> datetime_of_generated_images = new Dictionary<string, DateTime>();		public static System.Timers.Timer timer_to_delete;		public static double milliseconds_to_delete_generated_images = 1800000; /*(30 minutes is maximum, to store images in "download/generated", and "download/created")*/				private void Set_Timer_To_Delete_Generated_Images()		{			timer_to_delete = new System.Timers.Timer(milliseconds_to_delete_generated_images);			timer_to_delete.Elapsed += Delete_Old_Generated_Files;			timer_to_delete.AutoReset = false;			timer_to_delete.Enabled = true;		}		private void Delete_Old_Generated_Files(Object source, System.Timers.ElapsedEventArgs e)		{			if(datetime_of_generated_images.Count == 0){				timer_to_delete.Stop();				foreach(System.IO.FileInfo file in ((System.IO.DirectoryInfo) new DirectoryInfo("download"+Path.DirectorySeparatorChar+"generated")).GetFiles()){file.Delete();}				foreach(System.IO.FileInfo file in ((System.IO.DirectoryInfo) new DirectoryInfo("download"+Path.DirectorySeparatorChar+"created")).GetFiles()){file.Delete();}			}			else{				timer_to_delete.Start();				foreach(KeyValuePair<string, DateTime> generated_image in datetime_of_generated_images){					TimeSpan file_stored_time = (DateTime.Now).Subtract(generated_image.Value);					if(TimeSpan.Compare(file_stored_time, TimeSpan.FromMilliseconds( milliseconds_to_delete_generated_images )) >= 0){						if(							File.Exists(generated_image.Key)						){							( (System.IO.FileInfo) new FileInfo( generated_image.Key ) ).Delete();							datetime_of_generated_images.Remove(generated_image.Key);						}					}				}			}		}
		private HttpResponse generate_PNG(string query, string pathway = "")//?generate,1920,1080
		{
			//Console.WriteLine("args[0] = {0}, == \"\"", args[0], (args[0]==""));
			int width = 0; int height = 0; int bitlength = 0;
			pathway = ((pathway=="") ? ( (is_lite == true) ? "download"+Path.DirectorySeparatorChar+"generated" : "containers" ) : pathway );		//if pathway not specified, and if this was been request on lite-server, use "download/generated"-folder, to let them download it. Else - "containers"-folder, for full-server, or using the specified pathway.
			string [] splitted;
			string fractalgen_result = "";
			splitted = query.Split('|');
			string error = 	"Too many parameters.\nUse comma, as delimiter" + 
							"\"../api/convert-to-PNG/?generate_PNG,PNG_WIDTH,PNG_HEIGHT\"," + 
							"or \"../api/convert-to-PNG/generate_PNG/generate_PNG,bitlength_to_pack\"";
			if(splitted.Length>5){
				return new HttpResponse(StatusCode.BadRequest, error);
			}
			else if(splitted.Length==5 && splitted[1]=="fractalgen"){
				fractalgen_result = fractalgen.Program.Main_(new string[]{/*splitted[2],*/ pathway, splitted[3], splitted[4]});
			}
			else if(splitted.Length==4 && splitted[1]=="fractalgen"){
				fractalgen_result = fractalgen.Program.Main_(new string[]{ /*add this*/pathway, splitted[2], splitted[3]});
			}
			else if(splitted.Length==3 && splitted[1]=="fractalgen"){
				fractalgen_result = fractalgen.Program.Main_(new string[]{splitted[2]});
			}
			else if(splitted.Length==2 && splitted[1]=="fractalgen"){
				fractalgen_result = fractalgen.Program.Main_(new string[]{});
			}if(fractalgen_result != ""){if(pathway == "download"+Path.DirectorySeparatorChar+"generated"){timer_to_delete.Start();}
				string [] splitted_result = fractalgen_result.Split(new string[]{"Saved as: "}, StringSplitOptions.None); datetime_of_generated_images[splitted_result[1]] = DateTime.Now;
				fractalgen_result = fractalgen_result+"<br><a href=\"../"+(splitted_result[1]).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)+"\" target=\"_blank\">Open \""+(splitted_result[1]).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)+"\" as image</a>,<br><a href=\"../"+(splitted_result[1]).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)+"\" target=\"_blank\" download=\""+splitted_result[1].Split(Path.DirectorySeparatorChar).Last()+"\">Download "+splitted_result[1].Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split(Path.AltDirectorySeparatorChar).Last()+" as file</a>";
				return new HttpResponse(StatusCode.Ok, fractalgen_result);
			}
			else if(splitted.Length==4){/*pathway = ((is_lite == true) ? pathway : splitted[1]); // use default pathway for to prevent change it, and make DDoS, by send many requests, and save many files... */ width=nbpack.NBPackMain.parse_number(splitted[2]); height=nbpack.NBPackMain.parse_number(splitted[3]);}
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
			}if(pathway == "download"+Path.DirectorySeparatorChar+"generated"){timer_to_delete.Start();}
			var random_guid = Guid.NewGuid().ToString(); datetime_of_generated_images[(pathway + Path.DirectorySeparatorChar + random_guid + ".png")] = DateTime.Now;
			bmp.Save( pathway + Path.DirectorySeparatorChar + random_guid + ".png", ImageFormat.Png);					//png ImageFormat
			return new			HttpResponse(				StatusCode.Ok,				(					"Random PNG generated successfully. Width = " + width + ", height = " + height + ".\n" +					"Generated image saved to: "+(pathway + Path.DirectorySeparatorChar + random_guid + ".png")+					"<br>"+					"<a href=\""+						(("../"+pathway + "/"+ random_guid + ".png").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))+					"\""+					"target=\"_blank\">"+						"Open \""+							(("../"+pathway + "/" + random_guid + ".png").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))+						"\" as image"+					"</a>,"+					"<br>"+					"<a href=\""+						(("../"+pathway + "/"+ random_guid + ".png").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))+					"\" target=\"_blank\" download=\""+						(random_guid + ".png")+					"\">"+						"Download \""+							(random_guid + ".png")						+"\" as file."+					"</a>"				)			);

		}
		
		private HttpResponse ConvertToPNG(string dataURL_GET = "", string dataURL_POST = "") //default queue value is null
        {
			//Console.WriteLine("first {0}, second {1}", dataURL_GET.Substring(0, (dataURL_GET.Length>40)?40:dataURL_GET.Length), dataURL_POST.Substring(0, (dataURL_POST.Length>40)?40:dataURL_POST.Length));
//test split string
//input string with parameters for png-create
//output - splitted and extracted values...
//			int from_last_posts = 0;
			string dataURL = "";
//			string queue = "";															//string with hashes of posts, separated with comma.
//			int from_queue = 0;

			string queue_image_params = "";
//			if(			dataURL_GET.Trim()=="" 		&& 	dataURL_POST.Trim()==""		)	{queue_image_params = "";}
//			else if(	dataURL_POST.Trim()=="" 	&& 	dataURL_GET.Trim()!=""		)	{queue_image_params = dataURL_GET;}
//			else if(	dataURL_POST.Trim()!="" 	&& 	dataURL_GET.Trim()==""		)	{queue_image_params = dataURL_POST;}
//			else if(	dataURL_POST.Trim()!="" 	&& 	dataURL_GET.Trim()!=""		)	{
//				if(			dataURL_GET.IndexOf("%2F")	!=	-1 || dataURL_GET.IndexOf("%2f") != -1	)	{queue_image_params = dataURL_GET;}
//				else if(
//							dataURL_POST.IndexOf("%2F")	!=	-1 || dataURL_POST.IndexOf("%2f") != -1 ||
//							dataURL_POST.IndexOf("\n")	!=	-1 || dataURL_POST.IndexOf("%2Fn")!=-1 	||
//							dataURL_POST.IndexOf("%2fn")!=-1
//				)																						{queue_image_params = dataURL_POST;}
//				else	{
//							//Console.WriteLine("ConvertToPNG: 1. another case...");
//				}
//			}else	{
//						//Console.WriteLine("ConvertToPNG: 2. another case...{0}");
//			}

																						//if string send using GET-query this must to be escaped.
			queue_image_params = param_GET_POST(dataURL_GET, dataURL_POST);		//			queue_image_params = Uri.UnescapeDataString(queue_image_params); 			//Unescape this. "%2F" or "%2f" replaced to "/".
			var splitted = queue_image_params.Split('|');
			string pathway = ( (is_lite == true) ? "download"+Path.DirectorySeparatorChar+"generated" : "containers" );		//if this was been request on lite-server, use "download"-folder, to let them download it. Else - "containers"-folder.
			bool exists = System.IO.Directory.Exists(pathway); if(!exists){System.IO.Directory.CreateDirectory(pathway);} if(pathway == "download"+Path.DirectorySeparatorChar+"generated"){timer_to_delete.Start();}
			for(int i=0;i<splitted.Length;i++){
				if(splitted[i]==""){continue;}						//if empty string - continue. String is empty if '\n' at last of post-content.
				if(		splitted[i].IndexOf("data:") != -1
					&& 	splitted[i].IndexOf("base64,")!= -1
					&& 	nbpack.NBPackMain.IsBase64Encoded(splitted[i].Split(',')[1])
				)	{dataURL = splitted[i];}		//if dataURL specified
				else{
				
					
					//pathway = ((is_lite) ? pathway : splitted[i]);	//just folder name, like "download/generated" or "containers"-folder. Don't set it for request on lite-server, and use default predefined folder, to prevent change this and make DDoS, and StackOverFlow, by sending many requests, to save many files.
				
				}
			}

			if(queue_image_params.IndexOf("generate_PNG")!=-1){return generate_PNG((string)queue_image_params, (string)pathway);}

			if(dataURL==""){dataURL="No_dataURL_specified_for_source_image";}		//if string is null, this is empty string
			//if(queue.Trim().Length == 0){queue = "";}								//if trimmed queue value length == 0, leave this empty

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
					bmp.Save(pathway + Path.DirectorySeparatorChar + random_guid + ".png", ImageFormat.Png);
					Console.WriteLine("Saved to \""+(pathway + Path.DirectorySeparatorChar + random_guid + ".png")+"\"");
					bmp.Dispose();
				}datetime_of_generated_images[(pathway + Path.DirectorySeparatorChar + random_guid + ".png")] = DateTime.Now;
				return new HttpResponse(StatusCode.Ok,	("Saved to: \""+(pathway + Path.DirectorySeparatorChar + random_guid + ".png")+"\", <br>"+	"<a href=\""+("../" + pathway + "/" + random_guid + ".png").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)+"\" target=\"_blank\">"+	"Open \""+("../" + pathway + "/" + random_guid + ".png").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)+"\" as image"+	"</a>, <br>"+	"<a href=\""+("../" + pathway + "/" + random_guid + ".png").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)+"\" target=\"_blank\" download=\""+(random_guid + ".png")+"\">"+	"Download \""+(random_guid + ".png")+"\" as file."+	"</a>")	);
			}
			else{
				Console.WriteLine("DataURL not found. dataURL = {0}", dataURL.Substring(0, (dataURL.Length>40)?40:dataURL.Length));
				return new HttpResponse(StatusCode.Ok, ("dataURL: " + dataURL + "\n\nAfter encodeURIComponent(dataURL), you can send this HERE \nas GET parameter or POST (for big images);"));
			}
        }

        // example: prange/90-10 - gets not deleted posts from 91 to 100 (skip 90, take 10)
        private HttpResponse GetPresentRange(string GET = null, string POST = null)
		{
//			Console.WriteLine("DbApiHandler.cs. GetPresentRange. \n\n\n\n\n ");
			string arguments = param_GET_POST(GET, POST);	//get string with arguments
			
			string[] splitted = arguments.Split('-');		//split this string by "-" and put arguments to array.
			
			//define default values:
			int skip = 0;				//skip posts
			int count = 0;				//number posts to get this
			string only_hashes = null;	//can contains two values, if need to return hashes
			bool append_text = false;	//append text "post_was_deleted", //and "post_is_reported" to base64 of post message or not? (false by default)
			bool show_deleted = false;	//is need to show deleted posts? (false by default)
			
			NDB.Post[] posts;			//result if need to return 	array with posts 		Post[]
			string[] posts_hashes;		//result if "only_hashes" 	array with strings		string[]
			
			if(splitted.Length < 2){	//if lesser than 2 arguments specified
				return new HttpResponse(StatusCode.BadRequest, "DbApiHandler.cs. GetPresentRange. Invalid number of arguments. splitted.Length: "+splitted.Length); //return error.
			}//else, continue...
			
			//parse numbers from strings
			skip = nbpack.NBPackMain.parse_number(splitted[0]);						//skip
			count = nbpack.NBPackMain.parse_number(splitted[1]);					//count
			
			if(splitted.Length == 2){	//if only 2 arguments specified
				posts = _db.RangePresent(skip, count, append_text, show_deleted);		//get array with posts by running method from PostDb.cs, with default arguments.
				return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts));	//and return this as JSON.
			}//else, continue...
			if(splitted.Contains("append_text")){	//if need to append text, and this string found in argumets
				append_text = true;						//set it to true.
			}
			if(splitted.Contains("show_deleted")){	//if need to show deleted posts, and this string found in aruments
				show_deleted = true;					//set it to true
			}
			if(splitted.Contains("only_hashes")){	//if need to return only hashes of the posts, and this string found in arguments.
				only_hashes = "only_hashes";		//set this value in this string
				posts_hashes = _db.RangePresent_string(skip, count, only_hashes, show_deleted);			//run another method from NBPack.cs, with defined earlier arguments
				return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts_hashes));		//and return hashes of the posts, as JSON.
			}
			else if(splitted.Contains("hashes_with_bytelength")){	//if need to append bytelength to hashes, and this string was found in arguments
				only_hashes = "hashes_with_bytelength";			//set this value in this string
				posts_hashes = _db.RangePresent_string(skip, count, only_hashes, show_deleted);			//run another method from NBPack.cs, with defined earlier arguments
				return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts_hashes));		//and return the hashes with bytelength, as JSON.
			}else{																						//else, if no need to return hashes.
				posts = _db.RangePresent(skip, count, append_text, show_deleted);						//get the array with posts with defined earlier artuments.
				//Console.WriteLine("posts.Length"+posts.Length);
				string response = "";
				try{
					response = JsonConvert.SerializeObject(posts);	//hm... 1681 posts not return in browser... 1680 posts - return good, as text... No any catch.
					//Console.WriteLine("response.Length: "+response.Length+", response.Substring(response.Length-100): "+response.Substring(response.Length-100));
					return new HttpResponse(StatusCode.Ok, response);				//and return result as JSON.		Warning... 1681 posts not returned good in JSON...
				}catch (Exception ex){
					return new HttpResponse(StatusCode.BadRequest, ex.ToString());				//return Exception 
				}
			}
        }

        private HttpResponse ParamGet(string addr = null, string content = null)
        {
			if((content == null || content == "") && addr.IndexOf('?')!=-1){
				string[] splitted 	= addr.Split('?');
				addr 				= splitted[0];
				content 			= Uri.UnescapeDataString(splitted[1]);
			}
            return new HttpResponse(StatusCode.Ok, Configurator.Instance.GetValue(addr, content));	//get param or set default.
        }
		
		/*
			get only allowed params for lite-server.
		*/
        private HttpResponse ParamGet_lite(string GET = null, string POST = null)
        {
			var param = param_GET_POST(GET, POST);
				//	parameters, disabled to show on lite-server:
			string[] unallowed_params = new string []	{
														"places",				//places - temporary disabled, and can be available, after PNG-collect working on lite-server.
														"captcha_pack_file",
														"Proxy_List",
														"Services_Returns_External_IP",
														"ip",
														"port",
														"password",
														"no_tcp_delay",
														"check_version_update",
														"show_timestamps",
														"instant_retranslation",
														"detect_URLs",
														"show_deleted",
														"post_offset_in_tree_px",
														"post_delete_timeout",
														"use_spam_filter",
														"spam_filter",
														"remind_if_updates_exists",
														"post_count_notification_time",
														"check_updates_every_hours",
														"Download_Timeout_Sec",
														"last_update",
														"original_captcha_sha256",
														"last_another_repo_version",
														"captcha_url",
														"collect_memory_limit_to_wait",
												}
			;
            if (!Configurator.Instance.HasValue(param) || unallowed_params.Contains(param))
            {
                return new ErrorHandler(StatusCode.NotFound, "No such param.").Handle(null);
            }
            return new HttpResponse(StatusCode.Ok, Configurator.Instance.GetValue(param, ""));			//get param only, because this paramget API-call on public lite-server is opened for anonymous.
        }

        // returns available params in a JSON array
        private HttpResponse Params(string addr, string content)
        {
            var @params = Configurator.Instance.GetParams();
            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(@params));
        }

        private HttpResponse ParamSet(string addr, string content)
        {
			if(content == null && addr.IndexOf('?')!=-1){	//if second parameter from POST-query not defined, try parse first parameter from GET-query
				string[] splitted 	= addr.Split('?');
				addr 				= splitted[0];
				content 			= Uri.UnescapeDataString(splitted[1]);
			}

//			Console.WriteLine("DbApiHandler.cs. ParamSet. addr: "+addr+", content: "+content);	//show two parameters
			
			if(addr!=null && content!=null){
				Configurator.Instance.SetValue(addr, content);
				return new HttpResponse(StatusCode.Ok, "Ok");
			}
			else{
				return new HttpResponse(StatusCode.BadRequest, "DbApiHandler.cs. ParamSet. Invalid request. Value cann't be null: param:"+addr+", value:"+content);
			}
        }

        private HttpResponse GetPostByHash(string GET = null, string POST = null)
        {
			string arguments = param_GET_POST(GET, POST);
			string[] splitted = arguments.Split('-');
			string hash = splitted[0];
			bool appendText = ((splitted.Length == 2)? bool.Parse(splitted[1]) : false);

            var post = _db.GetPost(hash, appendText);

            if (post == null)
            {
                return new ErrorHandler(StatusCode.NotFound, "No such post.").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post));
        }

        private HttpResponse GetLastNPosts(string GET = null, string POST = null)	//from client 3.1
        {
			string arguments 	= param_GET_POST(GET, POST);
			string[] splitted 	= arguments.Split('-');
			string hash 		= splitted[0];
			string n 			= splitted[1];
			bool appendText 	= ((splitted.Length >= 3)? bool.Parse(splitted[2]) : false);
			bool fast 			= ((splitted.Length >= 4)? bool.Parse(splitted[3]) : false);

//			Console.WriteLine("GetLastNPosts. hash: "+hash+", n: "+n+", appendText: "+appendText+", fast: "+fast);
            List<NDB.Post> posts;
            try
            {
                posts = _db.GetLastNAnswers(hash, nbpack.NBPackMain.parse_number(n), appendText, fast);	//now ok.
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
        private HttpResponse Search(string GET = null, string POST = null)	//notUsed contains base64 encoded searchString, when POST-query sent.
        {
			try{
				string searchString = param_GET_POST(GET, POST);

				string [] splitted = searchString.Split('|');
				searchString = splitted[0].FromB64();
				Console.WriteLine("searchString: \""+searchString+"\"");
				var found = new List<NDB.Post>();
				
				int limit = 500;
				if(splitted.Length==2){
					limit = nbpack.NBPackMain.parse_number(splitted[1]);
				}
				
				for (int i = _db.GetPostCount() - 1; i >= 0; i--)
				{
					var post = _db.GetNthPost(i);
					
					if (post == null){
						continue;
					}

					var msg 	= (						//maybe this need to delete
									(post.message.StartsWith("post_was_deleted"))// || post.message.StartsWith("post_is_reported"))
										? post.message.Substring(16)
										: post.message
								)
								.FromB64();
					
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
			catch (Exception ex){
				Console.WriteLine(ex);
				return new HttpResponse(StatusCode.BadRequest, "[]");
			}	
        }

        private HttpResponse GetReplies(string GET = null, string POST = null)
        {
			string arguments = param_GET_POST(GET, POST);
			string[] splitted = arguments.Split('-');
			string hash = splitted[0];
			bool appendText = ((splitted.Length == 2) ? bool.Parse(splitted[1]) : false);
            var replies = _db.GetReplies(hash, appendText);
            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(replies));
        }

		//GET-query: http://127.0.0.1:7346/api/getposts/f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35
		//or POST-query to http://127.0.0.1:7346/api/getposts/POST/
		//with value "f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35"
		//return JSON-array with posts, corresponding this hashes.
        private HttpResponse GetPosts(string GET = "", string POST = "")						//GET-query or POST query
        {
			try{
				string hashes = param_GET_POST(GET, POST);		//string with hashes, joined with comma. Default value is null, and this string is empty.
//				Console.WriteLine("DbApiHandler.cs: GetPosts. hashes: "+hashes);
				var posts = _db.GetPosts(hashes); //get array with posts, by hash-list in the string.
				return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(posts));
			}
			catch (Exception e){
				Console.WriteLine(e);
				return new HttpResponse(StatusCode.BadRequest, "DbApiHandler.cs. GetPosts. Something is wrong: "+e);
			}
        }

        private HttpResponse AddPost(string replyTo, string content)
        {
            var post = new NDB.Post(replyTo, content);
            var added = _db.PutPost(post, allowReput, bypassValidation);

            if (!added)
            {
                return new ErrorHandler(StatusCode.BadRequest, "Can't add post, probably already exists").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post));
        }

		//Download posts from another nanodb api, by URL for fast syncronization.
        private HttpResponse Download_Posts(string GET = null, string POST = null)						//GET-query or POST query
        {
			string url = param_GET_POST(GET, POST);			//string with hashes, joined with comma. Default value is null, and this string is empty.
			//Console.WriteLine("url {0}", url);
			
            var status = _db.DownloadPosts(url, allowReput, bypassValidation); //download posts by URL
			
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
			//		GET method, you are limited to a maximum of 2,048 characters, minus the number of characters in the actual path
			//		However, the POST method is not limited by the size of the URL for submitting name and value pairs.
			//for POST-query can be sent clear JSON
			//	https://docs.microsoft.com/en-us/dotnet/api/system.web.configuration.httpruntimesection.maxrequestlength?view=netframework-4.8
			//	The maximum request size in kilobytes. The default size is 4096 KB (4 MB).
			
//			string JSON = null;		//string with hashes, joined with comma. Default value is null, and this string is empty.
//			if(POST==""){									//if string with second value is empty
//				JSON = Uri.UnescapeDataString(GET);			//then get hashes from get query
//			}else{											//else, if contens was been send, using POST-query
//				JSON = POST;								//get hashes from second parameter, without decodeURIComponent()
//			}
			string JSON = param_GET_POST(GET, POST);
		//	Console.WriteLine("JSON: "+JSON.Substring(0, 100));
		//	Console.WriteLine("DbApiHandler.cs. Upload_Posts. Accepted JSON: ");
		//	Console.WriteLine("first: "+JSON.Substring(0, ((JSON.Length >= 100) ? 100 : JSON.Length)));
		//	Console.WriteLine("last: "+JSON.Substring( (JSON.Length>100) ? JSON.Length-100 : 0 ));
			if(JSON.StartsWith("posts_uploading_large_data: [")){
		//		Console.WriteLine("JSON.StartsWith(\"posts_uploading_large_data: [\"): "+JSON.StartsWith("posts_uploading_large_data: ["));
				string json = JSON.Split(new string[] { "posts_uploading_large_data: " }, StringSplitOptions.None)[1];
				string[] object_arr = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(json);
				string result = "DbApiHandler.cs. Upload_Posts. Accepted: "+object_arr[0]+" posts. Added in DataBase: "+object_arr[1]+" posts.";
                Console.WriteLine(result);
				return new HttpResponse(StatusCode.Ok, result);
			}
			int[] status = _db.UploadPosts(JSON, allowReput, bypassValidation); //download posts by URL
			Console.WriteLine(
				"DbApiHandler.cs. UploadPosts. "+JsonConvert.SerializeObject(status[0])+" posts was been accepted, "+
				JsonConvert.SerializeObject(status[1])+" added in DataBase."
			);
            return new HttpResponse(
				StatusCode.Ok,
				JsonConvert.SerializeObject(status[0])+" posts was been accepted, "+
				JsonConvert.SerializeObject(status[1])+" added in DataBase."
			);
        }

		//accept posts in JSON from anywhere, and add this to DataBase, after validation.
        private HttpResponse Upload_Post(string GET = null, string POST = null) //GET-query or POST query
        {
			try{
				//for GET-query, need to use encodeURIComponent (Warning! There is limited data-size!)
				//for POST-query can be sent clear JSON.
//				string JSON = null;		//string with hashes, joined with comma. Default value is null, and this string is empty.
//				if(POST=="" || POST == null){					//if string with second value is empty
//					JSON = Uri.UnescapeDataString(GET);			//then get hashes from get query
//				}else{											//else, if contens was been send, using POST-query
//					JSON = POST;			//get hashes from second parameter
//				}
				string JSON = param_GET_POST(GET, POST);			//	Console.WriteLine("JSON: "+JSON.Substring(0, 100));
				bool status = _db.UploadPost(JSON, allowReput, bypassValidation); //download posts by URL
				return new HttpResponse(StatusCode.Ok, "Try to uploading post: "+JSON+"...\n"+status+((!status)?"\nMaybe this post already exist here.":""));
			}catch(Exception ex){
				Console.WriteLine(ex);
				return new HttpResponse(StatusCode.Ok, ex.ToString());
			}
        }
		
        private HttpResponse AddPosts(string none, string content)
        {
            try
            {
                var posts = JsonConvert.DeserializeObject<NDB.Post[]>(content);
                foreach (var p in posts)
                    _db.PutPost(p, allowReput, bypassValidation);
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
            var added = _db.PutPost(post, true, bypassValidation);

            if (!added)
            {
                return new ErrorHandler(StatusCode.BadRequest, "Can't add post, probably already exists").Handle(null);
            }

            return new HttpResponse(StatusCode.Ok, JsonConvert.SerializeObject(post));
        }

        private object _lockObj = new object();

		//Delete post "delete_once", or "delete_forever", if post was already "deleted_once".
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

		//Add reason of report the post with specified hash.
        private HttpResponse ReportPost(string GET = null, string POST = null)
        {
		
//			Console.WriteLine("GET: "+GET+" POST: "+POST+" URI: "+Uri.UnescapeDataString(GET));
			string hash_reason = null;
			string hash = null;
			string reason = null;
			
			try{
//				if( POST == null ){
//					hash_reason = Uri.UnescapeDataString(GET);
//				}else{
				hash_reason = param_GET_POST(GET, POST);	//					hash_reason = Uri.UnescapeDataString(POST);
//				}
			}catch(Exception ex){
				return new ErrorHandler(StatusCode.BadRequest, "Error report value: "+ex).Handle(null);
			}
			
			//Console.WriteLine("hash_reason: "+hash_reason);
			
			string [] splitted = hash_reason.Split(new string[]{ "|" }, 2, StringSplitOptions.None);
			hash = splitted[0];
			if(hash.Length>=2){ if(splitted[1].Length > 64){Console.WriteLine("Too long report-text. Maximum is 64 symbols. ("+splitted[1].Length+"> 64): "+", report-text, after substring: "+(splitted[1]).Substring(0, 64));}
				reason = (splitted[1].Length > 64) ? (splitted[1]).Substring(0, 64) : (splitted[1]);	//limit reason string length = 64 symbols max.
			}else{
				reason = "DbApiHandler.cs. ReportPost. undefined report reason.";
			}

			//Console.WriteLine("hash: "+hash+" reason: "+reason);

            int reported = 0;
        
            lock (_lockObj)
            {
                for (int i = 0; i < 999; i++)
                {
                    try
                    {
                        reported = _db.ReportPost(hash, reason);
                        i = 1000;
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(i*15);
                    }
                }
            }
			if 			( reported == 0 )	{
                return new ErrorHandler(StatusCode.BadRequest, "No such post.").Handle(null);
            }else if	( reported == 1 )	{
			    return new HttpResponse(StatusCode.Ok, "Post "+hash+" was been reported. Reason: "+reason);
            }else{
				return new HttpResponse(StatusCode.Ok, "Post "+hash+" just reported.");
			}
        }

        // returns hashes of posts with reports
        private HttpResponse GetReports(string notUsed1, string notUsed = null)
        {
            return new HttpResponse(StatusCode.Ok, _db.GetReports().ToString());
        }
		
		//Delete reports for post with hash
        private HttpResponse DeleteReportsForPostHash(string hash, string notUsed = null)
        {
			bool reports_deleted = _db.DeleteReportsForPostHash(hash);
			return new HttpResponse(
				(reports_deleted == true)
					? StatusCode.Ok
					: StatusCode.BadRequest
				,
				"Delete reports for post with "+hash+":"+reports_deleted
			);
        }
		
		//Undelete "deleted_once"-post
        private HttpResponse UndeletePost(string hash, string notUsed = null)
        {
//			Console.WriteLine("DbApiHandler.cs. UndeletePost. Try to undelete post...");
			bool post_undeleted = _db.UndeletePost(hash);
			return new HttpResponse(
				(post_undeleted == true)
					? StatusCode.Ok
					: StatusCode.BadRequest
				,
				"Undelete post with "+hash+": "+post_undeleted
			);
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
