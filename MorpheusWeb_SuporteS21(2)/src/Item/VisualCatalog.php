<?php

namespace Morpheus\Item;

/**
 * Catálogo de itens visuais/cosméticos (skins Mudream) para nome e ícone na web.
 */
class VisualCatalog
{
    /** @var array|null */
    private static $items = null;

    /** @var string|null */
    private static $sourcePath = null;

    public static function sourcePath()
    {
        if (self::$sourcePath === null) {
            $candidates = array(
                ROOT . DS . 'tmp' . DS . 'cache' . DS . 'visual-items.php',
                defined('CACHE_PATH') ? CACHE_PATH . 'visual-items.php' : null,
            );
            foreach ($candidates as $path) {
                if ($path && is_file($path)) {
                    self::$sourcePath = $path;
                    break;
                }
            }
        }
        return self::$sourcePath;
    }

    public static function load()
    {
        if (self::$items !== null) {
            return self::$items;
        }

        self::$items = array();
        $file = self::sourcePath();
        if ($file) {
            $data = include $file;
            if (is_array($data)) {
                self::$items = $data;
            }
        }

        if (empty(self::$items)) {
            self::$items = self::buildFromJson();
        }

        return self::$items;
    }

    public static function key($section, $index)
    {
        return ((int) $section) . '-' . ((int) $index);
    }

    public static function lookup($section, $index)
    {
        $items = self::load();
        $key = self::key($section, $index);
        return isset($items[$key]) ? $items[$key] : null;
    }

    public static function hasEntry($section, $index)
    {
        return self::lookup($section, $index) !== null;
    }

    public static function name($section, $index)
    {
        $row = self::lookup($section, $index);
        return ($row && !empty($row['name'])) ? $row['name'] : null;
    }

    public static function skin($section, $index)
    {
        $row = self::lookup($section, $index);
        return ($row && !empty($row['skin'])) ? $row['skin'] : null;
    }

    /**
     * @return array section => fallback gif filename
     */
    public static function sectionFallbacks()
    {
        return array(
            0 => '0-0.gif',
            2 => '2-0.gif',
            4 => '4-0.gif',
            5 => '5-0.gif',
            6 => '6-0.gif',
            7 => '7-0.gif',
            8 => '8-0.gif',
            9 => '9-0.gif',
            10 => '10-0.gif',
            11 => '11-0.gif',
            12 => '12-0.gif',
            13 => '13-0.gif',
            14 => '14-0.gif',
        );
    }

    public static function buildFromJson()
    {
        $map = array();
        $root = dirname(ROOT);
        $catalogFile = $root . DS . 'OpenMU' . DS . 'tools' . DS . 'mudream_cosmetic_catalog.json';
        $modelsFile = $root . DS . 'OpenMU' . DS . 'tools' . DS . 'mudream_cosmetic_models.json';

        $models = array();
        if (is_file($modelsFile)) {
            $json = file_get_contents($modelsFile);
            if (substr($json, 0, 3) === "\xEF\xBB\xBF") {
                $json = substr($json, 3);
            }
            $raw = json_decode($json, true);
            if (is_array($raw)) {
                foreach ($raw as $row) {
                    if (!isset($row['Group'], $row['Number'])) {
                        continue;
                    }
                    $models[self::key($row['Group'], $row['Number'])] = $row;
                }
            }
        }

        if (is_file($catalogFile)) {
            $json = file_get_contents($catalogFile);
            if (substr($json, 0, 3) === "\xEF\xBB\xBF") {
                $json = substr($json, 3);
            }
            $raw = json_decode($json, true);
            if (is_array($raw)) {
                foreach ($raw as $row) {
                    $isCosmetic = !empty($row['Cosmetic']) || (isset($row['Cosmetic']) && $row['Cosmetic']);
                    if (!$isCosmetic || !isset($row['Group'], $row['Number'], $row['Name'])) {
                        continue;
                    }
                    $key = self::key($row['Group'], $row['Number']);
                    $entry = array(
                        'name' => $row['Name'],
                        'section' => (int) $row['Group'],
                        'index' => (int) $row['Number'],
                        'cosmetic' => true,
                        'skin' => '',
                        'file' => '',
                    );
                    if (isset($models[$key])) {
                        if (!empty($models[$key]['Skin'])) {
                            $entry['skin'] = $models[$key]['Skin'];
                        }
                        if (!empty($models[$key]['File'])) {
                            $entry['file'] = $models[$key]['File'];
                        }
                    }
                    $map[$key] = $entry;
                }
            }
        }

        return $map;
    }

    public static function writeCache($items = null)
    {
        if ($items === null) {
            $items = self::buildFromJson();
        }
        $dir = ROOT . DS . 'tmp' . DS . 'cache';
        if (!is_dir($dir)) {
            @mkdir($dir, 0777, true);
        }
        $file = $dir . DS . 'visual-items.php';
        $export = "<?php\nreturn " . var_export($items, true) . ";\n";
        file_put_contents($file, $export, LOCK_EX);
        self::$items = $items;
        self::$sourcePath = $file;
        return $file;
    }
}
