<?php

Route::get("/events", function () {
    // Puxa apenas os eventos que estão ativos no painel
    $events = Connection::fetchAll("SELECT e.*, c.name as category_name, c.color as category_color FROM mw_events e LEFT JOIN mw_event_categories c ON e.category_id = c.id WHERE e.active = 1 ORDER BY e.id DESC");
    
    $currentDay = (int)date('w');
    $currentTime = date('H:i');

    foreach ($events as &$event) {
        $event['days'] = json_decode($event['days'], true);
        if (empty($event['days']) || !is_array($event['days'])) {
            $event['days'] = array("0", "1", "2", "3", "4", "5", "6");
        }
        
        $times = array_map('trim', explode(',', $event['times']));
        sort($times);
        
        $nextTimestamp = null;
        
        for ($i = 0; $i <= 7; $i++) {
            $checkDay = ($currentDay + $i) % 7;
            if (in_array((string)$checkDay, $event['days'])) {
                $checkDate = date('Y-m-d', strtotime("+$i days"));
                foreach ($times as $t) {
                    if (empty($t)) continue;
                    if ($i == 0 && $t <= $currentTime) continue; // Pula horários que já passaram hoje
                    
                    $ts = strtotime("$checkDate $t");
                    if ($nextTimestamp === null || $ts < $nextTimestamp) {
                        $nextTimestamp = $ts;
                        break;
                    }
                }
                if ($nextTimestamp !== null) break;
            }
        }
        $event['next_timestamp'] = $nextTimestamp;
    }
    
    // Ordena para os eventos mais próximos de começar ficarem no topo da lista
    usort($events, function($a, $b) {
        return $a['next_timestamp'] - $b['next_timestamp'];
    });

    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY name ASC");
    
    // Chama a view do nosso plugin
    View::display("../../../plugins/Events/views/public/index", array("events" => $events, "categories" => $categories));
})->name("events.list");