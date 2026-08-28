<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

$app->add(new Slim\Middleware\Session(array("name" => config("session.name", "morpheus"), "autorefresh" => true)));
$app->add(new Morpheus\Slim\Middleware\WhoopsMiddleware());
$app->container->singleton("connection", function () {
    return Morpheus\Database\DriverManager::getConnection();
});
$app->container->singleton("plugin", function () {
    return Plugin::getInstance();
});
Statical\SlimStatic\SlimStatic::boot($app);
$proxys = array("Connection" => "Morpheus\\Facade\\Proxy\\DriverManagerProxy", "Plugin" => "Morpheus\\Facade\\Proxy\\PluginProxy", "Session" => "Morpheus\\Facade\\Proxy\\SessionProxy");
foreach ($proxys as $alias => $proxy) {
    Statical::addProxyService($alias, $proxy, Container::getInstance(), strtolower($alias));
}

?>