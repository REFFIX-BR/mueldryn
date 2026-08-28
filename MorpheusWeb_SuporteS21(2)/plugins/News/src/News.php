<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace News;

class News
{
    private $_id = NULL;
    private $_title = NULL;
    private $_slug = NULL;
    private $_userId = NULL;
    private $_type = NULL;
    private $_body = NULL;
    private $_created = NULL;
    private $_link = NULL;
    private $_views = NULL;
    private $_comments = NULL;
    private $_allowComments = NULL;
    private $_author = NULL;
    private $_imageCover = NULL;
    public function __construct($id = NULL)
    {
        if ($id !== null) {
            $this->read($id);
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
    public function setTitle($title)
    {
        $this->_title = $title;
        return $this;
    }
    public function getTitle()
    {
        return $this->_title;
    }
    public function setSlug($slug)
    {
        $this->_slug = $slug;
        return $this;
    }
    public function getSlug()
    {
        return $this->_slug;
    }
    public function setUserId($userId)
    {
        $this->_userId = $userId;
        return $this;
    }
    public function getUserId()
    {
        return $this->_userId;
    }
    public function setType($type)
    {
        $this->_type = $type;
        return $this;
    }
    public function getType()
    {
        return $this->_type;
    }
    public function setBody($body)
    {
        $this->_body = $body;
        return $this;
    }
    public function getBody()
    {
        return $this->_body;
    }
    public function setLink($link)
    {
        $this->_link = $link;
        return $this;
    }
    public function getLink()
    {
        return $this->_link;
    }
    public function setViews($views)
    {
        $this->_views = $views;
        return $this;
    }
    public function getViews()
    {
        return $this->_views;
    }
    public function setCreated($created = NULL)
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
    public function setAllowedComments($allowComments)
    {
        $this->_allowComments = (bool) $allowComments;
        return $this;
    }
    public function isAllowedComments()
    {
        return $this->_allowComments;
    }
    public function setImageCover($image)
    {
        $this->_imageCover = $image;
        return $this;
    }
    public function getImageCover()
    {
        return $this->_imageCover;
    }
    public function hasImageCover()
    {
        return !empty($this->_imageCover);
    }
    public function exists()
    {
        return $this->getId() !== null;
    }
    public function getAuthor()
    {
        if ($this->_author === null) {
            $this->_author = \Morpheus\Database\Connection::fetchColumn("select name from mw_users where id = ?", array($this->getUserId()), array("integer"));
        }
        return $this->_author;
    }
    public function getComments()
    {
        if ($this->_comments === null) {
            $results = \Morpheus\Database\Connection::fetchAll("select c.id,\n                    c.author,\n                    c.comment,\n                    c.ip,\n                    c.admin,\n                    c.news_id as newsId,\n                    c.create_date as created\n                    from mw_news_comments c\n                    where c.news_id = ?\n                ", array($this->getId()), array("integer"));
            $comments = array();
            foreach ($results as $result) {
                $comment = new Comment();
                attach($comment, $result);
                $comments[] = $comment;
            }
            $this->_comments = $comments;
        }
        return $this->_comments;
    }
    public function hasComments()
    {
        $comments = $this->getComments();
        return !empty($comments);
    }
    public function toArray()
    {
        $comments = array();
        foreach ($this->getComments() as $comment) {
            $comments[] = $comment->toArray();
        }
        $news = array();
        if ($this->exists()) {
            $news = array("id" => $this->getId(), "title" => $this->getTitle(), "slug" => $this->getSlug(), "author" => $this->getAuthor(), "type" => $this->getType(), "link" => $this->getLink(), "views" => $this->getViews(), "body" => $this->getBody(), "created" => $this->getCreated(), "allow_comments" => $this->isAllowedComments(), "image_cover" => $this->getImageCover(), "comments" => $comments);
        }
        return $news;
    }
    public function readBySlug($slug = NULL)
    {
        $news = \Morpheus\Database\Connection::fetchAssoc("select n.id,\n                n.user_id as userId,\n                n.create_date as created,\n                n.body,\n                n.title,\n                n.slug,\n                n.type,\n                n.views,\n                n.link,\n                u.name,\n                n.allow_comments as allowedComments,\n                n.image_cover as imageCover\n                from mw_news n\n                join mw_users u on u.id = n.user_id\n                where n.slug = ?\n            ", array($slug));
        if (!empty($news)) {
            attach($this, $news);
        }
    }
    public function read($id = NULL)
    {
        $news = \Morpheus\Database\Connection::fetchAssoc("select n.id,\n                n.user_id as userId,\n                n.create_date as created,\n                n.body,\n                n.title,\n                n.slug,\n                n.type,\n                n.views,\n                n.link,\n                u.name,\n                n.allow_comments as allowedComments,\n                n.image_cover as imageCover\n                from mw_news n\n                join mw_users u on u.id = n.user_id\n                where n.id = ?\n            ", array($id), array("integer"));
        if (!empty($news)) {
            attach($this, $news);
        }
    }
    public function incrementViews()
    {
        \Morpheus\Database\Connection::update("mw_news", array("views" => $this->getViews() + 1), array("id" => $this->getId()), array("integer"));
    }
    public function insert()
    {
        \Morpheus\Database\Connection::insert("mw_news", array("title" => $this->getTitle(), "slug" => \Morpheus\Util\Inflector::slug($this->getTitle(), "-"), "body" => $this->getBody(), "link" => $this->getLink(), "type" => $this->getType(), "user_id" => \Morpheus\Admin\Auth::getInstance()->getUser("id"), "allow_comments" => $this->isAllowedComments(), "image_cover" => $this->getImageCover()), array("string", "string", "string", "string", "integer", "integer", "boolean", "string"));
        $this->setId(\Morpheus\Database\Connection::lastInsertId());
    }
    public function update()
    {
        \Morpheus\Database\Connection::update("mw_news", array("title" => $this->getTitle(), "slug" => \Morpheus\Util\Inflector::slug($this->getTitle(), "-"), "body" => $this->getBody(), "link" => $this->getLink(), "type" => $this->getType(), "allow_comments" => $this->isAllowedComments(), "image_cover" => $this->getImageCover()), array("id" => $this->getId()), array("string", "string", "string", "string", "integer", "boolean", "integer", "string"));
    }
    public function delete()
    {
        \Morpheus\Database\Connection::delete("mw_news", array("id" => $this->getId()));
    }
}

?>