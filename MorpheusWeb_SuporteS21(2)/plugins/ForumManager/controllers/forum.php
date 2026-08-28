<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

require PLUGIN_PATH . DS . "ForumManager" . DS . "functions.php";
App::hook("accounts.register", function ($account) {
    $cdata = config("conn." . config("forum.conn", "forum"));
    $conn = Morpheus\Database\DriverManager::getConnection(config("forum.conn", "forum"));
    if (config("forum.auto_register", false)) {
        switch (config("forum.news.type", "phpbb")) {
            case "mybb":
                $salt = random_str(8);
                $hash = md5(md5($salt) . md5($account->getPassword()));
                $conn->insert($cdata["table_prefix"] . "users", array("username" => $account->getUsername(), "password" => $hash, "salt" => $salt, "loginkey" => random_str(50), "email" => $account->getEmail(), "regdate" => time(), "lastactive" => time(), "lastvisit" => time(), "allownotices" => 1, "usergroup" => 2, "pmnotice" => 1, "pmnotify" => 1, "buddyrequestsauto" => 1, "threadmode" => "auto", "signature" => "", "buddylist" => "", "ignorelist" => "", "pmfolders" => "", "notepad" => "", "usernotes" => ""));
                break;
        }
    }
});
App::hook(array("home", "forum.news.latest"), function () {
    try {
        $conn = Morpheus\Database\DriverManager::getConnection(config("forum.conn", "forum"));
        $qb = $conn->createQueryBuilder();
        $base = config("forum.news.base_forum", "");
        switch (config("forum.news.type", "phpbb")) {
            case "vbulletin":
                $qb->select(array("threadid AS topic_id", "forumid AS forum_id", "title"))->from(config("conn.forum.table_prefix", "") . "thread")->setMaxResults(config("forum.news.limit", 10))->orderBy("threadid", "DESC");
                if (!empty($base)) {
                    $qb->where("forumid in (" . $base . ")");
                }
                break;
            case "ipb3":
                $qb->select(array("tid AS topic_id", "forum_id", "title"))->from(config("conn.forum.table_prefix", "") . "topics")->setMaxResults(config("forum.news.limit", 10))->orderBy("tid", "DESC");
                if (!empty($base)) {
                    $qb->where("forum_id in (" . $base . ")");
                }
                break;
            case "ipb":
                $qb->select(array("tid AS topic_id", "forum_id", "title"))->from(config("conn.forum.table_prefix", "") . "forums_topics")->setMaxResults(config("forum.news.limit", 10))->orderBy("tid", "DESC");
                if (!empty($base)) {
                    $qb->where("forum_id in (" . $base . ")");
                }
                break;
            case "phpbb":
                $qb->select(array("topic_id", "forum_id", "topic_title as title"))->from(config("conn.forum.table_prefix", "") . "topics")->setMaxResults(config("forum.news.limit", 10))->orderBy("topic_id", "DESC");
                if (!empty($base)) {
                    $qb->where("forum_id in (" . $base . ")");
                }
                break;
            case "mybb":
                $qb->select(array("tid as topic_id", "fid as forum_id", "subject as title", "dateline as created_at"))->from(config("conn.forum.table_prefix", "") . "threads")->setMaxResults(config("forum.news.limit", 10))->orderBy("tid", "DESC");
                if (!empty($base)) {
                    $qb->where("fid in (" . $base . ")");
                }
                break;
        }
        $topics = $qb->execute();
        View::display("ForumManager.home", array("layout" => false, "topics" => $topics));
    } catch (Exception $ex) {
        pe($ex);
    }
}, 10);

?>