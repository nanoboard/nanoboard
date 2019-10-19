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

function deletePostFromDb(hash) {
  $.post('../api/delete/' + hash)//;
//  $.get('../api/delete/' + hash)//;
  .done(function(r){
	console.log('delete post from db - response: ', r);
	notifyAboutPostCount(true);
  });
}