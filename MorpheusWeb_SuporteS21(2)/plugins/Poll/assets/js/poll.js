$(function () {

    $(document).on("submit", ".poll-vote-form", function (e) {

        var $this = $(this);
        var $submit = $this.find(":submit");
        var $inputs = $this.find(":input:not(:submit)");
        var $result = $(".poll-show-result");

        $submit.attr("disabled", true);
        $.post($this.attr("action"), $this.serialize(), function (data) {
            if (data.success) {
                jSuccess(data.message, "Sucesso :)");

                if (data.allow_ips) {
                    $result.click();
                } else {
                    $.get($result.attr("href"), function (html) {
                        $(".poll-container").html(html);
                    });
                }
            } else {
                jAlert(data.message, "Ops :(");
            }
            $submit.attr("disabled", false);
            $inputs.attr("checked", false).attr("selected", false);
        });

        e.preventDefault();

    });

    $(document).on("click", ".poll-show-result", function (e) {

        var $this = $(this);

        $.get($this.attr("href"), function (html) {

            $(".last-poll-result").show().html(html);

        });

        e.preventDefault();

    });

});