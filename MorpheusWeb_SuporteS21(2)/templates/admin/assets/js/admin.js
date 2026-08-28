var overlay = {
    create: function (ctx, size) {
        size = size || false;
        $(ctx || ".content-wrapper").append('<div class="preloader"><div class="loader' + (size ? ' loader-' + size : '') + '"></div></div>');
    },

    destroy: function () {
        $(".preloader").fadeOut();
    }
};

var fixHelper = function(e, ui) {
    ui.children().each(function() {
        $(this).width($(this).width());
    });
    return ui;
};

function runModal(ctx) {
    $("[data-load-with-modal]", ctx || document).click(function (e) {
        e.preventDefault();

        var $this = $(this);

        if ($this.data("dismiss") == "modal") return;

        var $modal = $("#" + $this.data("modal"));

        function modal() {
            $("body").modalmanager("loading");

            $modal.load($this.attr("href"), "", function () {
                $modal.modal({
                    replace: true,
                    backdrop: "static"
                });
                runCustomCheckbox($modal);
                runTooltip($modal);
            });

            $modal.on("hidden", function () {
                var $this = $(this);
                $this.data("modal", null);
                $this.html("");
            });

            $modal.on('shown.bs.modal', function () {
                $modal.find("input[type=text],input[type=email],input[type=number],input[type=checkbox],select,textarea").filter(":first").focus();
            });
        }

        modal();
    });
};

function runDatepicker(ctx) {
    $("input[data-show-calendar]", ctx || document).datepicker({
        format: "dd/mm/yyyy",
        autoclose: true,
        language: "pt-BR",
        todayHighlight: true
    });
}

function runEditor(ctx) {
    $("textarea.editor", ctx || document).each(function () {
        var $this = $(this);
        $this.summernote({
            height: $this.data("height") || 300,
            lang: "pt-BR",
            callbacks: {
                onImageUpload: function (files, editor, welEditable) {
                    data = new FormData();
                    data.append("file", files[0]);
                    $.ajax({
                        data: data,
                        type: "POST",
                        url: base + "admin/editor/upload_files",
                        cache: false,
                        contentType: false,
                        processData: false,
                        success: function (url) {
                            $this.summernote("insertImage", url);
//                        editor.insertImage(welEditable, url);
                        }
                    });
                }
            }
        });
    });
}

function focusForm() {
    $("form.form-focus").find("input[type=text],input[type=email],input[type=number],input[type=checkbox],select,textarea").not("[readonly]").filter(":first").focus().select();
}

function runCustomCheckbox(ctx) {
    $(":checkbox", ctx || document).icheck({
        checkboxClass: "icheckbox_square-blue",
        radioClass: "iradio_square-blue",
        increaseArea: "20%"
    });
}

function runInputMask(ctx) {
    $("input.money", ctx || document).unmaskMoney().maskMoney();
}

function activeMenuLink() {
    $(".treeview-menu a").click(function () {
        $(".treeview-menu .active").removeClass("active");
        $(this).closest("li").addClass("active");
    });
}

function loadPartial(partial) {
    var $partial = $(".partial-" + partial);
    $partial.append('<div class="loading-partial"></div>');
    var active = $(".treeview.active").attr("data-id");
    var active2 = $("ul.treeview-menu .active").attr("data-id");
    $.get(base + "admin/partial/" + partial, function (html) {
        $partial.html(html);
        if (partial === "sidebar") {
            var $active = $(".main-sidebar").find("[data-id=" + active + "]");
            if ($active.size() > 0) {
                $active.addClass("active").find("ul.treeview-menu:first").addClass("menu-open").show();
                $(".main-sidebar").find("ul.treeview-menu [data-id=" + active2 + "]").addClass("active");
                activeMenuLink();
            }
        }
    });
}

function runTooltip(ctx) {
    $('[data-toggle="tooltip"]', ctx || document).tooltip();
}

function initAfterLoadPage(ctx) {
    _init();
    runCustomCheckbox(ctx);
    //focusForm(ctx);
    runInputMask(ctx);
    runEditor(ctx);
    activeMenuLink();
    runModal();
    runDatepicker(ctx);

    $.AdminLTE.layout.fixSidebar();
    runTooltip(ctx);
}

