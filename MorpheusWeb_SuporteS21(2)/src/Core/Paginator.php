<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Core;

class Paginator extends \JasonGrimes\Paginator
{
    public function toHtml()
    {
        if ($this->numPages <= 1) {
            return "";
        }
        $html = "<div class=\"paginator\"><ul class=\"pagination\">";
        if ($this->getPrevUrl()) {
            $html .= "<li><a data-pagination href=\"" . $this->getPrevUrl() . "\">&laquo; " . __($this->previousText) . "</a></li>";
        }
        foreach ($this->getPages() as $page) {
            if ($page["url"]) {
                $html .= "<li" . ($page["isCurrent"] ? " class=\"active\"" : "") . "><a data-pagination href=\"" . $page["url"] . "\">" . $page["num"] . "</a></li>";
            } else {
                $html .= "<li class=\"disabled\"><span>" . $page["num"] . "</span></li>";
            }
        }
        if ($this->getNextUrl()) {
            $html .= "<li><a data-pagination href=\"" . $this->getNextUrl() . "\">" . __($this->nextText) . " &raquo;</a></li>";
        }
        $html .= "</div></ul>";
        return $html;
    }
}

?>