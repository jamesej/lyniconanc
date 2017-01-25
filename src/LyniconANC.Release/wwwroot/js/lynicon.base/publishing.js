publishing = (function ($) {

    var setPubStatus = function () {
        var id = $('#modelId').val();
        var type = $('#modelType').val();
        $.get('/lynicon/publish/getpublishstatus', { id: id, type: type }, function (status) {
            if (status == "Needspub")
                $('#fpbPublish').addClass('needs-pub');
            else if (status == "Unpub")
                $('#fpbPublish').addClass('unpub');
        });
    };

    $(window).load(setPubStatus);

    return {
        setPubStatus: setPubStatus
    };

})(jQuery);