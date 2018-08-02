require.config({ paths: { vs: 'lib/monaco-editor/min/vs' } });
require(['vs/editor/editor.main', 'js/proto3lang'], function (_, proto3lang)
{
    monaco.languages.register({ id: 'proto3lang' });
    monaco.languages.setMonarchTokensProvider('proto3lang', proto3lang);
    var editor = monaco.editor.create(document.getElementById('protocontainer'), {
        language: 'proto3lang'
    });
    var codeViewer = null;
    var codeResultSection = document.getElementById("coderesult");
    var oldDecorations = [];

    $(document).ready(function () {
        var hash = window.location.hash;
        if (hash.indexOf('#g') === 0 && hash.length > 2) {
            
            $.ajax({
                url: 'https://api.github.com/gists/' + hash.substr(2),
                type: 'GET',
                dataType: 'jsonp'
            }).success(function (gistdata) {
                // This can be less complicated if you know the gist file name
                var objects = [];
                for (file in gistdata.data.files) {
                    if (gistdata.data.files.hasOwnProperty(file)) {
                        editor.setValue(gistdata.data.files[file].content);
                        break;
                    }
                }
            }).error(function (e) { });
        }
    });
    document.getElementById("generatecsharp").addEventListener("click", function ()
    {
        var postData = {
            schema: editor.getValue({ preserveBOM: false, lineEnding: "\n" }),
            tooling: $('#tooling').find(":selected").val(),
            langver: $('#opt_langver').find(":selected").val(),
            names: $('#opt_names').find(":selected").val()
        };
        if ($('#opt_oneof').is(":checked")) {
            postData.oneof = $('#opt_oneof').val();
        }
        if ($('#opt_listset').is(":checked")) {
            postData.listset = $('#opt_listset').val();
        }
        jQuery.post("/generate", postData, function(data, textStatus, jqXHR)
        {
            if (data === null || data === undefined)
            {
                return;
            }
            var decorations = [];
            if (data.files && data.files.length)
            {
                var code = data.files[0].text;
                codeResultSection.style.display = "";
                if (codeViewer === null)
                {
                    codeViewer = monaco.editor.create(document.getElementById('csharpcontainer'), {
                        value: code,
                        language: 'csharp',
                        readOnly: true
                    });
                }
                else
                {
                    codeViewer.setValue(code);
                }
            }
            if (data.parserExceptions)
            {
                var length = data.parserExceptions.length;
                var haveErrors = false;
                for (var i = 0; i < length; i++)
                {
                    var parserException = data.parserExceptions[i];
                    if (parserException.isError) { haveErrors = true; }
                    decorations.push({
                        range: new monaco.Range(parserException.lineNumber, parserException.columnNumber, parserException.lineNumber, parserException.columnNumber + parserException.text.length),
                        options: {
                            inlineClassName: parserException.isError ? "redsquiggly" : "greensquiggly",
                            hoverMessage: parserException.message,
                            overviewRuler: {
                                color: parserException.isError ? "#E47777" : "#71B771",
                                position: parserException.isError ? monaco.editor.OverviewRulerLane.Right : monaco.editor.OverviewRulerLane.Center
                            }
                        }
                    });
                }
                if (haveErrors) { codeResultSection.style.display = "none"; }
            }
            if (data.exception)
            {
                codeResultSection.style.display = "none";
                decorations.push({
                        range: new monaco.Range(1, 1, editor.getModel().getLineCount(), 1),
                        options: {
                            isWholeLine: true,
                            inlineClassName: "redsquiggly",
                            hoverMessage: data.exception.message
                        }
                    });
            }
            oldDecorations = editor.deltaDecorations(oldDecorations, decorations);
        }, "json");
    });
});