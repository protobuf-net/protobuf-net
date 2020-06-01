
require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.18.0/min/vs' } });

// Before loading vs/editor/editor.main, define a global MonacoEnvironment that overwrites
// the default worker url location (used when creating WebWorkers). The problem here is that
// HTML5 does not allow cross-domain web workers, so we need to proxy the instantiation of
// a web worker through a same-domain script
window.MonacoEnvironment = {
    getWorkerUrl: function (workerId, label) {
        return `data:text/javascript;charset=utf-8,${encodeURIComponent(`
                                            self.MonacoEnvironment = {
                                              baseUrl: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.18.0/min/'
                                            };
                                            importScripts('https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.18.0/min/vs/base/worker/workerMain.js');`
        )}`;
    }
};

require(["vs/editor/editor.main"], function () {
    monaco.languages.setMonarchTokensProvider('protobuf', {
        keywords: [
            'import', 'option', 'message', 'package', 'service',
            'optional', 'rpc', 'returns', 'return', 'true', 'false'
        ],
        typeKeywords: [
            'double', 'float', 'int32', 'int64', 'uint32',
            'uint64', 'sint32', 'sint64', 'fixed32', 'fixed64',
            'sfixed32', 'sfixed64', 'bool', 'string', 'bytes'
        ],
        operators: [
            '=', '>', '<', '!', '~', '?', ':', '==', '<=', '>=', '!=',
            '&&', '||', '++', '--', '+', '-', '*', '/', '&', '|', '^', '%',
            '<<', '>>', '>>>', '+=', '-=', '*=', '/=', '&=', '|=', '^=',
            '%=', '<<=', '>>=', '>>>='
        ],
        symbols: /[=><!~?:&|+\-*\/^%]+/,
        escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,
        tokenizer: {
            root: [
                [/[a-z_$][\w$]*/, {
                    cases: {
                        '@typeKeywords': 'typeKeyword',
                        '@keywords': 'keyword',
                        '@default': 'identifier'
                    }
                }],
                [/[A-Z][\w\$]*/, 'type.identifier'],
                { include: '@whitespace' },

                // delimiters and operators
                [/[{}()\[\]]/, '@brackets'],
                [/[<>](?!@symbols)/, '@brackets'],
                [/@symbols/, {
                    cases: {
                        '@operators': 'operator',
                        '@default': ''
                    }
                }],
                // @ annotations.
                [/@\s*[a-zA-Z_\$][\w\$]*/, { token: 'annotation', log: 'annotation token: $0' }],
                // numbers
                [/\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
                [/0[xX][0-9a-fA-F]+/, 'number.hex'],
                [/\d+/, 'number'],
                // delimiter: after number because of .\d floats
                [/[;,.]/, 'delimiter'],
                // strings
                [/"([^"\\]|\\.)*$/, 'string.invalid'], // non-teminated string
                [/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],
                // characters
                [/'[^\\']'/, 'string'],
                [/(')(@escapes)(')/, ['string', 'string.escape', 'string']],
                [/'/, 'string.invalid']
            ],
            comment: [
                [/[^\/*]+/, 'comment'],
                [/\/\*/, 'comment', '@push'], // nested comment
                ["\\*/", 'comment', '@pop'],
                [/[\/*]/, 'comment']
            ],
            string: [
                [/[^\\"]+/, 'string'],
                [/@escapes/, 'string.escape'],
                [/\\./, 'string.escape.invalid'],
                [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }]
            ],
            whitespace: [
                [/[ \t\r\n]+/, 'white'],
                [/\/\*/, 'comment', '@comment'],
                [/\/\/.*$/, 'comment']
            ]
        }
    });
    monaco.editor.defineTheme('protobuf', {
        base: 'vs',
        inherit: true,
        rules: [
            { token: 'keyword', foreground: 'DB2121' },
            { token: 'typeKeyword', foreground: 'F84842', fontStyle: 'italic' },
            { token: 'identifier', foreground: '0C5ED7', fontStyle: 'bold' },
            { token: 'type.identifier', foreground: '00CA8C', fontStyle: 'bold' },
            { token: 'comment', foreground: '7A7A7A' },
            { token: 'number', foreground: '000000', fontStyle: 'italic' },
            { token: 'string', fontStyle: 'italic' }
        ]
    });
    monaco.languages.register({ id: 'protobuf' });
    window.initMonaco = function (block, component, language, readonly) {

        if (!block) {
            return;
        }

        block.monacoEditorModel = monaco.editor.createModel("", language);
        block.monacoEditorModel.onDidChangeContent(function (e) {
            component.invokeMethodAsync('OnEditorValueChanged', block.monacoEditorModel.getValue());
        });
        block.monacoEditor = monaco.editor.create(block, {
            language: language,
            minimap: {
                enabled: false
            },
            readOnly: readonly,
            automaticLayout: true,
            scrollBeyondLastLine: false,
            model: block.monacoEditorModel
        });

    };
    window.addMonacoError = function (block, lineNumber, lineEnd, columnNumber, columnEnd, message, isError) {
        var existingErrors = [];
        if (block.monacoErrors) {
            existingErrors = block.monacoErrors;
        }
        existingErrors.push({
            startLineNumber: lineNumber,
            startColumn: columnNumber,
            endLineNumber: lineEnd,
            endColumn: columnEnd,
            message: message,
            severity: isError ? monaco.MarkerSeverity.Error : monaco.MarkerSeverity.Warning
        });
        monaco.editor.setModelMarkers(block.monacoEditorModel, "owner", existingErrors);
        block.monacoErrors = existingErrors;
    };
    window.cleanMonacoError = function (block) {
        block.monacoErrors = [];
        monaco.editor.setModelMarkers(block.monacoEditorModel, "owner", []);
    };
    window.setMonaco = function (block, value) {
        return block.monacoEditor.setValue(value);
    };
    window.getMonaco = function (block) {
        return block.monacoEditor.getValue();
    };
});