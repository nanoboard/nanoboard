function shortenHash(hash) {
  return hash.substring(0,4) + '..' + hash.substring(28,32);
}

function replaceAll(text, search, replace) {
  return text.split(search).join(replace);
}

function escapeTags(text) {
  return text
    .replace(/>/gim, '&gt;')
    .replace(/</gim, '&lt;');
}

function detectImages(text) {
  var prefix = 'data:image/jpeg;base64,';
  var matches = text.match(/\[(i|x)mg=[A-Za-z0-9+\/=]{4,64512}\]/g);
  if (matches != null) {
    for (var i = 0; i < matches.length; i++) {
      var value = matches[i].toString();
      value = value.substring(5);
      value = value.substring(0, value.length - 1);
      value = '<img src="' + prefix + value + '" />';
      text = replaceAll(text, matches[i], value);
    }
  }
  return text;
}

function addPlace(place,uuid) {
  place = Base64.decode(place);
  console.log('add:' + place);
  $.get('../api/paramget/places')
    .done(function(arr){
      arr = arr.split('\n');
      var wasAdded = arr.indexOf(place) != -1;
      if (wasAdded) {
        $(document.getElementById(uuid)).text('added');
        pushNotification('Was added already.');
        return;
      }
      arr.push(place);
      $.post('../api/paramset/places', arr.join('\n'))
        .done(function(){
          $(document.getElementById(uuid)).text('added');
          pushNotification('Added: ' + place);
        });
    });
}

function delPlace(place,uuid) {
  place = Base64.decode(place);
  console.log('del:' + place);
  $.get('../api/paramget/places')
    .done(function(arr){
      arr = arr.split('\n');
      var wasAdded = arr.indexOf(place) != -1;
      if (!wasAdded) {
        $(document.getElementById(uuid)).text('');
        pushNotification('Not present or already deleted.');
        return;
      }
      var arr2 = [];
      for (var i = 0; i < arr.length; i++) {
        if (arr[i] != place) {
          arr2.push(arr[i]);
        }
      }
      arr = arr2;
      $.post('../api/paramset/places', arr.join('\n'))
        .done(function(){
          $(document.getElementById(uuid)).text('');
          pushNotification('Deleted: ' + place);
        });
    });
}

function generateUUID(){
    var d = new Date().getTime();
    if(window.performance && typeof window.performance.now === "function"){
        d += performance.now(); //use high-precision timer if available
    }
    var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = (d + Math.random()*16)%16 | 0;
        d = Math.floor(d/16);
        return (c=='x' ? r : (r&0x3|0x8)).toString(16);
    });
    return uuid;
}

function detectPlacesCommands(obj) {
  var text = obj.text();
  var html = obj.html();
  var incl = ''.includes == undefined ? function(x,y) { return x.contains(y); } : function(x,y) { return x.includes(y); };
  var matches = text.match(/(ADD|DEL(ETE|))[\s]*https?:\/\/[a-z%&\?\-=_\.0-9\/:#]+/g);
  if (matches == null) return;
  $.get('../api/paramget/places')
    .done(function(arr){
      arr = arr.split('\n');
      for (var i = 0; i < matches.length; i++) {
        var value = matches[i].toString();
        console.log(value);
        value = value.substring(value.indexOf('http'));
        var wasAdded = arr.indexOf(value) != -1;
        var uuid = generateUUID();
        html = replaceAll(html, value+'</a>', value +
          '</a>&nbsp;<a href=javascript:addPlace("'+Base64.encode(value)+'","'+uuid+'")><sup>[+]</sup></a>'+
          '<i><sup id="'+uuid+'">' + (wasAdded ? 'added' : '') + '</sup></i>' +
          '<a href=javascript:delPlace("'+Base64.encode(value)+'","'+uuid+'")><sup>[-]</sup></a>');
      }
      obj.html(html);
    });
}

function detectURLs(text) {
  var matches = text.match(/https?:\/\/[A-Za-z%&\?\-=_\.0-9\/:#]+/g);
  var you_re=new RegExp(".*youtube\.com.*")
  if (matches != null) {
    for (var i = 0; i < matches.length; i++) {
      var value = matches[i].toString();
      if (you_re.test(value))
      {
        value ='<a class="vd-vid" href="'+value+'"><span class="glyphicon glyphicon-play" aria-hidden="true"></span>'+value+'</a>'
        text = replaceAll(text, matches[i], value);

      }
      else
      {
        value = '<a target=_blank href="'+value+'">'+value+'</a>';
        text = replaceAll(text, matches[i], value);
      }
    }
  }
  return text;
}

function detectThreadLinks(text) {
var matches = text.match(/&gt;&gt;[a-f0-9]{32}/g);
  if (matches != null) {
    for (var i = 0; i < matches.length; i++) {
      var value = matches[i].toString();
      value = value.substring(8, value.length);
      value = '<a href="javascript:void(0);" onclick=_depth=2;loadThread("'+value+'")>&gt;&gt;' + value + '</a>';
      text = replaceAll(text, matches[i], value);
    }
  }
  return text;
}

function applyFormatting(text) {
  text = text.replace(/&gt;(.*)/gi, "<gr>&gt;$1</gr>")
  text = text.replace(/\[sign=[a-f0-9]{128}\]/gim, '');
  text = text.replace(/\[pow=[a-f0-9]{256}\]/gim, '');
  text = text.replace(/\[sp(oiler|)\]/gim, '[x]');
  text = text.replace(/\[\/sp(oiler|)\]/gim, '[/x]');
  var tags = 'biusxg';
  for (var x = 0; x < tags.length; x++) {
    var ch = tags.charAt(x);
    text = text
      .replace(new RegExp("\\[" + ch + "\\]", 'gim'), '<' + ch + '>')
      .replace(new RegExp("\\[/" + ch + "\\]", 'gim'), '</' + ch + '>');
  }
  text = detectImages(text);
  text = detectThreadLinks(text);
  text = replaceAll(text, '\n', '<br/>');
  text = replaceAll(text, '  ', '&nbsp; ');
  if (_detectURLs == 'true') text = detectURLs(text);
  return text
    .replace(/<x>/gim, '<sp>')
    .replace(/<\/x>/gim, '</sp>');
}

(function($) {
  $.fn.extend({
    addTemporaryClass: function(className, duration) {
      var elements = this;
      setTimeout(function() {
        elements.removeClass(className);
      }, duration);
      return this.each(function() {
        $(this).addClass(className);
      });
    }
  });
})(jQuery);
