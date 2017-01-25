function positionTool(selTool) {
    var $smCont = $('#simplemodal-container');
    var scTop = parseInt($smCont.css('top'), 10);
    var scPadTop = parseInt($smCont.css('padding-top'), 10);
    var scLeft = parseInt($smCont.css('left'), 10);
    var scPadLeft = parseInt($smCont.css('padding-left'), 10);
    $(selTool).css({ top: scTop + scPadTop, left: scLeft + scPadLeft });
    $('.simplemodal-close').css({ top: scTop + scPadTop - 16, left: scLeft + $smCont.width() + scPadLeft });
}
$.modal.impl.setPositionBase = $.modal.impl.setPosition;
$.modal.getContainer = function () {
    return this.impl.d.container;
};
$.modal.impl.setPosition = function () {
    var s = this;
    s.setPositionBase();
    s.d.container.trigger('move.modal');
};

function supports_html5_storage() {
    try {
        return 'localStorage' in window && window['localStorage'] !== null;
    } catch (e) {
        return false;
    }
}

function reindex($container, prefix, idx, addToExisting, addIfAbove) {
    alert(prefix);
    addIfAbove = addIfAbove || -999999;
    var coll = $container.closest('.collection')[0];
    $container.find("[id*=']']").andSelf().filter("[id*=']']").each(function () {
        if ($(this).closest('.collection')[0] !== coll)
            return;
        var id = $(this).prop('id');
        var newIdx = idx;
        var curr = parseInt(id.afterLast('[').upToLast(']'));
        if (addToExisting)
            newIdx = (curr > addIfAbove ? newIdx + curr : curr);
        $(this).prop('id', id.upToLast('[') + '[' + newIdx + ']' + id.afterLast(']'));
    });
    $container.find("[name*=']']").andSelf().filter("[name*=']']").each(function () {
        if ($(this).closest('.collection')[0] !== coll)
            return;
        var name = $(this).prop('name');
        var newIdx = idx;
        var curr = parseInt(name.afterLast('[').upToLast(']'));
        if (addToExisting)
            newIdx = (curr > addIfAbove ? newIdx + curr : curr);
        $(this).prop('name', name.upToLast('[') + '[' + newIdx + ']' + name.afterLast(']'));
    });
    $container.find("span.index").each(function () {
        if ($(this).closest('.collection')[0] !== coll)
            return;
        var newIdx = idx;
        var curr = parseInt($(this).text());
        if (addToExisting)
            newIdx = (curr > addIfAbove ? newIdx + curr : curr);
        else
            newIdx++;
        $(this).text(newIdx);
    });
}

var showLinkFields = function () {
    $link = $(this).closest('.l24-link');
    var isint = $link.find('.l24-link-isinternal input').attr('checked');
    $link.find('.l24-link-controller')[isint ? 'show' : 'hide']();
    $link.find('.l24-link-action')[isint ? 'show' : 'hide']();
    $link.find('.l24-link-url')[isint ? 'hide' : 'show']();
}

function setFirstLast($coll) {
    var coll = $coll[0];
    var $reorders = $coll.find('.reorder').filter(function (i, el) { return $(el).closest('.collection')[0] === coll; });
    $reorders.removeClass('first').removeClass('last');
    $reorders.first().addClass('first');
    $reorders.last().addClass('last');
}

function addItem($addButton, param, postAdd) {
    var postUrl = $('#editPanel form').prop('action').upTo('?');
    var prop = $addButton.prop('id').after('-');
    var depth = $addButton.prop('class').after('depth-').upTo(' ');
    var $collection = $addButton.prev('.collection');
    $collection.removeClass('closed');
    $collection.closest('.editor-unit').children('.child-closed').removeClass('child-closed').addClass('child-open');
    $collection.children('.reorder.last').removeClass('last');
    $.get(postUrl + '?$mode=property-item-html&propertyPath=' + prop + '&depth=' + depth)
        .success(function (html) {
            var $add = $($.trim(html)).find('.collection').first();
            //$add.find('.add-button').last().remove();
            var $lastDel = $collection.children('.collection-item-bar').children('.delete').last();
            var n = $lastDel.length ? (parseInt($lastDel.prop('id').afterLast('[').upTo(']')) + 1) : 0;
            reindex($add, $lastDel.prop('id').after('-'), n, false);
            var indentInc = parseInt($addButton.prop('class').after('indent-').upTo(' ')) - 1;
            $add.find("[class*='indent-']").each(function () {
                var cls = $(this).prop('class');
                var indent = parseInt(cls.after('indent-').upTo(' ')) + indentInc;
                $(this).prop('class', cls.upTo('indent-') + 'indent-' + indent + ' ' + cls.after('indent-').after(' '));
            });
            var $added = $add.contents().appendTo($collection);
            setFirstLast($collection);
            setupAfterLoad($added);
            if (postAdd) postAdd($added, param);
        });
}

