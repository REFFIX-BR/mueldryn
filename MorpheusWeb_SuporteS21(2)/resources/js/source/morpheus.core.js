(function ($) {
    $.morpheus = {
        base_path: "/",
        use_bootstrap: false,

        form: function (selector, options) {
            var defaults = {
                onSuccess: function () {},
                onInit: function () {},
                onError: function () {},
                onInternalError: function () {},
                onConfirmation: function (message, request) { if (confirm(message)) { request(); }}
            };

            options = $.extend(defaults, options);

            function clearErrorFields(form) {
                if ($.morpheus.use_bootstrap) {
                    form.find("input.is-invalid, textarea.is-invalid, select.is-invalid").each(function () {
                        $(this).removeClass("is-invalid").closest(".form-control").removeClass("has-danger").find(".invalid-feedback").remove();
                    });
                } else {
                    form.find("input.error, textarea.error, select.error").each(function () {
                        $(this).removeClass("error").parent().removeClass("error").find(".error-message").remove();
                    });
                }
            }

            $(document).on("submit", selector, function (e) {
                var _this = $(this);
                options.onInit(_this);

                var success = function (data) {
                    if (data.success) {
                        options.onSuccess(data, _this);
                        clearErrorFields(_this);
                    } else {
                        options.onError(data, _this);
                        clearErrorFields(_this);
                        if (data.errors) {
                            $.each(data.errors, function (k, v) {
                                var field = _this.find('[name="' + k + '"]');
                                if (field.size() > 0) {
                                    if ($.morpheus.use_bootstrap) {
                                        field.parent().addClass("has-danger")
                                        if (field.next().is(".invalid-feedback")) {
                                            field.next().html(v);
                                        } else {
                                            field.after('<div class="invalid-feedback">' + v + '</div>');
                                        }
                                        field.addClass("is-invalid");
                                        _this.find(":input.is-invalid:first").focus();
                                    } else {
                                        field.parent().addClass("error");
                                        if (field.next().is("div.error-message")) {
                                            field.next("label").html(v);
                                        } else {
                                            field.after('<div class="error-message"><label for="' + field.attr('id') + '">' + v + '</label></div>');
                                        }
                                        field.addClass("error");
                                        _this.find(":input.error:first").focus();
                                    }
                                }
                            });
                        }
                    }
                };

                var confirmation = _this.attr("data-confirmation");

                if (confirmation) {
                    options.onConfirmation(confirmation, sendRequest, _this);
                } else {
                    sendRequest();
                }

                function sendRequest() {
                    var attr = _this.attr("data-upload");
                    if (typeof attr !== typeof undefined && attr !== false) {
                        var $fields = _this.find("[data-upload-field]");
                        var data = new FormData(_this[0]);

                        $fields.each(function () {
                            var $field = $(this);
                            var files = $field[0].files;
                            if (files.length > 0) {
                                $.each(files, function (ix, file) {
                                    data.append($field.attr("name") + "[" + ix + "]", file);
                                });
                            } else {
                                data.append($field.attr("name"), "");
                            }
                        });

                        $.ajax({
                            url: _this.attr("action"),
                            dataType: "json",
                            cache: false,
                            contentType: false,
                            processData: false,
                            data: data,
                            type: "post",
                            success: function (data) {
                                success(data);
                            },
                            error: function (data) {
                                options.onInternalError(data, _this);
                            }
                        });
                    } else {
                        $.post(_this.attr("action"), _this.serialize(), function (data) {
                            success(data);
                        }, "json")
                            .error(function (data) {
                                options.onInternalError(data, _this);
                            });
                    }
                }

                e.preventDefault();
            });
        },

        alert: function (message, callback) {
            jAlert(message, null, callback);
        },

        confirm: function (message, callback) {
            jConfirm(message, null, callback);
        },

        success: function (message, callback) {
            jSuccess(message, null, callback);
        },

        prompt: function(message, value, callback) {
            jPrompt(message, value, null, callback);
        }
    };

    $.morpheus.overlay = {
        target: ".ajax-container",
        template: '<div class="overlay"><div class="progress progress-striped active"><div class="bar" style="width:100%"></div></div></div>',

        obj: null,

        create: function (target) {
            if (this.obj !== null) {
                this.destroy();
            }
            var $el = $($.morpheus.overlay.template);
            $(target || $.morpheus.overlay.target).append($el);
            this.obj = $el;
        },

        destroy: function () {
            if (this.obj !== null) {
                this.obj.remove();
                this.obj = null;
            }
        }
    }

    $.morpheus.alerts = {
        verticalOffset: -75,
        horizontalOffset: 0,
        repositionOnResize: true,
        overlayOpacity: .5,
        overlayColor: "#000000",
        okButton: "OK",
        cancelButton: "Cancel",
        dialogClass: null,

        alert: function (message, title, callback) {
            if (title == null) {
                title = "Erro :(";
            }

            $.morpheus.alerts._show(title, message, null, "alert", function (result) {
                if (callback) {
                    callback(result);
                }
            });
        },

        success: function (message, title, callback) {
            if (title == null) {
                title = "Sucesso :)";
            }
            $.morpheus.alerts._show(title, message, null, "success", function (result) {
                if (callback) {
                    callback(result);
                }
            });
        },

        confirm: function (message, title, callback) {
            if (title == null) {
                title = "Confirmação";
            }
            $.morpheus.alerts._show(title, message, null, "confirm", function (result) {
                if (callback) {
                    callback(result);
                }
            });
        },

        prompt: function (message, value, title, callback) {
            if (title == null) {
                title = "Confirmação";
            }
            $.morpheus.alerts._show(title, message, value, "prompt", function(result) {
                if (callback) {
                    callback(result);
                }
            });
        },

        // Private methods
        _show: function(title, msg, value, type, callback) {

            $.morpheus.alerts._hide();
            $.morpheus.alerts._overlay("show");

            $("body").append(
                '<div id="morpheus-popup-container">' +
                // '<div id="popup-title"></div>' +
                '<div class="morpheus-popup-content">' +
                '<div class="morpheus-popup-message"></div>' +
                '</div>' +
                '</div>');

            var $container = $("#morpheus-popup-container");

            if ($.morpheus.alerts.dialogClass) {
                $container.addClass($.morpheus.alerts.dialogClass);
            }

            $container.css({
                position: "fixed",
                zIndex: 9999999,
                padding: 0,
                margin: 0
            });

            $container.find(".morpheus-popup-title").text(title);
            $container.find(".morpheus-popup-content").addClass("morpheus-popup-" + type);
            $container.find(".morpheus-popup-message").html(msg.replace(/\n/g, '<br />'));

            $container.css({
                minWidth: $container.outerWidth(),
                maxWidth: $container.outerWidth()
            });

            $("#popup-title-container #popup-title").css({
                width: $("#popup-container").outerWidth() - 20
            })

            $.morpheus.alerts._reposition();
            $.morpheus.alerts._maintainPosition(true);

            switch (type) {
                case "alert":
                    $container.find(".morpheus-popup-message").after('<div class="morpheus-popup-panel"><a href="#" class="morpheus-popup-btn" id="morpheus-popup-ok">' + $.morpheus.alerts.okButton + '</a></div>');
                    $container.find("#morpheus-popup-ok").click(function(e) {
                        e.preventDefault();
                        $.morpheus.alerts._hide();
                        callback(true);
                    });
                    break;
                case "success":
                    $container.find(".morpheus-popup-message").after('<div class="morpheus-popup-panel"><a href="#" class="morpheus-popup-btn" id="morpheus-popup-ok">' + $.morpheus.alerts.okButton + '</a></div>');
                    $("#morpheus-popup-ok").click( function(e) {
                        e.preventDefault();
                        $.morpheus.alerts._hide();
                        callback(true);
                    });
                    break;
                case "confirm":
                    $container.find(".morpheus-popup-message").after('<div class="morpheus-popup-panel"> <a href="#" class="morpheus-popup-btn" id="morpheus-popup-ok">' + $.morpheus.alerts.okButton + '</a> <a href="#" class="morpheus-popup-btn" id="morpheus-popup-cancel">' + $.morpheus.alerts.cancelButton + '</a></div>');
                    $("#morpheus-popup-ok").click( function(e) {
                        e.preventDefault();
                        $.morpheus.alerts._hide();
                        if (callback) {
                            callback(true);
                        }
                    });
                    $("#morpheus-popup-cancel").click( function(e) {
                        e.preventDefault();
                        $.morpheus.alerts._hide();
                        if (callback) {
                            callback(false);
                        }
                    });
                    break;
                case "prompt":
                    $container.find(".morpheus-popup-message").append('<br /><input type="password" size="30" id="morpheus-popup-prompt" />').after('<div class="morpheus-popup-panel"><a href="#" class="morpheus-popup-btn" id="morpheus-popup-ok">' + $.morpheus.alerts.okButton + '</a> <a href="#" class="morpheus-popup-btn" id="morpheus-popup-cancel">' + $.morpheus.alerts.cancelButton + '</a></div>');
                    $("#morpheus-popup-prompt").width($container.find(".morpheus-popup-message").width() - 14);
                    $("#morpheus-popup-ok").click(function(e) {
                        e.preventDefault();
                        var val = $("#morpheus-popup-prompt").val();
                        $.morpheus.alerts._hide();
                        if (callback) {
                            callback(val);
                        }
                    });
                    $("#morpheus-popup-cancel").click(function(e) {
                        e.preventDefault();
                        $.morpheus.alerts._hide();
                        if (callback) {
                            callback(null);
                        }
                    });

                    if (value) {
                        $("#morpheus-popup-prompt").val(value);
                    }
                    $("#morpheus-popup-prompt").focus().select();
                    break;
            }
        },

        _hide: function() {
            $("#morpheus-popup-container").remove();
            $.morpheus.alerts._overlay("hide");
            $.morpheus.alerts._maintainPosition(false);
        },

        _overlay: function(status) {
            switch (status) {
                case "show":
                    $.morpheus.alerts._overlay("hide");
                    $("body").append('<div id="morpheus-popup-overlay"></div>');
                    $("#morpheus-popup-overlay").css({
                        position: "absolute",
                        zIndex: 9999998,
                        top: 0,
                        left: 0,
                        width: "100%",
                        height: $(document).height(),
                        background: $.morpheus.alerts.overlayColor,
                        opacity: $.morpheus.alerts.overlayOpacity
                    });
                    break;
                case "hide":
                    $("#morpheus-popup-overlay").remove();
                    break;
            }
        },

        _reposition: function() {
            var $container = $("#morpheus-popup-container");
            var top = (($(window).height() / 2) - ($container.outerHeight() / 2)) + $.morpheus.alerts.verticalOffset;
            var left = (($(window).width() / 2) - ($container.outerWidth() / 2)) + $.morpheus.alerts.horizontalOffset;
            if (top < 0) top = 0;
            if (left < 0) left = 0;

            $container.css({
                top: top + "px",
                left: left + "px"
            });
            $("#morpheus-popup-overlay").height($(document).height());
        },

        _maintainPosition: function(status) {
            if( $.morpheus.alerts.repositionOnResize ) {
                switch(status) {
                    case true:
                        $(window).bind('resize', function() {
                            $.morpheus.alerts._reposition();
                        });
                        break;
                    case false:
                        $(window).unbind('resize');
                        break;
                }
            }
        }

    }

    // Shortuct functions
    jAlert = function(message, title, callback) {
        $.morpheus.alerts.alert(message, title, callback);
    }

    jConfirm = function(message, title, callback) {
        $.morpheus.alerts.confirm(message, title, callback);
    };

    jSuccess = function(message, title, callback) {
        $.morpheus.alerts.success(message, title, callback);
    };

    jPrompt = function(message, value, title, callback) {
        $.morpheus.alerts.prompt(message, value, title, callback);
    };
})(jQuery);

