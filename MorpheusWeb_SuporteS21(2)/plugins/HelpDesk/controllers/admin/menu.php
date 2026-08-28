<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

$pendings = Connection::fetchColumn("SELECT count(1) FROM mw_helpdesk_tickets WHERE status = 1");
$waitings = Connection::fetchColumn("SELECT count(1) FROM mw_helpdesk_tickets WHERE status = 2");
$completeds = Connection::fetchColumn("SELECT count(1) FROM mw_helpdesk_tickets WHERE status = 3");
Morpheus\Menu\Menu::getInstance("admin")->add(array("id" => "support", "title" => __("Helpdesk"), "icon" => "ticket"))->add(array("id" => "support.pending", "title" => __("Tickets pending") . ($pendings ? "<small class=\"label pull-right bg-red\">" . $pendings . "</small>" : ""), "url" => "/admin/helpdesk/tickets/pending", "route" => "support.helpdesk.tickets"))->add(array("id" => "support.waiting", "title" => __("Tickets waiting") . ($waitings ? "<small class=\"label pull-right bg-red\">" . $waitings . "</small>" : ""), "url" => "/admin/helpdesk/tickets/waiting", "route" => "support.helpdesk.tickets"))->add(array("id" => "support.completed", "title" => __("Tickets completed") . ($completeds ? "<small class=\"label pull-right bg-red\">" . $completeds . "</small>" : ""), "url" => "/admin/helpdesk/tickets/completed", "route" => "support.helpdesk.tickets"))->add(array("id" => "support.sep1", "title" => "-"))->add(array("id" => "support.config", "title" => __("Settings"), "url" => "/admin/helpdesk/config"));

?>