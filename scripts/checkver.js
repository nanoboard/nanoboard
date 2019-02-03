/*
	Checks if version in repository differs from current
*/

var _repoVersion = 'https://raw.githubusercontent.com/nanoboard/nanoboard/feature/2.0/bin/Debug/pages/version.txt';
var _buildVersion = '../pages/version.txt';

function checkVersion() {
  if (_checkVersion != 'true') return;
  $.get(_repoVersion)
    .done(function(rv) {
      $.get(_buildVersion)
        .done(function(bv) {
          if (bv != rv) {
            pushNotification('Nanoboard client update is available: <a href=https://raw.githubusercontent.com/nanoboard/nanoboard/feature/2.0/release2.zip>[Download]</a>', 30000);
          }
        });
    });
}