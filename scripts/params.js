var _categories = 'bdd4b5fc1b3a933367bc6830fef72a35';
var _mainpost = 'f682830a470200d738d32c69e6c2b8a4';
var _rootpost = '00000000000000000000000000000000';
var _postWasDeletedMarker = 'post was deleted';
var _depth = 0;

var _showDeleted = 'true';
var _showTimestamps = 'true';
var _treeOffsetPx = 10;
var _detectURLs = 'true';
var _checkVersion = 'true';
var _instantRetranslation = 'true';
var _post_delete_timeout = 3000;
var _post_count_notification_time = 600;
var _deletedOpacity = 0.33;
var _spam_filter = [];
var _use_spam_filter= 'true';
function tryGetParam(param, def, cb) {
  $.get('../api/paramget/'+param)
    .done(function(v){cb(v);})
    .fail(function(){
      $.post('../api/paramset/'+param, def)
        .done(function(){cb(def);});
    });
}
function parseRegExps(regstr) {
    var strarr=regstr.split("\n")
    var regarr=[]
    for (i in strarr) {
        if (strarr[i] !=""){
            regarr.push(new RegExp(strarr[i], 'm'));
        }
    }
    return regarr
}
function reloadParams() {
  tryGetParam('show_deleted', 'true', function(v){ _showDeleted = v; });
  tryGetParam('show_timestamps', 'true', function(v){ _showTimestamps = v; });
  tryGetParam('check_version_update', 'true', function(v){ _checkVersion = v; });
  tryGetParam('instant_retranslation', 'true', function(v){ _instantRetranslation = v; });
  tryGetParam('detect_URLs', 'true', function(v){ _detectURLs = v; });
  tryGetParam('post_offset_in_tree_px', '10', function(v){ _treeOffsetPx = parseInt(v); });
  tryGetParam('post_delete_timeout', '3000', function(v){ _post_delete_timeout = parseInt(v); });
  tryGetParam('post_count_notification_time', '600', function(v){ _post_count_notification_time = parseInt(v); });
  tryGetParam('spam_filter', '', function(v){ _spam_filter = parseRegExps(v); });
  tryGetParam('use_spam_filter', 'true', function(v){ _use_spam_filter = v; });
}
