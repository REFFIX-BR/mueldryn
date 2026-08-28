<?php

Route::map('/admin/events', function () {
    $events = Connection::fetchAll("SELECT e.*, c.name as category_name FROM mw_events e LEFT JOIN mw_event_categories c ON e.category_id = c.id ORDER BY e.id DESC");
    View::display('Events.admin.events.index', array('title' => __('Events'), 'events' => $events));
});

Route::get('/admin/events/add', function () {
    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY name ASC");
    View::display('Events.admin.events.form', array('title' => __('Add new event'), 'action' => 'add', 'event' => null, 'categories' => $categories));
});

Route::post('/admin/events/add', function () {
    $name = Input::post('name');
    $category_id = (int) Input::post('category_id');
    $days = Input::post('days', array());
    $times = Input::post('times');
    $active = (int) Input::post('active', 1);

    Connection::insert('mw_events', array(
        'name' => $name,
        'category_id' => $category_id,
        'days' => json_encode(array_values($days)),
        'times' => $times,
        'active' => $active
    ));

    Flash::success(__('Saved successfully'));
    return Redirect::to('/admin/events');
});

Route::get('/admin/events/edit/:id', function ($id) {
    $event = Connection::fetchOne("SELECT * FROM mw_events WHERE id = ?", array($id));
    if (!$event) return App::notFound();

    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY name ASC");
    View::display('Events.admin.events.form', array('title' => 'Editar Evento', 'action' => 'edit', 'event' => $event, 'categories' => $categories));
});

Route::post('/admin/events/edit/:id', function ($id) {
    $event = Connection::fetchOne("SELECT * FROM mw_events WHERE id = ?", array($id));
    if (!$event) return App::notFound();

    $name = Input::post('name');
    $category_id = (int) Input::post('category_id');
    $days = Input::post('days', array());
    $times = Input::post('times');
    $active = (int) Input::post('active', 1);

    Connection::update('mw_events', array(
        'name' => $name,
        'category_id' => $category_id,
        'days' => json_encode(array_values($days)),
        'times' => $times,
        'active' => $active
    ), array('id' => $id));

    Flash::success(__('Saved successfully'));
    return Redirect::to('/admin/events');
});

Route::get('/admin/events/delete/:id', function ($id) {
    Connection::delete('mw_events', array('id' => $id));
    Flash::success(__('Deleted successfully'));
    return Redirect::to('/admin/events');
});

Route::map('/admin/events/categories', function () {
    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY name ASC");
    View::display('Events.admin.categories.index', array('title' => 'Categorias', 'categories' => $categories));
});

Route::get('/admin/events/categories/add', function () {
    View::display('Events.admin.categories.form', array('title' => 'Nova Categoria', 'action' => 'add', 'category' => null));
});

Route::post('/admin/events/categories/add', function () {
    Connection::insert('mw_event_categories', array(
        'name' => Input::post('name'),
        'color' => Input::post('color', '#3bafda')
    ));

    Flash::success(__('Saved successfully'));
    return Redirect::to('/admin/events/categories');
});

Route::get('/admin/events/categories/edit/:id', function ($id) {
    $category = Connection::fetchOne("SELECT * FROM mw_event_categories WHERE id = ?", array($id));
    if (!$category) return App::notFound();

    View::display('Events.admin.categories.form', array('title' => 'Editar Categoria', 'action' => 'edit', 'category' => $category));
});

Route::post('/admin/events/categories/edit/:id', function ($id) {
    $category = Connection::fetchOne("SELECT * FROM mw_event_categories WHERE id = ?", array($id));
    if (!$category) return App::notFound();

    Connection::update('mw_event_categories', array(
        'name' => Input::post('name'),
        'color' => Input::post('color', '#3bafda')
    ), array('id' => $id));

    Flash::success(__('Saved successfully'));
    return Redirect::to('/admin/events/categories');
});

Route::get('/admin/events/categories/delete/:id', function ($id) {
    Connection::delete('mw_event_categories', array('id' => $id));
    Flash::success(__('Deleted successfully'));
    return Redirect::to('/admin/events/categories');
});

Route::map('/admin/events/config', function () {
    if ($_SERVER['REQUEST_METHOD'] === 'POST') {
        Morpheus\Core\Config::save(array('events.timezone' => Input::post('timezone')));
        Flash::success(__('Saved successfully'));
        return Redirect::to('/admin/events/config');
    }

    View::display('Events.admin.config', array(
        'title' => __('Events plugin settings'),
        'timezones' => DateTimeZone::listIdentifiers()
    ));
});
