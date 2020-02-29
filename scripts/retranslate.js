/*
	retranslate.js

	when someone has active tab, he will retranslate random posts from his db,
	latest posts will retranslate more often
*/

function retranslate() {
	console.log('retranslate.js: retranslate();', '_instantRetranslation', _instantRetranslation);
  if (_instantRetranslation != 'true') return;
	$.get('../api/count')
		.done(function(cnt){
			cnt = parseInt(cnt);
      var rand = Math.floor(Math.pow(Math.random(), 0.25) * (cnt+1));
      if (rand >= cnt) rand = cnt - 1;
			$.get('../api/nget/' + rand)
				.done(function(post){
					httpPost(transportUri, post);
				});
		});
}

function test_retranslate(){
	var temp_instantRetranslation = _instantRetranslation;
	var temp_transportUri = transportUri;
	
	_instantRetranslation = 'true';
	transportUri = window.location.origin+'/api/upload-post';
	
	retranslate();
	setTimeout(
		function(){
			_instantRetranslation = temp_instantRetranslation;
			transportUri = temp_transportUri;
		}
		,5000
	);
}