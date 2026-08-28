function slide() {
    var $slider = $("#slider");

    if ($slider.size() > 0) {
        var sp = $slider.slippry();
    }
}

$(function () {
    slide();

    $(document).on("pjax:end", function() {
        var $slider = $(".slider-container");
        if ($slider.data("live")) {
            slide();
        }
    })
});