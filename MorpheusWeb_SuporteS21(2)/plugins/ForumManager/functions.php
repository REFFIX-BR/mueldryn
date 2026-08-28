<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

function random_str($length = 8)
{
    $set = array_merge(range(0, 9), range("A", "Z"), range("a", "z"));
    $str = array();
    for ($i = 0; $i < $length; $i++) {
        $str[] = $set[my_rand(0, 61)];
    }
    shuffle($str);
    return implode($str);
}
function my_rand($min = 0, $max = PHP_INT_MAX)
{
    if ($min === NULL || $max === NULL || $max < $min) {
        $min = 0;
        $max = PHP_INT_MAX;
    }
    if (version_compare(PHP_VERSION, "7.0", ">=")) {
        try {
            $result = random_int($min, $max);
        } catch (Exception $e) {
        }
        if (isset($result)) {
            return $result;
        }
    }
    $seed = secure_seed_rng();
    $distance = $max - $min;
    return $min + floor($distance * $seed / PHP_INT_MAX);
}
function secure_seed_rng()
{
    $bytes = PHP_INT_SIZE;
    do {
        $output = secure_binary_seed_rng($bytes);
        if ($bytes == 4) {
            $elements = unpack("i", $output);
            $output = abs($elements[1]);
        } else {
            $elements = unpack("N2", $output);
            $output = abs($elements[1] << 32 | $elements[2]);
        }
    } while (PHP_INT_MAX < $output);
    return $output;
}
function secure_binary_seed_rng($bytes)
{
    $output = NULL;
    if (version_compare(PHP_VERSION, "7.0", ">=")) {
        try {
            $output = random_bytes($bytes);
        } catch (Exception $e) {
        }
    }
    if (strlen($output) < $bytes) {
        if (@is_readable("/dev/urandom") && ($handle = @fopen("/dev/urandom", "rb"))) {
            $output = @fread($handle, $bytes);
            @fclose($handle);
        }
        if (strlen($output) < $bytes) {
            if (function_exists("mcrypt_create_iv")) {
                if (DIRECTORY_SEPARATOR == "/") {
                    $source = MCRYPT_DEV_URANDOM;
                } else {
                    $source = MCRYPT_RAND;
                }
                $output = @mcrypt_create_iv($bytes, $source);
            }
            if (strlen($output) < $bytes) {
                if (function_exists("openssl_random_pseudo_bytes") && (DIRECTORY_SEPARATOR == "/" || version_compare(PHP_VERSION, "5.3.4", ">="))) {
                    $output = openssl_random_pseudo_bytes($bytes, $crypto_strong);
                    if ($crypto_strong == false) {
                        $output = NULL;
                    }
                }
                if (strlen($output) < $bytes) {
                    if (class_exists("COM")) {
                        try {
                            $CAPI_Util = new COM("CAPICOM.Utilities.1");
                            if (is_callable(array($CAPI_Util, "GetRandom"))) {
                                $output = $CAPI_Util->GetRandom($bytes, 0);
                            }
                        } catch (Exception $e) {
                        }
                    }
                    if (strlen($output) < $bytes) {
                        $unique_state = microtime() . @getmypid();
                        $rounds = ceil($bytes / 16);
                        for ($i = 0; $i < $rounds; $i++) {
                            $unique_state = md5(microtime() . $unique_state);
                            $output .= md5($unique_state);
                        }
                        $output = substr($output, 0, $bytes * 2);
                        $output = pack("H*", $output);
                        return $output;
                    }
                    return $output;
                }
                return $output;
            }
            return $output;
        }
        return $output;
    }
    return $output;
}

?>