/*!
 PowerTip v1.3.0 (2017-01-15)
 https://stevenbenner.github.io/jquery-powertip/
 Copyright (c) 2017 Steven Benner (http://stevenbenner.com/).
 Released under MIT license.
 https://raw.github.com/stevenbenner/jquery-powertip/master/LICENSE.txt
*/
!function(a,b){"function"==typeof define&&define.amd?define(["jquery"],b):"object"==typeof module&&module.exports?module.exports=b(require("jquery")):b(a.jQuery)}(this,function(a){function b(){var b=this;b.top="auto",b.left="auto",b.right="auto",b.bottom="auto",b.set=function(c,d){a.isNumeric(d)&&(b[c]=Math.round(d))}}function c(a,b,c){function d(d,e){g(),a.data(u)?h():d?(e&&a.data(v,!0),i(),c.showTip(a)):(E.tipOpenImminent=!0,k=setTimeout(function(){k=null,f()},b.intentPollInterval))}function e(d){l&&(l=E.closeDelayTimeout=clearTimeout(l),E.delayInProgress=!1),g(),E.tipOpenImminent=!1,a.data(u)&&(a.data(v,!1),d?c.hideTip(a):(E.delayInProgress=!0,E.closeDelayTimeout=setTimeout(function(){E.closeDelayTimeout=null,c.hideTip(a),E.delayInProgress=!1,l=null},b.closeDelay),l=E.closeDelayTimeout))}function f(){var e=Math.abs(E.previousX-E.currentX),f=Math.abs(E.previousY-E.currentY),g=e+f;g<b.intentSensitivity?(h(),i(),c.showTip(a)):(E.previousX=E.currentX,E.previousY=E.currentY,d())}function g(a){k=clearTimeout(k),(E.closeDelayTimeout&&l===E.closeDelayTimeout||a)&&h()}function h(){E.closeDelayTimeout=clearTimeout(E.closeDelayTimeout),E.delayInProgress=!1}function i(){E.delayInProgress&&E.activeHover&&!E.activeHover.is(a)&&E.activeHover.data(t).hide(!0)}function j(){c.resetPosition(a)}var k=null,l=null;this.show=d,this.hide=e,this.cancel=g,this.resetPosition=j}function d(){function a(a,e,g,h,i){var j,k=e.split("-")[0],l=new b;switch(j=f(a)?d(a,k):c(a,k),e){case"n":l.set("left",j.left-g/2),l.set("bottom",E.windowHeight-j.top+i);break;case"e":l.set("left",j.left+i),l.set("top",j.top-h/2);break;case"s":l.set("left",j.left-g/2),l.set("top",j.top+i);break;case"w":l.set("top",j.top-h/2),l.set("right",E.windowWidth-j.left+i);break;case"nw":l.set("bottom",E.windowHeight-j.top+i),l.set("right",E.windowWidth-j.left-20);break;case"nw-alt":l.set("left",j.left),l.set("bottom",E.windowHeight-j.top+i);break;case"ne":l.set("left",j.left-20),l.set("bottom",E.windowHeight-j.top+i);break;case"ne-alt":l.set("bottom",E.windowHeight-j.top+i),l.set("right",E.windowWidth-j.left);break;case"sw":l.set("top",j.top+i),l.set("right",E.windowWidth-j.left-20);break;case"sw-alt":l.set("left",j.left),l.set("top",j.top+i);break;case"se":l.set("left",j.left-20),l.set("top",j.top+i);break;case"se-alt":l.set("top",j.top+i),l.set("right",E.windowWidth-j.left)}return l}function c(a,b){var c,d,e=a.offset(),f=a.outerWidth(),g=a.outerHeight();switch(b){case"n":c=e.left+f/2,d=e.top;break;case"e":c=e.left+f,d=e.top+g/2;break;case"s":c=e.left+f/2,d=e.top+g;break;case"w":c=e.left,d=e.top+g/2;break;case"nw":c=e.left,d=e.top;break;case"ne":c=e.left+f,d=e.top;break;case"sw":c=e.left,d=e.top+g;break;case"se":c=e.left+f,d=e.top+g}return{top:d,left:c}}function d(a,b){function c(){o.push(j.matrixTransform(l))}var d,e,f,g,h=a.closest("svg")[0],i=a[0],j=h.createSVGPoint(),k=i.getBBox(),l=i.getScreenCTM(),m=k.width/2,n=k.height/2,o=[],p=["nw","n","ne","e","se","s","sw","w"];if(j.x=k.x,j.y=k.y,c(),j.x+=m,c(),j.x+=m,c(),j.y+=n,c(),j.y+=n,c(),j.x-=m,c(),j.x-=m,c(),j.y-=n,c(),o[0].y!==o[1].y||o[0].x!==o[7].x)for(e=Math.atan2(l.b,l.a)*D,f=Math.ceil((e%360-22.5)/45),f<1&&(f+=8);f--;)p.push(p.shift());for(g=0;g<o.length;g++)if(p[g]===b){d=o[g];break}return{top:d.y+E.scrollTop,left:d.x+E.scrollLeft}}this.compute=a}function e(c){function e(a){a.data(u,!0),y.queue(function(b){f(a),b()})}function f(b){var d;if(b.data(u)){if(E.isTipOpen)return E.isClosing||g(E.activeHover),void y.delay(100).queue(function(a){f(b),a()});b.trigger("powerTipPreRender"),d=n(b),d&&(y.empty().append(d),b.trigger("powerTipRender"),E.activeHover=b,E.isTipOpen=!0,y.data(x,c.mouseOnToPopup),c.followMouse?h():(i(b),E.isFixedTipOpen=!0),y.addClass(c.popupClass),b.data(v)||q.on("click"+C,function(d){var e=d.target;e!==b[0]&&(c.mouseOnToPopup?e===y[0]||a.contains(y[0],e)||a.powerTip.hide():a.powerTip.hide())}),c.mouseOnToPopup&&!c.manual&&(y.on("mouseenter"+C,function(){E.activeHover&&E.activeHover.data(t).cancel()}),y.on("mouseleave"+C,function(){E.activeHover&&E.activeHover.data(t).hide()})),y.fadeIn(c.fadeInTime,function(){E.desyncTimeout||(E.desyncTimeout=setInterval(k,500)),b.trigger("powerTipOpen")}))}}function g(a){E.isClosing=!0,E.isTipOpen=!1,E.desyncTimeout=clearInterval(E.desyncTimeout),a.data(u,!1),a.data(v,!1),q.off("click"+C),y.off(C),y.fadeOut(c.fadeOutTime,function(){var d=new b;E.activeHover=null,E.isClosing=!1,E.isFixedTipOpen=!1,y.removeClass(),d.set("top",E.currentY+c.offset),d.set("left",E.currentX+c.offset),y.css(d),a.trigger("powerTipClose")})}function h(){if(!E.isFixedTipOpen&&(E.isTipOpen||E.tipOpenImminent&&y.data(w))){var a,d,e=y.outerWidth(),f=y.outerHeight(),g=new b;g.set("top",E.currentY+c.offset),g.set("left",E.currentX+c.offset),a=o(g,e,f),a!==F.none&&(d=p(a),1===d?a===F.right?g.set("left",E.windowWidth-e):a===F.bottom&&g.set("top",E.scrollTop+E.windowHeight-f):(g.set("left",E.currentX-e-c.offset),g.set("top",E.currentY-f-c.offset))),y.css(g)}}function i(b){var d,e;c.smartPlacement?(d=a.fn.powerTip.smartPlacementLists[c.placement],a.each(d,function(a,c){var d=o(j(b,c),y.outerWidth(),y.outerHeight());if(e=c,d===F.none)return!1})):(j(b,c.placement),e=c.placement),y.removeClass("w nw sw e ne se n s w se-alt sw-alt ne-alt nw-alt"),y.addClass(e)}function j(a,d){var e,f,g=0,h=new b;h.set("top",0),h.set("left",0),y.css(h);do e=y.outerWidth(),f=y.outerHeight(),h=l.compute(a,d,e,f,c.offset),y.css(h);while(++g<=5&&(e!==y.outerWidth()||f!==y.outerHeight()));return h}function k(){var b=!1;E.isTipOpen&&!E.isClosing&&!E.delayInProgress&&(a.inArray("mouseleave",c.closeEvents)>-1||a.inArray("mouseout",c.closeEvents)>-1||a.inArray("blur",c.closeEvents)>-1||a.inArray("focusout",c.closeEvents)>-1)&&(E.activeHover.data(u)===!1||E.activeHover.is(":disabled")?b=!0:m(E.activeHover)||E.activeHover.is(":focus")||E.activeHover.data(v)||(y.data(x)?m(y)||(b=!0):b=!0),b&&g(E.activeHover))}var l=new d,y=a("#"+c.popupId);0===y.length&&(y=a("<div/>",{id:c.popupId}),0===s.length&&(s=a("body")),s.append(y),E.tooltips=E.tooltips?E.tooltips.add(y):y),c.followMouse&&(y.data(w)||(q.on("mousemove"+C,h),r.on("scroll"+C,h),y.data(w,!0))),this.showTip=e,this.hideTip=g,this.resetPosition=i}function f(a){return Boolean(window.SVGElement&&a[0]instanceof SVGElement)}function g(a){return Boolean(a&&"number"==typeof a.pageX)}function h(){E.mouseTrackingActive||(E.mouseTrackingActive=!0,i(),a(i),q.on("mousemove"+C,l),r.on("resize"+C,j),r.on("scroll"+C,k))}function i(){E.scrollLeft=r.scrollLeft(),E.scrollTop=r.scrollTop(),E.windowWidth=r.width(),E.windowHeight=r.height()}function j(){E.windowWidth=r.width(),E.windowHeight=r.height()}function k(){var a=r.scrollLeft(),b=r.scrollTop();a!==E.scrollLeft&&(E.currentX+=a-E.scrollLeft,E.scrollLeft=a),b!==E.scrollTop&&(E.currentY+=b-E.scrollTop,E.scrollTop=b)}function l(a){E.currentX=a.pageX,E.currentY=a.pageY}function m(a){var b=a.offset(),c=a[0].getBoundingClientRect(),d=c.right-c.left,e=c.bottom-c.top;return E.currentX>=b.left&&E.currentX<=b.left+d&&E.currentY>=b.top&&E.currentY<=b.top+e}function n(b){var c,d,e=b.data(z),f=b.data(A),g=b.data(B);return e?(a.isFunction(e)&&(e=e.call(b[0])),d=e):f?(a.isFunction(f)&&(f=f.call(b[0])),f.length>0&&(d=f.clone(!0,!0))):g&&(c=a("#"+g),c.length>0&&(d=c.html())),d}function o(a,b,c){var d=E.scrollTop,e=E.scrollLeft,f=d+E.windowHeight,g=e+E.windowWidth,h=F.none;return(a.top<d||Math.abs(a.bottom-E.windowHeight)-c<d)&&(h|=F.top),(a.top+c>f||Math.abs(a.bottom-E.windowHeight)>f)&&(h|=F.bottom),(a.left<e||a.right+b>g)&&(h|=F.left),(a.left+b>g||a.right<e)&&(h|=F.right),h}function p(a){for(var b=0;a;)a&=a-1,b++;return b}var q=a(document),r=a(window),s=a("body"),t="displayController",u="hasActiveHover",v="forcedOpen",w="hasMouseMove",x="mouseOnToPopup",y="originalTitle",z="powertip",A="powertipjq",B="powertiptarget",C=".powertip",D=180/Math.PI,E={elements:null,tooltips:null,isTipOpen:!1,isFixedTipOpen:!1,isClosing:!1,tipOpenImminent:!1,activeHover:null,currentX:0,currentY:0,previousX:0,previousY:0,desyncTimeout:null,closeDelayTimeout:null,mouseTrackingActive:!1,delayInProgress:!1,windowWidth:0,windowHeight:0,scrollTop:0,scrollLeft:0},F={none:0,top:1,bottom:2,left:4,right:8};return a.fn.powerTip=function(b,d){var f,i,j=this;return j.length?"string"===a.type(b)&&a.powerTip[b]?a.powerTip[b].call(j,j,d):(f=a.extend({},a.fn.powerTip.defaults,b),i=new e(f),h(),j.each(function(){var b,d=a(this),e=d.data(z),g=d.data(A),h=d.data(B);d.data(t)&&a.powerTip.destroy(d),b=d.attr("title"),e||h||g||!b||(d.data(z,b),d.data(y,b),d.removeAttr("title")),d.data(t,new c(d,f,i))}),f.manual||(a.each(f.openEvents,function(b,c){a.inArray(c,f.closeEvents)>-1?j.on(c+C,function(b){a.powerTip.toggle(this,b)}):j.on(c+C,function(b){a.powerTip.show(this,b)})}),a.each(f.closeEvents,function(b,c){a.inArray(c,f.openEvents)<0&&j.on(c+C,function(b){a.powerTip.hide(this,!g(b))})}),j.on("keydown"+C,function(b){27===b.keyCode&&a.powerTip.hide(this,!0)})),E.elements=E.elements?E.elements.add(j):j,j):j},a.fn.powerTip.defaults={fadeInTime:200,fadeOutTime:100,followMouse:!1,popupId:"powerTip",popupClass:null,intentSensitivity:7,intentPollInterval:100,closeDelay:100,placement:"n",smartPlacement:!1,offset:10,mouseOnToPopup:!1,manual:!1,openEvents:["mouseenter","focus"],closeEvents:["mouseleave","blur"]},a.fn.powerTip.smartPlacementLists={n:["n","ne","nw","s"],e:["e","ne","se","w","nw","sw","n","s","e"],s:["s","se","sw","n"],w:["w","nw","sw","e","ne","se","n","s","w"],nw:["nw","w","sw","n","s","se","nw"],ne:["ne","e","se","n","s","sw","ne"],sw:["sw","w","nw","s","n","ne","sw"],se:["se","e","ne","s","n","nw","se"],"nw-alt":["nw-alt","n","ne-alt","sw-alt","s","se-alt","w","e"],"ne-alt":["ne-alt","n","nw-alt","se-alt","s","sw-alt","e","w"],"sw-alt":["sw-alt","s","se-alt","nw-alt","n","ne-alt","w","e"],"se-alt":["se-alt","s","sw-alt","ne-alt","n","nw-alt","e","w"]},a.powerTip={show:function(b,c){return g(c)?(l(c),E.previousX=c.pageX,E.previousY=c.pageY,a(b).data(t).show()):a(b).first().data(t).show(!0,!0),b},reposition:function(b){return a(b).first().data(t).resetPosition(),b},hide:function(b,c){var d;return c=!b||c,b?d=a(b).first().data(t):E.activeHover&&(d=E.activeHover.data(t)),d&&d.hide(c),b},toggle:function(b,c){return E.activeHover&&E.activeHover.is(b)?a.powerTip.hide(b,!g(c)):a.powerTip.show(b,c),b},destroy:function(b){var c=b?a(b):E.elements;return E.elements&&0!==E.elements.length?(c.off(C).each(function(){var b=a(this),c=[y,t,u,v];b.data(y)&&(b.attr("title",b.data(y)),c.push(z)),b.removeData(c)}),E.elements=E.elements.not(c),0===E.elements.length&&(r.off(C),q.off(C),E.mouseTrackingActive=!1,E.tooltips.remove(),E.tooltips=null),b):b}},a.powerTip.showTip=a.powerTip.show,a.powerTip.closeTip=a.powerTip.hide,a.powerTip});

