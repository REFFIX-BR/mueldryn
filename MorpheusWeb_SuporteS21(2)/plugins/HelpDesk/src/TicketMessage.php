<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace HelpDesk;

class TicketMessage
{
    private $_id = NULL;
    private $_account = NULL;
    private $_author = NULL;
    private $_message = NULL;
    private $_created = NULL;
    private $_ticketId = NULL;
    private $_images = NULL;
    private $_admin = NULL;
    public function setId($id)
    {
        $this->_id = $id;
        return $this;
    }
    public function getId()
    {
        return $this->_id;
    }
    public function setAuthor($author)
    {
        $this->_author = $author;
        return $this;
    }
    public function getAuthor()
    {
        return $this->_author;
    }
    public function setAccount($account)
    {
        $this->_account = $account;
        return $this;
    }
    public function getAccount()
    {
        return $this->_account;
    }
    public function setMessage($message)
    {
        $this->_message = $message;
        return $this;
    }
    public function getMessage()
    {
        return $this->_message;
    }
    public function setImages($images)
    {
        $this->_images = $images;
        return $this;
    }
    public function getImages()
    {
        return $this->_images;
    }
    public function hasImages()
    {
        return !empty($this->_images);
    }
    public function setCreated($created)
    {
        if ($created instanceof \DateTime) {
            $this->_created = $created;
        } else {
            $this->_created = new \DateTime($created);
        }
        return $this;
    }
    public function getCreated()
    {
        return $this->_created;
    }
    public function setTicketId($ticketId)
    {
        $this->_ticketId = $ticketId;
        return $this;
    }
    public function getTicketId()
    {
        return $this->_ticketId;
    }
    public function setAdmin($admin)
    {
        $this->_admin = (bool) $admin;
        return $this;
    }
    public function isAdmin()
    {
        return $this->_admin;
    }
    public function exists()
    {
        return $this->getId() !== null;
    }
    public function toArray()
    {
        $message = array();
        if ($this->exists()) {
            $message = array("id" => $this->id(), "account" => $this->getAccount(), "author" => $this->getAuthor(), "message" => $this->getComment(), "created" => $this->getCreated()->format(\DateTime::ISO8601), "ticket_id" => $this->getTicketId(), "admin" => $this->isAdmin(), "images" => $this->getImages());
        }
        return $message;
    }
    public function insert()
    {
        \Morpheus\Database\DriverManager::getConnection()->insert("mw_helpdesk_ticket_messages", array("message" => strip_tags($this->getMessage()), "account" => $this->getAccount(), "ticket_id" => $this->getTicketId(), "admin" => $this->isAdmin(), "images" => $this->getImages()), array("string", "string", "integer", "boolean", "string"));
        $this->setId(\Morpheus\Database\DriverManager::getConnection()->lastInsertId());
    }
}

?>