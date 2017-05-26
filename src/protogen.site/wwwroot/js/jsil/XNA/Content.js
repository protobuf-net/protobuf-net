/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

if (typeof ($jsilxna) === "undefined")
  throw new Error("JSIL.XNACore required");

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ContentLoadException", function ($) {
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.String], []), function (message) {
    System.Exception.prototype._ctor.call(this, message);
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ContentManager", function ($) {
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms.corlib.TypeRef("System.IServiceProvider")], []), function (serviceProvider) {
    this._serviceProvider = serviceProvider;
    this._rootDirectory = "";
  });

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms.corlib.TypeRef("System.IServiceProvider"), $.String], []), function (serviceProvider, rootDirectory) {
    this._serviceProvider = serviceProvider;
    this._rootDirectory = rootDirectory;
  });

  $.Method({
    Static: false,
    Public: true
  }, "Load", new JSIL.MethodSignature("!!0", [$.String], ["T"]),
  function ContentManager_Load (T, assetName) {
    var asset;

    try {
      asset = JSIL.Host.getAsset(assetName);
    } catch (exc) {
      if (exc.Message.indexOf("is not in the asset manifest") >= 0) {
        throw new Microsoft.Xna.Framework.Content.ContentLoadException(exc.Message);
      } else {
        throw exc;
      }
    }

    if (asset.ReadAsset) {
      asset.contentManager = this;

      var result = null;
      try {
        result = asset.ReadAsset(T);
      } catch (exc) {
        var signature = new JSIL.ConstructorSignature(
          Microsoft.Xna.Framework.Content.ContentLoadException.__Type__,
          ["System.String", "System.Exception"]
        );

        throw signature.Construct(
          "Failed to load asset '" + assetName + "' because an asset loader threw an exception.", exc
        );
      }

      if (result === null)
        JSIL.Host.warning("Asset '" + assetName + "' loader returned null.");

      return result;
    }

    if (HTML5Asset.$Is(asset)) {
      return asset;
    }

    if (asset === null)
      JSIL.Host.warning("Asset '" + assetName + "' loader returned null.");
    else
      throw new Microsoft.Xna.Framework.Content.ContentLoadException("Asset '" + assetName + "' is not an instance of HTML5Asset.");
  }),

  $.Method({
    Static: false,
    Public: true
  }, "Unload", JSIL.MethodSignature.Void, function () {
    // Unnecessary since we rely on the host to preload our assets.
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_ServiceProvider", new JSIL.MethodSignature($xnaasms.corlib.TypeRef("System.IServiceProvider"), [], []), function () {
    return this._serviceProvider;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_RootDirectory", new JSIL.MethodSignature(null, [$.String], []), function (rootDirectory) {
    this._rootDirectory = rootDirectory;
  });
  $.Method({
    Static: false,
    Public: true
  }, "get_RootDirectory", new JSIL.MethodSignature($.String, [], []), function () {
    return this._rootDirectory;
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ContentTypeReader", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[5].TypeRef("System.Type")], [])),
    function _ctor (targetType) {
      this.targetType = targetType;
      this.TargetIsValueType = !targetType.__IsReferenceType__;
    }
  );

  $.Method({Static:false, Public:true }, "get_CanDeserializeIntoExistingObject",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_CanDeserializeIntoExistingObject () {
      return false;
    }
  );

  $.Method({Static:false, Public:true }, "get_TargetType",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.Type"), [], [])),
    function get_TargetType () {
      return this.targetType;
    }
  );

  $.Method({Static:false, Public:true }, "get_TypeVersion",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_TypeVersion () {
      return 0;
    }
  );

  $.Method({Static:false, Public:false}, "Initialize",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReaderManager")], [])),
    function Initialize (manager) {

    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ContentTypeReader`1", function ($) {
  $.Method({Static:false, Public:false}, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      var assembly = $xnaasms.xna;

      assembly.Microsoft.Xna.Framework.Content.ContentTypeReader.prototype._ctor.call(
        this, assembly.Microsoft.Xna.Framework.Content.ContentTypeReader$b1.T.get(this)
      );
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.StringReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.String, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.String], [])),
    function Read (input, existingInstance) {
      return input.ReadString();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ByteReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Byte, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Byte], [])),
    function Read (input, existingInstance) {
      return input.ReadByte();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.CharReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Char, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Char], [])),
    function Read (input, existingInstance) {
      return input.ReadChar();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.Int16Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Int16, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Int16], [])),
    function Read (input, existingInstance) {
      return input.ReadInt16();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.Int32Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Int32, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Int32], [])),
    function Read (input, existingInstance) {
      return input.ReadInt32();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.Int64Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Int64, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Int64], [])),
    function Read (input, existingInstance) {
      return input.ReadInt64();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.UInt16Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.UInt16, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.UInt16], [])),
    function Read (input, existingInstance) {
      return input.ReadUInt16();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.UInt32Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.UInt32, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.UInt32], [])),
    function Read (input, existingInstance) {
      return input.ReadUInt32();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.UInt64Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.UInt64, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.UInt64], [])),
    function Read (input, existingInstance) {
      return input.ReadUInt64();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.SingleReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Single, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Single], [])),
    function Read (input, existingInstance) {
      return input.ReadSingle();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.DoubleReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Double, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Double], [])),
    function Read (input, existingInstance) {
      return input.ReadDouble();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.PointReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point")], [])),
    function Read (input, existingInstance) {
      var x = input.ReadInt32();
      var y = input.ReadInt32();

      var result = new Microsoft.Xna.Framework.Point(x, y);
      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.RectangleReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], [])),
    function Read (input, existingInstance) {
      var x = input.ReadInt32();
      var y = input.ReadInt32();
      var w = input.ReadInt32();
      var h = input.ReadInt32();

      var result = new Microsoft.Xna.Framework.Rectangle(x, y, w, h);
      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.Vector2Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")], [])),
    function Read (input, existingInstance) {
      return input.ReadVector2();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.Vector3Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function Read (input, existingInstance) {
      return input.ReadVector3();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.Vector4Reader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4")], [])),
    function Read (input, existingInstance) {
      return input.ReadVector4();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ArrayReader`1", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      var assembly = $xnaasms.xna;
      assembly.Microsoft.Xna.Framework.Content.ContentTypeReader$b1.prototype._ctor.call(
        this, System.Array.Of(
          assembly.Microsoft.Xna.Framework.Content.ArrayReader$b1.T.get(this)
        ).__Type__
      );
    }
  );

  $.Method({Static:false, Public:false}, "Initialize",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReaderManager")], [])),
    function Initialize (manager) {
      this.elementReader = manager.GetTypeReader(this.T);
    }
  );

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [new JSIL.GenericParameter("T", "Microsoft.Xna.Framework.Content.ArrayReader`1")]), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $jsilcore.TypeRef("System.Array", [new JSIL.GenericParameter("T", "Microsoft.Xna.Framework.Content.ArrayReader`1")])], [])),
    function Read (input, existingInstance) {
      var count = input.ReadInt32();
      if (existingInstance === null) {
        existingInstance = JSIL.Array.New(this.T, count);
      }

      for (var i = 0; i < count; i++) {
        existingInstance[i] = input.ReadObjectInternal$b1(this.T)(this.elementReader, null);
      }

      return existingInstance;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ListReader`1", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      var assembly = $xnaasms.xna;
      assembly.Microsoft.Xna.Framework.Content.ContentTypeReader$b1.prototype._ctor.call(
        this, System.Collections.Generic.List$b1.Of(
          assembly.Microsoft.Xna.Framework.Content.ListReader$b1.T.get(this)
        ).__Type__
      );
    }
  );

  $.Method({Static:false, Public:false}, "Initialize",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReaderManager")], [])),
    function Initialize (manager) {
      this.elementReader = manager.GetTypeReader(this.T);
    }
  );

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.Collections.Generic.List`1", [new JSIL.GenericParameter("T", "Microsoft.Xna.Framework.Content.ListReader`1")]), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $xnaasms[5].TypeRef("System.Collections.Generic.List`1", [new JSIL.GenericParameter("T", "Microsoft.Xna.Framework.Content.ListReader`1")])], [])),
    function Read (input, existingInstance) {
      var count = input.ReadInt32();
      if (existingInstance === null) {
        existingInstance = new(System.Collections.Generic.List$b1.Of(this.T))();
      }

      var readItemFunc = input.ReadObjectInternal$b1(this.T);

      while (count > 0) {
        var item = readItemFunc(this.elementReader, null);
        existingInstance.Add(item);
        count--;
      }

      return existingInstance;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.Texture2DReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D")], [])),
    function Read (input, existingInstance) {
      var asmGraphics = $xnaasms.xnaGraphics || $xnaasms.xna;
      var tTexture2D = JSIL.GetTypeFromAssembly(asmGraphics, "Microsoft.Xna.Framework.Graphics.Texture2D", [], true);
      var tSurfaceFormat = asmGraphics.Microsoft.Xna.Framework.Graphics.SurfaceFormat.__Type__;

      var surfaceFormat = tSurfaceFormat.$Cast(input.ReadInt32());
      var width = input.ReadInt32();
      var height = input.ReadInt32();
      var mipCount = input.ReadInt32();

      var result = existingInstance;
      if (result === null) result = JSIL.CreateInstanceOfType(tTexture2D, "$internalCtor", [null, width, height, mipCount > 1, surfaceFormat]);

      for (var i = 0; i < mipCount; i++) {
        var mipSize = input.ReadInt32();
        var mipBytes = input.ReadBytes(mipSize);

        if (i === 0)
          result.SetData$b1(System.Byte)(i, null, mipBytes, 0, mipSize);
      }

      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.SpriteFontReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont")], [])),
    function Read (input, existingInstance) {
      var asmXna = $xnaasms.xna;
      var asmGraphics = $xnaasms.xnaGraphics || $xnaasms.xna;

      var tList = System.Collections.Generic.List$b1;
      var tSpriteFont = asmGraphics.Microsoft.Xna.Framework.Graphics.SpriteFont;
      var tTexture2D = asmGraphics.Microsoft.Xna.Framework.Graphics.Texture2D;
      var tRectangle = asmXna.Microsoft.Xna.Framework.Rectangle;
      var tVector3 = asmXna.Microsoft.Xna.Framework.Vector3;

      var texture = input.ReadObject$b1(tTexture2D)();

      var glyphs = input.ReadObject$b1(tList.Of(tRectangle))();

      var cropping = input.ReadObject$b1(tList.Of(tRectangle))();

      var charMap = input.ReadObject$b1(tList.Of(System.Char))();

      var lineSpacing = input.ReadInt32();
      var spacing = input.ReadSingle();

      var kerning = input.ReadObject$b1(tList.Of(tVector3))();

      var defaultCharacter = null;
      if (input.ReadBoolean()) {
        defaultCharacter = input.ReadChar();
      }

      var result = new tSpriteFont(
        texture, glyphs, cropping, charMap, lineSpacing, spacing, kerning, defaultCharacter
      );

      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.EffectReader", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Effect"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Effect")], [])),
    function Read (input, existingInstance) {
      var count = input.ReadInt32();
      var effectCode = input.ReadBytes(count);

      // FIXME
      return JSIL.CreateInstanceOfType(Microsoft.Xna.Framework.Graphics.Effect.__Type__, null);
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ContentTypeReaderManager", function ($) {
  $.RawMethod(true, ".cctor2",
    function () {
      var assembly = $xnaasms.xna;
      var thisType = assembly.Microsoft.Xna.Framework.Content.ContentTypeReaderManager;

      thisType.nameToReader = {};
      thisType.targetTypeToReader = {};
      thisType.readerTypeToReader = {};
    }
  );

  $.Method({Static:true , Public:false}, "AddTypeReader",
    (new JSIL.MethodSignature(null, [
          $.String, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReader")
        ], [])),
    function AddTypeReader (readerTypeName, contentReader, reader) {
      var assembly = $xnaasms.xna;
      var thisType = assembly.Microsoft.Xna.Framework.Content.ContentTypeReaderManager;

      var targetType = reader.TargetType;
      thisType.targetTypeToReader[targetType.__TypeId__] = reader;
      thisType.readerTypeToReader[reader.GetType().__TypeId__] = reader;
      thisType.nameToReader[readerTypeName] = reader;
    }
  );

  $.Method({Static:true , Public:false}, "GetTypeReader",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReader"), [$xnaasms[5].TypeRef("System.Type"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader")], [])),
    function GetTypeReader (targetType, contentReader) {
      var assembly = $xnaasms.xna;
      var thisType = assembly.Microsoft.Xna.Framework.Content.ContentTypeReaderManager;

      var result = thisType.targetTypeToReader[targetType.__TypeId__];

      if (typeof (result) !== "object") {
        throw new Error("No content type reader known for type '" + targetType + "'.");
        return null;
      }

      return result;
    }
  );

  $.Method({Static:true , Public:false}, "ReadTypeManifest",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReader")]), [$.Int32, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader")], [])),
    function ReadTypeManifest (typeCount, contentReader) {
      var assembly = $xnaasms.xna;
      var thisType = assembly.Microsoft.Xna.Framework.Content.ContentTypeReaderManager;

      var result = new Array(typeCount);
      var readerManager = new thisType(contentReader);

      for (var i = 0; i < typeCount; i++) {
        var typeReaderName = contentReader.ReadString();
        var typeReaderVersionNumber = contentReader.ReadInt32();

        // We need to explicitly make the xna assembly the default search context since many of the readers are private classes
        var parsedTypeName = JSIL.ParseTypeName(typeReaderName);
        var typeReaderType = JSIL.GetTypeInternal(parsedTypeName, assembly, false);

        if (typeReaderType === null) {
          throw new Error("The type '" + typeReaderName + "' could not be found while loading asset '" + contentReader.assetName + "'.");
          return null;
        }

        var typeReaderInstance = JSIL.CreateInstanceOfType(typeReaderType);
        var targetType = typeReaderInstance.TargetType;
        if (!targetType) {
          throw new Error("The type reader '" + typeReaderName + "' is broken or not implemented.");
          return null;
        }

        var targetTypeName = targetType.toString();

        thisType.AddTypeReader(typeReaderName, contentReader, typeReaderInstance);

        readerManager.knownReaders[targetTypeName] = typeReaderInstance;

        result[i] = typeReaderInstance;
      }

      for (var i = 0; i < typeCount; i++) {
        result[i].Initialize(readerManager);
      }

      return result;
    }
  );

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader")], [])),
    function _ctor (contentReader) {
      this.contentReader = contentReader;
      this.knownReaders = {};
    }
  );

  $.Method({Static:false, Public:true }, "GetTypeReader",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReader"), [$xnaasms[5].TypeRef("System.Type")], [])),
    function GetTypeReader (targetType) {
      var typeName = targetType.toString();
      var reader = this.knownReaders[typeName];
      if (typeof (reader) === "object")
        return reader;

      var assembly = $xnaasms.xna;
      var thisType = assembly.Microsoft.Xna.Framework.Content.ContentTypeReaderManager;

      reader = thisType.GetTypeReader(targetType, this.contentReader);
      if (typeof (reader) === "object")
        return reader;

      throw new Error("No content type reader known for type '" + typeName + "'.");
      return null;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ContentReader", function ($) {
  $.Method({Static:false, Public:false}, ".ctor",
    new JSIL.MethodSignature(null, [
        $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentManager"), $xnaasms[5].TypeRef("System.IO.Stream"),
        $.String, $xnaasms[5].TypeRef("System.Action`1", [$xnaasms[5].TypeRef("System.IDisposable")]),
        $.Int32
      ], []),
    function _ctor (contentManager, input, assetName, recordDisposableObject, graphicsProfile) {
      var signature = new JSIL.MethodSignature(null, [$xnaasms[5].TypeRef("System.IO.Stream")], []);
      signature.Call(System.IO.BinaryReader.prototype, "_ctor", null, this, input);

      this.contentManager = contentManager;
      this.assetName = assetName;
      this.recordDisposableObject = recordDisposableObject;
      this.graphicsProfile = graphicsProfile;

      this.typeReaders = null;
    }
  );

  $.Method({Static:false, Public:true }, "get_AssetName",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_AssetName () {
      // XNA ContentReader.AssetName always has backslashes, so we need to preserve that
      //  because some content readers do stuff with AssetName
      return this.assetName.replace(/\//g, "\\");
    }
  );

  $.Method({Static:false, Public:true }, "get_ContentManager",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentManager"), [], [])),
    function get_ContentManager () {
      return this.contentManager;
    }
  );

  $.RawMethod(false, "makeError", function throwError (text) {
    return new Microsoft.Xna.Framework.Content.ContentLoadException(text);
  });

  $.Method({Static:false, Public:false}, "ReadHeader",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function ReadHeader () {
      var formatHeader = String.fromCharCode.apply(String, this.ReadBytes(3));
      if (formatHeader != "XNB")
        throw this.makeError("Invalid XNB format");

      var platformId = String.fromCharCode(this.ReadByte());
      switch (platformId) {
      case "w":
        break;
      default:
        throw this.makeError("Unsupported XNB platform: " + platformId);
      }

      var formatVersion = this.ReadByte();
      switch (formatVersion) {
      case 4:
      case 5:
        break;
      default:
        throw this.makeError("Unsupported XNB format version: " + formatVersion);
      }

      var formatFlags = this.ReadByte();

      var isHiDef = (formatFlags & 0x01) != 0;
      var isCompressed = (formatFlags & 0x80) != 0;

      if (isCompressed)
        throw this.makeError("Compressed XNBs are not supported");

      var uncompressedSize = this.ReadUInt32();

      var typeReaderCount = this.Read7BitEncodedInt();
      this.typeReaders = Microsoft.Xna.Framework.Content.ContentTypeReaderManager.ReadTypeManifest(typeReaderCount, this);

      if (!this.typeReaders)
        throw this.makeError("Failed to construct type readers");
    }
  );

  $.Method({Static:false, Public:true }, "ReadVector2",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [], [])),
    function ReadVector2 () {
      var x = this.ReadSingle();
      var y = this.ReadSingle();
      return new Microsoft.Xna.Framework.Vector2(x, y);
    }
  );

  $.Method({Static:false, Public:true }, "ReadVector3",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), [], [])),
    function ReadVector3 () {
      var x = this.ReadSingle();
      var y = this.ReadSingle();
      var z = this.ReadSingle();
      return new Microsoft.Xna.Framework.Vector3(x, y, z);
    }
  );

  $.Method({Static:false, Public:true }, "ReadVector4",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"), [], [])),
    function ReadVector4 () {
      var x = this.ReadSingle();
      var y = this.ReadSingle();
      var z = this.ReadSingle();
      var w = this.ReadSingle();
      return new Microsoft.Xna.Framework.Vector4(x, y, z, w);
    }
  );

  var readObjectImpl = function (self, T, typeReader, existingInstance) {
    if ((typeReader !== null) && (typeReader.TargetIsValueType)) {
      var signature = new JSIL.MethodSignature(T, [self.__ThisType__, T]);

      return signature.CallVirtual("Read", null, typeReader, self, existingInstance);
    }

    var typeId = self.Read7BitEncodedInt();

    if (typeId === 0)
      return null;

    typeReader = self.typeReaders[typeId - 1];
    if (typeof (typeReader) !== "object") {
      throw new Error("No type reader for typeId '" + typeId + "'. Misaligned XNB read is likely.");
      return null;
    }

    return typeReader.Read(self, existingInstance);
  };

  $.Method({Static:false, Public:true }, "ReadObject",
    (new JSIL.MethodSignature("!!0", [], ["T"])),
    function ReadObject$b1 (T) {
      return readObjectImpl(this, T, null, JSIL.DefaultValue(T));
    }
  );

  $.Method({Static:false, Public:true }, "ReadObject",
    (new JSIL.MethodSignature("!!0", ["!!0"], ["T"])),
    function ReadObject$b1 (T, existingInstance) {
      return readObjectImpl(this, T, null, existingInstance);
    }
  );

  $.Method({Static:false, Public:true }, "ReadObject",
    (new JSIL.MethodSignature("!!0", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReader"), "!!0"], ["T"])),
    function ReadObject$b1 (T, typeReader, existingInstance) {
      return readObjectImpl(this, T, typeReader, existingInstance);
    }
  );

  $.Method({Static:false, Public:false}, "ReadObjectInternal",
    (new JSIL.MethodSignature("!!0", [$.Object], ["T"])),
    function ReadObjectInternal$b1 (T, existingInstance) {
      return readObjectImpl(this, T, null, existingInstance);
    }
  );

  $.Method({Static:false, Public:false}, "ReadObjectInternal",
    (new JSIL.MethodSignature("!!0", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReader"), $.Object], ["T"])),
    function ReadObjectInternal$b1 (T, typeReader, existingInstance) {
      return readObjectImpl(this, T, typeReader, existingInstance);
    }
  );

  var readRawObjectImpl = function (self, T, existingInstance) {
    var assembly = $xnaasms.xna;
    var ctrm = assembly.Microsoft.Xna.Framework.Content.ContentTypeReaderManager;

    var typeReader = ctrm.GetTypeReader(T, self);
    return typeReader.Read(self, existingInstance);
  };

  $.Method({Static:false, Public:true }, "ReadRawObject",
    (new JSIL.MethodSignature("!!0", [], ["T"])),
    function ReadRawObject$b1 (T) {
      return readRawObjectImpl(this, T, JSIL.DefaultValue(T));
    }
  );

  $.Method({Static:false, Public:true }, "ReadRawObject",
    (new JSIL.MethodSignature("!!0", ["!!0"], ["T"])),
    function ReadRawObject$b1 (T, existingInstance) {
      return readRawObjectImpl(this, T, existingInstance);
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.DictionaryReader`2", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      var assembly = $xnaasms.xna;

      var tThis = assembly.Microsoft.Xna.Framework.Content.DictionaryReader$b2;
      var tKey = this.TKey = tThis.Key.get(this);
      var tValue = this.TValue = tThis.Value.get(this);
      var tDictionary = this.TDictionary = $jsilcore.System.Collections.Generic.Dictionary$b2.Of(tKey, tValue).__Type__;

      assembly.Microsoft.Xna.Framework.Content.ContentTypeReader.prototype._ctor.call(
        this, tDictionary
      );
    }
  );

  $.Method({Static:false, Public:false}, "Initialize",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReaderManager")], [])),
    function Initialize (manager) {
      this.keyReader = manager.GetTypeReader(this.TKey);
      this.valueReader = manager.GetTypeReader(this.TValue);
    }
  );

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.Collections.Generic.List`1", [new JSIL.GenericParameter("T", "Microsoft.Xna.Framework.Content.ListReader`1")]), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $xnaasms[5].TypeRef("System.Collections.Generic.List`1", [new JSIL.GenericParameter("T", "Microsoft.Xna.Framework.Content.ListReader`1")])], [])),
    function Read (input, existingInstance) {
      var count = input.ReadInt32();
      if (existingInstance === null)
        existingInstance = new (this.TDictionary.__PublicInterface__)();

      var readKeyFunc = input.ReadObjectInternal$b1(this.TKey);
      var readValueFunc = input.ReadObjectInternal$b1(this.TValue);

      while (count > 0) {
        var key = readKeyFunc(this.keyReader, null);
        var value = readValueFunc(this.valueReader, null);
        existingInstance.Add(key, value);
        count--;
      }

      return existingInstance;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Content.ReflectiveReader`1", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      var assembly = $xnaasms.xna;

      assembly.Microsoft.Xna.Framework.Content.ContentTypeReader.prototype._ctor.call(
        this, assembly.Microsoft.Xna.Framework.Content.ReflectiveReader$b1.T.get(this)
      );
    }
  );

  $.Method({Static:false, Public:true }, "get_CanDeserializeIntoExistingObject",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_CanDeserializeIntoExistingObject () {
      return !this.TargetIsValueType;
    }
  );

  $.Method({Static:false, Public:true }, "get_TypeVersion",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_TypeVersion () {
      return this.typeVersion;
    }
  );

  var makeMemberHelper = function (manager, type, setter) {
    var reader = manager.GetTypeReader(type);
    var tObject = $jsilcore.System.Object.__Type__;

    return function (contentReader, input, result) {
      var value = input.ReadObject$b1(tObject)(reader, null);
      if (!type.__PublicInterface__.$Is(value)) {
        var message = "Tried to read '" + type.get_Name() + "' but got a value of type '" + JSIL.GetType(value) + "'!";
        throw new Microsoft.Xna.Framework.Content.ContentLoadException(message);
      }

      setter(result, value);
    };
  };

  var makeFieldSetter = function (fieldInfo) {
    var fieldName = JSIL.EscapeName(fieldInfo.get_Name());

    return function (instance, value) {
      instance[fieldName] = value;
    };
  };

  var makePropertySetter = function (propertyInfo) {
    var propertySetter = propertyInfo.GetSetMethod(true);
    var setterName = propertySetter._descriptor.EscapedName;

    return function (instance, value) {
      (instance[setterName]) (value);
    };
  };

  var hasAttribute = function (member, attributeType) {
    var attributes = member.GetCustomAttributes(attributeType, false);

    return (attributes && attributes.length > 0);
  };

  $.RawMethod(false, "CreateMemberReaders", function (manager, members, result) {
    var tIgnoreAttribute = $xnaasms.xna.Microsoft.Xna.Framework.Content.ContentSerializerIgnoreAttribute.__Type__;
    var tSerializerAttribute = $xnaasms.xna.Microsoft.Xna.Framework.Content.ContentSerializerAttribute.__Type__;

    members_loop:
    for (var i = 0, l = members.length; i < l; i++) {
      var member = members[i], memberType, memberSetter;

      var fi = $jsilcore.System.Reflection.FieldInfo.$As(member);
      var pi = $jsilcore.System.Reflection.PropertyInfo.$As(member);

      if (fi !== null) {
        if (hasAttribute(fi, tIgnoreAttribute))
          continue;

        if (hasAttribute(fi, tSerializerAttribute)) {
          // FIXME: SharedResource
        } else {
          if (!fi.IsPublic)
            continue;
          if (fi.IsInitOnly)
            continue;
        }

        memberType = fi.get_FieldType();
        memberSetter = makeFieldSetter(fi);
      } else if (pi !== null) {
        if (hasAttribute(pi, tIgnoreAttribute))
          continue;

        if (pi.GetIndexParameters().Length > 0)
          continue;

        if (hasAttribute(pi, tSerializerAttribute)) {
          // FIXME: SharedResource

          var setMethod = pi.GetSetMethod(true);
          if (!setMethod)
            continue;
        } else {
          var setMethod = pi.GetSetMethod();
          if (!setMethod)
            continue;

          var accessors = pi.GetAccessors();
          for (var j = 0, la = accessors.length; j < la; j++) {
            var accessor = accessors[j];

            if (!accessor.IsPublic)
              continue members_loop;
          }
        }

        memberType = pi.get_PropertyType();
        memberSetter = makePropertySetter(pi);
      } else {
        throw new Error("Got a member that isn't a field or a property");
      }

      result.push(makeMemberHelper(manager, memberType, memberSetter));
    }
  });

  $.Method({Static:false, Public:false}, "Initialize",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentTypeReaderManager")], [])),
    function Initialize (manager) {
      var baseType = this.targetType.__BaseType__;
      if (
        baseType &&
        baseType.get_FullName &&
        (baseType.get_FullName() !== "System.Object") &&
        (baseType.get_FullName() !== "System.ValueType")
      ) {
        this.baseReader = manager.GetTypeReader(baseType);
      }

      var bindingFlags = $jsilcore.BindingFlags.$Flags("DeclaredOnly", "Instance", "Public", "NonPublic");
      // FIXME: The order that comes back from GetProperties/GetFields might not be the same as it is in the .NET CLR.
      //  Given this, how can we reliably support ReflectiveReader?
      var properties = this.targetType.GetProperties(bindingFlags);
      var fields = this.targetType.GetFields(bindingFlags);

      var result = this.memberHelpers = [];
      this.CreateMemberReaders(manager, properties, result);
      this.CreateMemberReaders(manager, fields, result);
    }
  );

  $.Method({Static:false, Public:false}, "Read",
    (new JSIL.MethodSignature($.Object, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), $.Object], [])),
    function Read (input, existingInstance) {
      if (!existingInstance)
        existingInstance = JSIL.CreateInstanceOfType(this.targetType);

      if (this.baseReader) {
        var readBaseInstance = this.baseReader.Read(input, existingInstance);
        if (readBaseInstance !== existingInstance)
          throw new Error("Base type reader did not return existing instance");
      }

      try {
        for (var i = 0, l = this.memberHelpers.length; i < l; i++) {
          var helper = this.memberHelpers[i];
          helper(this, input, existingInstance);
        }
      } catch (exc) {
        var message = "ReflectiveReader failed to load a '" + this.targetType.get_Name() + "': " + exc.get_Message();
        throw new Microsoft.Xna.Framework.Content.ContentLoadException(message, exc);
      }

      return existingInstance;
    }
  );

});