$(function () {
    initAfterLoadPage();

    $(document).pjax("a[data-ajax], ul.pagination a", ".ajax-container", {timeout: 0}).on("pjax:end", function() {
        initAfterLoadPage();
    }).on("pjax:send", function(e) {
        overlay.create(e.target);
    }).on("pjax:complete", function (e) {
        overlay.destroy();
    });


    $.morpheus.form("form[data-ajax]", {
        onInit: function (form) {
            var $submit = form.find(":submit");
			var modal = $(".modal").is(":visible");
			
			if (modal) {
				$submit.button("loading");
			} else {
				overlay.create();
			}
        },
        onError: function (data, form) {
            var $submit = form.find(":submit");

            if (data.message) {
                swal({
                    title: "Ops :(",
                    html: true,
                    text: data.message,
                    type: "error"
                });
            }

			var modal = $(".modal").is(":visible");
			
			if (modal) {
				$submit.button("reset");
			} else {
	            $submit.attr("disabled", false);
	            overlay.destroy();
			}
        },
        onInternalError: function (data, form) {
            var $submit = form.find(":submit");

            if (data.responseJSON.error) {
                swal({
                    title: "Ops :(",
                    html: true,
                    text: data.responseJSON.error.message,
                    type: "error"
                });
            }

			var modal = $(".modal").is(":visible");
			
			if (modal) {
				$submit.button("reset");
			} else {
	            $submit.attr("disabled", false);
	            overlay.destroy();
			}

        },
        onContentLoaded: function () {
            initAfterLoadPage();
            overlay.destroy();
        },
        onSuccess: function (data, form) {
            var $submit = form.find(":submit");

            if (data.message) {
                swal({
                    title: "Sucesso :)",
                    html: true,
                    text: data.message,
                    type: "success"
                });
            }

            if (data.partial) {
                loadPartial(data.partial);
            }

            if (data.redirect) {
                $.pjax({url: base.substring(0, base.length - 1) + data.redirect, container: ".ajax-container", timeout:0});
            }

			var modal = $(".modal").is(":visible");
			
			if (modal) {
				$(".modal").modal("hide");
			} else {
	            $submit.attr("disabled", false);
	            overlay.destroy();
			}
        }
    });

    $(document).on("click", "[data-send-modal-form]", function (e) {
        e.preventDefault();
        var $button = $(this);
        var $form = $("#" + $button.data("send-modal-form"));

        $button.button("loading");
        $.post($form.attr("action"), $form.serialize(), function (data) {
            if (typeof data == 'object') {
                if (data.success) {
                    swal({
                        title: "Sucesso :)",
                        html: true,
                        text: data.message,
                        type: "success"
                    });
                } else {
                    swal({
                        title: "Ops :(",
                        html: true,
                        text: data.error,
                        type: "error"
                    });
                }
                $(".modal").modal("hide");
                $button.button("reset");
            } else {
                var $container = $(".ajax-container");
                $container.html(data);
                $(".modal").modal("hide");
                initAfterLoadPage(".ajax-container");
            }
        }).fail(function () {
            swal({
                title: "Ops :(",
                html: true,
                text: "....",
                type: "error"
            });
            $button.button("reset");
        });
    });

    $(document).on("submit", "[data-account-find]", function (e) {
        e.preventDefault();
        var $this = $(this);

        overlay.create();
        $.post($this.attr("action"), $this.serialize(), function (html) {
            $(".ajax-container").html(html);
            overlay.destroy();
        });

    });


    $(document).on("click", "[data-ajax-delete]", function (e) {
        var $this = $(this);

        swal({
            title: "Confirmação",
            text: "Tem certeza que deseja excluir este registro?",
            type: "warning",
            html: true,
            showCancelButton: true,
            confirmButtonColor: "#DD6B55",
            confirmButtonText: "Sim, eu confirmo!",
            cancelButtonText: "Cancelar",
            closeOnConfirm: true
        }, function () {
            overlay.create();
            $.get($this.attr("href"), function (data) {
                if (data.redirect) {
                    $.pjax({url: base.substring(0, base.length - 1) + data.redirect, container: ".ajax-container", timeout:0});
                }
                overlay.destroy();
            });
        });

        e.preventDefault();
    });

    $(document).on("click", "[data-ajax-action]", function (e) {
        var $this = $(this);

        e.preventDefault();

        function proccess() {
            overlay.create();
            $.get($this.attr("href"), function (data) {
                overlay.destroy();

                if (data.success) {
                    if (data.message) {
                        swal({
                            title: "Successo :)",
                            html: true,
                            text: data.message,
                            type: "success"
                        });
                    }

                    if (data.partial) {
                        loadPartial(data.partial);
                    }
                } else {
                    if (data.message) {
                        swal({
                            title: "Ops :(",
                            html: true,
                            text: data.message,
                            type: "error"
                        })
                    }
                }

                if (data.redirect) {
                    $.pjax({
                        url: base.substring(0, base.length - 1) + data.redirect,
                        container: ".ajax-container",
                        timeout: 0
                    });
                }
            });
        }

        if ($this.data("confirm")) {
            swal({
                title: "Confirmação",
                text: $this.data("confirm"),
                type: "warning",
                html: true,
                showCancelButton: true,
                confirmButtonColor: "#DD6B55",
                confirmButtonText: "Sim, eu confirmo!",
                cancelButtonText: "Cancelar",
                closeOnConfirm: true
            }, function () {
                proccess();
            });
            overlay.destroy();
        } else {
            proccess();
        }
    });
});













