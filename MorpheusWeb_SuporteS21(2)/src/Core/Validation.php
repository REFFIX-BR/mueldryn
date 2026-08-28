<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Core;

class Validation
{
    private $_errors = array();
    private static $_source = array();
    const REQUIRED = 1;
    const ONLY_NUMBERS = 2;
    const ALPHANUMERIC = 3;
    const ALPHA = 4;
    const EMAIL = 5;
    const CAPTCHA = 6;
    const CPF = 7;
    const ALPHANUMERIC_LOWER = 8;
    public function __construct($source, $rules = NULL)
    {
        static::$_source = $source;
        if ($rules !== null) {
            $this->setRules($rules);
        }
    }
    public function getErrors()
    {
        return $this->_errors;
    }
    public function isValid()
    {
        return empty($this->_errors);
    }
    private function _translateRule($rule)
    {
        $translated = $rule;
        switch ($rule) {
            case self::REQUIRED:
                $translated = array("rule" => "notEmpty", "message" => __("Required field"));
                break;
            case self::ONLY_NUMBERS:
                $translated = array("rule" => "numeric", "message" => __("Enter only numbers"));
                break;
            case self::ALPHANUMERIC:
                $translated = array("rule" => "alphanumeric", "message" => __("Enter only alphanumeric"));
                break;
            case self::ALPHA:
                $translated = array("rule" => "alpha", "message" => __("Enter only alpha"));
                break;
            case self::EMAIL:
                $translated = array("rule" => "email", "message" => __("Invalid email format"));
                break;
            case self::CAPTCHA:
                $translated = array("rule" => "captcha", "message" => __("Invalid captcha text"));
                break;
            case self::CPF:
                $translated = array("rule" => "cpf", "message" => __("Invalid CPF"));
                break;
            case self::ALPHANUMERIC_LOWER:
                $translated = array("rule" => "alphanumericLower", "message" => __("Enter only alphanumeric and lower case"));
                break;
        }
        return $translated;
    }
    public function setRules($validates = array())
    {
        $this->_errors = array();
        $defaults = array("allowEmpty" => false, "message" => null, "condition" => true);
        foreach ($validates as $field => $rules) {
            if (!is_array($rules) || is_array($rules) && isset($rules["rule"])) {
                $rules = array($rules);
            }
            foreach ($rules as $message => $rule) {
                $rule = $this->_translateRule($rule);
                if (!is_array($rule)) {
                    $rule = array("rule" => $rule);
                }
                $rule = array_merge($defaults, $rule);
                if (isset(static::$_source[$field]) && $rule["condition"] && !$this->callValidationMethod($rule["rule"], static::$_source[$field], $rule["allowEmpty"])) {
                    $message = $rule["message"] === null ? is_string($message) ? $message : (is_array($rule["rule"]) ? $rule["rule"][0] : $rule["rule"]) : $rule["message"];
                    $this->_errors[$field] = $message;
                    break;
                }
            }
        }
    }
    public function callValidationMethod($params, $value, $empty = false)
    {
        $method = is_array($params) ? $params[0] : $params;
        if (strpos($method, "::")) {
            list($class, $method) = explode("::", $method);
        } else {
            $class = "Morpheus\\Core\\Validation";
        }
        if (is_array($params)) {
            $params[0] = $value;
        } else {
            $params = array($value);
        }
        if (empty($value) && $empty) {
            return true;
        }
        return call_user_func_array(array($class, $method), $params);
    }
    public static function alphanumeric($value)
    {
        return !(bool) preg_match("/[^a-z_0-9]/i", $value);
    }
    public static function alphanumericLower($value)
    {
        return !(bool) preg_match("/[^a-z_0-9]/", $value);
    }
    public static function between($value, $min, $max)
    {
        $value = strlen($value);
        return filter_var($value, FILTER_VALIDATE_INT, array("options" => array("min_range" => $min, "max_range" => $max))) !== false;
    }
    public static function blank($value)
    {
        return !preg_match("/[^\\s]/", $value);
    }
    public static function boolean($value)
    {
        $boolean = array(0, 1, "0", "1", true, false);
        return in_array($value, $boolean, true);
    }
    public static function comparison($value1, $operator, $value2)
    {
        switch ($operator) {
            case ">":
            case "greater":
                return $value2 < $value1;
            case "<":
            case "less":
                return $value1 < $value2;
            case ">=":
            case "greaterorequal":
                return $value2 <= $value1;
            case "<=":
            case "lessorequal":
                return $value1 <= $value2;
            case "==":
            case "equal":
                return $value1 == $value2;
            case "!=":
            case "notequal":
                return $value1 != $value2;
        }
        return false;
    }
    public static function regex($value, $regex)
    {
        return preg_match($regex, $value);
    }
    public static function date($value)
    {
        $regex = "%^(?:(?:31(/|-|\\.)(?:0?[13578]|1[02]))\\1|(?:(?:29|30)(/|-|\\.)(?:0?[1,3-9]|1[0-2])\\2))(?:(?:1[6-9]|[2-9]\\d)?\\d{2})\$|^(?:29(/|-|\\.)0?2\\3(?:(?:(?:1[6-9]|[2-9]\\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))\$|^(?:0?[1-9]|1\\d|2[0-8])(/|-|\\.)(?:(?:0?[1-9])|(?:1[0-2]))\\4(?:(?:1[6-9]|[2-9]\\d)?\\d{2})\$%";
        return (bool) preg_match($regex, $value);
    }
    public static function decimal($value, $places = NULL)
    {
        if (is_null($places)) {
            $regex = "/^[+-]?[\\d]+\\.[\\d]+([eE][+-]?[\\d]+)?\$/";
        } else {
            $regex = "/^[+-]?[\\d]+\\.[\\d]{" . $places . "}\$/";
        }
        return (bool) preg_match($regex, $value);
    }
    public static function email($value)
    {
        return filter_var($value, FILTER_VALIDATE_EMAIL) !== false;
    }
    public static function equals($value, $field)
    {
        return $value == static::$_source[$field];
    }
    public static function ip($value)
    {
        return filter_var($value, FILTER_VALIDATE_IP) !== false;
    }
    public static function length($value, $length)
    {
        $l = strlen($value);
        return $l == $length;
    }
    public static function minLength($value, $length)
    {
        $l = strlen($value);
        return $length <= $l;
    }
    public static function maxLength($value, $length)
    {
        $l = strlen($value);
        return $l <= $length;
    }
    public static function multiple($values, $list, $min = NULL, $max = NULL)
    {
        $values = array_filter($values);
        if (empty($values)) {
            return false;
        }
        if (!is_null($min) && count($values) < $min) {
            return false;
        }
        if (!is_null($max) && $max < count($values)) {
            return false;
        }
        foreach (array_keys($values) as $value) {
            if (!in_array($value, $list)) {
                return false;
            }
        }
        return true;
    }
    public static function inList($value, $list)
    {
        return in_array($value, $list);
    }
    public static function numeric($value)
    {
        return is_numeric($value) && 0 <= $value && $value == round($value);
    }
    public static function alpha($value)
    {
        return (bool) ctype_alpha($value);
    }
    public static function notEmpty($value)
    {
        return (bool) preg_match("/[^\\s]+/m", $value);
    }
    public static function range($value, $lower = NULL, $upper = NULL)
    {
        if (is_numeric($value)) {
            if (!is_null($lower) || !is_null($upper)) {
                $check_lower = $check_upper = true;
                if (!is_null($lower)) {
                    $check_lower = $lower < $value;
                }
                if (!is_null($upper)) {
                    $check_upper = $value < $upper;
                }
                return $check_lower && $check_upper;
            }
            return is_finite($value);
        }
        return false;
    }
    public static function time($value)
    {
        $regex = "/^([01]\\d|2[0-3])(:[0-5]\\d){1,2}\$|^(0?[1-9]|1[0-2])(:[0-5]\\d){1,2}\\s?[AaPp]m\$/";
        return (bool) preg_match($regex, $value);
    }
    public static function url($value)
    {
        return filter_var($value, FILTER_VALIDATE_URL) !== false;
    }
    public static function isUnique($value, $table, $field)
    {
        $total = \Morpheus\Database\Connection::fetchColumn("SELECT COUNT(1) AS total FROM " . $table . " WHERE " . $field . " = :value", array("value" => $value));
        return $total == 0;
    }
    public static function exists($value, $table, $field)
    {
        $total = \Morpheus\Database\Connection::fetchColumn("SELECT COUNT(1) AS total FROM " . $table . " WHERE " . $field . " = :value", array("value" => $value));
        return $total == 1;
    }
    public static function notExists($value, $table, $field)
    {
        $total = \Morpheus\Database\Connection::fetchColumn("SELECT COUNT(1) AS total FROM " . $table . " WHERE " . $field . " = :value", array("value" => $value));
        return empty($total) || $total == 0;
    }
    public static function captcha($value)
    {
        $session = new \SlimSession\Helper();
        return $session->get("captcha") !== null && strtolower(trim($value)) == $session->get("captcha");
    }
    public static function cpf($value)
    {
        $cpf = preg_replace("/[^0-9]/", "", (string) $value);
        if (strlen($cpf) != 11) {
            return false;
        }
        $i = 0;
        $j = 10;
        for ($soma = 0; $i < 9; $j--) {
            $soma += $cpf[$i] * $j;
            $i++;
        }
        $resto = $soma % 11;
        if ($cpf[9] != ($resto < 2 ? 0 : 11 - $resto)) {
            return false;
        }
        $i = 0;
        $j = 11;
        for ($soma = 0; $i < 10; $j--) {
            $soma += $cpf[$i] * $j;
            $i++;
        }
        $resto = $soma % 11;
        return $cpf[10] == ($resto < 2 ? 0 : 11 - $resto);
    }
}

?>