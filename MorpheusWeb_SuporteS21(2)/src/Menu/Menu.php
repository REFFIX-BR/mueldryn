<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Menu;

class Menu
{
    private static $_instance = array();
    private $_items = array();
    public static function getInstance($instance = "default")
    {
        if (!isset(static::$_instance[$instance])) {
            static::$_instance[$instance] = new self();
        }
        return static::$_instance[$instance];
    }
    public function add($item)
    {
        $path = explode(".", $item["id"]);
        $parent = null;
        if (1 < count($path)) {
            array_pop($path);
            $parent = join(".", $path);
        }
        $item["parent_id"] = $parent;
        if (!isset($item["sequence"])) {
            $item["sequence"] = 0;
        }
        $this->_items[$item["id"]] = $item;
        return $this;
    }
    public function getMenus()
    {
        return $this->_items;
    }
    public function getFormattedItems()
    {
        $items = \Morpheus\Util\Hash::sort($this->_items, "{s}.sequence", "asc");
        return \Morpheus\Util\Hash::nest($items);
    }
    public function build(array $items = NULL, $level = 0)
    {
        $items = $items === null ? $this->getFormattedItems() : $items;
        $user = admin();
        $access = explode(",", $user["access"]);
        $data = array();
        foreach ($items as $item) {
            $permission = $level == 0 || $user["super_user"] || in_array(isset($item["route"]) ? $item["route"] : $item["id"], $access);
            if (!$permission) {
                continue;
            }
            if (!empty($item["children"])) {
                $i = $this->build($item["children"], $level + 1);
                if (!empty($i)) {
                    $d = $item;
                    unset($d["children"]);
                    $d["children"] = $i;
                    $data[] = $d;
                }
            } else {
                $data[] = $item;
            }
        }
        return $data;
    }
    public function render(array $items = NULL, $level = 0)
    {
        $items = $items === null ? $this->build() : $items;
        $menu = "";
        $class = "";
        if ($level == 0) {
            $class = "sidebar-menu";
        } else {
            $class = "treeview-menu";
        }
        $menu .= "<ul class=\"" . $class . "\">";
        foreach ($items as $key => $item) {
            $hasChildren = !empty($item["children"]);
            $class = array();
            $hasChildren ? $class : null;
            $icon = isset($item["icon"]) ? "<i class=\"fa fa-" . $item["icon"] . "\"></i> " : "";
            $classes = !empty($class) ? static::attributes(array("class" => implode(" ", $class))) : null;
            if ($hasChildren) {
                $menu .= "<li" . $classes . " data-id=\"" . str_replace(".", "-", $item["id"]) . "\"><a data-ajax href=\"#\">" . $icon . "<span>" . $item["title"] . "</span> <i class=\"fa fa-angle-left pull-right\"></i></a>";
            } else {
                if ($item["title"] == "-") {
                    $menu .= "<li class=\"divider\"></li>";
                } else {
                    $menu .= "<li" . $classes . " data-id=\"" . str_replace(".", "-", $item["id"]) . "\"><a data-ajax href=\"" . url_to($item["url"]) . "\"><i class=\"fa fa-" . (isset($item["icon"]) ? $item["icon"] : "circle-o") . "\"></i> <span>" . $item["title"] . "</span></a>";
                }
            }
            $menu .= $hasChildren ? $this->render($item["children"], $level + 1) : null;
            $menu .= "</li>";
        }
        $menu .= "</ul>";
        return $menu;
    }
    protected static function attributes($attrs)
    {
        if (empty($attrs)) {
            return "";
        }
        if (is_string($attrs)) {
            return " " . $attrs;
        }
        $compiled = "";
        foreach ($attrs as $key => $val) {
            $compiled .= " " . $key . "=\"" . htmlspecialchars($val) . "\"";
        }
        return $compiled;
    }
}

?>