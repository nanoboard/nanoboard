function numSuffix(numStr) {
  if (numStr.endsWith('11')) return 's';
  if (numStr.endsWith('1')) return '';
  return 's';
}

function addPost(post, appendFunc, hasShowButton, short) {
  if (_use_spam_filter=='true' && post.hash != _categories){
    for (i in _spam_filter){
        if (_spam_filter[i].test(escapeTags(Base64.decode(post.message)))) return false;
    }
  }
  var locationBackup = window.location.href.toString();
  if (_depth > 1) hasShowButton = false;
  if (short == undefined) short = true;
  var d = $(document.createElement('div'));
  d
    .addClass('post')
    .attr('id', post.hash);
  if (_depth != 0)
    d.append('<gr>#' + (short&&_depth!=1?shortenHash(post.hash):post.hash) + '&nbsp;</gr>');
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
        if (size == '0')
          showLink.html('<span class="glyphicon glyphicon-comment not-avail" aria-hidden="true"></span><span class="btn-title not-avail">&thinsp;0</span>');
        else
          showLink.html('<span class="glyphicon glyphicon-comment" aria-hidden="true"></span><span class="btn-title">&thinsp;'+size+' – Show</span>');
      });
  }
  d.append('&nbsp;');
  d
    .append($('<a>')
      .attr('href', 'javascript:void(0)')
      .html('<span class="glyphicon glyphicon-trash" aria-hidden="true"></span><span class="btn-title">Delete</span>')
      .attr('title', 'Click to delete post forever.')
      .click(function() {
        if (post.hash == _categories) {
          pushNotification("Cannot delete root post.");
          return;
        }
        var undo = false;
        d.append(
          $('<button>')
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
          if (undo) return;
          deletePostFromDb(post.hash);
          d.remove();
          pushNotification('A post was deleted forever.');
        }, _post_delete_timeout);
      }));
  appendFunc(d);
  var inner = $('<div>')
    .addClass('post-inner')
    .html(applyFormatting(escapeTags(Base64.decode(post.message))))
    .appendTo(d);
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
      if (img.src.startsWith('data:image/jpeg;base64,UEsDB')) {
        $(img).replaceWith($('<a download=file'+(i+1)+'.zip href='+img.src.replace('image/jpeg','application/zip')+'>[file'+(i+1)+'.zip]</a>'));
      }
    }
  }
  if (_showTimestamps == 'false') {
	if (d.find('g').length != 0)
		d.find('br').first().remove();
    d.find('g').css('display','none');
  }
  return d;
}

function loadReplies(hash, offset, highlight) {
  $.get('../api/replies/' + hash)
    .done(function(arr){
      arr = JSON.parse(arr);
      if (arr.length == 0) return;
      for (var i = arr.length-1; i >= 0; i--) {
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
        }
      }
      vid_show()
    });
}

function loadThread(hash, highlight) {
  thisPosts = [];
  $.get('../api/replies/' + hash)
    .done(function(arr){
      arr = JSON.parse(arr);
      if (arr.length > 0) {
        $('#thread').empty();
      } else { 
        _depth -= 1; 
        pushNotification('This thread/category is empty.');
        return; 
      }
      $.get('../api/get/' + hash)
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
                reloadParams();
                setTimeout(function(){
                  loadThread(hash);                  
                }, 500);
              }));
          $('#thread').append(
            $('<a>')
              .attr('href','javascript:void(0)')
              .html('&nbsp;<span class="glyphicon glyphicon-chevron-down" aria-hidden="true"></span>Sort by date')
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
                  $(posts[i]).prepend("<span style=font-size:75% class=separat>#"+(i+1)+"</span>&nbsp;");
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
              .html('&nbsp;<span class="glyphicon glyphicon-chevron-up" aria-hidden="true"></span>Reversed sort')
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
              .html('&nbsp;<span class="glyphicon glyphicon-remove" aria-hidden="true"></span>Delete All')
              .click(function(){
                if (confirm('Are you sure you want delete all posts that you currently see?')) {
                  $('.glyphicon-trash').click();
                }
              })
            );

          addPost(post, function(d){ d.appendTo($('#thread')); }, false, false);
          if (_depth == 1) arr.reverse();
          for (var i = 0; i < arr.length; i++) {
            var deleted = Base64.decode(arr[i].message) == _postWasDeletedMarker;
            if (_showDeleted == 'false' && deleted) continue;
            var p = addPost(arr[i], function(d) {d.appendTo($('#thread'));}, true)
            if (p){
                p.css('margin-left',  _treeOffsetPx + 'px');
                if (deleted) p.css({ opacity: _deletedOpacity});
                if (highlight == arr[i].hash) {
                  p.addTemporaryClass('high', 8000);
                }
                if (_depth > 1) {
                  loadReplies(arr[i].hash, 2, highlight);
                }
            }
          }
          vid_show()
        });
    });
}