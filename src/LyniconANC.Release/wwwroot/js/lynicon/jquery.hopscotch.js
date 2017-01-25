/*
 * jQuery resize event - v1.1 - 3/14/2010
 * http://benalman.com/projects/jquery-resize-plugin/
 * 
 * Copyright (c) 2010 "Cowboy" Ben Alman
 * Dual licensed under the MIT and GPL licenses.
 * http://benalman.com/about/license/
 */
(function($,h,c){var a=$([]),e=$.resize=$.extend($.resize,{}),i,k="setTimeout",j="resize",d=j+"-special-event",b="delay",f="throttleWindow";e[b]=250;e[f]=true;$.event.special[j]={setup:function(){if(!e[f]&&this[k]){return false}var l=$(this);a=a.add(l);$.data(this,d,{w:l.width(),h:l.height()});if(a.length===1){g()}},teardown:function(){if(!e[f]&&this[k]){return false}var l=$(this);a=a.not(l);l.removeData(d);if(!a.length){clearTimeout(i)}},add:function(l){if(!e[f]&&this[k]){return false}var n;function m(s,o,p){var q=$(this),r=$.data(this,d);r.w=o!==c?o:q.width();r.h=p!==c?p:q.height();n.apply(this,arguments)}if($.isFunction(l)){n=l;return m}else{n=l.handler;l.handler=m}}};function g(){i=h[k](function(){a.each(function(){var n=$(this),m=n.width(),l=n.height(),o=$.data(this,d);if(m!==o.w||l!==o.h){n.trigger(j,[o.w=m,o.h=l])}});g()},e[b])}})(jQuery,this);

/*
* jquery.hopscotch.js: v1.0.1:21/4/2014
* http://easyresponsive.com/hopscotch
* 
* Copyright 2015 James Ellis-Jones
* Licensed under Eclipse Public License v1.0 http://www.eclipse.org/legal/epl-v10.html
*/

