$.morpheus.events = {};

function toSeconds(hour, minute, second) {
    return hour * 3600 + minute * 60 + second;
}

function toDateUTC(hours) {
    const d = new Date();
    const utc = d.getTime() + (d.getTimezoneOffset() * 60000);

    return new Date(utc + (3600000 * hours));
}

function getCookieEvents() {
    let events = $.cookie('events');

    return !events ? [] : events.split(',');
}

function getCheckbox(event, spotlight) {
    const cookieEvents = getCookieEvents();
    const templateCheckbox = $.morpheus.events.template_checkbox = [
        '<label class="switch">',
        '<input type="checkbox" class="notify-event" value="{value}"{checked} />',
        '<span class="switch-slider"></span>',
        '</label>'
    ].join('');

    if (!spotlight) {
        const checked = cookieEvents.indexOf(event.id.toString()) >= 0;
        return templateCheckbox.replace('{checked}', checked ? ' checked' : '')
            .replace('{value}', event.id.toString());
    }

    return '';
}

function removeElement(arr) {
    let what, a = arguments, L = a.length, ax;
    while (L > 1 && arr.length) {
        what = a[--L];
        while ((ax= arr.indexOf(what)) !== -1) {
            arr.splice(ax, 1);
        }
    }
    return arr;
}

function updateEventsTime(events, timezone, spotlight) {
    const date = toDateUTC(timezone);
    const time = toSeconds(date.getHours(), date.getMinutes(), date.getSeconds());

    const template = $.morpheus.events[spotlight ? 'template_spotlight' : 'template'] || [
        '<div class="event">',
        '<span class="name">',
        '{checkbox} {name}',
        '</span>',
        '<span class="time {class}">{countdown}</span>',
        '</div>'
    ].join('');
    const classDefault = $.morpheus.events.class_default || 'label-default';
    const classSuccess = $.morpheus.events.class_success || 'bg-success';
    const cookieEvents = getCookieEvents();

    let html = {};
    for (let i = 0; i < events.length; i++) {
        const event = events[i];

        const category = event.category_id === null ? '*' : event.category_id;
        let df = -1;

        if (spotlight && !event.spotlight) continue;

        if (html[category] === undefined) {
            html[category] = [];
        }

        if (event.type === 1) {
            let nextEvent = null;
            let nextTime = null;
            let timesMap = {};
            for (let j = 0; j < event.times.length; j++) {
                const eventTime = event.times[j];
                const parts = eventTime.split(':');
                const t = toSeconds(parts[0], parts[1], 0);

                timesMap[t] = eventTime;

                if (t > time) {
                    nextEvent = eventTime;
                    nextTime = t;
                    break;
                }
            }

            if (nextEvent === null) {
                nextEvent = Object.values(timesMap)[0];
                nextTime = Object.keys(timesMap)[0];
            }

            let diff = nextTime - time;
            if (diff < 0) diff += 3600 * 24;

            df = diff;

            let klass = classDefault;
            if (diff < 15 * 60) klass = classSuccess;

            const hour = parseInt(diff / 3600);
            diff -= 3600 * hour;
            const minute = parseInt(diff / 60);
            const second = diff - minute * 60;
            const countdown = ('0' + hour).slice(-2) + ':' + ('0' + minute).slice(-2) + ':' + ('0' + second).slice(-2);

            html[category].push(template.replace('{checkbox}', getCheckbox(event, spotlight))
                .replace('{name}', nextEvent + ' - ' + event.name)
                .replace('{class}', klass)
                .replace('{countdown}', countdown));

        } else if (event.type === 2) {
            let week = date.getDay();
            let counter = 0;
            let next = [0, 0, 0, 0, 0, 0, 0];
            let hoursText = [];

            for (let day in event.times) {
                const hrs = event.times[day];
                const w = day - 1;

                for (let h in hrs) {
                    let hour = hrs[h].split(':');
                    let ht = 0;
                    if (hour[0] === '00' && hour[1] === '00') {
                        ht = toSeconds(hour[0], hour[1], 1);
                    } else {
                        ht = toSeconds(hour[0], hour[1], 0);
                    }

                    if (next[w] === 0 || time - ht <= 0) {
                        next[w] = ht;
                        hoursText[w] = hrs;

                        break;
                    }
                }
            }

            for (; 1;) {
                if (next[week]
                    && (
                        week === date.getDay()
                        && next[week] > time
                        || week !== date.getDay()
                    )
                ) {
                    let dayOfWeek = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sab'][week];
                    if (week === date.getDay()) {
                        dayOfWeek = '';
                    }

                    let diff = week * 24 * 60 * 60 + next[week] - (date.getDay() * 24 * 60 * 60 + time);
                    if (diff < 0) {
                        diff = diff + 7 * 24 * 60 * 60;
                    }

                    df = diff;

                    let klass = classDefault;
                    if (diff < 60 * 60) klass = classSuccess;

                    const ht = parseInt(diff / (24 * 60 * 60));
                    diff = diff - ht * (24 * 60 * 60);
                    const hour = parseInt(diff / 3600);
                    diff = diff - 3600 * hour;
                    const minute = parseInt(diff / 60);
                    const second = diff - minute * 60;

                    let text = '';
                    if (ht) {
                        text = ht + "d " + (hour === 0 ? minute + 'm' : hour + 'h');
                    } else {
                        text = ('0' + hour).slice(-2) + ':' + ('0' + minute).slice(-2) + ':' + ('0' + second).slice(-2);
                    }

                    html[category].push(template.replace('{checkbox}', getCheckbox(event, spotlight))
                        .replace('{name}', event.name)
                        .replace('{class}', klass)
                        .replace('{countdown}', text));

                    break;
                }
                if (counter++ > 8) {
                    html[category].push(template.replace('{checkbox}', getCheckbox(event, spotlight))
                        .replace('{name}', event.name)
                        .replace('{class}', classDefault)
                        .replace('{countdown}', '7d'));
                    break;
                }
                week = (week + 1) % 7;
            }
        } else {
            html[category].push(template.replace('{checkbox}', '')
                .replace('{name}', event.name)
                .replace('{class}', '')
                .replace('{countdown}', event.times));
        }

        if (!spotlight) {
            if (df === 60 * 5 && cookieEvents.indexOf(event.id.toString()) >= 0) {
                notifyMe('Evento próximo', event.name + ' em 5 minutos.')
            }
        }
    }

    for (let category in html) {
        let evts = html[category];

        $((spotlight ? '.events-spotlight' : '.events') + (category === '*' ? '' : '-' + category)).html(evts.join(''));
    }

}

