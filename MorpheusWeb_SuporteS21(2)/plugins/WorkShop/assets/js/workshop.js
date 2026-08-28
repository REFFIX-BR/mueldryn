$(function () {

    $(document).on("change", "#workshop-item :input", function () {
        var $price = $("#upgrade-price");
        var $level = $("#item-level");
        var $option = $("#item-option");
        var $skill = $("#item-skill");
        var $luck = $("#item-luck");
        var $ancient = $("#item-ancient");
        var $harmony = $("#item-harmony");
        var $refine = $("#item-refine");
        var $sockets = $("[id^=item-socket-]");

        var $pricec = $(".workshop-upgrade-price");

        var price = 0;
        if ($level.val() > $level.data("default")) {
            price += ($level.val() - $level.data("default")) * $level.data("price");
        }

        if (($option.val() / 4) > ($option.data("default") / 4)) {
            price += (($option.val() / 4) - $option.data("default") / 4) * $option.data("price");
        }

        if (!$luck.is(":disabled") && $luck.is(":checked") && $luck.data("default") == 0) {
            price += $luck.data("price");
        }

        if (!$skill.is(":disabled") && $skill.size() > 0 && $skill.is(":checked") && $skill.data("default") == 0) {
            price += $skill.data("price");
        }

        if ($ancient.size() > 0 && $ancient.val() != $ancient.data("default")) {
            price += $ancient.data("price");
        }

        for (var i = 0; i <= 5; i++) {
            var $exc = $("#item-excellent-" + i);
            if (!$exc.is(":disabled") && $exc.size() > 0 && $exc.is(":checked") && $exc.data("default") == 0) {
                price += $exc.data("price");
            }
        }

        for (var i = 0; i < $sockets.size(); i++) {
            var $sock = $sockets.eq(i);
            if ($.inArray($sock.val(), ['255', '254']) && $sock.val() != $sock.data("default")) {
                price += $sock.data("price")
            }
        }

        if (!$refine.is(":disabled") && $refine.size() > 0 && $refine.is(":checked") && $refine.data("default") != 1) {
            price += $refine.data("price");
        }

        if ($harmony.size() > 0 && $harmony.val() != $harmony.data("default")) {
            price += $harmony.data("price")
        }

        if (price > 0) {
            $pricec.show();
        } else {
            $pricec.hide();
        }

        $price.html(price);
    });

});