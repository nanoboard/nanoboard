function sendPostToDb(post) {
  $.post('../api/add/' + post.replyTo, post.message)
    .done(function(response){
      addPost(JSON.parse(response), function(d){ d.insertAfter($('#'+post.replyTo)); }, false)
        .addTemporaryClass('high', 1000)
        .css('margin-left', parseInt($('#'+post.replyTo).css('margin-left'))+10+'px');
      pushNotification('Post was successfully added.');
      onAdd(post);

	  update_post_count = true;
	  notifyAboutPostCount(true);
	  update_post_count = true;
    })
    .fail(function() {
      pushNotification('Failed to add post (exists or too big).');
    });
}

function mockSendPostToDb(post) {
	//console.log('mockSendPostToDb');
  addPost(post, function(d){ d.insertAfter($('#'+post.replyTo)); }, false)
    .addTemporaryClass('high', 1000)
    .css('margin-left', parseInt($('#'+post.replyTo).css('margin-left'))+10+'px');
  pushNotification('Post was successfully added.');
  onAdd(post);

  notifyAboutPostCount(true);
}

/*
	JSON-object with deleted posts.
	{
		"hash1": ["reason", "post_was_deleted_once"],
		"hash2": ["reason", "post_was_deleted_forever"],
		//...
	}
	This array will be saved in LocalStorage, client-side, and will be updated from LocalStorage.
	Here will be stored the hashes of posts, whach was been "post_was_deleted_once" or "post_was_deleted_forever".
	If post-hash contains in this array, this post will be "post_was_deleted_once" or "post_was_deleted_forever" client-side,
	only for the current client, not on the server.
*/
var deleted_posts = {};	//define empty array, by default.

// save the object with the hashes of deleted posts - locally, in LocalStorage
function save_The_Hashes_Of_Deleted_Posts_Locally() {
	window.localStorage.setItem("deleted_posts", JSON.stringify(deleted_posts));
}
		
// Load object from LocalStorage - read the string of parameter "deleted_posts" in LocalStorage, and load "deleted_posts" object from there.
function Read_The_Hashes_Of_Deleted_Posts() {
	//print the value of the local storage "database" key
	if (window.localStorage.getItem("deleted_posts") == null) {
		//console.log("no any value in localstorage");
	} else {
		deleted_posts = JSON.parse(window.localStorage.getItem("deleted_posts"));
	}
}	

Read_The_Hashes_Of_Deleted_Posts();	//Run the loading object with the hashes of deleted_posts, from LocalStorage.


function deletePostFromDb(hash, reason, delete_forever) {
	
//	Read_The_Hashes_Of_Deleted_Posts();		//update deleted_posts from LocalStorage.
	
	if(typeof delete_forever === 'undefined'){
		delete_forever = false;
	}
	if(!delete_forever){	//if post don't delete_forever - send report to lite-server

		reason = reason.substring(0, ((reason.length<=64)?reason.length:64));
//		$.get('../api/report/'+encodeURIComponent(hash+"|"+reason))				//		get - URL parameter
		$.post('../api/report/', hash+"|"+reason)								//		POST as URL parameter

//  	$.post('../api/delete/' + hash)
//  	$.get('../api/delete/' + hash)
		.done(function(r){
			console.log('delete post from db - response: ', r);
			notifyAboutPostCount(true);
		});
	}else{					//if post was been "post_was_deleted_once" on full-server - don't send report for this post, and just delete forever it.
		notifyAboutPostCount(true);		
	}

	//delete post locally, on client-side.
	if (!deleted_posts.hasOwnProperty(hash)){					//if post already contains in deleted_posts
		if(delete_forever == true){									//if delete_forever (for posts, which was been "post_was_deleted_once" on full-server)
			deleted_posts[hash] = ["post_was_deleted_once", "post_was_deleted_forever"];			//just delete_forever, without send any report.
		}else{
			deleted_posts[hash] = [reason, "post_was_deleted_once"];			//else delete this once.
		}
	}else if (deleted_posts.hasOwnProperty(hash)){
		deleted_posts[hash] = [deleted_posts[hash][0], "post_was_deleted_forever"];		
	}
	save_The_Hashes_Of_Deleted_Posts_Locally();
}