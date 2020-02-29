function generateGuid() {	//generate guid.toLowerCase()
  var result, i, j;
  result = '';
  for(j=0; j<32; j++) {
    if( j == 8 || j == 12 || j == 16 || j == 20) 
      result = result + '-';
    i = Math.floor(Math.random()*16).toString(16)//.toUpperCase()
	;
    result = result + i;
  }
  return result;
}

//Function for checking, without throw error, is str base64-encoded or not. return true/false.
//check is base64 without throw error
function isBase64(str) {//return true, or false
    if(str===''){return false;}//if string is empty and not contains base64 characters
    try {
        return btoa(atob(str)) == str; //true if base64
    } catch (err) {
        return false;
    }
}

//add hash to queue-array and remove this, using javascript.
var queue = [];								//empty queue array...

//add hash to queue array
function queue_add(arr, hash) {
    arr.push(hash);							//push hash to array
	pushNotification(hash+" added to queue.");
}

//remove hash from queue array, if this exists there.
function queue_remove(arr, hash, show_notif) {
	var index = arr.indexOf(hash);			//find hash in array
	if (index > -1) {						//if found
		arr.splice(index, 1);				//remove this
		if( typeof show_notif == 'undefined' ){	pushNotification(hash+" removed from queue."); }	//false, if no need to show notif.
	}
}

//add or remove, using one function
function add_remove(arr, hash, element, generate, showhash){
	hash = hash.toString();			//hash to string, if this is integer
	if(current_queue.concat(arr).indexOf(hash)===-1){		//if hash not found in array
		if(generate===true){		//and if need to generate link
			return (
				'<a href="javascript:void(0)" onclick="add_remove(queue, \''+hash+'\', this, false, '+showhash+');"'+
				'title="Add post '+hash+' in queue...">'+
				'<span class="glyphicon glyphicon-log-in" aria-hidden="true"></span> '+
				((showhash)? '<gr>#'+shortenHash(hash)+'</gr>' : '')+
				'</a>'
			); //return link to add
		}
		else{														//if no need generate link, do action add
			queue_add(arr, hash);											//add hash to array
			element.innerHTML = '<span class="glyphicon glyphicon-log-out" aria-hidden="true"></span> '+
			((showhash)? '<gr>#'+shortenHash(hash)+'</gr>' : '');	//change link to remove link
			element.title = 'Remove post '+hash+' from queue...';	//set title for remove-link.
		}
	}else{							//if hash was been found in array
		if(generate===true){		//and if need to generage link
			return (
				'<a href="javascript:void(0)" onclick="add_remove(queue, \''+hash+'\', this, false, '+showhash+');"'+
				'title="remove post '+hash+' from queue...">'+
				'<span class="glyphicon glyphicon-log-out" aria-hidden="true"></span> '+
				((showhash)? '<gr>#'+shortenHash(hash)+'</gr>' : '')+
				'</a>'
			); //return link to remove
		}else{														//if no need to generate link, do action remove
			queue_remove(arr, hash);								//remove hash from array
			queue_remove(current_queue, hash);						//remove hash from array
			element.innerHTML = '<span class="glyphicon glyphicon-log-in" aria-hidden="true"></span> '+
			((showhash)? '<gr>#'+shortenHash(hash)+'</gr>' : '');		//Change link to add link
			element.title = 'Add post '+hash+' to queue...';	//set title for add-link.
		}
	}
	//console.log(queue);
}

function numSuffix(numStr) {
  if (numStr.endsWith('11')) return 's';
  if (numStr.endsWith('1')) return '';
  return 's';
}

	function replace_local_quotes(str){
		//replace posts to local-links
		
		/*
		//replace hashes to local links, inside links.
		
		//var extract_urls = /(https?:\/\/[^\s]+)/g;	//regular expression to get array with urls from string, or multistring
		var extract_urls = /(http(s)?:\/\/.)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&\/\/=]*)/g;	//regular expression to get array with urls from string, or multistring
		var test_hex = /^[0-9a-fA-F]+$/;			//regular expression to test is the string hex or not

		//cases of quoting the post, thread or category
		var cases = [
			//http(s):
			//Nanoboard client 3.0:
			'/pages/index.html#category',
			'/pages/index.html#thread',
			'/pages/index.html#',
			//karasiq nanoboard
			'/#category',	
			'/#thread',		
			'/#'			
		];
		var url_arr = str.match(extract_urls);
		console.log('url_arr', url_arr);
		var index = 0;															//increment index to search from this
		for(i=0; i<url_arr.length; i++){
			for(j = 0; j<cases.length; j++){
				console.log('cases[j]', cases[j], 'j', j);
				if(url_arr[i].indexOf(cases[j])!==-1){
					index = str.indexOf(cases[j], index)+cases[j].length; 		//find case from previous index
					var maybe_hex = str.substring(index, index+32);
					if(test_hex.test(maybe_hex)===true){
						str = str.split(url_arr[i]).join('>>'+maybe_hex);
						//str = str.split(maybe_hex).join('>>'+maybe_hex);
					}
					break;
				}
			}
		}
		return str;
		//this script replace only URL's.
		*/
		
		//next script replace all 32 byte hashes in posts.
		var extract_16bytes_hex = 	/[A-Fa-f0-9]{32}/g;			//Regular Expression to extract 32 hex symbols (16 bytes) from string. 
		var test_hex_char = 		/[A-Fa-f0-9]/;				//Regular Expression to test one char. Is hex or not...
		var hex_array = str.match(extract_16bytes_hex);			//array with hex strings...
		
		//console.log('hex_array', hex_array);

		var index = 0;											//start index is 0
		var temp_index = 0;										//index in the string, where current array element (hex) was been found
		var start_hex;											//start position of real hex
		var position;											//end_position of real hex
		var real_hex;											//real hex
		//var qoute_prefix = ' >>';
		//var qoute_prefix = ' &gt;&gt;';
		var qoute_prefix = function(value){return '<a href="javascript:void(0);" onclick=_depth=2;loadThread("'+value+'") title="Click to open post/thread/category...">'+value+'</a>';}

		for(
			i=0;								//from 0
					hex_array!==null			//if hex_array not null
				&& 	i<hex_array.length;			//up to hex_array.length
			i++									//for each hex strings in hex_array
		){
			temp_index = str.indexOf(hex_array[i], temp_index+1);			//find and save the index of current hex substring in the string,
																		//and find this from previous index, saved in var temp_index
			//console.log('temp_index', temp_index);						//show this...
			if(str.substring(temp_index-8, temp_index)==='category'){		//if before this hex, in the string found 'category'
				start_hex = temp_index;										//set current index for first char in real_hex
			}else if(str.substring(temp_index-3, temp_index+3)==='thread'){	//if 'thread' before hex, 'ead' is a part of hex, because this is hex symbols.
				start_hex = temp_index+3;									//shift index for first char in real_hex
			}else if(str.substring(temp_index-2, temp_index)==='/#'){		//if '/#'
				start_hex = temp_index;										//current index
			}else if(str.substring(temp_index-1, temp_index)==='#'){		//if '#'
				start_hex = temp_index;										//current index
			}else if(
					str.substring(temp_index-2, temp_index)==='>>'			//if this was been a post
				||	str.substring(temp_index-8, temp_index)==='&gt;&gt;'
			){		//if this was been a post
				//console.log('continue1');									//test
				continue;													//just leave this and continue
			}else if(
					str.substring(temp_index-1, temp_index)==='>'			//if this was been quote
				||	str.substring(temp_index-4, temp_index)==='&gt;'
			){
				start_hex = temp_index;										//current index
			}else if(
					typeof str.substring[temp_index-1] === 'undefined'				//if no any symbol
				||	!test_hex_char.test(str.substring(temp_index-1, temp_index))	//or not hex char
			){
				start_hex = temp_index;													//current index
			}else{																		//else
				//console.log('continue2');													//test
				continue;																	//continue
			}

			if(																			//if
					typeof str[start_hex+32] === 'undefined'							//last char undefined
				||																		//or
					(
							!test_hex_char.test(str[start_hex+32])						//not hex
						&&	str[start_hex+32] !== ']'									//and not ']'
						&&	!(/[0-9A-z-]/).test(str[start_hex+32])						//exclude this all characters
						&&	(															//and
								(typeof str[start_hex-1] !== 'undefined')				//if previous char not undefined
							&&	!test_hex_char.test(str[start_hex-1])					//and not hex
						)
					)
			){																			//replace this
				//console.log('str[start_hex-1]: ', str[start_hex-1]);					//show first char
				//console.log('str[start_hex+32]: ', str[start_hex+32]);				//show last char
				
				real_hex = str.substring(start_hex, start_hex+32);						//get real hex
				str = str.substring(0, start_hex) + qoute_prefix(real_hex) + str.substring(start_hex+32, str.length);	//add quote
				temp_index+=(qoute_prefix(real_hex).length-32);							//move index to search from this
			}
			else{																		//else, if hex char
				//console.log('last char is hex symbol, previous hex is not a hash from post. Do not replace...');  //do not replace.
				//console.log('nanoclient.js, replace_local_quotes() - else: str.substring(start_hex, start_hex+32); ', str.substring(start_hex, start_hex+32), 'str[start_hex+32]: ', str[start_hex+32]);
			}
		}

		//console.log('replace_local_quotes - str:', str);

		return str;
	}//end replace function
	//run this:
	//post_content = replace_local_quotes(post_content);	//replace posts to local links.

