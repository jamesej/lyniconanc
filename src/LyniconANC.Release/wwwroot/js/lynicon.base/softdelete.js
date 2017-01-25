(function ($) {
    $(document).ready(function () {
        var vsn = $('#lyn-item-version').val();
        if (vsn)
        {
            var vobj = null
            try
            {
                vobj = JSON.parse(vsn);
            } catch (err) { }
            if (vobj)
            {
                var existed = vobj['Existence'] && vobj.Existence == 'Existed';
                if (existed) {
                    $('#edit').prepend($('<svg style="height:100%; width:100%; position:absolute; top:0; left: 0;" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" preserveAspectRatio="none"><rect y="-50" x="49" width="2" height="200" transform="rotate(45 50 50)" style="stroke:none; fill:rgb(220, 107, 107);"></rect></svg>'));
                    $('#fpbMainDelete').off('click').click(function () {
                        var data = { id: $('#modelId').val(), typeName: $('#modelType').val() };
                        $.post('/lynicon/softdelete/retrieve', data, function (d) {
                            location.reload(true);
                        });
                    }).attr('id', 'fpbRetrieve').text('REINSTATE');
                }
            }

        }

    });
})(jQuery);