using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using NDB;			//to run _db.UploadPost(post_in_JSON) here.

namespace NServer
{
    /*
        Low-level limited implementation of HTTP server based on TCP socket.
    */
    class TcpServer
    {
        private TcpListener 	_server;							//full-server
        private Boolean 		_isRunning;							//is running full server or not?

        private TcpListener 	_server_lite;						//lite-server
        private Boolean 		_isRunning_lite;					//is running lite server or not?

		private bool			lite_server							= false;			//by default, lite-server is enabled. Run nanodb as "nanodb old" to disable it, or "nanodb lite" (to enable).
		private string no_tcp_delay 								= Configurator.Instance.GetValue("no_tcp_delay", "true");	//get value once to compare with it
        public event Action<HttpConnection> ConnectionAdded;		//for full-server
        public event Action<HttpConnection> ConnectionAdded_lite;	//for lite-server
		
		//modes to working with large JSON data from POST-request to "../api/upload-posts/"
		//True, if need to Upload_Posts from large POST-request, by reading JSON from TCP-stream, after send POST-request with large JSON data. Else - false.
		//		Warning, in this case, client can be disconnected.
		private bool Upload_Posts_From_TCP_Stream 					= false;	//start "nanodb.exe large_POST_mode1", (default mode)
		//True, if need to Save the large data from POST request to cache-file, and Upload_Posts then, from file.	Else - false.
		private bool Save_In_Cache_File_The_Large_POST_Request 		= false;	//start "nanodb.exe large_POST_mode2",
		//True, if need to Upload_Posts from file with JSON-cache
		//	This value will be automatically true, if POST-request with JSON will send to /api/upload-posts/, and if request saved in cache-file.
		private bool Upload_Posts_From_File_With_JSON_Cache			= false;
		//To disable auto-Uploading posts post-by-post, run	"nanodb.exe lite large_POST_mode0" (I got OutOfMemoryException for large POST-requests with JSON over 30 MB in tests);
		private string path 										= @"temp/Saved_cache_of_Large_POST_request_"+((string)(((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds)).ToString()))+".txt"; //use unique filename: unix-timestamp
		private NDB.PostDb _db;											//to run _db.UploadPost(post_in_JSON).
		public static bool allowReput = false; public	static bool bypassValidation = false;					//set skip validate pow and captcha? (true if yes, or false, by default)
		object _lock = new object();	//object to lock code for one stream
		public	static bool do_not_delete_cache_file = false;			//if no need to delete cache file, and need to generate new file to store posts as JSON. large_POST_mode3
        public TcpServer(NDB.PostDb db, string ip, int port, bool enable_lite_server = false, string large_POST_mode = "0", bool setAllowReput = false, bool setByPassValidation = false) //lite_server and mode - from Program.cs
        {
			if			(	large_POST_mode		==		"1"	)	{
				Console.WriteLine("\nMode 1:\nPosts will be uploaded one by one,\nfrom TCP stream, if POST data will be large.\n");
				Upload_Posts_From_TCP_Stream 				= 	true;
				Save_In_Cache_File_The_Large_POST_Request 	= 	false;
			}else if	(	large_POST_mode		==		"2"	|| large_POST_mode		==		"3")	{if(large_POST_mode == "3"){do_not_delete_cache_file = true;}
				Console.WriteLine("\nMode "+((do_not_delete_cache_file == true)?"3":"2")+":\nPosts will be cached in cache-file"+((do_not_delete_cache_file == true)?"(s)":"")+" in \"temp\"-folder\nif POST data will be large, then uploaded from cache.\n");
				Upload_Posts_From_TCP_Stream 				= 	false;
				Save_In_Cache_File_The_Large_POST_Request 	= 	true;
			}else{
				Console.WriteLine("\nMode 0 (old mode, OutOfMemoryException):\nPosts will be uploaded after all POST-data,\neven if data will be large.\n");
				Upload_Posts_From_TCP_Stream 				= 	false;
				Save_In_Cache_File_The_Large_POST_Request 	= 	false;
			} allowReput = setAllowReput; bypassValidation = setByPassValidation;
			lite_server = enable_lite_server;
//			Console.WriteLine("TcpServer.cs. TcpServer. lite_server: "+lite_server+", large_POST_mode: "+large_POST_mode);
			_db = db;
            Console.WriteLine("Listening on port " + port + " (private server)");
			if(lite_server == true){
				Console.WriteLine("Listening on port " + (port+1) + " (public server_lite)");
			}

            if (ip == "localhost")
            {
                IPAddress ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
                Console.WriteLine(ipAddress.ToString() + ":" + port);
                IPEndPoint ipLocalEndPoint = new IPEndPoint(ipAddress, port);
                _server = new TcpListener(ipLocalEndPoint);
				if(lite_server == true){
					IPEndPoint ipLocalEndPoint_lite = new IPEndPoint(ipAddress, port+1);
					_server_lite = new TcpListener(ipLocalEndPoint_lite);
				}
            }

            else
            {
                _server = new TcpListener(IPAddress.Parse(ip), port);
				if(lite_server == true){
					_server_lite = new TcpListener(IPAddress.Parse(ip), port+1);
				}
            }
        }
        public void Run()
        {
//			Console.WriteLine("TcpServer.cs. Run. lite_server: "+lite_server);
            _server.Start();
            _isRunning = true;

			if(lite_server == true){
				_server_lite.Start();
				_isRunning_lite = true;
			}

            LoopClients();
        }

