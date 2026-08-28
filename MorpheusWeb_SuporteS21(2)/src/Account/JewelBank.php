<?php

namespace Morpheus\Account;

/**
 * Banco de joias (Bless, Soul, Life, Creation, Chaos).
 * No OpenMU usa atributos da conta (mesmo "Banco de Joias" do client);
 * fallback: vault/warehouse.
 */
class JewelBank
{
    /** @return array<string,array> */
    public static function catalog()
    {
        return array(
            'jb' => array(
                'code' => 'jb',
                'short' => 'B',
                'name' => 'Jewel of Bless',
                'section' => 14,
                'index' => 13,
                'image' => '14-13.gif',
                'definition_id' => '00000080-000e-000d-0000-000000000000',
                'attribute_id' => 'a1b2c3d4-1002-4e5f-8a9b-0c1d2e3f4002',
            ),
            'js' => array(
                'code' => 'js',
                'short' => 'S',
                'name' => 'Jewel of Soul',
                'section' => 14,
                'index' => 14,
                'image' => '14-14.gif',
                'definition_id' => '00000080-000e-000e-0000-000000000000',
                'attribute_id' => 'a1b2c3d4-1003-4e5f-8a9b-0c1d2e3f4003',
            ),
            'jl' => array(
                'code' => 'jl',
                'short' => 'L',
                'name' => 'Jewel of Life',
                'section' => 14,
                'index' => 16,
                'image' => '14-16.gif',
                'definition_id' => '00000080-000e-0010-0000-000000000000',
                'attribute_id' => 'a1b2c3d4-1004-4e5f-8a9b-0c1d2e3f4004',
            ),
            'jcr' => array(
                'code' => 'jcr',
                'short' => 'C',
                'name' => 'Jewel of Creation',
                'section' => 14,
                'index' => 22,
                'image' => '14-22.gif',
                'definition_id' => '00000080-000e-0016-0000-000000000000',
                'attribute_id' => 'a1b2c3d4-1005-4e5f-8a9b-0c1d2e3f4005',
            ),
            'jc' => array(
                'code' => 'jc',
                'short' => 'Chaos',
                'name' => 'Jewel of Chaos',
                'section' => 12,
                'index' => 15,
                'image' => '12-15.gif',
                'definition_id' => '00000080-000c-000f-0000-000000000000',
                'attribute_id' => 'a1b2c3d4-1001-4e5f-8a9b-0c1d2e3f4001',
            ),
        );
    }

    /**
     * @param Account|string $account
     * @return array<string,int> keyed by jb/js/jl/jcr/jc
     */
    public static function counts($account)
    {
        $login = $account instanceof Account ? $account->getUsername() : (string) $account;
        $counts = array();
        foreach (self::catalog() as $code => $meta) {
            $counts[$code] = 0;
        }

        if (class_exists('\\Morpheus\\OpenMU\\Bridge') && \Morpheus\OpenMU\Bridge::enabled()) {
            try {
                $prev = ini_get('default_socket_timeout');
                ini_set('default_socket_timeout', '2');
                $open = \Morpheus\OpenMU\Bridge::countJewelBank($login);
                ini_set('default_socket_timeout', $prev);
                foreach ($counts as $code => $_) {
                    if (isset($open[$code])) {
                        $counts[$code] = (int) $open[$code];
                    }
                }
                return $counts;
            } catch (\Exception $e) {
                // fallback warehouse
            }
        }

        $acc = $account instanceof Account ? $account : account($login);
        if (!$acc || !$acc->exists()) {
            return $counts;
        }
        $wh = $acc->getWarehouse();
        $wh->load();
        foreach (self::catalog() as $code => $meta) {
            $counts[$code] = count($wh->getItemsByType($meta['section'], $meta['index']));
        }
        return $counts;
    }

    /**
     * @param array<string,int> $prices
     * @return true|string true or error message
     */
    public static function canAfford($buyerLogin, array $prices)
    {
        $have = self::counts($buyerLogin);
        foreach (self::catalog() as $code => $meta) {
            $need = isset($prices[$code]) ? (int) $prices[$code] : 0;
            if ($need > 0 && $have[$code] < $need) {
                return 'Você não possui ' . $meta['name'] . ' suficiente (precisa ' . $need . ', tem ' . $have[$code] . ').';
            }
        }
        return true;
    }

    /**
     * Move joias do comprador para o vendedor (vault OpenMU e/ou warehouse).
     * @param array<string,int> $prices
     */
    public static function transfer($fromLogin, $toLogin, array $prices)
    {
        $any = false;
        foreach ($prices as $v) {
            if ((int) $v > 0) {
                $any = true;
                break;
            }
        }
        if (!$any) {
            return true;
        }

        if (class_exists('\\Morpheus\\OpenMU\\Bridge') && \Morpheus\OpenMU\Bridge::enabled()) {
            \Morpheus\OpenMU\Bridge::transferJewelBank($fromLogin, $toLogin, $prices);
            return true;
        }

        $from = account($fromLogin);
        $to = account($toLogin);
        $from->getWarehouse()->load();
        $to->getWarehouse()->load();

        foreach (self::catalog() as $code => $meta) {
            $need = isset($prices[$code]) ? (int) $prices[$code] : 0;
            if ($need <= 0) {
                continue;
            }
            $items = $from->getWarehouse()->getItemsByType($meta['section'], $meta['index']);
            if (count($items) < $need) {
                throw new \Exception('Joias insuficientes: ' . $meta['name']);
            }
            for ($i = 0; $i < $need; $i++) {
                $from->getWarehouse()->removeItem($items[$i]);
                if (!$to->getWarehouse()->addItem($items[$i])) {
                    throw new \Exception('Sem espaço no banco do vendedor para ' . $meta['name']);
                }
            }
        }

        $from->getWarehouse()->update();
        $to->getWarehouse()->update();
        return true;
    }

    /** Normalize posted jewel prices. */
    public static function parsePrices($input)
    {
        $out = array();
        foreach (self::catalog() as $code => $_) {
            $out[$code] = isset($input[$code]) ? max(0, (int) $input[$code]) : 0;
        }
        return $out;
    }

    public static function hasAnyPrice(array $prices)
    {
        foreach ($prices as $v) {
            if ((int) $v > 0) {
                return true;
            }
        }
        return false;
    }

    public static function imageUrl($code)
    {
        $cat = self::catalog();
        if (!isset($cat[$code])) {
            return '';
        }
        return resource_to('/images/items/' . $cat[$code]['image']);
    }
}
