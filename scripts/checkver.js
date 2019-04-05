/*
	Checks if version in repository differs from current
*/

//var _repoVersion = 'https://raw.githubusercontent.com/nanoboard/nanoboard/feature/2.0/bin/Debug/pages/version.txt';		//old not working link
var _repoVersion = 'https://raw.githubusercontent.com/username1565/nanoboard/master/pages/version.txt';						//new working link
var _buildVersion = '../pages/version.txt';																					//localhost "pages/version.txt"

var wait_hours = parseInt(check_updates_every_hours)*60*60;	//long interval from params.js (in hours) to repeat ONLINE check updates,
															//if this updates was not been available in previous check.

function check_version_when_no_updates(){
	var now_unix_time = Math.floor(new Date().getTime()/1000);
	var last_update_unix_time = parseInt(last_update);
	if(isNaN(last_update_unix_time)){														//if last_update not loaded, this is NaN
//		console.log("NaN. Try again...");
		setTimeout(function(){check_version_when_no_updates();}, 100); return;				//repeat this function often before loading, return, and continue then, after loading...
	}
	var repeat_if_success = parseInt(remind_if_updates_exists)*60*1000;					//Short timeout in minutes, for check updates, and notify about it,
																							//if this updates was been available in previous checking.
	if( now_unix_time - last_update_unix_time >= wait_hours){								//if differences between current timestamp and previous update
																							//is more than interval to check update
		checkVersion(true);																		//check update online.
	}else if(updates_available == true){													//if updates was been available after previous checking
		checkVersion();																			//check update again and show notify more often		
	}
//	console.log("repeat_if_success", repeat_if_success, "wait_hours", wait_hours);
	setTimeout(function(){check_version_when_no_updates();}, repeat_if_success);			//and repeat this function, after short timeout.
}

function available_updates(){
	//if version.txt not equals - updates available.

	//pushNotification('Nanoboard client update is available: <a href=https://raw.githubusercontent.com/nanoboard/nanoboard/feature/2.0/release2.zip>[Download]</a>', 30000);
	pushNotification(
		'Nanoboard client update is available: '+
		//'<a href=https://github.com/username1565/nanoboard/releases/download/win32/nanodb.exe+noIPlogger+pathways_fixed.zip>[Download]</a>'
		'<a href="https://github.com/username1565/nanoboard/releases/">[Download]</a>'
		, 60000	//show this within 1 minute
	);
	//and repeat checking using short timeout "repeat_if_success", in minutes.
	updates_available = true;
	return;
}

function no_need_update(){
	//if both version.txt are equals - no any updates
	pushNotification(
						'<a href="../pages/version.txt">version.txt</a> '+
						'is equals with '+
						'<a href="https://github.com/username1565/nanoboard">username1565\'s</a> '+
						'<a href="https://raw.githubusercontent.com/username1565/nanoboard/master/pages/version.txt">version.txt</a>. '
						, 30000	//show this within 30 seconds.
	);
	updates_available = false;
	currentUnixTime = Math.floor(new Date().getTime()/1000).toString();		//set current unix timestamp to this variable, defined in params.js
	last_update = currentUnixTime;											//set this in this variable
	$.post('../api/paramset/'+'last_update', last_update);					//and in the field "last_update" in config-3.json
	//After this do not check updates,
	//within "check_updates_every_hours" hours.
//	console.log("version.txt === that version.txt. No need updates.");
}


function checkVersion(online) {												//online switch to true after long interval
  if (_checkVersion != 'true') return;																			//if no need to check version
  $.get(_buildVersion)																							//get version.txt from localhost
    .done(function(bv) {
      $.get('../api/paramget/'+'last_another_repo_version')														//load last another repo version from config-3.json
		//This need to don't download it so often, if this was been different.
        .done(function(larv) {																					//larv - last another repoVersion
			
			larv = decodeURIComponent(larv);																	//decodeURIcomponent
			
			if(		online==true									//if need to check update online
				|| larv == "first_start"								//or if this is value after first_start.
				//|| 	bv == larv										//or if local version are equals with saved version.
				//&& 	bv == larv										//and if local version are equals with saved version.
			){
				$.get(_repoVersion)																				//loading repositary version - online
	              .done(function(rv_remote) {																	//rv_remote - repoVersion in remote repositary
					if(rv_remote!=larv){																		//if this versions not equals
						setTimeout(function(){
							$.post('../api/paramset/'+'last_another_repo_version', encodeURIComponent(rv_remote));	//update encoded loaded verion in config-3.json
						},300);
					}
					if (bv != rv_remote){ available_updates(); }			//if this loaded version not equals with local version - show notification and change timeout				
					else{ no_need_update(); }								//else, if both versions (loaded and saved) are equals with local version - no need to update
				  });
			}else if(bv == larv){											//local version just not equals with last saved version
				no_need_update();												//Just repeat this, using short timeout
			}
			else{															//if local version not equals with saved version
				available_updates();											//updates are available
			}
			return;		  
        })
		.fail(function(){
			$.post('../api/paramset/'+'last_another_repo_version', "first_start");	//for first start - write this.
			setTimeout(function(){checkVersion(true);},2000);						//and check online
		});
		return;
    });
	return;
}

check_version_when_no_updates();