        public void Stop()
        {
//			Console.WriteLine("TcpServer.cs. Stop. lite_server: "+lite_server);

            Console.WriteLine("Server was shut down");
            _isRunning = false;
            _server.Stop();

			if(lite_server == true){
				_isRunning_lite = false;
				_server_lite.Stop();
			}

        }

        private void LoopClients()
        {
//			Console.WriteLine("TcpServer.cs. LoopClients. lite_server: "+lite_server);

			if(lite_server == false){	//if lite_server is disabled - listen only main full-server in one loop
				while (_isRunning)
				{
					if (!_server.Pending())
					{
						Thread.Sleep(100);
						continue;
					}

					try
					{
						TcpClient newClient = _server.AcceptTcpClient();
						Thread t = new Thread(()=>HandleClient(newClient, lite_server));	//here is full-server so second parameter is = false (is_lite_server?)
						t.Start();
					}
					catch
					{
					}
				}
			}
			else{	//else listen both servers full server and lite-server.
				bool continue1 = false;
				bool continue2 = false;

				while (_isRunning || _isRunning_lite)
				{
					if (!_server.Pending())
					{
						Thread.Sleep(100);
						continue1 = true;
					}

					if(continue1 == false){
						try
						{
							TcpClient newClient = _server.AcceptTcpClient();
							Thread t = new Thread(()=>HandleClient(newClient, continue1));	//here is full-server so second parameter is = false (is_lite_server?)
							t.Start();
						}
						catch
						{
						}
					}

					if (!_server_lite.Pending())
					{
						Thread.Sleep(100);
						continue2 = true;
					}

					if(continue2 == false){
						try
						{
							TcpClient newClient_lite = _server_lite.AcceptTcpClient();
							Thread t_lite = new Thread(()=>HandleClient(newClient_lite, !continue2));	//here is lite-server, so second parameter is true (is_lite_server?)
							t_lite.Start();
						}
						catch
						{
						}
					}
				
					continue1 = false;
					continue2 = false;
				}
				//end the loop to listen two servers
			}
			
        }

		private bool Save_Large_data_From_POST_Request(string cached_data, string pathway = ""){
			string specified_path = ((pathway != "")? pathway : path);
			// This text is added only once to the file.
			if (!File.Exists(specified_path))
			{
				// Create a file to write to.
				string[] createText = { cached_data };
				File.WriteAllLines(specified_path, createText, Encoding.UTF8);
			}
			// This text is always added, making the file longer over time
			// if it is not deleted.
			//cached_data = cached_data + Environment.NewLine;
			else{
				long cache_length = new System.IO.FileInfo(specified_path).Length;
				if(cache_length >= 256 * 1024 * 1024){	//256 MB - max file-size for cache-file.
					return false;
				}else{
					File.AppendAllText(specified_path, cached_data, Encoding.UTF8);
				}
			}
			return true;
		}
		