function setFilename($input, fname) {
    $input.val(fname);
    $input.siblings('span').text(fname);
    $input.closest('.lyn-image').find('.lyn-image-content').show().html("<div class='file-image-thumb' style='background-image:url(" + fname + ")'></div>");
    $input.closest('.lyn-image').find('.lyn-image-url, .lyn-image-alt').hide();
    setTimeout(notifyLayout, 200);
}


function reindexClass($container, classPrefix, offset) {
    $container.find('[class*="' + classPrefix + '"]').andSelf().filter('[class*="' + classPrefix + '"]').each(function () {
        var matchPrefix = new RegExp(classPrefix + '(\\d+)', 'g');
        $(this).prop('class', $(this).prop('class').replace(matchPrefix, function (match, n) {
            return classPrefix + (parseInt(n) + offset);
        }));
    });
}

function createGeneralContainer() {
    var $outer = $('#editPanel form .object.level-0');
    var $generalLabel = $("<div class='editor-label indent-0 parent child-closed' style='cursor:pointer'><label>General</label></div>");
    var $general = $("<div class='editor-field indent-0'></div>");
    var $generalFields = $outer.children().not('.parent-unit');
    $outer.append($("<div class='editor-unit parent-unit level-0'></div>").append($generalLabel).append($general));
    $general.append($generalFields.wrapAll("<div class='object level-1 closed'></div>").parent());
    reindexClass($generalFields, 'indent-', 1);
    reindexClass($generalFields, 'level-', 1);
}

function setupAfterLoad($container) {
    $container.find('.collection, .object').closest('.editor-field').prev('.editor-label')
    .addClass('parent').addClass('child-closed').css('cursor', 'pointer')
        .next('.editor-field').children('.object, .collection').addClass('closed');
    $container.find('.parent').closest('.editor-unit').addClass('parent-unit');

    $container.find('.l24-link').each(showLinkFields);
    $container.find('.l24-link-isinternal input').click(showLinkFields);
    $container.find('.l24-datetime').datepicker({ changeMonth: true, changeYear: true, dateFormat: 'yy-mm-dd' });
    if ($.fn.jqte) {
        $container.find('.lyn-jquery-te.te-min').jqte({
            center: false, color: false, fsize: false, format: false,
            indent: false, linktypes: ['Web Address', 'E-mail Address'],
            left: false, ol: false, outdent: false,
            right: false, rule: false, source: false, strike: false, ul: false,
            change: function () {
                if ($(this).css('position') != 'static')
                    notifyLayout();
            }
        });
        $container.find('.lyn-jquery-te.te-med').jqte({
            center: true, color: false, fsize: false, format: true,
            indent: false, linktypes: ['Web Address', 'E-mail Address'],
            left: true, ol: true, outdent: false,
            right: true, rule: false, source: false, strike: true, ul: true,
            change: function () {
                if ($(this).css('position') != 'static')
                    notifyLayout();
            }
        });
    }
    notifyVisible($container);
    notifyLayout();
}

function notifyChanged() {
    $('#save').css({ 'background-color': '#900000' });
}

