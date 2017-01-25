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
    prefix = prefix.replace(/\[[0-9]+\]$/, '').replace(/_[0-9]+$/, '');
    prefix += '[';
    addIfAbove = addIfAbove || -999999;
    var coll = $container.closest('.collection')[0];
    var sel = "[id*='" + prefix + "']";
    $container.find(sel).andSelf().filter(sel).each(function () {
        var id = $(this).prop('id');
        var newIdx = idx;
        var curr = parseInt(id.after(prefix).upTo(']'));
        if (addToExisting)
            newIdx = (curr > addIfAbove ? newIdx + curr : curr);
        $(this).prop('id', id.upTo(prefix) + prefix + newIdx + ']' + id.after(prefix).after(']'));
    });
    sel = "[name*='" + prefix + "']";
    $container.find(sel).andSelf().filter(sel).each(function () {
        var name = $(this).prop('name');
        var newIdx = idx;
        var curr = parseInt(name.after(prefix).upTo(']'));
        if (addToExisting)
            newIdx = (curr > addIfAbove ? newIdx + curr : curr);
        $(this).prop('name', name.upTo(prefix) + prefix + newIdx + ']' + name.after(prefix).after(']'));
    });
    prefix = prefix.replace(/\[$/, '_');
    var sel = "[id*='" + prefix + "']";
    $container.find(sel).andSelf().filter(sel).each(function () {
        var id = $(this).prop('id');
        var newIdx = idx;
        var curr = parseInt(id.after(prefix).upTo('_'));
        if (addToExisting)
            newIdx = (curr > addIfAbove ? newIdx + curr : curr);
        $(this).prop('id', id.upTo(prefix) + prefix + newIdx + (id.after(prefix).after('_') ? '_' + id.after(prefix).after('_') : ''));
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

function addItem($addButton, param, postAdd, url) {
    var postUrl = $('#editPanel form').prop('action').upTo('?');
    var prop = $addButton.prop('id').after('-');
    var depth = $addButton.prop('class').after('depth-').upTo(' ');
    var type = $('#modelType').val();
    var $collection = $addButton.hasClass('add-button-bottom')
        ? $addButton.closest('.collection')
        : $addButton.prevAll('.collection');
    $collection.removeClass('closed');
    $collection.closest('.editor-unit').children('.child-closed').removeClass('child-closed').addClass('child-open');
    notifyLayout();
    $collection.children('.reorder.last').removeClass('last');
    url = url || postUrl + '?$type=' + type + '&$mode=property-item-html&propertyPath=' + prop + '&depth=' + depth;
    setBoxSpinner($addButton.closest('.editor-unit'), true, '35px');
    $.get(url)
        .success(function (html) {
            setBoxSpinner($addButton.closest('.editor-unit'), false);
            var $add = $($.trim(html)).find('.collection').first();
            //$add.find('.add-button').last().remove();
            var $lastDel = $collection.children('.collection-item-bar.editor-label').children('.delete').last();
            if ($lastDel.length == 0)
                $lastDel = $collection.children().children('.collection-item-bar.editor-label').children('.delete').last();
            var n = $lastDel.length ? (parseInt($lastDel.prop('id').afterLast('[').upTo(']')) + 1) : 0;
            reindex($add, prop, n, false);
            var indentInc = parseInt($addButton.prop('class').after('indent-').upTo(' ')) - 1;
            $add.find("[class*='indent-']").each(function () {
                var cls = $(this).prop('class');
                var indent = parseInt(cls.after('indent-').upTo(' ')) + indentInc;
                $(this).prop('class', cls.upTo('indent-') + 'indent-' + indent + ' ' + cls.after('indent-').after(' '));
            });
            $add.find(".add-button-bottom").remove();
            var $added;
            if ($collection.children('.add-button-bottom').length)
                $added = $add.contents().insertBefore($collection.children('.add-button-bottom'));
            else
                $added = $add.contents().appendTo($collection);
            setFirstLast($collection);
            setupAfterLoad($added);
            if (postAdd) postAdd($added, param);
            notifyChanged();
        });
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
    $('.extends').closest('.editor-unit.level-1').addClass('extends');
    var showOpen = $outer.children('.editor-unit').length == 1;
    if (showOpen)
        toggleParent($generalLabel);
}

function setupAfterLoad($container) {
    $container.find('.collection, .object').closest('.editor-field').prev('.editor-label')
    .addClass('parent').addClass('child-closed').css('cursor', 'pointer')
        .next('.editor-field').children('.object, .collection').addClass('closed');
    $container.find('.parent').closest('.editor-unit').addClass('parent-unit');

    $container.find('.lyn-datetime').datepicker({ changeMonth: true, changeYear: true, dateFormat: 'yy-mm-dd' });
    $container.find('.lyn-datetime-now').click(function () {
        var $dt = $(this).siblings('.lyn-datetime');
        $dt.datepicker('setDate', 'today');
        var dNow = new Date();
        var hrs = dNow.getHours();
        if (hrs < 10) hrs = '0' + hrs;
        var mins = dNow.getMinutes();
        if (mins < 10) mins = '0' + mins;
        $dt.val($dt.val() + " " + hrs + ":" + mins);
        $(this).siblings('.text-display').text($dt.val());
        notifyChanged();
    });
    if ($.fn.jqte) {
        $container.find('.lyn-jquery-te').each(function () {
            var $ed = $(this);
            var lvl = $ed.hasClass('te-max') ? 2 : ($ed.hasClass('te-med') ? 1 : 0);
            var options = {
                linktypes: ['Web Address', 'E-mail Address'],
                change: function () {
                    notifyChanged();
                    if ($ed.css('position') != 'static')
                        notifyLayout();
                },
                source: userIsAdmin
            }
            if (lvl < 2) {
                options.color = options.fsize = options.format = options.indent
                 = options.outdent = options.rule = options.u = false;
            }
            if (lvl < 1) {
                options.center = options.format = options.left = options.ol = options.right
                 = options.strike = options.ul = false;
            }
            $ed.jqte(options);
        });
    }
    notifyVisible($container);
    notifyLayout();
}

var _lyn_data_dirty = false;
var _lyn_saving = false;

function notifyChanged() {
    $('#save').css({ 'background-color': '#900000' });
    _lyn_data_dirty = true;
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
                var $coll = $this.closest('.collection');
                var $moves = $this.nextAll();
                $this.next().remove();
                $this.remove();
                reindex($moves, prefix, -1, true);
                setFirstLast($coll);
                notifyLayout();
            } else if ($this.hasClass('reorder-up')) {
                var prefix = $this.children('i').prop('id').after('-');
                var $this = $this.closest('.collection-item-bar');
                $this.addClass('moving').siblings('.collection-item-bar').removeClass('moving');
                var $block = $this.add($this.next());
                var $above = $this.prev().prev();
                var $moves = $above.add($above.next());
                reindex($moves, prefix, -99999, true); // must avoid certain inputs, e.g. radio buttons, having same name when they shouldn't
                $above.before($block);
                reindex($block, prefix, -1, true);
                reindex($moves, prefix, 100000, true);
                setFirstLast($this.closest('.collection'));
            } else if ($this.hasClass('reorder-down')) {
                var prefix = $this.children('i').prop('id').after('-');
                var $this = $this.closest('.collection-item-bar');
                $this.addClass('moving').siblings('.collection-item-bar').removeClass('moving');
                var $block = $this.add($this.next());
                var $below = $this.next().next();
                var $moves = $below.add($below.next());
                reindex($moves, prefix, -100000, true);
                $below.next().after($block);
                reindex($block, prefix, 1, true);
                reindex($moves, prefix, 99999, true);
                setFirstLast($this.closest('.collection'));
            }
            notifyChanged();
        }).delegate('#save', 'click', function (ev) {
            ev.preventDefault();
            $fs = $('#formState');
            $fs.val($fs.val() + $(window).scrollTop());
            _lyn_saving = true;
            var pingUrl = location.href + (location.href.indexOf("?") < 0 ? "?$mode=ping" : "&$mode=ping");
            $.post(pingUrl, {}, function (resp) {
                if (resp == "OK") {
                    $('#editPanel form').append($("<input type='hidden' name='editAction'/>").val($(this).prop('id'))).submit();
                } else {
                    alert('Your login seems to have expired, try logging in again in another window, then click SAVE here again');
                }
            }).fail(function () {
                alert('There is a problem with your internet connection, try and fix this then click SAVE again');
            });
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
            var currStepMatches = /edit-([0-9]+)[^ ]*/.exec($('#container').prop('class'));
            var oldClass = currStepMatches[0];
            var currStep = parseInt(currStepMatches[1]);
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
            $(this).next('input.text-box').css('display', 'inline-block').focus();
            ev.stopPropagation();
        }).on('keydown', '.text-box', function (ev) {
            if (ev.keyCode == 13) {
                $(this).css('display','none');
                $(this).siblings('.text-display').css('display', 'inline-block').text($(this).val());
            }
            notifyChanged();
        }).on('click', '.lyn-html-popout', function (ev) {
            var $rte = $(this).prev();
            var $rteParent = $rte.parent();
            var rteWidth = $rte.width();
            var rteHeight = $rte.height();
            $rte.css({ 'z-index': '1010', position: 'fixed', width: '75%' });
            $rte.find('.jqte_editor').css('max-height', '800px');
            var currRteHeight = rteHeight;
            var resizeFunc = function () {
                var newRteHeight = $rte.height();
                if (currRteHeight != newRteHeight) {
                    $('#modalPlaceholder').height(newRteHeight);
                    currRteHeight = newRteHeight;
                    $.modal.update(newRteHeight, $rte.width());
                }
            };
            $('body').mouseup(resizeFunc);

            $("<div id='modalPlaceholder' style='background-color: #eoeoff; width: 75%;'></div>")
                .height($rte.height())
                .modal({
                    overlayClose: true,
                    onClose: function(dialog) {
                        $rte.css({ position: '', 'z-index': '', height: rteHeight + 'px', width: rteWidth + 'px' });
                        $rte.find('.jqte_editor').css('max-height', '');
                        $.modal.getContainer().unbind('move.modal');
                        $('body').off('mouseup', resizeFunc);
                        $.modal.close();
                        notifyLayout();
                    }
                });

            $('.simplemodal-close').css({
                'z-index': '1003', position: 'fixed', display: 'block',
                'background-image': 'url(/images/lynicon/close-white.png)',
                width: '16px', height: '16px'});
            positionTool($rte);
            $.modal.getContainer().bind('move.modal', function() { positionTool($rte); });
        });

        $(window).on('beforeunload', function () {
            if (_lyn_data_dirty && !_lyn_saving)
                return 'You have not yet saved your changes';
        });
    });
})(jQuery);