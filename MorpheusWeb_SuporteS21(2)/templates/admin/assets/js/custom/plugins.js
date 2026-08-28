$(function () {
    $(document).on("click", ".check-plugin-verions", function (e) {
        var $this = $(this);
        $this.button('loading');

        $.getJSON(base + "admin/plugins/check-updates", function (data) {
            $.each(data, function (index, plugin) {
                var $p = $(".plugin-" + plugin.name);
                if ($p.size() > 0) {
                    $p.addClass("warning");
                    $p.find(".update").show();

                    $n = $p.find(".new-version")
                    $n.show().append(" " + plugin.update);
                }
            });
            $this.button('reset');
        });
        e.preventDefault();
    })
});