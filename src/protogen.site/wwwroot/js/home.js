require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' } });
require(['vs/editor/editor.main', 'js/proto3lang'], function (_, proto3lang)
{
    monaco.languages.register({ id: 'proto3lang' });
    monaco.languages.setMonarchTokensProvider('proto3lang', proto3lang);
    var editor = monaco.editor.create(document.getElementById('protocontainer'), {
        language: 'proto3lang'
    });
    var codeViewer = null;
    var codeResultSection = document.getElementById("coderesult");
    var oldDecorations = []
    document.getElementById("generatecsharp").addEventListener("click", function ()
    {
        jQuery.post("/generate", "schema=" + editor.getValue({ preserveBOM: false, lineEnding: "\n" }), function (data, textStatus, jqXHR)
        {
            if (data == null)
            {
                return;
            }
            var decorations = [];
            if (data.code)
            {
                codeResultSection.style.display = "";
                if (codeViewer == null)
                {
                    codeViewer = monaco.editor.create(document.getElementById('csharpcontainer'), {
                        value: data.code,
                        language: 'csharp',
                        readOnly: true
                    });
                }
                else
                {
                    codeViewer.setValue(data.code);
                }
            }
            if (data.parserExceptions)
            {
                codeResultSection.style.display = "none";
                var length = data.parserExceptions.length;
                for (var i = 0; i < length; i++)
                {
                    var parserException = data.parserExceptions[i];
                    decorations.push({
                        range: new monaco.Range(parserException.lineNumber, parserException.columnNumber, parserException.lineNumber, parserException.columnNumber + parserException.text.length),
                        options: {
                            inlineClassName: parserException.isError ? "redsquiggly" : "greensquiggly",
                            hoverMessage: parserException.message
                        }
                    });
                }
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