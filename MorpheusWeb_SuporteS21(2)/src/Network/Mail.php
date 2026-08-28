<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Network;

class Mail extends \PHPMailer
{
    public function __construct()
    {
        $this->isSMTP();
        $this->Host = config("smtp.host");
        $this->SMTPAuth = true;
        $this->Username = config("smtp.username");
        $this->Password = config("smtp.password");
        $this->Port = config("smtp.port");
        $this->CharSet = "UTF-8";
        if (config("smtp.secure")) {
            $this->SMTPSecure = "tls";
        }
        $from = config("smtp.from");
        if (is_array($from)) {
            foreach ($from as $key => $value) {
                $this->From = $value;
                $this->FromName = $key;
            }
        } else {
            if (strpos($from, "<") !== false) {
                $parts = explode($from, "<");
                $this->From = trim(str_replace(">", "", $parts[1]));
                $this->FromName = trim($parts[0]);
            } else {
                $this->From = $from;
                $this->FromName = $from;
            }
        }
        $this->isHTML(true);
        parent::__construct();
    }
    public function addAddress($email, $name = "")
    {
        parent::addAddress($email, $name);
        return $this;
    }
    public function setMessage($message)
    {
        $this->Body = $message;
        return $this;
    }
    public function getMessage()
    {
        return $this->Body;
    }
    public function setSubject($subject)
    {
        $this->Subject = $subject;
        return $this;
    }
    public function getSubject()
    {
        return $this->Subject;
    }
    public function send($message = NULL)
    {
        if ($message !== null) {
            $this->setMessage($message);
        }
        return parent::send();
    }
    public function setMessageFromTemplate($file, $data)
    {
        $smarty = new \Smarty();
        $vars = array("template_dir" => TEMPLATE_PATH . "mail/", "compile_dir" => CACHE_PATH . "smarty/", "cache_dir" => CACHE_PATH);
        foreach ($vars as $name => $value) {
            $smarty->{$name} = $value;
        }
        foreach ($data as $key => $value) {
            $smarty->assign($key, $value);
        }
        $smarty->assign("url", url_to("/", true));
        $smarty->assign("config", \Morpheus\Core\Config::get());
        $smarty->assign("content", $smarty->fetch($file . ".html"));
        $this->Body = $smarty->fetch("layout.html");
        return $this;
    }
    public function getError()
    {
        return $this->ErrorInfo;
    }
}

?>