		//UploadPosts as JSON, from (string) readData, parse this string, then update (int) accepted_post, and (int) uploaded_posts,
		//and if need (bool) substring_end string from '"}]' to '"', and add '}', then, parse it and UploadPost from parsed JSON.
		private Object[] Upload_Posts_from_JSON_string(string readData, int accepted_posts, int uploaded_posts, bool substring_end){
			lock(_lock){if(readData.Contains("\"}]") && readData.Contains("[{\"hash\":\"")){Console.WriteLine("Cut headers for another request."); readData = (readData.Split(new string[] { "\"}]" }, StringSplitOptions.None)[0])+"\"},{\"hash\":\""+readData.Split(new string[] { "[{\"hash\":\"" }, StringSplitOptions.None)[1];}//cut POST headers from second request, cached and appended in the same file.
				string [] output = readData.Split(new string[] { "},{" }, StringSplitOptions.None);
//				Console.WriteLine("Upload_Posts_from_JSON_string: after split readData by (\"},{\"), output.Length: "+output.Length);
				string the_rest = "";
				for(var i = 1; i<=output.Length; i++){
					if(i == output.Length){
//						Console.WriteLine("last post data (partial): "+output[(i-1)].Substring(0, 41));	//show full post hash
//						Console.WriteLine("last post data (partial): "+output[(i-1)].Substring(0, 1500));	//show full post hash
						
						if(substring_end && output[(i-1)].EndsWith("\"}]")){
//							Console.WriteLine("last post data (partial): "+output[(i-1)].Substring(output[(i-1)].Length-30));	//show the end	'"}]', not '"'
							output[(i-1)] = output[(i-1)].Substring(0, output[(i-1)].Length-2);
//							Console.WriteLine("last post data (partial): "+output[(i-1)].Substring(output[(i-1)].Length-30));	//show the end	'"}]', not '"'
						}
					}
					try{
						if(output[(i-1)].EndsWith("\"")){
							string post_in_JSON = ((output[(i-1)].StartsWith("{")?"":"{")+output[(i-1)]+"}");	//add "{" and "}" for all not first elements, because first element contains '{"hash":"'

							try{
//								Console.WriteLine("post data (partial) begin: "+post_in_JSON.Substring(0, 30));	//show full post hash
//								Console.WriteLine("post data (partial) end: "+post_in_JSON.Substring(post_in_JSON.Length-30));	//show the end	'"}]', not '"'

								Post post_data = Newtonsoft.Json.JsonConvert.DeserializeObject<Post>(post_in_JSON);
//								Console.Write((accepted_posts+i)+": Try uploading post from request data: №i: "+i);
								Console.Write((accepted_posts+i)+": Try uploading ");
								bool status = _db.UploadPost(post_in_JSON, allowReput, bypassValidation);
								if(status==true){
									uploaded_posts += 1;
//									Console.Write(" TRUE!\n");
								}else{
//									Console.Write(" False.\n");										
								}
							}catch // (Exception except)
							{
								//Console.WriteLine("i: "+i+"except: "+except);
//								Console.WriteLine("post data (partial) begin: "+post_in_JSON.Substring(0, 30));					//show full post hash
//								Console.WriteLine("post data (partial) end: "+post_in_JSON.Substring(post_in_JSON.Length-30));	//show the end	'"}]', not '"'
								accepted_posts -= 1;				//remove this part from the number of accepted posts.
								if(i == output.Length){
									the_rest = (output[(i-1)].StartsWith("{")?"":"{")+output[(i-1)];				//add "{" in the last partial JSON, because "{" not added for first element.
								}
							}
						}else if(i == output.Length){
							the_rest = (output[(i-1)].StartsWith("{")?"":"{")+output[(i-1)];				//add "{" in the last partial JSON, because "{" not added for first element.
							accepted_posts -= 1;				//remove this part from the number of accepted posts.
							//add this in beginning of next part, in the future, to try parse and upload the whole post.
						}
					}
					catch{
						if(i == output.Length){
							the_rest = (output[(output.Length-1)].StartsWith("{")?"":"{")+output[(output.Length-1)];
							readData = the_rest;	//leave the rest in readData, and continue to read...
						}
						break;
					}
					if(i == output.Length){
						readData = the_rest;	//leave the rest in readData, and continue to read...
					}
				}
				accepted_posts += output.Length;
//				Console.WriteLine("Upload_Posts_from_JSON_string (rest): readData.Length: "+readData.Length);
				return new Object[]{(string)readData, (int)accepted_posts, (int)uploaded_posts};	//return Object array with different types.
			}
		}
		
