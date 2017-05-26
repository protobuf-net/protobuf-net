/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

$private = $jsilcore;

JSIL.DeclareNamespace("JSIL");
JSIL.DeclareNamespace("JSIL.XML");
JSIL.DeclareNamespace("System");
JSIL.DeclareNamespace("System.Xml");

    JSIL.MakeEnum(
      "System.Xml.XmlNodeType", true, {
          None: 0,
          Element: 1,
          Attribute: 2,
          Text: 3,
          CDATA: 4,
          EntityReference: 5,
          Entity: 6,
          ProcessingInstruction: 7,
          Comment: 8,
          Document: 9,
          DocumentType: 10,
          DocumentFragment: 11,
          Notation: 12,
          Whitespace: 13,
          SignificantWhitespace: 14,
          EndElement: 15,
          EndEntity: 16,
          XmlDeclaration: 17
      }, false
    );

    ﻿JSIL.ImplementExternals("System.Xml.XmlNameTable", function ($) {
        $.Method({ Static: false, Public: false }, ".ctor",
          JSIL.MethodSignature.Void,
          function () {
              this._names = {};
          }
        );

        $.Method({ Static: false, Public: true }, "Add",
          new JSIL.MethodSignature($.String, [$.String], []),
          function Add(str) {
              var result = this._names[str];
              if (typeof (result) === "string")
                  return result;

              this._names[str] = str;
              return str;
          }
        );

        $.Method({ Static: false, Public: true }, "Get",
          new JSIL.MethodSignature($.String, [$.String], []),
          function Get(str) {
              var result = this._names[str];

              if (typeof (result) !== "string")
                  return null;

              return result;
          }
        );

    });

    JSIL.MakeClass("System.Object", "System.Xml.XmlNameTable", true, [], function ($) {
      $.ExternalMembers(false,
        ".ctor", "Add", "Get"
      );
    });

    ﻿    ﻿    JSIL.XML.ReaderFromStream = function (stream) {
      // FIXME: Won't work if the stream is written to while being read from.

      var streamLength = stream.Length.ToInt32();
      var bytes = JSIL.Array.New(System.Byte, streamLength);
      stream.Read(bytes, 0, streamLength);

      var xml;

      // Detect UTF-8 BOM and remove it because browsers choke on it.
      if ((bytes[0] === 0xEF) && (bytes[1] === 0xBB) && (bytes[2] === 0xBF)) {
        xml = System.Text.Encoding.UTF8.$decode(bytes, 3, bytes.length - 3);
      } else {
        xml = JSIL.StringFromByteArray(bytes);
      }

      return JSIL.XML.ReaderFromString(xml);
    };

    JSIL.XML.ReaderFromString = function (xml) {
      var parser = new DOMParser();
      var root = parser.parseFromString(xml, "application/xml");

      if ((root === null) || (root.documentElement.localName == "parsererror")) {
        throw new Error("Failed to parse XML document");
      }

      var result = JSIL.CreateInstanceOfType(
        System.Xml.XmlReader.__Type__, "$fromDOMNode", [root]
      );
      return result;
    };


    JSIL.ImplementExternals("System.Xml.XmlReader", function ($) {
        var ntNone = System.Xml.XmlNodeType.None;
        var ntElement = System.Xml.XmlNodeType.Element;
        var ntAttribute = System.Xml.XmlNodeType.Attribute;
        var ntText = System.Xml.XmlNodeType.Text;
        var ntWhitespace = System.Xml.XmlNodeType.Whitespace;
        var ntComment = System.Xml.XmlNodeType.Comment;
        var ntDocument = System.Xml.XmlNodeType.Document;
        var ntEndElement = System.Xml.XmlNodeType.EndElement;

        var docProto = (window.Document.prototype);
        var elementProto = (window.Element.prototype);
        var attrProto = (window.Attr.prototype);
        var textProto = (window.Text.prototype);

        var sNode = "node";
        var sChildren = "children";
        var sSiblings = "siblings";
        var sClosing = "closing";

        var sBeforeDocument = "before document";
        var sAfterDocument = "after document";

        $.RawMethod(false, "$fromDOMNode", function (domNode) {
            this._domNode = domNode;
            this._eof = false;
            this.$setCurrentNode(null, sBeforeDocument);
            this.nameTable = new System.Xml.XmlNameTable();
            this.advanceCount = 0;
        });

        $.RawMethod(false, "$setCurrentNode", function (node, state) {
            this._current = node;
            // Index of next attribute to be fetched using LocalName/Prefix/Value
            // when fetchMode is 'attrs'
            this.currentAttributeIndex = 0;
            // Fetch mode can be 'attrs' or 'element'. It is switched when calling funcs
            // MoveToNextAttribute and MoveToElement
            this.fetchMode = 'element';
            this._state = state;

            if ((typeof (node) === "undefined") || (node === null)) {
                this._nodeType = ntNone;
                return false;
            }

            if (typeof (node) !== "object") {
                throw new Error("Non-object node:" + String(node));
            }

            switch (node.nodeType) {
                case Node.ELEMENT_NODE:
                    if (state === sClosing) {
                        this._nodeType = ntEndElement;
                    } else {
                        this._nodeType = ntElement;
                    }
                    break;
                case Node.TEXT_NODE:
                    if (System.String.IsNullOrWhiteSpace(node.nodeValue)) {
                        this._nodeType = ntWhitespace;
                    } else {
                        this._nodeType = ntText;
                    }
                    break;
                case Node.COMMENT_NODE:
                    this._nodeType = ntComment;
                    break;
                case Node.DOCUMENT_NODE:
                    if (state !== sClosing) {
                        // Skip directly to the root node
                        return this.$setCurrentNode(node.firstChild, "node");
                    } else {
                        return this.$setCurrentNode(null, sAfterDocument);
                    }
                default:
                    JSIL.Host.warning("Unsupported node type: " + node.nodeType + " " + node);
                    return false;
            }

            return true;
        });

        $.RawMethod(false, "$moveNext", function () {
            var cur = this._current;
            if (cur === null) {
                if (this._eof) {
                    return this.$setCurrentNode(null, sAfterDocument);
                } else {
                    return this.$setCurrentNode(this._domNode, sNode);
                }
            }

            if (this._state === sNode) {
                this._state = sChildren;
            }

            if (this._state === sChildren) {
                if (cur.firstChild !== null)
                    return this.$setCurrentNode(cur.firstChild, sNode);

                this._state = sClosing;
            }

            if (this._state === sSiblings) {
                if (cur.nextSibling !== null)
                    return this.$setCurrentNode(cur.nextSibling, sNode);

                this._state = sClosing;
            }

            if (this._state === sClosing) {
                if (cur.nextSibling !== null)
                    return this.$setCurrentNode(cur.nextSibling, sNode);

                return this.$setCurrentNode(cur.parentNode, sClosing);
            }

            this._eof = true;
            return this.$setCurrentNode(null, sAfterDocument);
        });

        $.RawMethod(false, "$skip", function () {
            var cur = this._current;
            if (cur === null) {
                this._eof = true;
                return this.$setCurrentNode(null, sAfterDocument);
            }

            if (cur.nextSibling !== null) {
                return this.$setCurrentNode(cur.nextSibling, sNode);
            } else if (cur.parentNode !== null) {
                return this.$setCurrentNode(cur.parentNode, sClosing);
            } else {
                this._eof = true;
                return this.$setCurrentNode(null, sAfterDocument);
            }
        });

        $.Method({ Static: false, Public: true }, "Read",
          (new JSIL.MethodSignature($.Boolean, [], [])),
          function Read() {
              this.advanceCount += 1;
              return this.$moveNext();
          }
        );

        $.Method({ Static: false, Public: true }, "ReadToFollowing",
          (new JSIL.MethodSignature($.Boolean, [$.String], [])),
          function ReadToFollowing(localName) {
              while (this.Read()) {
                  if ((this._nodeType === ntElement) && (this.get_LocalName() === localName))
                      return true;
              }

              return false;
          }
        );

        $.Method({ Static: false, Public: true }, "ReadToNextSibling",
          (new JSIL.MethodSignature($.Boolean, [$.String], [])),
          function ReadToNextSibling(localName) {
              while (this.$skip()) {
                  if ((this._nodeType === ntElement) && (this.get_LocalName() === localName))
                      return true;
                  else if (this._nodeType === ntEndElement)
                      return false;
              }

              return false;
          }
        );

        $.Method({ Static: false, Public: true }, "Skip",
          (JSIL.MethodSignature.Void),
          function Skip() {
              this.$skip();
          }
        );

        $.Method({ Static: false, Public: true }, "MoveToElement",
          (new JSIL.MethodSignature($.Boolean, [], [])),
          function MoveToElement() {
              if (this.fetchMode == 'attrs') {
                  this.fetchMode = 'element';
                  this.currentAttributeIndex = 0;
                  // true if position has been "changed"
                  return true;
              }
              return false;
          }
        );

        $.Method({ Static: false, Public: true }, "MoveToFirstAttribute",
          (new JSIL.MethodSignature($.Boolean, [], [])),
          function MoveToFirstAttribute() {
              // FIXME
              return false;
          }
        );

        $.Method({ Static: false, Public: true }, "MoveToNextAttribute",
          (new JSIL.MethodSignature($.Boolean, [], [])),
          function MoveToNextAttribute() {
              if (this.fetchMode == 'element') {
                  if (this._current.attributes.length != 0) {
                      this.fetchMode = 'attrs';
                      this.currentAttributeIndex = 0;
                      return true;
                  } else
                      return false;
              } else {
                  if (this.currentAttributeIndex + 1 < this._current.attributes.length) {
                      this.currentAttributeIndex++;
                      return true;
                  } else
                      return false;
              }
          }
        );

        $.Method({ Static: false, Public: true }, "MoveToContent",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlNodeType"), [], [])),
          function MoveToContent() {
              while (true) {
                  switch (this._nodeType) {
                      case ntText:
                      case ntElement:
                      case ntEndElement:
                          return this._nodeType;
                  }

                  if (!this.Read())
                      return this._nodeType;
              }
          }
        );

        $.RawMethod(false, "$isTextualNode", function (includingComments) {
            switch (this._nodeType) {
                case ntText:
                case ntWhitespace:
                    return true;
                case ntComment:
                    return includingComments;
            }

            return false;
        });

        $.Method({ Static: false, Public: true }, "get_IsEmptyElement",
          (new JSIL.MethodSignature($.Boolean, [], [])),
          function get_IsEmptyElement() {
              if (this._current === null)
                  return true;

              if (this.$isTextualNode(true))
                  return false;

              // The DOM makes it impossible to tell whether an element is actually an empty element.
              // Furthermore, all elements with no children become empty elements when being serialized
              //  in Mozilla.
              // So, that sucks. This is broken.

              var noChildren = (typeof (this._current.childNodes) === "undefined") ||
                (this._current.childNodes === null) ||
                (this._current.childNodes.length === 0);

              return noChildren;
          }
        );

        $.Method({ Static: false, Public: true }, "IsStartElement",
          (new JSIL.MethodSignature($.Boolean, [], [])),
          function IsStartElement() {
              return this.MoveToContent() == ntElement;
          }
        );

        $.Method({ Static: false, Public: true }, "IsStartElement",
          (new JSIL.MethodSignature($.Boolean, [$.String], [])),
          function IsStartElement(name) {
              return (this.MoveToContent() == ntElement) &&
                (this.Name == name);
          }
        );

        $.Method({ Static: false, Public: true }, "IsStartElement",
          (new JSIL.MethodSignature($.Boolean, [$.String, $.String], [])),
          function IsStartElement(localname, ns) {
              return (this.MoveToContent() == ntElement) &&
                (this.LocalName == localname) &&
                (this.NamespaceURI == ns);
          }
        );

        $.Method({ Static: false, Public: true }, "ReadStartElement",
          (JSIL.MethodSignature.Void),
          function ReadStartElement() {
              if (!this.IsStartElement())
                  throw new Error("Start element not found");

              this.Read();
          }
        );

        $.Method({ Static: false, Public: true }, "ReadEndElement",
          (JSIL.MethodSignature.Void),
          function ReadEndElement() {
              if (this.MoveToContent() != ntEndElement)
                  throw new Error("End element not found");

              this.Read();
          }
        );

        $.Method({ Static: false, Public: true }, "ReadStartElement",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function ReadStartElement(name) {
              if (!this.IsStartElement(name))
                  throw new Error("Start element not found");

              this.Read();
          }
        );

        $.Method({ Static: false, Public: true }, "ReadStartElement",
          (new JSIL.MethodSignature(null, [$.String, $.String], [])),
          function ReadStartElement(localname, ns) {
              if (!this.IsStartElement(localname, ns))
                  throw new Error("Start element not found");

              this.Read();
          }
        );

        $.Method({ Static: false, Public: true }, "ReadElementString",
          (new JSIL.MethodSignature($.String, [], [])),
          function ReadElementString() {
              this.ReadStartElement();

              var result = this.ReadString();
              this.ReadEndElement();
              return result;
          }
        );

        $.Method({ Static: false, Public: true }, "ReadElementString",
          (new JSIL.MethodSignature($.String, [$.String], [])),
          function ReadElementString(name) {
              this.ReadStartElement(name);

              var result = this.ReadString();
              this.ReadEndElement();
              return result;
          }
        );

        $.Method({ Static: false, Public: true }, "ReadElementString",
          (new JSIL.MethodSignature($.String, [$.String, $.String], [])),
          function ReadElementString(localname, ns) {
              this.ReadStartElement(localname, ns);

              var result = this.ReadString();
              this.ReadEndElement();
              return result;
          }
        );

        $.Method({ Static: false, Public: true }, "ReadContentAsString",
          (new JSIL.MethodSignature($.String, [], [])),
          function ReadContentAsString() {
              return this.ReadString();
          }
        );

        $.Method({ Static: false, Public: true }, "ReadString",
          (new JSIL.MethodSignature($.String, [], [])),
          function ReadString() {
              var result = "";
              // this.MoveToElement();

              // If we're positioned on a start element, advance into the body to find the text
              if (this._nodeType == ntElement) {
                  if (this.get_IsEmptyElement())
                      return result;

                  if (!this.Read())
                      throw new Error("Failed to read string");

                  if (this._nodeType == ntEndElement)
                      return result;
              }

              while (this.$isTextualNode(false)) {
                  result += this._current.nodeValue;

                  if (!this.Read())
                      break;
              }

              return result;
          }
        );

        $.Method({ Static: false, Public: true }, "ReadSubtree",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [], [])),
          function ReadSubtree() {
            var result = JSIL.CreateInstanceOfType(System.Xml.XmlReader.__Type__, "$fromDOMNode", [this._current.cloneNode(true)]);

            this._state = sSiblings;
            return result;
          }
        );

        $.RawMethod(false, "SetupReadElementContent", function () {
            if (this._nodeType != ntElement)
                throw new System.Exception("Invalid start node for ReadElementContent");

            var isEmpty = this.IsEmptyElement;

            this.Read();
            if (isEmpty)
                return false;

            if (this._nodeType == ntEndElement) {
                this.Read();
                return false;
            } else if (this._nodeType == ntElement) {
                throw new System.Exception("Element contains another element, not text content");
            }

            return true;
        });

        $.RawMethod(false, "FinishReadElementContent", function () {
            if (this._nodeType != ntEndElement)
                throw new System.Exception("Read element string content but didn't end on an EndElement");

            this.Read();
        });

        $.Method({ Static: false, Public: true }, "ReadElementContentAsString",
          (new JSIL.MethodSignature($.String, [], [])),
          function ReadElementContentAsString() {
              var result = "";

              if (this.SetupReadElementContent()) {
                  result = this.ReadString();
                  this.FinishReadElementContent();
              }

              return result;
          }
        );

        $.Method({ Static: false, Public: true }, "get_NodeType",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlNodeType"), [], [])),
          function get_NodeType() {
              return this._nodeType;
          }
        );

        $.Method({ Static: false, Public: true }, "get_NameTable",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlNameTable"), [], [])),
          function get_NameTable() {
              return this.nameTable;
          }
        );

        $.Method({ Static: false, Public: true }, "get_Name",
          (new JSIL.MethodSignature($.String, [], [])),
          function get_Name() {
              if (this._current !== null) {
                  if (this.fetchMode == 'element')
                      return this._current.tagName || null;
                  else if (this.fetchMode == 'attrs')
                      return this._current.attributes.item(this.currentAttributeIndex).name;
              }
              return null;
          }
        );

        $.Method({ Static: false, Public: true }, "get_LocalName",
          (new JSIL.MethodSignature($.String, [], [])),
          function get_LocalName() {
              if (this._current !== null) {
                  if (this.fetchMode == 'element')
                      return this._current.localName || null;
                  else if (this.fetchMode == 'attrs')
                      return this._current.attributes.item(this.currentAttributeIndex).localName;
              }
              return null;
          }
        );

        $.Method({ Static: false, Public: true }, "get_NamespaceURI",
          (new JSIL.MethodSignature($.String, [], [])),
          function get_NamespaceURI() {
              if (this._current !== null) {
                  if (this.fetchMode == 'element')
                      return this._current.namespaceURI || "";
                  else if (this.fetchMode == 'attrs')
                      return this._current.attributes.item(this.currentAttributeIndex).namespaceURI;
              }
              return "";
          }
        );

        $.Method({ Static: false, Public: true }, "get_Value",
          (new JSIL.MethodSignature($.String, [], [])),
          function get_Value() {
              if (this._current !== null) {
                  if (this.fetchMode == 'element')
                      return this._current.nodeValue || null;
                  else if (this.fetchMode == 'attrs')
                      return this._current.attributes.item(this.currentAttributeIndex).value;
              }
              return null;
          }
        );

        $.Method({ Static: false, Public: true }, "get_Prefix",
          (new JSIL.MethodSignature($.String, [], [])),
          function get_Value() {
              if (this._current !== null) {
                  if (this.fetchMode == 'element')
                      return this._current.prefix || '';
                  else if (this.fetchMode == 'attrs')
                      return this._current.attributes.item(this.currentAttributeIndex).prefix || '';
              }
              return null;
          }
        );

        $.Method({ Static: false, Public: false }, "get_AdvanceCount",
          (new JSIL.MethodSignature($.Int32, [], [])),
          function get_AdvanceCount() {
              return this.advanceCount;
          }
        );

        $.Method({ Static: false, Public: true }, "get_AttributeCount",
          (new JSIL.MethodSignature($.Int32, [], [])),
          function get_AttributeCount() {
              if (this.$isTextualNode(true))
                  return 0;

              if (this._current !== null)
                  return this._current.attributes.length;

              return 0;
          }
        );

        $.Method({ Static: false, Public: true }, "get_HasAttributes",
                (new JSIL.MethodSignature($.Boolean, [], [])),
                function get_HasAttributes() {
                    if (this._current == null) return false;
                    return (this.currentAttributeIndex < this._current.attributes.length);
                }
        );

        var getAttributeByName = function GetAttribute(name) {
            if (this._current.hasAttribute && this._current.hasAttribute(name))
                return this._current.getAttribute(name);

            return null;
        };

        var getAttributeByNameNS = function GetAttribute(name, namespaceURI) {
            if (this._current.hasAttributeNS && this._current.hasAttributeNS(namespaceURI, name))
                return this._current.getAttributeNS(namespaceURI, name);

            return null;
        };

        var getAttributeByIndex = function GetAttribute(i) {
            return this._current.attributes[i].value;
        };

        $.Method({ Static: false, Public: true }, "GetAttribute",
          (new JSIL.MethodSignature($.String, [$.String], [])),
          getAttributeByName
        );

        $.Method({ Static: false, Public: true }, "GetAttribute",
          (new JSIL.MethodSignature($.String, [$.String, $.String], [])),
          getAttributeByNameNS
        );

        $.Method({ Static: false, Public: true }, "GetAttribute",
          (new JSIL.MethodSignature($.String, [$.Int32], [])),
          getAttributeByIndex
        );

        $.Method({ Static: false, Public: true }, "get_Item",
          (new JSIL.MethodSignature($.String, [$.Int32], [])),
          getAttributeByIndex
        );

        $.Method({ Static: false, Public: true }, "get_Item",
          (new JSIL.MethodSignature($.String, [$.String], [])),
          getAttributeByName
        );

        $.Method({ Static: false, Public: true }, "get_Item",
          (new JSIL.MethodSignature($.String, [$.String, $.String], [])),
          getAttributeByNameNS
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [$.String], [])),
          function Create(inputUri) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [$.String, $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings")], [])),
          function Create(inputUri, settings) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [
                $.String, $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings"),
                $xmlasms[16].TypeRef("System.Xml.XmlParserContext")
          ], [])),
          function Create(inputUri, settings, inputContext) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [$xmlasms[5].TypeRef("System.IO.Stream")], [])),
          function Create(input) {
              return JSIL.XML.ReaderFromStream(input);
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [$xmlasms[5].TypeRef("System.IO.Stream"), $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings")], [])),
          function Create(input, settings) {
              // FIXME
              return JSIL.XML.ReaderFromStream(input);
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [
                $xmlasms[5].TypeRef("System.IO.Stream"), $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings"),
                $.String
          ], [])),
          function Create(input, settings, baseUri) {
              // FIXME
              return JSIL.XML.ReaderFromStream(input);
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [
                $xmlasms[5].TypeRef("System.IO.Stream"), $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings"),
                $xmlasms[16].TypeRef("System.Xml.XmlParserContext")
          ], [])),
          function Create(input, settings, inputContext) {
              // FIXME
              return JSIL.XML.ReaderFromStream(input);
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [$xmlasms[5].TypeRef("System.IO.TextReader")], [])),
          function Create(input) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [$xmlasms[5].TypeRef("System.IO.TextReader"), $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings")], [])),
          function Create(input, settings) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [
                $xmlasms[5].TypeRef("System.IO.TextReader"), $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings"),
                $.String
          ], [])),
          function Create(input, settings, baseUri) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [
                $xmlasms[5].TypeRef("System.IO.TextReader"), $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings"),
                $xmlasms[16].TypeRef("System.Xml.XmlParserContext")
          ], [])),
          function Create(input, settings, inputContext) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [$xmlasms[16].TypeRef("System.Xml.XmlReader"), $xmlasms[16].TypeRef("System.Xml.XmlReaderSettings")], [])),
          function Create(reader, settings) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "Dispose",
          (JSIL.MethodSignature.Void),
          function Dispose() {
              // FIXME
          }
        );

        $.Method({ Static: false, Public: false }, "Dispose",
          (new JSIL.MethodSignature(null, [$.Boolean], [])),
          function Dispose(disposing) {
              // FIXME
          }
        );
    });

    JSIL.MakeClass("System.Object", "System.Xml.XmlReader", true, [], function ($) {
      $.ExternalMembers(false,
        "Read", "MoveToContent",
        "get_AdvanceCount", "get_AttributeCount",
        "get_IsEmptyElement",
        "get_LocalName", "get_NameTable",
        "get_NodeType", "get_Name",
        "get_NamespaceURI", "get_Value"
      );

      $.Property({ Static: false, Public: false }, "AdvanceCount");
      $.Property({ Static: false, Public: true }, "AttributeCount");
      $.Property({ Static: false, Public: true }, "IsEmptyElement");
      $.Property({ Static: false, Public: true }, "LocalName");
      $.Property({ Static: false, Public: true }, "NodeType");
      $.Property({ Static: false, Public: true }, "Name");
      $.Property({ Static: false, Public: true }, "NameTable");
      $.Property({ Static: false, Public: true }, "NamespaceURI");
      $.Property({ Static: false, Public: true }, "Value");

      $.ImplementInterfaces($jsilcore.TypeRef("System.IDisposable"))
    });

    ﻿    ﻿    JSIL.XML.WriterForStream = function (stream) {
      var result = JSIL.CreateInstanceOfType(
        System.Xml.XmlWriter.__Type__, "$forStream", [stream]
      );

      return result;
    };


    JSIL.ImplementExternals("System.Xml.XmlWriter", function ($) {

        $.RawMethod(false, "$forStream", function (stream) {
            this._disposed = false;
            this._stream = stream;
            this._stack = [];
            this._needPrologue = true;
        });

        $.RawMethod(false, "$pushElement", function (elementName) {
            var elt = {
                name: elementName,
                closePending: true,
                endElementPending: true,
                empty: true
            };

            this._stack.push(elt);

            return elt;
        });

        $.RawMethod(false, "$flush", function (forClose) {
            while (this._stack.length > 0) {
                this.$flushOne(forClose);
                this._stack.pop();
            }
        });

        $.RawMethod(false, "$flushOne", function (includeEndElement) {
            var item = this._stack[this._stack.length - 1];
            if (!item)
                return;

            if (item.empty && item.closePending && item.endElementPending && includeEndElement) {
                item.closePending = item.endElementPending = false;
                this.$write(" />");
            } else {
                if (item.closePending) {
                    item.closePending = false;
                    this.$write(">");
                }

                if (item.endElementPending && includeEndElement) {
                    item.endElementPending = false;
                    this.$write("</");
                    this.$write(item.name);
                    this.$write(">");
                }
            }
        });

        $.RawMethod(false, "$writeAttr", function (name, value) {
            var item = this._stack[this._stack.length - 1];
            if (!item)
                throw new Error("No element open");

            if (!item.closePending)
                throw new Error("Element start tag already closed");

            this.$write(" ");
            this.$write(name);
            this.$write("=\"");
            this.$writeEscaped(value);
            this.$write("\"");
        });

        $.RawMethod(false, "$writeEscaped", function (str) {
            this.$write(str);
        });

        $.RawMethod(false, "$write", function (str) {
            if (this._needPrologue)
                this.WriteStartDocument();

            for (var i = 0, l = str.length; i < l; i++) {
                var ch = str[i];
                var byte = ch.charCodeAt(0);
                this._stream.WriteByte(byte);
            }
        });

        $.RawMethod(false, "$dispose", function () {
            if (this._disposed)
                return;

            this._disposed = true;
            this.$flush(true);
            this._stream.Close();
        });

        $.Method({ Static: false, Public: true }, "Close",
          (JSIL.MethodSignature.Void),
          function Close() {
              this.$dispose();
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$.String], [])),
          function Create(outputFileName) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$.String, $xmlasms[16].TypeRef("System.Xml.XmlWriterSettings")], [])),
          function Create(outputFileName, settings) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[5].TypeRef("System.IO.Stream")], [])),
          function Create(output) {
              return JSIL.XML.WriterForStream(output);
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[5].TypeRef("System.IO.Stream"), $xmlasms[16].TypeRef("System.Xml.XmlWriterSettings")], [])),
          function Create(output, settings) {
              // FIXME
              return JSIL.XML.WriterForStream(output);
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[5].TypeRef("System.IO.TextWriter")], [])),
          function Create(output) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[5].TypeRef("System.IO.TextWriter"), $xmlasms[16].TypeRef("System.Xml.XmlWriterSettings")], [])),
          function Create(output, settings) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[5].TypeRef("System.Text.StringBuilder")], [])),
          function Create(output) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[5].TypeRef("System.Text.StringBuilder"), $xmlasms[16].TypeRef("System.Xml.XmlWriterSettings")], [])),
          function Create(output, settings) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[16].TypeRef("System.Xml.XmlWriter")], [])),
          function Create(output) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: true, Public: true }, "Create",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriter"), [$xmlasms[16].TypeRef("System.Xml.XmlWriter"), $xmlasms[16].TypeRef("System.Xml.XmlWriterSettings")], [])),
          function Create(output, settings) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "Flush",
          (JSIL.MethodSignature.Void),
          function Flush() {
              this.$flush(false);
          }
        );

        $.Method({ Static: false, Public: true }, "get_Settings",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlWriterSettings"), [], [])),
          function get_Settings() {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "get_WriteState",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.WriteState"), [], [])),
          function get_WriteState() {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "get_XmlLang",
          (new JSIL.MethodSignature($.String, [], [])),
          function get_XmlLang() {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "get_XmlSpace",
          (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlSpace"), [], [])),
          function get_XmlSpace() {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "LookupPrefix",
          (new JSIL.MethodSignature($.String, [$.String], [])),
          function LookupPrefix(ns) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteAttributes",
          (new JSIL.MethodSignature(null, [$xmlasms[16].TypeRef("System.Xml.XmlReader"), $.Boolean], [])),
          function WriteAttributes(reader, defattr) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteAttributeString",
          (new JSIL.MethodSignature(null, [
                $.String, $.String,
                $.String
          ], [])),
          function WriteAttributeString(localName, ns, value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteAttributeString",
          (new JSIL.MethodSignature(null, [$.String, $.String], [])),
          function WriteAttributeString(localName, value) {
              this.$writeAttr(localName, value);
          }
        );

        $.Method({ Static: false, Public: true }, "WriteAttributeString",
          (new JSIL.MethodSignature(null, [
                $.String, $.String,
                $.String, $.String
          ], [])),
          function WriteAttributeString(prefix, localName, ns, value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteBase64",
          (new JSIL.MethodSignature(null, [
                $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
                $.Int32
          ], [])),
          function WriteBase64(buffer, index, count) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteBinHex",
          (new JSIL.MethodSignature(null, [
                $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
                $.Int32
          ], [])),
          function WriteBinHex(buffer, index, count) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteCData",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteCData(text) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteCharEntity",
          (new JSIL.MethodSignature(null, [$.Char], [])),
          function WriteCharEntity(ch) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteChars",
          (new JSIL.MethodSignature(null, [
                $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32,
                $.Int32
          ], [])),
          function WriteChars(buffer, index, count) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteComment",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteComment(text) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteDocType",
          (new JSIL.MethodSignature(null, [
                $.String, $.String,
                $.String, $.String
          ], [])),
          function WriteDocType(name, pubid, sysid, subset) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteElementString",
          (new JSIL.MethodSignature(null, [$.String, $.String], [])),
          function WriteElementString(localName, value) {
              this.WriteStartElement(localName);

              if (!System.String.IsNullOrWhiteSpace(value))
                  this.WriteString(value);

              this.WriteEndElement();
          }
        );

        $.Method({ Static: false, Public: true }, "WriteElementString",
          (new JSIL.MethodSignature(null, [
                $.String, $.String,
                $.String
          ], [])),
          function WriteElementString(localName, ns, value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteElementString",
          (new JSIL.MethodSignature(null, [
                $.String, $.String,
                $.String, $.String
          ], [])),
          function WriteElementString(prefix, localName, ns, value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteEndAttribute",
          (JSIL.MethodSignature.Void),
          function WriteEndAttribute() {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteEndDocument",
          (JSIL.MethodSignature.Void),
          function WriteEndDocument() {
              this.$flush(true);
          }
        );

        $.Method({ Static: false, Public: true }, "WriteEndElement",
          (JSIL.MethodSignature.Void),
          function WriteEndElement() {
              this.$flushOne(true);
              this._stack.pop();
          }
        );

        $.Method({ Static: false, Public: true }, "WriteEntityRef",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteEntityRef(name) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteFullEndElement",
          (JSIL.MethodSignature.Void),
          function WriteFullEndElement() {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: false }, "WriteLocalNamespaces",
          (new JSIL.MethodSignature(null, [$xmlasms[16].TypeRef("System.Xml.XPath.XPathNavigator")], [])),
          function WriteLocalNamespaces(nsNav) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteName",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteName(name) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteNmToken",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteNmToken(name) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteNode",
          (new JSIL.MethodSignature(null, [$xmlasms[16].TypeRef("System.Xml.XmlReader"), $.Boolean], [])),
          function WriteNode(reader, defattr) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteNode",
          (new JSIL.MethodSignature(null, [$xmlasms[16].TypeRef("System.Xml.XPath.XPathNavigator"), $.Boolean], [])),
          function WriteNode(navigator, defattr) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteProcessingInstruction",
          (new JSIL.MethodSignature(null, [$.String, $.String], [])),
          function WriteProcessingInstruction(name, text) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteQualifiedName",
          (new JSIL.MethodSignature(null, [$.String, $.String], [])),
          function WriteQualifiedName(localName, ns) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteRaw",
          (new JSIL.MethodSignature(null, [
                $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32,
                $.Int32
          ], [])),
          function WriteRaw(buffer, index, count) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteRaw",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteRaw(data) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartAttribute",
          (new JSIL.MethodSignature(null, [$.String, $.String], [])),
          function WriteStartAttribute(localName, ns) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartAttribute",
          (new JSIL.MethodSignature(null, [
                $.String, $.String,
                $.String
          ], [])),
          function WriteStartAttribute(prefix, localName, ns) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartAttribute",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteStartAttribute(localName) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartDocument",
          (JSIL.MethodSignature.Void),
          function WriteStartDocument() {
              this._needPrologue = false;
              this.$write('<?xml version="1.0" encoding="');
              this.$write("utf-8");
              this.$write('"?>');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartDocument",
          (new JSIL.MethodSignature(null, [$.Boolean], [])),
          function WriteStartDocument(standalone) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartElement",
          (new JSIL.MethodSignature(null, [$.String, $.String], [])),
          function WriteStartElement(localName, ns) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartElement",
          (new JSIL.MethodSignature(null, [
                $.String, $.String,
                $.String
          ], [])),
          function WriteStartElement(prefix, localName, ns) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteStartElement",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteStartElement(localName) {
              this.$flushOne(false);

              this.$write("<");
              this.$write(localName);

              var elt = this.$pushElement(localName);
          }
        );

        $.Method({ Static: false, Public: true }, "WriteString",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteString(text) {
              this.$flushOne(false);

              this.$writeEscaped(text);
          }
        );

        $.Method({ Static: false, Public: true }, "WriteSurrogateCharEntity",
          (new JSIL.MethodSignature(null, [$.Char, $.Char], [])),
          function WriteSurrogateCharEntity(lowChar, highChar) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$.Object], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$.Boolean], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$xmlasms[5].TypeRef("System.DateTime")], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$.Double], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$.Single], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$xmlasms[5].TypeRef("System.Decimal")], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$.Int32], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteValue",
          (new JSIL.MethodSignature(null, [$.Int64], [])),
          function WriteValue(value) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "WriteWhitespace",
          (new JSIL.MethodSignature(null, [$.String], [])),
          function WriteWhitespace(ws) {
              throw new Error('Not implemented');
          }
        );

        $.Method({ Static: false, Public: true }, "Dispose",
          (JSIL.MethodSignature.Void),
          function Dispose() {
              // FIXME
              this.$dispose();
          }
        );

        $.Method({ Static: false, Public: false }, "Dispose",
          (new JSIL.MethodSignature(null, [$.Boolean], [])),
          function Dispose(disposing) {
              // FIXME
              this.$dispose();
          }
        );

    });

    JSIL.MakeClass($jsilcore.TypeRef("System.Object"), "System.Xml.XmlWriter", true, [], function ($) {
      $.Field({ Static: false, Public: false }, "writeNodeBuffer", $jsilcore.TypeRef("System.Array", [$.Char]));

      $.Constant({ Static: true, Public: false }, "WriteNodeBufferSize", 1024);

      $.Property({ Static: false, Public: true }, "Settings");
      $.Property({ Static: false, Public: true }, "WriteState");
      $.Property({ Static: false, Public: true }, "XmlLang");
      $.Property({ Static: false, Public: true }, "XmlSpace");

      $.ImplementInterfaces($jsilcore.TypeRef("System.IDisposable"))
    });


var $xmlasms = new JSIL.AssemblyCollection({
    5: "mscorlib",
    6: "System",
    16: "System.Xml",
});

﻿

JSIL.ImplementExternals("System.Xml.Serialization.XmlSerializer", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$xmlasms[5].TypeRef("System.Type")], [])),
      function _ctor(type) {
          this.type = type;
      }
    );


    var getType = function (name) {
        var parsed = JSIL.ParseTypeName(name);
        return JSIL.GetTypeInternal(parsed, JSIL.GlobalNamespace, true);
    };

    $.RawMethod(false, "XmlReaderFromStream", function (stream) {
        return JSIL.XML.ReaderFromStream(stream);
    });

    $.RawMethod(false, "XmlWriterForStream", function (stream) {
        return JSIL.XML.WriterForStream(stream);
    });

    $.RawMethod(false, "GetContractClass", function () {
        var contractName = "Microsoft.Xml.Serialization.GeneratedAssembly.XmlSerializerContract";

        var contractAssembly = this.type.Assembly.FullName;
        if (this.type.IsArray) {
            contractAssembly = this.type.GetElementType().Assembly.FullName;
        }
        var indexOfFirstComaInAssemblyName = contractAssembly.search(",");
        if (indexOfFirstComaInAssemblyName >= 0) {
            contractAssembly = contractAssembly.substring(0, indexOfFirstComaInAssemblyName) + ".XmlSerializers" + contractAssembly.substring(indexOfFirstComaInAssemblyName, contractAssembly.length);
            contractName = contractName + ", " + contractAssembly;
        }

        var contractType = getType(contractName);

        if (!contractType)
            throw new Error("No XmlSerializer assembly loaded.");

        var contract = JSIL.CreateInstanceOfType(contractType);

        if (!contract)
            throw new Error("Could not create XmlSerializer contract class.");

        return contract;
    });

    $.RawMethod(false, "MakeSerializationReader", function (xmlReader, events, encodingStyle) {
        var contract = this.GetContractClass();
        var reader = contract.get_Reader();

        reader.Init(xmlReader, events, encodingStyle, null);

        return reader;
    });

    $.RawMethod(false, "MakeSerializationWriter", function (xmlWriter, namespaces, encodingStyle, id) {
        var contract = this.GetContractClass();
        var writer = contract.get_Writer();
        writer.Init(xmlWriter, namespaces, encodingStyle, id, null);

        return writer;
    });

    $.RawMethod(false, "MakeSerializer", function () {
        var contract = this.GetContractClass();
        var serializer = contract.GetSerializer(this.type);

        if (!serializer)
            throw new Error("XmlSerializer assembly does not contain a serializer for type '" + this.type.toString() + "'");

        return serializer;
    });

    $.RawMethod(false, "DeserializeInternal", function Deserialize(serializer, reader) {
        var signature = new JSIL.MethodSignature($.Object, [$xmlasms[16].System.Xml.Serialization.XmlSerializationReader], []);

        return signature.CallVirtual("Deserialize", null, serializer, reader);
    });

    $.RawMethod(false, "SerializeInternal", function Serialize(serializer, writer, value) {
        var signature = new JSIL.MethodSignature(null, [$.Object, $xmlasms[16].System.Xml.Serialization.XmlSerializationWriter], []);

        return signature.CallVirtual("Serialize", null, serializer, value, writer);
    });


    $.Method({ Static: false, Public: true }, "Deserialize",
      (new JSIL.MethodSignature($.Object, [$xmlasms[5].TypeRef("System.IO.Stream")], [])),
      function Deserialize(stream) {
          return this.Deserialize(
            this.XmlReaderFromStream(stream),
            null, null
          );
      }
    );

    $.Method({ Static: false, Public: true }, "Deserialize",
      (new JSIL.MethodSignature($.Object, [$xmlasms[5].TypeRef("System.IO.TextReader")], [])),
      function Deserialize(textReader) {
          throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "Deserialize",
      (new JSIL.MethodSignature($.Object, [$xmlasms[16].TypeRef("System.Xml.XmlReader")], [])),
      function Deserialize(xmlReader) {
          return this.Deserialize(xmlReader, null, null);
      }
    );

    $.Method({ Static: false, Public: true }, "Deserialize",
      (new JSIL.MethodSignature($.Object, [$xmlasms[16].TypeRef("System.Xml.XmlReader"), $xmlasms[16].TypeRef("System.Xml.Serialization.XmlDeserializationEvents")], [])),
      function Deserialize(xmlReader, events) {
          return this.Deserialize(xmlReader, null, events);
      }
    );

    $.Method({ Static: false, Public: true }, "Deserialize",
      (new JSIL.MethodSignature($.Object, [$xmlasms[16].TypeRef("System.Xml.XmlReader"), $.String], [])),
      function Deserialize(xmlReader, encodingStyle) {
          return this.Deserialize(xmlReader, encodingStyle, null);
      }
    );

    $.Method({ Static: false, Public: true }, "Deserialize",
      (new JSIL.MethodSignature($.Object, [
            $xmlasms[16].TypeRef("System.Xml.XmlReader"), $.String,
            $xmlasms[16].TypeRef("System.Xml.Serialization.XmlDeserializationEvents")
      ], [])),
      function Deserialize(xmlReader, encodingStyle, events) {
          var reader = this.MakeSerializationReader(xmlReader, events, encodingStyle);
          var serializer = this.MakeSerializer();

          return this.DeserializeInternal(serializer, reader);
      }
    );


    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [$xmlasms[5].TypeRef("System.IO.TextWriter"), $.Object], [])),
      function Serialize(textWriter, o) {
          throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [
            $xmlasms[5].TypeRef("System.IO.TextWriter"), $.Object,
            $xmlasms[16].TypeRef("System.Xml.Serialization.XmlSerializerNamespaces")
      ], [])),
      function Serialize(textWriter, o, namespaces) {
          throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [$xmlasms[5].TypeRef("System.IO.Stream"), $.Object], [])),
      function Serialize(stream, o) {
          this.Serialize(
            this.XmlWriterForStream(stream), o,
            null, null, null
          );
      }
    );

    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [
            $xmlasms[5].TypeRef("System.IO.Stream"), $.Object,
            $xmlasms[16].TypeRef("System.Xml.Serialization.XmlSerializerNamespaces")
      ], [])),
      function Serialize(stream, o, namespaces) {
          this.Serialize(
            this.XmlWriterForStream(stream), o,
            namespaces, null, null
          );
      }
    );

    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [$xmlasms[16].TypeRef("System.Xml.XmlWriter"), $.Object], [])),
      function Serialize(xmlWriter, o) {
          this.Serialize(
            xmlWriter, o,
            null, null, null
          );
      }
    );

    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [
            $xmlasms[16].TypeRef("System.Xml.XmlWriter"), $.Object,
            $xmlasms[16].TypeRef("System.Xml.Serialization.XmlSerializerNamespaces")
      ], [])),
      function Serialize(xmlWriter, o, namespaces) {
          this.Serialize(
            xmlWriter, o,
            namespaces, null, null
          );
      }
    );

    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [
            $xmlasms[16].TypeRef("System.Xml.XmlWriter"), $.Object,
            $xmlasms[16].TypeRef("System.Xml.Serialization.XmlSerializerNamespaces"), $.String
      ], [])),
      function Serialize(xmlWriter, o, namespaces, encodingStyle) {
          this.Serialize(
            xmlWriter, o,
            namespaces, encodingStyle, null
          );
      }
    );

    $.Method({ Static: false, Public: true }, "Serialize",
      (new JSIL.MethodSignature(null, [
            $xmlasms[16].TypeRef("System.Xml.XmlWriter"), $.Object,
            $xmlasms[16].TypeRef("System.Xml.Serialization.XmlSerializerNamespaces"), $.String,
            $.String
      ], [])),
      function Serialize(xmlWriter, o, namespaces, encodingStyle, id) {
          var writer = this.MakeSerializationWriter(xmlWriter, namespaces, encodingStyle, id);
          var serializer = this.MakeSerializer();

          return this.SerializeInternal(serializer, writer, o);
      }
    );

});
JSIL.ImplementExternals("System.Xml.Serialization.XmlSerializationReader", function ($) {

    $.Method({ Static: false, Public: false }, "Init",
      (new JSIL.MethodSignature(null, [
            $xmlasms[16].TypeRef("System.Xml.XmlReader"), $xmlasms[16].TypeRef("System.Xml.Serialization.XmlDeserializationEvents"),
            $.String, $xmlasms[16].TypeRef("System.Xml.Serialization.TempAssembly")
      ], [])),
      function Init(r, events, encodingStyle, tempAssembly) {
          this.r = r;

          this.typeID = r.NameTable.Add("type");
          this.nullID = r.NameTable.Add("null");
          this.nilID = r.NameTable.Add("nil");

          this.instanceNsID = r.NameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
          this.instanceNs2000ID = r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema-instance");
          this.instanceNs1999ID = r.NameTable.Add("http://www.w3.org/1999/XMLSchema-instance");

          this.InitIDs();
      }
    );

    $.Method({ Static: false, Public: false }, "CheckReaderCount",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", [$.Int32]), $jsilcore.TypeRef("JSIL.Reference", [$.Int32])], [])),
      function CheckReaderCount(/* ref */ whileIterations, /* ref */ readerCount) {
          if (true) {
              whileIterations.set(whileIterations.get() + 1);

              if ((whileIterations.get() & 128) == 128) {
                  if (readerCount.get() == this.ReaderCount) {
                      throw new InvalidOperationException("XmlReader is stuck");
                  }

                  readerCount.set(this.ReaderCount);
              }
          }

      }
    );

    $.Method({ Static: false, Public: true }, "get_Reader",
      (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlReader"), [], [])),
      function get_Reader() {
          return this.r;
      }
    );

    $.Method({ Static: false, Public: true }, "get_ReaderCount",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_ReaderCount() {
          // Can't use the property because of type system nonsense.
          return this.r.advanceCount;
      }
    );

    $.RawMethod(false, "$getNullAttribute", function () {
        var a = this.r.GetAttribute(this.nilID, this.instanceNsID);
        if (a !== null)
            return System.Xml.XmlConvert.ToBoolean(a);

        a = this.r.GetAttribute(this.nilID, this.instanceNs2000ID);
        if (a !== null)
            return System.Xml.XmlConvert.ToBoolean(a);

        a = this.r.GetAttribute(this.nilID, this.instanceNs1999ID);
        if (a !== null)
            return System.Xml.XmlConvert.ToBoolean(a);

        return false;
    });

    $.Method({ Static: false, Public: false }, "ReadNull",
      (new JSIL.MethodSignature($.Boolean, [], [])),
      function ReadNull() {
          if (!this.$getNullAttribute())
              return false;

          this.r.Skip();
          return true;
      }
    );

    $.Method({ Static: false, Public: true }, "ReadEndElement",
      (JSIL.MethodSignature.Void),
      function ReadEndElement() {
          while (this.r.NodeType == System.Xml.XmlNodeType.Whitespace)
              this.r.Skip();

          if (this.r.NodeType == System.Xml.XmlNodeType.None) {
              this.r.Skip();
              return;
          }

          this.r.ReadEndElement();
      }
    );

    $.Method({ Static: false, Public: false }, "GetXsiType",
      (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.XmlQualifiedName"), [], [])),
      function GetXsiType() {
          var result = new System.Xml.XmlQualifiedName();

          var r = this.r;
          var a = r.GetAttribute(this.typeID, this.instanceNsID);
          if (a !== null) {
              result.name = a;
              return result;
          }

          a = r.GetAttribute(this.typeID, this.instanceNs2000ID);
          if (a !== null) {
              result.name = a;
              return result;
          }

          a = r.GetAttribute(this.typeID, this.instanceNs1999ID);
          if (a !== null) {
              result.name = a;
              return result;
          }

          return null;
      }
    );

    $.Method({ Static: false, Public: false }, "ReadSerializable",
      (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.Serialization.IXmlSerializable"), [$xmlasms[16].TypeRef("System.Xml.Serialization.IXmlSerializable")], [])),
      function ReadSerializable(serializable) {
          return this.ReadSerializable(serializable, false);
      }
    );

    $.Method({ Static: false, Public: false }, "ReadSerializable",
      (new JSIL.MethodSignature($xmlasms[16].TypeRef("System.Xml.Serialization.IXmlSerializable"), [$xmlasms[16].TypeRef("System.Xml.Serialization.IXmlSerializable"), $.Boolean], [])),
      function ReadSerializable(serializable, wrappedAny) {
          var localName, namespace;

          if (wrappedAny) {
              localName = this.r.LocalName;
              namespace = this.r.NamespaceURI;
              this.r.Read();
              this.r.MoveToContent();
          }

          serializable.ReadXml(this.r);

          if (wrappedAny) {
              var ntNone = System.Xml.XmlNodeType.None;
              var ntWhitespace = System.Xml.XmlNodeType.Whitespace;
              var ntEndElement = System.Xml.XmlNodeType.EndElement;

              while (this.r.NodeType === ntWhitespace)
                  this.r.Skip();

              if (this.r.NodeType === ntNone)
                  this.r.Skip();

              if (
                this.r.NodeType === ntEndElement &&
                this.r.LocalName === localName &&
                this.r.NamespaceURI === namespace
              )
                  this.Reader.Read();
          }

          return serializable;
      }
    );

});
JSIL.ImplementExternals("System.Xml.XmlQualifiedName", function ($) {

    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
          this.name = "";
          this.ns = "";
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.String], [])),
      function _ctor(name) {
          this.name = name;
          this.ns = "";
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.String, $.String], [])),
      function _ctor(name, ns) {
          this.name = name;
          this.ns = ns;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Name",
      (new JSIL.MethodSignature($.String, [], [])),
      function get_Name() {
          return this.name;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Namespace",
      (new JSIL.MethodSignature($.String, [], [])),
      function get_Namespace() {
          return this.ns;
      }
    );

    var equalsImpl = function (lhs, rhs) {
        if (lhs === rhs)
            return true;

        if ((lhs === null) || (rhs === null))
            return lhs === rhs;

        return (lhs.name == rhs.name) && (lhs.ns == rhs.ns);
    }

    $.Method({ Static: true, Public: true }, "op_Equality",
      (new JSIL.MethodSignature($.Boolean, [$xmlasms[16].TypeRef("System.Xml.XmlQualifiedName"), $xmlasms[16].TypeRef("System.Xml.XmlQualifiedName")], [])),
      function op_Equality(a, b) {
          return equalsImpl(a, b);
      }
    );

    $.Method({ Static: true, Public: true }, "op_Inequality",
      (new JSIL.MethodSignature($.Boolean, [$xmlasms[16].TypeRef("System.Xml.XmlQualifiedName"), $xmlasms[16].TypeRef("System.Xml.XmlQualifiedName")], [])),
      function op_Inequality(a, b) {
          return !equalsImpl(a, b);
      }
    );

});
JSIL.ImplementExternals("System.Xml.XmlConvert", function ($) {

    $.Method({ Static: true, Public: true }, "ToDouble",
      (new JSIL.MethodSignature($.Double, [$.String], [])),
      function ToDouble(s) {
          return parseFloat(s);
      }
    );

    $.Method({ Static: true, Public: true }, "ToSingle",
      (new JSIL.MethodSignature($.Single, [$.String], [])),
      function ToSingle(s) {
          return parseFloat(s);
      }
    );

    $.Method({ Static: true, Public: true }, "ToInt16",
      (new JSIL.MethodSignature($.Int16, [$.String], [])),
      function ToInt16(s) {
          var i = parseInt(s, 10);
          if (isNaN(i))
              throw new Error("Invalid integer");

          return i;
      }
    );

    $.Method({ Static: true, Public: true }, "ToInt32",
      (new JSIL.MethodSignature($.Int32, [$.String], [])),
      function ToInt32(s) {
          var i = parseInt(s, 10);
          if (isNaN(i))
              throw new Error("Invalid integer");

          return i;
      }
    );

    $.Method({ Static: true, Public: true }, "ToInt64",
      (new JSIL.MethodSignature($.Int64, [$.String], [])),
      function ToInt64(s) {
          var i = parseInt(s, 10);
          if (isNaN(i))
              throw new Error("Invalid integer");

          return i;
      }
    );

    $.Method({ Static: true, Public: true }, "ToUInt16",
      (new JSIL.MethodSignature($.UInt16, [$.String], [])),
      function ToUInt16(s) {
          var i = parseInt(s, 10);
          if (isNaN(i) || i < 0)
              throw new Error("Invalid unsigned integer");

          return i;
      }
    );

    $.Method({ Static: true, Public: true }, "ToUInt32",
      (new JSIL.MethodSignature($.UInt32, [$.String], [])),
      function ToUInt32(s) {
          var i = parseInt(s, 10);
          if (isNaN(i) || i < 0)
              throw new Error("Invalid unsigned integer");

          return i;
      }
    );

    $.Method({ Static: true, Public: true }, "ToUInt64",
      (new JSIL.MethodSignature($.UInt64, [$.String], [])),
      function ToUInt64(s) {
          var i = parseInt(s, 10);
          if (isNaN(i) || i < 0)
              throw new Error("Invalid unsigned integer");

          return i;
      }
    );

    $.Method({ Static: true, Public: true }, "ToBoolean",
      (new JSIL.MethodSignature($.Boolean, [$.String], [])),
      function ToBoolean(s) {
          var text = s.toLowerCase().trim();
          if (text == "true")
              return true;
          else if (text == "false")
              return false;

          var i = parseInt(s, 10);
          if (isNaN(i))
              throw new Error("Invalid boolean");

          return (i != 0);
      }
    );

    $.Method({ Static: true, Public: true }, "ToByte",
      (new JSIL.MethodSignature($.Byte, [$.String], [])),
      function ToByte(s) {
          var i = parseInt(s, 10);
          if (isNaN(i) || i < 0 || i > 255)
              throw new Error("Invalid byte");

          return i;
      }
    );
});










