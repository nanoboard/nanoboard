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

function updateCategoriesBar() {
  $('#categories').empty();
  $.get('../api/replies/' + _categories)
    .done(function(replies){
	  replies = JSON.parse(replies);
	  for (var i = 0; i < replies.length; i++){
	    var reply = replies[i];
	    if (reply.message != 'cG9zdCB3YXMgZGVsZXRlZA==') // if category post was deleted - skip
	       $('#categories').append('<a href="#category'+reply.hash+'">['+stripTags(applyFormatting(Base64.decode(reply.message)))+']</a> ');
	  }
    });
}

function updatePlacesBar() {
  $.get('../api/paramget/places')
      .done(function(v){
        v = v.split('\n');
        $('#placesd').empty();
        $('#placesd').append('<b>Places (to post PNG containers to):</b><br/>')
        for (var i = 0; i < v.length; i++) {
          if (v[i].length > 0 && v[i][0]=='#') continue;
          $('#placesd').append('• <a target=_blank href="'+v[i]+'">'+v[i]+'</a><br/>')
        }
        $('#placesd').append('You can edit this list on <a href=params.html>[Settings]</a> page.');
      });
}

var postCount = 0;

function notifyAboutPostCount() {
  $.get('../api/count')
    .done(function(data){
      data = parseInt(data);
      if (data != postCount) {
        if (postCount != 0) {
          var countStr = (data - postCount).toString();
          pushNotification(countStr + ' post' + numSuffix(countStr) + ' added.', _post_count_notification_time);
        }
        postCount = data;
        $('#statusd').html('<a href=javascript:void(0)>Posts (including deleted): '+postCount+'</a>');
      }
    })
    .fail(function(){
      pushNotification('Connection to server lost.', 900);
    });
}

function notifyAboutNotifications() {
  $.get('../notif')
    .done(function(data){
      pushNotification(applyFormatting(data), _post_count_notification_time);
    })
    .fail(function(){
      // do nothing
    });
}

var _location = '';

$(function() {
  var collectionRun = false;
  var creationRun = false;

  $('#png-collect').click(function(){
    $.get('../api/png-collect')
      .done(function(){collectionRun = true;});
    $('#png-collect').hide();
    pushNotification('PNG collection started.');
  });
  $('#png-create').click(function(){
    pushNotification('PNG creation started.');
    $('#png-create').hide();
    $.get('../api/png-create')
      .done(function(){
        creationRun = true;
      });
  });

  setInterval(function() {
    if (creationRun)
    $.get('../api/png-create-avail')
      .done(function(){
        if (!creationRun) return;
        $('#png-create').show();
        //pushNotification('PNG creation finished (check your "upload" folder).');
        $('#png-create').show();
        creationRun = false;
      })
      .fail(function(){$('#png-create').hide();});
    if (collectionRun)
    $.get('../api/png-collect-avail')
      .done(function(){
        if (!collectionRun) return;
        $('#png-collect').show();
        pushNotification('PNG collection finished.');
        collectionRun = false;
      })
      .fail(function(){$('#png-collect').hide();});
  }, 300);

  reloadParams();

  setInterval(function() {
    var incl = ''.includes == undefined ? function(x,y) { return x.contains(y); } : function(x,y) { return x.includes(y); };
    var newLocation = window.location.href.toString();
    if (newLocation != _location) {
      _location = newLocation;
      if (_location.endsWith('#') || _location.endsWith('html')) {
        _depth = 0;
        loadThread(_categories);
      } else if (incl(_location, '#thread')) {
        _depth = 2;
        loadThread(_location.split('#thread')[1]);
      } else if (incl(_location, '#category')) {
        _depth = 1;
        loadThread(_location.split('#category')[1]);
      } else if (incl(_location, '#last')) {
        showLast(parseInt(_location.split('#last')[1]));
      } else {
        // do nothing intentionally
      }
    }
  }, 100);

  updateCategoriesBar();

  setInterval(function(){
    updatePlacesBar();
  }, 2000);

  setInterval(function(){
    retranslate();
  }, 300000);
  setInterval(function(){
    checkVersion();
  }, 60000);
  checkVersion();
  setInterval(function(){ notifyAboutPostCount(); }, 1000);
  setInterval(function(){ notifyAboutNotifications(); }, _post_count_notification_time/4);
});
