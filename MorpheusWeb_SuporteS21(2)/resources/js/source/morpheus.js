function runCollapse() {
    $("[data-collapse]").click(function (e) {
        var $this = $(this);
        var $collapse = $($this.data("collapse"));
        if ($collapse.is(":visible")) {
            $collapse.hide();
        } else {
            $collapse.show();
        }
        e.preventDefault();
    });
}

function runTooltip()
{
    $("[data-tooltip]").each(function () {
        var $this = $(this);
        $this.attr("title", "loading...");
        $this.powerTip({
            popupId: "morpheus-tooltip",
            followMouse: true
        }).on("powerTipRender", function () {
            var xhr = $.ajax({
                url: $this.attr("data-tooltip-url"),
                type: "GET"
            });
            xhr.done(function (response) {
                $("#morpheus-tooltip").html(response);
            });
        });
    });
}

function loadPartial(partial) {
    var $partial = $(".partial-" + partial);
    $partial.append('<div class="loading-partial"></div>');
    $.get($.morpheus.base_path + "partial/" + partial, function (html) {
        $partial.html(html);
    });
}

function initAfterLoadPage() {
    runCollapse();
    runTooltip();
}

$(function () {
    initAfterLoadPage();

    $.pjax.defaults.scrollTo = false;

    $.morpheus.form("form[data-ajax]", {
        onInit: function (form) {
            var $submit = form.find(":submit");
            $submit.attr("disabled", true);
            $.morpheus.overlay.create();
        },
        onError: function (data, form) {
            var $submit = form.find(":submit");

            if (data.error) {
                $.morpheus.alert(data.error)
            } else {
                if (data.message) {
                    $.morpheus.alert(data.message)
                }
            }

            if (data.required_pid) {
                $.morpheus.prompt("Informe seu Peronsal ID", "", function (value) {
                    if (value != null) {
                        var $pid = form.find("[name=pid]");
                        $pid.val(value);
                        form.submit();
                        $pid.val("");
                    }
                });
            }

            $submit.attr("disabled", false);
            $.morpheus.overlay.destroy();
        },
        onSuccess: function (data, form) {
            var $submit = form.find(":submit");

            if (data.force_redirect) {
                window.location = data.force_redirect;
                return;
            }

            if (data.message) {
                $(".form-action :text, .form-action textarea").val("");
                $.morpheus.success(data.message);
            }

            if (data.partial) {
                loadPartial(data.partial);
            }

            if (data.redirect) {
                $.pjax({url: $.morpheus.base_path.substring(0, $.morpheus.base_path.length - 1) + data.redirect, container: ".ajax-container", timeout:0});
            }

            $.morpheus.overlay.destroy();
            $submit.attr("disabled", false);
        },
        onInternalError: function (data, form) {
            var $submit = form.find(":submit");
            var json = data.responseJSON;

            if (json.error) {
               $.morpheus.alert(json.error.message)
            }

            $.morpheus.overlay.destroy();
            $submit.attr("disabled", false);
        },
        onConfirmation: function (message, request, form) {
            var $submit = form.find(":submit");
            $submit.attr("disabled", false);
            $.morpheus.overlay.destroy();
            $.morpheus.confirm(message, function (action) {
                if (action) {
                    $.morpheus.overlay.create();
                    request();
                }
            });
        }
    });

    $.morpheus.form("form[data-ajax-login]", {
        onInit: function (form) {
            var $submit = form.find(":submit");
            var $loading = form.find(".loading-login");
            $submit.hide();
            $loading.show();
        },
        onError: function (data, form) {
            var $submit = form.find(":submit");
            var $loading = form.find(".loading-login");

            if (data.message) {
                $.morpheus.alert(data.message);
            }

            $loading.hide();
            $submit.show();
        },
        onSuccess: function (data, form) {
            if (data.partial) {
                loadPartial(data.partial);
            }

            if (data.redirect) {
                $.pjax({url: $.morpheus.base_path.substring(0, $.morpheus.base_path.length - 1) + data.redirect, container: ".ajax-container", timeout:0});
            }
        }
    });

    $(document).on("click", "[data-ajax-logout]", function (e) {
        e.preventDefault();
        var $this = $(this);
        $.morpheus.overlay.create();

        $.getJSON($this.attr("href"), function (data) {
            if (data.partial) {
                loadPartial(data.partial);
            }

            if (data.redirect) {
                $.pjax({url: $.morpheus.base_path.substring(0, $.morpheus.base_path.length - 1) + data.redirect, container: ".ajax-container", timeout:0});
            }
        });
    });

	$(document).pjax("a[data-ajax], a[data-pagination]", ".ajax-container", {timeout: 0})
		.on("pjax:end", function() {
			initAfterLoadPage();
		}).on("pjax:send", function(e) {
		  	$.morpheus.overlay.create(e.target);
		}).on("pjax:complete", function (e, j) {
            $.morpheus.overlay.destroy();
		}).on("pjax:error", function (event, xhr) {
            if (xhr.status === 401) {
                var data = JSON.parse(xhr.responseText);
                $.morpheus.alert(data.error);
                if (data.partial) {
                    loadPartial(data.partial);
                }
                return false;
            }
		    return true;
        });

    $(document).on("click", "[data-ajax-action]", function (e) {
        var $this = $(this);

        if ($this.hasClass("disabled")) {
            return;
        }

        if ($this.data("confirm")) {
            $.morpheus.confirm($this.data("confirm"), function (ok) {
                if (ok) {
                    sendRequest();
                }
            });
        } else {
            sendRequest();
        }

        function sendRequest() {
            $.morpheus.overlay.create();
            $this.addClass("disabled");
            $.get($this.attr("href"), function (data) {
                $.morpheus.overlay.destroy();
                if (data.confirm) {
                    $.morpheus.confirm(data.message, function (ok) {
                        if (ok) {
                            $.morpheus.overlay.create();
                            $.get($this.attr("href") + "?accept", function (json) {
                                $.morpheus.overlay.destroy();
                                processRequest(json);
                            });
                        }
                    });
                } else {
                    processRequest(data);
                }
                $this.removeClass("disabled");
            });

            function processRequest(data) {
                if (data.success) {
                    if (data.partial) {
                        loadPartial(data.partial);
                    }

                    if (data.redirect) {
                        $.pjax({url: $.morpheus.base_path.substring(0, $.morpheus.base_path.length - 1) + data.redirect, container: ".ajax-container", timeout:0});
                    }

                    $.morpheus.success(data.message);
                } else {
                    $.morpheus.alert(data.message);
                }
            }
        }

        e.preventDefault();
    });

    $(document).on("click", ".reload-captcha", function (e) {
        var $this = $(this);
        var $container = $this.closest(".captcha");
        $container.find("img").attr("src", $.morpheus.base_path + "captcha?t=" + new Date().getTime());
        e.preventDefault();
    });
});