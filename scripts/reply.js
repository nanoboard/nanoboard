/*!
 * jQuery.selection - jQuery Plugin
 *
 * Copyright (c) 2010-2014 IWASAKI Koji (@madapaja).
 * http://blog.madapaja.net/
 * Under The MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
(function($, win, doc) {
    /**
     * get caret status of the selection of the element
     *
     * @param   {Element}   element         target DOM element
     * @return  {Object}    return
     * @return  {String}    return.text     selected text
     * @return  {Number}    return.start    start position of the selection
     * @return  {Number}    return.end      end position of the selection
     */
    var _getCaretInfo = function(element){
        var res = {
            text: '',
            start: 0,
            end: 0
        };

        if (!element.value) {
            /* no value or empty string */
            return res;
        }

        try {
            if (win.getSelection) {
                /* except IE */
                res.start = element.selectionStart;
                res.end = element.selectionEnd;
                res.text = element.value.slice(res.start, res.end);
            } else if (doc.selection) {
                /* for IE */
                element.focus();

                var range = doc.selection.createRange(),
                    range2 = doc.body.createTextRange();

                res.text = range.text;

                try {
                    range2.moveToElementText(element);
                    range2.setEndPoint('StartToStart', range);
                } catch (e) {
                    range2 = element.createTextRange();
                    range2.setEndPoint('StartToStart', range);
                }

                res.start = element.value.length - range2.text.length;
                res.end = res.start + range.text.length;
            }
        } catch (e) {
            /* give up */
        }

        return res;
    };

    /**
     * caret operation for the element
     * @type {Object}
     */
    var _CaretOperation = {
        /**
         * get caret position
         *
         * @param   {Element}   element         target element
         * @return  {Object}    return
         * @return  {Number}    return.start    start position for the selection
         * @return  {Number}    return.end      end position for the selection
         */
        getPos: function(element) {
            var tmp = _getCaretInfo(element);
            return {start: tmp.start, end: tmp.end};
        },

        /**
         * set caret position
         *
         * @param   {Element}   element         target element
         * @param   {Object}    toRange         caret position
         * @param   {Number}    toRange.start   start position for the selection
         * @param   {Number}    toRange.end     end position for the selection
         * @param   {String}    caret           caret mode: any of the following: "keep" | "start" | "end"
         */
        setPos: function(element, toRange, caret) {
            caret = this._caretMode(caret);

            if (caret === 'start') {
                toRange.end = toRange.start;
            } else if (caret === 'end') {
                toRange.start = toRange.end;
            }

            element.focus();
            try {
                if (element.createTextRange) {
                    var range = element.createTextRange();

                    if (win.navigator.userAgent.toLowerCase().indexOf("msie") >= 0) {
                        toRange.start = element.value.substr(0, toRange.start).replace(/\r/g, '').length;
                        toRange.end = element.value.substr(0, toRange.end).replace(/\r/g, '').length;
                    }

                    range.collapse(true);
                    range.moveStart('character', toRange.start);
                    range.moveEnd('character', toRange.end - toRange.start);

                    range.select();
                } else if (element.setSelectionRange) {
                    element.setSelectionRange(toRange.start, toRange.end);
                }
            } catch (e) {
                /* give up */
            }
        },

        /**
         * get selected text
         *
         * @param   {Element}   element         target element
         * @return  {String}    return          selected text
         */
        getText: function(element) {
            return _getCaretInfo(element).text;
        },

        /**
         * get caret mode
         *
         * @param   {String}    caret           caret mode
         * @return  {String}    return          any of the following: "keep" | "start" | "end"
         */
        _caretMode: function(caret) {
            caret = caret || "keep";
            if (caret === false) {
                caret = 'end';
            }

            switch (caret) {
                case 'keep':
                case 'start':
                case 'end':
                    break;

                default:
                    caret = 'keep';
            }

            return caret;
        },

        /**
         * replace selected text
         *
         * @param   {Element}   element         target element
         * @param   {String}    text            replacement text
         * @param   {String}    caret           caret mode: any of the following: "keep" | "start" | "end"
         */
        replace: function(element, text, caret) {
            var tmp = _getCaretInfo(element),
                orig = element.value,
                pos = $(element).scrollTop(),
                range = {start: tmp.start, end: tmp.start + text.length};

            element.value = orig.substr(0, tmp.start) + text + orig.substr(tmp.end);

            $(element).scrollTop(pos);
            this.setPos(element, range, caret);
        },

        /**
         * insert before the selected text
         *
         * @param   {Element}   element         target element
         * @param   {String}    text            insertion text
         * @param   {String}    caret           caret mode: any of the following: "keep" | "start" | "end"
         */
        insertBefore: function(element, text, caret) {
            var tmp = _getCaretInfo(element),
                orig = element.value,
                pos = $(element).scrollTop(),
                range = {start: tmp.start + text.length, end: tmp.end + text.length};

            element.value = orig.substr(0, tmp.start) + text + orig.substr(tmp.start);

            $(element).scrollTop(pos);
            this.setPos(element, range, caret);
        },

        /**
         * insert after the selected text
         *
         * @param   {Element}   element         target element
         * @param   {String}    text            insertion text
         * @param   {String}    caret           caret mode: any of the following: "keep" | "start" | "end"
         */
        insertAfter: function(element, text, caret) {
            var tmp = _getCaretInfo(element),
                orig = element.value,
                pos = $(element).scrollTop(),
                range = {start: tmp.start, end: tmp.end};

            element.value = orig.substr(0, tmp.end) + text + orig.substr(tmp.end);

            $(element).scrollTop(pos);
            this.setPos(element, range, caret);
        }
    };

    /* add jQuery.selection */
    $.extend({
        /**
         * get selected text on the window
         *
         * @param   {String}    mode            selection mode: any of the following: "text" | "html"
         * @return  {String}    return
         */
        selection: function(mode) {
            var getText = ((mode || 'text').toLowerCase() === 'text');

            try {
                if (win.getSelection) {
                    if (getText) {
                        // get text
                        return win.getSelection().toString();
                    } else {
                        // get html
                        var sel = win.getSelection(), range;

                        if (sel.getRangeAt) {
                            range = sel.getRangeAt(0);
                        } else {
                            range = doc.createRange();
                            range.setStart(sel.anchorNode, sel.anchorOffset);
                            range.setEnd(sel.focusNode, sel.focusOffset);
                        }

                        return $('<div></div>').append(range.cloneContents()).html();
                    }
                } else if (doc.selection) {
                    if (getText) {
                        // get text
                        return doc.selection.createRange().text;
                    } else {
                        // get html
                        return doc.selection.createRange().htmlText;
                    }
                }
            } catch (e) {
                /* give up */
            }

            return '';
        }
    });

    /* add selection */
    $.fn.extend({
        selection: function(mode, opts) {
            opts = opts || {};

            switch (mode) {
                /**
                 * selection('getPos')
                 * get caret position
                 *
                 * @return  {Object}    return
                 * @return  {Number}    return.start    start position for the selection
                 * @return  {Number}    return.end      end position for the selection
                 */
                case 'getPos':
                    return _CaretOperation.getPos(this[0]);

                /**
                 * selection('setPos', opts)
                 * set caret position
                 *
                 * @param   {Number}    opts.start      start position for the selection
                 * @param   {Number}    opts.end        end position for the selection
                 */
                case 'setPos':
                    return this.each(function() {
                        _CaretOperation.setPos(this, opts);
                    });

                /**
                 * selection('replace', opts)
                 * replace the selected text
                 *
                 * @param   {String}    opts.text            replacement text
                 * @param   {String}    opts.caret           caret mode: any of the following: "keep" | "start" | "end"
                 */
                case 'replace':
                    return this.each(function() {
                        _CaretOperation.replace(this, opts.text, opts.caret);
                    });

                /**
                 * selection('insert', opts)
                 * insert before/after the selected text
                 *
                 * @param   {String}    opts.text            insertion text
                 * @param   {String}    opts.caret           caret mode: any of the following: "keep" | "start" | "end"
                 * @param   {String}    opts.mode            insertion mode: any of the following: "before" | "after"
                 */
                case 'insert':
                    return this.each(function() {
                        if (opts.mode === 'before') {
                            _CaretOperation.insertBefore(this, opts.text, opts.caret);
                        } else {
                            _CaretOperation.insertAfter(this, opts.text, opts.caret);
                        }
                    });

                /**
                 * selection('get')
                 * get selected text
                 *
                 * @return  {String}    return
                 */
                case 'get':
                    /* falls through */
                default:
                    return _CaretOperation.getText(this[0]);
            }

            return this;
        }
    });
})(jQuery, window, window.document);


