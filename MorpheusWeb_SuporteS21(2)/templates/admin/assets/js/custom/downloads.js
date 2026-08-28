$(function () {
    
    $(document).on("click", ".add-link", function (e) {
        var $this = $(this);
        var $container = $this.closest("form").find(".container-add-link");
        var index = ($container.find(":input").size() / 2) + 1;
        $container.append('<div class="input-group" style="width:100%;margin-top:5px;"><input class="form-control" type="text" placeholder="Nome" name="links[' + index + '][name]" style="width:40%;margin-right:2%" /><input class="form-control" type="text" placeholder="Link" name="links[' + index + '][link]" style="width:58%" />');

        e.preventDefault();
    });
    
});