$.AdminLTE = {};

$.AdminLTE.options = {
  navbarMenuSlimscroll: true,
  navbarMenuSlimscrollWidth: "3px",
  navbarMenuHeight: "200px",
  animationSpeed: 500,
  sidebarToggleSelector: "[data-toggle='offcanvas']",
  sidebarPushMenu: true,
  sidebarSlimScroll: true,
  sidebarExpandOnHover: false,
  enableControlSidebar: true,
  controlSidebarOptions: {
    //Which button should trigger the open/close event
    toggleBtnSelector: "[data-toggle='control-sidebar']",
    //The sidebar selector
    selector: ".control-sidebar",
    //Enable slide over content
    slide: true
  },
  screenSizes: {
    xs: 480,
    sm: 768,
    md: 992,
    lg: 1200
  }
};


$(function () {
    "use strict";
    $("body").removeClass("hold-transition");
    if (typeof AdminLTEOptions !== "undefined") {
        $.extend(true, $.AdminLTE.options, AdminLTEOptions);
    }

    var o = $.AdminLTE.options;

    _init();

    $.AdminLTE.layout.activate();
    $.AdminLTE.tree(".sidebar");
    $.AdminLTE.controlSidebar.activate();
    $.AdminLTE.pushMenu.activate(o.sidebarToggleSelector);

});

/* ----------------------------------
 * - Initialize the AdminLTE Object -
 * ----------------------------------
 * All AdminLTE functions are implemented below.
 */
