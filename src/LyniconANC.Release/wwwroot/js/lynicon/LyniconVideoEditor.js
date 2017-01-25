$(document).ready(function () {

    function parseVideoData(data) {
        return JSON.parse(data);
    }

    function setVideoFilename($input, $mobileVidInput, $posterInput, fname, fNameObj) {
        var posterFname = null;
        if (fname.indexOf("|") >= 0) {
            posterFname = fname.split("|")[1];
            fname = fname.split("|")[0];
            $posterInput.val(posterFname);
            $posterInput.siblings(".text-display").text(posterFname);
            $posterInput.closest(".lyn-image").find(".lyn-image-content").show().html("<div class='file-image-thumb' style='background-image:url(" + posterFname + ")'></div>");
            $posterInput.closest(".lyn-image").find(".lyn-image-url, .lyn-image-alt").hide();
        }

        if (fNameObj.Mobile != null) {
            $mobileVidInput.val(fNameObj.Mobile);
        }

        if (fNameObj.Desktop != null) {
            $input.val(fNameObj.Desktop);
            $input.siblings(".text-display").text(fNameObj.Desktop);
            $input.closest(".lyn-video").find(".lyn-video-content").show().html("<video class=\"thumb-vid\" src=\"" + fNameObj.Desktop + "\"" + (posterFname ? " poster=\"" + posterFname + "\"" : "") + " controls/>");
        }


        $input.closest(".lyn-video").find(".lyn-video-url, .lyn-video-poster").hide();
        setTimeout(notifyLayout, 200);
    }

    $("body").on("click", ".lyn-video", function (ev) {
        if ($(ev.target).is(".lyn-video-load, input, .lyn-image") || $(ev.target).closest(".lyn-image").length)
            return;
        $(this).find(".lyn-video-content, .lyn-video-url, .lyn-video-poster").toggle();
    }).on("click", ".lyn-video-load", function () {
        var $this = $(this);
        var $fname = $this.closest(".lyn-video").find(".lyn-video-url input");
        var $mobileVidInput = $this.closest(".lyn-video").find(".lyn-video-mobile-url input");
        var $posterFname = $this.closest(".lyn-video").find(".lyn-video-poster .lyn-image-url input");
        var info = null;
        top.getFile(null, info, function (fname) {
            //var files = fname.split(",");
            if ($this.hasClass("lyn-video-load")) {

                function checkFileIsVideo(filePath) {
                    var suffix = filePath.split("|")[0].afterLast(".").upTo("?").toLowerCase();
                    if (suffix && suffix.length && "mp4|webm".indexOf(suffix) < 0) {
                        return false;
                    }
                    return true;
                }

                //for (var i = 0; i < files.length; i++) {
                //    var suffix = files[i].split("|")[0].afterLast(".").upTo("?").toLowerCase();
                //    if (suffix && suffix.length && "mp4|webm".indexOf(suffix) < 0)
                //        return "Please only video files";
                //}
                function replaceAll(str, oldVal, newVal) {
                    var replacedStr = str.replace(oldVal, newVal);
                    if (replacedStr == str) {
                        return replacedStr;
                    }
                    return replaceAll(replacedStr, oldVal, newVal);
                }
                fname = replaceAll(fname, "'", "\"");
                var splitFname = fname.split("|")[0];
                if (fname.indexOf("object Object") > -1) {
                    alert('Video is still being encoded');
                    return;
                }
                var values = parseVideoData(splitFname);
                if (!checkFileIsVideo(values.Mobile) || !checkFileIsVideo(values.Desktop)) {
                    return "Please only video files";
                }
            }
            //if (files.length == 1) {
                setVideoFilename($fname, $mobileVidInput, $posterFname, fname, values);
            //} else {
                //if (confirm("You have selected " + files.length + " files, do you want to add them all?")) {
                //    var $addButton = $this.closest(".collection").children(".add-button");
                //    setVideoFilename($fname, $mobileVidInput, $posterFname, $.trim(files[0]));
                //    for (var i = 1; i < files.length; i++) {
                //        addItem($addButton, i, function ($added, idx) {
                //            setVideoFilename($added.find(".lyn-file-url"), $posterFname, $.trim(files[idx]));
                //        });
                //    }
                //} else
                //    return "Please select your files";
            //}
            return null;
        });
        return false;
    });
});