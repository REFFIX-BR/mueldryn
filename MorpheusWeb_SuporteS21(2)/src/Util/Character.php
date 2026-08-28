<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Character
{
    public static function status($online)
    {
        if ($online) {
            return "<span class=\"online\">Online</span>";
        }
        return "<span class=\"offline\">Offline</span>";
    }
    public static function getGroupedClass()
    {
        $classes = config("characters.classes", array());
        $grouped = array();
        $acum = array();
        foreach ($classes as $code => $class) {
            if ($code % 16 === 0 && !empty($acum)) {
                $grouped[] = $acum;
                $acum = array();
            }
            $acum[$code] = $class;
        }
        $grouped[] = $acum;
        return $grouped;
    }
    public static function className($class)
    {
        $classes = config("characters.classes", array());
        if (isset($classes[$class])) {
            return $classes[$class]["name"];
        }
        if (class_exists("\\Morpheus\\OpenMU\\Bridge")) {
            $mapped = \Morpheus\OpenMU\Bridge::mapOpenMuClassToClassic($class);
            if ($mapped != $class && isset($classes[$mapped])) {
                return $classes[$mapped]["name"];
            }
        }
        return "-";
    }
    public static function baseClass($class)
    {
        $classes = config("characters.classes", array());
        for ($initial = $class; $initial % 16 != 0; $initial--) {
        }
        return isset($classes[$initial]) ? $classes[$initial] : null;
    }
    public static function classNameByShotName($name)
    {
        $classes = config("characters.classes", array());
        foreach ($classes as $code => $class) {
            if ($class["short_name"] === strtoupper($name)) {
                return $class["name"];
            }
        }
        return "";
    }
    public static function classShortName($class)
    {
        $classes = config("characters.classes", array());
        if (isset($classes[$class])) {
            return $classes[$class]["short_name"];
        }
        if (class_exists("\\Morpheus\\OpenMU\\Bridge")) {
            $mapped = \Morpheus\OpenMU\Bridge::mapOpenMuClassToClassic($class);
            if ($mapped != $class && isset($classes[$mapped])) {
                return $classes[$mapped]["short_name"];
            }
        }
        return "-";
    }
    /**
     * Icon slug for class art (dw, bk, elf, dl, sum, rf).
     * Uses classic base-class groups (every 16 codes).
     */
    public static function classIconSlug($class)
    {
        $class = (int) $class;
        if (class_exists("\\Morpheus\\OpenMU\\Bridge")) {
            $mapped = \Morpheus\OpenMU\Bridge::mapOpenMuClassToClassic($class);
            if ($mapped != $class) {
                $class = (int) $mapped;
            }
        }
        $base = $class - $class % 16;
        $map = array(
            0 => "dw",
            16 => "bk",
            32 => "elf",
            48 => "bk",
            64 => "dl",
            80 => "sum",
            96 => "rf"
        );
        return isset($map[$base]) ? $map[$base] : "dw";
    }
    public static function classIconUrl($class)
    {
        return asset_to("/images/classes/" . static::classIconSlug($class) . ".png");
    }
    public static function mapName($map)
    {
        $maps = config("maps", array());
        return isset($maps[$map]) ? $maps[$map]["name"] : "-";
    }
    public static function getClassesByShortName()
    {
        $classes = array();
        foreach (config("characters.classes", array()) as $code => $class) {
            $classes[$class["short_name"]] = $code;
        }
        return $classes;
    }
    public static function isClass($class, $classes)
    {
        if (!is_array($classes)) {
            $classes = array($classes);
        }
        $found = false;
        $shortNames = static::getClassesByShortName();
        foreach ($classes as $c) {
            if (isset($shortNames[$c]) && $class == $shortNames[$c]) {
                $found = true;
                break;
            }
        }
        return $found;
    }
    public static function avatar($avatar, $default = "/images/no-avatar.png")
    {
        if (plugin_active("Avatar")) {
            return util("Avatar.Avatar")->url($avatar);
        }
        return asset_to($default);
    }
}

?>