function _init() {
  'use strict';
  /* Layout
   * ======
   * Fixes the layout height in case min-height fails.
   *
   * @type Object
   * @usage $.AdminLTE.layout.activate()
   *        $.AdminLTE.layout.fix()
   *        $.AdminLTE.layout.fixSidebar()
   */
  $.AdminLTE.layout = {
    activate: function () {
      var _this = this;
      _this.fix();
      _this.fixSidebar();
      $(window, ".wrapper").resize(function () {
        _this.fix();
        _this.fixSidebar();
      });
    },
    fix: function () {
      //Get window height and the wrapper height
      var neg = $('.main-header').outerHeight() + $('.main-footer').outerHeight();
      var window_height = $(window).height();
      var sidebar_height = $(".sidebar").height() + 20;
      //Set the min-height of the content and sidebar based on the
      //the height of the document.
      if ($("body").hasClass("fixed")) {
        $(".content-wrapper, .right-side").css('min-height', window_height - $('.main-footer').outerHeight());
      } else {
        var postSetWidth;
        if (window_height >= sidebar_height) {
          $(".content-wrapper, .right-side").css('min-height', window_height - neg);
          postSetWidth = window_height - neg;
        } else {
          $(".content-wrapper, .right-side").css('min-height', sidebar_height);
          postSetWidth = sidebar_height;
        }

        //Fix for the control sidebar height
        var controlSidebar = $($.AdminLTE.options.controlSidebarOptions.selector);
        if (typeof controlSidebar !== "undefined") {
          if (controlSidebar.height() > postSetWidth)
            $(".content-wrapper, .right-side").css('min-height', controlSidebar.height());
        }

      }
    },
    fixSidebar: function () {
      if ($.AdminLTE.options.sidebarSlimScroll) {

        if (typeof $.fn.slimScroll != 'undefined') {
          //$(".sidebar").slimScroll({destroy: true}).height("auto");
          //$(".sidebar").slimscroll({
          //  height: ($(window).height() - $(".main-header").height()) + "px",
          //  color: "rgba(0,0,0,0.2)",
          //  size: "3px"
          //});
        }
      }
    }
  };

  /* PushMenu()
   * ==========
   * Adds the push menu functionality to the sidebar.
   *
   * @type Function
   * @usage: $.AdminLTE.pushMenu("[data-toggle='offcanvas']")
   */
  $.AdminLTE.pushMenu = {
    activate: function (toggleBtn) {
      //Get the screen sizes
      var screenSizes = $.AdminLTE.options.screenSizes;

      //Enable sidebar toggle
      $(toggleBtn).on('click', function (e) {
        e.preventDefault();

        //Enable sidebar push menu
        if ($(window).width() > (screenSizes.sm - 1)) {
          if ($("body").hasClass('sidebar-collapse')) {
            $("body").removeClass('sidebar-collapse').trigger('expanded.pushMenu');
          } else {
            $("body").addClass('sidebar-collapse').trigger('collapsed.pushMenu');
          }
        }
        //Handle sidebar push menu for small screens
        else {
          if ($("body").hasClass('sidebar-open')) {
            $("body").removeClass('sidebar-open').removeClass('sidebar-collapse').trigger('collapsed.pushMenu');
          } else {
            $("body").addClass('sidebar-open').trigger('expanded.pushMenu');
          }
        }
      });

      $(".content-wrapper").click(function () {
        //Enable hide menu when clicking on the content-wrapper on small screens
        if ($(window).width() <= (screenSizes.sm - 1) && $("body").hasClass("sidebar-open")) {
          $("body").removeClass('sidebar-open');
        }
      });

      //Enable expand on hover for sidebar mini
      if ($.AdminLTE.options.sidebarExpandOnHover
              || ($('body').hasClass('fixed')
                      && $('body').hasClass('sidebar-mini'))) {
        this.expandOnHover();
      }
    },
    expandOnHover: function () {
      var _this = this;
      var screenWidth = $.AdminLTE.options.screenSizes.sm - 1;
      //Expand sidebar on hover
      $('.main-sidebar').hover(function () {
        if ($('body').hasClass('sidebar-mini')
                && $("body").hasClass('sidebar-collapse')
                && $(window).width() > screenWidth) {
          _this.expand();
        }
      }, function () {
        if ($('body').hasClass('sidebar-mini')
                && $('body').hasClass('sidebar-expanded-on-hover')
                && $(window).width() > screenWidth) {
          _this.collapse();
        }
      });
    },
    expand: function () {
      $("body").removeClass('sidebar-collapse').addClass('sidebar-expanded-on-hover');
    },
    collapse: function () {
      if ($('body').hasClass('sidebar-expanded-on-hover')) {
        $('body').removeClass('sidebar-expanded-on-hover').addClass('sidebar-collapse');
      }
    }
  };

  /* Tree()
   * ======
   * Converts the sidebar into a multilevel
   * tree view menu.
   *
   * @type Function
   * @Usage: $.AdminLTE.tree('.sidebar')
   */
  $.AdminLTE.tree = function (menu) {
    var _this = this;
    var animationSpeed = $.AdminLTE.options.animationSpeed;
    $(document).on('click', menu + ' li a', function (e) {
      //Get the clicked link and the next element
      var $this = $(this);
      var checkElement = $this.next();

      //Check if the next element is a menu and is visible
      if ((checkElement.is('.treeview-menu')) && (checkElement.is(':visible'))) {
        //Close the menu
        checkElement.slideUp(animationSpeed, function () {
          checkElement.removeClass('menu-open');
          //Fix the layout in case the sidebar stretches over the height of the window
          _this.layout.fix();
        });
        checkElement.parent("li").removeClass("active");
      }
      //If the menu is not visible
      else if ((checkElement.is('.treeview-menu')) && (!checkElement.is(':visible'))) {
        //Get the parent menu
        var parent = $this.parents('ul').first();
        //Close all open menus within the parent
        var ul = parent.find('ul:visible').slideUp(animationSpeed);
        //Remove the menu-open class from the parent
        ul.removeClass('menu-open');
        //Get the parent li
        var parent_li = $this.parent("li");

        //Open the target menu and add the menu-open class
        checkElement.slideDown(animationSpeed, function () {
          //Add the class active to the parent li
          checkElement.addClass('menu-open');
          parent.find('li.active').removeClass('active');
          parent_li.addClass('active');
          //Fix the layout in case the sidebar stretches over the height of the window
          _this.layout.fix();
        });
      }
      //if this isn't a link, prevent the page from being redirected
      if (checkElement.is('.treeview-menu')) {
        e.preventDefault();
      }
    });
  };

  /* ControlSidebar
   * ==============
   * Adds functionality to the right sidebar
   *
   * @type Object
   * @usage $.AdminLTE.controlSidebar.activate(options)
   */
  $.AdminLTE.controlSidebar = {
    //instantiate the object
    activate: function () {
      //Get the object
      var _this = this;
      //Update options
      var o = $.AdminLTE.options.controlSidebarOptions;
      //Get the sidebar
      var sidebar = $(o.selector);
      //The toggle button
      var btn = $(o.toggleBtnSelector);

      //Listen to the click event
      btn.on('click', function (e) {
        e.preventDefault();
        //If the sidebar is not open
        if (!sidebar.hasClass('control-sidebar-open')
                && !$('body').hasClass('control-sidebar-open')) {
          //Open the sidebar
          _this.open(sidebar, o.slide);
        } else {
          _this.close(sidebar, o.slide);
        }
      });

      //If the body has a boxed layout, fix the sidebar bg position
      var bg = $(".control-sidebar-bg");
      _this._fix(bg);

      //If the body has a fixed layout, make the control sidebar fixed
      if ($('body').hasClass('fixed')) {
        _this._fixForFixed(sidebar);
      } else {
        //If the content height is less than the sidebar's height, force max height
        if ($('.content-wrapper, .right-side').height() < sidebar.height()) {
          _this._fixForContent(sidebar);
        }
      }
    },
    //Open the control sidebar
    open: function (sidebar, slide) {
      //Slide over content
      if (slide) {
        sidebar.addClass('control-sidebar-open');
      } else {
        //Push the content by adding the open class to the body instead
        //of the sidebar itself
        $('body').addClass('control-sidebar-open');
      }
    },
    //Close the control sidebar
    close: function (sidebar, slide) {
      if (slide) {
        sidebar.removeClass('control-sidebar-open');
      } else {
        $('body').removeClass('control-sidebar-open');
      }
    },
    _fix: function (sidebar) {
      var _this = this;
      if ($("body").hasClass('layout-boxed')) {
        sidebar.css('position', 'absolute');
        sidebar.height($(".wrapper").height());
        $(window).resize(function () {
          _this._fix(sidebar);
        });
      } else {
        sidebar.css({
          'position': 'fixed',
          'height': 'auto'
        });
      }
    },
    _fixForFixed: function (sidebar) {
      sidebar.css({
        'position': 'fixed',
        'max-height': '100%',
        'overflow': 'auto',
        'padding-bottom': '50px'
      });
    },
    _fixForContent: function (sidebar) {
      $(".content-wrapper, .right-side").css('min-height', sidebar.height());
    }
  };
}