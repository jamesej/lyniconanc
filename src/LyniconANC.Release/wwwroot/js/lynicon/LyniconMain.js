String.prototype.left = function(n) {
    if (n <= 0) return "";
    else if (n > this.length) return this;
    else return this.substring(0, n);
}

String.prototype.right = function(n) {
    if (n <= 0) return "";
    else if (n > this.length) return this;
    else {
        var len = this.length;
        return this.substring(len, len - n);
    }
};

String.prototype.upTo = function(s) {
    var pos = this.indexOf(s);
    if (pos < 0) return this;
    return this.substring(0, pos);
}

String.prototype.upToLast = function(s) {
    var pos = this.lastIndexOf(s);
    if (pos < 0) return this;
    return this.substring(0, pos);
}

String.prototype.after = function(s){
    var pos = this.indexOf(s);
    if (pos < 0) return "";
    return this.substring(pos + s.length);
}

String.prototype.afterLast = function(s) {
    var pos = this.lastIndexOf(s);
    if (pos < 0) return "";
    return this.substring(pos + s.length);
}

function ensureKeyValue(url, key, val) {
    if (val) {
        var conj = '';
        if (url.indexOf(key + "=") < 0)
            conj = url.indexOf('?') < 0 ? "?" : "&";
        var tail = url.after(key + "=").after("&");
        return url.upTo(key + "=") + conj + key + "=" + encodeURI(val) + (tail ? ("&" + tail) : "");
    } else {
        var newUrl = url.upTo(key + "=") + url.after(key + "=").after("&");
        if (newUrl.substr(newUrl.length - 1) == '&' || newUrl.substr(newUrl.length - 1) == '?')
            newUrl = newUrl.substr(0, newUrl.length - 1);
        return newUrl;
    }
}

function initAjax() {
    $('body').css('cursor', 'wait');
}
function endAjax() {
    $('body').css('cursor', 'auto');
}
$.ajaxSetup({ error: endAjax });
