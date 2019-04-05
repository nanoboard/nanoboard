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

var post_count_limit_for_searching = 499;
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
              //$.get('../api/search/'+search)
              $.post('../api/search', search+'|'+post_count_limit_for_searching)
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
							
//							replaced_urls.push(replaceAll(links[i].outerHTML, search, replacement));	//add replaced HTML to second array
							replaced_urls.push(replaceAll_search(links[i].outerHTML, search, replacement));	//add replaced HTML to second array

							console.log('replace links: for i', i, 'links.length', links.length);							
						}
						
						var arrays_diff = arr_diff(urls, replaced_urls);						//create array with differences
						console.log(
							'urls', urls, 'urls.length', urls.length,
							'\nreplaced_urls', replaced_urls, 'replaced_urls.length', replaced_urls.length,
							'\narrays_diff', arrays_diff, 'arrays_diff.length', arrays_diff.length
						);
						urls = replaced_urls = []; 												//delete previous two arrays.

//						var replaced_html = replaceAll(html, search, replacement);				//replace all for highlighting found text
						var replaced_html = replaceAll_search(html, search, replacement);				//replace all for highlighting found text

//						for(d=0; d<arrays_diff.length; d += 2){		//for all differences
						for(d=0; d<arrays_diff.length/2; d++){		//for all differences							//GOOD
//						for(d=0; d<=arrays_diff.length/2; d++){		//for all differences
//						for(d=0; d<arrays_diff.length-1; d += 1){		//for all differences
						
							console.log('arrays_diff: for d', d, 'arrays_diff.length', arrays_diff.length);
							console.log('arrays_diff', arrays_diff);

							var doc = document.createElement("html");											//create html element
							doc.innerHTML = arrays_diff[d];														//insert original link html, there
//							doc.innerHTML = arrays_diff[((arrays_diff.length/2)+d)];														//insert original link html, there
							var links = doc.getElementsByTagName("a");											//get this link by tag name as element
							
//							links[0].innerHTML = replaceAll(links[0].innerHTML, search, replacement);			//new link = replace only text there inside original link
//WAS WORKING				links[0].innerHTML = replaceAll_search(links[0].innerHTML, search, replacement);			//new link = replace only text there inside original link
							
							console.log(
								'links', links,
								'\nlink..... before......: links[0].outerHTML = ', links[0].outerHTML,
								', \nreplace search = ', search, ' to ',' \nreplacement = ', replacement,
								'\nin text = links[0].innerHTML = ', links[0].innerHTML
							);
							
//							links[0].innerHTML = replaceAll_search(links[0].innerHTML, search, replacement);				//new link = replace only text there inside original link
							links[0].innerHTML = replaceAll_search(links[0].innerHTML, search, replacement);	//new link = replace only text there inside original link
							console.log(
								'links', links,
								'\nlink..... after......: links[0].outerHTML = ', links[0].outerHTML,
								', \nreplaced search = ', search, ' to ',' \nreplacement = ', replacement,
								'\nin result text = links[0].innerHTML = ', links[0].innerHTML
							);
							
//							replaced_html = replaceAll(replaced_html, arrays_diff[d+1], links[0].outerHTML);	//replace invalid link to new outerHTML of new link.							
//							replaced_html = replaceAll_search(replaced_html, arrays_diff[d+1], links[0].outerHTML);	//replace invalid link to new outerHTML of new link.
							replaced_html = replaceAll_search(replaced_html, arrays_diff[((arrays_diff.length/2)+d)], links[0].outerHTML);	//replace invalid link to new outerHTML of new link.
						}
						
						console.log('replaced_html', replaced_html);		//show replaced html
						$(this).html(replaced_html);						//append replaced html
						
						//test queries:
						//https://github.com/Karasiq/nanoboard			//OK
						//downloading-updates-16.png					//OK
						//ttps://github.com/Karasiq/nanoboard			//OK
						//HTtP											//OK!
						
						/*
Why no highlighting?
http not HTtP, and split by HTtP not working.
	Ok... Another function replaceAll_search in post.js must split this good...
		
		
						*/
						
						
						
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
