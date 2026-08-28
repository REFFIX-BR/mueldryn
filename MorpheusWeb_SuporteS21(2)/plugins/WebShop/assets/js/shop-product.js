function itemPrice() {
    var $price = $("#product-price");
    var $level = $("#product-level");
    var $option = $("#product-option");
    var $skill = $("#product-skill");
    var $luck = $("#product-luck");
    var $ancient = $("#product-ancient");
    var $harmony = $("#product-harmony");
    var $refine = $("#product-refine");
    var $sockets = $("[id^=product-socket-]");

    var $cupon = $("#product-coupon");

    var price = $price.data("price");
    price += $level.size() > 0 ? ($level.val() * $level.data("price")) : 0;
    price += $option.size() > 0 ? (($option.val() / 4) * $option.data("price")) : 0;
    price += $luck.size() > 0 ? $luck.is(":checked") ? $luck.data("price") : 0 : 0;
    price += $skill.size() > 0 ? $skill.is(":checked") ? $skill.data("price") : 0 : 0;
    price += $ancient.size() > 0 ? $ancient.val() !== "0" ? $ancient.data("price") : 0 : 0;

    for (var i = 0; i <= 5; i++) {
        var $exc = $("#exc-" + i);
        if ($exc.size() > 0 && $exc.is(":checked")) {
            price += $exc.data("price");
        }
    }

    for (var i = 0; i < $sockets.size(); i++) {
        var $sock = $sockets.eq(i);
        if ($.inArray($sock.val(), ['255', '254'])) {
            price += $sock.data("price");
        }
    }

    price += $refine.size() > 0 ? $refine.is(":checked") ? $refine.data("price") : 0 : 0;
    price += $harmony.size() > 0 ? $harmony.val() !== "" ? $harmony.data("price") : 0 : 0;

    if ($cupon.size() > 0 && $cupon.data("discount")) {
        price -= price * $cupon.data("discount") / 100;
    }

    $price.html(Math.floor(price));
}

$(function () {

    $(document).on("change", "#shop-product :input", function () {
        itemPrice();
    });

    $(document).on("click", "#excellent-options :checkbox", function () {
        var $exc = $("#excellent-options");
        var max = $exc.data("max-excellents");
        var checkeds = $exc.find(":checkbox:checked").size();

        if (max == checkeds) {
            $exc.find(":checkbox:not(:checked)").attr("disabled", true);
        } else {
            $exc.find(":checkbox").attr("disabled", false);
        }
    });

    $(document).on("change", "#product-coupon", function () {
        var $this = $(this);

        $this.data("discount", 0);

        if ($.trim($this.val()) !== "") {
            $.getJSON($.morpheus.base_path + "json/shop/coupon/" + $this.val(), function (data) {
                if (data.success) {
                    $this.data("discount", data.data.discount);
                    itemPrice();
                } else {
                    $this.data("discount", 0);
                }
            })
        }

        itemPrice();
    });
});