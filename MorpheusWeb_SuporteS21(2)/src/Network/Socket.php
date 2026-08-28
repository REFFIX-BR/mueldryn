<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Network;

class Socket
{
    private $_sock = NULL;
    public function connect($host = NULL, $port = NULL, $timeout = 2)
    {
        $this->_sock = socket_create(AF_INET, SOCK_STREAM, SOL_TCP);
        socket_set_option($this->_sock, SOL_SOCKET, SO_RCVTIMEO, array("sec" => $timeout, "usec" => 0));
        if (!($connect = socket_connect($this->_sock, $host, $port))) {
            throw new Socket\Exception("Unable to connect socket " . $host);
        }
        return $connect;
    }
    public function send($data, $length = false)
    {
        $length = $length ? $length : strlen($data);
        if (socket_write($this->_sock, $data, $length) == false) {
            throw new Socket\Exception("Failed to send packet");
        }
    }
    public function read($length = 2048, $type = PHP_BINARY_READ)
    {
        return socket_read($this->_sock, $length, $type);
    }
    public function close()
    {
        socket_close($this->_sock);
    }
    public function __destruct()
    {
        $this->close();
    }
}

?>