//check is base64 without throw error
function isBase64(str) {//return true, or false
    if(str===''){return false;}//if string is empty and not contains base64 characters
    try {
        return btoa(atob(str)) == str; //true if base64
    } catch (err) {
        return false;
    }
}

//append style for textarea, when this contains base64 error.
function appendStyle(styles) {
  var css = document.createElement('style');
  css.type = 'text/css';

  if (css.styleSheet) css.styleSheet.cssText = styles;
  else css.appendChild(document.createTextNode(styles));

  document.getElementsByTagName("head")[0].appendChild(css);
}
var textarea_base64_error = '.base64_error{border: 2px solid red;}'
window.onload = function() { appendStyle(textarea_base64_error) };		//do appending

//function to check base64 for all elements, with one bb-code.
function check_base64_in_tags(text, bb_code){//bb_code = 'img', 'xmg', 'file', or another bb_code, where need to check base64...

	if(text.indexOf('['+bb_code)!==-1){	//if '[img', '[xmg', '[file' - founded in string 
		var regex = new RegExp('\\['+bb_code, 'gi'),	//	'/\[img/gi', '/\[xmg/gi', '/\[file/gi'
		//var regex = /\[xmg=/gi,
		result,
		tag_indexes = [];							//array with indexes, where bb_code was been founded
		while ( (result = regex.exec(text)) ) {
			tag_indexes.push(result.index);			//push index to array
		}
		//console.log('tag_indexes: ', tag_indexes);	//show array with indexes
		
		for(i=0;i<tag_indexes.length;i++){															//for each index
			//console.log('tag_indexes[i]', tag_indexes[i]);
			var first_index_base64 =
				(bb_code=='img' || bb_code == 'xmg')												//if 'img' or 'xmg'
				? text.indexOf('=', tag_indexes[i])+1												//read base from next symbol '=' ('[img=', '[xmg=').
				: (bb_code==='file')																//or else, if [file(...)]
					? text.indexOf(']', tag_indexes[i])+1											//read base from next ']', and skip attributes...
					: tag_indexes[i];																//or leave current index for another tags...
			//console.log('bb_code', bb_code, 'first_index_base64', first_index_base64);

			var last_index =																		//find last index of base64
				text.indexOf(
					(bb_code==='file')																//for 'file' bb-code
						?	'['																		//this is index of next founded '['
						:	']'																		//for 'img' and 'xmg' this is index of next founded ']'
					,
					tag_indexes[i]+1																//search from next index, after previous index.
				);
			//console.log('first_index_base64', first_index_base64, 'last_index', last_index);
			
			var must_be_base64 = text.substring(first_index_base64, last_index);
			//console.log('must_be_base64', must_be_base64);
			if(!isBase64(must_be_base64)){
				return [bb_code, first_index_base64, last_index];										//if not base64 founded, return index...
			}
		}
	}
	return true;																					//else, after all - return true;
}

