var _categories = 'bdd4b5fc1b3a933367bc6830fef72a35';
var _mainpost = 'f682830a470200d738d32c69e6c2b8a4';
var _rootpost = '00000000000000000000000000000000';
var _postWasDeletedMarker = 'post was deleted';
var _depth = 0;

var _showDeleted = 'true';
var _showTimestamps = 'true';
var _treeOffsetPx = 10;
var _detectURLs = 'true';
var _checkVersion = 'false';							//temporary "false"
var _instantRetranslation = 'false';					//May working with BitMessage. Default = true, but for me - false.
var _post_delete_timeout = 3000;
var _post_count_notification_time = 2400;				//was been 600 but 2400 for me
//var _post_count_notification_time = 16000;
//var _post_count_notification_time = 24000;
var _deletedOpacity = 0.33;
var _spam_filter = [];
var _use_spam_filter= 'true';

var currentUnixTime = Math.floor(new Date().getTime()/1000).toString(); //current_unix timestamp
var check_updates_every_hours = "72";		//hours			//using in checkver.js as long timeout, when no any updates
var remind_if_updates_exists = "10"			//minutes		//using in checkver.js as short timeout, when updates is available.
var last_update = 'current_time_or_time_of_last_update';
var last_another_repo_version = 'first_start';				//last another repo version or default value, if first start

var captcha_pack_file = "captcha.nbc";
var original_captcha_sha256 = "0732888283037E2B17FFF361EAB73BEC26F7D2505CDB98C83C80EC14B9680413";

var captcha_url = "https://github.com/Karasiq/nanoboard/releases/download/v1.2.0/ffeaeb19.nbc";

var updates_available = false;								//false is version.txt are equals, and no any updates, else - true;


function tryGetParam(param, def, cb) {
  $.get('../api/paramget/'+param)
    .done(function(v){cb(v);})
    .fail(function(){
      $.post('../api/paramset/'+param, def)
        .done(function(){cb(def);})
		.fail(
			function(){
				setTimeout(
					function(){
						tryGetParam(param, def, cb);
					},
					100
				);
			}
		);
    });
}

function parseRegExps(regstr) {
/*
	var regstr = 'word1\n\
word2\\n\n\
//word3\\n//(disabled)\n\
//disabled_word\n\
\\n\\n\n\
word4\n\
';	//	--->	["/word1/m", "/word2/m", "/word4/m"]
*/

//if serialized string in config-3.json
//var strarr=regstr.split("\\n").join("\n").split("\n");		//"one\ntwo\\n\nthree" -> ["one\ntwo", "\nthree"] -> one\ntwo\n\nthree -> [ "one", "two", "", "three" ]
//var strarr=regstr.split("\n");								//"one\ntwo\\n\nthree" -> regstr one\ntwo\\n\nthree -> strarr Array [ "one", "two\n", "three" ]

    //var strarr=regstr.split("\n");
	
	if(regstr==""){regstr = '[""]';}
		//	var strarr=JSON.parse(regstr).replace(/\\/g, '\\\\')).join('\n');
			var strarr=JSON.parse(regstr);						//if serialized JSON in config-3.jwon
		//	var strarr=JSON.parse(regstr.replace(/\\/g, '\\\\'));
	

/*
	console.log(
		'regstr', regstr
	, 	'\nstrarr', strarr
	,	JSON.parse(
			JSON.stringify(
				regstr.split('\n')
			)
		)
	);
*/	
    var regarr=[];
    for (i in strarr) {
        if (strarr[i] !="" &&
			(
				strarr[i].substring(0, 2)!=='//'					//if this is not comment starts from '//', like in JavaScript
			&&	strarr[i][0]!='#'									//if this is not comment, starts with '#'
			)
		){
            //console.log(new RegExp(strarr[i], 'm'));				//show generated regexp
            regarr.push(new RegExp(strarr[i], 'm'));
        }
    }
	//console.log(regarr);											//show array with regexps
    return regarr;
}
function reloadParams() {
  tryGetParam('show_deleted', 					_showDeleted, 								function(v){ _showDeleted = v; });
  tryGetParam('show_timestamps', 				_showTimestamps, 							function(v){ _showTimestamps = v; });
  tryGetParam('check_version_update', 			_checkVersion, 								function(v){ _checkVersion = v; });
  tryGetParam('instant_retranslation', 			_instantRetranslation, 						function(v){ _instantRetranslation = v; });
  tryGetParam('detect_URLs', 					_detectURLs, 								function(v){ _detectURLs = v; });
  tryGetParam('post_offset_in_tree_px', 		_treeOffsetPx.toString(), 					function(v){ _treeOffsetPx = parseInt(v); });
  tryGetParam('post_delete_timeout', 			_post_delete_timeout.toString(), 			function(v){ _post_delete_timeout = parseInt(v); });
  tryGetParam('post_count_notification_time', 	_post_count_notification_time.toString(), 	function(v){ _post_count_notification_time = parseInt(v); });
  tryGetParam('spam_filter', 					_spam_filter.join('\n'), 					function(v){ _spam_filter = parseRegExps(v); });
  tryGetParam('use_spam_filter', 				_use_spam_filter, 							function(v){ _use_spam_filter = v; });
  tryGetParam('check_updates_every_hours', 		check_updates_every_hours, 					function(v){ check_updates_every_hours = v; });
  tryGetParam('remind_if_updates_exists', 		remind_if_updates_exists, 					function(v){ remind_if_updates_exists = v; });  
  tryGetParam('last_update', 					currentUnixTime, 							function(v){ last_update = v; });
  tryGetParam('last_another_repo_version', 		last_another_repo_version,
	function(v){
		last_another_repo_version = v;
		if(last_another_repo_version=="first_start"){
			checkVersion(true);
		}
	}
  );
  tryGetParam('captcha_pack_file', 				captcha_pack_file, 						function(v){ captcha_pack_file = v; });
  tryGetParam('original_captcha_sha256', 		original_captcha_sha256, 				function(v){ original_captcha_sha256 = v; });
  tryGetParam('captcha_url', 					captcha_url, 							function(v){ captcha_url = v; });
}
