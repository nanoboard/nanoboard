using Newtonsoft.Json;
using NDB;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text;

namespace NDB
{
	//In client 3.1 - undefined comparer for .NET Framework 4.0.
	//Define this.
	public class DTComparer : IComparer<DateTime> {
        public int Compare(DateTime x, DateTime y) {
			//System.Threading.Thread.Sleep(5);
//            Console.WriteLine(x+" < - > "+y+"; x.Year = "+x.Year+", y.Year = "+y.Year);	//too many comparations after loading the page, if GetLastNAnswers fast = false;
            long ticks = (x - y).Ticks;
            if (ticks < 0) return -1;
            if (ticks > 0) return 1;
            return 0;
        }
    }
    /*
        Class that operates on posts database.
    */
    public class PostDb
    {
        private readonly string _index = "index-3.json";  // name of index file
        private const string DiffFile = "diff-3.list";    // name of file that keeps changes to the index
		private bool _index_locked = false;
		private bool DiffFile_locked = false;
        private readonly string DeletedStub = "post_was_deleted";		// this message returned when someone asks for deleted post
//        private readonly string ReportedStub = "post_is_reported";		// this message returned when someone asks for deleted post
        private const string DataPrefix = "";							// will be prepended to a data chunk name
        private const string DataSuffix = ".db3";						// data chunk extension
        private string _data = "0.db3";									// initial data chunk filename
        private int _dataIndex = 0;										// initial data chunk name index
        private int _dataSize = 0;										// size of current data chunk will be here
        private const int DataLimit = 1024 * 1024 * 1024;				// 1GB allowed to be stored inside one chunk before creating new one
        private const int CacheLimit = 1000;							// how many posts to keep in memory (reduce disk read operations)

        private Dictionary<string, DbPostRef> _refs; 			// index entries by hash
        private Dictionary<string, List<DbPostRef>> _rrefs; 	// replies by hash
        private HashSet<string> _deleted; 						// hashes entries marked as "deleted_once" (Undelete is possible for this, because this still contains in DataBase).
        private HashSet<string> _free; 							// hashes of entries marked as "post_was_deleted_forever", space of which is not used now (empty space)
        private List<string> _ordered; 							// just all hashes from index.json in the same order as in file
        private Dictionary<string,Post> _cache; 				// cached posts by hash
		public Dictionary<string, DateTime> hash_DateTime = new Dictionary<string, DateTime>();	//_refs hash -> DateTime of post with this hash. Need to sort _refs.
        private object _lock = new object();					//object to lock variables for rewrite values, while this values used.

        private Dictionary<string, List<string>> _reported; 				// reasons of reports for post by post.hash
        private Dictionary<string, List<string>> _GetReplies_cache_list; 	// cached sorted replies for post with hash, for appendText = false;
		public static bool allowReput = false;
//		4 cases:									Properties
//			1. Post was not exists and new.			!_refs.ContainsKey(post.hash)
//			2. Post already exists in database.		_refs.ContainsKey(post.hash) && _ordered.Contains(post.hash)
//			3. Post is "deleted_once".				_refs.ContainsKey(post.hash) && _refs[post.hash].deleted = true && _deleted.Contains(post.hash) 
//			4. Post is "post_was_deleted_forever".	_refs.ContainsKey(post.hash) && _refs[post.hash].file = "post_was_deleted_forever,0.db3" _free.Contains(post.hash) && ((post_space_nulls_was_rewritted == true) ? ((post.offset == 0) && (post.length == 0)) : (true))