function notifyMe(title, body) {
    if (!window.Notification) {
        return;
    }
    if (Notification.permission !== 'granted') {
        Notification.requestPermission();
    } else {
        const n = new Notification($.morpheus.server_name + ' - ' + title, {
            icon: $.morpheus.logo,
            body: body
        });
        n.onclick = function() {
            window.focus();
        }
    }
}

$(function () {
    if (window.Notification && Notification.permission !== 'granted') {
        Notification.requestPermission();
    }

    $('#event-category').change(function () {
        const $this = $(this);
        if ($this.val() === '*') {
            $('.events').show();
        } else {
            $('.events').hide();
            $('.events-' + $this.val()).show();
        }
    });

    $('#event-spotlight-category').change(function () {
        const $this = $(this);
        if ($this.val() === '*') {
            $('.events-spotlight').show();
        } else {
            $('.events-spotlight').hide();
            $('.events-spotlight-' + $this.val()).show();
        }
    });

    $.getJSON($.morpheus.base_path + 'events?json=true', function (data) {
        const events = data.events;
        let cookieEvents = getCookieEvents();

        $(document).on('click', '.notify-event', function () {
            const $this = $(this);
            const value = $this.val();

            if ($this.is(':checked') && $.inArray(value, cookieEvents) === -1) {
                cookieEvents.push(value);
            } else {
                cookieEvents = removeElement(cookieEvents, value);
            }

            $.cookie('events', cookieEvents.join(','), {
                expires: 365
            });
        });

        updateEventsTime(events, data.timezone, true);
        setInterval(function () {updateEventsTime(events, data.timezone, true)}, 1000);

        updateEventsTime(events, data.timezone, false);
        setInterval(function () {updateEventsTime(events, data.timezone, false)}, 1000);
    });
});