(function ($) {
    $.fn.hopscotchLayout = function (idx, substituteSel) {
        var idxOld = -1;
        if (typeof this.data('hs-layout-idx') !== 'undefined')
            idxOld = this.data('hs-layout-idx');
        if (idxOld == idx) {
            if (this.data('hs-track-layout'))
                substitute(this, substituteSel, idx, true);
            return this;
        }

        var $this = this,
            cls = this.attr('class') || '',
            layout = { max: -1 };

        if (cls.indexOf('hs-index-') < 0)
            this.addClass('hs-index-' + idx);
        else
            this.attr('class', cls.replace(/hs-index-\d+/, 'hs-index-' + idx));

        // put layout requirements attributes and jQuery object into a holding object
        function getLayoutInfo($item, sInfo) {
            var info = {};
            if (sInfo == 'X') {
                info.hidden = true;
            } else {
                info.rowcols = sInfo.split(',');
            }
            info.$item = $item;
            return info;
        }
        // recursive function for attaching layoutInfo to a tree-structured object
        function setLayout(layoutInfo, rowcol, depth, subLayout) {
            if (layoutInfo['hidden']) {
                if (!subLayout['hidden'])
                    subLayout.hidden = [];
                subLayout.hidden.push(layoutInfo);
            } else {
                var parts = rowcol[depth].split(':'),
                    size = 1,
                    partsDiv = parts[0].split('/'),
                    idx = parseInt(partsDiv[0]);
                if (parts.length > 1) size = /^[0-9]*$/.test(parts[1]) ? parseInt(parts[1]) : parts[1].trim();
                if (partsDiv.length > 1) size = -parseInt(partsDiv[1]);
                if (rowcol.length - 1 == depth) {
                    layoutInfo.size = size;
                    subLayout[idx] = layoutInfo;
                } else {
                    if (!subLayout[idx])
                        subLayout[idx] = { max: -1, size: size };
                    setLayout(layoutInfo, rowcol, depth + 1, subLayout[idx]);
                }
                if (idx > subLayout.max)
                    subLayout.max = idx;
            }
        }
        // process a layout child into the layout object
        function putLayout($item) {
            var rowVals, layoutInfo;
            rowVals = $item.attr('data-grid-location').replace(/\s/g, '').replace('|', ';').split(';');

            if (rowVals.length <= idx)
                layoutInfo = getLayoutInfo($item, rowVals[rowVals.length - 1].trim());
            else
                layoutInfo = getLayoutInfo($item, rowVals[idx].trim());
            setLayout(layoutInfo, layoutInfo.rowcols, 0, layout);
        }
        // recurse through container children pulling layout children out of containers
        // created by hopscotch
        function buildLayout($item) {
            if ($item == $this || $item.hasClass('hs-container'))
                $item.children().each(function () { buildLayout($(this)); });
            else if ($item.attr('data-grid-location'))
                putLayout($item);
        }

        $this.trigger('hopscotch:beforeLayout', [idxOld, idx]);
        buildLayout(this);
        
        // recursively build the dom from layout tree object, inserting necessary
        // container divs for layout
        function construct($container, subLayout, mode, coords) {
            var totSize = 0, i, $fillItem = null, $endItem = null, first = true;
            for (i = 0; i <= subLayout.max; i++) {
                if (subLayout[i]) {
                    if (subLayout[i]['$item']) {
                        if (mode == 'col') {
                            subLayout[i].$item.css({ 'float': 'left', 'width': '100%', 'display': '' });
                        } else {
                            subLayout[i].$item.css({ 'float': 'left', 'display': '' });
                        }
                    } else {
                        var subContCoords = coords + '-' + i;
                        var newMode = mode == 'col' ? 'row' : 'col';
                        subLayout[i].$item = $("<div class='hs-container hs-" + newMode + "-container hs-container" + subContCoords +"'></div>");
                        construct(subLayout[i].$item, subLayout[i], newMode, subContCoords);
                    }
                    if (subLayout[i].size == "*") // we need to put the fill item as the last one in the container so it goes in between float left/float right
                        $fillItem = subLayout[i].$item;
                    else if ($fillItem) {
                        if ($endItem)
                            $endItem.before(subLayout[i].$item);
                        else
                            $container.append(subLayout[i].$item);
                        $endItem = subLayout[i].$item;
                    }
                    else
                        $container.append(subLayout[i].$item);
                    if (first) {
                        subLayout[i].$item.addClass(mode == 'col' ? 'hs-topmost' : 'hs-leftmost');
                        first = false;
                    }
                    if (i == subLayout.max)
                        subLayout[i].$item.addClass(mode == 'col' ? 'hs-bottommost' : 'hs-rightmost');

                    totSize += subLayout[i].size;
                }
            }
            if ($fillItem)
                $container.append($fillItem);
            if (subLayout['hidden']) {
                for (i = 0; i < subLayout['hidden'].length; i++) {
                    var sl = subLayout['hidden'][i];
                    $container.append(sl['$item']);
                    sl['$item'].css({ 'display': 'none' });
                }
            }
            if (mode == 'row') {
                var totPc = 0, idx = 0, leftRem = 0, rightRem = 0, fillIdx = -1,
                    $children = $container.children();
                for (i = 0; i <= subLayout.max; i++) {
                    if (subLayout[i]) {
                        var pc, size = subLayout[i].size;
                        if (typeof size == "string") {
                            if (size == "*") {
                                fillIdx = i;
                                subLayout[i].$item.addClass('hs-fill');
                            } else {
                                if (fillIdx > -1) {
                                    rightRem += parseInt(size);
                                    subLayout[i].$item.css('float', 'right');
                                } else
                                    leftRem += parseInt(size);
                                subLayout[i].$item.css('width', size);
                            }
                        } else {
                            if (size < 0)
                                pc = 100.0 / (-size);
                            else if (i == subLayout.max)
                                pc = 100.0 - totPc;
                            else
                                pc = (size / totSize) * 100.0;
                            subLayout[i].$item.css('width', pc + '%');
                        }
                        idx++;
                        totPc += pc;
                    }
                }
                if (fillIdx > -1)
                    subLayout[fillIdx].$item.css({ marginLeft: leftRem + 'rem', marginRight: rightRem + 'rem', float: 'none', width: 'auto' });
            }
        }

        function cleanContainers($container)
        {
            $container.children('.hs-container').each(function () {
                cleanContainers($(this));
                if (!$(this).children().length)
                    $(this).remove();
            });
        }

        function cssValue($el, cssProp) {
            return parseFloat($el.css(cssProp).replace('px', ''));
        }
        // substitute attribute values into all recursive children of container
        function substitute($cont, substituteSel, idx, onlyLayout) {
            if (!$cont.length)
                return false;
            substituteSel = substituteSel || '*';
            var patt = /data-hs([0-9]*)-([a-z][a-zA-Z0-9_-]*)/;
            var trackLayout = false;
            $cont.each(function () {
                trackLayout = substitute($(this).children(substituteSel), substituteSel, idx, onlyLayout) || trackLayout;
            });
            $cont.each(function () {
                var el = this;
                var attrs = el.attributes,
                    changes = {};
                for (var j = 0; j < attrs.length; j++) {
                    if (patt.test(attrs[j].name)) {
                        var attrIdx = attrs[j].name.replace(patt, '$1'),
                            attrName = attrs[j].name.replace(patt, '$2');
                        if ((attrIdx != '' && parseInt(attrIdx) == idx) || (attrIdx == '' && !changes[attrName]))
                            changes[attrName] = attrs[j].value;
                    }
                }

                for (var c in changes) {
                    console.log('changing ' + c + ' to ' + changes[c]);
                    if ('font-size,width,height,top,left,bottom,right,margin-top,margin-bottom,margin-left,margin-right,padding-top,padding-bottom,padding-left,padding-right,background-image,background-position'.indexOf(c) >= 0) {
                        var unitPatt = /([0-9-][\.0-9]*)(pw|ph|%w|%h|%sh|r[ltrb]#[-\w]+)/;
                        if (unitPatt.test(changes[c])) {
                            var val = parseFloat(changes[c].replace(unitPatt, '$1'));
                            var units = changes[c].replace(unitPatt, '$2');
                            var px = 0;
                            switch (units) {
                                case 'pw': px = parseFloat($(el).css('width').replace('px','')) / val; break;
                                case 'ph': px = parseFloat($(el).css('height').replace('px', '')) / val; break;
                                case '%w': px = parseFloat($(el).css('width').replace('px', '')) * val / 100.0; break;
                                case '%h': px = parseFloat($(el).css('height').replace('px', '')) * val / 100.0; break;
                                case '%sh':
                                    var maxSiblingHeight = Math.max.apply(null, $(el).siblings().map(function () {
                                        var isHeightProp = new RegExp('data-hs' + idx + '?-height');
                                        for (var j = 0; j < this.attributes.length; j++)
                                            if (isHeightProp.test(this.attributes[j].name) && this.attributes[j].value.slice(-3) == '%sh')
                                                return 0;
                                        return parseFloat($(this).css('height').replace('px', ''));
                                    }).get())
                                    px = maxSiblingHeight * val / 100.0;
                                    break;
                                default:
                                    var relPatt = /r([ltrb])(#[-\w]+)/;
                                    if (relPatt.test(units)) {
                                        var relEdge = units.replace(relPatt, '$1');
                                        var relSel = units.replace(relPatt, '$2');
                                        var relOffs = { top: 0, left: 0 };
                                        try {
                                            relOffs = $(relSel).offset();
                                        } catch (ex) {}
                                        var relDim = 0;
                                        switch (relEdge) {
                                            case 'l': relDim = relOffs.left; break;
                                            case 't': relDim = relOffs.top; break;
                                            case 'r': relDim = relOffs.left + $(relSel).width(); break;
                                            case 'b': relDim = relOffs.top + $(relSel).height(); break;
                                        }
                                        var $par = $(el).offsetParent();
                                        var parOffs = $par.offset();
                                        var thisOffs = $(el).offset();
                                        var pxOffs = 0;
                                        switch (c) {
                                            case 'left': pxOffs = relDim - parOffs.left; break;
                                            case 'top': pxOffs = relDim - parOffs.top; break;
                                            case 'right': pxOffs = (parOffs.left + $par.width()) - relDim; break;
                                            case 'bottom': pxOffs = (parOffs.top + $par.height()) - relDim; break;
                                            case 'margin-left': pxOffs = cssValue($(el), 'margin-left') + relDim - thisOffs.left; break;
                                            case 'margin-top': pxOffs = cssValue($(el), 'margin-top') + relDim - thisOffs.top; break;
                                            case 'margin-right': pxOffs = cssValue($(el), 'margin-right') + (thisOffs.left + $(el).width()) - relDim; break;
                                            case 'margin-bottom': pxOffs = cssValue($(el), 'margin-bottom') + (thisOffs.top + $(el).height()) - relDim; break;
                                        }
                                        px = val + pxOffs;
                                    }
                                    break;
                            }
                            $(el).css(c, px + 'px');
                            console.log('= ' + px + 'px');
                            trackLayout = true;
                        } else if (!onlyLayout) {
                            $(el).css(c, changes[c]);
                        }
                    } else if (!onlyLayout)  {
                        if (el[c] != changes[c])
                            el[c] = changes[c];
                    }
                }
            });
            $cont.data('hs-track-layout', trackLayout);
            return trackLayout;
        }

        //var $placeholder = $("<div></div>");
        //$placeholder.insertAfter(this);
        var wrapId = '_hs_temp_wrapper_' + $this.prop('id');
        $this.wrap("<div id='" + wrapId + "'></div>");
        $('#' + wrapId).height($this.height());
        // Pulls the layout item out of the document so that
        // all child changes are redrawn in one go.
        this.detach();
        construct(this, layout, 'col', '');
        cleanContainers(this);
        var trackLayout = substitute(this, substituteSel, idx);
        this.addClass('hs-container').addClass('hs-outer-container');
        // put the layout container back into the document
        this.appendTo($('#' + wrapId));
        this.unwrap();

        var this2 = this;
        console.log('trackLayout ' + trackLayout);
        if (trackLayout)
            substitute(this2, substituteSel, idx, true);

        // trigger event just after reinsertion into dom - means we can rely on 
        // proper dimensions
        $this.trigger('hopscotch:layout', [idxOld, idx]);

        this.data('hs-layout-idx', idx);
        return this;
    }
    // runs hopscotchLayout if width crosses a stop point
    $.fn.hopscotchResize = function (stopPoints, substituteSel, onChange, beforeChange) {
        var $this = this;


        if ($.type(stopPoints) === 'string') {
            var cmd = stopPoints;
            if (cmd == "update") {
                $this.data('hs-layout-idx', -1);
                $this.trigger('resize');
            }
            return $this;
        }
        if ($.isFunction(substituteSel)) {
            beforeChange = onChange;
            onChange = substituteSel;
            substituteSel = null;
        }
        $this.bind('resize', function () { setLayout($(this)); });
        
        function setLayout($resized) {
            // check for width oscillation
            clearTimeout($resized.data('hs-osc-timeout'));
            $resized.data('hs-osc-timeout', setTimeout(function () { $resized.data('hs-prev-widths', []); }, 800));
            var width = $resized.width();
            var widths = $resized.data('hs-prev-widths') || [];
            if (widths.indexOf(width) >= 0) {
                return;
            }
            if (widths.push(width) > 2)
                widths.shift();
            $resized.data('hs-prev-widths', widths);

            for (var idx = 0; idx < stopPoints.length; idx++) {
                if (width < stopPoints[idx])
                    break;
            }
            $resized.hopscotchLayout(idx);
        }
        if ($.isFunction(onChange))
            $this.bind('hopscotch:layout', function (ev) { ev.stopPropagation(); onChange(ev); });
        if ($.isFunction(beforeChange))
            $this.bind('hopscotch:beforeLayout', function (ev) { ev.stopPropagation(); beforeChange(ev); });
        setLayout($this);

        return $this;
    }

})(jQuery, this);