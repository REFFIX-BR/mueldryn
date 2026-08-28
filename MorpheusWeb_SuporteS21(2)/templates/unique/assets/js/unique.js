function tooltip() {
    $("[data-toggle=tooltip]").tooltip({
        html: true
    });
}

function tabs() {
    $("ul.tabs li a").click(function(e) {
        var $this = $(this);
        var tab_id = $this.attr("data-tab");
        var $li = $this.closest("li");

        $("ul.tabs li").removeClass("active");
        $(".tab-content").removeClass("active");

        $li.addClass("active");
        $("#" + tab_id).addClass("active");

        e.preventDefault();
    });
}

function scroll() {
    var st = $(window).scrollTop();
    if (st > 0) {
        $(".navbar").addClass("sticky-top resumed");
    } else {
        $(".navbar").removeClass("sticky-top resumed");
    }
}

$(function () {
    tooltip();
    tabs();
    scroll();

    $(window).scroll(function () {
        scroll();
    });

    $("#change-top-ranking").change(function () {
        var $this = $(this);
        var target = $this.val();

        $(".top-ranking-home").hide();
        $("#" + target).fadeIn();
    });

    $(document).on("pjax:end", function() {
        tooltip();
        tabs();
    });

    $(document).on("pjax:success", function() {
        $("html, body").animate({
            scrollTop: $('.ajax-container').offset().top - 200
        }, 500);
    });
});