//check base64 for many bb-codes.
function check_base64(textarea){
	var text = textarea.value;
	
	var bb_codes_with_base64 = ['xmg', 'img', 'file'];												//array with bb-codes, where need to check base64...
	var result;																						//just define this.
	for(index_code=0; index_code<bb_codes_with_base64.length; index_code++){						//for each bb-code
		result = check_base64_in_tags(text, bb_codes_with_base64[index_code]);						//check all base64 strings for this code in the text
		if(result!==true){																			//if somewhere not base64, array will be returned
			textarea.title =																			//add data from this array to title
			"'"+result[0]+"'"+' bb-code: Base64 not detected!\nOffsets in text: '+result[1]+'-'+result[2]+
			'\n\n'+ 'If you send this post, data can be not displayed correctly.'+
			'\nAlso, you can insert this bb-code (without base64)'
			'\nbetween bb-code [code][/code], and ignore this warning.\n';
			//textarea.style.border = '1px solid red';															//make red border for textarea.
			textarea.classList.add('base64_error');																//but add class to activate appended style.
			return false;																						//and return false.
		}
	}
	//else, if false not returned, all base64 is base64-encoded.
	textarea.title = '';																						//remove title
//	textarea.style.border = '1px solid lime';																	//change border color to lime
	textarea.classList.remove('base64_error');																	//remove class, and turn textarea back.
	return true;																								//return true;

/*
	//test text for textarea:
[xmg=123][xmg=4567][xmg=891][img=011][img=1213][img=141][file]516[/file][file]1718[/file][file]192[/file]
	//4 digits interpretting as base64
*/	

}

