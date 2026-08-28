<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Item
{
    public static function getExcellentsType($section, $index)
    {
        $item = \Morpheus\Item\Item::getStorage()->getItem($section, $index);
        switch ($item["slot"]) {
            case -1:
            case "*":
                $type = -1;
                break;
            case 0:
            case 1:
                if ($section == 6) {
                    $type = 2;
                } else {
                    $type = 1;
                }
                break;
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
                $type = 2;
                break;
            case 7:
                if ($section == 12 && in_array($index, array(36, 37, 38, 39, 40, 43, 50, 267))) {
                    $type = 6;
                } else {
                    if ($section == 12 && in_array($index, array(266))) {
                        $type = 7;
                    } else {
                        if ($section == 12 && in_array($index, array(262, 263, 264, 265))) {
                            $type = 8;
                        } else {
                            $type = 3;
                        }
                    }
                }
                break;
            case 8:
                $type = -1;
                break;
            case 9:
                $type = 1;
                break;
            case 10:
            case 11:
                $type = 5;
                break;
            default:
                $type = -1;
                break;
        }
        return $type;
    }
    public static function getExcellents($section, $index)
    {
        $type = static::getExcellentsType($section, $index);
        switch ($type) {
            case -1:
                $options = array();
                break;
            case 1:
                $options = array("Increases recovery rate of mana (Mana / 8)", "Increases recovery rate of life (Life / 8)", "Increase the speed of magical damage +7", "Increase magical damage +1%", "Increase magic damage + level/20", "Success in excellent damage +10%");
                break;
            case 2:
                $options = array("Increased rate of acquisition of Zen +40%", "Success of Defense +10%", "Mirrors in 5% Damage Received", "Lowers the damage by +4%", "Increase mana +4%", "Increases in life +4%");
                break;
            case 3:
                $options = array("Life increased by +125", "Mana increased by +125", "Defense of the opponent ignored by 3%", "Stamina increased by +50", "Increase the speed of magic damage to +5");
                break;
            case 4:
                $options = array("+Damage", "+Defense", "+Illusion");
                break;
            case 5:
                $options = array("Increases zens who fall into +40%", "+10% Defensive success rank", "Returns the blow +5%", "Received low blow +4%", "Increase mana +4%", "Increases in life +4%");
                break;
            case 6:
                $options = array("Increase mana after killing monster + mana / 8", "Increases life after killing monster + life / 8", "Increases attack speed +7", "Adds +2% damage", "+ Increases damage level/20", "+10% Defensive success rank");
                break;
            case 7:
                $options = array("Chance of Double Damage +4%", "Chance of Damage From Breaking Enemy's Defense +4%", "Defense +4%", "Complete recovery of life in 4% rate");
                break;
            case 8:
                $options = array("Chance of Damage From Breaking Enemy's Defense +3%", "Chance of Fully Recovering Life +5%");
                break;
        }
        return $options;
    }
    public static function getExcellentOptions($section, $index)
    {
        if (config("item.use_option_file", false)) {
            $excellents = array();
            $options = static::getOptions($section, $index);
            foreach ($options as $option) {
                if ($option["skill"] === "*" && $option["luck"] === "*" && $option["option"] === "*") {
                    $excellents[] = sprintf($option["name"], $option["value"]);
                }
            }
            if (empty($excellents)) {
                return static::getExcellents($section, $index);
            }
            return $excellents;
        } else {
            return static::getExcellents($section, $index);
        }
    }
    public static function getAncients($section, $index)
    {
        if (in_array($section, array(6, 7, 8, 9, 10, 11, 13)) || $section == 13 && in_array($index, array(8, 9, 21, 22, 23))) {
            return array("name" => "rings", "options" => array(1 => "Add stamina +5", 2 => "Add stamina +10"));
        }
        if (in_array($section, array(0, 1, 2, 3, 4, 5)) || $section == 13 && in_array($index, array(12, 13, 25, 26, 27))) {
            return array("name" => "weapons", "options" => array(1 => "Add strength +5", 2 => "Add strength +10"));
        }
        return array("name" => "-", "options" => array());
    }
    public static function getAvailableAncients($section, $index)
    {
        $ancients = array();
        if (!static::isAncient($section, $index)) {
            return $ancients;
        }
        if (config("server.team", "gmo") == "igcn") {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_ItemSetType.xml";
            $file2 = ROOT . DS . "resources" . DS . "files" . DS . "IGC_ItemSetOption.xml";
            $xml = new \DOMDocument();
            $xml->load($file);
            foreach ($xml->getElementsByTagName("Section") as $section) {
                foreach ($section->getElementsByTagName("Item") as $i) {
                    if ($section->getAttribute("Index") == $index) {
                        $xml2 = new \DOMDocument();
                        $xml2->load($file2);
                        foreach ($xml2->getElementsByTagName("SetItem") as $si) {
                            if (in_array($si->getAttribute("Index"), array($i->getAttribute("TierI"), $i->getAttribute("TierII")))) {
                                $ix = $si->getAttribute("Index") == $i->getAttribute("TierI") ? 1 : 2;
                                $ancients[$ix] = $si->getAttribute("Name");
                            }
                        }
                        break;
                    }
                }
            }
        } else {
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SetItemType.txt", "r"))) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            while (!feof($file)) {
                $types = fscanf($file, "%d %d %d %d %d");
                if (isset($types[0]) && !strpos($types[0], "//") && $section == $types[0] && $index == $types[1]) {
                    if (!($file2 = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SetItemOption.txt", "r"))) {
                        throw new \Exception("Was not possible to open the file, verify that the file has permissions");
                    }
                    while (!feof($file2)) {
                        $infos = fscanf($file2, "%d \"%[^\"]\" %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d");
                        if (isset($infos[0]) && !strpos($infos[0], "//") && in_array($infos[0], array($types[3], $types[4]))) {
                            $i = $infos[0] == $types[3] ? 1 : 2;
                            $ancients[$i] = $infos[1];
                        }
                    }
                    break;
                }
            }
            fclose($file);
            if (isset($file2)) {
                fclose($file2);
            }
        }
        return $ancients;
    }
    public static function getSocketName($item, $index)
    {
        $sockets = static::getSockets($item->getSection());
        $name = "";
        foreach ($sockets as $type => $socks) {
            foreach ($socks as $socket) {
                if ($item->getSocket($index) == $socket["value"]) {
                    $name = $socket["name"];
                    break 2;
                }
            }
        }
        return $name;
    }
    public static function getSockets($section, $max = 5)
    {
        $data = array();
        if ($section <= 5) {
            $allow = array(1, 3, 5);
        } else {
            $allow = array(2, 4, 6);
        }
        if (config("server.team", "gmo") == "igcn") {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_SocketOption.xml";
            $xml = new \DOMDocument();
            $xml->load($file);
            foreach ($xml->getElementsByTagName("SocketItemOptionSettings") as $setting) {
                foreach ($setting->getElementsByTagName("Option") as $option) {
                    for ($i = 0; $i < $max; $i++) {
                        $idx = $option->getAttribute("Index");
                        $value = $idx + $i * 50;
                        $bonus = $option->getAttribute("BonusValue" . ($i + 1));
                        $element = $option->getAttribute("ElementType");
                        if (in_array($element, $allow)) {
                            $type = "-";
                            switch ($element) {
                                case 1:
                                    $type = "Fire";
                                    break;
                                case 2:
                                    $type = "Water";
                                    break;
                                case 3:
                                    $type = "Ice";
                                    break;
                                case 4:
                                    $type = "Wind";
                                    break;
                                case 5:
                                    $type = "Lightning";
                                    break;
                                case 6:
                                    $type = "Earth";
                                    break;
                            }
                            $complement = "";
                            if (in_array($idx, array(0, 5, 10, 12, 13, 14, 16, 17, 20, 22, 23, 30, 32))) {
                                $complement = "%";
                            }
                            if (!isset($data[$type])) {
                                $data[$type] = array();
                            }
                            $data[$type][] = array("type" => $type, "name" => $option->getAttribute("Name") . " +" . $bonus . $complement, "value" => $value);
                        }
                    }
                }
            }
        } else {
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SocketItemOption.txt", "rb+"))) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            $category = -1;
            while (!feof($file)) {
                $line = fgets($file);
                $line = trim($line, " \t\r\n");
                if (substr($line, 0, 2) == "//" || substr($line, 0, 2) == "#" || $line == "") {
                    continue;
                }
                if (($pos = strpos($line, "//")) !== false) {
                    $line = substr($line, 0, $pos);
                }
                $line = trim($line, " \t\r\n");
                if ($category == -1) {
                    if (is_numeric($line)) {
                        $category = $line;
                    }
                } else {
                    if (strtolower($line) == "end") {
                        break;
                    }
                    $columns = preg_split("/[\\s,]*\\\"([^\\\"]+)\\\"[\\s,]*|[\\s,]*'([^']+)'[\\s,]*|[\\s,]+/", $line, 0, PREG_SPLIT_NO_EMPTY | PREG_SPLIT_DELIM_CAPTURE);
                    for ($i = 0; $i < $max; $i++) {
                        $value = $columns[0] + $i * 50;
                        $initial = 4;
                        if (9 < count($columns)) {
                            $initial = 5;
                        }
                        $option = $columns[$i + $initial];
                        if (in_array($columns[1], $allow)) {
                            $type = "-";
                            switch ($columns[1]) {
                                case 1:
                                    $type = "Fire";
                                    break;
                                case 2:
                                    $type = "Water";
                                    break;
                                case 3:
                                    $type = "Ice";
                                    break;
                                case 4:
                                    $type = "Wind";
                                    break;
                                case 5:
                                    $type = "Lightning";
                                    break;
                                case 6:
                                    $type = "Earth";
                                    break;
                            }
                            $complement = "";
                            if (in_array($columns[0], array(0, 5, 10, 12, 13, 14, 16, 17, 20, 22, 23, 30, 32))) {
                                $complement = "%";
                            }
                            if (!isset($data[$type])) {
                                $data[$type] = array();
                            }
                            $data[$type][] = array("type" => $type, "name" => $columns[3] . " +" . $option . $complement, "value" => $value);
                        }
                    }
                }
            }
        }
        return $data;
    }
    public static function hasItemImage(\Morpheus\Item\Item $item)
    {
        $path = ROOT . DS . "resources" . DS . "images" . DS . "items" . DS;
        $key = $item->getSection() . "-" . $item->getIndex();
        if (file_exists($path . $key . "-" . $item->getLevel() . ".gif")) {
            return true;
        }
        return file_exists($path . $key . ".gif");
    }

    public static function hasCustomVisualImage($section, $index)
    {
        $path = ROOT . DS . "resources" . DS . "images" . DS . "items" . DS . "visual" . DS;
        $key = ((int) $section) . "-" . ((int) $index);
        return is_file($path . $key . ".png") || is_file($path . $key . ".gif") || is_file($path . $key . ".jpg");
    }

    public static function hasDisplayImage(\Morpheus\Item\Item $item)
    {
        if (static::hasItemImage($item)) {
            return true;
        }
        if (static::hasCustomVisualImage($item->getSection(), $item->getIndex())) {
            return true;
        }
        if (class_exists("\\Morpheus\\Item\\VisualCatalog") && \Morpheus\Item\VisualCatalog::hasEntry($item->getSection(), $item->getIndex())) {
            return true;
        }
        if (class_exists("\\Morpheus\\OpenMU\\InventorySync") && \Morpheus\OpenMU\InventorySync::lookupName($item->getSection(), $item->getIndex())) {
            return true;
        }
        if (class_exists("\\Morpheus\\OpenMU\\Bridge") && \Morpheus\OpenMU\Bridge::enabled()) {
            if (\Morpheus\OpenMU\Bridge::itemDefinitionName($item->getSection(), $item->getIndex()) !== "") {
                return true;
            }
        }
        return false;
    }

    /**
     * Ícone oficial Mudream CDN (render 3D igual ao site mudream.online).
     */
    public static function mudreamIconUrl($section, $index)
    {
        $cfg = config('mudream_icons', array());
        if (empty($cfg['enabled'])) {
            return null;
        }

        $section = (int) $section;
        $index = (int) $index;
        $minIndex = isset($cfg['min_index']) ? (int) $cfg['min_index'] : 300;
        $isCosmetic = $index >= $minIndex;
        if (!$isCosmetic && class_exists("\\Morpheus\\Item\\VisualCatalog")) {
            $isCosmetic = \Morpheus\Item\VisualCatalog::hasEntry($section, $index);
        }
        if (!$isCosmetic) {
            return null;
        }

        $season = !empty($cfg['season']) ? $cfg['season'] : '6plus';
        $base = !empty($cfg['cdn_base'])
            ? rtrim($cfg['cdn_base'], '/')
            : 'https://dreamassets.fra1.cdn.digitaloceanspaces.com/items_seasons';

        return $base . '/' . rawurlencode($season) . '/' . $section . '/' . $index . '.webp';
    }

    /**
     * PNG local do OpenMU ItemEditor (vanilla).
     */
    public static function openMuEditorIconPath($section, $index, $level = 0)
    {
        $root = config('openmu_item_editor');
        if (!$root || !is_dir($root)) {
            return null;
        }
        $section = (int) $section;
        $index = (int) $index;
        $level = max(0, min(15, (int) $level));
        $candidates = array(
            'item_' . $section . '_' . $index . '_' . $level . '.png',
            'item_' . $section . '_' . $index . '_0.png',
            'item_' . $section . '_' . $index . '_' . $level . '_e.png',
            'item_' . $section . '_' . $index . '_0_e.png',
        );
        foreach ($candidates as $file) {
            $path = rtrim($root, '/\\') . DIRECTORY_SEPARATOR . $file;
            if (is_file($path)) {
                return $path;
            }
        }
        return null;
    }

    /**
     * URL do ícone gerado (cosméticos / itens sem .gif no pack web).
     */
    public static function visualIconUrl($section, $index, $square = 48, $openMuName = null)
    {
        $section = (int) $section;
        $index = (int) $index;
        $square = max(32, min(128, (int) $square));
        $url = "/visual-item-icon.php?section=" . $section . "&index=" . $index . "&size=" . $square;

        $name = ($openMuName !== null) ? trim((string) $openMuName) : "";
        if ($name === "" && class_exists("\\Morpheus\\OpenMU\\Bridge") && \Morpheus\OpenMU\Bridge::enabled()) {
            $name = \Morpheus\OpenMU\Bridge::itemDefinitionName($section, $index);
        }
        if ($name !== "") {
            $url .= "&name=" . rawurlencode($name);
        }

        return resource_to($url);
    }

    public static function getDisplayName(\Morpheus\Item\Item $item, $withLevel = true)
    {
        $section = (int) $item->getSection();
        $index = (int) $item->getIndex();
        $level = (int) $item->getLevel();
        $multiples = config("item.multiple_names", array());

        if (isset($multiples[$section][$index][$level])) {
            return $multiples[$section][$index][$level];
        }

        $name = trim((string) $item->getName());
        if ($name === "" || strcasecmp($name, "Unknown Item") === 0) {
            $name = "";
        }

        if ($name === "" && class_exists("\\Morpheus\\OpenMU\\Bridge") && \Morpheus\OpenMU\Bridge::enabled()) {
            $muName = \Morpheus\OpenMU\Bridge::itemDefinitionName($section, $index);
            if ($muName !== "") {
                $name = $muName;
            }
        }

        if ($name === "" && class_exists("\\Morpheus\\OpenMU\\InventorySync")) {
            $openMuName = \Morpheus\OpenMU\InventorySync::lookupName($section, $index);
            if ($openMuName) {
                $name = $openMuName;
            }
        }

        if ($name === "" && class_exists("\\Morpheus\\Item\\VisualCatalog")) {
            $catalogName = \Morpheus\Item\VisualCatalog::name($section, $index);
            if ($catalogName) {
                $name = $catalogName;
            }
        }

        if ($name === "") {
            try {
                $stored = static::name($section, $index);
                if ($stored !== "") {
                    $name = $stored;
                }
            } catch (\Exception $ex) {
            }
        }

        if ($name === "") {
            $name = "Item " . $section . "-" . $index;
        }

        if ($withLevel && $level > 0 && !isset($multiples[$section][$index][$level])) {
            $name .= " +" . $level;
        }

        return $name;
    }

    public static function getDisplayImage(\Morpheus\Item\Item $item, $square = 32, $openMuName = null)
    {
        if (static::hasItemImage($item)) {
            return static::getImage($item);
        }

        $section = (int) $item->getSection();
        $index = (int) $item->getIndex();
        $key = $section . "-" . $index;

        $mudreamUrl = static::mudreamIconUrl($section, $index);
        if ($mudreamUrl !== null) {
            return $mudreamUrl;
        }

        $visualDir = ROOT . DS . "resources" . DS . "images" . DS . "items" . DS . "visual" . DS;
        foreach (array('.png', '.gif', '.jpg') as $ext) {
            if (is_file($visualDir . $key . $ext)) {
                return resource_to("/images/items/visual/" . $key . $ext);
            }
        }

        if (class_exists("\\Morpheus\\Item\\VisualCatalog") && \Morpheus\Item\VisualCatalog::hasEntry($section, $index)) {
            return static::visualIconUrl($section, $index, $square, $openMuName);
        }

        if (class_exists("\\Morpheus\\OpenMU\\InventorySync") && \Morpheus\OpenMU\InventorySync::lookupName($section, $index)) {
            return static::visualIconUrl($section, $index, $square, $openMuName);
        }

        if (class_exists("\\Morpheus\\OpenMU\\Bridge") && \Morpheus\OpenMU\Bridge::enabled()) {
            $muName = ($openMuName !== null && trim((string) $openMuName) !== "")
                ? trim((string) $openMuName)
                : \Morpheus\OpenMU\Bridge::itemDefinitionName($section, $index);
            if ($muName !== "") {
                return static::visualIconUrl($section, $index, $square, $muName);
            }
        }

        $fallbacks = class_exists("\\Morpheus\\Item\\VisualCatalog")
            ? \Morpheus\Item\VisualCatalog::sectionFallbacks()
            : array();
        if (isset($fallbacks[$section])) {
            $fallback = ROOT . DS . "resources" . DS . "images" . DS . "items" . DS . $fallbacks[$section];
            if (is_file($fallback)) {
                return resource_to("/images/items/" . $fallbacks[$section]);
            }
        }

        return static::visualIconUrl($section, $index, $square, $openMuName);
    }
    public static function getImage(\Morpheus\Item\Item $item)
    {
        $path = ROOT . DS . "resources" . DS . "images" . DS . "items" . DS;
        $key = $item->getSection() . "-" . $item->getIndex();
        if (file_exists($path . $key . "-" . $item->getLevel() . ".gif")) {
            $key = $key . "-" . $item->getLevel();
        }
        if (!file_exists($path . $key . ".gif")) {
            return resource_to("/item-image.php?section=" . (int) $item->getSection() . "&index=" . (int) $item->getIndex() . "&width=" . $item->getWidth() * 32 . "&height=" . $item->getHeight() * 32 . "&square=32");
        }
        return resource_to("/images/items/" . $key . ".gif");
    }
    public static function getFullName(\Morpheus\Item\Item $item, $span = true)
    {
        $isExcellent = false;
        if ($item->isAncient()) {
            $class[] = "ancient";
        } else {
            if ($item->isExcellent() && $item->getSection() < 12) {
                $isExcellent = true;
                if ($item->hasSocket()) {
                    $class[] = "excellent-socket";
                } else {
                    $class[] = "excellent";
                }
            } else {
                $class[] = "normal";
                if (6 < $item->getLevel()) {
                    $class[] = "high-level";
                } else {
                    if ($item->hasLuck() || 0 < $item->getOption()) {
                    }
                }
            }
        }
        $name = $item->getName() . (0 < $item->getLevel() ? " +" . $item->getLevel() : "");
        $multiples = config("item.multiple_names", array());
        if (isset($multiples[$item->getSection()][$item->getIndex()][$item->getLevel()])) {
            $name = $multiples[$item->getSection()][$item->getIndex()][$item->getLevel()];
        }
        return ($span ? "<span class=\"" . join(" ", $class) . "\">" : "") . ($isExcellent ? "Excellent " : "") . (in_array("ancient", $class) ? $item->getAncient()->getName() . " " : "") . $name . ($span ? "</span>" : "");
    }
    public static function getAncientName(\Morpheus\Item\Item $item)
    {
        if (!$item->isAncient()) {
            $name = $item->getName();
        } else {
            $name = $item->getName();
            if (config("server.team", "gmo") == "igcn") {
                $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_ItemSetType.xml";
                $file2 = ROOT . DS . "resources" . DS . "files" . DS . "IGC_ItemSetOption.xml";
                $xml = new \DOMDocument();
                $xml->load($file);
                foreach ($xml->getElementsByTagName("Section") as $section) {
                    foreach ($section->getElementsByTagName("Item") as $i) {
                        if ($section->getAttribute("Index") == $item->getSection() && $i->getAttribute("Index") == $item->getIndex()) {
                            $option = $item->getAncient() === 1 ? $i->getAttribute("TierI") : $i->getAttribute("TierII");
                            $xml2 = new \DOMDocument();
                            $xml2->load($file2);
                            foreach ($xml2->getElementsByTagName("SetItem") as $si) {
                                if ($option == $si->getAttribute("Index")) {
                                    $name = $si->getAttribute("Name") . " " . $item->getName();
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            } else {
                if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SetItemType.txt", "r"))) {
                    throw new \Exception("Was not possible to open the file, verify that the file has permissions");
                }
                while (!feof($file)) {
                    $types = fscanf($file, "%d %d %d %d %d");
                    if (isset($types[0]) && !strpos($types[0], "//") && $item->getSection() == $types[0] && $item->getIndex() == $types[1]) {
                        $option = $item->getAncient() === 1 ? $types[3] : $types[4];
                        if (!($file2 = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SetItemOption.txt", "r"))) {
                            throw new \Exception("Was not possible to open the file, verify that the file has permissions");
                        }
                        while (!feof($file2)) {
                            $infos = fscanf($file2, "%d \"%[^\"]\" %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d");
                            if (isset($infos[0]) && !strpos($infos[0], "//") && isset($option) && $option == $infos[0]) {
                                $name = $infos[1] . " " . $item->getName();
                                break;
                            }
                        }
                        break;
                    }
                }
                fclose($file);
                if (isset($file2)) {
                    fclose($file2);
                }
            }
            return $name;
        }
    }
    public static function hasHarmony($section)
    {
        return $section <= 11;
    }
    public static function hasAncient($section, $index)
    {
        if (config("server.team", "gmo") == "igcn") {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_ItemSetType.xml";
            $xml = new \DOMDocument();
            $xml->load($file);
            $ancient = false;
            foreach ($xml->getElementsByTagName("Section") as $sec) {
                foreach ($sec->getElementsByTagName("Item") as $item) {
                    if ($sec->getAttribute("Index") == $section && $item->getAttribute("Index") == $index) {
                        $ancient = true;
                    }
                }
            }
            return $ancient;
        } else {
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SetItemType.txt", "r"))) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            $ancient = false;
            while (!feof($file)) {
                $types = fscanf($file, "%d %d %d %d %d");
                if (isset($types[0]) && !strpos($types[0], "//") && $section == $types[0] && $index == $types[1]) {
                    $ancient = true;
                }
            }
            fclose($file);
            return $ancient;
        }
    }
    public static function isAncient($section, $index)
    {
        return static::hasAncient($section, $index);
    }
    public static function getSkillName(\Morpheus\Item\Item $item)
    {
        $name = "";
        if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "Skill.txt", "r"))) {
            throw new \Exception("Was not possible to open the file, verify that the file has permissions");
        }
        $skill = 0;
        try {
            $skill = \Morpheus\Item\Item::getStorage()->getItem($item->getSection(), $item->getIndex())["skill"];
        } catch (\Exception $ex) {
        }
        while (!feof($file)) {
            $info = fscanf($file, "%d \"%[^\"]\" %d %d %d");
            if (!strpos($info[0], "//") && isset($info[0]) && $skill == $info[0]) {
                $name = $info[1] . (0 < $info[3] ? " (Mana:" . $info[3] . ")" : "");
                break;
            }
        }
        fclose($file);
        return $name;
    }
    public static function hasSkill($section, $index)
    {
        try {
            $item = \Morpheus\Item\Item::getStorage()->getItem($section, $index);
            return 0 < $item["skill"];
        } catch (\Exception $ex) {
            return false;
        }
    }
    public static function hasRefine($section, $index)
    {
        if (config("server.team", "gmo") == "igcn") {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_Item380Option.xml";
            $xml = new \DOMDocument();
            $xml->load($file);
            $refine = false;
            foreach ($xml->getElementsByTagName("Item") as $item) {
                if ($item->getAttribute("Cat") == $section && $item->getAttribute("Index") == $index) {
                    $refine = true;
                }
            }
            return $refine;
        } else {
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "380ItemType.txt", "r"))) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            $refine = false;
            while (!feof($file)) {
                $types = fscanf($file, "%d %d %d %d");
                if (isset($types[0]) && !strpos($types[0], "//") && $section == $types[0] && $index == $types[1]) {
                    $refine = true;
                }
            }
            fclose($file);
            return $refine;
        }
    }
    public static function getRefineName(\Morpheus\Item\Item $item, $slot = 1)
    {
        if (config("server.team", "gmo") == "igcn") {
            $file = ROOT . DS . "resources" . DS . "files" . DS . "IGC_Item380Option.xml";
            $xml = new \DOMDocument();
            $xml->load($file);
            $name = "";
            foreach ($xml->getElementsByTagName("Item") as $item) {
                if ($item->getAttribute("Cat") == $item->getSection() && $item->getAttribute("Index") == $item->getIndex()) {
                    $name = "null";
                    break;
                }
            }
            return $name;
        } else {
            if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "380ItemType.txt", "r"))) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            if (!($file2 = fopen(ROOT . DS . "resources" . DS . "files" . DS . "380ItemOption.txt", "r"))) {
                throw new \Exception("Was not possible to open the file, verify that the file has permissions");
            }
            $name = "";
            while (!feof($file)) {
                $types = fscanf($file, "%d %d %d %d");
                if (isset($types[0]) && !strpos($types[0], "//") && $item->getSection() == $types[0] && $item->getIndex() == $types[1]) {
                    while (!feof($file2)) {
                        $options = fscanf($file2, "%d \"%[^\"]\" %d");
                        if ($types[$slot + 1] == $options[0]) {
                            $name = $options[1] . " +" . $options[2];
                            break 2;
                        }
                    }
                }
            }
            fclose($file);
            return $name;
        }
    }
    public static function hasSockets($section, $index)
    {
        $sockets = false;
        if (config("server.team", "gmo") === "igcn") {
            $default = \Morpheus\Item\Item::getStorage()->getItem($section, $index);
            return in_array($default["type"], array(2, 4));
        }
        if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SocketItemType.txt", "r"))) {
            throw new \Exception("Was not possible to open the file, verify that the file has permissions");
        }
        while (!feof($file)) {
            $types = fscanf($file, "%d %d %d");
            if (isset($types[0]) && !strpos($types[0], "//") && $section == $types[0] && $index == $types[1]) {
                $sockets = true;
            }
        }
        fclose($file);
        return $sockets;
    }
    public static function getMaxSockets($section, $index)
    {
        $max = 0;
        if (!($file = fopen(ROOT . DS . "resources" . DS . "files" . DS . "SocketItemType.txt", "r"))) {
            throw new \Exception("Was not possible to open the file, verify that the file has permissions");
        }
        while (!feof($file)) {
            $types = fscanf($file, "%d %d %d");
            if (isset($types[0]) && !strpos($types[0], "//") && $section == $types[0] && $index == $types[1]) {
                $max = $types[2];
            }
        }
        fclose($file);
        return $max;
    }
    public static function image($section, $index)
    {
        $section = (int) $section;
        $index = (int) $index;
        $path = ROOT . DS . "resources" . DS . "images" . DS . "items" . DS;
        $key = $section . "-" . $index;
        if (file_exists($path . $key . ".gif")) {
            return resource_to("/images/items/" . $key . ".gif");
        }

        try {
            $it = new \Morpheus\Item\Item();
            $it->setSection($section)->setIndex($index)->update();
            return self::getDisplayImage($it, 48);
        } catch (\Exception $e) {
        }

        if (is_file($path . "no-image.png")) {
            return resource_to("/images/items/no-image.png");
        }
        return resource_to("/images/items/no.gif");
    }

    /**
     * Dados normalizados para grids de banco/mercado (nome + ícone de cosméticos).
     */
    public static function bankItemView(\Morpheus\Item\Item $item, array $extra = array(), $iconSize = 48, $openMuName = null)
    {
        if ($openMuName !== null && trim((string) $openMuName) !== '') {
            $name = trim((string) $openMuName);
            $level = (int) $item->getLevel();
            $multiples = config("item.multiple_names", array());
            $section = (int) $item->getSection();
            $index = (int) $item->getIndex();
            if ($level > 0 && !isset($multiples[$section][$index][$level])) {
                $name .= " +" . $level;
            }
        } else {
            $name = self::getDisplayName($item);
        }

        return array_merge(array(
            'section' => (int) $item->getSection(),
            'index' => (int) $item->getIndex(),
            'name' => $name,
            'level' => (int) $item->getLevel(),
            'hex' => $item->getHex(),
            'image' => self::getDisplayImage($item, $iconSize, $openMuName),
        ), $extra);
    }
    public static function name($section, $index)
    {
        try {
            return \Morpheus\Item\Item::getStorage()->getItem($section, $index)["name"];
        } catch (\Exception $ex) {
            return "";
        }
    }
    public static function getOptionText(\Morpheus\Item\Item $item)
    {
        $name = null;
        $defaults = $item->getDefaults();
        if (0 < $item->getOption()) {
            if ($item->getSection() == 6) {
                $name = "Additional Defense Rate +" . $item->getOption() / 4 * 5;
            } else {
                if (in_array($defaults["slot"], array(9, 10, 11))) {
                    $name = "Automatic Hp Recovery " . $item->getOption() . "%";
                } else {
                    $type = static::getExcellentsType($item->getSection(), $item->getIndex());
                    if (in_array($type, array(1, 3, 6))) {
                        $name = "Additional Damage +" . $item->getOption();
                    } else {
                        if (in_array($type, array(2, 5))) {
                            $name = "Additional Defense +" . $item->getOption();
                        }
                    }
                }
            }
        }
        return $name;
    }
    public static function getAncientText($section, $index, $ancient)
    {
        $text = "";
        if (0 < $ancient) {
            if (in_array($section, array(6, 7, 8, 9, 10, 11, 13)) || $section == 13 && in_array($index, array(8, 9, 21, 22, 23))) {
                $text = "Increase Stamina +" . $ancient * 5;
            } else {
                if (in_array($section, array(0, 1, 2, 3, 4, 5)) || $section == 13 && in_array($index, array(12, 13, 25, 26, 27))) {
                    $text = "Increase Strength +" . $ancient * 5;
                }
            }
        }
        return $text;
    }
    public static function getClassesBySeason()
    {
        $version = config("server.version_name", "Season 20");
        $version = is_scalar($version) ? trim((string) $version) : "";
        
        if ($version === "97d") {
            return array("dw", "sm", "dk", "bk", "fe", "me");
        }
        
        $classes = array("dw", "sm", "dk", "bk", "fe", "me");
        
        if (!preg_match('/^Season\s+(\d+)$/i', $version, $matches)) {
            return array_merge($classes, array("mg", "dl"));
        }
        
        $season = (int) $matches[1];
        
        if ($season >= 1) {
            $classes = array_merge($classes, array("mg", "dl"));
        }
        
        if ($season >= 4) {
            $classes = array_merge($classes, array(
                "gm", "bm", "he", "dm", "le", "su", "bs", "dim"
            ));
        }
        
        if ($season >= 6) {
            $classes = array_merge($classes, array("rf", "fm"));
        }
        
        if ($season >= 10) {
            $classes = array_merge($classes, array("gl", "ml"));
        }
        
        if ($season >= 14) {
            $classes = array_merge($classes, array(
                "sw", "drk", "ne", "mk", "el", "ds", "fb", "shl",
                "rw", "rsm", "grm", "mrw"
            ));
        }
        
        if ($season >= 15) {
            $classes = array_merge($classes, array("sl", "rsl", "mas", "sla"));
        }
        
        if ($season >= 16) {
            $classes = array_merge($classes, array("gc", "gb", "mgb", "hgc"));
        }
        
        if ($season >= 17) {
            $classes = array_merge($classes, array(
                "ww", "lm", "shw", "lw",
                "mal", "wom", "am", "mm"
            ));
        }
        
        if ($season >= 18) {
            $classes = array_merge($classes, array("ik", "mik", "im", "myk"));
        }
        
        if ($season >= 20) {
            $classes = array_merge($classes, array(
                "daw", "igk", "re", "duk", "foe", "es", "bf", "al", "irw",
                "ros", "mgc", "gw", "bam", "ppk",
                "ac", "alm", "acm", "af", "cre"
            ));
        }
        
        if ($season >= 21) {
            $classes = array_merge($classes, array("cr", "icr", "mp", "sp", "tc"));
        }
        
        return array_values(array_unique($classes));
    }
    public static function canEquip($section, $index)
    {
        $for = array();
        $defaults = \Morpheus\Item\Item::getStorage()->getItem($section, $index);
        if (!isset($defaults["class"])) {
            return $for;
        }
        $availableClasses = static::getClassesBySeason();
        switch ($defaults["class"]["dw"]) {
            case 1:
                $for = array_merge($for, array("dw", "sm", "gm", "sw", "daw"));
                break;
            case 2:
                $for = array_merge($for, array("sm", "gm", "sw", "daw"));
                break;
            case 3:
                $for = array_merge($for, array("gm", "sw", "daw"));
                break;
            case 4:
                $for = array_merge($for, array("sw", "daw"));
                break;
            case 5:
                $for = array_merge($for, array("daw"));
                break;
        }
        switch ($defaults["class"]["dk"]) {
            case 1:
                $for = array_merge($for, array("dk", "bk", "bm", "drk", "igk"));
                break;
            case 2:
                $for = array_merge($for, array("bk", "bm", "drk", "igk"));
                break;
            case 3:
                $for = array_merge($for, array("bm", "drk", "igk"));
                break;
            case 4:
                $for = array_merge($for, array("drk", "igk"));
                break;
            case 5:
                $for = array_merge($for, array("igk"));
                break;
        }
        switch ($defaults["class"]["fe"]) {
            case 1:
                $for = array_merge($for, array("fe", "me", "he", "ne", "re"));
                break;
            case 2:
                $for = array_merge($for, array("me", "he", "ne", "re"));
                break;
            case 3:
                $for = array_merge($for, array("he", "ne", "re"));
                break;
            case 4:
                $for = array_merge($for, array("ne", "re"));
                break;
            case 5:
                $for = array_merge($for, array("re"));
                break;
        }
        switch ($defaults["class"]["mg"]) {
            case 1:
                $for = array_merge($for, array("mg", "dm", "mk", "duk"));
                break;
            case 2:
                $for = array_merge($for, array("dm", "mk", "duk"));
                break;
            case 3:
                $for = array_merge($for, array("dm", "mk", "duk"));
                break;
            case 4:
                $for = array_merge($for, array("mk", "duk"));
                break;
            case 5:
                $for = array_merge($for, array("duk"));
                break;
        }
        if ($defaults["class"]["dl"]) {
            switch ($defaults["class"]["dl"]) {
                case 1:
                    $for = array_merge($for, array("dl", "le", "el", "foe"));
                    break;
                case 2:
                    $for = array_merge($for, array("le", "el", "foe"));
                    break;
                case 3:
                    $for = array_merge($for, array("le", "el", "foe"));
                    break;
                case 4:
                    $for = array_merge($for, array("el", "foe"));
                    break;
                case 5:
                    $for = array_merge($for, array("foe"));
                    break;
            }
        }
        if (isset($defaults["class"]["su"])) {
            switch ($defaults["class"]["su"]) {
                case 1:
                    $for = array_merge($for, array("su", "bs", "dim", "ds", "es"));
                    break;
                case 2:
                    $for = array_merge($for, array("bs", "dim", "ds", "es"));
                    break;
                case 3:
                    $for = array_merge($for, array("dim", "ds", "es"));
                    break;
                case 4:
                    $for = array_merge($for, array("ds", "es"));
                    break;
                case 5:
                    $for = array_merge($for, array("es"));
                    break;
            }
        }
        if (isset($defaults["class"]["rf"])) {
            switch ($defaults["class"]["rf"]) {
                case 1:
                    $for = array_merge($for, array("rf", "fm", "fb", "bf"));
                    break;
                case 2:
                    $for = array_merge($for, array("fm", "fb", "bf"));
                    break;
                case 3:
                    $for = array_merge($for, array("fm", "fb", "bf"));
                    break;
                case 4:
                    $for = array_merge($for, array("fb", "bf"));
                    break;
                case 5:
                    $for = array_merge($for, array("bf"));
                    break;
            }
        }
        if (isset($defaults["class"]["gl"])) {
            switch ($defaults["class"]["gl"]) {
                case 1:
                    $for = array_merge($for, array("gl", "ml", "shl", "al"));
                    break;
                case 2:
                    $for = array_merge($for, array("ml", "shl", "al"));
                    break;
                case 3:
                    $for = array_merge($for, array("ml", "shl", "al"));
                    break;
                case 4:
                    $for = array_merge($for, array("shl", "al"));
                    break;
                case 5:
                    $for = array_merge($for, array("al"));
                    break;
            }
        }
        if (isset($defaults["class"]["rw"])) {
            switch ($defaults["class"]["rw"]) {
                case 1:
                    $for = array_merge($for, array("rw", "rsm", "grm", "mrw", "irw"));
                    break;
                case 2:
                    $for = array_merge($for, array("rsm", "grm", "mrw", "irw"));
                    break;
                case 3:
                    $for = array_merge($for, array("grm", "mrw", "irw"));
                    break;
                case 4:
                    $for = array_merge($for, array("mrw", "irw"));
                    break;
                case 5:
                    $for = array_merge($for, array("irw"));
                    break;
            }
        }
        if (isset($defaults["class"]["sl"])) {
            switch ($defaults["class"]["sl"]) {
                case 1:
                    $for = array_merge($for, array("sl", "rsl", "mas", "sla", "ros"));
                    break;
                case 2:
                    $for = array_merge($for, array("rsl", "mas", "sla", "ros"));
                    break;
                case 3:
                    $for = array_merge($for, array("mas", "sla", "ros"));
                    break;
                case 4:
                    $for = array_merge($for, array("sla", "ros"));
                    break;
                case 5:
                    $for = array_merge($for, array("ros"));
                    break;
            }
        }
        if (isset($defaults["class"]["gc"])) {
            switch ($defaults["class"]["gc"]) {
                case 1:
                    $for = array_merge($for, array("gc", "gb", "mgb", "hgc", "mgc"));
                    break;
                case 2:
                    $for = array_merge($for, array("gb", "mgb", "hgc", "mgc"));
                    break;
                case 3:
                    $for = array_merge($for, array("mgb", "hgc", "mgc"));
                    break;
                case 4:
                    $for = array_merge($for, array("hgc", "mgc"));
                    break;
                case 5:
                    $for = array_merge($for, array("mgc"));
                    break;
            }
        }
        if (isset($defaults["class"]["ww"])) {
            switch ($defaults["class"]["ww"]) {
                case 1:
                    $for = array_merge($for, array("ww", "lm", "shw", "lw", "gw"));
                    break;
                case 2:
                    $for = array_merge($for, array("lm", "shw", "lw", "gw"));
                    break;
                case 3:
                    $for = array_merge($for, array("shw", "lw", "gw"));
                    break;
                case 4:
                    $for = array_merge($for, array("lw", "gw"));
                    break;
                case 5:
                    $for = array_merge($for, array("gw"));
                    break;
            }
        }
        if (isset($defaults["class"]["mal"])) {
            switch ($defaults["class"]["mal"]) {
                case 1:
                    $for = array_merge($for, array("mal", "wom", "am", "mm", "bam"));
                    break;
                case 2:
                    $for = array_merge($for, array("wom", "am", "mm", "bam"));
                    break;
                case 3:
                    $for = array_merge($for, array("am", "mm", "bam"));
                    break;
                case 4:
                    $for = array_merge($for, array("mm", "bam"));
                    break;
                case 5:
                    $for = array_merge($for, array("bam"));
                    break;
            }
        }
        if (isset($defaults["class"]["ik"])) {
            switch ($defaults["class"]["ik"]) {
                case 1:
                    $for = array_merge($for, array("ik", "mik", "im", "myk", "ppk"));
                    break;
                case 2:
                    $for = array_merge($for, array("mik", "im", "myk", "ppk"));
                    break;
                case 3:
                    $for = array_merge($for, array("im", "myk", "ppk"));
                    break;
                case 4:
                    $for = array_merge($for, array("myk", "ppk"));
                    break;
                case 5:
                    $for = array_merge($for, array("ppk"));
                    break;
            }
        }
        if (isset($defaults["class"]["ac"])) {
            switch ($defaults["class"]["ac"]) {
                case 1:
                    $for = array_merge($for, array("ac", "alm", "acm", "af", "cre"));
                    break;
                case 2:
                    $for = array_merge($for, array("alm", "acm", "af", "cre"));
                    break;
                case 3:
                    $for = array_merge($for, array("acm", "af", "cre"));
                    break;
                case 4:
                    $for = array_merge($for, array("af", "cre"));
                    break;
                case 5:
                    $for = array_merge($for, array("cre"));
                    break;
            }
        }
        if (isset($defaults["class"]["cr"])) {
            switch ($defaults["class"]["cr"]) {
                case 1:
                    $for = array_merge($for, array("cr", "icr", "mp", "sp", "tc"));
                    break;
                case 2:
                    $for = array_merge($for, array("icr", "mp", "sp", "tc"));
                    break;
                case 3:
                    $for = array_merge($for, array("mp", "sp", "tc"));
                    break;
                case 4:
                    $for = array_merge($for, array("sp", "tc"));
                    break;
                case 5:
                    $for = array_merge($for, array("tc"));
                    break;
            }
        }
        return array_values(array_intersect($for, $availableClasses));
    }
    public static function getOptions($section, $index)
    {
        $path = ROOT . DS . "resources" . DS . "files" . DS . "ItemOption.txt";
        $path2 = ROOT . DS . "resources" . DS . "files" . DS . "ItemOptionName.txt";
        $options = array();
        if (!($file = fopen($path, "r"))) {
            throw new \Exception("Was not possible to open the file, verify that the file has permissions");
        }
        if (!($file2 = fopen($path2, "r"))) {
            throw new \Exception("Was not possible to open the file, verify that the file has permissions");
        }
        $names = array();
        while (!feof($file2)) {
            $info = fscanf($file2, "%d %d \"%[^\"]\"");
            if (!strpos($info[0], "//") && isset($info[0])) {
                $names[$info[1]] = $info[2];
            }
        }
        while (!feof($file)) {
            $info = fscanf($file, "%d %d %d %d %d %s %s %s");
            if (!strpos($info[0], "//") && isset($info[0])) {
                $range = $section * 512 + $index;
                if ($info[3] <= $range && $range <= $info[4] && isset($names[$info[1]])) {
                    $options[$info[0]] = array("name" => $names[$info[1]], "value" => $info[2], "skill" => $info[5], "luck" => $info[6], "option" => $info[7]);
                }
            }
        }
        return $options;
    }
}

?>