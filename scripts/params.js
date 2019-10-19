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

var collect_memory_limit_to_wait = 200;						//Megabytes

var places = '';
var skin = 'futaba';
var Download_Timeout_Sec = 30;	//seconds. This is timeout to stop downloading image while collectPNG runned, if tht file of this image is not available.

//try get value of param or set it, if this value is not available.
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
  tryGetParam('Download_Timeout_Sec', 			Download_Timeout_Sec.toString(), 			function(v){ Download_Timeout_Sec = v; });
  tryGetParam('last_another_repo_version', 		last_another_repo_version,
	function(v){
		last_another_repo_version = v;
		if(last_another_repo_version=="first_start"){
			checkVersion(true);
		}
	}
  );
  tryGetParam('captcha_pack_file', 				captcha_pack_file, 							function(v){ captcha_pack_file = v; });
  tryGetParam('original_captcha_sha256', 		original_captcha_sha256, 					function(v){ original_captcha_sha256 = v; });
  tryGetParam('captcha_url', 					captcha_url, 								function(v){ captcha_url = v; });
  tryGetParam(
	'collect_memory_limit_to_wait',
	(collect_memory_limit_to_wait * 1024 * 1024).toString(),
	function(v){ collect_memory_limit_to_wait = v; }
  );

//Try to set default params, if this is empty, for example, when config.json was been deleted.

//set default places:
	tryGetParam('places',							places,
		function(v){
			if(v == "" || v == "# put urls to threads here, each at new line:\n"){
				var default_places =
					"# put URLs here, one per line\n";
				
				//array with places:
				var Defult_places_links_array = [
					//to download from here, put containers in /download folder or enable save_files in the query of collectPNG to save files there.
					"http://127.0.0.1:7346/download/",
					"http://dobrochan.com/mad/res/75979.xhtml",	//each
					"http://sibirchan.ru/b/res/10656.html",		//item
					"http://volgach.ru/b/res/5226.html",		//separated 
					"http://02ch.su/b/res/7379.html",			//with comma
					"http://chaos.cyberpunk.us/st/50\n"+		//or strings with "\n"
					"http://endchan.xyz/test/res/971.html\n"+	//many strings
"http://xynta.ch/b/res/10540.html\n"+							//tabs does not matter here
"http://www.nowere.net/wa/res/6271.html\n\
http://alphachan.org/art/res/329789.html\n\
http://hamstakilla.com/b/22279"
//or multistring with "\n\ in the end of each line", without tabs or spaces in the beginning of each line.
				];
				
				//generate JSON-string
				for(i=0; i<Defult_places_links_array.length; i++){
					default_places += Defult_places_links_array[i]+'\n';
				}

				$.post('../api/paramset/places', default_places)	//set as default value of the "places" parameter.
				.done(function(){places = default_places;})			//See config.json, after loading index.html
				.fail(
					function(){
						console.log("params.js: Fail to set default_places in config."); //show error in console.log
						setTimeout(
							function(){
								location.reload();				//and reload the page with futaba-skin.
							},
							500									//after 500 milliseconds.
						);
					}
				);
			}
			else{
				places = v;
			}
		}
	);
  
//set default skin:
	tryGetParam('skin',							skin,
		function(v){
			if(v == ""){
				var default_skin = "futaba";
				$.post('../api/paramset/skin', default_skin)
				.done(function(){
					skin = default_skin;
					setTimeout(
						function(){
							location.reload();				//and reload the page with futaba-skin.
						},
						500									//after 500 milliseconds.
					);
				})
				.fail(
					function(){
						console.log("params.js: Fail to set default_skin in config.");
						setTimeout(
							function(){
								location.reload();				//and reload the page with futaba-skin.
							},
							500									//after 500 milliseconds.
						);
					}
				);
			}
			else{
				skin = v;
			}
		}
	);
  
}
