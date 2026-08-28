<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

App::hook("slim.before.dispatch", function () {
    if (admin_logged_in() === false) {
        $uri = App::request()->getResourceUri();
        if ($uri !== "/admin/login") {
            App::redirect("/admin/login");
        }
    } else {
        $route = App::router()->getCurrentRoute();
        if ($route) {
            $name = $route->getName();
            if ($name && !admin("super_user") && !admin($name)) {
                App::redirect("/admin/forbidden");
            }
        }
    }
});
Morpheus\Menu\Menu::getInstance("admin")->add(array("id" => "accounts", "title" => __("Accounts"), "icon" => "users"))
->add(array("id" => "accounts.all", "title" => "Todas as Contas", "url" => "/admin/characters/loggedin"))
->add(array("id" => "accounts.online", "title" => __("Players online"), "url" => "/admin/characters/online"))
->add(array("id" => "items", "title" => __("Items"), "icon" => "cube"))
->add(array("id" => "items.find", "title" => __("Find by serial"), "url" => "/admin/items/find"))
->add(array("id" => "plugins", "title" => __("Plugins"), "icon" => "plug", "url" => "/admin/plugins"))
->add(array("id" => "marketchars", "title" => "Venda de Personagens", "icon" => "shopping-cart", "url" => "/admin/marketchars"))
->add(array("id" => "workshop", "title" => "Oficina", "icon" => "wrench", "url" => "/admin/workshop/config"))
->add(array("id" => "lootbox", "title" => "LootBox", "icon" => "gift", "url" => "/admin/lootboxes"))
->add(array("id" => "auction", "title" => "Leilão", "icon" => "gavel", "url" => "/admin/auctions/items"))
->add(array("id" => "guias", "title" => "Guias", "icon" => "book"))
->add(array("id" => "guias.categories", "title" => "Categorias", "url" => "/admin/guias/categories"))
->add(array("id" => "guias.guides", "title" => "Guias", "url" => "/admin/guias/guides"))
->add(array("id" => "achievements", "title" => "Conquistas", "icon" => "trophy", "url" => "/admin/achievements"))
->add(array("id" => "raffles", "title" => "Rifas", "icon" => "ticket", "url" => "/admin/raffles"))
->add(array("id" => "events", "title" => __("Events"), "icon" => "calendar", "url" => "/admin/events"))
->add(array("id" => "configs", "title" => __("Configs"), "sequence" => 99, "icon" => "gears"))
->add(array("id" => "configs.template", "title" => __("Template"), "url" => "/admin/configs/template"))
->add(array("id" => "configs.general", "title" => __("General"), "url" => "/admin/configs/general"))
->add(array("id" => "configs.downloads", "title" => __("Downloads"), "url" => "/admin/downloads"))
->add(array("id" => "configs.coins", "title" => __("Coins"), "url" => "/admin/coins"))
->add(array("id" => "configs.vip", "title" => __("Vip system"), "url" => "/admin/configs/vip"))
->add(array("id" => "configs.services", "title" => __("Services"), "url" => "/admin/services"))
->add(array("id" => "configs.register", "title" => __("Register"), "url" => "/admin/configs/register"))
->add(array("id" => "configs.rankings", "title" => __("Rankings"), "url" => "/admin/configs/rankings"))
->add(array("id" => "configs.emails", "title" => __("E-mail templates"), "url" => "/admin/configs/emails"))
->add(array("id" => "configs.cronjobs", "title" => __("Cron Jobs"), "url" => "/admin/cronjobs"))
->add(array("id" => "configs.sep1", "title" => "-"))
->add(array("id" => "configs.groups", "title" => __("Groups"), "url" => "/admin/groups"))
->add(array("id" => "configs.users", "title" => __("Users"), "url" => "/admin/users"))
->add(array("id" => "configs.sep2", "title" => "-"));
/*
->add(array("id" => "configs.update", "title" => "Morpheus update", "url" => "/admin/update", "sequence" => 9999))
->add(array("id" => "mw-market", "title" => __("Morpheus Market"), "sequence" => 100, "icon" => "cart-plus"))
->add(array("id" => "mw-market.plugins", "title" => __("Plugins"), "url" => "/admin/market/plugins"))
->add(array("id" => "mw-market.templates", "title" => __("Templates"), "url" => "/admin/market/templates"));
*/
?>