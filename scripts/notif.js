var _notifInited = false;

function notifInit() {
	if (_notifInited) return;
  _notifInited = true;
  $('<div>').addClass('notif_area').appendTo($('body'));
}

function showNotification(text, x, y, t) {
  if (text == '') return;
	if (t == undefined) t = 1000;
	$('<div>')
  	.addClass('notif')
  	.appendTo($('body'))
    .html(text)
    .css('position', 'absolute')
    .css('left', x)
    .css('top', y)
    .delay(t)
    .slideUp(100, function(){ $(this).remove() });
}

function pushNotification(text, t) {
  //if (text == '') return;
	if (t == undefined) t = 1000;
  notifInit();
	$('<div>')
  	.addClass('notif')
  	.appendTo($('.notif_area'))
    .html(text)
    .delay(t)
    .slideUp(100, function(){ $(this).remove(); });
}
