$(function () {
   
    $(document).on("submit", ".serch-account", function (e) {
        var $this = $(this);
        var $container = $this.closest(".window_frame");

        overlay.create();
        $.post($this.attr("action"), $this.serialize(), function (data) {
            $(".search-accounts-result").html(data);
            overlay.destroy();
        });
        
        e.preventDefault();
    });

    $(document).uitooltip({
        items: ".item",
        track: true,
        content: function (callback) {
            var $this = $(this);
            $.get($this.data("tooltip-url"), function (data) {
                callback(data);
            });
        }
    });
});