(function ($) {
    $(document).ready(function () {
        var openerStep = supports_html5_storage() ? window.localStorage["_lyn-opener"] : 20;
        if (openerStep == null) openerStep = 20;
        $('#container').addClass('edit-' + openerStep);
        if ($('#formState').length && $('#formState').val())
            $(window).scrollTop($('#formState').val().afterLast(';'));

        setupAfterLoad($('#editPanel'));

        createGeneralContainer();

        $('#editPanel .object.level-0').masonry();

        $('body').delegate('.action-button', 'click', function () {
            var $this = $(this);
            if ($this.hasClass('delete')) {
                var prefix = $this.prop('id').after('-');
                $this = $this.closest('.collection-item-bar');
                $coll = $this.closest('.collection');
                reindex($this.nextAll(), prefix, -1, true);
                $this.next().remove();
                $this.remove();
                setFirstLast($coll);
                notifyLayout();
            } else if ($this.hasClass('reorder-up')) {
                var prefix = $this.prop('id').after('-');
                var $this = $this.closest('.collection-item-bar');
                var $block = $this.add($this.next());
                var $above = $this.prev().prev();
                var $moves = $above.add($above.next());
                reindex($moves, prefix, -99999, true); // must avoid certain inputs, e.g. radio buttons, having same name when they shouldn't
                $above.before($block);
                reindex($block, -1, true);
                reindex($moves, prefix, 100000, true);
                setFirstLast($this.closest('.collection'));
            } else if ($this.hasClass('reorder-down')) {
                var prefix = $this.prop('id').after('-');
                var $this = $this.closest('.collection-item-bar');
                var $block = $this.add($this.next());
                var $below = $this.next().next();
                var $moves = $below.add($below.next());
                reindex($moves, prefix, -100000, true);
                $below.next().after($block);
                reindex($block, prefix, 1, true);
                reindex($moves, prefix, 99999, true);
                setFirstLast($this.closest('.collection'));
            }
        }).delegate('#save', 'click', function (ev) {
            ev.preventDefault();
            $fs = $('#formState');
            $fs.val($fs.val() + $(window).scrollTop());
            $('#editPanel form').append($("<input type='hidden' name='editAction'/>").val($(this).prop('id'))).submit();
        }).delegate('.add-button', 'click', function () {
            addItem($(this));
        }).delegate('._L24Html', 'click', function () {
            var $this = $(this);
            top.showHtml($this.html(), function (h) {
                $this.html(h);
                $this.siblings('input').val(h);
            });
        }).delegate('#editPanel form input, #editPanel form select, #editPanel form textarea', 'change', function () {
            notifyChanged();
        }).delegate('#editPanel .l24-link-isinternal input', 'click', function () {
            var isInt = $(this).is(':checked');
            var $link = $(this).closest('.l24-link');
            if (isInt) {
                $link.find('.l24-link-controller, .l24-link-action').hide();
                $link.find('.l24-link-url').show();
            } else {
                $link.find('.l24-link-controller, .l24-link-action').show();
                $link.find('.l24-link-url').hide();
            }
        }).delegate('#editPanel .l24-find-reference', 'click', function (ev) {
            ev.preventDefault();
            var $this = $(this);
            var $container = $this.closest('.lyn-reference');
            getItem($container.find('input.lyn-reference-id').val(), function (item) {
                $container.find('.lyn-reference-id').val(item.id);
                $container.find('.lyn-reference-title').text(item.title);
                $container.find('.lyn-reference-datatype').val(item.datatype);
            });
        }).delegate('#opener-out, #opener-in', 'click', function (ev) {
            var step = $(this).prop('id') == 'opener-out' ? 20 : -20;
            currStep = parseInt($('#container').prop('class').split('-')[1]);
            var oldClass = 'edit-' + currStep;
            currStep += step;
            if (currStep < 0) currStep = 0;
            if (currStep > 100) currStep = 100;
            var newClass = 'edit-' + currStep;
            if (oldClass == newClass) return;
            $('#container').removeClass(oldClass).addClass(newClass);
            if (supports_html5_storage())
                window.localStorage["_lyn-opener"] = currStep;
            notifyLayout();
        });

        $('#edit').on('click', '.text-display', function (ev) {
            $(this).css('display', 'none');
            $(this).siblings('input.text-box').css('display', 'inline-block').focus();
            ev.stopPropagation();
        }).on('click', '.lyn-html-popout', function (ev) {
            var $rte = $(this).prev();
            var $rteParent = $rte.parent();
            var rteWidth = $rte.width();
            var rteHeight = $rte.height();
            $rte.css({ 'z-index': '1010', position: 'fixed', width: '75%' });
            var isRTEResizeDrag = false;
            $('.jqte_editor').mousedown(function () { isRTEResizeDrag = true; console.log('drag.start'); });
            $('body').mouseup(function () {
                if (isRTEResizeDrag) {
                    isRTEResizeDrag = false;
                    console.log('drag.end');
                    $('#modalPlaceholder').width($rte.width()).height($rte.height());
                    $.modal.update($rte.height(), $rte.width());
                }
            });

            $("<div id='modalPlaceholder' style='background-color: #eoeoff; width: 75%;'></div>")
                .height($rte.height())
                .modal({
                    overlayClose: true,
                    onClose: function(dialog) {
                        $rte.css({ position: '', 'z-index': '', height: rteHeight + 'px', width: rteWidth + 'px' });
                        $.modal.getContainer().unbind('move.modal');
                        $.modal.close();
                        notifyLayout();
                    }
                });

            $('.simplemodal-close').css({
                'z-index': '1003', position: 'fixed', display: 'block',
                'background-image': 'url(/lynicon/embedded/Content/Images/close-white.png/)',
                width: '16px', height: '16px'});
            positionTool($rte);
            $.modal.getContainer().bind('move.modal', function() { positionTool($rte); });
        });
    });
})(jQuery);