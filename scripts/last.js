
function loadRootThread(hash) {
  var first = hash;
  var prev = hash;
  var fn = function(){
    $.get('../api/get/' + hash)
      .done(function(p){
        p = JSON.parse(p);
        if (p.replyTo == _categories)
        {
          _depth = 2;
          loadThread(prev, first);
          return;
        }
        prev = p.hash;
        hash = p.replyTo;
        fn();
      });
  };
  fn();
}

function loadRootThreadHash(hash,pp,cb) {
  $.post('../api/find-thread/' + hash, _categories)
    .done(function(res){
      cb(res,pp);
    })
    .fail(function(){
      cb(null,pp);
    });
/*
  var first = hash;
  var prev = hash;
  var fn = function(){
    $.get('../api/get/' + hash)
      .done(function(p){
        p = JSON.parse(p);
        if (p.replyTo == _categories)
        {
          cb(prev,pp);
          return;
        }
        prev = p.hash;
        hash = p.replyTo;
        fn();
      });
  };
  fn();
*/
}

function showLast(N, from_index){
	
	//console.log('from_index', from_index, 'N', N);
	
	var from_index = (typeof from_findex === 'undefined') ? false : from_findex;
	
	//console.log('from_index', from_index, 'N', N);
    
    $.get('../api/pcount')
      .done(function(cnt){
        if(from_index===false){
			cnt = parseInt(cnt);
			total_posts.innerHTML = cnt;

			var si_div = start_index_last_posts_div;	//get hidden div element
			si_div.style.display = "block";				//show div with input element
			var lsi = lastli_start_index;				//get input element with for start_index

			from_index =
			lsi.value = 
						( (cnt-N) < lsi.value )
							? Math.max(cnt-N,0)			//set value of start index
							: lsi.value					//or previous value
			;

			lsi.min = 0;								//only positive numbers.
			lsi.max = cnt;								//up to post count
			showN.max = cnt;							//no more than post count...
			showN.value = (N>cnt) ? cnt : N;			//Set value, if selected 500 or 200 posts, when total post count lesser this values.
			
		}else{
			from_index = Math.max(from_index,0);								//if from_index is negative number this will be 0
			//showN.value = (N>(cnt-from_index)) ? (cnt-from_index) : N;		//Set value			
			showN.value = (N>(cnt-from_index)) ? (cnt-from_index) : from_index;	//Set value			
		}
		
/*
		console.log(
					"function showLast: "
		,	'\n',	"from_index", from_index
		//,	'\n',	"si_div.style.display", si_div.style.display
		//,	'\n',	"lsi", lsi
		//,	'\n',	"lsi.value", lsi.value
		);
*/

		if( ( from_index === '' ) || ( [ 10, 50, 100, 200, 500 ].indexOf( N )!==-1 ) ){
			from_index = lsi.value = ( cnt - N );
		}

		//console.log('from_index', from_index, 'N', N, 'cnt', cnt);
    
        $.get('../api/prange/'+from_index+'-'+N)					//start_index = cnt-N || 0 - not negative.
          .done(function(arr){
            active_tab("lastli")
            arr = JSON.parse(arr);
/*
//3.1
			if(document.getElementById('createPNG').style.display === 'block'){
				var threadId = 'thread';
			}else{
				var threadId = 'queue_thread';
			}
*/			
            if (arr.length > 0) {
//3.0
              $('#thread').empty();
//3.1
//              $('#'+threadId).empty();
            } else { return; }
            for (var i = arr.length - 1; i >= 0; i--) {
//3.0
              var p = addPost(arr[i], function(d) { d.appendTo($('#thread')); }, false);
//3.1
//              var p = addPost(arr[i], function(d) { d.appendTo($('#'+threadId)); }, false);
              if (arr[i].hash != _categories && 
                  arr[i].replyTo != _categories && 
                  arr[i].replyTo != _rootpost &&
                  p
                  ) {
                loadRootThreadHash(p.attr('id'), p,
                  function(h,pp) {
                  pp.append(
                    $('<a>')
//3.0
                      .attr('href', '#thread' + h)
//3.1
//                      .attr('href', '#'+threadId + h)
                      .html('<span class="glyphicon glyphicon-menu-hamburger" aria-hidden="true"></span><span class="btn-title">&thinsp;'+(h == null ? 'Thread Not Found' : 'Thread')+'</span>')
                      .click(function(){
                        //loadRootThread($(this).parent().attr('id'));
                      })
                    );
                  });
              }
            }
            vid_show()
          });
      });
  }
