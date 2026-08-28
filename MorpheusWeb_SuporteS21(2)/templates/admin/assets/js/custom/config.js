$(function () {
    $(document).on("click", ".add-secret-question", function (e) {
        var $this = $(this);
        var $container = $this.closest("form").find(".container-add-secret-question");
        var index = $container.find(":input").size() + 1;
        $container.append('<input class="form-control" type="text" style="margin-top:5px;" name="secret_questions[]" />');
        e.preventDefault();
    });

    $(document).on("click", ".add-vip-type", function (e) {
        var $this = $(this);
        var $container = $this.closest("form").find(".container-add-vip-type");
        var index = $container.find(":input").size() + 1;
        $container.append('<div class="input-group" style="width:100%;margin-top:5px;"><input class="form-control" type="text" value="" name="types[' + index + '][code]" placeholder="Código" style="width:30%;margin-right:2%;" /><input  class="form-control" type="text" value="" name="types[' + index + '][name]" placeholder="Nome do VIP" style="width:68%;" /></div>');
        e.preventDefault();
    });
});