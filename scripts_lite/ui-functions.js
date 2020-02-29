function vd_onclick(event){
        event.preventDefault();
        var prefix='<iframe width="420" height="315" src="https://www.youtube.com/embed/'
        var postfix='" frameborder="0" allowfullscreen></iframe>'
        var jelem=$(event.target)
        jelem.off("click");
        var finder=new RegExp("v=.*$")
        var elem=event.target.toString();
        var id =elem.match(finder)[0].toString().replace("v=","")
        var vid_elem=jelem.after(prefix+id+postfix).next()
        console.log(vid_elem)
        jelem.removeClass( "vd-vid" ).addClass( "vd-shown" );
        jelem.click(function(event){
        event.preventDefault();
        vid_elem.remove()
        $(event.target).removeClass( "vd-shown" ).addClass( "vd-vid" ).off("click").click(vd_onclick);
        
        })
        //jelem
    }
function active_tab(id){
$(".nav li").removeClass("active")
$("#"+id).addClass("active")
}
function vid_show() {
    $("a.vd-vid").off("click")
    $("a.vd-vid").click(vd_onclick)
}
