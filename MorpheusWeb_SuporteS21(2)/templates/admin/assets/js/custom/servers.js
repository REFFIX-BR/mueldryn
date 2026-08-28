$(function () {
    
    $(document).on("click", ".add-server", function (e) {
        var $this = $(this);
        var $container = $this.closest("form").find(".container-add-server");
        var index = ($container.find(":input").size() / 2) + 1;
        $container.append('<div class="input-group" style="width:100%;margin-top:5px;"> <input class="form-control" style="width:22%;margin-right:2%" type="text" placeholder="Servidor" name="servers[' + index + '][server]"> <input class="form-control" style="width:22%;margin-right:2%" type="text" placeholder="Nome" name="servers[' + index + '][name]"> <input class="form-control" style="width:22%;margin-right:2%" type="text" placeholder="IP" name="servers[' + index + '][ip]"> <input class="form-control" style="width:15%;margin-right:2%" type="text" placeholder="Porta" name="servers[' + index + '][port]"> <input class="form-control" style="width:11%" type="text" placeholder="Max users" name="servers[' + index + '][max]"> </div>');

        e.preventDefault();
    });
    
});