/*!
 * Copyright 2012, Chris Wanstrath
 * Released under the MIT License
 * https://github.com/defunkt/jquery-pjax
 */

(function($){

// When called on a container with a selector, fetches the href with
// ajax into the container or with the data-pjax attribute on the link
// itself.
//
// Tries to make sure the back button and ctrl+click work the way
// you'd expect.
//
// Exported as $.fn.pjax
//
// Accepts a jQuery ajax options object that may include these
// pjax specific options:
//
//
// container - String selector for the element where to place the response body.
//      push - Whether to pushState the URL. Defaults to true (of course).
//   replace - Want to use replaceState instead? That's cool.
//
// For convenience the second parameter can be either the container or
// the options object.
//
// Returns the jQuery object
    function fnPjax(selector, container, options) {
        options = optionsFor(container, options)
        return this.on('click.pjax', selector, function(event) {
            var opts = options
            if (!opts.container) {
                opts = $.extend({}, options)
                opts.container = $(this).attr('data-pjax')
            }
            handleClick(event, opts)
        })
    }

// Public: pjax on click handler
//
// Exported as $.pjax.click.
//
// event   - "click" jQuery.Event
// options - pjax options
//
// Examples
//
//   $(document).on('click', 'a', $.pjax.click)
//   // is the same as
//   $(document).pjax('a')
//
// Returns nothing.
    function handleClick(event, container, options) {
        options = optionsFor(container, options)

        var link = event.currentTarget
        var $link = $(link)

        if (link.tagName.toUpperCase() !== 'A')
            throw "$.fn.pjax or $.pjax.click requires an anchor element"

        // Middle click, cmd click, and ctrl click should open
        // links in a new tab as normal.
        if ( event.which > 1 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey )
            return

        // Ignore cross origin links
        if ( location.protocol !== link.protocol || location.hostname !== link.hostname )
            return

        // Ignore case when a hash is being tacked on the current URL
        if ( link.href.indexOf('#') > -1 && stripHash(link) == stripHash(location) )
            return

        // Ignore event with default prevented
        if (event.isDefaultPrevented())
            return

        var defaults = {
            url: link.href,
            container: $link.attr('data-pjax'),
            target: link
        }

        var opts = $.extend({}, defaults, options)
        var clickEvent = $.Event('pjax:click')
        $link.trigger(clickEvent, [opts])

        if (!clickEvent.isDefaultPrevented()) {
            pjax(opts)
            event.preventDefault()
            $link.trigger('pjax:clicked', [opts])
        }
    }

// Public: pjax on form submit handler
//
// Exported as $.pjax.submit
//
// event   - "click" jQuery.Event
// options - pjax options
//
// Examples
//
//  $(document).on('submit', 'form', function(event) {
//    $.pjax.submit(event, '[data-pjax-container]')
//  })
//
// Returns nothing.
    function handleSubmit(event, container, options) {
        options = optionsFor(container, options)

        var form = event.currentTarget
        var $form = $(form)

        if (form.tagName.toUpperCase() !== 'FORM')
            throw "$.pjax.submit requires a form element"

        var defaults = {
            type: ($form.attr('method') || 'GET').toUpperCase(),
            url: $form.attr('action'),
            container: $form.attr('data-pjax'),
            target: form
        }

        if (defaults.type !== 'GET' && window.FormData !== undefined) {
            defaults.data = new FormData(form)
            defaults.processData = false
            defaults.contentType = false
        } else {
            // Can't handle file uploads, exit
            if ($form.find(':file').length) {
                return
            }

            // Fallback to manually serializing the fields
            defaults.data = $form.serializeArray()
        }

        pjax($.extend({}, defaults, options))

        event.preventDefault()
    }

// Loads a URL with ajax, puts the response body inside a container,
// then pushState()'s the loaded URL.
//
// Works just like $.ajax in that it accepts a jQuery ajax
// settings object (with keys like url, type, data, etc).
//
// Accepts these extra keys:
//
// container - String selector for where to stick the response body.
//      push - Whether to pushState the URL. Defaults to true (of course).
//   replace - Want to use replaceState instead? That's cool.
//
// Use it just like $.ajax:
//
//   var xhr = $.pjax({ url: this.href, container: '#main' })
//   console.log( xhr.readyState )
//
// Returns whatever $.ajax returns.
    function pjax(options) {
        options = $.extend(true, {}, $.ajaxSettings, pjax.defaults, options)

        if ($.isFunction(options.url)) {
            options.url = options.url()
        }

        var hash = parseURL(options.url).hash

        var containerType = $.type(options.container)
        if (containerType !== 'string') {
            throw "expected string value for 'container' option; got " + containerType
        }
        var context = options.context = $(options.container)
        if (!context.length) {
            throw "the container selector '" + options.container + "' did not match anything"
        }

        // We want the browser to maintain two separate internal caches: one
        // for pjax'd partial page loads and one for normal page loads.
        // Without adding this secret parameter, some browsers will often
        // confuse the two.
        if (!options.data) options.data = {}
        if ($.isArray(options.data)) {
            options.data.push({name: '_pjax', value: options.container})
        } else {
            options.data._pjax = options.container
        }

        function fire(type, args, props) {
            if (!props) props = {}
            props.relatedTarget = options.target
            var event = $.Event(type, props)
            context.trigger(event, args)
            return !event.isDefaultPrevented()
        }

        var timeoutTimer

        options.beforeSend = function(xhr, settings) {
            // No timeout for non-GET requests
            // Its not safe to request the resource again with a fallback method.
            if (settings.type !== 'GET') {
                settings.timeout = 0
            }

            xhr.setRequestHeader('X-PJAX', 'true')
            xhr.setRequestHeader('X-PJAX-Container', options.container)

            if (!fire('pjax:beforeSend', [xhr, settings]))
                return false

            if (settings.timeout > 0) {
                timeoutTimer = setTimeout(function() {
                    if (fire('pjax:timeout', [xhr, options]))
                        xhr.abort('timeout')
                }, settings.timeout)

                // Clear timeout setting so jquerys internal timeout isn't invoked
                settings.timeout = 0
            }

            var url = parseURL(settings.url)
            if (hash) url.hash = hash
            options.requestUrl = stripInternalParams(url)
        }

        options.complete = function(xhr, textStatus) {
            if (timeoutTimer)
                clearTimeout(timeoutTimer)

            fire('pjax:complete', [xhr, textStatus, options])

            fire('pjax:end', [xhr, options])
        }

        options.error = function(xhr, textStatus, errorThrown) {
            var container = extractContainer("", xhr, options)

            var allowed = fire('pjax:error', [xhr, textStatus, errorThrown, options])
            if (options.type == 'GET' && textStatus !== 'abort' && allowed) {
                locationReplace(container.url)
            }
        }

        options.success = function(data, status, xhr) {
            var previousState = pjax.state

            // If $.pjax.defaults.version is a function, invoke it first.
            // Otherwise it can be a static string.
            var currentVersion = typeof $.pjax.defaults.version === 'function' ?
                $.pjax.defaults.version() :
                $.pjax.defaults.version

            var latestVersion = xhr.getResponseHeader('X-PJAX-Version')

            var container = extractContainer(data, xhr, options)

            var url = parseURL(container.url)
            if (hash) {
                url.hash = hash
                container.url = url.href
            }

            // If there is a layout version mismatch, hard load the new url
            if (currentVersion && latestVersion && currentVersion !== latestVersion) {
                locationReplace(container.url)
                return
            }

            // If the new response is missing a body, hard load the page
            if (!container.contents) {
                locationReplace(container.url)
                return
            }

            pjax.state = {
                id: options.id || uniqueId(),
                url: container.url,
                title: container.title,
                container: options.container,
                fragment: options.fragment,
                timeout: options.timeout
            }

            if (options.push || options.replace) {
                window.history.replaceState(pjax.state, container.title, container.url)
            }

            // Only blur the focus if the focused element is within the container.
            var blurFocus = $.contains(context, document.activeElement)

            // Clear out any focused controls before inserting new page contents.
            if (blurFocus) {
                try {
                    document.activeElement.blur()
                } catch (e) { /* ignore */ }
            }

            if (container.title) document.title = container.title

            fire('pjax:beforeReplace', [container.contents, options], {
                state: pjax.state,
                previousState: previousState
            })
            context.html(container.contents)

            // FF bug: Won't autofocus fields that are inserted via JS.
            // This behavior is incorrect. So if theres no current focus, autofocus
            // the last field.
            //
            // http://www.w3.org/html/wg/drafts/html/master/forms.html
            var autofocusEl = context.find('input[autofocus], textarea[autofocus]').last()[0]
            if (autofocusEl && document.activeElement !== autofocusEl) {
                autofocusEl.focus()
            }

            executeScriptTags(container.scripts)

            var scrollTo = options.scrollTo

            // Ensure browser scrolls to the element referenced by the URL anchor
            if (hash) {
                var name = decodeURIComponent(hash.slice(1))
                var target = document.getElementById(name) || document.getElementsByName(name)[0]
                if (target) scrollTo = $(target).offset().top
            }

            if (typeof scrollTo == 'number') $(window).scrollTop(scrollTo)

            fire('pjax:success', [data, status, xhr, options])
        }


        // Initialize pjax.state for the initial page load. Assume we're
        // using the container and options of the link we're loading for the
        // back button to the initial page. This ensures good back button
        // behavior.
        if (!pjax.state) {
            pjax.state = {
                id: uniqueId(),
                url: window.location.href,
                title: document.title,
                container: options.container,
                fragment: options.fragment,
                timeout: options.timeout
            }
            window.history.replaceState(pjax.state, document.title)
        }

        // Cancel the current request if we're already pjaxing
        abortXHR(pjax.xhr)

        pjax.options = options
        var xhr = pjax.xhr = $.ajax(options)

        if (xhr.readyState > 0) {
            if (options.push && !options.replace) {
                // Cache current container element before replacing it
                cachePush(pjax.state.id, [options.container, cloneContents(context)])

                window.history.pushState(null, "", options.requestUrl)
            }

            fire('pjax:start', [xhr, options])
            fire('pjax:send', [xhr, options])
        }

        return pjax.xhr
    }

// Public: Reload current page with pjax.
//
// Returns whatever $.pjax returns.
    function pjaxReload(container, options) {
        var defaults = {
            url: window.location.href,
            push: false,
            replace: true,
            scrollTo: false
        }

        return pjax($.extend(defaults, optionsFor(container, options)))
    }

// Internal: Hard replace current state with url.
//
// Work for around WebKit
//   https://bugs.webkit.org/show_bug.cgi?id=93506
//
// Returns nothing.
    function locationReplace(url) {
        window.history.replaceState(null, "", pjax.state.url)
        window.location.replace(url)
    }


    var initialPop = true
    var initialURL = window.location.href
    var initialState = window.history.state

// Initialize $.pjax.state if possible
// Happens when reloading a page and coming forward from a different
// session history.
    if (initialState && initialState.container) {
        pjax.state = initialState
    }

// Non-webkit browsers don't fire an initial popstate event
    if ('state' in window.history) {
        initialPop = false
    }

// popstate handler takes care of the back and forward buttons
//
// You probably shouldn't use pjax on pages with other pushState
// stuff yet.
    function onPjaxPopstate(event) {

        // Hitting back or forward should override any pending PJAX request.
        if (!initialPop) {
            abortXHR(pjax.xhr)
        }

        var previousState = pjax.state
        var state = event.state
        var direction

        if (state && state.container) {
            // When coming forward from a separate history session, will get an
            // initial pop with a state we are already at. Skip reloading the current
            // page.
            if (initialPop && initialURL == state.url) return

            if (previousState) {
                // If popping back to the same state, just skip.
                // Could be clicking back from hashchange rather than a pushState.
                if (previousState.id === state.id) return

                // Since state IDs always increase, we can deduce the navigation direction
                direction = previousState.id < state.id ? 'forward' : 'back'
            }

            var cache = cacheMapping[state.id] || []
            var containerSelector = cache[0] || state.container
            var container = $(containerSelector), contents = cache[1]

            if (container.length) {
                if (previousState) {
                    // Cache current container before replacement and inform the
                    // cache which direction the history shifted.
                    cachePop(direction, previousState.id, [containerSelector, cloneContents(container)])
                }

                var popstateEvent = $.Event('pjax:popstate', {
                    state: state,
                    direction: direction
                })
                container.trigger(popstateEvent)

                var options = {
                    id: state.id,
                    url: state.url,
                    container: containerSelector,
                    push: false,
                    fragment: state.fragment,
                    timeout: state.timeout,
                    scrollTo: false
                }

                if (contents) {
                    container.trigger('pjax:start', [null, options])

                    pjax.state = state
                    if (state.title) document.title = state.title
                    var beforeReplaceEvent = $.Event('pjax:beforeReplace', {
                        state: state,
                        previousState: previousState
                    })
                    container.trigger(beforeReplaceEvent, [contents, options])
                    container.html(contents)

                    container.trigger('pjax:end', [null, options])
                } else {
                    pjax(options)
                }

                // Force reflow/relayout before the browser tries to restore the
                // scroll position.
                container[0].offsetHeight // eslint-disable-line no-unused-expressions
            } else {
                locationReplace(location.href)
            }
        }
        initialPop = false
    }

// Fallback version of main pjax function for browsers that don't
// support pushState.
//
// Returns nothing since it retriggers a hard form submission.
    function fallbackPjax(options) {
        var url = $.isFunction(options.url) ? options.url() : options.url,
            method = options.type ? options.type.toUpperCase() : 'GET'

        var form = $('<form>', {
            method: method === 'GET' ? 'GET' : 'POST',
            action: url,
            style: 'display:none'
        })

        if (method !== 'GET' && method !== 'POST') {
            form.append($('<input>', {
                type: 'hidden',
                name: '_method',
                value: method.toLowerCase()
            }))
        }

        var data = options.data
        if (typeof data === 'string') {
            $.each(data.split('&'), function(index, value) {
                var pair = value.split('=')
                form.append($('<input>', {type: 'hidden', name: pair[0], value: pair[1]}))
            })
        } else if ($.isArray(data)) {
            $.each(data, function(index, value) {
                form.append($('<input>', {type: 'hidden', name: value.name, value: value.value}))
            })
        } else if (typeof data === 'object') {
            var key
            for (key in data)
                form.append($('<input>', {type: 'hidden', name: key, value: data[key]}))
        }

        $(document.body).append(form)
        form.submit()
    }

// Internal: Abort an XmlHttpRequest if it hasn't been completed,
// also removing its event handlers.
    function abortXHR(xhr) {
        if ( xhr && xhr.readyState < 4) {
            xhr.onreadystatechange = $.noop
            xhr.abort()
        }
    }

// Internal: Generate unique id for state object.
//
// Use a timestamp instead of a counter since ids should still be
// unique across page loads.
//
// Returns Number.
    function uniqueId() {
        return (new Date).getTime()
    }

    function cloneContents(container) {
        var cloned = container.clone()
        // Unmark script tags as already being eval'd so they can get executed again
        // when restored from cache. HAXX: Uses jQuery internal method.
        cloned.find('script').each(function(){
            if (!this.src) $._data(this, 'globalEval', false)
        })
        return cloned.contents()
    }

// Internal: Strip internal query params from parsed URL.
//
// Returns sanitized url.href String.
    function stripInternalParams(url) {
        url.search = url.search.replace(/([?&])(_pjax|_)=[^&]*/g, '').replace(/^&/, '')
        return url.href.replace(/\?($|#)/, '$1')
    }

// Internal: Parse URL components and returns a Locationish object.
//
// url - String URL
//
// Returns HTMLAnchorElement that acts like Location.
    function parseURL(url) {
        var a = document.createElement('a')
        a.href = url
        return a
    }

// Internal: Return the `href` component of given URL object with the hash
// portion removed.
//
// location - Location or HTMLAnchorElement
//
// Returns String
    function stripHash(location) {
        return location.href.replace(/#.*/, '')
    }

// Internal: Build options Object for arguments.
//
// For convenience the first parameter can be either the container or
// the options object.
//
// Examples
//
//   optionsFor('#container')
//   // => {container: '#container'}
//
//   optionsFor('#container', {push: true})
//   // => {container: '#container', push: true}
//
//   optionsFor({container: '#container', push: true})
//   // => {container: '#container', push: true}
//
// Returns options Object.
    function optionsFor(container, options) {
        if (container && options) {
            options = $.extend({}, options)
            options.container = container
            return options
        } else if ($.isPlainObject(container)) {
            return container
        } else {
            return {container: container}
        }
    }

// Internal: Filter and find all elements matching the selector.
//
// Where $.fn.find only matches descendants, findAll will test all the
// top level elements in the jQuery object as well.
//
// elems    - jQuery object of Elements
// selector - String selector to match
//
// Returns a jQuery object.
    function findAll(elems, selector) {
        return elems.filter(selector).add(elems.find(selector))
    }

    function parseHTML(html) {
        return $.parseHTML(html, document, true)
    }

// Internal: Extracts container and metadata from response.
//
// 1. Extracts X-PJAX-URL header if set
// 2. Extracts inline <title> tags
// 3. Builds response Element and extracts fragment if set
//
// data    - String response data
// xhr     - XHR response
// options - pjax options Object
//
// Returns an Object with url, title, and contents keys.
    function extractContainer(data, xhr, options) {
        var obj = {}, fullDocument = /<html/i.test(data)

        // Prefer X-PJAX-URL header if it was set, otherwise fallback to
        // using the original requested url.
        var serverUrl = xhr.getResponseHeader('X-PJAX-URL')
        obj.url = serverUrl ? stripInternalParams(parseURL(serverUrl)) : options.requestUrl

        var $head, $body
        // Attempt to parse response html into elements
        if (fullDocument) {
            $body = $(parseHTML(data.match(/<body[^>]*>([\s\S.]*)<\/body>/i)[0]))
            var head = data.match(/<head[^>]*>([\s\S.]*)<\/head>/i)
            $head = head != null ? $(parseHTML(head[0])) : $body
        } else {
            $head = $body = $(parseHTML(data))
        }

        // If response data is empty, return fast
        if ($body.length === 0)
            return obj

        // If there's a <title> tag in the header, use it as
        // the page's title.
        obj.title = findAll($head, 'title').last().text()

        if (options.fragment) {
            var $fragment = $body
            // If they specified a fragment, look for it in the response
            // and pull it out.
            if (options.fragment !== 'body') {
                $fragment = findAll($fragment, options.fragment).first()
            }

            if ($fragment.length) {
                obj.contents = options.fragment === 'body' ? $fragment : $fragment.contents()

                // If there's no title, look for data-title and title attributes
                // on the fragment
                if (!obj.title)
                    obj.title = $fragment.attr('title') || $fragment.data('title')
            }

        } else if (!fullDocument) {
            obj.contents = $body
        }

        // Clean up any <title> tags
        if (obj.contents) {
            // Remove any parent title elements
            obj.contents = obj.contents.not(function() { return $(this).is('title') })

            // Then scrub any titles from their descendants
            obj.contents.find('title').remove()

            // Gather all script[src] elements
            obj.scripts = findAll(obj.contents, 'script[src]').remove()
            obj.contents = obj.contents.not(obj.scripts)
        }

        // Trim any whitespace off the title
        if (obj.title) obj.title = $.trim(obj.title)

        return obj
    }

// Load an execute scripts using standard script request.
//
// Avoids jQuery's traditional $.getScript which does a XHR request and
// globalEval.
//
// scripts - jQuery object of script Elements
//
// Returns nothing.
    function executeScriptTags(scripts) {
        if (!scripts) return

        var existingScripts = $('script[src]')

        scripts.each(function() {
            var src = this.src
            var matchedScripts = existingScripts.filter(function() {
                return this.src === src
            })
            if (matchedScripts.length) return

            var script = document.createElement('script')
            var type = $(this).attr('type')
            if (type) script.type = type
            script.src = $(this).attr('src')
            document.head.appendChild(script)
        })
    }

// Internal: History DOM caching class.
    var cacheMapping      = {}
    var cacheForwardStack = []
    var cacheBackStack    = []

// Push previous state id and container contents into the history
// cache. Should be called in conjunction with `pushState` to save the
// previous container contents.
//
// id    - State ID Number
// value - DOM Element to cache
//
// Returns nothing.
    function cachePush(id, value) {
        cacheMapping[id] = value
        cacheBackStack.push(id)

        // Remove all entries in forward history stack after pushing a new page.
        trimCacheStack(cacheForwardStack, 0)

        // Trim back history stack to max cache length.
        trimCacheStack(cacheBackStack, pjax.defaults.maxCacheLength)
    }

// Shifts cache from directional history cache. Should be
// called on `popstate` with the previous state id and container
// contents.
//
// direction - "forward" or "back" String
// id        - State ID Number
// value     - DOM Element to cache
//
// Returns nothing.
    function cachePop(direction, id, value) {
        var pushStack, popStack
        cacheMapping[id] = value

        if (direction === 'forward') {
            pushStack = cacheBackStack
            popStack  = cacheForwardStack
        } else {
            pushStack = cacheForwardStack
            popStack  = cacheBackStack
        }

        pushStack.push(id)
        id = popStack.pop()
        if (id) delete cacheMapping[id]

        // Trim whichever stack we just pushed to to max cache length.
        trimCacheStack(pushStack, pjax.defaults.maxCacheLength)
    }

// Trim a cache stack (either cacheBackStack or cacheForwardStack) to be no
// longer than the specified length, deleting cached DOM elements as necessary.
//
// stack  - Array of state IDs
// length - Maximum length to trim to
//
// Returns nothing.
    function trimCacheStack(stack, length) {
        while (stack.length > length)
            delete cacheMapping[stack.shift()]
    }

// Public: Find version identifier for the initial page load.
//
// Returns String version or undefined.
    function findVersion() {
        return $('meta').filter(function() {
            var name = $(this).attr('http-equiv')
            return name && name.toUpperCase() === 'X-PJAX-VERSION'
        }).attr('content')
    }

// Install pjax functions on $.pjax to enable pushState behavior.
//
// Does nothing if already enabled.
//
// Examples
//
//     $.pjax.enable()
//
// Returns nothing.
    function enable() {
        $.fn.pjax = fnPjax
        $.pjax = pjax
        $.pjax.enable = $.noop
        $.pjax.disable = disable
        $.pjax.click = handleClick
        $.pjax.submit = handleSubmit
        $.pjax.reload = pjaxReload
        $.pjax.defaults = {
            timeout: 650,
            push: true,
            replace: false,
            type: 'GET',
            dataType: 'html',
            scrollTo: 0,
            maxCacheLength: 20,
            version: findVersion
        }
        $(window).on('popstate.pjax', onPjaxPopstate)
    }

// Disable pushState behavior.
//
// This is the case when a browser doesn't support pushState. It is
// sometimes useful to disable pushState for debugging on a modern
// browser.
//
// Examples
//
//     $.pjax.disable()
//
// Returns nothing.
    function disable() {
        $.fn.pjax = function() { return this }
        $.pjax = fallbackPjax
        $.pjax.enable = enable
        $.pjax.disable = $.noop
        $.pjax.click = $.noop
        $.pjax.submit = $.noop
        $.pjax.reload = function() { window.location.reload() }

        $(window).off('popstate.pjax', onPjaxPopstate)
    }


// Add the state property to jQuery's event object so we can use it in
// $(window).bind('popstate')
    if ($.event.props && $.inArray('state', $.event.props) < 0) {
        $.event.props.push('state')
    } else if (!('state' in $.Event.prototype)) {
        $.event.addProp('state')
    }

// Is pjax supported by this browser?
    $.support.pjax =
        window.history && window.history.pushState && window.history.replaceState &&
        // pushState isn't reliable on iOS until 5.
        !navigator.userAgent.match(/((iPod|iPhone|iPad).+\bOS\s+[1-4]\D|WebApps\/.+CFNetwork)/)

    if ($.support.pjax) {
        enable()
    } else {
        disable()
    }

})(jQuery)