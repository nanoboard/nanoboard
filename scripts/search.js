function arr_diff (a1, a2) {

    var a = [], diff = [];

    for (var i = 0; i < a1.length; i++) {
        a[a1[i]] = true;
    }

    for (var i = 0; i < a2.length; i++) {
        if (a[a2[i]]) {
            delete a[a2[i]];
        } else {
            a[a2[i]] = true;
        }
    }

    for (var k in a) {
        diff.push(k);
    }

    return diff;
}

$(function() {
  $( "#search" ).submit(function( event ) {
          event.preventDefault();
          location.href="#search"
          if ($('.searchfield').val()!=""){
              $('#thread').empty();
              $('.searchfield').focus();
              $('#thread').append('<hr>');
              $('#thread').append('<div id="searchresult"></div>');
              $('#searchresult').empty();
              $('#searchresult').append('<img style="border: 0px" src="../images/spinner.gif">');
              var search = Base64.encode($('.searchfield').val());
              $.post('../api/search', search)
                .done(function(arr){
                  $('#searchresult').empty();
				  console.log('arr', arr);
                  arr = JSON.parse(arr);
                  if (arr.length == 0) {
                    $('#searchresult').append('No results<br/>');
                    return;
                  } else { 
                    $('#searchresult')
                      .append('Results: ' + arr.length + 
                              (arr.length >= 500 ? '(limit reached)' : '') + '<br/>');
                  }
                  for (var i = arr.length - 1; i >= 0; i--) {
                    var p = addPost(arr[i], function(d) {
                      d.appendTo($('#searchresult')); 
                    }, false);
                    if (arr[i].hash != _categories && 
                        arr[i].replyTo != _categories && 
                        arr[i].replyTo != _rootpost)
                    p.append(
                      $('<a>')
                        .attr('href', 'javascript:void(0)')
                        //.text('[Thread]')
                        .html('<span class="glyphicon glyphicon-menu-hamburger" aria-hidden="true"></span><span class="btn-title">&thinsp;Thread</span>')
                        .click(function(){
                          loadRootThread($(this).parent().attr('id'));
                        })
                      );
                  }
                  search = Base64.decode(search);
                  setTimeout(function(){
					$('.post-inner').each(function() {
//old code
//                      replaceAll($(this).html().toString(), search, '<span class="word-search">' + search + '</span>')

						//replace by another way...
						var html = $(this).html().toString();									//get html content
						var replacement = '<span class="word-search">' + search + '</span>';	//html for highlighting

						var doc = document.createElement("html");								//create html element
						doc.innerHTML = html;													//insert html content, there
						var links = doc.getElementsByTagName("a");								//get all links by tag name
						var urls = [];															//define empty array for links
						var replaced_urls = [];													//define second empty array for replaced links

						for (var i=0; i<links.length; i++) {									//for all links
						    urls.push(links[i].outerHTML);												//add html of this links - to array
							replaced_urls.push(replaceAll(links[i].outerHTML, search, replacement));	//add replaced HTML to second array
						}
						
						var arrays_diff = arr_diff(urls, replaced_urls);						//create array with differences
						urls = replaced_urls = []; 												//delete previous two arrays.

						var replaced_html = replaceAll(html, search, replacement);				//replace all for highlighting found text
						
						for(d=0; d<arrays_diff.length; d += 2){		//for all differences
						
							var doc = document.createElement("html");											//create html element
							doc.innerHTML = arrays_diff[d];														//insert original link html, there
							var links = doc.getElementsByTagName("a");											//get this link by tag name as element
							links[0].innerHTML = replaceAll(links[0].innerHTML, search, replacement);			//new link = replace only text there inside original link
							replaced_html = replaceAll(replaced_html, arrays_diff[d+1], links[0].outerHTML);	//replace invalid link to new outerHTML of new link.
						}

						//console.log('replaced_html', replaced_html);		//show replaced html
						$(this).html(replaced_html);						//append replaced html
						
						//test queries:
						//https://github.com/Karasiq/nanoboard			//OK
						//downloading-updates-16.png					//OK
						//ttps://github.com/Karasiq/nanoboard			//OK

						});
					}, 1000);
                  $('img').click(function(){
                    $(this).toggleClass('full');
                  });
                });
            }
            else {
                location.href="#"
            }   
})
})
