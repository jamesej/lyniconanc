var lynicon = {
    getCrop: function (url, cropDef) {
        if (url == null) {
            return url;
        }
        var pathParts = url.split("/");
        if (pathParts.length == 1)
            return url;
        var fileParts = pathParts[pathParts.length - 1].split(".");
        if (fileParts.length == 2)
            fileParts.splice(1, 0, cropDef);
        else if (fileParts.length == 3)
            fileParts[1] = cropDef;
        pathParts[pathParts.length - 1] = fileParts.join(".");
        return pathParts.join("/");
    }
}