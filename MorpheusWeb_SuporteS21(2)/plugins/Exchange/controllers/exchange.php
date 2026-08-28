<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

View::add("script", "Exchange.exchange");
Route::map("/panel/account/exchange", service("account.exchange"), function () {
    $account = user();
    if (Request::isPost()) {
        $amount = ceil(Input::post("amount"));
        $from = Input::post("from");
        $to = Input::post("to");
        $validation = new Morpheus\Core\Validation(Input::post(), array("amount" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "from" => Morpheus\Core\Validation::REQUIRED, "to" => Morpheus\Core\Validation::REQUIRED));
        if ($validation->isValid()) {
            $changes = config("exchange");
            if (!isset($changes[$from])) {
                return error(__("Invalid coin"));
            }
            if (!array_key_exists($to, $changes[$from]["rates"])) {
                return error(__("Invalid coin"));
            }
            if ($from === $to) {
                return error(__("You can't convert same coins type"));
            }
            if ($account->getCoin($from) < $amount) {
                return error(__("You don't have enough %s", array(config("coins", array())[$from]["name"])));
            }
            try {
                Connection::transactional(function () use($account, $amount, $to, $from) {
                    $changes = config("exchange");
                    $change = $changes[$from];
                    $tax = ceil($amount * $change["tax"] / 100);
                    $total = floor(($amount - $tax) * $change["rates"][$to]);
                    $account->addCoin($from, 0 - $amount);
                    $account->addCoin($to, $total);
                    $account->updateCoins();
                    Morpheus\Account\Service::payRates("account.exchange", $account);
                    $account->log(_e("Change %s to %s", array($amount . " " . util("Coin")->name($from), $total . " " . util("Coin")->name($to))), "exchange");
                });
                return success(__("Coins successfully converted"), array("partial" => "login", "redirect" => "/panel/account/exchange"));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::service("Exchange.exchange", "account.exchange", array("coins" => config("exchange", array())));
})->via("GET", "POST");
Route::get("/exchange/avaliable-coins/:coin", function ($coin) {
    $change = config("exchange")[$coin];
    $coins = array();
    foreach ($change["rates"] as $id => $rate) {
        $coins[$id] = config("coins")[$id]["name"];
    }
    return json(array("coins" => $coins));
});
Route::get("/exchange/calculate-rate", function () {
    $amount = ceil(Input::get("amount"));
    $from = Input::get("from");
    $to = Input::get("to");
    $changes = config("exchange");
    if ($from === $to) {
        return json(array("success" => false, "error" => __("You can't convert same coins type")));
    }
    if ($amount < 0) {
        return json(array("success" => false, "error" => __("Invalid amount value")));
    }
    if (isset($changes[$from])) {
        $change = $changes[$from];
        if (isset($change["rates"][$to])) {
            $tax = ceil($amount * $change["tax"] / 100);
            $total = ($amount - $tax) * $change["rates"][$to];
            return json(array("success" => true, "amount" => floor($total), "coin" => config("coins", array())[$to]["name"], "tax" => $tax, "tax_coin" => config("coins", array())[$from]["name"]));
        }
    }
});

?>