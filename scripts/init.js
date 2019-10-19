function stripTags(text) {
  text = text.replace(/<g>.*<\/g>/gim, '');
  text = text.replace(/<.{1}>/gim, '');
  text = text.replace(/<\/.{1}>/gim, '');
  text = text.replace(/<br?\/>/gim, ' ');
  text = text.replace(/^\s*/gim, '');
  text = text.replace(/\s*$/gim, '');
  text = text.replace(/\s/gim, '&nbsp;');
  if (text.length > 48) text = text.substring(0, 48) + '...';
  return text;
}

var list_of_categories_hashes = [];	//list of hashes the post of categories.
//Need to select subcategories, to add link to category. See "function append_thread_and_post_links", in nanoclient.js

function updateCategoriesBar() {
  $('#categories').empty();
  $.get('../api/replies/' + _categories)
    .done(function(replies){
	  replies = JSON.parse(replies);
	  for (var i = 0; i < replies.length; i++){
	    var reply = replies[i];
	    if (reply.message != 'cG9zdCB3YXMgZGVsZXRlZA==' && stripTags(applyFormatting(Base64.decode(reply.message)))!==''){ // if category post was deleted or empty - skip
	       $('#categories').append('<a href="#category'+reply.hash+'">['+stripTags(applyFormatting(Base64.decode(reply.message)))+']</a> ');	//else - show category
		   list_of_categories_hashes.push(reply.hash);	//and save the hash of category post.
		}
	  }
    });
}

function getPosts(string_with_hashes_comma) {						//string, with hashes, joined with comma.
  $.post('../api/getposts/'+'POST/', string_with_hashes_comma)		//send string, using POST-query
  //$.get('../api/getposts/' + string_with_hashes_comma)			//send string, using GET-query. Warning! URL-length have the bytesize-limit...
  
  //Queue test. Test creating PNG with specified posts.
  //$.post('../api/png-create/', string_with_hashes_comma)			//send string, using POST-query
  //$.get('../api/png-create/' + string_with_hashes_comma)			//send string, using GET-query. Warning! URL-length have the bytesize-limit...
    .done(function(replies){	//some test function for response...
	  replies = JSON.parse(replies);
	  for (var i = 0; i < replies.length; i++){
	    var reply = replies[i];
	    if (reply.message != 'cG9zdCB3YXMgZGVsZXRlZA==') // if category post was deleted - skip, else
	       //$('#categories').append('<a href="#category'+reply.hash+'">['+stripTags(applyFormatting(Base64.decode(reply.message)))+']</a> ');
		   console.log(reply);
	  }
    })
    .fail(function() {//if fail...
      pushNotification('Failed to add post (exists or too big).');
    });
}
//getPosts('f682830a470200d738d32c69e6c2b8a4,cd94a3d60f2f521806abebcd3dc3f549,bdd4b5fc1b3a933367bc6830fef72a35'); //test.

var places = '';
var update_places = false;				//false - if no need to regular update, true - if need regular update (2 tabs opened, places changed in second tab, then update links on first tab).

function updatePlacesBar_once(){
	var to_false = !update_places;
	update_places = true;
	if(to_false==true){updatePlacesBar();}
	update_places = !to_false;
}

function updatePlacesBar() {
	if(update_places==false) {
		//console.log("No need to update places...");
		return;
	}
	$.get('../api/paramget/places')
	.done(
		function(v){
			if(places!==v){
				places = v;
				v = v.split('\n');
				$('#placesd').empty();
				$('#placesd').append('<b><a href="javascript:void(0)" onclick="updatePlacesBar_once();" title="Click here to update places links from settings, if this was been changed...">Places</a> (to post PNG containers to):</b><br/>')
				for (var i = 0; i < v.length; i++) {
					if (v[i].length > 0 && v[i][0]=='#') continue;
					$('#placesd').append('• <a target=_blank href="'+v[i]+'">'+v[i]+'</a><br/>')
				}
				$('#placesd').append('You can edit this list on <a href=params.html>[Settings]</a> page.');
				setTimeout(updatePlacesBar, 1000);
			}else{
				setTimeout(updatePlacesBar, _post_count_notification_time);
			}
		}
	);
}
setTimeout(
	function(){
		updatePlacesBar_once();
	},
	2000
);

var postCount = 0;
var update_post_count = false;				//true when need to regular update, false - stop update post count...

