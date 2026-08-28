<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace HelpDesk\Util;

class HelpDesk
{
    public static function ticketStatus($status)
    {
        switch ($status) {
            case 1:
                return "<span class=\"ticket-status-p\">Pendente</span>";
            case 2:
                return "<span class=\"ticket-status-a\">Em andamento</span>";
            case 3:
                return "<span class=\"ticket-status-c\">Concluído</span>";
            case 4:
                return "<span class=\"ticket-status-r\">Cancelado</span>";
        }
    }
}

?>