$(function () {
    $(".services-table .root").each(function () {
        var $this = $(this);

        $this.sortable({
            opacity: 0.6,
            items: $this.find("td"),
            cursor: "move",
            placeholder: "placeholder",
            axis: "y"
        });

    });
});