function update_Post_count_once(){
	var to_false_count = !update_post_count;
	update_post_count = true;
	if(to_false_count==true){notifyAboutPostCount(true);}
	update_post_count = !to_false_count;
}

function notifyAboutPostCount(update_param) {
//	console.log('update_post_count', update_post_count);
	if( update_post_count == false ){
		if( update_param !== true ){
			return;
		}
	}

	$.get('../api/count')
/*
$.ajax({
	url: '../api/count',
	headers: {
        'Connection': 'Keep-Alive',								//can not open keep-alive connection to using long-polling
        'Content-Type': 'text/html; charset=utf-8',
		'Keep-Alive': 'timeout=5, max=1000'
    }
//  data: data,
//  success: success,		//success function === .done()
//  dataType: dataType
})
*/
	.done(function(data){
		data = parseInt(data);
//		console.log('total posts: '+data);
		if (data != postCount) {
			if (postCount != 0) {
				var countStr = (data - postCount).toString();
				pushNotification(countStr + ' post' + numSuffix(countStr) + ' added to database.', _post_count_notification_time);
			}
			postCount = data;
			setTimeout(function(){notifyAboutPostCount();}, 1000);
		}else{
			setTimeout(function(){notifyAboutPostCount();}, _post_count_notification_time);
		}
		$('#statusd1').html('<a href="javascript:void(0);" onclick="notifyAboutPostCount(true);">Posts (including deleted once): '+postCount+'</a>');
		$('#statusd2').html('<a href="javascript:void(0);" onclick="notifyAboutPostCount(true);">Posts (including deleted once): '+postCount+'</a>');
    })
    .fail(function(){
      pushNotification('Connection to server lost.', 900);
	  setTimeout(function(){notifyAboutPostCount(true);}, _post_count_notification_time);
    });
}


