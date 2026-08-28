<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

App::hook("admin.dashboard.tiles.render", function () use($pendings, $waitings) {
    View::display("HelpDesk.tiles", array("layout" => false, "pendings" => $pendings, "waitings" => $waitings));
});
Route::get("/helpdesk/tickets(/:type)", function ($type = "") {
    $where = "";
    if ($type === "pending") {
        $where = "1";
    } else {
        if ($type === "waiting") {
            $where = "2";
        } else {
            if ($type === "completed") {
                $where = "3";
            }
        }
    }
    $where = empty($where) ? "" : " WHERE status = " . $where;
    $tickets = Connection::fetchAll("SELECT ht.*,\n        (SELECT COUNT(1) FROM mw_helpdesk_ticket_messages WHERE ticket_id = ht.id) as total_messages\n        FROM mw_helpdesk_tickets ht " . $where . "\n        ORDER BY ht.create_date ASC");
    View::display("HelpDesk.index", array("tickets" => $tickets));
})->name("helpdesk");
Route::map("/helpdesk/config", function () {
    if (Request::isPost()) {
        try {
            $departments = Input::post("helpdesk_department");
            Morpheus\Core\Config::save(array("helpdesk.rules" => Input::post("helpdesk_rules"), "helpdesk.departments" => !empty($departments) ? array_filter($departments, function ($var) {
                return !empty($var);
            }) : array(), "helpdesk.max_char_message" => Input::post("helpdesk_max_char_message"), "helpdesk.timeout" => Input::post("helpdesk_timeout")));
            return success(__("The settings has been saved"), array("redirect" => "/admin/helpdesk/config"));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
    $config = array("rules" => config("helpdesk.rules"), "departments" => config("helpdesk.departments", array()), "max_char_message" => config("helpdesk.max_char_message", 400), "timeout" => config("helpdesk.timeout"));
    View::display("HelpDesk.config", array("config" => $config));
})->via("GET", "POST")->name("helpdesk.config");
Route::get("/helpdesk/tickets/view/:id", function ($id) {
    $ticket = new HelpDesk\Ticket($id);
    $status = array(1 => __("Pending"), 2 => __("Waiting"), 3 => __("Completed"), 4 => __("Canceled"));
    $type = "pending";
    switch ($ticket->getStatus()) {
        case 2:
            $type = "waiting";
            break;
        case 3:
            $type = "completed";
            break;
    }
    View::display("HelpDesk.view", array("status" => $status, "ticket" => $ticket, "type" => $type));
})->name("helpdesk.view");
Route::post("/helpdesk/tickets/add_message/:ticket", function ($ticketId) {
    try {
        $files = array();
        $error = false;
        $images = Input::file("images");
        if (!empty($images)) {
            $dir = ROOT . DS . "uploads" . DS . "helpdesk" . DS;
            foreach (rework_upload_files($_FILES["images"]) as $file) {
                $upload = new upload($file, config("locale", "pt_BR"));
                $upload->file_auto_rename = true;
                $upload->file_max_size = "2M";
                $upload->allowed = array("image/png", "image/jpeg", "image/gif", "image/webp", "application/pdf");
                $upload->process($dir);
                if ($upload->processed) {
                    $files[] = $upload->file_dst_name;
                    $upload->clean();
                } else {
                    $error = $upload->error;
                }
            }
        }
        if (!$error) {
            $m = Input::post("message");
            if (!empty($m)) {
                $message = new HelpDesk\TicketMessage();
                $message->setMessage(Input::post("message"))->setAdmin(true)->setAccount(admin("username"))->setImages(join(",", $files))->setTicketId($ticketId);
                $message->insert();
            }
        }
        $ticket = new HelpDesk\Ticket($ticketId);
        $ticket->setStatus(Input::post("status"))->setUpdated(new DateTime());
        $ticket->update();
        return success(__("Message has been added to the ticket #%s", array($ticketId)), array("partial" => "sidebar", "redirect" => "/admin/helpdesk/tickets/view/" . $ticketId));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
});

?>