function addReplyForm(id) {
  var form = $('<div>')
    .addClass('post').addClass('reply-div')
    .insertAfter($('#' + id))
    .css('margin-left', parseInt($('#' + id).css('margin-left')) + _treeOffsetPx + 'px')
    .append($('<div>').addClass('reply')
      .append($('<textarea oninput="check_base64(this);" onclick="check_base64(this);">').val('[g]' + new Date().toUTCString() + ', client: 3.0[/g]\n'))
      .append($('<br>'))
      .append($('<button>')
        .addClass('reply-button btn btn-danger ')
        .text('Cancel')
        .click(function() {
          $(this).parent().parent().remove();
        }))
      .append($('<button>')
        .text('Send')
        .addClass('reply-button btn btn-primary')
        .click(function() {
          var pst = Base64.encode(id + $(this).parent().find('textarea').val());
          var waitPowModal = $('<div>');
          waitPowModal.addClass('pow_modal');
          waitPowModal.html('<b>Wait for POW</b><br/>usually less than a minute...');
          $('body').append(waitPowModal);
          var form = $(this).parent().parent();
          form.hide();
          $.post('../pow', pst)
            .done(function(token){
              $.get('../captcha/' + token)
                .done(function(dataUri){
                  waitPowModal.remove();
                  var captchaModal = $('<div>');
                  captchaModal.addClass('captcha_modal');
                  captchaModal.append('<img class="captcha_image" src="' + dataUri + '"><br/>');
                  captchaModal.append('<textarea class="captcha_answer"></textarea><br/>');
                  var captchaBtn = $('<button>');
                  captchaBtn
                    .text('Send')
                    .addClass('reply-button btn btn-primary')
                    .click(function(){
                      var answer = $('.captcha_answer').val();
                      $.post('../solve/' + token, Base64.encode(answer))
                        .done(function(postStr){
                          form.remove();
                          mockSendPostToDb(JSON.parse(postStr));
                          captchaModal.remove();
                        })
                        .fail(function(){
                          captchaModal.remove();
                          pushNotification('Wrong captcha answer, please try again', 5000);
                          form.show();
                        });
                    });
                  captchaModal.append(captchaBtn);
                  captchaModal.append($('<button>').text('Cancel').addClass('reply-button btn btn-danger').click(function(){
                    captchaModal.remove();
                    form.show();
                    $.post('../solve/' + token, Base64.encode("~~~~~"));
                  }));
                  $('body').append(captchaModal);
                  $('.captcha_answer').focus();
                });
            });
        })
        /*.click(function() {
          sendPostToDb({
            'replyTo': id,
            'message': Base64.encode($(this).parent().find('textarea').val())
          });
          $(this).parent().parent().remove();
        })*/)
      .append(($('<button>')
        .text('Attach image')
        .addClass('reply-button btn btn-default')
        .click(function() {
        __current_text_input=$(this).parent().children(":first")
        $('#imgmodal').modal()
        $('#scale').click()
        })))
      .append('<hr>Format selection: ')
      .append($('<a href=javascript:void(0)>')
        .html('<b>[b]</b>')
        .click(function(){
          var sel = $(this).parent().find('textarea').selection();
          $(this).parent().find('textarea').selection('replace', {text: '[b]' + sel + '[/b]'});
        })
      )
      .append($('<a href=javascript:void(0)>')
        .html('<i>[i]</i>')
        .click(function(){
          var sel = $(this).parent().find('textarea').selection();
          $(this).parent().find('textarea').selection('replace', {text: '[i]' + sel + '[/i]'});
        })
      )
      .append($('<a href=javascript:void(0)>')
        .html('<span style=text-decoration:underline>[u]</span>')
        .click(function(){
          var sel = $(this).parent().find('textarea').selection();
          $(this).parent().find('textarea').selection('replace', {text: '[u]' + sel + '[/u]'});
        })
      )
      .append($('<a href=javascript:void(0)>')
        .html('<span style=text-decoration:line-through>[s]</span>')
        .click(function(){
          var sel = $(this).parent().find('textarea').selection();
          $(this).parent().find('textarea').selection('replace', {text: '[s]' + sel + '[/s]'});
        })
      )
      .append($('<a href=javascript:void(0)>')
        .html('[spoiler]')
        .click(function(){
          var sel = $(this).parent().find('textarea').selection();
          $(this).parent().find('textarea').selection('replace', {text: '[spoiler]' + sel + '[/spoiler]'});
        })
      )
    );
    var offset = form.offset();
    offset.top -= 100;
    $('html, body').animate({
      scrollTop: offset.top,
      scrollLeft: offset.left
     });
}