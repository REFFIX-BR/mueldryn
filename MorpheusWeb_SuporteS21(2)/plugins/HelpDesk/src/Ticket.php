<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace HelpDesk;

class Ticket
{
    private $_id = NULL;
    private $_department = NULL;
    private $_message = NULL;
    private $_subject = NULL;
    private $_created = NULL;
    private $_updated = NULL;
    private $_status = NULL;
    private $_account = NULL;
    private $_images = NULL;
    private $_messages = NULL;
    public function __construct($id = NULL, $account = NULL)
    {
        if ($id !== null) {
            $this->read($id, $account);
        }
    }
    public function setId($id)
    {
        $this->_id = $id;
        return $this;
    }
    public function getId()
    {
        return $this->_id;
    }
    public function setDepartment($department)
    {
        $this->_department = $department;
        return $this;
    }
    public function getDepartment()
    {
        return $this->_department;
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
    public function setSubject($subject)
    {
        $this->_subject = $subject;
        return $this;
    }
    public function getSubject()
    {
        return $this->_subject;
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
    public function setUpdated($updated)
    {
        if ($updated instanceof \DateTime) {
            $this->_updated = $updated;
        } else {
            $this->_updated = new \DateTime($updated);
        }
        return $this;
    }
    public function getUpdated()
    {
        return $this->_updated;
    }
    public function setStatus($status)
    {
        $this->_status = $status;
        return $this;
    }
    public function getStatus()
    {
        return $this->_status;
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
    public function getMessages()
    {
        if ($this->_messages === null) {
            $results = \Morpheus\Database\DriverManager::getConnection()->fetchAll("SELECT\n                    m.id,\n                    m.message,\n                    m.images,\n                    m.admin,\n                    m.account,\n                    m.create_date as created,\n                    CASE WHEN m.admin = 1 THEN u.name ELSE mi.memb_name COLLATE DATABASE_DEFAULT END as author\n                    FROM mw_helpdesk_ticket_messages m\n                    LEFT JOIN MEMB_INFO mi ON m.account = mi.memb___id\n                    LEFT JOIN mw_users u ON m.account = u.username\n                    WHERE m.ticket_id = ?\n                ", array($this->getId()), array("integer"));
            $messages = array();
            foreach ($results as $result) {
                $message = new TicketMessage();
                attach($message, $result);
                $messages[] = $message;
            }
            $this->_messages = $messages;
        }
        return $this->_messages;
    }
    public function hasMessages()
    {
        $messages = $this->getMessages();
        return !empty($messages);
    }
    public function exists()
    {
        return $this->getId() !== null;
    }
    public function read($id = NULL, $account = NULL)
    {
        $ticket = \Morpheus\Database\DriverManager::getConnection()->fetchAssoc("SELECT\n                t.id,\n                t.department,\n                t.create_date as created,\n                t.update_date as updated,\n                t.message,\n                t.status,\n                t.subject,\n                t.images,\n                t.account\n                FROM mw_helpdesk_tickets t\n                WHERE t.id = ?\n                " . ($account !== null ? "AND t.account = ?" : "") . "\n            ", array_merge(array($id), $account !== null ? array($account) : array()), array_merge(array("integer"), $account !== null ? array("string") : array()));
        if (!empty($ticket)) {
            attach($this, $ticket);
        }
    }
    public function insert()
    {
        \Morpheus\Database\DriverManager::getConnection()->insert("mw_helpdesk_tickets", array("department" => $this->getDepartment(), "subject" => strip_tags($this->getSubject()), "message" => strip_tags($this->getMessage()), "images" => $this->getImages(), "account" => user()->getUsername()), array("string", "string", "string", "string", "string"));
        $this->setId(\Morpheus\Database\DriverManager::getConnection()->lastInsertId());
    }
    public function update()
    {
        \Morpheus\Database\DriverManager::getConnection()->update("mw_helpdesk_tickets", array("status" => $this->getStatus(), "update_date" => $this->getUpdated()->format("Y-m-d H:i:s")), array("id" => $this->getId()), array("string", "string", "integer"));
    }
    public function delete()
    {
        \Morpheus\Database\DriverManager::getConnection()->delete("mw_helpdesk_tickets", array("id" => $this->getId()));
    }
}

?>