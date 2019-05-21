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

//now this value not hardcoded in the method Search in \Server\DbApiHandler.cs
var post_count_limit_for_searching = 500;

function change_show_max_search(value){
	console.log('change_show_max_search('+value+')');
	post_count_limit_for_searching = value;
}

$(function() {
  $( "#search" ).submit(function( event ) {
			
			hide_create_png();	//hide "Create PNG" tab if this was been active.
			
			start_index_last_posts_div.style.display = "none";		//hide this div if showed.
			show_max_search.style.display = "block";				//Show this div if hidden
			search_show_max.value = post_count_limit_for_searching;	//insert value.
			
          event.preventDefault();
          location.href="#search"
          if ($('.searchfield').val()!=""){
              $('#thread').empty();
              $('.searchfield').focus();
              $('#thread').append('<hr>');
              $('#thread').append('<div id="searchresult"></div>');
              $('#searchresult').empty();
              $('#searchresult').append('<img style="border: 0px" src="../images/spinner.gif">');
              
				if( ( ( /[A-Fa-f0-9]{32}/g ).test( $('.searchfield').val() ) ) ){	//if search query seems like post hash
					
					//search return post with this hash and replies...

					var search = ($('.searchfield').val()).trim();
					
					console.log(	search, ' - seems like hash'	);
					$.get('../api/get/'+search)			//return one post
					.done(
						function(onepost){
							$('#searchresult').empty();
							
							//console.log('onepost', onepost);
							
							var arr = [];
							arr[0] = JSON.parse(onepost);
							
							//console.log('arr', arr);

							$.get('../api/replies/'+search)	//return array with posts if exists.
							.done(
								function(replies){
									
									//console.log('replies', replies);
									
									replies = JSON.parse(replies);
									arr = replies.concat(arr);
									
									//console.log('arr', arr);
									
									if (arr.length == 0) {
										$('#searchresult').append('No results<br/>');
										return;
									} else { 
										$('#searchresult')
										.append('Results: ' + arr.length + 
												(arr.length >= 500 ? '(limit reached)' : '') + '<br/>');
									}
									for (var i = arr.length - 1; i >= 0; i--) {

										if( i === (arr.length - 1) ){
											$('#searchresult').append('<b>Post with hash: '+'<span class="word-search">' + search + '</span>'+'</b><br>');
										}else if(i === (arr.length - 2)){
											$('#searchresult').append('<b>Replies for post with hash: '+'<span class="word-search">' + search + '</span>'+'</b><br>');
										}

										var p = addPost(
											arr[i],
											function(d) {
												d.appendTo($('#searchresult')); 
											},
											false
										);
										if (
												arr[i].hash != _categories
											&&	arr[i].replyTo != _categories
											&&	arr[i].replyTo != _rootpost
										){
											p.append(
												$('<a>')
												.attr('href', 'javascript:void(0)')
												//.text('[Thread]')
												.html('<span class="glyphicon glyphicon-menu-hamburger" aria-hidden="true"></span><span class="btn-title">&thinsp;Thread</span>')
												.click(
													function(){
														loadRootThread($(this).parent().attr('id'));
													}
												)
											);
										
											p.append(
												$('<a>')
												.attr('href', '#thread' + arr[i].hash)
												//.text('[Thread]')
												.html(
													'&nbsp;&nbsp;&nbsp;<span class="glyphicon glyphicon-paperclip" aria-hidden="true"></span>'+
													'<span class="btn-title">&thinsp;POST</span>'
												)
												.click(
													function(){
														//loadRootThread($(this).parent().attr('id'));
													}
												)
											);
										}
										console.log('post links must to be appended...');
									}
									$('img').click(
										function(){
											$(this).toggleClass('full');
										}
									);
								}
							)
						}
					);

				}else{	//else if query not seems like post hash - search this inside posts.
					var search = Base64.encode($('.searchfield').val());
				

					//$.get('../api/search/'+search)
					$.post('../api/search', search+'|'+post_count_limit_for_searching)
						.done(
							function(arr){
								$('#searchresult').empty();
								//console.log('arr', arr);
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
									var p = addPost(
										arr[i],
										function(d) {
											d.appendTo($('#searchresult')); 
										},
										false
									);

									if (
											arr[i].hash != _categories
										&&	arr[i].replyTo != _categories
										&&	arr[i].replyTo != _rootpost
									){
										p.append(
											$('<a>')
											.attr('href', 'javascript:void(0)')
											//.text('[Thread]')
											.html('<span class="glyphicon glyphicon-menu-hamburger" aria-hidden="true"></span><span class="btn-title">&thinsp;Thread</span>')
											.click(
												function(){
													loadRootThread($(this).parent().attr('id'));
												}
											)
										);
										
										p.append(
											$('<a>')
											.attr('href', '#thread' + arr[i].hash)
											//.text('[Thread]')
											.html(
												'&nbsp;&nbsp;&nbsp;<span class="glyphicon glyphicon-paperclip" aria-hidden="true"></span>'+
												'<span class="btn-title">&thinsp;POST</span>'
											)
											.click(
												function(){
													//loadRootThread($(this).parent().attr('id'));
												}
											)
										);
									}
								}

								search = Base64.decode(search);
								setTimeout(
									function(){
										$('.post-inner').each(
											function() {
												//replace by another way...
												var html = $(this).html().toString();									//get html content

												//console.log('html', html);

												var replacement = '<span class="word-search">' + search + '</span>';	//html for highlighting

												var doc = document.createElement("html");								//create html element
												doc.innerHTML = html;													//insert html content, there
												var links = doc.getElementsByTagName("a");								//get all links by tag name
												var imgs = doc.getElementsByTagName("img");								//get images.
												var urls = [];															//define empty array for links
												var replaced_urls = [];													//define second empty array for replaced links

												for (var i=0; i<links.length; i++) {									//for all links
													urls.push(links[i].outerHTML);												//add html of this links - to array
													replaced_urls.push(replaceAll_search(links[i].outerHTML, search, replacement));	//add replaced HTML to second array
												}

												for (var i=0; i<imgs.length; i++) {										//for all imgs
													urls.push(imgs[i].outerHTML);										//add html of this imgs - to array
													replaced_urls.push(replaceAll_search(imgs[i].outerHTML, search, replacement));	//add replaced HTML to second array
												}
						
												var arrays_diff = arr_diff(urls, replaced_urls);						//create array with differences
												urls = replaced_urls = []; 												//delete previous two arrays.

												var replaced_html = replaceAll_search(html, search, replacement);				//replace all for highlighting found text

												for(d=0; d<arrays_diff.length/2; d++){		//for all differences							//GOOD
													var doc = document.createElement("html");											//create html element
													doc.innerHTML = arrays_diff[d];														//insert original link html, there
													var links = doc.getElementsByTagName("a");											//get this link by tag name as element
								
													links[0].innerHTML = replaceAll_search(links[0].innerHTML, search, replacement);	//new link = replace only text there inside original link
													replaced_html = replaceAll_search(replaced_html, arrays_diff[((arrays_diff.length/2)+d)], links[0].outerHTML);	//replace invalid link to new outerHTML of new link.
												}
												//console.log('replaced_html', replaced_html);		//show replaced html
												$(this).html(replaced_html);						//append replaced html

//test queries:
//https://github.com/Karasiq/nanoboard			//OK
//downloading-updates-16.png					//OK
//ttps://github.com/Karasiq/nanoboard			//OK
//HTtP											//OK!
//base64_fragment_in_picture					//Now ok, if "base64_fragment_in_picture" - is inside base64 of picture dataURL...
											}
										);
									},
									1000
								);
								$('img').click(
									function(){
										$(this).toggleClass('full');
									}
								);
							}
						);
				}
            }
            else {
                location.href="#"
            }   
})
})