        public PostDb(bool EnableAllowReput = false)
        {	allowReput = EnableAllowReput;	//set allowReput from specified parameter or from default, after create instance of new PostDb()
            /* 
                Determining which data chunk file to use for new posts:
                for example check 0.db, 1.db, 2.db etc while file with size less than Limit
                will be found or stop at not existing yet file name like 3.db to use it for new posts.
            */
            for (int i = 0; i < int.MaxValue; i++)
            {
                _data = DataPrefix + i + DataSuffix;
                _dataIndex = i;

                if (!File.Exists(DataPrefix + i + DataSuffix))
                {
                    break;
                }
                else
                {
                    long length = new System.IO.FileInfo(_data).Length;
                    _dataSize = (int)length;

                    if (length > DataLimit)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            _refs = new Dictionary<string, DbPostRef>();
            _rrefs = new Dictionary<string, List<DbPostRef>>();
            _deleted = new HashSet<string>();
            _free = new HashSet<string>();
            _cache = new Dictionary<string, Post>();
            _GetReplies_cache_list 		= new Dictionary<string, List<string>>();			//cached sorted replies for post, by hash, for appendText = false;
            _ordered = new List<string>();
            _reported = new Dictionary<string, List<string>>();
            ReadRefs();
            
            /*
                Hardcoded root post, categories post and example category post - adding them to DB below
            */
            var initialPostsStr = @"[{'hash':'bdd4b5fc1b3a933367bc6830fef72a35','message':'W2Jd0JrQkNCi0JXQk9Ce0KDQmNCYWy9iXQrQp9GC0L7QsdGLINGB0L7Qt9C00LDRgtGMINC90L7QstGD0Y4g0LrQsNGC0LXQs9C+0YDQuNGOLCDQvtGC0LLQtdGC0YzRgtC1INC90LAg0Y3RgtC+INGB0L7QvtCx0YnQtdC90LjQtS4K0J7RgtCy0LXRgtGM0YLQtSDQvdCwINC+0LTQvdGDINC40Lcg0LrQsNGC0LXQs9C+0YDQuNC5LCDRh9GC0L7QsdGLINGB0L7Qt9C00LDRgtGMINGC0LDQvCDRgtGA0LXQtC4=','replyTo':'f682830a470200d738d32c69e6c2b8a4'},{'hash':'cd94a3d60f2f521806abebcd3dc3f549','message':'W2Jd0JHRgNC10LQv0KDQsNC30L3QvtC1Wy9iXQ==','replyTo':'bdd4b5fc1b3a933367bc6830fef72a35'},{'hash':'cd94a3d60f2f521806abebcd3dc3f549','message':'W2Jd0JHRgNC10LQv0KDQsNC30L3QvtC1Wy9iXQ==','replyTo':'bdd4b5fc1b3a933367bc6830fef72a35'},{'hash':'bdd4b5fc1b3a933367bc6830fef72a35','message':'W2Jd0JrQkNCi0JXQk9Ce0KDQmNCYWy9iXQrQp9GC0L7QsdGLINGB0L7Qt9C00LDRgtGMINC90L7QstGD0Y4g0LrQsNGC0LXQs9C+0YDQuNGOLCDQvtGC0LLQtdGC0YzRgtC1INC90LAg0Y3RgtC+INGB0L7QvtCx0YnQtdC90LjQtS4K0J7RgtCy0LXRgtGM0YLQtSDQvdCwINC+0LTQvdGDINC40Lcg0LrQsNGC0LXQs9C+0YDQuNC5LCDRh9GC0L7QsdGLINGB0L7Qt9C00LDRgtGMINGC0LDQvCDRgtGA0LXQtC4=','replyTo':'f682830a470200d738d32c69e6c2b8a4'},{'hash':'f682830a470200d738d32c69e6c2b8a4','message':'e1dlbGNvbWUgdG8gTmFub2JvYXJkfQ==','replyTo':'00000000000000000000000000000000'}]";
            var initialPosts = JsonConvert.DeserializeObject<Post[]>(initialPostsStr);

            foreach (var p in initialPosts)
            {
                PutPost(p, allowReput, true);	//PotPost without allowReput for default posts.
            }
        }

        public Post GetNthPost(int n)
        {
            if (n >= _ordered.Count) return null;
            return GetPost(_ordered[n]);
        }

        /*
            Returns recursive replies count for specified post (except deleted)
        */
        public int GetThreadSize(string hash)
        {
            if (!_rrefs.ContainsKey(hash)) return 0;
            int count = 0;
            var stack = new Stack<List<DbPostRef>>();
            stack.Push(_rrefs[hash]);

            while (stack.Count > 0)
            {
                var elem = stack.Pop();
                count += elem.ToArray().Where(r => !r.deleted).Count();

                foreach (var reply in elem.ToArray())
                {
                    if (_rrefs.ContainsKey(reply.hash))
                        stack.Push(_rrefs[reply.hash]);
                }
            }

            return count;
        }

        public int GetPostCount()
        {
            return _ordered.Count;
        }

        /*
            Increases dataSize counter and checks if it still meets the Limit,
            if not - change filename of current data chunk.
        */
        private void IncreaseCheckDataSize(int length)
        {
            _dataSize += length;

            if (_dataSize > DataLimit)
            {
                _dataIndex += 1;
                _data = DataPrefix + _dataIndex + DataSuffix;
                _dataSize = 0;
            }
        }

        /*
            Add new index entry to the instance collections.
        */
        private void AddDbRef(DbPostRef r)				//add specified DbPostRef - r to database.
        { 
			if(_refs.ContainsKey(r.hash)){						//if this reference already exists in database
				return;												//don't add it.
			}else{												//else
				_refs[r.hash] = r;									//add to _refs
			}

			if (r.deleted && !r.file.StartsWith("post_was_deleted_forever,") && r.length > 0){	//	if "deleted_once", and not "post_was_deleted_forever"
				_deleted.Add(r.hash);												//	just mark as deleted_once, and continue, like for undeleted post
            }else if(r.file.StartsWith("post_was_deleted_forever,") || r.length == 0){					//	"post_was_deleted_forever" posts (have length is 0);
				
				if(!_free.Contains(r.hash)){							//and if this not contains in _free
					_free.Add(r.hash);										//	if this offset-length (file area) is not used yet, mark it as free, add to _free
				}
				return;																//	don't add to _ordered, and don't add to replies.
			}

            // ensure there is a list for replies: 
            if (!_rrefs.ContainsKey(r.replyTo)){				//	if this was been first reply to post with hash r.replyTo
                _rrefs[r.replyTo] = new List<DbPostRef>();		//	add empty list to that hash.
			}
			_rrefs[r.replyTo].Add(r); 							//	and add as reply to it's parent, then
			
			if(!_ordered.Contains(r.hash)){
				_ordered.Add(r.hash);
			}
        } 

        // reading diff
        /*
            This method is called while reading diff file
            to update index entries collection to the most recent state.
        */
		private void UpdateDbRef(DbPostRef r)						//Update DbPostRef to r, from record of diff-file.
		{
			if (!r.deleted && _deleted.Contains(r.hash)) {	//if post of this DbPostRef is not "deleted_once", but not contains in _deleted
				_deleted.Remove(r.hash);						//this is not deleted.
			}
			
			if(r.file.StartsWith("post_was_deleted_forever,") || r.length == 0){ //if post was been "post_was_deleted_forever" or if r.length == 0 (message length is null)

				if(!_free.Contains(r.hash)){			//and if _free still not contains this
					_free.Add(r.hash);					//add 		hash 	to 		_free
				}
				if(_deleted.Contains(r.hash)){			//if this post in _deleted
					_deleted.Remove(r.hash);						//remove there.
				}
			}else{	//else if not "post_was_deleted_forever"

				if (r.deleted && r.length > 0){			//if "deleted_once" and message.length over 0 bytes.
					_deleted.Add(r.hash);					//just add to _deleted.
				}
				//Now, need to add it to replies in _rrefs
				if (!_rrefs.ContainsKey(r.replyTo)){			//if r.replyTo not contains in _rrefs
					_rrefs[r.replyTo] = new List<DbPostRef>();		//add empty list.
				}
				//if replies to this r.replyTo, already exists,
				if(_rrefs[r.replyTo].Contains(r)){
					_rrefs[r.replyTo].Add(r);				//add r to _rrefs, only if r.hash not contains there, else don't add
				}
				if(!_ordered.Contains(r.hash)){
					_ordered.Add(r.hash);					//add r to _ordered, only if r.hash not contains there, else don't add
				}
			}

			_refs[r.hash] = r;										//and anyway add this to _refs.
		}

        /*
            Reads index file and diff file, updates index file.
        */
        private void ReadRefs()	//read refs from files _index, and diff, and load this refs.
        {
			lock(_lock){if (File.Exists (_index)) 
			{
				while(_index_locked){System.Threading.Thread.Sleep(10);}
				_index_locked = true;
				var indexString = File.ReadAllText(_index);
				_index_locked = false;

				var refs = JsonConvert.DeserializeObject<Index> (indexString).indexes;

				foreach (var r in refs) 
				{
					AddDbRef (r);
				}
			}

            if (File.Exists(DiffFile))
            {
				while(DiffFile_locked){System.Threading.Thread.Sleep(10);}
				DiffFile_locked = true;
                var diffs = File.ReadAllLines(DiffFile);
				DiffFile_locked = false;

				Dictionary<string, DbPostRef> unique_diffs = new Dictionary<string, DbPostRef>();	//here will be only last values from diff-file.
				
                foreach (var diff in diffs)
                {
                    var r = JsonConvert.DeserializeObject<DbPostRef>(diff);	//read DbPostRef from as record from diff-file
					unique_diffs[r.hash] = r;	//update if exists or add
                }

				foreach(KeyValuePair<string, DbPostRef> diff in unique_diffs){	//for each unique value
					var r = diff.Value;											//get last DbPostRef
					UpdateDbRef(r);											//and update DbPostRef in database from this last diff-record.
                }
				
				//rewrite diff-file.
				while(DiffFile_locked){System.Threading.Thread.Sleep(10);}
				DiffFile_locked = true;
                File.WriteAllText(DiffFile, "");
				DiffFile_locked = false;
            }
			
			//Read reports from txt-files in "reports"-folder.
			string subPath = "reports";
			bool exists = System.IO.Directory.Exists(subPath);
			if(!exists){	//create "reports"-folder for reports, if this does not exists
				System.IO.Directory.CreateDirectory(subPath);
			}else{			//read reports from the files in "reports"-folder
				try 
				{
					string[] files = Directory.GetFiles(@"reports", "*.txt");
//					Console.WriteLine("The number of txt-files in reports folder is {0}.", files.Length);
					foreach (string file in files) 
					{
//						Console.WriteLine("file: "+file);
						string hash = file.Substring(8, 32);	//get hash of reported post from filename.
						List<string> reports = new List<string>(File.ReadAllLines(file));
						
//						//show each report string from file
//						foreach (string report in reports){
//							Console.WriteLine("report: "+report);
//						}

						//add reports from file to "_reported" variable with hash-table, dictionary
						if(!_reported.ContainsKey(hash) && System.Text.RegularExpressions.Regex.IsMatch(hash, @"\A\b[0-9a-fA-F]{32}\b\Z")){ //if this not contains, and if there is md5-hash in filename with 32 hexadecimal characters.
							_reported[hash] = reports;
						}

						var sorted_reported = _reported.OrderByDescending(x => x.Value.Count); 				//order by lists.Count (number of reports);
						_reported = sorted_reported.ToDictionary(pair => pair.Key, pair => pair.Value);		//to Enumerable list - Dictionary
					}
				} 
				catch (Exception e) 
				{
					Console.WriteLine("The process of reading txt-files with reports was been failed: {0}", e.ToString());
				}				
			}

			Flush();}
        }
		public void ReadRefs_private(){ReadRefs();}
        #region INanoDb implementation

        [Obsolete]
        public string[] GetAllHashes()
        {
            return _ordered.Where(k => !_deleted.Contains(k)).ToArray();
        }

        /*
            Returns count of not deleted posts
        */
        public int GetPresentCount()
        {
            return _ordered.Where(k => !_deleted.Contains(k)).ToArray().Length;
        }

        /*
            Returns slice of not deleted (false) posts
        */
        public Post[] RangePresent(int skip, int count, bool append_text = false, bool show_deleted = false)
        {
            return _ordered.Where(k => ((show_deleted)?(k!=null):(!_deleted.Contains(k)))).Skip(skip).Take(count).Select(h => GetPost(h, append_text)).ToArray();
        }

		//From client 3.1
        public string[] RangePresent_string(int skip, int count, string only_hashes, bool show_deleted = false)
        {
//			try{
				if(only_hashes.Contains("hashes_with_bytelength")){
					while(_index_locked){System.Threading.Thread.Sleep(10);}
					_index_locked = true;
					var indexString = File.ReadAllText(_index);
					_index_locked = false;
					
					var refs = JsonConvert.DeserializeObject<Index> (indexString).indexes;
					List<string> hashes_bytes = new List<string>();
				
					var hashes = _ordered.Where(k => ((show_deleted)?(k!=null):(!_deleted.Contains(k)))).Skip(skip).Take(count).ToArray();
				
					foreach (var r in refs) 
					{
						if(hashes.Contains(r.hash)){
							hashes_bytes.Add(
								JsonConvert.SerializeObject(
									new string[]	{
										r.hash
										,
										(r.length+32).ToString()			//Just get post bytelength from JSON-file with indexes (_index)
									}/*, Formatting.Indented */
								)
							);
						}
					}
					return hashes_bytes.ToArray();							//return hashes with bytelength
				}else{
					return _ordered.Where(k => ((show_deleted)?(k!=null):(!_deleted.Contains(k)))).Skip(skip).Take(count).ToArray();	//return hashes only
				}
//			}
//			catch(Exception ex){
//				Console.WriteLine(ex);
//				return _ordered.Where(k => !_deleted.Contains(k)).Skip(skip).Take(count).ToArray();	//return something to see catch exception
//			}
        }
        
		/**
			Add post to _GetReplies_cache_list, after sucessfully PutPost or ReputPost.
			DownloadingPosts, UploadingPosts, parsing and solving captcha using PutPost method to add post.
			So need just need to run this method, before PutPost will return true.
		*/
		private void Update_GetReplies_cache(string hash, bool need_add = false){	//need_add is false, by default. true, for add hash in _GetReplies_cache_list.
			if(need_add){																//if need_add
				if (!_GetReplies_cache_list.ContainsKey(_refs[hash].replyTo)){				//and if this was been first reply to post with hash _refs[hash].replyTo
					_GetReplies_cache_list[_refs[hash].replyTo] = new List<string>();		//create new empty list there
				}
				_GetReplies_cache_list[_refs[hash].replyTo].Add(hash);	//and add hash there, in that list.
			}
			return;	//and return, or just return if need_add = false, and ReputPost is failed.
		}
        /*
            Adds post back to DB after deletion.
        */
        private bool ReputPost(Post p)	//reput Post p
        {
			try{
				lock (_lock)	//lock database
				{
					var bytes = Encoding.UTF8.GetBytes(p.message.FromB64());	//get post message
					DbPostRef r;					//define port reference
					//format: {"h":"bdd4b5fc1b3a933367bc6830fef72a35","r":"f682830a470200d738d32c69e6c2b8a4","o":0,"l":230,"d":false,"f":"0.db3"}
					if(_refs.ContainsKey(p.hash) && !_free.Contains(p.hash)){	//if post.hash already contains in _refs and not "post_was_deleted_forever", this post already exists, including posts "deleted_once". But this post can be damaged.
						r = _refs[p.hash];				//get this reference
						
						//Now, need to compare post, because this can be writted in database - with errors.
						if(r.length == bytes.Length){						//if data.length is equals with post-byteLength - try to compare post-message.
							var post_in_database = GetPost(p.hash);				//get post
							if(post_in_database.message == p.message){				//if message base64 of post is equals of base64 post-message
								return false;											//don't add existing post, including "deleted_once" posts, and return false.
							}
							else{												//else
								FileUtil.Write(r.file, bytes, r.offset);			//rewrite post-data from bytes in r.offset, if post-data was been damaged in database.
								return true;
							}
						}
						//else, continue to reput this post..
					}else{							//else if p.hash was not been found in _refs, this is new post or "post_was_deleted_forever"-post.
						
						r = new DbPostRef();			//create new and empty DbPostRef()
						r.hash = p.hash;				//add hash from post.hash
						r.replyTo = p.replyto;			//add replyTo from p.replyto
						r.offset = ((_refs.ContainsKey(p.hash)) ? _refs[p.hash].offset : FileUtil.Append(_data, bytes));	//take offset from previous record, if this "post_was_delete_forever", or write new post in database, and add new offset to DbReference
						r.file = _data;					//add database file
						//new database reference created.
					
					}
	
					r.length = bytes.Length;		//add bytelength to reference
                
					if(r.deleted != false && r.deleted != true){	//if r.deleted not true and not false, and if this is undefined or null
						r.deleted = false;								//set this as false. New post is not "deleted_once".
						_deleted.Remove(r.hash);		//remove from posts "deleted_once", if this was been "deleted_once"
					}
					
					_refs[p.hash] = r;				//update post in _refs
	
					// reput posts which was been "post_was_deleted_forever";
					if (_free.Contains(r.hash))			//if post "post_was_deleted_forever"
					{
						_free.Remove(r.hash);			//now this not "post_was_deleted_forever", because ReputPost
						if(r.length != 0){FileUtil.Write(r.file, bytes, r.offset);	/*//write post in DataBase.*/} else {r.offset = FileUtil.Append(r.file, bytes);	/*//write post in database, and add new offset*/}
						UpdateDbRef(r);//						AddDbRef(r);	//add new reference to database

						//write diffs
						while(DiffFile_locked){System.Threading.Thread.Sleep(10);}	
						DiffFile_locked = true;	
						FileUtil.Append(DiffFile, JsonConvert.SerializeObject(r /*, Formatting.Indented*/) + "\n");	//lock diff-file and write this post.	
						DiffFile_locked = false;	
	
//						ReadRefs();	//update database from _index and DiffFile
						return true;	//and return true	
					}
					// else try to find best empty area in some file
					else if (_free.Any())	//else try to write this post in empty space instead "post_was_deleted_forever" posts.
					{
						DbPostRef best = null;		//create empty database reference
						int min = int.MaxValue;			//set min to MaxValue.
						var freeArr = _free.ToArray();	//get array with all "post_was_deleted_forever" posts
	
						for (int i = 0; i < _free.Count; i++)	//for each "post_was_deleted_forever" posts
						{
							var fr = _refs[freeArr[i]];			//get reference from _refs
	
							if (fr.length >= r.length)					//if length of "post_was_deleted_forever"-post >= r.length (length of post which need to write)
							{
								int diff = r.length - fr.length;		//save difference between lengths

								if (diff < min)							//if difference leser than min
								{
									min = diff;							//now this is min
									best = fr;							//and save this reference as the best
								}
							}
						}
						if (best != null)								//if best not null
						{
							best.length = 0;							//set length of this as 0
							_free.Remove(best.hash);					//remove from _free
							r.offset = best.offset;						//set r.offset as best.offset
							string database_file = best.file.Substring(25, best.file.Length-25);			//substring "post_was_deleted_forever," and extract database filename
							FileUtil.Write(database_file, bytes, r.offset);		//wiite bytes of current post to current database, because best.file = "post_was_deleted_forever,"+database_file
							r.file = database_file;								//_data, because best.file = deleted.forever.
							best.offset = 0;									//set offset of this as 0
							//best.file = null;									//don't replace best.file to null

							AddDbRef(r);								//add db reference

							while(DiffFile_locked){System.Threading.Thread.Sleep(10);}					//while diff-file locked sleep 10 milliseconds
							DiffFile_locked = true;														//then, lock diff-file
							FileUtil.Append(DiffFile, 	JsonConvert.SerializeObject(best /*, Formatting.Indented */) + "\n"+
														JsonConvert.SerializeObject(r /*, Formatting.Indented */) + "\n"			//and write there this, by one command
							);
							DiffFile_locked = false;													//unlock diff-file
						
//							ReadRefs();	//update database from _index and DiffFile
							return true;
						}
					}
					// no apropriate space was found - extend current data chunk
					r.offset = FileUtil.Append(_data, bytes);
					r.file = _data;
					IncreaseCheckDataSize(r.length);

					AddDbRef(r);

					while(DiffFile_locked){System.Threading.Thread.Sleep(10);}	
					DiffFile_locked = true;	
					FileUtil.Append(DiffFile, JsonConvert.SerializeObject(r /*, Formatting.Indented */) + "\n");	
					DiffFile_locked = false;	
				
					return true;	
				}
			}catch(Exception ex){
				Console.WriteLine("PostDb.cs. ReputPost. Exception: "+ex);
				return false;
			}
        }	

		public bool PutPostBusy = false;
		
        public bool PutPost(Post p, bool allowReput = false, bool bypassValidation = false)	//temporary reput false, and was been full captcha validation.
        {
			try{
				while(PutPostBusy){System.Threading.Thread.Sleep(1);}
				
				PutPostBusy = true;
				
	            if (!PostsValidator.Validate(p, bypassValidation)){ 		// do not add posts that fail validation
                    PutPostBusy = false;
					return false;
				}

				lock (_lock)
				{
					if (_refs.ContainsKey(p.hash) && !allowReput){ 	//	Do not add existing posts, including "deleted_once" posts.
																	//	Do not add post hash of which is equals of existing post.
																	//	Do not add post which was been "post_was_deleted_forever".
						PutPostBusy = false;
						return false;
					}else if(_refs.ContainsKey(p.hash) && allowReput){	//if need to reput existing post, including "deleted_once", and "post_was_deleted_forever" (when database is damaged, for example)
						bool reput_status = ReputPost(p);							//just try to reput it.
						PutPostBusy = false;
						Update_GetReplies_cache(p.hash, reput_status);	//use received status to add post in _GetReplies_cache, or not.
						return reput_status;
					}
					
					//	else if this is new post
					//	start creating new index entry
					var r = new DbPostRef();									//create new index entry
					r.hash = p.hash;											//add hash
					r.replyTo = p.replyto;										//add replyTo
					var bytes = Encoding.UTF8.GetBytes(p.message.FromB64());	//add bytes of message
					r.length = bytes.Length;									//add length
					
					// try to find some empty space in db file chunks
					if (_free.Any())
					{
						DbPostRef best = null;
						int min = int.MaxValue;
						var freeArr = _free.ToArray();
						
						for (int i = 0; i < _free.Count; i++)
						{
							var fr = _refs[freeArr[i]];
							
							if (fr.length >= r.length)
							{
								int diff = r.length - fr.length;
								
								if (diff < min)
								{
									min = diff;
									best = fr;
								}
							}
						}	
						
						// found some useful place - write post's message into it,
						// also update diff file immediately
						if (best != null)
						{
							best.length = 0;							//set length of this as 0
							_free.Remove(best.hash);					//remove from _free
							r.offset = best.offset;						//set r.offset as best.offset
							string database_file = best.file.Substring(25, best.file.Length-25);			//substring "post_was_deleted_forever," and extract database filename
							FileUtil.Write(database_file, bytes, r.offset);		//wiite bytes of current post to current database, because best.file = "post_was_deleted_forever,"+database_file
							r.file = database_file;								//_data, because best.file = deleted.forever.
							best.offset = 0;									//set offset of this as 0
							//best.file = null;									//don't replace best.file to null

							AddDbRef(r);								//add db reference

							while(DiffFile_locked){System.Threading.Thread.Sleep(10);}					//while diff-file locked sleep 10 milliseconds
							DiffFile_locked = true;														//then, lock diff-file
							FileUtil.Append(
								DiffFile,				JsonConvert.SerializeObject(best /*, Formatting.Indented */) + "\n"+
														JsonConvert.SerializeObject(r /*, Formatting.Indented */) + "\n"			//and write there this, by one command
							);
							DiffFile_locked = false;													//unlock diff-file
							
							PutPostBusy = false;
							Update_GetReplies_cache(p.hash, true); //add post in _GetReplies_cache.
							return true;
						}
					}
					// no appropriate space was found - extend current data chunk file
					r.offset = FileUtil.Append(_data, bytes);
					r.file = _data;
					IncreaseCheckDataSize(r.length);
					AddDbRef(r);
					
					while(DiffFile_locked){System.Threading.Thread.Sleep(10);}
					DiffFile_locked = true;
					FileUtil.Append(DiffFile, JsonConvert.SerializeObject(r /*, Formatting.Indented */) + "\n");
					DiffFile_locked = false;
				}	
				Update_GetReplies_cache(p.hash, true); //add post in _GetReplies_cache.
				PutPostBusy = false;
				return true;
			}catch(Exception ex){
				Console.WriteLine("PostDb.cs. PutPost. Exception: "+ex);
				return false;
			}
        }

        /*
            Mark post as deleted, erase it's message bytes (write zeros to the file),
            update diff file
        */
        public int DeletePost(string hash)
        {
            if ( !_refs.ContainsKey(hash) ){		//if post cann't be deleted
				Console.WriteLine("Post cann't be deleted and not contains in _refs.");
                return 0;							//do nothing and just return this status code.
			}
			else if ( _deleted.Contains(hash) ){	//if post already "deleted_once", and deleted again.
				//remove from replies
				if (_rrefs.ContainsKey(_refs[hash].replyTo)){				//if this was been a reply for another post
					if(_rrefs[_refs[hash].replyTo].Count != 0){
						(_rrefs[_refs[hash].replyTo]).RemoveAll(r => (r.hash == hash));		//remove this from replies.
					}else{
						_rrefs.Remove(_refs[hash].replyTo);
					}
				}
				//and remove from _GetReplies_cache_list
				if(_GetReplies_cache_list.ContainsKey(_refs[hash].replyTo)){			//if reply hash contains in _GetReplies_cache_list
					if(_GetReplies_cache_list[_refs[hash].replyTo].Contains(hash)){		//and if hash contains in list
						_GetReplies_cache_list[_refs[hash].replyTo].Remove(hash);		//remove hash from list
					}
					if(_GetReplies_cache_list[_refs[hash].replyTo].Count == 0){			//if list length == 0
						_GetReplies_cache_list.Remove(_refs[hash].replyTo);				//remove record with reply hash - from this dictionary.
					}
				}
	
				//Remove this from DB...
				_deleted.Remove(hash);				//this is not "deleted_once"
				_free.Add(hash);					//this is "post_was_deleted_forever" now
				_refs[hash].deleted = true;														//this is deleted.
				_refs[hash].file = "post_was_deleted_forever,"+_refs[hash].file;				//change db-filename by adding this string for "post_was_deleted_forever"
				
				FileUtil.Write(_data, new byte[_refs[hash].length], _refs[hash].offset);		//fill post content by nulls, in DataBase - 0.db3
				//_refs[hash].length	= 0;		//0 in length.
				//_refs[hash].file = null;			//null in file.
				
				while(DiffFile_locked){System.Threading.Thread.Sleep(10);}
				DiffFile_locked = true;
				FileUtil.Append(DiffFile, JsonConvert.SerializeObject(_refs[hash] /*, Formatting.Indented */) + "\n");		//and write differences in diff-3.list
				DiffFile_locked = false;
				
				_ordered.Remove(hash);			//from _ordered.
				_cache.Remove(hash);			//and from _cache.
				
				DeleteReportsForPostHash(hash);	//also, delete all reports for this post, if exists.
				
				return 2;						//and return this status code.
			}else{ //else - set "deleted_once" and leave this post in database.
				
				_refs[hash].deleted = true;		//mark as "deleted_once"
				
				while(DiffFile_locked){System.Threading.Thread.Sleep(10);}	
				DiffFile_locked = true;	
				FileUtil.Append(DiffFile, JsonConvert.SerializeObject(_refs[hash]/*, Formatting.Indented */) + "\n");		//write differences in diff-3.list	
				DiffFile_locked = false;	
				
				if (_cache.ContainsKey(hash)){									//if this was been in _cache
					_cache[hash].message = DeletedStub+_cache[hash].message;	//remove it
				}
				_deleted.Add(hash);				//add to another "deleted_once"
				return 1;						//and return this status-code.
			}
        }
		
        /*
			Undelete the post, which was been deleted_once
        */
        public bool UndeletePost(string hash)
        {
            if (!_refs.ContainsKey(hash)){
                return false;
			}	
            var r = _refs[hash];
			
            if (r.deleted)	//if post "deleted_once"
            {
				r.deleted = false;				//remove "deleted_once"-mark here
				_refs[hash].deleted = false;	//and here
				_deleted.Remove(hash);			//and remove it here
            }else{
				return false;
			}	
			
            var chunk = FileUtil.Read(_data, r.offset, r.length);
            var p = new Post();
            p.hash = hash;
            p.replyto = r.replyTo;
            p.message = Encoding.UTF8.GetString(chunk).ToB64();
			
			_cache[hash] = p;	//update post in _cache
			
            lock (_lock)
            {
				// clear half of cache if cache limit was met
				if (_cache.Keys.Count > CacheLimit)
				{
					while (_cache.Keys.Count > CacheLimit - CacheLimit/10)
					{
						_cache.Remove(_cache.Keys.First());
					}
				}
			}
			
			while(DiffFile_locked){System.Threading.Thread.Sleep(1);}	
			DiffFile_locked = true;	
			FileUtil.Append(DiffFile, JsonConvert.SerializeObject(_refs[hash]/*, Formatting.Indented */) + "\n");		//diff-3.list	
			DiffFile_locked = false;	
			
            return true;	
        }

        /*
            Report post before local deletion on lite-server
        */
        public int ReportPost(string hash, string reason)
        {
            if ( !_refs.ContainsKey(hash) || _free.Contains(hash) || _deleted.Contains(hash) ){		//if post cann't be deleted
                return 0;																				//do not do nothing
			}
			else{
				if(!_reported.ContainsKey(hash)){
					_reported[hash] = new List<string>();
				}
				(_reported[hash]).Add(reason);
				
				File.AppendAllText(@"reports\"+hash+".txt", reason + Environment.NewLine);
				
//				if (_cache.ContainsKey(hash)){									//if this was been in _cache
//					_cache[hash].message = ReportedStub+_cache[hash].message;	//remove it
//				}

				return 1;
			}
        }

        /*
            Returns hashes of posts with reports
        */
        public string GetReports()
        {
			var text = string.Empty;
			var sorted_reported = _reported.OrderByDescending(x => x.Value.Count); //order by lists.Count (number of reports);
			_reported = sorted_reported.ToDictionary(pair => pair.Key, pair => pair.Value);
			for (int post = 0; post < _reported.Count; post++) {
				var item = _reported.ElementAt(post);
				var itemKey = item.Key;
				
				text += itemKey + ",";							//get hashes of reported posts
			}
            return text;
        }
		
        /*
            Delete reports for post with specified hash
        */
        public bool DeleteReportsForPostHash(string hash)
        {
				if(_reported.ContainsKey(hash)){
					_reported.Remove(hash);
					
					if(_cache.ContainsKey(hash)){
						_cache[hash].message = (_cache[hash].message).Substring(16);
					}
					
					File.Delete(@"reports\"+hash+".txt");
					
					var sorted_reported = _reported.OrderByDescending(x => x.Value.Count); //order by lists.Count (number of reports);
					_reported = sorted_reported.ToDictionary(pair => pair.Key, pair => pair.Value);
					
					return true;
				}else{
					return false;
				}
		}

        /*
            Get post from DB by hash.
            Returns null if not found.
            This method does caching of recently requested posts for faster response.
        */
        public Post GetPost(string hash, bool append_text = false)
        {
//			Console.WriteLine("GetPost. append_text: "+append_text);
            if (_cache.ContainsKey(hash)){
				if(append_text){
//					Console.WriteLine("_cache[hash]: "+_cache[hash].message.Substring(0, 30));
					return _cache[hash];
				}else{
//					Console.WriteLine("_cache[hash]: "+_cache[hash].message.Substring(0, 30));
					return new Post(_cache[hash].replyto, _cache[hash].message.Replace(DeletedStub, ""));	//.Replace(ReportedStub, ""));
				}
			}

            if (!_refs.ContainsKey(hash) || _free.Contains(hash)){ //if not added or if post_was_deleted_forever
                return null;
			}
            var r = _refs[hash];

            var chunk = FileUtil.Read(_data, r.offset, r.length);
            var p = new Post();
            p.hash = hash;
            p.replyto = r.replyTo;
            p.message = ((r.deleted)?DeletedStub/*.FromB64()*/:"")+Encoding.UTF8.GetString(chunk).ToB64(); 	//mark "deleted_once" posts and append post-content without deletion this.
//            p.message = ((_reported.ContainsKey(p.hash))?ReportedStub:"")+p.message; 					//mark this as reported, if post was been reported
//			Console.WriteLine("PostDb.cs. GetPost. p.message: "+p.message);

            lock (_lock)
            {
				// clear half of cache if cache limit was met
				if (_cache.Keys.Count > CacheLimit)
				{
					while (_cache.Keys.Count > CacheLimit - CacheLimit/10)
					{
						if(_cache.ContainsKey(_cache.Keys.First())){
							_cache.Remove(_cache.Keys.First());		//without if, sometimes System.ArgumentNullException: parameter key
						}
					}
				}
			}
            _cache[hash] = p;
			
			if(append_text){
//				Console.WriteLine("_cache[hash]: "+_cache[hash].message.Substring(0, 30));
				return p;
			}else{
//				Console.WriteLine("_cache[hash]: "+_cache[hash].message.Substring(0, 30));
				return new Post(p.replyto, p.message.Replace(DeletedStub, ""));//.Replace(ReportedStub, ""));
			}
        }
        public Post[] GetPosts(string hashes_joined_with_comma, int take=-1) 			//string "hash1,hash2,hash3,..."
        {
			string[] array_hashes = hashes_joined_with_comma.Split(',');	//split by comma
			//for (int i = 0; i < array_hashes.Length; i++) 					//for each hash
			//{
				//Console.WriteLine(array_hashes[i]);								//Just display this.
			//}
            var array_with_posts = new Post[((take!=-1) ? take : array_hashes.Length)];						//post with length of array hashes
            for (int i = 0; i < ((take!=-1) ? take : array_hashes.Length); i++)					//for each hash in array
            {
                array_with_posts[i] = GetPost(array_hashes[i]);							//get post by hash and add this
            }
            return array_with_posts;														//return array with posts
        }
		//from client 3.1 - get last n posts in thread
        public List<Post> GetLastNAnswers(string hash, int n, bool appendText = false, bool fast = false)
        {
            List<Post> res_ln = new List<Post>();
			if(_free.Contains(hash)){				//if post deleted forever or just deleted and contains in _deleted, and not contains in _refs
				return res_ln;							//return empty list
			}
            if ( !_rrefs.ContainsKey(hash) ){	//if post, thread, cathegory - no have replies
				return res_ln;							//return empty list
            }
            var stack = new Stack<List<DbPostRef>>();
            stack.Push(_rrefs[hash]);

            while (stack.Count > 0)
            {
                var elem = stack.Pop();            

                foreach (var reply in elem.ToArray())
                {
					if(_free.Contains(reply.hash)){
						continue;
					}
					

					var post = GetPost(reply.hash, appendText);
					if(post==null){
						continue;						
					}
				//	else if(post.date==null){
				//		continue;
				//	}
					if(fast==true){
						if( ( DateTime.Now.Year - post.date.Year) > 2 ){
							continue;
						}
					}
					
                    res_ln.Add(post);
                    if (_rrefs.ContainsKey(reply.hash))
                        stack.Push(_rrefs[reply.hash]);
                }
            }
                        
            //	res_ln = res_ln.OrderBy<Post, DateTime>(a => a.date, Comparer<DateTime>.Create((a, b) => (DateTime.Compare(a, b)))).ToList(); //unstable in .NET v4.0
				res_ln = res_ln.OrderBy<Post, DateTime>((a) => { if(!hash_DateTime.ContainsKey(a.hash)){ hash_DateTime[a.hash] = a.date;} return hash_DateTime[a.hash]; }, new DTComparer()).ToList();			//	res_ln = res_ln.OrderBy<Post, DateTime>((a) => {if(hash_DateTime.ContainsKey(a.hash)){return hash_DateTime[a.hash];}else{return hash_DateTime[a.hash] = a.date;}}/*a.date*/, new DTComparer()).ToList();
			//	res_ln = res_ln.OrderByDescending<Post, DateTime>(a => a.date).ToList();	//maybe working in .NET v4.0
			//	Console.WriteLine("res_ln.Count = "+res_ln.Count); //shorter, when bool fast = true

            if (n == 0) return res_ln;
            return (res_ln.Count < n) ? res_ln : res_ln.GetRange(res_ln.Count - n, n);
        }

		/**
			Get sorted replies for thread (hash).
			appendText - append text of "deleted_once" posts ("post_was_deleted")
//			and "reported" posts ("post_is_reported")
			Default value - false.
			Cache added. _GetReplies_cache, appendText = false, there.
		*/
        public Post[] GetReplies(string hash, bool appendText = false)
        {
			lock(_lock){											//lock all variables
				

				if(_GetReplies_cache_list.ContainsKey(hash)){								//if list of sorted hashes of replies for this hash already contains in the _GetReplies_cache_list[hash]
					Post[] cached_res = new Post[_GetReplies_cache_list[hash].Count];		//create new array with posts Post[]
					for(int i = 0; i<_GetReplies_cache_list[hash].Count; i++){					//and for each hash from list
						cached_res[i] = GetPost(_GetReplies_cache_list[hash][i], appendText);	//get post by hash and put posts in cached_res, with appendText (true/false)
					}
					return cached_res;		//return this array with posts Post[], as result, to don't run the comparers again.
				}

				if (!_rrefs.ContainsKey(hash) || _free.Contains(hash)){		//if post not have any replies or if post was "post_was_deleted_forever"
					return new Post[0];											//just return empty post
				}
				
				var res = new Post[_rrefs[hash].Count];				//create new array res, with all replies for hash.
				var rrefs = _rrefs[hash].ToArray();					//copy array with hashes of all replies to this new array string[].

				for (int i = 0; i < rrefs.Length; i++){				//for all hashes
					res[i] = GetPost(rrefs[i].hash, appendText);		//get post to Post[] res, appendText (true/false);
				}
	
				if (
						GetPost(hash, appendText).replyto == "bdd4b5fc1b3a933367bc6830fef72a35"		//if this is category and reply to "Create categories post"
					||	GetPost(hash, appendText).replyto == "f682830a470200d738d32c69e6c2b8a4"		//or if this is "Create categories post" and reply to this post
				){	//do this all
					res = res.OrderBy<Post, DateTime>(	//sort all replies by dateTime
							(a)
							=>
							{
								try{
									if(a == null) {																				//if a==null
										Console.WriteLine("PostDb.cs. GetReplies. Orderby. a == null. return standard date.");	//show it
										return DateTime.Parse("01.01.0001 0:00:00");											//and return default DateTime.
									}if(!hash_DateTime.ContainsKey(a.hash)){hash_DateTime[a.hash] = a.date;}
									DateTime last_date =												//try to get last 1 reply
											( GetLastNAnswers(a.hash, 1, false, true).Count == 0 )		//if after fast checking reply was not found, and all replies was been older than 2 years
												? hash_DateTime[a.hash]		/*a.date*/												//use date of this post/category
												: GetLastNAnswers(a.hash, 1, false, false).Last().date	//else use the date of last reply.
									;
									last_date = ( ( DateTime.Now - last_date ).Ticks < 0 )		//if unix timestamp lesser than 0
											?	DateTime.Parse("01.01.0001 0:00:00")				//seems like fake date - in future, remove
											:	last_date											//or leave this date
									;
									return last_date;											//return date of last reply.
								}
								catch(Exception ex){											//else
									Console.WriteLine("PostDb.cs, GetReplies, OrderBy: "+ex);	//show this
									return DateTime.Parse("01.01.0001 0:00:00");				//and return default.
								}
							}
							,
							new DTComparer()	//use DTComparer to compare date-time.
						)
						.Reverse()			//reverse result
						.ToArray()			//ToArray this.
					;
				}else{
					res =
						res.OrderBy<Post, DateTime>(	//sort replies by date and time
							(a)
							=>
							{
								try{
									if(a == null) {return DateTime.Parse("01.01.0001 0:00:00");}if(!hash_DateTime.ContainsKey(a.hash)){hash_DateTime[a.hash] = a.date;}
									DateTime last_date =
											( GetLastNAnswers(a.hash, 1, false, true).Count == 0 )		//if fast fail
												? hash_DateTime[a.hash]			/*a.date*/												//use date of this post/category
												: GetLastNAnswers(a.hash,1, false, false).Last().date	//else last
									;
									last_date = ( ( DateTime.Now - last_date ).Ticks < 0 )		//if unix timestamp lesser than 0
											?	DateTime.Parse("01.01.0001 0:00:00")				//seems like fake date - remove
											:	last_date											//or leave
									;
									return last_date;
								}
								catch(Exception ex){
									Console.WriteLine("PostDb.cs, GetReplies, OrderBy: "+ex);
									return DateTime.Parse("01.01.0001 0:00:00");
								}
							}
							,
							new DTComparer()
						)
						//without reverse...
						.ToArray()	//now this OK.
					;
				}

/*
				//display array with hashes of thread and date of last post for which this was been sorted.
				Console.WriteLine("Array, before return:");
				for(var i=0; i<res.Length; i++){
					DateTime last_date;
					try{
						if(res[i] == null) {last_date = DateTime.Parse("01.01.0001 0:00:00");}
						else{
							lock(_lock){
								last_date =
										( GetLastNAnswers(res[i].hash, 1, false, true).Count == 0 )		//fast
											? res[i].date
											: GetLastNAnswers(res[i].hash,1, false, false).Last().date	//fast
								;
								last_date = ( ( DateTime.Now - last_date ).Ticks < 0 )	//if unix timestamp lesser than 0
										?	DateTime.Parse("01.01.0001 0:00:00")				//seems like fake - remove
										:	last_date											//or leave
								;
							}
						}
					}
					catch(Exception ex){
						Console.WriteLine("PostDb.cs, GetReplies, OrderBy: "+ex);
						last_date = DateTime.Parse("01.01.0001 0:00:00");
					}
					Console.WriteLine(
						"res[i].hash: "
						+res[i].hash
						+", res[i].date: "
						+last_date
					);
				}
				//Dates from new - to old now. OK.
*/

				//write sorted replies hashes in _GetReplies_cache_list
				if(!_GetReplies_cache_list.ContainsKey(hash)){				//if list of sorted replies for hash - not contains in dictionary by hash
					_GetReplies_cache_list[hash] = new List<string>();		//create empty list by this hash
					for(int i = 0; i<res.Length; i++){						//and for each post of res
						_GetReplies_cache_list[hash].Add(res[i].hash);		//add hash of each post in list.
					}
				}
				return res;	//and finally, return res(ult), res(ponse), after all.
			}
        }

        /*
            Rewrite index.json. Called once at start, during runtime all changes go
            to the diff file - new line for each change.
        */
        private void Flush()
        {			int hash_DateTime_Count = 0; if(new FileInfo("datetime_by_hash.json").Exists){hash_DateTime = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(File.ReadAllText("datetime_by_hash.json")); hash_DateTime_Count = hash_DateTime.Count;}foreach (KeyValuePair<string, DbPostRef> hash in _refs){if(hash_DateTime.ContainsKey(hash.Key)){continue;}else if(_cache.ContainsKey(hash.Key)){hash_DateTime[hash.Key] = _cache[hash.Key].date;}else{Post post = GetPost(hash.Key); if(post != null){hash_DateTime[hash.Key] = _cache[hash.Key].date;}}/*//Console.WriteLine("Key = {0}, Value = {1}", hash.Key, hash.Value);*/}/*//hash -> DateTime;*/var sorted_refs = _refs.OrderBy(_refs_item => hash_DateTime.FirstOrDefault(hash_DateTime_item => hash_DateTime_item.Key == _refs_item.Key).Value).ToDictionary(dbref => dbref.Key, dbref => dbref.Value);	/*//sort dict2 by DateTime-value in dict1.*/ /* foreach (KeyValuePair<string, string> kvp in sorted_refs){Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);} // show sorted dictionary*/ if(hash_DateTime_Count != hash_DateTime.Count){File.WriteAllText("datetime_by_hash.json", JsonConvert.SerializeObject( hash_DateTime, Formatting.Indented ));} /*save dictionary if number of post there, was been changed.*/
			var index = new Index();/*//sort all indexes by date-time, on start the program. This need to re-write indexes, because from order of records in this - depends the order of _ordered list, and LAST POSTS order. Need to restart program, to update order of Last Posts, after PNG-collect, for example.*/
			index.indexes = sorted_refs.Values.ToArray();			//index.indexes = _refs.Values.ToArray();
			var json = JsonConvert.SerializeObject(index, Formatting.None /*, or Formatting.Indented */);

/*
			while(_index_locked){System.Threading.Thread.Sleep(10);}
			_index_locked = true;
            File.WriteAllText("2"+_index, json);				//write to another file.
			_index_locked = false;
            
			while(_index_locked){System.Threading.Thread.Sleep(10);}
			_index_locked = true;
			var indexString = File.ReadAllText("2"+_index);		//read this to compare.
			_index_locked = false;

			if(json==indexString){								//if equals
				while(_index_locked){System.Threading.Thread.Sleep(10);}
				_index_locked = true;
				File.Delete(_index);								//remove index
				_index_locked = false;

				FileInfo fi = new FileInfo("2"+_index);				//read fileInfo for 2_index
				if (fi.Exists)											//if exists
				{
					while(_index_locked){System.Threading.Thread.Sleep(10);}
					_index_locked = true;
					fi.MoveTo(_index);									//rename to _index.
					_index_locked = false;
				}
			}
*/

			//File.WriteAllText(_index, json);							//old code - just write, but sometimes NUL bytes there writed.
			
			lock(_lock){while(_index_locked){System.Threading.Thread.Sleep(10);}
			_index_locked = true;
			File.WriteAllText(_index, json);							//old code - just write, but sometimes NUL bytes there writed.
			_index_locked = false;}
        }
		
		public static bool IsMD5(string input)	//validate hash string.
		{
			if (String.IsNullOrEmpty(input))
			{
				return false;
			}

			return System.Text.RegularExpressions.Regex.IsMatch(input, "^[0-9a-fA-F]{32}$", System.Text.RegularExpressions.RegexOptions.Compiled);
		}

		private bool downloading = false; //true, while downloading.
		//download post by URL, using API
		public bool DownloadPosts(string url, bool allowReput = false, bool bypassValidation = false){ //URL is the string "http://127.0.0.1:7346/api/" or "http://mydomain.onion:8080/api/" from where need to download posts, as JSON, from /prange/ responses.
		
			while(
					downloading == true
				//|| 	uploading == true
			) {
				System.Threading.Thread.Sleep(10);
			}
			downloading = true;
			//Console.WriteLine("DownloadPosts: url {0}", url);
			
			int max_posts = 40;	//maximum size of HTML page is 2.5 MB = 2621440 bytes / 65535 bytes/post ~ 40 posts

			//get post count, by url
			var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url+"count"); //		api/count

			int posts_count;
			using (var response = (System.Net.HttpWebResponse)request.GetResponse())
			{
				var encoding = Encoding.GetEncoding(response.CharacterSet);
				using (var responseStream = response.GetResponseStream())
				using (var reader = new StreamReader(responseStream, encoding))
				posts_count = nbpack.NBPackMain.parse_number(reader.ReadToEnd());
			}
			//Console.WriteLine("posts_count {0}", posts_count);
			
			//loading the posts by parts of max_posts
			for (var i = 0; (i*max_posts)<posts_count; i++){
					
					Console.WriteLine("Part: {0}", i);
					System.Threading.Thread.Sleep(10);
					Console.WriteLine(url+"prange/"+(i*max_posts)+"-"+max_posts+"&only_hashes");

				//Loading posts
					var posts_array = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url+"prange/"+(i*max_posts)+"-"+max_posts);

					string posts;	//define JSON-string
					using (var response = (System.Net.HttpWebResponse)posts_array.GetResponse())
					{
						var encoding = Encoding.GetEncoding(response.CharacterSet);
						using (var responseStream = response.GetResponseStream())
						using (var reader = new StreamReader(responseStream, encoding))
						posts = reader.ReadToEnd();	//write JSON to string
					}
					//Console.WriteLine( "result: {0}", posts);
					List<Post> data = JsonConvert.DeserializeObject<List<Post>>(posts);	//JSON string to list of Posts
					//Console.WriteLine( "data.Count {0}", data.Count);					//show Length of list with Posts
					
					for(var post = 0; post<data.Count; post++){	//for each post in list
						//try to add this post
						Console.WriteLine(
							"Post hash: {0}, "+
							"md5? {1}, "+
							"added: {2} ",
							data[post].hash,
							IsMD5(data[post].hash),
							PutPost(data[post], allowReput, bypassValidation)	//do not allow reput and make full validation (this is the better to prevent wipe)
							//ReputPost(data[post])	//do not allow reput and make full validation
						);
					}
					Console.WriteLine("\n");	//show empty line after end of each part.
				//End loading posts.
			}ReadRefs();	//update database from _index and DiffFile
			downloading = false;
			
			Console.WriteLine("End downloading posts.\n");
			return true;
		}
		
		private bool uploading = false;	//true, when uploading in progress.
		//uploading posts
		public int[] UploadPosts(string JSON, bool allowReput = false, bool bypassValidation = false){ //posts in JSON, like response of http://127.0.0.1:7346/api/prange/0-10
			List<Post> posts_data = JsonConvert.DeserializeObject<List<Post>>(JSON);	//JSON string to list of Posts
			Console.WriteLine("UploadPosts: posts_data.Count: "+posts_data.Count+" posts accepted...");
			
			while(
					uploading == true
				||	downloading == true
			) {
				System.Threading.Thread.Sleep(10);
			}
			
			uploading = true;
			int posts_added = 0;
			for(var post_index = 0; post_index<posts_data.Count; post_index++){	//for each post in list
				//try to add this post
				bool added = PutPost(posts_data[post_index], allowReput, bypassValidation);	//do not allow reput (false) and make full validation captcha (false)
				if(added == true){
					posts_added += 1;
				}
				Console.WriteLine(
					"("+(post_index+1)+"/"+posts_data.Count+
					"), Hash: {0}, "+
					"md5? {1}, "+
					"added: {2} ",
					posts_data[post_index].hash,
					IsMD5(posts_data[post_index].hash),
					added
				);
			}ReadRefs();	//update database from _index and DiffFile
			uploading = false;
			
			Console.WriteLine("End uploading posts.\n");
			return new int[]{posts_data.Count, posts_added};
		}

		//uploading one post, like BitMessage instantRetranslation.
		public bool UploadPost(string JSON, bool allowReput = false, bool bypassValidation = false){ //posts in JSON, like response of http://127.0.0.1:7346/api/prange/0-10 , but not array, just one post.
			try{
				lock(_lock){
					Post post_data = JsonConvert.DeserializeObject<Post>(JSON);	//JSON string to Post

					bool is_added = PutPost(post_data, allowReput, bypassValidation);

					Console.Write(
						"Post hash: {0}, "+
						"md5? {1}, Status: ",
						post_data.hash,
						IsMD5(post_data.hash)
					);
					Console.Write(((is_added==true)?"Added.":"Not added.")+"\n");
					return is_added;
				}
			}catch(Exception ex){
//				Console.WriteLine(JSON);
//				Console.WriteLine(ex);
//				Console.WriteLine("ex.GetType().ToString():"+ex.GetType().ToString());
				return (ex.GetType().ToString()!="Newtonsoft.Json.JsonReaderException");	//false
			}
		}
        #endregion
    }
}
