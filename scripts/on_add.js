var transportUri = 'http://127.0.0.1:7543';
var _transportMsgLimit = 250000;
var _base64scale = 1 + 1/3.0;
var _maxPostSize = 65536*_base64scale + 32 + 32 + 'hashreplyTomessage{},,,"""""":::   \n\n\n'.length + 100;

function httpPost(uri, data) {
	console.log('on_add.js: httppost(\''+uri+'\', \''+data+'\')');
  var x = new XMLHttpRequest();
  x.open('POST', uri);
  x.send(data);
}

function recursivelySendParentsArray(post, arr) {
  if (arr == undefined) arr = [ post ];
  $.get('../api/get/' + post.replyTo)
    .done(function(data){
      data = JSON.parse(data);
      var str = JSON.stringify(arr);
      if (str.length >= _transportMsgLimit - _maxPostSize) {
        console.log('Try to send through Bitmessage Transport: ' + "random post");
        //console.log('sending to transport: ' + str);
        httpPost(transportUri, str);
        arr = [];
      }
      arr.push(data);
      recursivelySendParentsArray(data, arr);
    })
    .fail(function() {  // 404 - means we've reached the top
      var str = JSON.stringify(arr);
      console.log('Try to send through Bitmessage Transport: ' + "random post");
      //console.log('sending to transport: ' + str);
      httpPost(transportUri, str);
    });
}

/*
	This function is called each time user adds a post through web-interface.
	post - contains string with json of post object in it, where message is base64 encoded
*/
function onAdd(post) {
  recursivelySendParentsArray(post);
}