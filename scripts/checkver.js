/*
	Checks if version in repository differs from current
*/

//var _repoVersion = 'https://raw.githubusercontent.com/nanoboard/nanoboard/feature/2.0/bin/Debug/pages/version.txt';	//old not working link
var _repoVersion = 'https://raw.githubusercontent.com/username1565/nanoboard/master/pages/version.txt';					//new working link
var _buildVersion = '../pages/version.txt';

function checkVersion() {
  if (_checkVersion != 'true') return;
  $.get(_repoVersion)
    .done(function(rv) {
      $.get(_buildVersion)
        .done(function(bv) {
          if (bv != rv) {
            //pushNotification('Nanoboard client update is available: <a href=https://raw.githubusercontent.com/nanoboard/nanoboard/feature/2.0/release2.zip>[Download]</a>', 30000);
            pushNotification('Nanoboard client update is available: <a href=https://github.com/username1565/nanoboard/releases/download/win32/nanodb.exe+noIPlogger+pathways_fixed.zip>[Download]</a>', 30000);
          }
        });
    });
}