function detect_files(post_content){
  //detect files
  var not_a_files = 0;																	//increment this, when not a file and no need to replace this...
  if(post_content.indexOf('[file')!==-1){												//if bb-code "file" found
	var files_array = post_content.split('[file');										//split by file
	//console.log('files_array, after splitting by \'[file\' = ', files_array);			//show array in console, with description
	for(i=1;i<files_array.length;i++){													//for each element up to array length
		//console.log(
		//				'files_array[i]', files_array[i],								//show array element
		//		'\n'+	'array length: ', files_array.length							//and array length
		//);
		var maybe_base = 	(
									files_array[i].split('"]')[1]						//split by '"]' if name/type exists
								|| 	files_array[i].split(']')[1]						//or if prefious value is undefined, and [file] (without name/type) - then split by ']' to get base64
							)
							.split('[/file')[0];										//split by "[/file", not by "[/file]" to get base64, because by ']' this can be already splitted.

		//console.log('i = ', i, 'maybe_base', maybe_base);								//show i value too.

		if(isBase64(maybe_base.trim())){																//check is base64
			//console.log('base64!');																//if yes - show notification in console
			//console.log('base64! trimmed maybe_base: ', maybe_base.trim().substring(0,40));			//with part of base64, not full value.
																										//and try to replace file to download link...
			//var split_by_base = post_content.split(maybe_base);									//split post by this base64 (old code)
			var split_by_base = post_content.split(']'+maybe_base+'[');								//now split post by this base64, but in the middle of ']BASE64[' - if many links for the same file was been replaced. And don't split replaced HTML-links, by this base64.
			var before_base = split_by_base[0];														//here contains previous part of post and filename with filetype
			var split_by_file = before_base.split('[file');											//split by '[file'. In the second element must be filename and filetype.

			var check_filename_regexp = /^[-\wёЁА-я^&'@{}[\],$=!#().%+~]+$/;								//regexp to test is filename correct? Latin and Cyrillic characters in filename.extension
			//	console.log(/^[-\w^&'@{}[\],$=!#().%+~]+$/.test("test1_FiLEнёЁйМ.tИкСt") === true); 		//old regexp - FALSE, because ciryllic characters inside the string...
			//	console.log(/^[-\wёЁА-я^&'@{}[\],$=!#().%+~]+$/.test("test1_FiLEнёЁйМ.tИкСt") === true); 	//TRUE

			var part = not_a_files+1;																	//first part of array must contains filename and filetype
			var filename = split_by_file[part].split('name="')[1].split('"')[0];						//set filename by splitting this by 'name="' and '"'
			//console.log('filename', filename);														//show this
			var filetype = split_by_file[part].split('type="')[1].split('"')[0];						//set filetype by splitting this by 'type="' and '"'
			//console.log('filetype', filetype);														//show filetype

			
			while(true){																			//cycle without end
				if(typeof split_by_file[part]=='undefined'){break;}									//if no any element - break
				if(!check_filename_regexp.test(filename)){												//if no filename there
					part++;																					//check next part of splitted post
					filename = split_by_file[part].split('name="')[1].split('"')[0];						//get filename
					//console.log('filename', filename);														//show this
					filetype = split_by_file[part].split('type="')[1].split('"')[0];						//get filetype
					//console.log('filetype', filetype);														//show this.
					continue;																				//and continue cycle without running next code...
				}
				else{																				//else, if not code and filename exist and valid...
				//	console.log(																		//show checking results
				//		"(check_filename_regexp.test(filename))",
				//		(check_filename_regexp.test(filename)),
				//		'part', part
				//	);
					break;																				//and break from cycle...
				}
			}
			//when filename and filetype is valid...

			//build link to download this file.
			var html_link = '<a href="data:'+filetype+';base64,'+maybe_base+'" download="'+filename+'">['+filename+']</a>'
			//and add link to download this as binary,
			//with green button as base64 encoded PNG image to this link - without tab symbols.

//icon to download as binary:

			+'<a href="/pages/download_as_binary.html" target="_blank" title="If need to save base64 as file with another filename.ext">\
<img src="\
data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/\
9hAAAACXBIWXMAAA7EAAAOxAGVKw4bAAABCElEQVQ4jZ3TMUoDQRjF8V+SRURExQN4Ai+gFkkjegqxsQwSQcQiMGgrIugJvEB6beIRcgUPIBoExSJY7MRk\
18kG/WBZ9r3v/75hZqcmVcEBLrESlSG6gvtyay0BL+ANiyXnE6uCr2mxnpifJWBRy8piKiClzfSqmv8d8KcVTDYx2MBafJ5mBDTxilfBcznxEIMKWPQGOE\
ot6QLXFfC47tAdfzR+5D5aHuQ/z1YF3BYmQqNgT0KWsT0PzgOCfS0v+j6mQh6xhJ3Yd4PjAhysa2nW0cNmITZvPMMpztEpT45ML8NI6uxz4OqXXqxRJr9p\
u/GSjMbGDKA+9d7DMEMHtziZM61c72h/A3foMuoBqRh/AAAAAElFTkSuQmCC\">'
			+'</a>'

			;

			//console.log('link - generated sucessfully...\n', 'html_link: ', html_link);																//show stage and this link...

			//console.log('replace bb-code to link...\n', post_content.split('[file name="'+filename+'" type="'+filetype+'"]'+maybe_base+'[/file]'));	//show stage and splitted array...
			
			post_content = post_content.split('[file name="'+filename+'" type="'+filetype+'"]'+maybe_base+'[/file]').join(html_link);	//and replace bb-code [file] - to this link.

			//console.log('post_content: ', post_content);																				//show results...
		}else{																								//not base64 in the middle of [file][/file]
			//console.log('not base64', maybe_base);														//show notification in console
			not_a_files++;																					//and do not extract info about this file, after next splitting by "[file"...
		}
		//and continue replace files to links in all post, splitted by file-tag, again and again...
	}
	//console.log('post_content: ', post_content);																//show post in the end and continue script
  }//or continue script without replacing...
  return post_content;
}
	//run this:
	//post_content = detect_files(post_content);	//replace posts to local links.


	
//Begin functions to show-hide post and save in LocalStorage:
/*
		//remove hash from queue array, if this exists there.
		function queue_remove(arr, hash) {
			var index = arr.indexOf(hash);			//find hash in array
			if (index > -1) {						//if found
				arr.splice(index, 1);				//remove this
			}
		}
*/
//already defined...

		var hidden_hashlist = [];	//array with hashes of hidden posts.
		
		// save array in LocalStorage
		function saveStatusLocally() {
			window.localStorage.setItem("hidden_posts", JSON.stringify(hidden_hashlist));
		}
		
		// read the string
		function readStatus() {
			//print the value of the local storage "database" key
			if (window.localStorage.getItem("hidden_posts") == null) {
				//console.log("no any value in localstorage");
			} else {
				hidden_hashlist = JSON.parse(window.localStorage.getItem("hidden_posts"));
			}
		}	

		function hide(element){
			//console.log(element);
		 	//glyphicon glyphicon-eye-close
			
			element.parentElement.classList.add("post_type_hidden");
			//console.log('hide: parent_element: ',element.parentElement);
			
			//console.log(element.parentElement.parentElement.classList);
			
			//element.innerHTML = "Hidden";

			var Post_hash = element.parentElement.id;	//get Post_hash
			
			element.childNodes[0].classList.remove("glyphicon-eye-close");
			element.childNodes[0].classList.add("glyphicon-eye-open");
			element.childNodes[0].title = "Click to show this hidden post. Post_hash: "+Post_hash;
			
			pushNotification(Post_hash+" hidden now.");							//just show the notification.
			
			hidden_hashlist.push(Post_hash);									//push Post_hash to array with hashes of hidden posts
			saveStatusLocally();												//save this in LocalStorage
			//readStatus();														//read from
			//console.log(hidden_hashlist);										//show array
		}
		
		function show(element){
			//glyphicon glyphicon-eye-open 	
			
			element.parentElement.classList.remove("post_type_hidden");
			//console.log('show: parent_element: ',element.parentElement);
			//element.innerHTML = "Hide";

			var Post_hash = element.parentElement.id;	//get Post_hash
			element.childNodes[0].classList.remove("glyphicon-eye-open");
			element.childNodes[0].classList.add("glyphicon-eye-close");
			element.childNodes[0].title = "Click to hide this post. Post_hash: "+Post_hash;
			
			pushNotification(Post_hash+" showed now.");							//just show the notification.
			
			queue_remove(hidden_hashlist, Post_hash, false);					//remove Post_hash from array with hashes of hidden posts,
			//and remove this using queue_remove function, without notification about dequeue.
			
			saveStatusLocally();												//save this in LocalStorage
			//readStatus();														//read from
			//console.log(hidden_hashlist);										//show array
			
			
		}

		function toogle_show_hide(element){
			if(
				element.parentElement.classList.contains("post_type_hidden")
			){
				//console.log('show(element);');
				show(element);
			}else{
				//console.log('hide(element);');
				hide(element);
			}
		}
		
		//hide hidden posts
		function load_hidden(){
			//localStorage.removeItem("hidden_posts");								//Just remove used item to clear LocalStorage from this.
			readStatus();															//read array with the hashes of hidden posts from LocalStorage
			//console.log(hidden_hashlist);											//show this
			/*
			for(i=0;i<hidden_hashlist.length;i++){									//for each Post_hash there
				console.log(document.getElementById(hidden_hashlist[i]));										//find post div by id with this Post_hash
				document.getElementById(hidden_hashlist[i]).classList.add("post_type_hidden");					//find post div by id with this Post_hash
				document.getElementById(hidden_hashlist[i]).childNodes[0].classList.remove("glyphicon-eye-close");
				document.getElementById(hidden_hashlist[i]).childNodes[0].classList.add("glyphicon-eye-open");				
			}
			*/
		}
		//setTimeout(load_hidden,10000);											//wait loading the page...
		load_hidden();																//no need to load the page. Just read the array, and using values for generating posts.
		
//end functions to hide-show posts and save in LocalStorage

function show_hide_post_code(hash, show_hide){	//show_hide = true/false
	if(typeof show_hide === 'undefined'){
		show_hide = 'toogle';
	}
	var pre_element = document.getElementById('pre_'+hash);
	pre_element.style.display =
		(show_hide == 'toogle')
			?	(pre_element.style.display === 'none')
				? 'block'
				: 'none'
			: (show_hide == true)
				? 'block'
				: 'none'
	;

	var post_div = document.getElementById('post_'+hash);
	post_div.style.display =
		(show_hide == 'toogle')
			? (post_div.style.display === 'none')
				? 'block'
				: 'none'
			: (show_hide == true)
				? 'none'
				: 'block'
	;

	var link = document.getElementById('code_'+hash);
	link.innerHTML = ((post_div.style.display === 'none') && (pre_element.style.display === 'block'))
						?
							'<span class="glyphicon glyphicon-tags" aria-hidden="true" title="Hide code"></span>'
							//"Hide code"
						:
							'<span class="glyphicon glyphicon-file" aria-hidden="true" title="Show code"></span>'
							//"Show code";
}

function replace_deleted_once(post_message){
	return post_message.replace('post_was_deleted', '');//.replace('post_is_reported', '');

/*
	return	(
				(
						(post_message).substring(0, 16) === 'post_was_deleted'
					|| 	(post_message).substring(0, 16) === 'post_is_reported'
				)
					? (post_message).substring(16, post_message.length)
					: post_message
			)
	;
*/
}

function addPost(post, appendFunc, hasShowButton, short) {

//	console.log('post',post);

	if(post == null){return false;}

  if (_use_spam_filter=='true' && post.hash != _categories){
    for (i in _spam_filter){
        if (_spam_filter[i].test(escapeTags(Base64.decode(post.message)))) return false;
    }
  }

  
	if(deleted_posts.hasOwnProperty(post.hash)){				//if post was been deleted locally on lite-server
		if(deleted_posts[post.hash][1] == "post_was_deleted_forever"){		//and if this post_was_deleted_once
			return false;												//just don't add this post locally, for the current client.
		}
	}

  var locationBackup = window.location.href.toString();
  if (_depth > 1) hasShowButton = false;
  if (short == undefined) short = true;
  var d = $(document.createElement('div'));
  d
    .addClass('post')
    .addClass('post__details'+((hidden_hashlist.indexOf(post.hash)!=-1) ? ' post_type_hidden' : ""))
    .attr('id', post.hash);

	//add show-hide post button
    d.append('&nbsp;&nbsp;<a href="javascript:void(0)" onclick="toogle_show_hide(this);">'+
		'<span class="glyphicon '+
		(
			(hidden_hashlist.indexOf(post.hash)!=-1)
				? 'glyphicon-eye-open " title="Click to show this hidden post. Post_hash: '+post.hash+'"'
				: 'glyphicon-eye-close " title="Click to hide this post. Post_hash: '+post.hash+'"'
		)
		+
		' aria-hidden="true"></span></a>&nbsp;&nbsp;'); 					//add glyphicon to hide-show post.
		
	//add show-hide code button
	d.append('&nbsp;<a id="code_'+post.hash+'" href="javascript:void(0);" onclick="show_hide_post_code(\''+
					post.hash
				+'\');"><span class="glyphicon glyphicon-file" aria-hidden="true" title="Show code"></span></a>&nbsp;&nbsp;'
	);

  if (_depth != 0){
    //d.append('<gr>#' + (short&&_depth!=1?shortenHash(post.hash):post.hash) + '&nbsp;</gr>');	//old code.
    d.append((add_remove(queue, post.hash, '', true, true)) + '&nbsp;');
  }
  else{
    d.append((add_remove(queue, post.hash, '', true)) + '&nbsp;');	
  }
  if (_depth != 0) {
    $('<a>')
      .attr('href', '#' + post.replyTo)
      .click(function() {
        $('#' + post.replyTo)
          .addTemporaryClass('high', 1000);
        setTimeout(function(){
          console.log('assigning location');
          _location = locationBackup;
          location.assign(locationBackup);
        }, 200);
      })
      .appendTo(d)
      .html('^' + shortenHash(post.replyTo));
  }
  d.append('&nbsp;');
  d
    .append($('<a>')
      .attr('href', 'javascript:void(0)')
      .html('<span class="glyphicon glyphicon-pencil" aria-hidden="true"></span><span class="btn-title">&thinsp;Reply</span>')
      .click(function() {
        addReplyForm(post.hash);
        d.next().find('textarea').focus();
		d.next().find('.previewbtn').click();
      }));
  if (hasShowButton) {
    d.append('&nbsp;');
    var showLink = 
      $('<a>')
        .attr('href', (_depth==0?'#category':'#thread') + post.hash)
        .text('[Show]')
        .click(function() {
          //_depth += 1;
          //loadThread(post.hash);
        });
    d.append(showLink);
    $.get('../api/threadsize/' + post.hash)
      .done(function(size){
        if (size == '0'){
          showLink.html('<span class="glyphicon glyphicon-comment not-avail" aria-hidden="true"></span><span class="btn-title not-avail">&thinsp;0</span>');
        }else{
          showLink.html('<span class="glyphicon glyphicon-comment" aria-hidden="true"></span><span class="btn-title">&thinsp;'+size+' – Show</span>');
		}
      });
  }
  d.append('&nbsp;');
  d
    .append($('<a>')
      .attr('href', 'javascript:void(0)')
      .html('<span class="glyphicon glyphicon-trash" aria-hidden="true"></span><span class="btn-title">Delete</span>')
      .attr('title', 'Click to report and delete post locally. \nPost hash will be saved in LocalStorage, and this post will not be available for you.\n Also, we will review your report, and can delete this forever. (draft function)')
      .click(function() {
        if (post.hash == _categories) {
          pushNotification("Cannot delete root post.");
          return;
        }
        var undo = false;
		$('#undelete_'+post.hash).hide();	//remove "undelete"-button
        d.append(
          $('<button id="undo_'+post.hash+'">')
            .text('Undo')
            .click(function(){
              undo = true;
              $(this).remove();
            })
            .append($('<span>').html('&nbsp;')
              .css({ background: 'red', height: '5px', marginLeft: '5px'})
              .animate({width: '100px'},50)
              .animate({width: '0px'},Math.random()*200+_post_delete_timeout)));
        setTimeout(function(){
			if (undo){
				return;
			}
			
//			console.log("post.message: "+post.message);
			
			var report_reason = null;
//			if(post.message === 'cG9zdCB3YXMgZGVsZXRlZA=='){
			if((post.message).substring(0, 16) === 'post_was_deleted'){
				var confirm_delete_forever = confirm("Do you really want to delete this post forever, locally, for you?");
			}else{
				do{
					var report_reason = prompt("We must not delete anything,\nbut WHY do you think, we need to delete this shit-post?", "wipe with coprofilia");
					if(report_reason != null && report_reason.length>64){
						alert("Maximum is 64 symbols for report-string. report_reason.length: "+report_reason.length+"\nTry again...");
					}
				}
				while(report_reason != null && report_reason.length>64);
			}
			
			console.log("report_reason", report_reason, ( report_reason != null ));
			console.log("confirm_delete_forever", confirm_delete_forever, (confirm_delete_forever === true));
			
			if ( ( report_reason != null ) || (confirm_delete_forever === true) ) {
				show_hide_post_code(post.hash, true);
//				console.log(post, '\n', "(post.message === 'cG9zdCB3YXMgZGVsZXRlZA==')", (post.message === 'cG9zdCB3YXMgZGVsZXRlZA=='));
//				console.log(post, '\n', "((post.message).substring(0, 16) === 'post_was_deleted')", ((post.message).substring(0, 16) === 'post_was_deleted');
//				if((post.message === 'cG9zdCB3YXMgZGVsZXRlZA==') || deleted_posts.hasOwnProperty(post.hash)){
				if(((post.message).substring(0, 16) === 'post_was_deleted') || deleted_posts.hasOwnProperty(post.hash)){
//					console.log("post was been deleted_once. Delete forever.", (post.message === 'cG9zdCB3YXMgZGVsZXRlZA=='), deleted_posts.hasOwnProperty(post.hash));
//					console.log("post was been deleted_once. Delete forever.", ((post.message).substring(0, 16) === 'post_was_deleted'), deleted_posts.hasOwnProperty(post.hash));
					deletePostFromDb(post.hash, report_reason.substring(0, ((report_reason.length<=64)?report_reason.length:64)), true);	// report post, which was been deleted_once on full-server, and delete_forever it by adding it to LocalStorage. See "/scripts/api.js", function deletePostFromDb()
//					console.log('try to remove d');
					d.remove();
				}else{
					deletePostFromDb(post.hash, report_reason.substring(0, ((report_reason.length<=64)?report_reason.length:64)), false);			// report post, and delete_once it by adding it to LocalStorage. See "/scripts/api.js", function deletePostFromDb()
//					post.message = 'cG9zdCB3YXMgZGVsZXRlZA==';
					post.message = 'post_was_deleted'+post.message;
//					console.log('try to add deleted post with opacity');
//					console.log('d', d);
//					d.find('#post_'+post.hash).html(Base64.decode('cG9zdCB3YXMgZGVsZXRlZA=='));
					d.find('#post_'+post.hash).html('post_was_deleted');
					d.css({ opacity: _deletedOpacity});
					d.find('#undo_'+post.hash).remove();

					//append "Undelete"-button for the post, which was been "deleted_once", to recovery this post.
					d.append(
						$('<button id="undelete_'+post.hash+'">')
							.text('Undelete')
							.attr('title', 'See the source code of deleted post, before undelete this.')
							.click(
								function(){
									var really_undelete = confirm(
										'Do you really want to show the post\n'+
										'(hash = '+post.hash+')\n'+
										'which was been deleted_once, client-side, with report reason:\n'+deleted_posts[post.hash][0]
									);
									if(really_undelete){
										console.log("2 Try to undelete post on lite-server...");
										delete deleted_posts[post.hash];
										save_The_Hashes_Of_Deleted_Posts_Locally();
										d.css({ opacity: ''});
										//show_hide_post_code(post.hash, true);
										post.message = $('#pre_'+post.hash).html().split('_________')[1];
										$('#post_'+post.hash).html(applyFormatting(replace_local_quotes(post.message)).substring(5)); //show post without <br/>
										$(this).remove();
//										$('#undo_'+post.hash).remove();
									}
								}
							)
					);
					//end append "Undelete"-button.
				}
				pushNotification('A post_was_deleted forever.');
			}
			else{
				$('#undelete_'+post.hash).show();	//remove "undelete"-button
				d.find('#undo_'+post.hash).remove();
				return;
			}
        }, _post_delete_timeout);
      }));
  appendFunc(d);
  
//	var post_content = escapeTags(Base64.decode(replace_deleted_once(post.message)));

  //This code need function isBase64(str), and this function in beginning of this script
	var post_content =
		escapeTags(
			Base64.decode(
						(
							(
									deleted_posts.hasOwnProperty(post.hash)
								&& 	(deleted_posts[post.hash][1] == "deleted_once")
							)
							||	((post.message).substring(0, 16) === 'post_was_deleted')
						)
							?	"cG9zdCB3YXMgZGVsZXRlZA=="
							:	post.message//((post.message).substring(0, 16) === 'post_is_reported')
								//?	(post.message).substring(16)
								//:	post.message
			)
		)
	;

  //console.log('At beginning, post_content was been: \n', post_content);				//Value of post_content at beginning, with description

	if(_detectURLs === 'true'){
		post_content = replace_local_quotes(post_content);	//replace posts to local links.
	}
	//post_content = detect_files(post_content);			//replace files to links
  
//	console.log(applyFormatting(post_content));
  var inner = $('<div>')
	.attr('id', 'post_'+post.hash)
    .addClass('post-inner')
    .addClass('post__message')
//    .html(applyFormatting(escapeTags(Base64.decode(post.message))))											//old code
    .html(
		applyFormatting(post_content)											//<--- HERE using replaced contend of post
	)
    .appendTo(d);
	
	d.append('<pre id="pre_'	+
									post.hash
									+ '"style="display: none;">'
			+	'Post hash: '		+			post.hash + '\n'
			+	'ReplyTo: '			+			post.replyTo + '\n'
			+	'Date: '			+			post.date + '(added in version 3.1 to sort posts) \n'
			+	'Message: '			+ '\n_________\n'	+
				Base64.decode(replace_deleted_once(post.message))
				.replace(	//replace HTML code to entities, to do not replase links, after.
					/[\u00A0-\u9999<>\&]/gim,
					function(i) {
						return '&#'+i.charCodeAt(0)+';';
					}
				)
			+
		'</pre>'
	)
	;

  detectPlacesCommands(inner);
  d.find('img').click(function(){
    $(this).toggleClass('full');
  });
  // detect zip files:
  var imgs = d.find('img');
  var imgcnt = imgs.length;
  if (imgcnt > 0) {
    for (var i = 0; i < imgcnt; i++) {
      var img = imgs[i];
		//console.log('img', img);
		if (img.src.startsWith('data:image/jpeg;base64,UEsDB')) {//if PK at first - then zip						//saving backward compatibility with old zipJPEGs
//			$(img).replaceWith($('<a download=file'+(i+1)+'.zip href='+img.src.replace('image/jpeg','application/zip')+'>[file'+(i+1)+'.zip]</a>')); //[file1.zip]
//			$(img).replaceWith($('<a download='+generateGuid()+'.zip href='+img.src.replace('image/jpeg','application/zip')+'>['+generateGuid()+'.zip]</a>'));	//random guid for noname zip-files.

//			$(img).replaceWith($('<a download='+Sha256.hash(img.src.replace('image/jpeg','application/zip')).substring(0,10)+'.zip href='+img.src.replace('image/jpeg','application/zip')+'>['+Sha256.hash(img.src.replace('image/jpeg','application/zip')).substring(0,10)+'.zip]</a>')); //partial sha256 for noname zip-files.

			//partial sha256 for noname zip files.
			var fileBaseLink = img.src; // Hope you'll do it right
			fileBaseLink = 	fileBaseLink.replace('image/jpeg','application/zip').replace("//", "\/\/");

			// Better without substring(). More uinque bytes -> better.
		//	var fileHash = Sha256.hash(fileBaseLink)

			// Another improvement here: to get hash from decoded file. In that case, we may check integrity easily
			var file_base64 = fileBaseLink.split("base64,")[1];
			try{
				var decoded_file = atob(file_base64);
				var fileHash = Sha256.hash(decoded_file);
				var fileName = fileHash + '.zip';
			}
			catch(err){
				var fileHash = Sha256.hash(fileBaseLink);
				var fileName = fileHash + '.zip';
				console.log("file "+fileName+" has a broken base64. Error: "+err);
			}
//			console.log("fileBaseLink", fileBaseLink);
			$(img).replaceWith(
				$('<a download=\"'+fileName+'\"	href=\"'+fileBaseLink+'\">['+fileName+']</a>')
			);
		}
		
		else if(img.src.indexOf('iVBORw0K')!==-1){				//find PNG by base64 signature
//			console.log("png found")
			img.src = (img.src).replace('image/jpeg','image/png')	//replace type for PNG-image.
		}
		else if(img.src.indexOf('R0lGODlh')!==-1){	//skip GIF by base64 signature
			//do not replace GIF image
		}

		else if(!img.src.startsWith('data:image/jpeg;base64,/9j/')){//Если не JPEG - вывести ссылку.
	//		console.log("img not zipjpeg - else");
	//		console.log('post_content: \n', post_content);
//			console.log('img.src: \n', img.src);
			
			//partial sha256 for noname zip files.
			var fileBaseLink = img.src; // Hope you'll do it right
			fileBaseLink = 	fileBaseLink.replace('image/jpeg','application/octet-stream');

			// Better without substring(). More uinque bytes -> better.
		//	var fileHash = Sha256.hash(fileBaseLink)
		
			// Another improvement here: to get hash from decoded file. In that case, we may check integrity easily
			var file_base64 = fileBaseLink.split("base64,")[1];
			
			//console.log('file_base64', file_base64.substring(0, 24));
			
			var add_to_filename = '_binary.txt';
			
			var fileName, fileHash, decoded_file;
			
			try{
				
				decoded_file = atob(file_base64);
				fileHash = Sha256.hash(decoded_file);
				
				//fileHash = Sha256.hash(img.src);	//dataURL hash.
				
				if(//if icon to download_as_binary.html
						fileHash==='5c99aa0a72257895f2bc292d29db9caea14485e19dce4327e60adfbec3cbf92e'	//if hash of base64
					||	fileHash==='a9646873a1d7e4aa81b1aa264dbc374f0451730566ebc50ce0a2235ff5845731'	//or if hash of dataURL
				){ return; }	//do nothing
				
				if(
					decoded_file.startsWith('RIFF')
				){
					if(decoded_file.substring(8, 12)=="WEBP"){
						add_to_filename = ".webp";
						fileBaseLink = fileBaseLink.replace('application/octet-stream', 'image/webp');
						img.src = fileBaseLink;
					}else if(decoded_file.substring(8, 12)=="WAVE"){
						add_to_filename = ".wav";
						fileBaseLink = fileBaseLink.replace('application/octet-stream', 'audio/vnd.wave');
						fileName = fileHash + add_to_filename;
						$(img).replaceWith(
							$('<a href="'+fileBaseLink+'" target="_blank">['+fileName+']</a>')
						);
					}else if(decoded_file.substring(8, 12)=="AVI "){
						add_to_filename = ".avi";
						fileBaseLink = fileBaseLink.replace('application/octet-stream', 'video/vnd.avi');
						fileName = fileHash + add_to_filename;
						$(img).replaceWith(
							$('<a href="'+fileBaseLink+'" target="_blank">['+fileName+']</a>')
						);
					}
				}else{
					fileName = fileHash + add_to_filename;
					//console.log('add_to_filename', add_to_filename, 'fileHash', fileHash, 'fileName', fileName, 'fileBaseLink', fileBaseLink);
					$(img).replaceWith(
						$('<a download="'+fileName+'" href="'+fileBaseLink+'">['+fileName+']</a>')
					);
				}
			}
			catch(err){
				fileHash = Sha256.hash(fileBaseLink);
				fileName = fileHash + add_to_filename;
				console.log("file "+fileName+" has a broken base64. Error: "+err);
				$(img).replaceWith(
					$('<a download="'+fileName+'" href="'+fileBaseLink+'">['+fileName+']</a>')
				);
			}
		}
    }
  }
  if (_showTimestamps == 'false') {
	if (d.find('g').length != 0)
		d.find('br').first().remove();
    d.find('g').css('display','none');
  }
//	if (post.message === 'cG9zdCB3YXMgZGVsZXRlZA==' || deleted_posts.hasOwnProperty(post.hash)){	//on lite-server, check is post-hash contains in LocalStorage?
	if ((post.message).substring(0, 16) === 'post_was_deleted' || deleted_posts.hasOwnProperty(post.hash)){	//on lite-server, check is post-hash contains in LocalStorage?
//		if(post.message === 'cG9zdCB3YXMgZGVsZXRlZA=='){											//if this post was been deleted on the server, and "deleted_once"
		if((post.message).substring(0, 16) === 'post_was_deleted'){											//if this post was been deleted on the server, and "deleted_once"
			console.log(post.hash+" was deleted_once, on full-server - just opacity for it...");						//just set opacity for this, and show replies, and source code of this post.
			d.css({ opacity: _deletedOpacity});
		}else if(deleted_posts[post.hash][1] == "deleted_once"){									//if post was been deleted_once on lite-server
			console.log(post.hash+" was deleted_once, on lite-server - just opacity for it...");
//			post.message = "post_was_deleted";															//just hide message of this post locally
			d.css({ opacity: _deletedOpacity});															//add opacity, and show replies for this post.

			//append "undo"-button for the post, which was been "deleted_once", to recovery this post.
			d.append(
				$('<button id="undelete_'+post.hash+'">')
					.text('Undelete')
					.attr('title', 'See the source code of deleted post, before undelete this.')
					.click(
						function(){
							var really_undelete = confirm(
								'Do you really want to show the post\n'+
								'(hash = '+post.hash+'\n'+
								'which was been deleted_once, client-side, with report reason:\n'+deleted_posts[post.hash][0]
							);
							if(really_undelete){
								console.log("1 Try to undelete post on lite-server...");
								delete deleted_posts[post.hash];
								save_The_Hashes_Of_Deleted_Posts_Locally();
								d.css({ opacity: ''});
								//show_hide_post_code(post.hash, true);
								post.message = $('#pre_'+post.hash).html().split('_________')[1];
								$('#post_'+post.hash).html(applyFormatting(replace_local_quotes(post.message)).substring(5)); //show post without <br/>
								$(this).remove();
//								$('#undo_'+post.hash).remove();
							}
						}
					)
			);			
		}else if(deleted_posts[post.hash][1] == "post_was_deleted_forever"){									//if post was been deleted_forever on lite-server
			console.log("post_was_deleted_forever, not show it...");												//just don't show this locally.
//			d.html({""});
		}
	}
	
  return d;
}

function append_thread_and_post_links(p, post, is_found_thread_string){
	/*
		Tree of posts:
		00000000000000000000000000000000 (not available, root)
			root post (hidden)
				Categories (post about ability to create catecories, this is a reply for root post)
					Category (boards, this is a replies for this post, like in thread)
						Threads in category (are replies in category)
							posts in threads (are replies in thread)
								posts-replies for posts (are replies for posts, like in threads)
								thread not found for this posts, but this posts can be a threads, if there will be replies.
		So, now, need to move by this tree...
		posts can be a threads, so threads as replies in cathegory, will be a "subcategories", but this is just threads...
	*/	
	//console.log("post", post.hash, post.replyTo, is_found_thread_string);

	if(post.replyTo === "00000000000000000000000000000000"){			//if root post
		is_found_thread_string = "Root Post";
	}else if( post.hash === _categories ){								//if categories
		is_found_thread_string = "Categories";
	}else if( post.replyTo === _categories ){							//if category
		is_found_thread_string = "Category";
	}else if(list_of_categories_hashes.indexOf(post.replyTo)!==-1){		//if subcategory (thread)
		is_found_thread_string = "Subcategory";
	}else if( is_found_thread_string !== "Thread" ){			//if just post
		is_found_thread_string = "Unanswered post";
	}else{																//else, just post, which contains replies, and this is like thread.
		is_found_thread_string = "Thread";		
	}
	
	//console.log("post", post.hash, post.replyTo, is_found_thread_string);

	//Append "Thread not found"
	p.append(
		$('<a>')
		.attr('href', '#'+ (is_found_thread_string=="Categories"?"category":"thread") + ( (is_found_thread_string=="Root Post")? post.hash : post.replyTo) )
		.html(
			'<span class="glyphicon glyphicon-menu-hamburger" aria-hidden="true"></span>'+
			'<span class="btn-title">&thinsp;'
			+
			(
				(is_found_thread_string=="Root Post")
				?"Root Post"
				:(is_found_thread_string=="Categories")
					?"Root Post"
					:(is_found_thread_string=="Category")
						?"Categories"
						:(is_found_thread_string=="Subcategory")
							?"Category"	//subcategory - this is a reply to category.
							:"Thread"	//else, unanswered posts are replies to another posts, and this is a threads for this posts.
			)
			+
			'</span>'
		)
		.click(function(){
			//loadRootThread($(this).parent().attr('id'));
		})
	);

	//Append "Link to single post"
	p.append(
		$('<a>')
		.attr('href', '#thread' + post.hash)
		.html(
			'&nbsp;&nbsp;&nbsp;<span class="glyphicon glyphicon-paperclip" aria-hidden="true"></span>'+
			'<span class="btn-title">&thinsp;'
			+
			(is_found_thread_string === "Subcategory" ? "Thread" : is_found_thread_string)
			+'</span>'
		)
		.click(function(){
			//loadRootThread($(this).parent().attr('id'));
		})
	);
}

function loadReplies(hash, offset, highlight) {
//  $.get('../api/replies/' + hash)
  $.get('../api/replies/' + hash +'-'+ 'true')	//last parameter appendText = true, to append text info to post.message, if post was been deleted or reported.
    .done(function(arr){
      arr = JSON.parse(arr);
      if (arr.length == 0) return;
      for (var i = arr.length-1; i >= 0; i--) {
		
		//if(arr[i]===null) break;	//wrong captcha was been with this error.
        if(arr[i]===null) continue;
		
        var deleted = Base64.decode(arr[i].message) == _postWasDeletedMarker;
        if (_showDeleted == 'false' && deleted) continue;
        var p = addPost(arr[i], function(d) { d.insertAfter($('#'+hash)); }, false)
        if (p){
            p.css('margin-left', offset * _treeOffsetPx + 'px');
            if (deleted) p.css({ opacity: _deletedOpacity });
            loadReplies(arr[i].hash, offset + 1, highlight);
            if (highlight == arr[i].hash) {
              p.addTemporaryClass('high', 8000);
            }
			append_thread_and_post_links(p, arr[i], "Unanswered post")
        }
      }
      vid_show()
    });
}

//3.1
function AddBriefView(hash, deleted, highlight)
{
	//console.log('AddBriefView - hash:', hash);
//	$.post('../api/getlastn/' + hash, '3')
	$.post('../api/getlastn/', hash +'-'+ '3' + '-' + 'true')	//true, means appendText to base64 encoded message of post: ("post_was_deleted", and "post_is_reported")
	.done(function(brief)
	{						
		brief = JSON.parse(brief);
//		brief.reverse();	//maybe no need reverse..., because: console.log('brief', brief);
		if(brief.length==0) return;
		for(var i=0;i<brief.length;i++)
		{
//			if (brief[i].message === 'cG9zdCB3YXMgZGVsZXRlZA=='){//for deleted posts
			if ((brief[i].message).substring(0, 16) === 'post_was_deleted'){//for deleted posts
				deleted = true;										//show deleted post with opacity
				//continue;											//skip deleted posts
			}
			
			var p1=addPost(brief[i], function(d){d.insertAfter($('#'+hash));}, false, true);
			if(p1)
			{
				p1.css('margin-left', 2 * _treeOffsetPx + 'px');
//				if (deleted) p1.css({ opacity: _deletedOpacity });	//make all posts deleted, if category deleted. But if post in category deleted, all posts with opacity... disable it...
				if (highlight == brief[i].hash) {
				  p1.addTemporaryClass('high', 8000);
				}
				append_thread_and_post_links(p1, brief[i], "Unanswered post")
			}
		}
	});
}

function loadThread(hash, highlight) {
  thisPosts = [];
//  $.get('../api/replies/' + hash)
  $.get('../api/replies/' + hash +'-'+	'true')	//last parameter appendText = true, to append text info to post.message, if post was been deleted or reported.
    .done(function(arr){
      arr = JSON.parse(arr);
	  //console.log('loadThred, arr: ', arr);
      if (arr.length > 0) {
        $('#thread').empty();
      } else { 
        _depth -= 1; 
        pushNotification('This thread/category is empty.');
        //return; 					//don't return and show post
		$('#thread').empty();		//don't repeat posts
      }
//      $.get('../api/get/' + hash)
      $.get('../api/get/' + hash +'-'+ 'true')	//last parameter appendText = true, to append text info to post.message, if post was been deleted or reported.
        .done(function(post){
          post = JSON.parse(post);
          if (_depth > 0) {
            $('#thread').append(
              $('<a>')
                .attr('href', (post.replyTo != _categories) ? ('#category' + post.replyTo) : ('#'))
                .html('<b><span class="glyphicon glyphicon-arrow-up" aria-hidden="true"></span>Up</b>')
                .click(function(){
                  //_depth -= 1;
                  //loadThread(post.replyTo);
                }));
          }
          $('#thread').append('&nbsp;');
          $('#thread').append(
            $('<a>')
              .attr('href','javascript:void(0)')
              .html('<span class="glyphicon glyphicon-refresh" aria-hidden="true"></span>Refresh')
              .click(function(){
//                reloadParams();
                setTimeout(function(){
                  loadThread(hash);                  
                }, 500);
              }));
          $('#thread').append(
            $('<a>')
              .attr('href','javascript:void(0)')
              .html('<span class="glyphicon glyphicon-chevron-down" aria-hidden="true"></span>Sort by date')
              .click(function(){
                $('.separat').remove();
                var thread = $('g').parent().parent().parent();
                var posts = $('g').parent().parent();
                var first = $('.post')[0];
                var sorted = posts.sort(function(a,b){
                a.style.marginLeft = 0;
                b.style.marginLeft = 0;
                var d1 = new Date(a.innerHTML.replace(/.*\<g\>/,'').replace(/\<\/g\>.*/,'').replace(/^[A-z ,]{4,5}/,'').replace(/,.*/,''));
                var d2 = new Date(b.innerHTML.replace(/.*\<g\>/,'').replace(/\<\/g\>.*/,'').replace(/^[A-z ,]{4,5}/,'').replace(/,.*/,''));
                return d1 > d2 ? 1 : -1;
                });
                $('<div class=separat>End of sorting. Posts without timestamps:</div>').insertAfter($('#thread_top'));
                sorted.detach().insertAfter($('#thread_top'));
                if (sorted[0] != first)
                  $(first).detach().insertBefore(sorted[0]);

                posts = $('.post');
                for (var i = 1; i < posts.length; i++) {
                  $(posts[i]).prepend("<span style=font-size:75% class=separat>#"+(i+1)+"</span>");
                  var href = $(posts[i]).find('a')[0].href;
                  href = href.split('#')[1];
                  var parent = $('#' + href);
                  var reflink = $('<span class=separat><a style=font-size:75% href=#'+posts[i].id+'>&gt;&gt;'+shortenHash(posts[i].id)+'</a> </span>');
                  var safI = i;
                  $(reflink.find('a')).click(function(){
                    console.log('click' + $(this).attr('href'));
                    $($(this).attr('href')).addTemporaryClass('high', 1000);
                  });
                  parent.append(reflink);
                }

              })
            );
          $('#thread').append(
            $('<a>')
              .attr('href','javascript:void(0)')
              .html('<span class="glyphicon glyphicon-chevron-up" aria-hidden="true"></span>Reversed sort')
              .click(function(){
                $('.separat').remove();
                var thread = $('g').parent().parent().parent();
                var posts = $('g').parent().parent();
                var first = $('.post')[0];
                var sorted = posts.sort(function(a,b){
                a.style.marginLeft = 0;
                b.style.marginLeft = 0;
                var d1 = new Date(a.innerHTML.replace(/.*\<g\>/,'').replace(/\<\/g\>.*/,'').replace(/^[A-z ,]{4,5}/,'').replace(/,.*/,''));
                var d2 = new Date(b.innerHTML.replace(/.*\<g\>/,'').replace(/\<\/g\>.*/,'').replace(/^[A-z ,]{4,5}/,'').replace(/,.*/,''));
                return d1 < d2 ? 1 : -1;
                });
                $('<div class=separat>End of sorting. Posts without timestamps:</div>').insertAfter($('#thread_top'));
                sorted.detach().insertAfter($('#thread_top'));
                if (sorted[0] != first)
                  $(first).detach().insertBefore(sorted[0]);
              })
            );
		$('#thread').append(
			$('<a id="thread_top">')
			.attr('href','javascript:void(0)')
			.html('<span class="glyphicon glyphicon-remove" aria-hidden="true"></span>Delete All')
			.click(
				function(){
					if (confirm('Are you sure you want delete all posts that you currently see?')) {
						$('.glyphicon-trash').click();
					}
				}
			)
		);

			p2 = addPost(post, function(d){ d.appendTo($('#thread')); }, false, false);
			if(p2){
				append_thread_and_post_links(p2, post, "Category")
			}
			if (_depth == 1 || _depth == 0){
				//without reverse.
				//arr.reverse();	//was been reverse, but reverse now inside PostDb.cs, GetReplies()
			}else{
				arr.reverse();	//maybe need to reverse another, but seems like good, without it.
			}
//			console.log("arr.length", arr.length);
			for (var i = 0; i < arr.length; i++) {
//				console.log('nanoclient.js: arr: ', arr[i].hash, arr[i]);
				if(arr[i]===null)continue;
				var deleted = Base64.decode(arr[i].message) == _postWasDeletedMarker;
				if (_showDeleted == 'false' && deleted){continue;}
				var p = addPost(arr[i], function(d) {d.appendTo($('#thread'));}, true)
				if (p){
					p.css('margin-left',  _treeOffsetPx + 'px');
					if (deleted){
						p.css({ opacity: _deletedOpacity});
					}
					if (highlight == arr[i].hash) {
						p.addTemporaryClass('high', 8000);
					}
					if (_depth > 1) {
						loadReplies(arr[i].hash, 2, highlight);
					}
					if(_depth==1)	//from client 3.1	
					{
						AddBriefView(arr[i].hash, deleted, highlight);
					}
					append_thread_and_post_links(p, arr[i], "Thread")
				}
			}
          vid_show()
        });
    });
}