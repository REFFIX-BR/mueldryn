<?php
/**
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.4
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 *
 * @ Zend guard decoder PHP 5.6
 **/

View::add('script', array('LootBox.roulette.min', 'LootBox.lootbox'));
App::hook('accounts.register', function($account) {
	$lootboxes = Connection::fetchAll('select * ' . "\r\n" . '        from mw_lootboxes l' . "\r\n" . '        where l.active = 1' . "\r\n" . '        and (l.expire_date is null or l.expire_date > getdate())' . "\r\n" . '        and l.gift_register is not null' . "\r\n" . '        and l.gift_register > 0' . "\r\n" . '        and (select count(1) from mw_lootboxes_items where lootbox_id = l.id) > 0' . "\r\n" . '    ');

	foreach ($lootboxes as $box) {
		for ($i = 0; $i < $box['gift_register']; $i++) {
			Connection::insert('mw_lootboxes_account', array('account' => $account->getUsername(), 'lootbox_id' => $box['id']));
		}
	}
});
Route::get('/lootboxes', authenticate(), plugin_license('LootBox'), function() {
	$boxes = Connection::fetchAll('select * ' . "\r\n" . '        from mw_lootboxes l ' . "\r\n" . '        where l.active = 1' . "\r\n" . '        and (l.expire_date is null or l.expire_date > getdate())' . "\r\n" . '        and (select count(1) from mw_lootboxes_items where lootbox_id = l.id) > 0' . "\r\n" . '    ');
	$query = Connection::fetchAll('select b.id' . "\r\n" . '        , count(1) as total' . "\r\n" . '        from mw_lootboxes_account la' . "\r\n" . '        join mw_lootboxes b on b.id = la.lootbox_id' . "\r\n" . '        where la.account = ? ' . "\r\n" . '        and la.open_date is null' . "\r\n" . '        group by b.id' . "\r\n" . '    ', array(user()->getUsername()));
	$availables = array();

	foreach ($query as $box) {
		$availables[$box['id']] = $box['total'];
	}

	View::display('LootBox.index', array('boxes' => $boxes, 'availables' => $availables));
});
Route::get('/lootboxes/:id', authenticate(), plugin_license('LootBox'), function($id) {
	$box = Connection::fetchAssoc('select * ' . "\r\n" . '        from mw_lootboxes l' . "\r\n" . '        where l.id = ?' . "\r\n" . '        and l.active = 1' . "\r\n" . '        and (l.expire_date is null or l.expire_date > getdate())' . "\r\n" . '        and (select count(1) from mw_lootboxes_items where lootbox_id = l.id) > 0' . "\r\n" . '    ', array($id));

	if (empty($box)) {
		return App::notFound();
	}

	$items = Connection::fetchAll('select * ' . "\r\n" . '        from mw_lootboxes_items' . "\r\n" . '        where lootbox_id = ?' . "\r\n" . '        order by id' . "\r\n" . '    ', array($box['id']));
	View::display('LootBox.view', array('box' => $box, 'items' => $items));
});
Route::map('/lootboxes/gift/:id', authenticate(), plugin_license('LootBox'), function($id) {
	$box = Connection::fetchAssoc('select * ' . "\r\n" . '        from mw_lootboxes l' . "\r\n" . '        where l.id = ?' . "\r\n" . '        and l.active = 1' . "\r\n" . '        and (l.expire_date is null or l.expire_date > getdate())' . "\r\n" . '        and (select count(1) from mw_lootboxes_items where lootbox_id = l.id) > 0' . "\r\n" . '    ', array($id));

	if (empty($box)) {
		return error(__('blabla'));
	}

	if (Request::isPost()) {
		$post = Input::post();
		$validation = new Morpheus\Core\Validation($post, array(
	'account' => array(
		0                             => Morpheus\Core\Validation::REQUIRED,
		__('Username doen\'t exists') => array(
			'rule' => array('exists', 'MEMB_INFO', 'memb___id')
			)
		)
	));

		if ($validation->isValid()) {
			if (user()->getCoin($box['coin']) < $box['amount']) {
				return error(__('You do not have enough %s', array(util('Coin')->name($box['coin']))));
			}

			try {
				Connection::transactional(function($conn) use($box) {
					$conn->insert('mw_lootboxes_account', array('lootbox_id' => $box['id'], 'account' => Input::post('account'), 'gift_from' => user()->getUsername()), array('integer', 'string', 'string'));
					user()->addCoin($box['coin'], -$box['amount']);
					user()->update();
				});
				return success(__('gift has been sent'), array('partial' => 'login', 'redirect' => '/lootboxes/gift/' . $box['id']));
			}
			catch (Exception $ex) {
				return error($ex);
			}
		}
		else {
			return error($validation->getErrors());
		}
	}

	View::display('LootBox.gift', array('box' => $box));
})->via('GET', 'POST');
Route::get('/lootboxes/open/:id', authenticate(), plugin_license('LootBox'), function($id) {
	$box = Connection::fetchAssoc('select * ' . "\r\n" . '        from mw_lootboxes l' . "\r\n" . '        where l.id = ?' . "\r\n" . '        and l.active = 1' . "\r\n" . '        and (l.expire_date is null or l.expire_date > getdate())' . "\r\n" . '        and (select count(1) from mw_lootboxes_items where lootbox_id = l.id) > 0' . "\r\n" . '    ', array($id));

	if (empty($box)) {
		return error(__('blabla'));
	}

	$available = Connection::fetchAssoc('select * ' . "\r\n" . '        from mw_lootboxes_account' . "\r\n" . '        where account = ?' . "\r\n" . '        and open_date is null' . "\r\n" . '        order by create_date' . "\r\n" . '    ', array(user()->getUsername()));
	if (empty($available) && (user()->getCoin($box['coin']) < $box['amount'])) {
		return error(__('You do not have enough %s', array(util('Coin')->name($box['coin']))));
	}

	$items = Connection::fetchAll('select * ' . "\r\n" . '        from mw_lootboxes_items' . "\r\n" . '        where lootbox_id = ?' . "\r\n" . '        order by id' . "\r\n" . '    ', array($box['id']));
	$possibilities = $indexes = array();

	foreach ($items as $i => $item) {
		$possibilities[$item['id']] = $item['item_percent'];
		$indexes[$item['id']] = $i;
	}

	$itemid = randomw($possibilities);
	$idx = $indexes[$itemid];
	$it = Connection::fetchAssoc('select * ' . "\r\n" . '        from mw_lootboxes_items ' . "\r\n" . '        where id = ?' . "\r\n" . '    ', array($itemid));
	$section = $it['section'];
	$index = $it['idx'];
	$item = new Morpheus\Item\Item();
	$item->setSection($section)->setIndex($index);
	$levels = array_filter(json_decode($it['percent_levels'], true), function($arr) {
		return !empty($arr) && (0 < $arr);
	});

	if (!empty($levels)) {
		$item->setLevel(randomw($levels));
	}

	$options = array_filter(json_decode($it['percent_options'], true), function($arr) {
		return !empty($arr) && (0 < $arr);
	});

	if (!empty($options)) {
		$item->setOption(randomw($options));
	}

	if (!empty($it['percent_luck'])) {
		$luckp = array(100 - $it['percent_luck'], $it['percent_luck']);
		$item->setLuck((bool) randomw($luckp));
	}

	if (util('Item')->hasSkill($section, $index) && !empty($it['percent_skill'])) {
		$skillp = array(100 - $it['percent_skill'], $it['percent_skill']);
		$item->setSkill((bool) randomw($skillp));
	}

	$excellents = array_filter(json_decode($it['percent_exc'], true), function($arr) {
		return !empty($arr) && (0 < $arr);
	});

	foreach ($excellents as $ix => $percent) {
		$exp = array(100 - $percent, $percent);
		$item->addExcellent($ix - 1, (bool) randomw($exp));
	}

	if (util('Item')->hasRefine($section, $index) && !empty($it['percent_refine'])) {
		$refinep = array(100 - $it['percent_refine'], $it['percent_refine']);
		$item->setRefine((bool) randomw($refinep));
	}

	if (!empty($it['percent_harmony'])) {
		$harmonyp = array(100 - $it['percent_harmony'], $it['percent_harmony']);
		$randh = (bool) randomw($harmonyp);

		if ($randh) {
			$harmonys = util('Item')->getHarmonys($section);
			$type = $harmonys[array_rand($harmonys)];
			$level = $type['levels'][array_rand($type['levels'])];
			$item->setHarmonyType($type['index'])->setHarmonyLevel($level['index']);
		}
	}

	if (util('Item')->hasSockets($section, $index)) {
		$sockts = array_filter(json_decode($it['percent_sockets'], true), function($arr) {
			return !empty($arr) && (0 < $arr);
		});

		if (!empty($sockts)) {
			$sockets = util('Item')->getSockets($section);
			$availables = array();

			foreach ($sockets as $type => $socks) {
				foreach ($socks as $socket) {
					$availables[] = $socket['value'];
				}
			}

			foreach ($sockts as $ix => $percent) {
				$socketp = array(100 - $percent, $percent);

				if ((bool) randomw($socketp)) {
					$av = array_rand($availables);
					$item->addSocket($ix, $av);
					unset($availables[$av]);
				}
			}
		}
	}

	$defaults = $item->getDefaults();
	$durability = (isset($defaults['durability']) ? (255 < $defaults['durability'] ? 255 : $defaults['durability']) : 255);
	$item->setDurability($durability);
	$item->getSerial()->generate();
	$item->update();
	$warehouse = user()->getWarehouse();
	$warehouse->create();
	$warehouse->load();

	if (!$warehouse->addItem($item)) {
		return error(__('No space in your warehouse'));
	}

	try {
		Connection::transactional(function($conn) use($warehouse, $box, $item, $available) {
			if (empty($available)) {
				$conn->insert('mw_lootboxes_account', array('lootbox_id' => $box['id'], 'account' => user()->getUsername(), 'open_date' => date('Y-m-d'), 'item' => $item->generate()), array('integer', 'string', 'string', 'string'));
				user()->addCoin($box['coin'], -$box['amount']);
				user()->update();
			}
			else {
				$conn->update('mw_lootboxes_account', array('open_date' => date('Y-m-d'), 'item' => $item->generate()), array('id' => $available['id']));
			}

			$warehouse->update();
		});
		return success(__('Item ' . util('Item')->getFullName($item, false) . ' added to your warehouse'), array('index' => $idx, 'item' => $item->getHex()));
	}
	catch (Exception $ex) {
		return error($ex->getMessage());
	}
});

?>
