(function ($) {
    $.morpheus = {
        form: function (selector, options) {
            var defaults = {
                onSuccess: function () {},
                onInit: function () {},
                onError: function () {},
                onContentLoaded: function () {},
                onInternalError: function () {}
            };

            options = $.extend(defaults, options);

            function clearErrorFields(form) {
                form.find("input.error, textarea.error, select.error").each(function () {
                    $(this).removeClass("error").closest(".form-group").removeClass("has-error").find(".help-block").remove();
                });
            }

            $(document).on("submit", selector, function (e) {
                var _this = $(this);
                options.onInit(_this);

                var success = function (data) {
                    if (_this.data("content")) {
                        var content = _this.data("content");
                        var $content = $(content);

                        $content.html(data);

                        options.onContentLoaded();
                    } else {
                        if (data.success && !data.error) {
                            options.onSuccess(data, _this);
                            clearErrorFields(_this);
                        } else {
                            options.onError(data, _this);
                            clearErrorFields(_this);
                            if (data.errors) {
                                $.each(data.errors, function (k, v) {
                                    var field = _this.find('[name="' + k + '"]');
                                    if (field.size() > 0) {
                                        field.closest(".form-group").addClass("has-error");
                                        if (field.next().is("div.help-block")) {
                                            field.next("label").html(v);
                                        } else {
                                            if (field.closest(".form-group").find(".note-editor").size() > 0) {
                                                field.closest(".form-group").find(".note-editor").after('<div class="help-block"><label for="' + field.attr('id') + '">' + v + '</label></div>');
                                            } else {
                                                field.after('<div class="help-block"><label for="' + field.attr('id') + '">' + v + '</label></div>');
                                            }
                                        }
                                        field.addClass("error");
                                        _this.find(":input.error:first").focus();
                                    }
                                });
                            }
                        }
                    }
                };

                var attr = _this.attr("data-upload");

                if (typeof attr !== typeof undefined && attr !== false) {
                    var $field = _this.find("[data-upload-field]");
                    var data = new FormData();

                    $.each($field, function () {
                        var $f = $(this);

                        $.each($f[0].files, function (i, file) {
                            data.append($f.attr("name") + "[" + i + "]", file);
                        });
                    });


                    $.each(_this.serializeArray(), function (index, f) {
                        data.append(f.name, f.value);
                    });
                    //
                    // _this.find(":input:not(:hidden):not(:submit):not(:file)").each(function () {
                    //     var $this = $(this);
                    //     data.append($this.attr("name"), $this.val());
                    // });

                    $.ajax({
                        url: _this.attr("action"),
                        //dataType: "json",
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
                    }).error(function (data) {
                        options.onInternalError(data, _this);
                    });
                }

                e.preventDefault();
            });
        }
    }
})(jQuery);