var _location = '';
var max_bytelength_in_container_to_resize = 140000;	//max bytelength in container to resize this.
$(function() {
  var collectionRun = true;
  var creationRun = true;

  var max_connections = 10;
  $('#png-collect').click(function(){
	collectionRun = true;

	update_post_count = true;		//when collect - update post count
	notifyAboutPostCount(true);			//run update post count, and repeat by interval.
	
    //$.get('../api/png-collect')																//collect, using RAM, without saving files. Max_connections = 6 (by default)
    //$.get('../api/png-collect'+'/'+"collect_using_files|save_files|16")						//collect, using files, with saving this, and do it with max_connections = 16
	//$.get('../api/png-collect'+'/'+encodeURIComponent("collect_using_files|save_files"))		//collect, using files, without deleting. Parameters sending with encodeURIComponent()
	//$.post('../api/png-collect/', encodeURIComponent("collect_using_files|delete_files"))		//collect, using files, with deleting this. Post query, with encoded params.
    //$.post('../api/png-collect', encodeURIComponent("collect_using_RAM|save_files|8"))		//test save, from RAM, max_connections = 8
	//$.post('../api/png-collect', "collect_using_RAM|save_files|"+max_connections)								//test save, from RAM, max_connections = 10
	//$.post('../api/png-collect', encodeURIComponent("collect_using_RAM|delete_files|"+max_connections))		//test save, from RAM, max_connections = 10
	
	//This need to try run collect from "download" folder: http://127.0.0.1:7346/download/
	$.post('../api/png-collect', encodeURIComponent("collect_using_RAM|do_not_save_and_do_not_delete|"+max_connections))	//test save, from RAM, without deleting and adding posts. max_connections = 10.
	//Success!
	
	//collect to download folder:
//	$.post('../api/png-collect', encodeURIComponent("collect_using_RAM|save_files|"+max_connections))	//test save, from RAM, without deleting and adding posts. max_connections = 10.
	//Success!

	.done(
		function(data){				//when PNG collect finished
			console.log("done png-collect GET-query: "+data);	//show this
			setTimeout(
				function(){
					update_post_count = false;					//and disable update post-count, 
				},
				500												//after timeout
			);
			
		}
	);
    //$('#png-collect').hide();
    $('#png-collect').addClass('disabled');
	$('#collect_text').text('Collection started...');
    pushNotification('PNG collection started.');
  });
  $('#png-create-bookmark').click(function(){
	if($(this).hasClass("active")){
		$('#createPNG').hide();
		$('#page').show();
		$(this).removeClass("active");
	}else{
		$(this).addClass("active");
		console.log('show...');
		$('#page').hide();
	}
	pushNotification('Wait for loading data...');
  });
  $('#png-create').click(function(){
	creationRun = true;
	var from_queue = 			document.getElementById('from_queue').value;
	var from_last_posts = 	document.getElementById('from_last_posts').value;
	//console.log(	'init.js: png-create onclick.',
	//				'\n from_last_posts: ', from_last_posts,
	//				'\n, dataURL: ', dataURL,
	//				'\n queue: ', queue,
	//				'\n from_queue: ', from_queue
	//);
    pushNotification('PNG creation started.');
    $('#png-create').addClass("disabled");
	$('#png-create-text').text("Wait for generate...");
	
//    $.get('../api/png-create')

	console.log(
		'random posts from last: ', randlp.value,
		'random_on_client_side', random_on_client_side,
		'list_random_posts.length', list_random_posts.length,
		'list_random_posts', list_random_posts
	);
	if(random_on_client_side==true){								//if random posts was been generated on client-side.
	
		console.log('random_on_client_side', random_on_client_side);
		console.log('list_random_posts', list_random_posts);
		console.log('current_queue before modification: ', current_queue);
		current_queue = current_queue.concat(list_random_posts);	//add items from list_random_posts -> to current_queue
		console.log('current_queue after concat: ', current_queue);
		
		console.log("current_queue before mix", current_queue);
		current_queue = shuffle(current_queue);
		console.log("current_queue before mix", current_queue);
	}
	
	
    $.post(		'../api/png-create/',
				startlp.value+'-'+from_last_posts+'\n'+
				(						// and dataURL with selected source image. Delimiter is '\n' between this two blocks.
					(dataURL!=="")		//if dataURL not empty string
						?
						dataURL+'\n'	//append dataURL
						:
						'No_dataURL_specified_for_source_image.\n'				//else, append string with length over 32 symbols
				)
				+	current_queue.join(',')+'\n'	//send queue
				+	current_queue.length+'\n'
				+	( (random_on_client_side===true) ? 0 : randlp.value)+'\n'
				+	'max_bytelength='+max_bytelength_in_container_to_resize
			)
      .done(function(response){
		console.log("done png-create post-query..."+response);
//		Create_PNG_result.innerHTML = response;									//add response to div on PNG creation tab
//		current_queue = [];			//remove all packed hashes from current_queue

		$('#png-create').removeClass('disabled');										//this can activated in notifyAboutNotifications function
		$('#png-create-text').text('Create PNG');										//show crete button
		pushNotification('PNG creation finished (check your "upload" folder).');
		creationRun = false;
      })
	  .fail(function(){creationRun = false;});
  });

function check_avails(hide) {//hide true when need to hide notif.							//run checking avails
	var hide = (typeof hide === 'undefined') ? false : hide;
    if (creationRun){																			//if png creating started
		$.get('../api/png-create-avail')
		.done(function(data){
			//$('#png-create').show();
			if(data=='Finished.'){
				$('#png-create').removeClass('disabled');										//this can activated in notifyAboutNotifications function
				$('#png-create-text').text('Create PNG');										//show crete button
				if(hide==false){
					pushNotification('PNG creation finished (check your "upload" folder).');
				}
				creationRun = false;
				setTimeout(check_avails(hide), _post_count_notification_time);						//using long timeout
			}else{
				$('#png-create').addClass("disabled");											//disable button
				$('#png-create-text').text("Wait for generate...");
				creationRun = true;
			}
		})
		.fail(function(){
			$('#png-create').addClass("disabled");
			$('#png-create-text').text("Wait for generate...");
			//setTimeout(check_avails, 100);														//update after short timeout
			
		});
	}
	else if (collectionRun){
		$.get('../api/png-collect-avail')
		.done(function(data){
			//$('#png-collect').show();
			if(data=="Finished."){
				$('#png-collect').removeClass('disabled');										//enable button
				$('#collect_text').text(' Collect PNG');
				if(hide==false){
					pushNotification('PNG collection finished.');
					console.log("PNG collection finished... ", 'total_posts_available: ', total_posts_available, 'postCount: ', postCount);
					total_posts_available = postCount;
					//console.log("collection finished...");
				}
				collectionRun = false;
				setTimeout(function(){check_avails(hide);}, _post_count_notification_time);
			}else{
				$('#png-collect').addClass("disabled");											//disable button
				$('#collect_text').text('Collection started...');
				collectionRun = true;
			}
		})
		.fail(function(){
			$('#png-collect').addClass("disabled");												//disable button
			$('#collect_text').text('Collection started...');
			//setTimeout(check_avails, 100);														//update after short timeout
			
		});
	}else{
		setTimeout(function(){check_avails(hide);}, _post_count_notification_time);
	}
}

var hide_first_notif = true;					//true to hide first notif, false to show.
function check_avails_once(){
	//console.log('run check avails once...');
	if(collectionRun || creationRun){
		check_avails(hide_first_notif);
		//console.log('check_avails('+hide_first_notif+')......');

		if(hide_first_notif==true){
			setTimeout(
				function(){
					hide_first_notif = false;
				},
				5000		//static interval to don't show notif about "collect png finished" and "create png finished", and don't show this until the loading page.
			);
		}
	}
}
	//setInterval(check_avails, _post_count_notification_time*4);
	//check_avails();																				//run this once.
	
var generated_container_name = "";
var dont_show_notif = false;
var add_saved = false;					//add pathway for saved file without replace hashes. true if add, false if innerHTML
var already_saved_to = "";				//this string need to save notif, when file saved, but hashes still not returned.

function notifyAboutNotifications() {
	//console.log('run check avails from notif.....');
	check_avails_once();
	/*
		When "Collect PNG" running,
		The content of posts in parsed containers, after downloading this files -
		this content contains in the notifications.
		There can be base64 of images, etc...
		If downloading PNG is faster, and update notifications slower,
		the usage of memory is growing, because not all notifications downloaded
		using /notif query, and before this - this is stored in the nanodb-server memory.
		This can return OutOfMemoryException for many pictures and fast collect.
	*/
	$.get('../notif')
		.done(function(data){
			//if(data.indexOf("Saved PNG to /upload/")!==-1){		//Make button clickable, after receive last notification.
			//	$('#png-create').removeClass('disabled');
			//	$('#png-create-text').text('Create PNG');
			//}
		if(data!==''){
//			pushNotification(applyFormatting(data), _post_count_notification_time);
//			setTimeout(notifyAboutNotifications, 1000);

			//console.log('notify data'+data, data.substring(0,21));
			if(data.substring(0,21)=='Saved PNG to /upload/'){
				generated_container_name = data.substring(21);
				if(add_saved==true){									//if need add
					if(Create_PNG_result.innerHTML.indexOf(generated_container_name)!==-1){
						Create_PNG_result.innerHTML += data;				//just add
					}else{
						already_saved_to = data;
					}
					add_saved = false;									//and false
				}
				else{													//else
					if(Create_PNG_result.innerHTML.indexOf(generated_container_name)!==-1){
						Create_PNG_result.innerHTML = data;					//insert in html and clear previous results.
					}else{
						already_saved_to = data;
					}
				}
				//current_queue = [];												//remove all packed hashes from current_queue
			}else if(data.substring(0,29)=="Hashes of posts, packed into "){
				//Create_PNG_result.innerHTML += 		'<br>'+	data.split('["')[0]
				Create_PNG_result.innerHTML = 		'<br>'+	data.split('["')[0]
												+	'<br><div id="packed_hashes"></div>';
				//var packed_hashes_list_element = document.getElementById('packed_hashes');
				
				var packed_hashes_array = [];
				packed_hashes_array = (data.indexOf('["')!==-1) ? JSON.parse('["' + data.split('["')[1]) : [];
				//var JSON_packed_hashes = JSON.parse('["' + data.split('["')[1]);
				//packed_hashes_list_element;
				console.log("packed_hashes_array", packed_hashes_array);
				update_hashes_of_last_post(packed_hashes_array, 'packed_hashes', packed_hashes_array.length);
				if(already_saved_to===""){
					add_saved = true;
				}else{
					Create_PNG_result.innerHTML += already_saved_to;
					already_saved_to = "";
				}

				console.log(
							'packed hashes displayed!',
							'\n',	'queue = ', queue,
							'\n',	'current_queue = ', current_queue
				);

					//dequeue all packed hashes from current queue.
				for(i=0; i<=packed_hashes_array.length; i++){
					queue_remove(current_queue, packed_hashes_array[i]);
					//queue_remove(queue, packed_hashes_array[i]);
				}

				console.log(
							'after remove packed hashes: ',
							'\n',	'queue = ', queue,
							'\n',	'current_queue = ', current_queue
				);
				
					//update values, after massive dequeue
				piq.innerHTML = packed_hashes_array.length;
			
				if(fq.value>packed_hashes_array.length){
					fq.value = packed_hashes_array.length;
					pfq.innerHTML = packed_hashes_array.length;
				}
				

				update_hashes_of_last_post(current_queue, 'queue', current_queue.length); //generate list with hashes of packed posts
				//update_hashes_of_last_post(queue, 'queue', queue.length); //generate list with hashes of packed posts
				
				if(fq.value>queue.length){
					fq.value = queue.length;
					pfq.innerHTML = queue.length;
				}
				
				console.log('current_queue.length', current_queue.length);
				console.log('queue.length', queue.length, 'fq.value', fq.value, 'pfq.innerHTML', pfq.innerHTML);
				

				change_values();	//after update queue

				//current_queue = [];
				
//				Create_PNG_result.innerHTML += '<br>'+data;
				dont_show_notif = true;
			}else if(data == "Your containers dir is empty! Add container(s)"){
				Create_PNG_result.innerHTML = 'Your containers dir is empty! Add container(s). You can generate container <a href="http://127.0.0.1:7346/pages/convert-to-PNG.html" target="_blank">here</a>.';
			}
/*
			else if(data.substring(0,29)=="[b][g]Extracted post:[/g][/b]"){
				console.log('EXTRACTED POST...', _post_count_notification_time);
				pushNotification(applyFormatting(data), 5000);	//add static timeout to show posts after extract when collect.
				dont_show_notif = true;
			}
*/

			if(dont_show_notif === true){dont_show_notif = false;}
			else{
				pushNotification(applyFormatting(data), _post_count_notification_time);
			}
			//console.log('notif received - short timeout');
			setTimeout(notifyAboutNotifications, 100);			//short timeout if notify received.
		}else{
			//console.log('notif not received - long timeout');
			setTimeout(notifyAboutNotifications, _post_count_notification_time);
		}
    })
    .fail(function(){
		setTimeout(notifyAboutNotifications, _post_count_notification_time);
    });
}


  reloadParams();
	  
  setInterval(function() {
    var incl = ''.includes == undefined ? function(x,y) { return x.contains(y); } : function(x,y) { return x.includes(y); };
    var newLocation = window.location.href.toString();
    if (newLocation != _location) {
      _location = newLocation;
      if (_location.endsWith('#') || _location.endsWith('html')) {
//      if (_location.substr(-1) == '#' || _location.substr(-4) == 'html') {	//if last symbols is this
        _depth = 0;
        loadThread(_categories);
      } else if (incl(_location, '#thread')) {
        _depth = 2;
        loadThread(_location.split('#thread')[1]);
      } else if (incl(_location, '#category')) {
        _depth = 1;
        loadThread(_location.split('#category')[1]);
      } else if (incl(_location, '#last')) {
        showLast(parseInt(_location.split('#last')[1]));	//show last N-posts
      } else {
        // do nothing intentionally
      }
    }
  }, 100);

  updateCategoriesBar();

  //setInterval(function(){
  //  updatePlacesBar();
  //}, 2000);
  
/*
  setInterval(function(){
    retranslate();				//how this working???
  }, 300000);
*/

function run_retranslate(){
	//repeat retranslate() by Interval, if _instantRetranslation==true, else don't repeat
	if( _instantRetranslation == true){
		retranslate();
		setTimeout(
			function(){
				run_retranslate();
			},
			300000
		);
	}else{
		console.log("_instantRetranslation = "+_instantRetranslation+". No need to start instant retranslation. ");
	}
}
run_retranslate();	//Run once.

/*
  setInterval(function(){
    checkVersion();
  }, 300000);
  //checkVersion();				//this running once in checkver.js - function check_version_when_no_updates();
*/

  
  //console.log('remind_if_updates_exists', remind_if_updates_exists);
  
  
  
  
//  updatePlacesBar();				//disable it to stop XHR queries	//This can be enabled to regular update places. If this updated in second tab, this updating in the first. But now, using function updatePlacesBar_once
//  notifyAboutPostCount();			//disable it to stop XHR queries
	update_Post_count_once();
  notifyAboutNotifications();		//disable it to stop XHR queries
  
  
  //setInterval(function(){ notifyAboutPostCount(); }, 1000);
  //setInterval(function(){ notifyAboutNotifications(); }, _post_count_notification_time/4);
  //setInterval(function(){ notifyAboutNotifications(); }, _post_count_notification_time/4);
});
