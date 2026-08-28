$(function () {

	$(document).on("change", "#coin-from", function () {
		var $this = $(this);
		var $to = $("#coin-to");

		$to.html('<option value=""></option>');
		$.get($.morpheus.base_path + "exchange/avaliable-coins/" + $this.val(), function (data) {
			$.each(data.coins, function (key, value) {
				$to.append('<option value="' + key + '">' + value + '</option>');
			})
		});
	});

	$(document).on("change", ".calculate-rate", function () {

		var $amount = $("#coin-amount");
		var $from = $("#coin-from");
		var $to = $("#coin-to");
		var $camount = $("#change-amount");
		var $atext = $("#change-amount-text");
		var $tax = $("#change-tax");
		var $ttext = $("#change-tax-text");

		if ($amount.val() !== "" 
			&& $from.val() !== ""
			&& $to.val() !== "") {

			$.get($.morpheus.base_path + "exchange/calculate-rate?amount=" + $amount.val() + "&from=" + $from.val() + "&to=" + $to.val(), function (data) {
				if (data.success) {
					$atext.show();
					$camount.text(data.amount + " " + data.coin);
					if (data.tax > 0) {
						$ttext.show();
						$tax.html(data.tax + " " + data.tax_coin);
					}
				} else {
					$ttext.hide();
					$tax.html("");
					$atext.hide();
					$camount.html('<span style="color:red">' + data.error + '</span>');
				}
			});
		} else {
			$camount.html("");
			$ttext.hide();
			$tax.html("");
			$atext.hide();
		}

	});

});