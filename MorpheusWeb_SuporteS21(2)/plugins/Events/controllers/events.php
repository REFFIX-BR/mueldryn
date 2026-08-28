<?php

function events_next_timestamp($event, $timezone)
{
    $days = json_decode($event['days'], true);
    if (!is_array($days)) $days = array();
    $times = array_filter(array_map('trim', explode(',', $event['times'])));

    if (empty($times)) return null;

    $now = new DateTime('now', new DateTimeZone($timezone));
    $currentDay = (int) $now->format('w');
    $currentTime = $now->format('H:i');

    $candidates = array();

    for ($offset = 0; $offset < 8; $offset++) {
        $checkDay = ($currentDay + $offset) % 7;

        if (!empty($days) && !in_array((string) $checkDay, $days) && !in_array($checkDay, $days)) {
            continue;
        }

        foreach ($times as $time) {
            if (!preg_match('/^\d{1,2}:\d{2}$/', $time)) continue;

            if ($offset === 0 && $time <= $currentTime) continue;

            $dt = clone $now;
            $dt->modify("+{$offset} days");
            $dt->setTime((int) substr($time, 0, strpos($time, ':')), (int) substr($time, strpos($time, ':') + 1), 0);
            $candidates[] = $dt->getTimestamp();
        }
    }

    if (empty($candidates)) return null;

    sort($candidates);
    return $candidates[0];
}

function events_format_for_js($event)
{
    $days = json_decode($event['days'], true);
    if (!is_array($days)) $days = array();
    $times = array_filter(array_map('trim', explode(',', $event['times'])));

    $formatted = array(
        'id' => (int) $event['id'],
        'name' => $event['name'],
        'category_id' => $event['category_id'] !== null ? (int) $event['category_id'] : null,
        'spotlight' => true,
        'active' => (bool) $event['active']
    );

    if (empty($days)) {
        $formatted['type'] = 1;
        $formatted['times'] = array_values($times);
    } else {
        $formatted['type'] = 2;
        $formatted['times'] = array();
        foreach ($days as $day) {
            $formatted['times'][(int) $day + 1] = array_values($times);
        }
    }

    return $formatted;
}

function events_get_data($timezone)
{
    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY name ASC");
    $events = Connection::fetchAll("SELECT e.*, c.name as category_name, c.color as category_color FROM mw_events e LEFT JOIN mw_event_categories c ON e.category_id = c.id WHERE e.active = 1 ORDER BY c.name ASC, e.name ASC");

    foreach ($events as &$event) {
        $event['next_timestamp'] = events_next_timestamp($event, $timezone);
    }

    return array('categories' => $categories, 'events' => $events);
}

Route::get('/events', function () {
    $timezone = config('events.timezone', date_default_timezone_get());

    if (Input::get('json') === 'true') {
        $events = Connection::fetchAll("SELECT * FROM mw_events WHERE active = 1");
        $jsEvents = array();
        foreach ($events as $event) {
            $jsEvents[] = events_format_for_js($event);
        }

        $dtz = new DateTimeZone($timezone);
        $utc = new DateTimeZone('UTC');
        $now = new DateTime('now', $utc);
        $offset = $dtz->getOffset($now) / 3600;

        header('Content-Type: application/json');
        echo json_encode(array('events' => $jsEvents, 'timezone' => $offset));
        exit;
    }

    $data = events_get_data($timezone);
    View::display('Events.index', array(
        'title' => __('Events'),
        'categories' => $data['categories'],
        'events' => $data['events']
    ));
});

App::hook('events.spotlight', function () {
    $timezone = config('events.timezone', date_default_timezone_get());
    $data = events_get_data($timezone);
    View::display('Events.spotlight', array(
        'layout' => false,
        'categories' => $data['categories'],
        'events' => $data['events']
    ));
}, 30);

app()->view()->add('css', array('Events.events'));
app()->view()->add('script', array('Events.events', 'Events.jquery.cookie'));
