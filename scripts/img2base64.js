$(function(){
/// sharpen image:
/// USAGE:
///    sharpen(context, width, height, mixFactor)
///  mixFactor: [0.0, 1.0]
function sharpen(ctx, w, h, mix) {

  var weights = [0, -1, 0, -1, 5, -1, 0, -1, 0],
    katet = Math.round(Math.sqrt(weights.length)),
    half = (katet * 0.5) | 0,
    dstData = ctx.createImageData(w, h),
    dstBuff = dstData.data,
    srcBuff = ctx.getImageData(0, 0, w, h).data,
    y = h;

  while (y--) {

    x = w;

    while (x--) {

      var sy = y,
        sx = x,
        dstOff = (y * w + x) * 4,
        r = 0,
        g = 0,
        b = 0,
        a = 0;

      for (var cy = 0; cy < katet; cy++) {
        for (var cx = 0; cx < katet; cx++) {

          var scy = sy + cy - half;
          var scx = sx + cx - half;

          if (scy >= 0 && scy < h && scx >= 0 && scx < w) {

            var srcOff = (scy * w + scx) * 4;
            var wt = weights[cy * katet + cx];

            r += srcBuff[srcOff] * wt;
            g += srcBuff[srcOff + 1] * wt;
            b += srcBuff[srcOff + 2] * wt;
            a += srcBuff[srcOff + 3] * wt;
          }
        }
      }

      dstBuff[dstOff] = r * mix + srcBuff[dstOff] * (1 - mix);
      dstBuff[dstOff + 1] = g * mix + srcBuff[dstOff + 1] * (1 - mix);
      dstBuff[dstOff + 2] = b * mix + srcBuff[dstOff + 2] * (1 - mix)
      dstBuff[dstOff + 3] = srcBuff[dstOff + 3];
    }
  }

  ctx.putImageData(dstData, 0, 0);
}

var _loader=$('#inputFileToLoad')[0];

function updateImage(loader) {
  _loader = loader;
  var file = _loader.files[0];
  var reader = new FileReader();
  reader.onloadend = function() {
    var res = reader.result;
    if ($('#imgtype').val().startsWith('No compression')) {
      $('#result').text(res.toString());
        $('#info').html('Length (base64): ' + res.length +
          '<br>Max allowed: 64512');
        if (res.length > 64512) {
          $('.output').find('img').attr('src', 'error');
          $('#info').css('color','red');
        } else {
          $('#result').text('[xmg='+res.substring(res.indexOf(',')+1)+']');
          $('.output').find('img').attr('src', res);
          $('#info').css('color','black');
        }
      return;
    }
    var canvas = document.createElement("canvas");
    var ctx = canvas.getContext("2d");
    img = new Image();
    img.onload = function() {
      canvas.width = img.width;
      canvas.height = img.height;
      ctx.drawImage(img, 0, 0, img.width, img.height, 0, 0, img.width, img.height);
      sharpen(ctx, img.width, img.height, $('#sharpness').val() / 100.0);
      var scale = 1.0 / ($('#scale').val() / 100.0);
      var shr = new Image();
      shr.onload = function() {
        canvas.width = img.width / scale;
        canvas.height = img.height / scale;
        ctx.drawImage(
          shr, 0, 0, img.width, img.height, 0, 0,
          img.width / scale, img.height / scale);
        var imgType = $('#imgtype').val()=='JPEG'?'image/jpeg':'image/webp';
        var dataURL = canvas.toDataURL(imgType, $('#quality').val() / 100.0);
        $('#img-preview-btn').off();
        $('#img-preview-btn').click(function(){
          $('body').append(
            $('<img>')
              .attr('src', dataURL)
              .css('max-width', '10000px')
              .css('max-height', '10000px')
              .css('position', 'fixed')
              .css('z-index', '10000')
              .css('top', '0')
              .css('left', '0')
              .click(function(){
                $(this).remove();
              }));
        });
        $('#info').html('Length (base64): ' + dataURL.length +
          '<br>Max allowed: 64512<br>'+Math.floor(img.width/scale)+'x'+Math.floor(img.height/scale)+'px');
        if (dataURL.length > 64512) {
          $('#info').css('color','red');
          $('#result').text('error');
        } else {
          $('#info').css('color','black');
          $('#result').text('[xmg='+dataURL.substring(dataURL.indexOf(',')+1)+']');
        }
      };
      var shrd = canvas.toDataURL();
      shr.src = shrd;
    }
    img.src = res;
  }
  reader.readAsDataURL(file);
}
/*
$('#update').click(function() {
  updateImage(_loader);
});
*/
$('#imgtype').change(function() {
  updateImage(_loader);
});


$('#sharpness').change(function() {
  updateImage(_loader);
});
$('#quality').change(function() {
  updateImage(_loader);
});
$('#scale').change(function() {
  updateImage(_loader);
});
$('.apply-modal').click(function() {
    var cursorPos = __current_text_input.prop('selectionStart');
    v = __current_text_input.val();
    var textBefore = v.substring(0,  cursorPos);
    var textAfter  = v.substring(cursorPos);
    __current_text_input.val( textBefore+$("#result").text()+textAfter );
    $("#imgmodal").modal('hide')
})

$('#inputFileToLoad').change(function() {
  updateImage(this)
});
});