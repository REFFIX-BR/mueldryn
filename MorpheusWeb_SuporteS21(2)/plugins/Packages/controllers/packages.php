<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

function packages_is_skin($package)
{
    $desc = isset($package["description"]) ? trim($package["description"]) : "";
    return stripos($desc, "skin:") === 0;
}

function packages_is_vip_card($package)
{
    $desc = isset($package["description"]) ? trim($package["description"]) : "";
    return stripos($desc, "vip:") === 0;
}

function packages_skin_rarity($package)
{
    $desc = isset($package["description"]) ? trim($package["description"]) : "";
    if (preg_match('/^skin:(normal|rare|epic|legendary)/i', $desc, $m)) {
        return strtolower($m[1]);
    }
    return "normal";
}

function packages_skin_blurb($package)
{
    $desc = isset($package["description"]) ? trim($package["description"]) : "";
    $lines = preg_split('/\r\n|\r|\n/', $desc);
    array_shift($lines);
    return trim(implode("\n", $lines));
}

function packages_vip_meta($package)
{
    $desc = isset($package["description"]) ? trim($package["description"]) : "";
    $lines = preg_split('/\r\n|\r|\n/', $desc);
    $family = "vip";
    if (preg_match('/^vip:([a-z0-9_-]+)/i', isset($lines[0]) ? $lines[0] : "", $m)) {
        $family = strtolower($m[1]);
    }
    $old = null;
    $discount = null;
    $blurbLines = array();
    foreach ($lines as $i => $line) {
        if ($i === 0) {
            continue;
        }
        if (preg_match('/^old:(.+)$/i', trim($line), $m)) {
            $old = (float) str_replace(",", ".", $m[1]);
            continue;
        }
        if (preg_match('/^discount:(.+)$/i', trim($line), $m)) {
            $discount = trim($m[1]);
            continue;
        }
        $blurbLines[] = $line;
    }
    return array(
        "family" => $family,
        "old" => $old,
        "discount" => $discount,
        "blurb" => trim(implode("\n", $blurbLines)),
    );
}

function packages_group_vip_cards($packages)
{
    $groups = array();
    foreach ($packages as $package) {
        if (!packages_is_vip_card($package)) {
            continue;
        }
        $meta = packages_vip_meta($package);
        $family = $meta["family"];
        if (!isset($groups[$family])) {
            $groups[$family] = array(
                "family" => $family,
                "title" => $package["package"],
                "image" => $package["image"],
                "blurb" => $meta["blurb"],
                "discount" => $meta["discount"],
                "options" => array(),
            );
        }
        $groups[$family]["options"][] = array(
            "id" => (int) $package["id"],
            "days" => (int) $package["vipdays"],
            "price" => (float) $package["price"],
            "old" => $meta["old"],
            "viptype" => $package["viptype"],
        );
        if (empty($groups[$family]["image"]) && !empty($package["image"])) {
            $groups[$family]["image"] = $package["image"];
        }
        if (empty($groups[$family]["discount"]) && !empty($meta["discount"])) {
            $groups[$family]["discount"] = $meta["discount"];
        }
    }
    foreach ($groups as &$group) {
        usort($group["options"], function ($a, $b) {
            return $a["days"] - $b["days"];
        });
        $default = null;
        foreach ($group["options"] as $opt) {
            if ($opt["days"] === 30) {
                $default = $opt;
                break;
            }
        }
        if ($default === null && !empty($group["options"])) {
            $default = $group["options"][0];
        }
        $group["default"] = $default;
    }
    unset($group);
    $order = array("all", "exp", "drop", "craft");
    $sorted = array();
    foreach ($order as $key) {
        if (isset($groups[$key])) {
            $sorted[] = $groups[$key];
            unset($groups[$key]);
        }
    }
    foreach ($groups as $group) {
        $sorted[] = $group;
    }
    return $sorted;
}

Route::get("/packages", service("credit-shop"), function () {
    $all = Connection::fetchAll("SELECT *\n        FROM mw_packages\n        WHERE active = 1\n        ORDER BY sequence\n    ");
    $vipCards = packages_group_vip_cards($all);
    $coinPackages = array();
    foreach ($all as $package) {
        if (!packages_is_skin($package) && !packages_is_vip_card($package)) {
            $coinPackages[] = $package;
        }
    }
    View::display("Packages.index", array("vipCards" => $vipCards, "packages" => $coinPackages));
});

Route::get("/skins", service("credit-shop"), function () {
    $all = Connection::fetchAll("SELECT *\n        FROM mw_packages\n        WHERE active = 1\n        ORDER BY sequence\n    ");
    $skins = array();
    foreach ($all as $package) {
        if (packages_is_skin($package)) {
            $package["rarity"] = packages_skin_rarity($package);
            $package["blurb"] = packages_skin_blurb($package);
            $skins[] = $package;
        }
    }
    $nextReset = config("skins.next_reset");
    if (empty($nextReset)) {
        $dt = new DateTime("next monday 18:00");
        $nextReset = $dt->format("c");
    }
    View::display("Packages.skins", array("skins" => $skins, "nextReset" => $nextReset));
});

Route::get("/packages/buy/:id", service("credit-shop"), function ($id) {
    $package = Connection::fetchAssoc("SELECT *\n        FROM mw_packages\n        WHERE id = ?\n    ", array($id));
    if (!empty($package)) {
        $accept = Input::get("accept");
        if (user()->getCredit() < $package["price"]) {
            return error(__("You do not have enough credits to make this purchase"));
        }
        if (!empty($package["viptype"]) && !isset($accept) && $package["viptype"] != user()->getVipType()) {
            return error(__("You are changing your vip plan would like to continue anyway?"), array("confirm" => true));
        }
        try {
            Connection::transactional(function () use($package) {
                user()->addCredit(0 - $package["price"]);
                $logs = array();
                if (!empty($package["viptype"])) {
                    if (user()->getVipExpire() == NULL || user()->getVipType() != $package["viptype"]) {
                        user()->setVipExpire(new DateTime());
                    }
                    user()->getVipExpire()->add(new DateInterval("P" . $package["vipdays"] . "D"));
                    user()->setVipType($package["viptype"]);
                    $vipLabel = config("vip.types." . $package["viptype"]);
                    if (is_array($vipLabel) && isset($vipLabel["name"])) {
                        $vipLabel = $vipLabel["name"];
                    }
                    $logs[] = $vipLabel . ": " . $package["vipdays"] . " " . __("days");
                }
                $options = json_decode($package["coins"], true);
                if (!is_array($options)) {
                    $options = array();
                }
                foreach ($options as $coin => $amount) {
                    if ($coin === "" || strpos($coin, "_") === 0) {
                        continue;
                    }
                    user()->addCoin($coin, $amount);
                    $logs[] = config("coins")[$coin]["name"] . ": " . $amount;
                }
                if (packages_is_skin($package)) {
                    $logs[] = "Skin: " . $package["package"];
                }
                user()->update();
                Connection::insert("mw_logs", array("log" => __("Bought the package %s by %s", array($package["package"], util("Number")->format($package["price"], 2, ",") . " [" . util("Text")->toList($logs, "e") . "]")), "account" => user()->getUsername(), "type" => "package", "table_name" => "mw_packages", "primary_key" => $package["id"]), array("string", "string", "string", "string", "string"));
            });
            $redirect = packages_is_skin($package) ? "/skins" : "/packages";
            return success(__("Package %s was purchased", array($package["package"])), array("partial" => "login", "redirect" => $redirect));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
});

?>
