<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

App::hook("accounts.register", function ($account) {
    $code = Session::get("icode");
    if ($code !== NULL) {
        $id = Connection::fetchColumn("select memb___id \n            from " . table_with_db("MEMB_INFO") . " \n            where indication_code = ?\n        ", array($code));
        $acc = account($id);
        if ($acc->exists()) {
            Connection::update(table_with_db("MEMB_INFO"), array("indicated_by" => $code), array("memb___id" => $account->getUsername()));
        }
    }
});
App::hook("accounts.info", function ($account) {
    $code = Connection::fetchColumn("select indication_code \n        from " . table_with_db("MEMB_INFO") . " \n        where memb___id = ?\n    ", array($account["username"]));
    $indications = Connection::fetchAll("select memb___id \n        from " . table_with_db("MEMB_INFO") . " \n        where indicated_by = ?\n    ", array($code));
    $rewards = Connection::fetchAll("select sum(ir.amount) as total\n        , ig.coin as coin\n        from mw_indication_rewards ir\n        join mw_indication_goals ig on ig.id = ir.indication_goal_id\n        where ir.rewarded = 0\n        and ir.account = ?\n        group by ig.coin\n    ", array($account["username"]));
    View::display("Indication.info", array("layout" => false, "indications" => count($indications), "rewards" => $rewards, "code" => $code));
});
Route::get("/indication/rescue", function () {
    $rewards = Connection::fetchAll("select ir.amount as total\n        , ig.coin as coin\n        , ir.id as reward_id\n        from mw_indication_rewards ir\n        join mw_indication_goals ig on ig.id = ir.indication_goal_id\n        where ir.rewarded = 0\n        and account = ?\n    ", array(user()->getUsername()));
    try {
        Connection::transactional(function ($conn) use($rewards) {
            $coins = array();
            foreach ($rewards as $reward) {
                $conn->update("mw_indication_rewards", array("rewarded" => 1), array("id" => $reward["reward_id"]));
                user()->addCoin($reward["coin"], $reward["total"]);
                user()->updateCoins();
                if (!isset($coins[$reward["coin"]])) {
                    $coins[$reward["coin"]] = 0;
                }
                $coins[$reward["coin"]] += $reward["total"];
            }
            foreach ($coins as $coin => $total) {
                user()->log(_e("Rescued %s in indication program", array($total . " " . util("Coin")->name($coin))), "indication", NULL, NULL, array("amount" => $total, "coin" => $coin));
            }
        });
        return success("Coin's added in your account", array("redirect" => "/panel/account/info", "partial" => "panel"));
    } catch (Exception $ex) {
        return error($ex);
    }
});
Route::get("/i-:code", function ($code) {
    Session::set("icode", $code);
    App::redirect("/register");
});
Route::get("/indication/participate", function () {
    $code = Connection::fetchColumn("select max(indication_code) \n        from " . table_with_db("MEMB_INFO") . "\n    ");
    Connection::update(table_with_db("MEMB_INFO"), array("indication_code" => $code + 1), array("memb___id" => user()->getUsername()));
    return success(__("Now you are participating the indication program"), array("redirect" => "/panel/account/info"));
});

?>