<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace News;

class Comment
{
    private $_id = NULL;
    private $_author = NULL;
    private $_comment = NULL;
    private $_created = NULL;
    private $_newsId = NULL;
    private $_ip = NULL;
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
        if ($author instanceof \Morpheus\Account\Account) {
            $this->_author = $author;
        } else {
            $this->_author = new \Morpheus\Account\Account($author);
        }
        return $this;
    }
    public function getAuthor()
    {
        return $this->_author;
    }
    public function setComment($comment)
    {
        $this->_comment = $comment;
        return $this;
    }
    public function getComment()
    {
        return $this->_comment;
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
    public function setNewsId($newsId)
    {
        $this->_newsId = $newsId;
        return $this;
    }
    public function getNewsId()
    {
        return $this->_newsId;
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
    public function setIp($ip)
    {
        $this->_ip = $ip;
        return $this;
    }
    public function getIp()
    {
        return $this->_ip;
    }
    public function exists()
    {
        return $this->getId() !== null;
    }
    public function toArray()
    {
        $comment = array();
        if ($this->exists()) {
            $comment = array("id" => $this->id(), "author" => $this->getAuthor()->toArray(), "comment" => $this->getComment(), "created" => $this->getCreated()->format(\DateTime::ISO8601), "news_id" => $this->getNewsId(), "ip" => $this->getIp());
        }
        return $comment;
    }
    public function insert()
    {
        \Morpheus\Database\DriverManager::getConnection()->insert("mw_news_comments", array("comment" => strip_tags($this->getComment()), "author" => $this->getAuthor()->getUsername(), "news_id" => $this->getNewsId(), "ip" => env("REMOTE_ADDR"), "admin" => $this->isAdmin()), array("string", "string", "integer", "string", "boolean"));
        $this->setId(\Morpheus\Database\DriverManager::getConnection()->lastInsertId());
    }
}

?>