function clearAllInterval() {
    var interval_id = window.setInterval("", 9999); // Get a reference to the last
    // interval +1
    for (var i = 1; i < interval_id; i++)
        window.clearInterval(i);
}

function startTimers() {
    var $items = $('.auction-items .item');

    $items.each(function () {
        var $this = $(this);
        var $time = $this.find('.time');
        var end = parseInt($this.attr('data-time-end'), 10);
        var time = parseInt($this.attr('data-time'), 10);
        var now = parseInt($this.attr('data-now'), 10);

        if (time) {
            var diff = time - (now - end);

            var interval = setInterval(function () {
                if ((diff - 1) >= 0) {
                    var min = parseInt(diff / 60);
                    var seg = diff % 60;

                    if (min < 10) {
                        min = '0' + min;
                        min = min.substr(0, 2);
                    }

                    if (seg <= 9) {
                        seg = '0' + seg;
                    }

                    var print = min + ':' + seg;

                    $time.html(print);

                    diff--;
                } else {
                    clearInterval(interval);
                }
            }, 1000);
        }
    })
}

$(function () {
    if (use_pusher) {
        Pusher.logToConsole = true;
        var pusher = new Pusher(pusher_auth_key, {
            cluster: 'us2',
            forceTLS: true
        });

        var channel = pusher.subscribe('auction');
        channel.bind('bid', function (data) {
            clearAllInterval();

            var id = data.id;
            var $item = $('.auction-items [data-id=' + id + ']');

            if ($item.size() > 0) {
                $item.attr('data-time', data.best_bid_datetime);
                $item.attr('data-now', (new Date()).getTime() / 1000);
            }

            startTimers();
        });

        channel.bind('close', function (data) {
            clearAllInterval();

            var id = data.id;
            var $item = $('.auction-items [data-id=' + id + ']');

            if ($item.size() > 0) {
                $item.remove();
            }

            startTimers();
        });
    }

    startTimers();
    $(document).on("pjax:end", function() {
        if (window.location.pathname.indexOf('/auctions/items') >= 0) {
            clearAllInterval();
            startTimers();
        }
    });

    $(document).on('click', '.auction-bid', function (e) {
        var $this = $(this);

        $.getJSON($.morpheus.base_path + 'auction/items/info/' + $this.data('id'), function (data) {
            var info = data.text_bid;
            info += '<br /><br />' + data.text_initial_bid + ': ' + data.initial_bid + ' ' + data.coin;
            info += '<br />' + data.text_best_bid + ': ' + data.best_bid + ' ' + data.coin + '<br />';

            $.morpheus.prompt(info, '', function (value) {
                if (value) {
                    $.morpheus.overlay.create();
                    $.post($this.attr('href'), {
                        'bid': value,
                        '_token': $('meta[name="csrf-token"]').attr('content')
                    }, function (res) {
                        $.morpheus.overlay.destroy();

                        if (res.success) {
                            $.morpheus.success(res.message);
                        } else {
                            $.morpheus.alert(res.message);
                        }

                        if (res.partial) {
                            loadPartial(res.partial);
                        }

                        if (res.redirect) {
                            $.pjax({
                                url: $.morpheus.base_path.substring(0, $.morpheus.base_path.length - 1) + res.redirect,
                                container: ".ajax-container",
                                timeout: 0
                            });
                        }
                    });
                }
            });

            $('#morpheus-popup-prompt').attr('type', 'text');
        });

        e.preventDefault();
    })
});