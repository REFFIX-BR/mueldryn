<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

define("ROOT_PATH", dirname(__DIR__));
require ROOT_PATH . DIRECTORY_SEPARATOR . "vendor" . DIRECTORY_SEPARATOR . "autoload.php";
require ROOT_PATH . DIRECTORY_SEPARATOR . "constants.php";
require ROOT_PATH . DIRECTORY_SEPARATOR . "src" . DIRECTORY_SEPARATOR . "basics.php";
Morpheus\Core\Config::addFiles(array("characters.php", "database.php", "items.php"));
$conn = Morpheus\Database\DriverManager::addConnection("default", config("conn.default"));
$conn->connect();
Morpheus\Core\Config::load();
$hex = $_GET["hex"];
$item = new Morpheus\Item\Item($hex);
$item->parse();
$defaults = $item->getDefaults();
echo "\r\n<div class=\"item-desc\">\r\n    <div class=\"name\">\r\n        ";
echo Morpheus\Util\Item::getFullName($item);
echo "    </div>\r\n\r\n    ";
if ($item->getDefinitions()->hasDurability()) {
    echo "        <div class=\"durability\">\r\n            Durability: [";
    echo $item->getDurability();
    echo "/";
    echo $item->getDefinitions()->getDurability();
    echo "]\r\n        </div>\r\n    ";
}
echo "\r\n    ";
if ($item->getDefinitions()->hasRefine() && $item->getRefine()) {
    echo "        <div class=\"refine\">\r\n            ";
    echo $item->getDefinitions()->getRefineText();
    echo "        </div>\r\n    ";
}
echo "\r\n    ";
if ($item->getDefinitions()->hasHarmony() && $item->getHarmony()->has()) {
    echo "        <div class=\"harmony\">\r\n            ";
    echo $item->getHarmony()->getName();
    echo "        </div>\r\n    ";
}
echo "\r\n    ";
if ($item->hasSkill()) {
    echo "        <div class=\"skill\">\r\n            Skill ";
    echo Morpheus\Util\Item::getSkillName($item);
    echo "        </div>\r\n    ";
}
echo "\r\n    ";
$can = $item->getDefinitions()->canEquipTexts();
echo "    ";
if (!empty($can)) {
    echo "        <div class=\"can-equip-block\">\r\n            ";
    foreach ($can as $text) {
        echo "                <div class=\"can-equip\">";
        echo $text;
        echo "</div>\r\n            ";
    }
    echo "        </div>\r\n    ";
}
echo "\r\n    ";
if ($item->hasLuck() || 0 < $item->getOption() || $item->isExcellent() || $item->isAncient()) {
    echo "        <div class=\"options-block\">\r\n            ";
    if ($item->isAncient()) {
        echo "                <div class=\"blue\">\r\n                    ";
        echo $item->getAncient()->toText();
        echo "                </div>\r\n            ";
    }
    echo "\r\n            ";
    if ($item->hasLuck()) {
        echo "                <div class=\"luck\">\r\n                    ";
        echo $item->getDefinitions()->getLuckText();
        echo "                </div>\r\n            ";
    }
    echo "\r\n            ";
    if (0 < $item->getOption()) {
        echo "                <div class=\"blue\">\r\n                    ";
        echo $item->getDefinitions()->getOptionText();
        echo "                </div>\r\n            ";
    }
    echo "\r\n            ";
    if ($item->getDefinitions()->hasExcellentOptions()) {
        echo "                ";
        foreach ($item->getDefinitions()->getExcellentOptions() as $index => $ex) {
            echo "                    ";
            if ($item->getExcellent($index)) {
                echo "                        <div class=\"blue\">\r\n                            ";
                echo $ex;
                echo "                        </div>\r\n                    ";
            }
            echo "                ";
        }
        echo "            ";
    }
    echo "        </div>\r\n    ";
}
echo "\r\n    ";
if ($item->getDefinitions()->hasSockets()) {
    echo "        <div class=\"socket-block\">\r\n            ";
    foreach ($item->getSockets() as $i => $socket) {
        echo "                ";
        if ($socket->has()) {
            echo "                    <div class=\"socket\">\r\n                        Socket #";
            echo $i + 1;
            echo ":\r\n                        ";
            if ($socket->isEmpty()) {
                echo "                            empty\r\n                        ";
            } else {
                echo "                            ";
                echo $socket->getName();
                echo "                        ";
            }
            echo "                    </div>\r\n                ";
        }
        echo "            ";
    }
    echo "        </div>\r\n    ";
}
echo "</div>";

?>