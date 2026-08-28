function payment() {
    $payment = $("#payment");
    $coin = $("#coin").closest(".form-coin");
    if ($payment.val() == "coin") {
        $coin.show();
    } else {
        $coin.hide();
    }
}

$(function () {
    payment();
    $(document).on("change", "#payment", function () {
        payment();
    });
});