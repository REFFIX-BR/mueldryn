<?php
/**
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.4
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 *
 * @ Zend guard decoder PHP 5.6
 **/

return array(
	'name'        => 'LootBox',
	'author'      => 'Flavio Hernandes',
	'version'     => '1.0.0',
	'description' => 'Sistema aletório de geração de itens',
	'website'     => 'https://morpheusmuweb.com/',
	'services'    => array(
		'lootbox' => array('name' => 'Caixas da sorte', 'allowed' => true, 'url' => '/lootboxes')
		)
	);

?>
