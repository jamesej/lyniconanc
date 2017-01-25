
function notifyLayout() {
    $("#editPanel .object.level-0").masonry("layout");
}

function notifyVisible($container) {
    var showDeferreds = [];
    $container.trigger("shown", [ showDeferreds ]);
    $container.find("select.post-load-select:visible").each(function () {
        loadRefSelect($(this));
    });
    if ($.fn.chosen)
        $container.find("select.chosen-select:visible").chosen({ search_contains: true });
    if ($.fn.selectize)
        $container.find("select.lyn-selectize:visible").each(function () {
            initSelectize($(this));
        });
    $container.find("select.lyn-jquiac:visible").each(function () {
        initJquiac($(this));
    });
    return $.when.apply($, showDeferreds);
}

function notifyAddSelectOption($container, type, val, txt)
{
    for (var list in lynSelectLists) {
        if (list.indexOf(type) >= 0)
            lynSelectLists[list].push({ value: val, text: txt });
    }
    $container.find(".chosen-container").each(function () {
        var $listId = $(this).siblings("input.select-list-id");
        var v = $listId.val();
        if (v && v.indexOf(type) >= 0) {
            $(this).siblings("select.chosen-select")
                .append($("<option value=\"\"" + val + "\"\">" + txt + "</option>"))
                .trigger("chosen:updated");
        }
    });
}

function setBoxSpinner($box, isStart, margin) {
    var $bar = $box.children('.editor-label');
    if (isStart) {
        $bar.data('spinner', setTimeout(function () {
            var $spinner = $('<img src="/images/lynicon/ajax-loader.gif"/>').addClass('spinner');
            if (margin) $spinner.css('right-margin', margin);
            $bar.append($spinner);
            $bar.data('spinner', '');
        }, 800));
    } else {
        $bar.children('.spinner').remove();
        if ($bar.data('spinner')) {
            clearTimeout($bar.data('spinner'));
            $bar.data('spinner', '');
        }
    }
}

function toggleParent($parentLabel) {
    var $editor = $parentLabel.next(".editor-field")
    var $collobj = $editor.children(".collection, .object, .object-wrapper");
    if ($collobj.hasClass("object-wrapper"))
        $collobj = $collobj.children(".collection, .object");
    if ($collobj.length == 0) return;
    $parentLabel.toggleClass("child-closed").toggleClass("child-open");
    var wasClosed = $collobj.hasClass('closed');
    var $wrap = $editor.wrap('<div></div>').parent();
    if (wasClosed)
        $wrap.css('visibility', 'hidden');
    else {
        $wrap.css({ height: $editor.height() });
        $editor.slideUp(300, function () {
            $editor.show().unwrap();
            notifyLayout();
        });
    }
    $collobj.toggleClass("closed");
    var formState = $("#formState").val();
    if (!wasClosed) {
        if (formState || (formState == ""))
            $("#formState").val(formState.replace($collobj.prop("id") + ";", ""));
    } else {
        //$('#formState').val(formState + $collection.prop('id') + ";");
        var makeVisible = notifyVisible($collobj);
        notifyLayout();
        makeVisible.done(function () {
            $wrap.css({ height: $editor.height() });
            $editor.hide();
            $wrap.css({ visibility: 'visible' });
            $editor.slideDown(300, function () {
                $editor.unwrap();
                notifyLayout();
            });
        });
    }
}

$(document).ready(function () {
    $("body").on("click", ".editor-label.parent", function (ev) {
        if (!$(ev.target).hasClass("parent")) // ignore click on buttons on bar
            return;
        toggleParent($(this));
    }).on("dblclick", ".editor-unit.level-0 > .editor-label", function (ev) {
        $(this).closest(".editor-unit").toggleClass("wide");
        notifyLayout();
    });
});

