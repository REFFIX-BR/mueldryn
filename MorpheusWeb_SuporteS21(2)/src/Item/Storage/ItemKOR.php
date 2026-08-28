<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item\Storage;

class ItemKOR extends AbstractStorage
{
    protected function parse()
    {
        if (!($file = fopen($this->getFile(), "rb+"))) {
            throw new \Exception("Was not possible to open the file, verify that the file has permissions");
        }
        $section = -1;
        $key = null;
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
            if ($section == -1) {
                if (is_numeric($line)) {
                    $section = $line;
                }
            } else {
                if (strtolower($line) == "end") {
                    $section = -1;
                    continue;
                }
                $columns = preg_split("/[\\s,]*\\\"([^\\\"]+)\\\"[\\s,]*|[\\s,]*'([^']+)'[\\s,]*|[\\s,]+/", $line, 0, PREG_SPLIT_NO_EMPTY | PREG_SPLIT_DELIM_CAPTURE);
                $old = array("index" => 0, "x" => 1, "y" => 2, "name" => 6);
                $new = array("index" => 0, "x" => 3, "y" => 4, "name" => 8, "skill" => 2, "slot" => 1);
                if ($key === null) {
                    if (count($columns) <= 19) {
                        $key = $old;
                    } else {
                        $key = $new;
                    }
                }
                $data = array("name" => isset($columns[$key["name"]]) ? $columns[$key["name"]] : "", "width" => isset($columns[$key["x"]]) ? $columns[$key["x"]] : 1, "height" => isset($columns[$key["y"]]) ? $columns[$key["y"]] : 1, "id" => $columns[0], "skill" => isset($key["skill"]) && isset($columns[$key["skill"]]) ? $columns[$key["skill"]] : 0, "slot" => isset($key["slot"]) && isset($columns[$key["slot"]]) ? $columns[$key["slot"]] : 0);
                if (0 <= $section && $section <= 5) {
                    $data += array("damage" => array("min" => $columns[10], "max" => $columns[11]), "attack_speed" => $columns[12], "durability" => $columns[13], "magic" => array("durability" => $columns[14], "damage" => $columns[15]), "requirement" => array("level" => $columns[16], "strength" => $columns[17], "dexterity" => $columns[18], "vitality" => $columns[19], "energy" => $columns[20], "command" => $columns[21]), "class" => array("dw" => $columns[23], "dk" => $columns[24], "fe" => $columns[25], "mg" => $columns[26], "dl" => $columns[27], "su" => isset($columns[28]) ? $columns[28] : null, "rf" => isset($columns[29]) ? $columns[29] : null, "gl" => isset($columns[30]) ? $columns[30] : null, "rw" => isset($columns[31]) ? $columns[31] : null, "sl" => isset($columns[32]) ? $columns[32] : null, "gc" => isset($columns[33]) ? $columns[33] : null, "ww" => isset($columns[34]) ? $columns[34] : null, "mal" => isset($columns[35]) ? $columns[35] : null, "ik" => isset($columns[36]) ? $columns[36] : null, "ac" => isset($columns[37]) ? $columns[37] : null, "cr" => isset($columns[38]) ? $columns[38] : null));
                } else {
                    if (5 < $section && $section <= 11) {
                        $data += array("defense" => $columns[10], "durability" => $columns[12], "class" => array("dw" => $columns[20], "dk" => $columns[21], "fe" => $columns[22], "mg" => $columns[23], "dl" => $columns[24], "su" => isset($columns[25]) ? $columns[25] : null, "rf" => isset($columns[26]) ? $columns[26] : null, "gl" => isset($columns[27]) ? $columns[27] : null, "rw" => isset($columns[28]) ? $columns[28] : null, "sl" => isset($columns[29]) ? $columns[29] : null, "gc" => isset($columns[30]) ? $columns[30] : null, "ww" => isset($columns[31]) ? $columns[31] : null, "mal" => isset($columns[32]) ? $columns[32] : null, "ik" => isset($columns[33]) ? $columns[33] : null, "ac" => isset($columns[34]) ? $columns[34] : null, "cr" => isset($columns[35]) ? $columns[35] : null));
                        switch ($section) {
                            case 6:
                                $data += array("defense_success" => $columns[11]);
                                break;
                            case 7:
                                $data += array("magic_defense" => $columns[11]);
                                break;
                            case 8:
                                $data += array("magic_defense" => $columns[11]);
                                break;
                            case 9:
                                $data += array("magic_defense" => $columns[11]);
                                break;
                            case 10:
                                $data += array("attack_speed" => $columns[11]);
                                break;
                            case 11:
                                $data += array("walk_speed" => $columns[11]);
                                break;
                        }
                    } else {
                        if ($section == 12) {
                            $data += array("defense" => $columns[10], "durability" => $columns[11], "requirement" => array("level" => $columns[12], "strength" => $columns[13], "dexterity" => $columns[14], "energy" => $columns[15], "command" => $columns[16]), "class" => array("dw" => $columns[18], "dk" => $columns[19], "fe" => $columns[20], "mg" => $columns[21], "dl" => $columns[22], "su" => isset($columns[23]) ? $columns[23] : null, "rf" => isset($columns[24]) ? $columns[24] : null, "gl" => isset($columns[25]) ? $columns[25] : null, "rw" => isset($columns[26]) ? $columns[26] : null, "sl" => isset($columns[27]) ? $columns[27] : null, "gc" => isset($columns[28]) ? $columns[28] : null, "ww" => isset($columns[29]) ? $columns[29] : null, "mal" => isset($columns[30]) ? $columns[30] : null, "ik" => isset($columns[31]) ? $columns[31] : null, "ac" => isset($columns[32]) ? $columns[32] : null, "cr" => isset($columns[33]) ? $columns[33] : null));
                        } else {
                            if ($section == 13) {
                                $data += array("durability" => $columns[10], "resistance" => array("ice" => $columns[11], "poison" => $columns[12], "light" => $columns[13], "fire" => $columns[14], "earth" => $columns[15], "wind" => $columns[16], "water" => $columns[17]), "class" => array("dw" => $columns[19], "dk" => $columns[20], "fe" => $columns[21], "mg" => $columns[22], "dl" => $columns[23], "su" => isset($columns[24]) ? $columns[24] : null, "rf" => isset($columns[25]) ? $columns[25] : null, "gl" => isset($columns[26]) ? $columns[26] : null, "rw" => isset($columns[27]) ? $columns[27] : null, "sl" => isset($columns[28]) ? $columns[28] : null, "gc" => isset($columns[29]) ? $columns[29] : null, "ww" => isset($columns[30]) ? $columns[30] : null, "mal" => isset($columns[31]) ? $columns[31] : null, "ik" => isset($columns[32]) ? $columns[32] : null, "ac" => isset($columns[33]) ? $columns[33] : null, "cr" => isset($columns[34]) ? $columns[34] : null));
                            } else {
                                if ($section == 15) {
                                    $data += array("requirement" => array("level" => $columns[10], "energy" => $columns[11]), "class" => array("dw" => $columns[13], "dk" => $columns[14], "fe" => $columns[15], "mg" => $columns[16], "dl" => $columns[17], "su" => isset($columns[18]) ? $columns[18] : null, "rf" => isset($columns[19]) ? $columns[19] : null, "gl" => isset($columns[20]) ? $columns[20] : null, "rw" => isset($columns[21]) ? $columns[21] : null, "sl" => isset($columns[22]) ? $columns[22] : null, "gc" => isset($columns[23]) ? $columns[23] : null, "ww" => isset($columns[24]) ? $columns[24] : null, "mal" => isset($columns[25]) ? $columns[25] : null, "ik" => isset($columns[26]) ? $columns[26] : null, "ac" => isset($columns[27]) ? $columns[27] : null, "cr" => isset($columns[28]) ? $columns[28] : null));
                                }
                            }
                        }
                    }
                }
                $this->_items[$section][$columns[0]] = $data;
            }
        }
   return $this->_items;
    }
}

?>