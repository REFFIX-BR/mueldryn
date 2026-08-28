<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Slim\Middleware;

class WhoopsMiddleware extends \Slim\Middleware
{
    public function call()
    {
        $app = $this->app;
        if ($app->config("debug") === true) {
            $app->config("debug", false);
            $app->container->singleton("whoopsPrettyPageHandler", function () {
                return new \Whoops\Handler\PrettyPageHandler();
            });
            $app->container->singleton("whoopsJsonResponseHandler", function () {
                $handler = new \Whoops\Handler\JsonResponseHandler();
                return $handler;
            });
            $app->whoopsSlimInfoHandler = $app->container->protect(function () use($app) {
                try {
                    $request = $app->request();
                } catch (RuntimeException $e) {
                    return NULL;
                }
                $current_route = $app->router()->getCurrentRoute();
                $route_details = array();
                if ($current_route !== null) {
                    $route_details = array("Route Name" => $current_route->getName() ?: "<none>", "Route Pattern" => $current_route->getPattern() ?: "<none>", "Route Middleware" => $current_route->getMiddleware() ?: "<none>");
                }
                $app->whoopsPrettyPageHandler->addDataTable("Slim Application", array_merge(array("Charset" => $request->headers("ACCEPT_CHARSET"), "Locale" => $request->getContentCharset() ?: "<none>", "Application Class" => get_class($app)), $route_details));
                $app->whoopsPrettyPageHandler->addDataTable("Slim Application (Request)", array("URI" => $request->getRootUri(), "Request URI" => $request->getResourceUri(), "Path" => $request->getPath(), "Query String" => $request->params() ?: "<none>", "HTTP Method" => $request->getMethod(), "Script Name" => $request->getScriptName(), "Base URL" => $request->getUrl(), "Scheme" => $request->getScheme(), "Port" => $request->getPort(), "Host" => $request->getHost()));
            });
            $whoops_editor = $app->config("whoops.editor");
            if ($whoops_editor !== null) {
                $app->whoopsPrettyPageHandler->setEditor($whoops_editor);
            }
            $app->container->singleton("whoops", function () use($app) {
                $run = new \Whoops\Run();
                $run->pushHandler($app->whoopsPrettyPageHandler);
                if (is_ajax()) {
                    $run->pushHandler($app->whoopsJsonResponseHandler);
                }
                $run->pushHandler($app->whoopsSlimInfoHandler);
                return $run;
            });
            $app->error(array($app->whoops, \Whoops\Run::EXCEPTION_HANDLER));
        }
        $this->next->call();
    }
}

?>