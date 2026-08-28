<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/support/tickets", authenticate(), function () {
    $tickets = Connection::fetchAll("SELECT *\n        FROM mw_helpdesk_tickets\n        WHERE account = :account\n        ORDER BY id DESC\n    ", array("account" => user()->getUsername()));
    View::display("HelpDesk.index", array("tickets" => $tickets));
});
Route::map("/support/tickets/add", authenticate(), function () {
    if (Request::isPost()) {
        $maxCharMessage = config("helpdesk.max_char_message", 400);
        $validation = new Morpheus\Core\Validation(Input::post(), array("department" => array(Morpheus\Core\Validation::REQUIRED), "subject" => array(Morpheus\Core\Validation::REQUIRED), "message" => array(Morpheus\Core\Validation::REQUIRED, __("Field must contain a max %s characters", $maxCharMessage) => array("rule" => array("maxlength", $maxCharMessage)))));
        if ($validation->isValid()) {
            $last = Connection::fetchColumn("select create_date\n                from mw_helpdesk_tickets\n                where account = ?\n                order by id desc\n            ", array(user()->getUsername()));
            try {
                if (empty($last) || strtotime($last) + config("helpdesk.timeout", 0) < time()) {
                    $files = array();
                    $error = false;
                    $images = Input::file("images");
                    if (!empty($images)) {
                        $images = rework_upload_files($images);
                        foreach ($images as $image) {
                            $upload = upload($image, array("dir" => "uploads" . DS . "helpdesk" . DS, "allowed" => array("image/png", "image/jpeg", "image/gif", "image/webp", "application/pdf")));
                            if ($upload["success"]) {
                                $files[] = $upload["filename"];
                            } else {
                                $error = $upload["error"];
                            }
                        }
                    }
                    if (!$error) {
                        $ticket = new HelpDesk\Ticket();
                        $ticket->setDepartment(Input::post("department"))->setMessage(Input::post("message"))->setSubject(Input::post("subject"))->setImages(join(",", $files));
                        $ticket->insert();
                        return success(__("Ticket has been opened!"), array("redirect" => "/support/tickets/view/" . $ticket->getId()));
                    }
                    foreach ($files as $file) {
                        unlink(ROOT . DS . "uploads" . DS . "helpdesk" . DS . $file);
                    }
                    return error($error);
                } else {
                    return error(__("You can't open another ticket"));
                }
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("HelpDesk.add");
})->via("GET", "POST");
Route::get("/support/tickets/view/:id", authenticate(), function ($id) {
    $ticket = new HelpDesk\Ticket($id, user()->getUsername());
    if (!$ticket->exists()) {
        App::notFound();
    } else {
        View::display("HelpDesk.view", array("ticket" => $ticket));
    }
});
Route::post("/support/tickets/add_message/:ticket", authenticate(), function ($ticket) {
    $maxCharMessage = config("helpdesk.max_char_message", 400);
    $validation = new Morpheus\Core\Validation(Input::post(), array("message" => array(Morpheus\Core\Validation::REQUIRED, __("Field must contain a max %s characters", $maxCharMessage) => array("rule" => array("maxlength", $maxCharMessage)))));
    if ($validation->isValid()) {
        try {
            $files = array();
            $error = false;
            $images = Input::file("images");
            if (!empty($images)) {
                $images = rework_upload_files($images);
                foreach ($images as $image) {
                    $upload = upload($image, array("dir" => "uploads" . DS . "helpdesk" . DS, "allowed" => array("image/png", "image/jpeg", "image/gif", "image/webp", "application/pdf")));
                    if ($upload["success"]) {
                        $files[] = $upload["filename"];
                    } else {
                        $error = $upload["error"];
                    }
                }
            }
            if (!$error) {
                $message = new HelpDesk\TicketMessage();
                $message->setMessage(Input::post("message"))->setAdmin(false)->setAccount(user()->getUsername())->setTicketId($ticket)->setImages(join(",", $files));
                $message->insert();
                return success(__("Message has been added to the ticket #%s", array($ticket)), array("redirect" => "/support/tickets/view/" . $ticket));
            }
            return error($error);
        } catch (Exception $ex) {
            return error($ex);
        }
    } else {
        return error($validation->getErrors());
    }
});

?>