		/*
			UploadPosts from JSON in cached file.
		*/
		private string[] Upload_Posts_From_Saved_File(string path, string headers, bool do_not_delete_cache_file = false){	//file path, and headers for response.
			string readData = "";			//string to return
			int accepted_posts = 0;			//number of accepted posts to return
			int uploaded_posts = 0;			//number of uploaded posts to return
			
			int _bufferSize = 16384;		//buffer size (default 16KB * 1024B/KB)
			bool is_first_block = true;		//number of block (will be incremented)

//			int large_limit_bytes 	= 	4*1024*1024;	//read file by this parts (bytes)
			int large_limit_bytes 	= 	131072;			//test
			
			if( new FileInfo( path ).Length == 0 )		//if file.Length == 0
			{
				string error = "TcpClient.cs. Upload_Posts_From_Saved_File. File "+path+" is empty! return.";	//save this error
				Console.WriteLine(error);																//show this in console.
				return new string[]{error, path};																			//and return as string.
			}

			StringBuilder stringBuilder = new StringBuilder();											//define stringBuilder.
			FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);				//Open the file in path in fileStream
			string trimmed_block = "";																	//define this string to save trimmed block in the end.

			using (StreamReader streamReader = new StreamReader(fileStream))							//use streamReader to read fileStream.
			{
				char[] fileContents = new char[_bufferSize];											//Define char array with length of _bufferSize.
				int charsRead = _bufferSize;															//define int variable to save readed chars (_bufferSize by defalt)
				
				while (charsRead > 0)																	//while chars reading, and not end of blocks
				{
					fileContents = new char[charsRead];													//resize char array up to readed chars

					try{																				//try
						charsRead = streamReader.Read(fileContents, 0, _bufferSize);						//to read characters by blocks _bufferSize.
					}
					catch{																				//on catch
						break;																				//just break
					}
					
					trimmed_block = (new string(fileContents)).Substring(0, charsRead);					//trim last block from null bytes, by substring this string
					stringBuilder.Append(trimmed_block);												//append each trimmed_block to string builder, and let his length growing.
					
					if(is_first_block == true){															//for first block (true at starting)
						readData = stringBuilder.ToString();											//get readData
						string [] splitted = readData.Split(new string[] { "[{\"hash\":\"" }, StringSplitOptions.None);	//make one split, to get two elements in array.
//						headers = splitted[1];																			//headers already exists (as parameter, or extract this).
						readData = "{\"hash\":\""+splitted[1];															//extract the part of JSON, and skip headers.
						stringBuilder.Length = 0;						//clear stringBuilder
						stringBuilder.Capacity = 0;						//clear stringBuilder
						stringBuilder.Append(readData);					//append modified readData in clean stringBuilder.
						is_first_block = false;							//Do not repeat this all anymore.
					}
					if(stringBuilder.Length >= (large_limit_bytes) || (streamReader.Peek() < 0)){	//if length of StringBuilder need to reduce, or if no any next character in file
						Object [] result_of_uploading = Upload_Posts_from_JSON_string					//upload posts from JSON
							(
								stringBuilder.ToString(),											//put the partial JSON from stringBuilder, as string.
								accepted_posts,														//put this statistic variable
								uploaded_posts,														//and this
								( ( streamReader.Peek() < 0 ) ? true : false )						//for last record true, when no any more characters in fileStream.
							)
						;

						readData 					= 	(string)	result_of_uploading[0];			//extract readData as result of uploading posts
						accepted_posts 				= 	(int)		result_of_uploading[1];			//extract the number of accepted posts
						uploaded_posts 				= 	(int)		result_of_uploading[2];			//extract the number of successfully uploaded posts.

						stringBuilder.Length = 0;				//clear string builder
						stringBuilder.Capacity = 0;				//Length and Capacity
						stringBuilder.Append(readData);			//and append the rest of result to stringBuilder, for another iteration.
						
						if( streamReader.Peek() < 0 ){												//for last part of file, with full JSON
//							Console.WriteLine("readData: "+readData);								//show readData
							if(readData == ""){														//check is this empty string, and if empty
								readData 				= 	(string)(								//make response with headers
																		((string)result_of_uploading[0] == "")
																			? (headers+"posts_uploading_large_data: ["+accepted_posts+", "+uploaded_posts+"]")
																			: headers+result_of_uploading[0]
															)
								;
							}
//							Console.WriteLine("readData: "+readData);	//show this,
							break;										//and break from cycle.
						}//else - continue
					}else{
						//in this case of small stringBuilder - continue filling the stringBuilder with JSON, in following next iterations.
					}
				}			//Do this all in cycle, while blocks available in file.
			}		//and do not using streamReader.
			if(do_not_delete_cache_file == false){File.Delete(path);}else{path = @"temp/Saved_cache_of_Large_POST_request_"+(string)((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString()+".txt";}/*//then delete file with cached JSON, or generate new filename, and don't delete previous file.*/ _db.ReadRefs_private();
			return new string[]{readData, path};	//and return response with headers.
		}
		
		private static bool data_still_writting = false;				//this will be true, while data is writting in NetworkStream, then - false.
		private static bool Network_stream_writting_error = false;		//this will be true, when error writting, and false, after .Dispose() NetworkStream.
		
		private static void async_callback(IAsyncResult async_result){	//this is async-callback for stream.BeginWrite(), this will be runned on start write.
			NetworkStream client_NetworkStream = (NetworkStream)(async_result).AsyncState;
			try{
				client_NetworkStream.EndWrite(async_result);	//this command will wait write all data, after stream.BeginWrite, and runing this callback function.
//				Console.WriteLine((DateTime.Now.ToString("HH:mm:ss.ffffff"))+", data_still_writting = false... no catch...");		
				data_still_writting = false;					//now, turn this false;
			}
			catch
			{
//				Console.WriteLine((DateTime.Now.ToString("HH:mm:ss.ffffff"))+", data_still_writting = false... catch...");		
				data_still_writting = false;					//now, turn this false;
				//if error, do nothing
			}
		}
		
		private bool Write_bytes_in_TCPSocket_block_by_block(NetworkStream stream, byte[] bu){
			
							//	Try to write response in TcpSocket - block-by-block:
							//int write_block_size = 1024;						//1		KB 	block		//working good
							//int write_block_size = 16384;						//16	KB 	block		//sometimes lagging
							//int write_block_size = 1*1024*1024;				//1		MB 	block		//working with good with async-write
//							int write_block_size = 16*1024*1024;				//1		MB 	block		//working with good with async-write

							//The MSDN documentation says the default size of the send and receive buffers for TcpClient is 8192 bytes, or 8K.
							//The documentation does not specify a limit as to how big these buffers can be.
							int write_block_size = 8192;					//8		KB 	block		//lagging without async-write

							int blocks 		= bu.Length / write_block_size;
							int rest_length = bu.Length % write_block_size;
							
							for(int block_num = 0; (block_num * write_block_size)<=bu.Length; block_num += 1){
							
//								int buffer_length = (
//										( ( bu.Length - ( block_num * write_block_size ) ) >= write_block_size )
//											? write_block_size
//											: ( bu.Length - ( block_num * write_block_size ) )
//									)
//								;

//								if(buffer_length == 0){
//									break;
//								}

//								//Console.WriteLine("block_num: "+block_num+", ( block_num * write_block_size ): "+( block_num * write_block_size )+", buffer_length: "+buffer_length);
//								byte[] write_buffer = new byte[buffer_length];
//								Buffer.BlockCopy(bu, (block_num * write_block_size), write_buffer, 0, buffer_length);
								
								
								while(
										!stream.CanWrite
									||	data_still_writting == true
									|| 	Network_stream_writting_error == true
								){			//if cann't write to the stream, or if data still writting
									Thread.Sleep(1);											//just sleep 1 millisecond
								}
								lock(_lock){
									try{
//										Console.WriteLine("lock...");
//										stream.Write(write_buffer, 0, write_buffer.Length);			//old code. 8KB, 16KB, and 1MB buffers - lagging.

											//BeginWrite(Byte[], Int32, Int32, AsyncCallback, Object)	
											//Begins an asynchronous write to a stream.
											data_still_writting = true;	//set this true
//											Console.WriteLine((DateTime.Now.ToString("HH:mm:ss.ffffff"))+", still_writting...");

//											stream.BeginWrite(write_buffer, 0, write_buffer.Length, new AsyncCallback(async_callback), stream);		//write copied array.

											stream.BeginWrite(
												bu,																//buffer
												(block_num * write_block_size),									//start offset in buffer
												( ( block_num == blocks ) ? rest_length : write_block_size),	//data length to write
												new AsyncCallback(async_callback),								//callback
												stream															//NetworkStream
											);																			//NOW OK.

									}catch	//(Exception ex)	//get exception to show it
									{

//										Console.WriteLine("stream.Write. Exception: \n"+ex);		//show exception
										
//										Console.WriteLine(
//											"stream.CanWrite:"+stream.CanWrite+"\n"+
//											"stream.CanTimeout:"+stream.CanTimeout+"\n"+
//										//	"stream.Socket:"+stream.Socket+"\n"+			//private somewhere, and cann't be showed
//										//	"stream.Writeable:"+stream.Writeable+"\n"+		//private somewhere, and cann't be showed
//											"stream.Length:"+stream.Length+"\n"+			//private somewhere, and cann't be showed
//											"stream.WriteTimeout:"+stream.WriteTimeout+"\n"
//										);
										data_still_writting = false;
										Network_stream_writting_error = true;
										return false;
									}
								}
//								Console.WriteLine("unlock...");
							}
				return true;
		}

        private void HandleClient(TcpClient client, bool is_lite_server)	//HandleClient on the full-server and on the lite-server.
        {
//            TcpClient client = (TcpClient)obj;				//TcpClient
            NetworkStream stream = client.GetStream();		//Get NetworkStream from TcpClient
            String readData = "";							//empty strin with request data, as string.
            bool noTcpDelay = (no_tcp_delay == "true");		//dont read config-3.json every time, again and again, and just compare with already extracted value.
            stream.ReadTimeout = noTcpDelay ? 15 : 100;		//set different TCP-delay
            byte[] buffer = new byte[16384];				//define empty buffer 16KBytes
            int len = -1;									//define this
            int contentLength = 0;							//this value will be updated from headers.
            List<byte> raw = new List<byte>();				//list with bytes of request-data.

			bool too_large = false;							//this will be true, when data of request is too large.
			int large_size = 0;								//bytesize, bytelength of large request data.
			int large_limit_bytes 	= 	4*1024*1024;		//this is a bytelimit for small requests.
//			int large_limit_bytes 	= 	131072;				//test small bytelimit.
			string headers = "";							//this string will contains headers of large request with posts in JSON which is sent on "../api/upload-posts/"
			bool posts_uploading_large_data = false;		//this will be true if Upload_Posts_From_TCP_Stream == true, and large data in POST-query on "../api/upload-posts/"
			int accepted_posts = 0;							//this is a number of accepted posts to uploading this.
			int uploaded_posts = 0;							//this is a number of posts, which are sucessfully added in DataBase, after UploadPost.
			int uploaded_bytes = 0;							//this is a number of uploaded bytes.
			int round_uploading = 0;						//this is a number of round uploading.
			int temp_readData_length = 0;					//this is a readData.Length, before all modifications of this string.
            do
            {
                try
                {
                    if (!noTcpDelay){
						if (raw.Count == 0 || raw[0] == (byte)'P'){
							Thread.Sleep(50);							//set delay
						}
					}
					len = stream.Read(buffer, 0, buffer.Length);						//try to read data from stream to buffer.
                    var block = System.Text.Encoding.UTF8.GetString(buffer, 0, len);	//if success convert this to UTF8
                    readData += block;													//append to readData-string
                    for (int i = 0; i < len; i++){
                        raw.Add(buffer[i]);												//and add bytes to list of bytes.
					}
					if(readData.Length >= ( large_limit_bytes ) ){						//if bytelimit to accept JSON.
						temp_readData_length = readData.Length;							//save readData.Length
						if(readData.Contains("/api/upload-posts/") && Upload_Posts_From_TCP_Stream == true){	//if this was been request to "/api/upload-posts/" and need to UploadPost from stream...
							posts_uploading_large_data = true;													//turn this to on, true.
							string [] splitted = readData.Split(new string[] { "[{\"hash\":\"" }, StringSplitOptions.None);	//make one split, and get two elements in array.
							headers = splitted[0];																//extract headers and save it
							readData = "{\"hash\":\""+splitted[1];												//extract JSON, and skip headers.
							uploaded_bytes += headers.Length + 1;												//headers + '[';
						}else if(posts_uploading_large_data == true || too_large == true){	//if flag already specified.
							//just continue
						}else if(Save_In_Cache_File_The_Large_POST_Request == true){		//if this mode
							string [] splitted = readData.Split(new string[] { "[{\"hash\":\"" }, StringSplitOptions.None);	//make one split, and two elements in array.
							headers = splitted[0];																//extract headers and save it
							too_large = true;																	//set this flag
							if(readData.Contains("/api/upload-posts/")){										//if this was been request to "/api/upload-posts/"
								Upload_Posts_From_File_With_JSON_Cache = true;									//turn this on to save the file.
							}
						}
						if(posts_uploading_large_data == true){													//if mode 1
							Object [] result_of_uploading = Upload_Posts_from_JSON_string(readData, accepted_posts, uploaded_posts, false);	//try to addPosts from readData.
							readData 					= 	(string)	result_of_uploading[0];					//update this value
							accepted_posts 				= 	(int)		result_of_uploading[1];					//update this value
							uploaded_posts 				= 	(int)		result_of_uploading[2];					//update this value
							raw 						= 	new List<Byte>(Encoding.ASCII.GetBytes(readData));	//update this value
							round_uploading += 1;																//increment it
							uploaded_bytes += temp_readData_length - ( ( round_uploading == 1 ) ? headers.Length : 0 ) - readData.Length;	//update uploaded_bytes
						}else if(too_large == true){															//if this just large file
							large_size += readData.Length;														//update large_size
							Save_Large_data_From_POST_Request(readData);										//write the part of request (or JSON) in file, to UploadPosts from this file.
							readData = "";																		//set this as empty string
							raw = new List<byte>();																//clear byte-array.
						}
					}
                }
                catch//		(Exception ex)	//if exception of reading socket...
                {
                    if (!noTcpDelay){		//if noTcpDelay
						if (raw.Count == 0 || raw[0] == (byte)'P'){	//and if this
							Thread.Sleep(50);							//just wait
						}
					}
					string extract_content_Length = ((posts_uploading_large_data == true) ? headers : readData);	//define the place of content-length
                    if (contentLength == 0 && extract_content_Length.Contains("Content-Length"))					//if this place contains Content-Length, and this have no value.
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(extract_content_Length, "Content-Length: [0-9]+");	//match content-length value
                        if (match.Success)																								//and if match success
                        {
                            contentLength = nbpack.NBPackMain.parse_number(match.Value.Split(' ')[1]);									//parse this as number from text
                            contentLength += extract_content_Length.Split(new[]{ "\r\n\r\n" }, StringSplitOptions.None)[0].Length;		//and add this value.
                        }
                    }
					if(posts_uploading_large_data == true){					//if need to read and upload from TCP-NetworkStream
						contentLength -= uploaded_bytes;						//this was been already uploaded, just update content-length
					}else if(too_large == true && contentLength !=0){		//if was been cached, and contentLength not null
						contentLength -= large_size;							//update content-length
					}
                    len = -1;												//set this as -1, to update in next iteration
                }						//end catch.
            }														//do this all
            while (len > 0 || raw.Count < contentLength);			//while was been possible to read data, or while raw.Count lesser than content.Length
			//This cycle try to read data block-by-block, and add this to request.
			//Large data was been processed block-by-block, and in the end of this cycle will be returned the rest, and need to process this.
			
			if(posts_uploading_large_data == true){	//if need to UploadPosts while reading TcpStream, here is the rest.
				try{									//try to do this
					Object [] result_of_uploading = Upload_Posts_from_JSON_string(readData, accepted_posts, uploaded_posts, true);	//upload posts from the rest.
					accepted_posts 			= 	(int)result_of_uploading[1];	//update this value
					uploaded_posts 			= 	(int)result_of_uploading[2];	//and this								
					readData 				= 	(string)(
															(
																(string)result_of_uploading[0] == "")													//if empty string returned.
																	? (headers+"posts_uploading_large_data: ["+accepted_posts+", "+uploaded_posts+"]")	//return this
																	: headers+result_of_uploading[0]													//or this
												);
					raw 					= 	new List<Byte>(Encoding.ASCII.GetBytes(readData));	//update raw - list of bytes.
				}
				catch (Exception exception){			//on catch
					Console.WriteLine("TcpServer.cs. HandleClient. Exception: "+exception);	//show exception
				}
				posts_uploading_large_data = false;		//and turn off this now.
			}else if(too_large == true && Upload_Posts_From_File_With_JSON_Cache == true){	//if this was been large data, and if this is was bee request to "/api/upload-posts/"
				//just read the cached query from cache file, and remove this file.
//				Console.WriteLine("After do-while, too_large. Cache the rest readData. readData.Length: "+readData.Length);
//				Console.WriteLine("After do-while, too_large. Cache rest readData end: "+readData.Substring(readData.Length-100));
					Save_Large_data_From_POST_Request(readData);				//write the rest JSON, in the cache-file, to UploadPost from this file.
				Console.WriteLine("Try to load posts from json in file.");
				string[] result_of_upload = Upload_Posts_From_Saved_File(path, headers, do_not_delete_cache_file); readData = result_of_upload[0]; path = result_of_upload[1]; //after this need to read this file, and upload posts from this (maybe need to use global variables path and do_not_delete_cache_file), and don't pass this as arguments.
				raw = new List<Byte>(Encoding.ASCII.GetBytes(readData));		//then update this value, from new readData value
					Upload_Posts_From_File_With_JSON_Cache = false;				//turn off this, now.
				Console.WriteLine("Done.");										//show this.
			}else if(too_large == true){							//if was been writted in cache another large request-data
				System.IO.File.WriteAllText(path, string.Empty);	//just clear the file
				too_large = false;									//and turn off this now, and do not do nothing.
			}
			//after this all...
            try
            {
                if ((is_lite_server == false && ConnectionAdded != null) || (is_lite_server == true && ConnectionAdded_lite!=null))	//ConnectionAdded!
                {
					HttpConnection connect_client = new HttpConnection(raw.ToArray(), readData, (ascii, utf8) =>		//ConnectionAdded!
                    {//send response
                        byte[] ba = ascii == null ? new byte[0] : Encoding.ASCII.GetBytes(ascii);	//headers
                        byte[] bu = utf8 == null ? new byte[0] : Encoding.UTF8.GetBytes(utf8);		//response
                        try
                        {	//send response for client.
                            stream.Write(ba, 0, ba.Length);			//write headers
							//now need to write repsonse...
//                            stream.Write(bu, 0, bu.Length);		//cann't write 50 MB, and show catch. System.IO.IOException, System.Net.Sockets.SocketException

							bool was_writted = Write_bytes_in_TCPSocket_block_by_block(stream, bu);	//use separate method, to write response block-by-block
//                            stream.Flush();							//however, because NetworkStream is not buffered, it has no effect on network streams. 
//							stream.Dispose(true);						//Not working
							stream.Dispose();							//clear memory from stream.

							if( was_writted == false ){					//if error
								client.Close();							//close client
								Network_stream_writting_error = false;	//and turn this off
							}
                        }
                        catch	(Exception ex)							//if exception
                        {
							Console.WriteLine("TcpServer.cs. HandleClient. ConnectionAdded. 1-st functino (ascii).\nException:\n"+ex); //show this
                        }
                        finally
                        {
							stream.Dispose();							//clear memory from stream.
                            client.Close();								//and close client
                        }
                    }, (ascii, bytes) =>
                    {
                        byte[] ba = Encoding.ASCII.GetBytes(ascii);
                        byte[] bu = bytes;
                        try
                        {
                            stream.Write(ba, 0, ba.Length);
//                            stream.Write(bu, 0, bu.Length);		//cann't write 50 MB, and show catch. System.IO.IOException, System.Net.Sockets.SocketException

							bool was_writted = Write_bytes_in_TCPSocket_block_by_block(stream, bu);
//                            stream.Flush();							//however, because NetworkStream is not buffered, it has no effect on network streams. 
//							stream.Dispose(true);						//Not working
							stream.Dispose();							//clear memory from stream.

							if( was_writted == false ){					//if error
								client.Close();							//close client
								Network_stream_writting_error = false;	//and turn this off
							}

                        }
                        catch	(Exception ex)							//if exception
                        {
							Console.WriteLine("TcpServer.cs. HandleClient. ConnectionAdded. 2-nd function (bytes).\nException:\n"+ex);	//show this
                        }
                        finally
                        {
							stream.Dispose();							//clear memory from stream.
                            client.Close();
                        }
                    });
					
                    if(is_lite_server == false && ConnectionAdded != null){
						ConnectionAdded(connect_client);
					}else if(is_lite_server == true && ConnectionAdded_lite!=null){
						ConnectionAdded_lite(connect_client);
					}
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Something is wrong with connection between your browser and nanoboard client...");
                Console.WriteLine("But that's ok, don't worry.");
				Console.WriteLine("Exception: "+ex);
            }
            finally
            {
				stream.Dispose();							//clear memory from stream.
                client.Close();
            }
        }
    }
}
