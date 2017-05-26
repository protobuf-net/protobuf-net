/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined") throw new Error("JSIL.Core required");

var $drawDebugRects = false, $drawDebugBoxes = false;
var $useTextCaching = true, $textCachingSupported = true;

var $jsilxna = JSIL.DeclareAssembly("JSIL.XNA");

JSIL.DeclareNamespace("JSIL");

$jsilxna.allowWebGL = true;
$jsilxna.testedWebGL = false;
$jsilxna.workingWebGL = false;

var $xnaasms = new JSIL.AssemblyCollection({
  corlib: "mscorlib",
  xna: "Microsoft.Xna.Framework",
  xnaGraphics: "Microsoft.Xna.Framework.Graphics",
  xnaGame: "Microsoft.Xna.Framework.Game",
  xnaStorage: "Microsoft.Xna.Framework.Storage",
  0: "Microsoft.Xna.Framework",
  1: "Microsoft.Xna.Framework.Game",
  2: "Microsoft.Xna.Framework.GamerServices",
  4: "Microsoft.Xna.Framework.Input.Touch, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553",
  5: "mscorlib",
  11: "System.Drawing",
  15: "System.Windows.Forms",
  18: "Microsoft.Xna.Framework.Xact",
});

var getXnaGraphics = function () {
  return $xnaasms.xnaGraphics || $xnaasms.xna;
};

var getXnaStorage = function () {
  return $xnaasms.xnaStorage || $xnaasms.xna;
};

$jsilxna.colorRef = function () {
  var graphicsAsm = JSIL.GetAssembly("Microsoft.Xna.Framework.Graphics", true);
  if (graphicsAsm !== null)
    return $xnaasms.xna.TypeRef("Microsoft.Xna.Framework.Color");
  else
    return $xnaasms.xna.TypeRef("Microsoft.Xna.Framework.Graphics.Color");
};

$jsilxna.graphicsRef = function (name) {
  var graphicsAsm = JSIL.GetAssembly("Microsoft.Xna.Framework.Graphics", true);
  if (graphicsAsm !== null)
    return graphicsAsm.TypeRef(name);
  else
    return $xnaasms.xna.TypeRef(name);
};

JSIL.MakeClass($jsilcore.System.Object, "HTML5Asset", true, [], function ($) {
  $.RawMethod(false, ".ctor", function (assetName) {
    this.name = assetName;
  });

  $.RawMethod(false, "toString", function () {
    return "<XNA Asset '" + this.name + "'>";
  });
});

JSIL.MakeClass("HTML5Asset", "HTML5ImageAsset", true, [], function ($) {
  $.RawMethod(false, ".ctor", function (assetName, image) {
    HTML5Asset.prototype._ctor.call(this, assetName);
    image.assetName = assetName;

    this.image = image;
  });

  $.RawMethod(false, "ReadAsset",
    function HTML5ImageAsset_ReadAsset (type) {
      var tTexture = Microsoft.Xna.Framework.Graphics.Texture2D.__Type__;
      return JSIL.CreateInstanceOfType(tTexture, "$fromImage", [null, this.image]);
    }
  );
});

JSIL.MakeClass("HTML5Asset", "SoundAssetBase", true, [], function ($) {

  $.Method({Static:false, Public:true }, "Play",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function Play () {
      return this.Play(1, 0, 0);
    }
  );

  $.Method({Static:false, Public:true }, "Play",
    (new JSIL.MethodSignature($.Boolean, [$.Single, $.Single, $.Single], [])),
    function Play (volume, pitch, pan) {
      var instance = this.$newInstance();

      instance.set_volume(volume);
      instance.set_pan(pan);
      instance.set_pitch(pitch);

      instance.play();

      return true;
    }
  );

  $.Method({Static:false, Public:true }, "CreateInstance",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Audio.SoundEffectInstance"), [], [])),
    function CreateInstance () {
      return new Microsoft.Xna.Framework.Audio.SoundEffectInstance(this, false);
    }
  );

});

JSIL.MakeClass("SoundAssetBase", "CallbackSoundAsset", true, [], function ($) {
  $.RawMethod(false, ".ctor", function (assetName, createInstance) {
    HTML5Asset.prototype._ctor.call(this, assetName);

    this.$createInstance = createInstance;
    this.freeInstances = [];
  });

  $.RawMethod(false, "$newInstance", function () {
    if (this.freeInstances.length > 0) {
      return this.freeInstances.pop();
    } else {
      return this.$createInstance(0);
    }
  });
});

JSIL.MakeClass("HTML5Asset", "RawXNBAsset", true, [], function ($) {
  var $NewCr = function () {
    return ($NewCr = JSIL.Memoize(
      new JSIL.ConstructorSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentReader"), [
        $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentManager"), $xnaasms[5].TypeRef("System.IO.Stream"),
        $.String, $xnaasms[5].TypeRef("System.Action`1", [$xnaasms[5].TypeRef("System.IDisposable")]),
        $.Int32
      ])
    )) ();
  };

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", JSIL.MethodSignature.Void, function (assetName, rawBytes) {
    if (JSIL.GetAssembly("JSIL.IO", true) === null) {
      throw new Error("JSIL.IO is required");
    }

    HTML5Asset.prototype._ctor.call(this, assetName);
    this.bytes = rawBytes;
    this.contentManager = null;
  });

  $.Method({
    Static: false,
    Public: true
  }, "ReadAsset", JSIL.MethodSignature.Void, function RawXNBAsset_ReadAsset (type) {
    if (!type)
      throw new Error("ReadAsset must be provided a type object");

    var memoryStream = new System.IO.MemoryStream(this.bytes, false);

    var contentReader = $NewCr().Construct(
      this.contentManager, memoryStream, this.name, null, 0
    );

    contentReader.ReadHeader();

    var sharedResourceCount = contentReader.Read7BitEncodedInt();
    var sharedResources = new Array(sharedResourceCount);

    var mainObject = contentReader.ReadObject$b1(type)();

    for (var i = 0; i < sharedResourceCount; i++)
      sharedResources[i] = content.ReadObject$b1(System.Object)();

    return mainObject;
  });
});

JSIL.MakeClass("RawXNBAsset", "SpriteFontAsset", true, [], function ($) {
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", JSIL.MethodSignature.Void, function (assetName, rawBytes) {
    RawXNBAsset.prototype._ctor.call(this, assetName, rawBytes);
  });
});

JSIL.MakeClass("RawXNBAsset", "Texture2DAsset", true, [], function ($) {
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", JSIL.MethodSignature.Void, function (assetName, rawBytes) {
    RawXNBAsset.prototype._ctor.call(this, assetName, rawBytes);
  });
});

var vectorUtil = {
  makeOperatorCore: function (name, tVector, body, argCount, leftScalar, rightScalar) {
    var js = body.join("\r\n");

    var typeName = String(tVector.typeName);
    var suffixedName;

    if (argCount < 1) {
      suffixedName = name;
    } else {
      suffixedName = name + "[";
      if (argCount == 1) {
        suffixedName += (leftScalar ? "float" : "vec");
      } else if (argCount == 2) {
        suffixedName += (leftScalar ? "float" : "vec") + "," +
          (rightScalar ? "float" : "vec");
      }
      suffixedName += "]";
    }

    var functionName = typeName + "." + suffixedName;

    switch (argCount) {
      case 0:
        return JSIL.CreateNamedFunction(
          functionName,
          [],
          js
        );
      case 1:
        return JSIL.CreateNamedFunction(
          functionName,
          ["value"],
          js
        );
      case 2:
        return JSIL.CreateNamedFunction(
          functionName,
          ["lhs", "rhs"],
          js
        );
      case 3:
        return JSIL.CreateNamedFunction(
          functionName,
          ["lhs", "rhs", "amount"],
          js
        );
      default:
        throw new Error("Invalid argument count");
    }
  },

  bindToPrototype: function (fn, typeRef, dataMembers) {
    var state = {
      resolvedType: null,
      typeRef: typeRef
    };

    JSIL.SetLazyValueProperty(
      state, "$instance",
      function VectorMethod_GetCreator () {
        if (state.resolvedType === null)
          state.resolvedType = state.typeRef.get();

        var creatorBody = "";
        for (var i = 0; i < dataMembers.length; i++)
          creatorBody += "this." + dataMembers[i] + " = " + dataMembers[i] + ";\r\n";

        var creator = JSIL.CreateNamedFunction(state.resolvedType.__Type__.__FullName__, dataMembers, creatorBody, null);
        creator.prototype = state.resolvedType.prototype;
        return creator;
      }
    );

    return fn.bind(state);
  },

  makeArithmeticOperator: function ($, name, staticMethodName, operator, dataMembers, tLeft, tRight, tResult) {
    var leftScalar = (tLeft !== tResult);
    var rightScalar = (tRight !== tResult);

    if (leftScalar && rightScalar)
      throw new Error("Invalid type combination");

    var body = [];
    body.push("return new this.$instance(");

    for (var i = 0; i < dataMembers.length; i++) {
      var dataMember = dataMembers[i];
      var line = "+(";

      if (leftScalar)
        line += "lhs ";
      else
        line += "lhs." + dataMember + " ";

      line += operator;

      if (rightScalar)
        line += " rhs";
      else
        line += " rhs." + dataMember + "";

      line += ")";

      if (i < (dataMembers.length - 1))
        line += ",";

      body.push(line);
    }

    body.push(");")

    var fn = vectorUtil.makeOperatorCore(name, tResult, body, 2, leftScalar, rightScalar);
    fn = vectorUtil.bindToPrototype(fn, tResult, dataMembers);

    $.Method({Static: true , Public: true }, name,
      new JSIL.MethodSignature(tResult, [tLeft, tRight], []),
      fn
    );

    $.Method({Static: true , Public: true }, staticMethodName,
      new JSIL.MethodSignature(tResult, [tLeft, tRight], []),
      fn
    );

    var makeRef = function (t) {
      return $jsilcore.TypeRef("JSIL.Reference", [t]);
    };

    var wrapper;

    if (leftScalar) {
      wrapper = function VectorOperator_Scalar_Ref (lhs, rhs, result) {
        result.set(fn(lhs, rhs.get()));
      }
    } else if (rightScalar) {
      wrapper = function VectorOperator_Ref_Scalar (lhs, rhs, result) {
        result.set(fn(lhs.get(), rhs));
      }
    } else {
      wrapper = function VectorOperator_Ref_Ref (lhs, rhs, result) {
        result.set(fn(lhs.get(), rhs.get()));
      }
    }

    $.Method({Static: true , Public: true }, staticMethodName,
      new JSIL.MethodSignature(null, [
        leftScalar ? tLeft : makeRef(tLeft),
        rightScalar ? tRight : makeRef(tRight),
        makeRef(tResult)
      ], []),
      wrapper
    );
  },

  makeLogicOperator: function ($, name, operator, bindingOperator, dataMembers, tVector) {
    var body = [];
    body.push("return (");

    for (var i = 0; i < dataMembers.length; i++) {
      var dataMember = dataMembers[i];
      var line = "  (lhs." + dataMember + " ";

      line += operator;

      line += " rhs." + dataMember + ") ";

      if (i < dataMembers.length - 1)
        line += bindingOperator;

      body.push(line);
    }

    body.push(");")

    var fn = vectorUtil.makeOperatorCore(name, tVector, body, 2, false, false);

    $.Method({Static: true , Public: true }, name,
      new JSIL.MethodSignature($.Boolean, [tVector, tVector], []),
      fn
    );
  },

  makeNegationOperator: function ($, dataMembers, tVector) {
    var body = [];
    body.push("return new this.$instance(");

    for (var i = 0; i < dataMembers.length; i++) {
      var dataMember = dataMembers[i];
      var line = "-value." + dataMember;

      if (i < (dataMembers.length - 1))
        line += ",";

      body.push(line);
    }

    body.push(");");

    var fn = vectorUtil.makeOperatorCore("op_UnaryNegation", tVector, body, 1, false, false);
    fn = vectorUtil.bindToPrototype(fn, tVector, dataMembers);

    $.Method({Static: true , Public: true }, "op_UnaryNegation",
      new JSIL.MethodSignature(tVector, [tVector], []),
      fn
    );
  },

  makeLengthGetter: function ($, name, isSquared, dataMembers, tVector) {
    var body = [];
    body.push("return " + (isSquared ? "(" : "Math.sqrt("));

    for (var i = 0; i < dataMembers.length; i++) {
      var dataMember = dataMembers[i];
      var line = "  (this." + dataMember + " * this." + dataMember + ")";
      if (i < dataMembers.length - 1)
        line += " + ";
      body.push(line);
    }

    body.push(");");

    var fn = vectorUtil.makeOperatorCore(name, tVector, body, 0);

    $.Method({Static: false, Public: true }, name,
      new JSIL.MethodSignature($.Single, [], []),
      fn
    );
  },

  makeDistanceFunction: function ($, name, isSquared, dataMembers, tVector) {
    var body = [];
    body.push("var result = +0.0, current;");

    for (var i = 0; i < dataMembers.length; i++) {
      var dataMember = dataMembers[i];

      body.push("current = +(rhs." + dataMember + " - lhs." + dataMember + ");");
      body.push("result += (current * current);");
    }

    if (isSquared)
      body.push("return result;");
    else
      body.push("return Math.sqrt(result);");

    var fn = vectorUtil.makeOperatorCore(name, tVector, body, 2);

    $.Method({Static: true, Public: true }, name,
      new JSIL.MethodSignature($.Single, [tVector, tVector], []),
      fn
    );
  },

  makeNormalizer: function ($, dataMembers, tVector) {
    var body = [];
    body.push("var factor = +1.0 / this.Length();");

    for (var i = 0; i < dataMembers.length; i++) {
      var dataMember = dataMembers[i];
      var line = "this." + dataMember + " *= factor;";
      body.push(line);
    }

    var fn = vectorUtil.makeOperatorCore("Normalize", tVector, body, 0);

    $.Method({Static: false, Public: true }, "Normalize",
      JSIL.MethodSignature.Void,
      fn
    );

    $.Method({Static: true , Public: true }, "Normalize",
      new JSIL.MethodSignature(tVector, [tVector], []),
      function Normalize_Static (v) {
        var result = v.MemberwiseClone();
        fn.call(result);
        return result;
      }
    );
  },

  makeLengthMethods: function ($, dataMembers, tVector) {
    vectorUtil.makeLengthGetter($, "LengthSquared", true, dataMembers, tVector);
    vectorUtil.makeLengthGetter($, "Length", false, dataMembers, tVector);

    vectorUtil.makeDistanceFunction($, "DistanceSquared", true, dataMembers, tVector);
    vectorUtil.makeDistanceFunction($, "Distance", false, dataMembers, tVector);

    vectorUtil.makeNormalizer($, dataMembers, tVector);
  },

  makeLerpMethod: function ($, dataMembers, tVector) {
    var name = "Lerp";
    var body = [];
    body.push("var result = lhs.MemberwiseClone();");

    for (var i = 0; i < dataMembers.length; i++) {
      var dataMember = dataMembers[i];

      body.push("result." + dataMember + " += (rhs." + dataMember + " - lhs." + dataMember + ") * amount;");
    }

    body.push("return result;");

    var fn = vectorUtil.makeOperatorCore(name, tVector, body, 3);

    $.Method({Static: true, Public: true }, name,
      new JSIL.MethodSignature(tVector, [tVector, tVector, $.Single], []),
      fn
    );
  },

  makeOperators: function ($, dataMembers, tVector) {
    var operators = [
      ["op_Addition", "+", false, "Add"],
      ["op_Subtraction", "-", false, "Subtract"],
      ["op_Division", "/", true, "Divide"],
      ["op_Multiply", "*", true, "Multiply"]
    ];

    for (var i = 0; i < operators.length; i++) {
      var name = operators[i][0];
      var operator = operators[i][1];
      var withScalar = operators[i][2];
      var staticMethodName = operators[i][3];

      vectorUtil.makeArithmeticOperator($, name, staticMethodName, operator, dataMembers, tVector, tVector, tVector);

      if (withScalar) {
        var nameSuffixed = name + "Scalar";
        var staticMethodNameSuffixed = staticMethodName + "Scalar";
        vectorUtil.makeArithmeticOperator($, nameSuffixed, staticMethodNameSuffixed, operator, dataMembers, tVector, $.Single, tVector);

        var nameSuffixed2 = nameSuffixed + "Left";
        var staticMethodNameSuffixed2 = staticMethodNameSuffixed + "Left";
        vectorUtil.makeArithmeticOperator($, nameSuffixed2, staticMethodNameSuffixed2, operator, dataMembers, $.Single, tVector, tVector);
      }
    }

    vectorUtil.makeNegationOperator($, dataMembers, tVector);

    vectorUtil.makeLogicOperator($, "op_Equality", "===", "&&", dataMembers, tVector);
    vectorUtil.makeLogicOperator($, "op_Inequality", "!==", "||", dataMembers, tVector);

    vectorUtil.makeLengthMethods($, dataMembers, tVector);

    vectorUtil.makeLerpMethod($, dataMembers, tVector);
  },

  makeConstants: function ($, tVector, constants) {
    var makeGetter = function (values) {
      var state = null;

      return function () {
        if (state === null)
          state = JSIL.CreateInstanceOfType(
            tVector.get().__Type__, "_ctor", values
          );

        return state;
      }
    };

    for (var k in constants) {
      var values = constants[k];
      var getter = makeGetter(values);

      $.Method({Static: true , Public: true}, "get_" + k,
        new JSIL.MethodSignature(tVector, [], []),
        getter
      );
    }
  }
};

JSIL.ImplementExternals("Microsoft.Xna.Framework.Vector2", function ($) {
  vectorUtil.makeConstants(
    $, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), {
      "UnitX": [1, 0],
      "UnitY": [0, 1],
      "Zero": [0, 0],
      "One": [1, 1]
    }
  );

  vectorUtil.makeOperators(
    $, ["X", "Y"], $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")
  );

  $.RawMethod(false, "toString", function () {
     return "{X:" + this.X + " Y:" + this.Y + "}";
  });

  $.Method({Static:true , Public:true }, "Clamp",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")
        ], [])),
    function Clamp (value, min, max) {
      var result = JSIL.CreateInstanceObject(Microsoft.Xna.Framework.Vector2.prototype);
      result.X = Microsoft.Xna.Framework.MathHelper.Clamp(value.X, min.X, max.X);
      result.Y = Microsoft.Xna.Framework.MathHelper.Clamp(value.Y, min.Y, max.Y);
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "Transform",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function Transform (position, matrix) {
      var result = JSIL.CreateInstanceObject(Microsoft.Xna.Framework.Vector2.prototype);
      result.X = (position.X * matrix.xScale) + matrix.xTranslation;
      result.Y = (position.Y * matrix.yScale) + matrix.yTranslation;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "Dot",
    (new JSIL.MethodSignature($.Single, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")], [])),
    function Dot (vector1, vector2) {
      return vector1.X * vector2.X + vector1.Y * vector2.Y;
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Single, $.Single], []), function Vector2_ctor (x, y) {
    this.X = +x;
    this.Y = +y;
  });
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Single], []), function Vector2_ctor (value) {
    this.X = this.Y = +value;
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Vector3", function ($) {
  vectorUtil.makeConstants(
    $, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), {
      "Backward": [0, 0, 1],
      "Forward": [0, 0, -1],
      "Left": [-1, 0, 0],
      "Right": [1, 0, 0],
      "Up": [0, 1, 0],
      "Down": [0, -1, 0],
      "UnitX": [1, 0, 0],
      "UnitY": [0, 1, 0],
      "UnitZ": [0, 0, 1],
      "Zero": [0, 0, 0],
      "One": [1, 1, 1]
    }
  );

  vectorUtil.makeOperators(
    $, ["X", "Y", "Z"], $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")
  );

  $.RawMethod(false, "toString", function () {
     return "{X:" + this.X + " Y:" + this.Y + " Z:" + this.Z + "}";
  });

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Single, $.Single, $.Single], []), function Vector3_ctor (x, y, z) {
    this.X = +x;
    this.Y = +y;
    this.Z = +z;
  });
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Single], []), function Vector3_ctor (value) {
    this.X = this.Y = this.Z = +value;
  });
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $.Single], []), function Vector3_ctor (xy, z) {
    this.X = +xy.X;
    this.Y = +xy.Y;
    this.Z = +z;
  });

  $.Method({Static:true , Public:true }, "Clamp",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")
        ], [])),
    function Clamp (value, min, max) {
      var result = JSIL.CreateInstanceObject(Microsoft.Xna.Framework.Vector3.prototype);
      result.X = Microsoft.Xna.Framework.MathHelper.Clamp(value.X, min.X, max.X);
      result.Y = Microsoft.Xna.Framework.MathHelper.Clamp(value.Y, min.Y, max.Y);
      result.Z = Microsoft.Xna.Framework.MathHelper.Clamp(value.Z, min.Z, max.Z);
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "Transform",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function Transform (position, matrix) {
      var result = JSIL.CreateInstanceObject(Microsoft.Xna.Framework.Vector3.prototype);
      result.X = (position.X * matrix.xScale) + matrix.xTranslation;
      result.Y = (position.Y * matrix.yScale) + matrix.yTranslation;
      result.Z = (position.Z * matrix.zScale) + matrix.zTranslation;
      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Vector4", function ($) {
  vectorUtil.makeConstants(
    $, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"), {
      "Zero": [0, 0, 0, 0],
      "One": [1, 1, 1, 1]
    }
  );

  vectorUtil.makeOperators(
    $, ["X", "Y", "Z", "W"], $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4")
  );

  $.RawMethod(false, "toString", function () {
     return "{X:" + this.X + " Y:" + this.Y + " Z:" + this.Z + " W: " + this.W + "}";
  });

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Single, $.Single, $.Single, $.Single], []), function Vector4_ctor (x, y, z, w) {
    this.X = +x;
    this.Y = +y;
    this.Z = +z;
    this.W = +w;
  });
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $.Single, $.Single], []), function Vector4_ctor (xy, z, w) {
    this.X = +xy.X;
    this.Y = +xy.Y;
    this.Z = +z;
    this.W = +w;
  });
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), $.Single], []), function Vector4_ctor (xyz, w) {
    this.X = +xyz.X;
    this.Y = +xyz.Y;
    this.Z = +xyz.Z;
    this.W = +w;
  });
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Single], []), function Vector4_ctor (value) {
    this.X = this.Y = this.Z = this.W = +value;
  });

  $.Method({Static:true , Public:true }, "Clamp",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"), [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4")
        ], [])),
    function Clamp (value, min, max) {
      var result = JSIL.CreateInstanceObject(Microsoft.Xna.Framework.Vector4.prototype);
      result.X = Microsoft.Xna.Framework.MathHelper.Clamp(value.X, min.X, max.X);
      result.Y = Microsoft.Xna.Framework.MathHelper.Clamp(value.Y, min.Y, max.Y);
      result.Z = Microsoft.Xna.Framework.MathHelper.Clamp(value.Z, min.Z, max.Z);
      result.W = Microsoft.Xna.Framework.MathHelper.Clamp(value.W, min.W, max.W);
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "Transform",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function Transform (position, matrix) {
      var result = JSIL.CreateInstanceObject(Microsoft.Xna.Framework.Vector4.prototype);
      result.X = (position.X * matrix.xScale) + matrix.xTranslation;
      result.Y = (position.Y * matrix.yScale) + matrix.yTranslation;
      result.Z = (position.Z * matrix.zScale) + matrix.zTranslation;
      result.W = (position.W * matrix.wScale) + matrix.wTranslation;
      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Matrix", function ($) {
  var matrix = $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix");

  $.Method({
    Static: true,
    Public: true
  }, ".cctor2", JSIL.MethodSignature.Void, function () {
    // FIXME
    var identity = Microsoft.Xna.Framework.Matrix._identity = new Microsoft.Xna.Framework.Matrix();

    identity.xTranslation = identity.yTranslation = identity.zTranslation = 0;
    identity.xRotation = identity.yRotation = identity.zRotation = 0;
    identity.xScale = identity.yScale = identity.zScale = 1;
  });

  $.RawMethod(false, "__CopyMembers__",
    function Matrix_CopyMembers (source, target) {
      target.xScale = source.xScale || 0;
      target.yScale = source.yScale || 0;
      target.zScale = source.zScale || 0;
      target.xTranslation = source.xTranslation || 0;
      target.yTranslation = source.yTranslation || 0;
      target.zTranslation = source.zTranslation || 0;
      target.xRotation = source.xRotation || 0;
      target.yRotation = source.yRotation || 0;
      target.zRotation = source.zRotation || 0;
    }
  );

  $.Method({Static:true , Public:true }, "get_Identity",
    (new JSIL.MethodSignature(matrix, [], [])),
    function get_Identity () {
      return Microsoft.Xna.Framework.Matrix._identity;
    }
  );

  $.Method({Static:true , Public:true }, "CreateLookAt",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")
        ], [])),
    function CreateLookAt (cameraPosition, cameraTarget, cameraUpVector) {
      // FIXME
      return Microsoft.Xna.Framework.Matrix._identity;
    }
  );

  $.Method({Static:true , Public:true }, "CreateLookAt",
    (new JSIL.MethodSignature(null, [
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")]), $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")]),
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")]), $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")])
        ], [])),
    function CreateLookAt (/* ref */ cameraPosition, /* ref */ cameraTarget, /* ref */ cameraUpVector, /* ref */ result) {
      // FIXME
      result.set(Microsoft.Xna.Framework.Matrix._identity);
    }
  );

  $.Method({Static:true , Public:true }, "CreateOrthographic",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $.Single, $.Single,
          $.Single, $.Single
        ], [])),
    function CreateOrthographic (width, height, zNearPlane, zFarPlane) {
      // FIXME
      return Microsoft.Xna.Framework.Matrix._identity;
    }
  );

  $.Method({Static:true , Public:true }, "CreateOrthographic",
    (new JSIL.MethodSignature(null, [
          $.Single, $.Single,
          $.Single, $.Single,
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")])
        ], [])),
    function CreateOrthographic (width, height, zNearPlane, zFarPlane, /* ref */ result) {
      // FIXME
      result.set(Microsoft.Xna.Framework.Matrix._identity);
    }
  );

  $.Method({Static:true , Public:true }, "CreateOrthographicOffCenter",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $.Single, $.Single,
          $.Single, $.Single,
          $.Single, $.Single
        ], [])),
    function CreateOrthographicOffCenter (left, right, bottom, top, zNearPlane, zFarPlane) {
      // FIXME
      return Microsoft.Xna.Framework.Matrix._identity;
    }
  );

  $.Method({Static:true , Public:true }, "CreateOrthographicOffCenter",
    (new JSIL.MethodSignature(null, [
          $.Single, $.Single,
          $.Single, $.Single,
          $.Single, $.Single,
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")])
        ], [])),
    function CreateOrthographicOffCenter (left, right, bottom, top, zNearPlane, zFarPlane, /* ref */ result) {
      // FIXME
      result.set(Microsoft.Xna.Framework.Matrix._identity);
    }
  );

  $.Method({Static:true , Public:true }, "CreatePerspective",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $.Single, $.Single,
          $.Single, $.Single
        ], [])),
    function CreatePerspective (width, height, nearPlaneDistance, farPlaneDistance) {
      // FIXME
      return Microsoft.Xna.Framework.Matrix._identity;
    }
  );

  $.Method({Static:true , Public:true }, "CreatePerspective",
    (new JSIL.MethodSignature(null, [
          $.Single, $.Single,
          $.Single, $.Single,
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")])
        ], [])),
    function CreatePerspective (width, height, nearPlaneDistance, farPlaneDistance, /* ref */ result) {
      // FIXME
      result.set(Microsoft.Xna.Framework.Matrix._identity);
    }
  );

  $.Method({Static:true , Public:true }, "CreatePerspectiveFieldOfView",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $.Single, $.Single,
          $.Single, $.Single
        ], [])),
    function CreatePerspectiveFieldOfView (fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance) {
      // FIXME
      return Microsoft.Xna.Framework.Matrix._identity;
    }
  );

  $.Method({Static:true , Public:true }, "CreatePerspectiveFieldOfView",
    (new JSIL.MethodSignature(null, [
          $.Single, $.Single,
          $.Single, $.Single,
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")])
        ], [])),
    function CreatePerspectiveFieldOfView (fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance, /* ref */ result) {
      // FIXME
      result.set(Microsoft.Xna.Framework.Matrix._identity);
    }
  );

  $.Method({Static:true , Public:true }, "CreatePerspectiveOffCenter",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $.Single, $.Single,
          $.Single, $.Single,
          $.Single, $.Single
        ], [])),
    function CreatePerspectiveOffCenter (left, right, bottom, top, nearPlaneDistance, farPlaneDistance) {
      // FIXME
      return Microsoft.Xna.Framework.Matrix._identity;
    }
  );

  $.Method({Static:true , Public:true }, "CreatePerspectiveOffCenter",
    (new JSIL.MethodSignature(null, [
          $.Single, $.Single,
          $.Single, $.Single,
          $.Single, $.Single,
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")])
        ], [])),
    function CreatePerspectiveOffCenter (left, right, bottom, top, nearPlaneDistance, farPlaneDistance, /* ref */ result) {
      // FIXME
      result.set(Microsoft.Xna.Framework.Matrix._identity);
    }
  );

  $.Method({Static:true , Public:true }, "CreateRotationX",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$.Single], [])),
    function CreateRotationX (radians) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xRotation = radians;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "CreateRotationY",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$.Single], [])),
    function CreateRotationY (radians) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.yRotation = radians;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "CreateRotationZ",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$.Single], [])),
    function CreateRotationZ (radians) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.zRotation = radians;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "CreateScale",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function CreateScale (scales) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xScale = scales.X;
      result.yScale = scales.Y;
      result.zScale = scales.Z;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "CreateScale",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$.Single], [])),
    function CreateScale (scale) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xScale = scale;
      result.yScale = scale;
      result.zScale = scale;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "CreateScale",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $.Single, $.Single,
          $.Single
        ], [])),
    function CreateScale (xScale, yScale, zScale) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xScale = xScale;
      result.yScale = yScale;
      result.zScale = zScale;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "CreateTranslation",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function CreateTranslation (position) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xTranslation = position.X;
      result.yTranslation = position.Y;
      result.zTranslation = position.Z;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "CreateTranslation",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [
          $.Single, $.Single,
          $.Single
        ], [])),
    function CreateTranslation (xPosition, yPosition, zPosition) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xTranslation = xPosition;
      result.yTranslation = yPosition;
      result.zTranslation = zPosition;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "Invert",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function Invert (matrix) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xTranslation = -matrix.xTranslation;
      result.yTranslation = -matrix.yTranslation;
      result.zTranslation = -matrix.zTranslation;

      result.xScale = 1 / (matrix.xScale + 0.000001);
      result.yScale = 1 / (matrix.yScale + 0.000001);
      result.zScale = 1 / (matrix.zScale + 0.000001);

      return result;
    }
  );

  $.Method({Static:true , Public:true }, "op_Multiply",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function Multiply (matrix1, matrix2) {
      // FIXME
      var result = Microsoft.Xna.Framework.Matrix._identity.MemberwiseClone();

      result.xTranslation = matrix1.xTranslation + matrix2.xTranslation;
      result.yTranslation = matrix1.yTranslation + matrix2.yTranslation;
      result.zTranslation = matrix1.zTranslation + matrix2.zTranslation;

      result.xScale = matrix1.xScale * matrix2.xScale;
      result.yScale = matrix1.yScale * matrix2.yScale;
      result.zScale = matrix1.zScale * matrix2.zScale;

      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GameComponentCollection", function ($) {
  $.RawMethod(false, "$internalCtor", function (game) {
    this._game = game;

    this._ctor();
  });

  $.RawMethod(false, "$OnItemAdded", function (item) {
    if (this._game.initialized) {
      if (typeof (item.Initialize) === "function")
        item.Initialize();
    }
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Game", function ($) {
  $.Method({Static: true, Public: true}, "ForceQuit",
    JSIL.MethodSignature.Void,
    function () {
      Microsoft.Xna.Framework.Game._QuitForced = true;
    }
  );

  $.Method({Static: true, Public: true}, "ForcePause",
    JSIL.MethodSignature.Void,
    function () {
      Microsoft.Xna.Framework.Game._PauseForced = true;
    }
  );

  $.Method({Static: true, Public: true}, "ForceUnpause",
    JSIL.MethodSignature.Void,
    function () {
      Microsoft.Xna.Framework.Game._PauseForced = false;

      var ns = Microsoft.Xna.Framework.Game._NeedsStep;
      Microsoft.Xna.Framework.Game._NeedsStep = null;

      if (ns)
        ns._QueueStep();
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      var tContentManager = JSIL.GetTypeFromAssembly(
        $xnaasms.xna, "Microsoft.Xna.Framework.Content.ContentManager", [], true
      );

      var tGameTime = JSIL.GetTypeFromAssembly(
        $xnaasms.xnaGame, "Microsoft.Xna.Framework.GameTime", [], true
      );

      this.gameServices = new Microsoft.Xna.Framework.GameServiceContainer();
      this.content = JSIL.CreateInstanceOfType(tContentManager, [this.gameServices]);
      this.components = JSIL.CreateInstanceOfType(
        Microsoft.Xna.Framework.GameComponentCollection.__Type__, "$internalCtor", [this]
      );
      this.targetElapsedTime = System.TimeSpan.FromTicks(System.Int64.FromInt32(166667));
      this.isFixedTimeStep = true;
      this.forceElapsedTimeToZero = true;
      this._isDead = false;
      this.initialized = false;

      this._gameTime = JSIL.CreateInstanceOfType(tGameTime, null);
      this._lastFrame = this._nextFrame = this._started = 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_Components",
    (new JSIL.MethodSignature($xnaasms[1].TypeRef("Microsoft.Xna.Framework.GameComponentCollection"), [], [])),
    function get_Components () {
      return this.components;
    }
  );

  $.Method({Static:false, Public:true }, "get_Content",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Content.ContentManager"), [], [])),
    function get_Content () {
      return this.content;
    }
  );

  $.Method({Static:false, Public:true }, "get_GraphicsDevice",
    (new JSIL.MethodSignature($jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), [], [])),
    function get_GraphicsDevice () {
      return this.graphicsDeviceService.GraphicsDevice;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsActive",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsActive () {
      return JSIL.Host.isPageVisible() && !Microsoft.Xna.Framework.Game._QuitForced && !this._isDead;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsFixedTimeStep",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsFixedTimeStep () {
      return this.isFixedTimeStep;
    }
  );

  $.Method({Static:false, Public:true }, "get_TargetElapsedTime",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.TimeSpan"), [], [])),
    function get_TargetElapsedTime () {
      return this.targetElapsedTime;
    }
  );

  $.Method({Static:false, Public:true }, "get_Services",
    (new JSIL.MethodSignature($xnaasms[1].TypeRef("Microsoft.Xna.Framework.GameServiceContainer"), [], [])),
    function get_Services () {
      return this.gameServices;
    }
  );

  $.Method({Static:false, Public:true }, "get_Window",
    (new JSIL.MethodSignature($xnaasms[1].TypeRef("Microsoft.Xna.Framework.GameWindow"), [], [])),
    function get_Window () {
      // FIXME
      if (!this._window)
        this._window = new Microsoft.Xna.Framework.GameWindow();

      return this._window;
    }
  );

  $.Method({Static:false, Public:false}, "get_IsMouseVisible",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsMouseVisible () {
      var oc = this.graphicsDeviceService.GraphicsDevice.originalCanvas;
      return (oc.style.cursor !== "none");
    }
  );

  $.Method({Static:false, Public:true }, "set_IsMouseVisible",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_IsMouseVisible (value) {
      var oc = this.graphicsDeviceService.GraphicsDevice.originalCanvas;
      oc.style.cursor = value ? "default" : "none";
    }
  );

  $.Method({Static:false, Public:true }, "set_IsFixedTimeStep",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_IsFixedTimeStep (value) {
      this.isFixedTimeStep = value;
    }
  );

  $.Method({Static:false, Public:true }, "set_TargetElapsedTime",
    (new JSIL.MethodSignature(null, [$xnaasms[5].TypeRef("System.TimeSpan")], [])),
    function set_TargetElapsedTime (value) {
      this.targetElapsedTime = value;
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, "Initialize", JSIL.MethodSignature.Void, function Game_Initialize () {
    this.initialized = true;

    for (var i = 0, l = this.components._size; i < l; i++) {
      var component = this.components._items[i];
      component.Initialize();
    }

    this.LoadContent();

    var gameControl = JSIL.Host.getService("gameControl", true);
    if (gameControl)
      gameControl.started(this);
  });
  $.Method({
    Static: false,
    Public: true
  }, "LoadContent", JSIL.MethodSignature.Void, function () {

  });
  $.Method({
    Static: false,
    Public: true
  }, "UnloadContent", JSIL.MethodSignature.Void, function () {

  });
  $.Method({
    Static: false,
    Public: true
  }, "ResetElapsedTime", JSIL.MethodSignature.Void, function () {
    this.forceElapsedTimeToZero = true;
  });

  $.RawMethod(false, "$ComponentsOfType", function Game_$ComponentsOfType (type) {
    var result = new Array();
    for (var i = 0, l = this.components._size; i < l; i++) {
      var item = this.components._items[i];

      if (type.$Is(item))
        result.push(item);
    }
    return result;
  });

  $.Method({Static:false, Public:false}, "Draw",
    (new JSIL.MethodSignature(null, [$xnaasms[1].TypeRef("Microsoft.Xna.Framework.GameTime")], [])),
    function Game_Draw (gameTime) {
      if (Microsoft.Xna.Framework.Game._QuitForced || this._isDead)
        return;

      var drawableComponents = this.$ComponentsOfType(Microsoft.Xna.Framework.IDrawable.__Type__);
      drawableComponents.sort(function (lhs, rhs) {
        return JSIL.CompareValues(lhs.get_DrawOrder(), rhs.get_DrawOrder());
      });

      for (var i = 0, l = drawableComponents.length; i < l; i++) {
        var drawable = drawableComponents[i];

        if (drawable.Visible)
          drawable.Draw(gameTime);
      }
    }
  );

  $.Method({Static:false, Public:false}, "Update",
    (new JSIL.MethodSignature(null, [$xnaasms[1].TypeRef("Microsoft.Xna.Framework.GameTime")], [])),
    function Game_Update (gameTime) {
      if (Microsoft.Xna.Framework.Game._QuitForced || this._isDead)
        return;

      var updateableComponents = this.$ComponentsOfType(Microsoft.Xna.Framework.IUpdateable.__Type__);
      updateableComponents.sort(function (lhs, rhs) {
        return JSIL.CompareValues(lhs.get_UpdateOrder(), rhs.get_UpdateOrder());
      });

      for (var i = 0, l = updateableComponents.length; i < l; i++) {
        var updateable = updateableComponents[i];

        if (updateable.Enabled)
          updateable.Update(gameTime);
      }
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, "Run", JSIL.MethodSignature.Void, function Game_Run () {
    this._profilingMode = (document.location.search.indexOf("profile") >= 0);
    this._balanceFPSCheckbox = (document.getElementById("balanceFramerate") || null);
    if (this._balanceFPSCheckbox)
      this._balanceFPSCheckbox.checked = !this._profilingMode;

    Microsoft.Xna.Framework.Game._QuitForced = false;
    this.Initialize();
    this._QueueStep();
  });

  $.RawMethod(false, "_QueueStep", function Game_QueueStep () {
    if (Microsoft.Xna.Framework.Game._QuitForced || this._isDead)
      return;

    var self = this;
    var stepCallback = self._Step.bind(self);

    JSIL.Host.scheduleTick(stepCallback);
  });

  $.RawMethod(false, "_TimedUpdate", function Game_TimedUpdate (longFrame) {
    // FIXME: We have to sample time in two different ways here.
    // One is used for the framerate indicator, the other is used
    //  to avoid frameskip after a really long update. We want the
    //  frameskip suppression to be consistent in replay recording situations
    //  but we want to display actual update perf when running a replay.

    var updateActuallyStarted = JSIL.$GetHighResTime();
    var updateStarted = JSIL.Host.getTickCount();

    this.Update(this._gameTime);

    var updateEnded = JSIL.Host.getTickCount();
    var updateActuallyEnded = JSIL.$GetHighResTime();

    // Detect long updates and suppress frameskip.
    if ((updateEnded - updateStarted) > longFrame) {
      this.suppressFrameskip = true;
    }

    this._updateTimings.push(updateActuallyEnded - updateActuallyStarted);

    var svc = JSIL.Host.getService("gameTiming", true);
    if (svc)
      svc.update(updateActuallyEnded - updateActuallyStarted);
  });

  $.RawMethod(false, "_MaybeReportFPS", function Game_MaybeReportFPS (now) {
    var elapsed = now - this._lastFPSReport;
    if (elapsed >= 250) {
      this._ReportFPS(now);
    }
  });

  $.RawMethod(false, "_ReportFPS", function Game_ReportFPS (now) {
    var maxTimingSamples = 60;

    this._lastFPSReport = now;

    // FIXME: Slower than necessary.

    while (this._drawTimings.length > maxTimingSamples)
      this._drawTimings.shift();

    while (this._updateTimings.length > maxTimingSamples)
      this._updateTimings.shift();

    var updateTimeSum = 0, drawTimeSum = 0;

    for (var i = 0, l = this._drawTimings.length; i < l; i++)
      drawTimeSum += this._drawTimings[i];

    for (var i = 0, l = this._updateTimings.length; i < l; i++)
      updateTimeSum += this._updateTimings[i];

    var updateTimeAverage = updateTimeSum / this._updateTimings.length;
    var drawTimeAverage = drawTimeSum / this._drawTimings.length;

    var isWebGL = this.graphicsDeviceService.GraphicsDevice.context.isWebGL || false;
    var cacheBytes = ($jsilxna.imageChannelCache.countBytes + $jsilxna.textCache.countBytes);

    JSIL.Host.reportPerformance(drawTimeAverage, updateTimeAverage, cacheBytes, isWebGL);
  });

  $.RawMethod(false, "_FixedTimeStep", function Game_FixedTimeStep (
    elapsed, frameDelay, millisecondInTicks, maxElapsedTimeMs, longFrame
  ) {
    var tInt64 = $jsilcore.System.Int64;
    var frameLength64 = tInt64.FromNumber(frameDelay * millisecondInTicks);
    this._gameTime.elapsedGameTime._ticks = frameLength64;
    this._gameTime.elapsedGameTime.$invalidate();

    elapsed += this._extraTime;
    this._extraTime = 0;

    if (elapsed > maxElapsedTimeMs)
      elapsed = maxElapsedTimeMs;

    var numFrames = Math.floor(elapsed / frameDelay);
    if (numFrames < 1) {
      numFrames = 1;
      this._extraTime = elapsed - frameDelay;
    } else {
      this._extraTime = elapsed - (numFrames * frameDelay);
    }

    for (var i = 0; i < numFrames; i++) {
      this._gameTime.totalGameTime._ticks = tInt64.op_Addition(
        this._gameTime.totalGameTime._ticks, frameLength64, this._gameTime.totalGameTime._ticks
      );
      this._gameTime.totalGameTime.$invalidate();

      this._TimedUpdate(longFrame);
    }
  });

  $.RawMethod(false, "_VariableTimeStep", function Game_VariableTimeStep (
    elapsed, frameDelay, millisecondInTicks, maxElapsedTimeMs, longFrame
  ) {
    this._extraTime = 0;
    this.suppressFrameskip = false;

    if (elapsed > maxElapsedTimeMs)
      elapsed = maxElapsedTimeMs;

    var tInt64 = $jsilcore.System.Int64;
    var elapsed64 = tInt64.FromNumber(elapsed * millisecondInTicks);

    this._gameTime.elapsedGameTime._ticks = elapsed64;
    this._gameTime.elapsedGameTime.$invalidate();
    this._gameTime.totalGameTime._ticks = tInt64.op_Addition(
      this._gameTime.totalGameTime._ticks, elapsed64, this._gameTime.totalGameTime._ticks
    );
    this._gameTime.totalGameTime.$invalidate();

    this._TimedUpdate(longFrame);
  });

  $.RawMethod(false, "_RenderAFrame", function Game_RenderAFrame () {
    var started = JSIL.$GetHighResTime();

    var device = this.get_GraphicsDevice();

    device.$UpdateViewport();
    device.$Clear(null);

    this.Draw(this._gameTime);

    var ended = JSIL.$GetHighResTime();

    this._drawTimings.push(ended - started);

    var svc = JSIL.Host.getService("gameTiming", true);
    if (svc)
      svc.draw(ended - started);
  });

  $.RawMethod(false, "_Step", function Game_Step () {
    var now = JSIL.Host.getTickCount();
    var actualNow = JSIL.$GetHighResTime();

    var frameDelay = this.targetElapsedTime.get_TotalMilliseconds();
    if (frameDelay <= 0)
      JSIL.RuntimeError("Game frame duration must be a positive, nonzero number!");

    if (this._lastFrame === 0) {
      var elapsed = frameDelay;
      var total = 0;
      this._started = now;
      this._lastFPSReport = actualNow;
      this._updateTimings = [];
      this._drawTimings = [];
      this._extraTime = 0;
      this.suppressFrameskip = true;
    } else {
      var elapsed = now - this._lastFrame;
      var total = now - this._started;
    }

    this._MaybeReportFPS(actualNow);

    $jsilxna.imageChannelCache.maybeEvictItems();
    $jsilxna.textCache.maybeEvictItems();

    if (this.forceElapsedTimeToZero) {
      this.forceElapsedTimeToZero = false;
      this._extraTime = 0;
      elapsed = 0;
    }

    this._lastFrame = now;
    this._nextFrame = now + frameDelay;

    var millisecondInTicks = 10000;
    var maxElapsedTimeMs = frameDelay * 4;
    var longFrame = frameDelay * 3;

    this._profilingMode = (document.location.search.indexOf("profile") >= 0);
    if (this._balanceFPSCheckbox)
      this._profilingMode = !this._balanceFPSCheckbox.checked;

    // Isolate the try block into its own function since it will be running in the interpreter
    this._TimeStep(elapsed, frameDelay, millisecondInTicks, maxElapsedTimeMs, longFrame);
  });

  $.RawMethod(false, "_TimeStep", function Game_TimeStep (elapsed, frameDelay, millisecondInTicks, maxElapsedTimeMs, longFrame) {
    var failed = true;

    try {
      if (this.isFixedTimeStep && !this.suppressFrameskip && !this._profilingMode) {
        this._FixedTimeStep(elapsed, frameDelay, millisecondInTicks, maxElapsedTimeMs, longFrame);
      } else {
        this._VariableTimeStep(elapsed, frameDelay, millisecondInTicks, maxElapsedTimeMs, longFrame);
      }

      this._RenderAFrame();

      failed = false;
    } finally {
      if (failed || Microsoft.Xna.Framework.Game._QuitForced) {
        this.Exit();
      } else if (Microsoft.Xna.Framework.Game._PauseForced) {
        Microsoft.Xna.Framework.Game._NeedsStep = this;
      } else {
        this._QueueStep();
      }
    }
  });

  $.Method({
    Static: false,
    Public: true
  }, "Exit", JSIL.MethodSignature.Void, function Game_Exit () {
    this.Dispose();
  });

  $.Method({
    Static: false,
    Public: true
  }, "Dispose", JSIL.MethodSignature.Void, function Game_Dispose () {
    this.UnloadContent();

    this._isDead = true;

    try {
      var canvas = JSIL.Host.getCanvas();
      var ctx = canvas.getContext("2d") || canvas.getContext("webgl-2d");
      ctx.setTransform(1, 0, 0, 1, 0, 0);
      ctx.globalAlpha = 1;
      ctx.globalCompositeOperation = "source-over";
      ctx.fillStyle = "black";
      ctx.fillRect(0, 0, 99999, 99999);

      var fsb = document.getElementById("fullscreenButton");
      if (fsb)
        fsb.style.display = "none";

      var stats = document.getElementById("stats");
      if (stats)
        stats.style.display = "none";
    } catch (exc) {
    }
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GameComponent", function ($) {

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[1].TypeRef("Microsoft.Xna.Framework.Game")], [])),
    function _ctor (game) {
      this.enabled = true;
      this.initialized = false;
      this.game = game;
    }
  );

  $.Method({Static:false, Public:true }, "get_UpdateOrder",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_UpdateOrder () {
      return 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_Enabled",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_Enabled () {
      return this.enabled;
    }
  );

  $.Method({Static:false, Public:true }, "get_Game",
    (new JSIL.MethodSignature($xnaasms[1].TypeRef("Microsoft.Xna.Framework.Game"), [], [])),
    function get_Game () {
      return this.game;
    }
  );

  $.Method({Static:false, Public:true }, "set_Enabled",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_Enabled (value) {
      this.enabled = value;
    }
  );

  $.Method({Static:false, Public:true }, "Initialize",
    (JSIL.MethodSignature.Void),
    function Initialize () {
      if (this.initialized) return;

      this.initialized = true;
    }
  );

  $.Method({Static:false, Public:true }, "Update",
    (new JSIL.MethodSignature(null, [$xnaasms[1].TypeRef("Microsoft.Xna.Framework.GameTime")], [])),
    function Update (gameTime) {
    }
  );

  $.Method({Static: false, Public: true }, "Dispose",
    (JSIL.MethodSignature.Void),
    function Dispose () {
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.DrawableGameComponent", function ($) {

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[1].TypeRef("Microsoft.Xna.Framework.Game")], [])),
    function _ctor (game) {
      Microsoft.Xna.Framework.GameComponent.prototype._ctor.call(this, game);

      this.visible = true;
    }
  );

  $.Method({Static:false, Public:true }, "Draw",
    (new JSIL.MethodSignature(null, [$xnaasms[1].TypeRef("Microsoft.Xna.Framework.GameTime")], [])),
    function Draw (gameTime) {
    }
  );

  $.Method({Static:false, Public:true }, "get_DrawOrder",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_DrawOrder () {
      return 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_GraphicsDevice",
    (new JSIL.MethodSignature($jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), [], [])),
    function get_GraphicsDevice () {
      return this.game.graphicsDeviceService.GraphicsDevice;
    }
  );

  $.Method({Static:false, Public:true }, "get_Visible",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_Visible () {
      return this.visible;
    }
  );

  $.Method({Static:false, Public:true }, "Initialize",
    (JSIL.MethodSignature.Void),
    function Initialize () {
      if (this.initialized) return;

      Microsoft.Xna.Framework.GameComponent.prototype.Initialize.call(this);

      this.LoadContent();
    }
  );

  $.Method({Static:false, Public:false}, "LoadContent",
    (JSIL.MethodSignature.Void),
    function LoadContent () {
    }
  );

  $.Method({Static:false, Public:true }, "set_Visible",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_Visible (value) {
      this.visible = value;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GameTime", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      this.totalGameTime = new System.TimeSpan();
      this.elapsedGameTime = new System.TimeSpan();
      this.isRunningSlowly = false;
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $xnaasms[5].TypeRef("System.TimeSpan"), $xnaasms[5].TypeRef("System.TimeSpan"),
          $.Boolean
        ], [])),
    function _ctor (totalGameTime, elapsedGameTime, isRunningSlowly) {
      this.totalGameTime = totalGameTime;
      this.elapsedGameTime = elapsedGameTime;
      this.isRunningSlowly = isRunningSlowly;
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[5].TypeRef("System.TimeSpan"), $xnaasms[5].TypeRef("System.TimeSpan")], [])),
    function _ctor (totalGameTime, elapsedGameTime) {
      this.totalGameTime = totalGameTime;
      this.elapsedGameTime = elapsedGameTime;
      this.isRunningSlowly = false;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsRunningSlowly",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsRunningSlowly () {
      return this.isRunningSlowly;
    }
  );

  $.Method({Static:false, Public:true }, "get_TotalGameTime",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.TimeSpan"), [], [])),
    function get_TotalGameTime () {
      return this.totalGameTime;
    }
  );

  $.Method({Static:false, Public:true }, "get_ElapsedGameTime",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.TimeSpan"), [], [])),
    function get_ElapsedGameTime () {
      return this.elapsedGameTime;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Rectangle", function ($) {
  $.RawMethod(true, ".cctor2", function () {
    Microsoft.Xna.Framework.Rectangle._empty = new Microsoft.Xna.Framework.Rectangle();
  });

  $.Method({Static: true, Public: true}, "get_Empty",
    new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [], []),
    function () {
      return Microsoft.Xna.Framework.Rectangle._empty;
    }
  );

  $.Method({Static:true , Public:true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], [])),
    function op_Equality (lhs, rhs) {
      return lhs.X === rhs.X && lhs.Y === rhs.Y && lhs.Width === rhs.Width && lhs.Height === rhs.Height;
    }
  );

  $.Method({Static:true , Public:true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], [])),
    function op_Inequality (lhs, rhs) {
      return lhs.X !== rhs.X || lhs.Y !== rhs.Y || lhs.Width !== rhs.Width || lhs.Height !== rhs.Height;
    }
  );

  var intersectImpl = function (lhs, rhs) {
    var lhsX2 = (lhs.X + lhs.Width) | 0;
    var rhsX2 = (rhs.X + rhs.Width) | 0;
    var lhsY2 = (lhs.Y + lhs.Height) | 0;
    var rhsY2 = (rhs.Y + rhs.Height) | 0;

    var x1 = (lhs.X > rhs.X) ? lhs.X : rhs.X;
    var y1 = (lhs.Y > rhs.Y) ? lhs.Y : rhs.Y;
    var x2 = (lhsX2 < rhsX2) ? lhsX2 : rhsX2;
    var y2 = (lhsY2 < rhsY2) ? lhsY2 : rhsY2;

    if (x2 > x1 && y2 > y1)
      return new Microsoft.Xna.Framework.Rectangle(
        x1 | 0, y1 | 0, (x2 - x1) | 0, (y2 - y1) | 0
      );

    return Microsoft.Xna.Framework.Rectangle._empty;
  };

  $.Method({Static:true , Public:true }, "Intersect",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], [])),
    intersectImpl
  );

  $.Method({Static:true , Public:true }, "Intersect",
    (new JSIL.MethodSignature(null, [
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]),
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")])
        ], [])),
    function Intersect (/* ref */ value1, /* ref */ value2, /* ref */ result) {
      result.set(intersectImpl(value1.get(), value2.get()));
    }
  );

  var unionImpl = function (lhs, rhs) {
    var lhsX2 = (lhs.X + lhs.Width) | 0;
    var rhsX2 = (rhs.X + rhs.Width) | 0;
    var lhsY2 = (lhs.Y + lhs.Height) | 0;
    var rhsY2 = (rhs.Y + rhs.Height) | 0;

    var x1 = (lhs.X < rhs.X) ? lhs.X : rhs.X;
    var y1 = (lhs.Y < rhs.Y) ? lhs.Y : rhs.Y;
    var x2 = (lhsX2 > rhsX2) ? lhsX2 : rhsX2;
    var y2 = (lhsY2 > rhsY2) ? lhsY2 : rhsY2;

    if (x2 > x1 && y2 > y1)
      return new Microsoft.Xna.Framework.Rectangle(
        x1 | 0, y1 | 0, (x2 - x1) | 0, (y2 - y1) | 0
      );

    return Microsoft.Xna.Framework.Rectangle._empty;
  };

  $.Method({Static:true , Public:true }, "Union",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], [])),
    unionImpl
  );

  $.Method({Static:true , Public:true }, "Union",
    (new JSIL.MethodSignature(null, [
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]),
          $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")])
        ], [])),
    function Union (/* ref */ value1, /* ref */ value2, /* ref */ result) {
      result.set(unionImpl(value1.get(), value2.get()));
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.Int32, $.Int32,
          $.Int32, $.Int32
        ], [])),
    function _ctor (x, y, width, height) {
      this.X = x | 0;
      this.Y = y | 0;
      this.Width = width | 0;
      this.Height = height | 0;
    }
  );

  $.RawMethod(false, "toString", function () {
     return "{X:" + this.X + " Y:" + this.Y + " Width:" + this.Width + " Height:" + this.Height + "}";
  });

  $.Method({Static:false, Public:true }, "Contains",
    (new JSIL.MethodSignature($.Boolean, [$.Int32, $.Int32], [])),
    function Contains (x, y) {
      return this.X <= x &&
        x < ((this.X + this.Width) | 0) &&
        this.Y <= y &&
        y < ((this.Y + this.Height) | 0);
    }
  );

  $.Method({Static:false, Public:true }, "ContainsPoint",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point")], [])),
    function ContainsPoint (value) {
      return this.X <= value.X &&
        value.X < ((this.X + this.Width) | 0) &&
        this.Y <= value.Y &&
        value.Y < ((this.Y + this.Height) | 0);
    }
  );

  $.Method({Static:false, Public:true }, "ContainsRectangle",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], [])),
    function ContainsRectangle (value) {
      return this.X <= value.X &&
        ((value.X + value.Width) | 0) <= ((this.X + this.Width) | 0) &&
        this.Y <= value.Y &&
        ((value.Y + value.Height) | 0) <= ((this.Y + this.Height) | 0);
    }
  );

  $.Method({Static:false, Public:true }, "get_Bottom",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Bottom () {
      return (this.Y + this.Height) | 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_Center",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point"), [], [])),
    function get_Center () {
      return new Microsoft.Xna.Framework.Point(
        (this.X + (this.Width / 2)) | 0,
        (this.Y + (this.Height / 2)) | 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "get_Left",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Left () {
      return this.X;
    }
  );

  $.Method({Static:false, Public:true }, "get_Location",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point"), [], [])),
    function get_Location () {
      return new Microsoft.Xna.Framework.Point(this.X, this.Y);
    }
  );

  $.Method({Static:false, Public:true }, "get_Right",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Right () {
      return (this.X + this.Width) | 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_Top",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Top () {
      return this.Y;
    }
  );

  $.Method({Static:false, Public:true }, "Inflate",
    (new JSIL.MethodSignature(null, [$.Int32, $.Int32], [])),
    function Inflate (x, y) {
      this.X = (this.X - x) | 0;
      this.Y = (this.Y - y) | 0;
      this.Width = (this.Width + (x * 2)) | 0;
      this.Height = (this.Height + (y * 2)) | 0;
    }
  );

  $.Method({Static:false, Public:true }, "Intersects",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], [])),
    function Intersects (value) {
      return value.X < ((this.X + this.Width) | 0) &&
              this.X < ((value.X + value.Width) | 0) &&
              value.Y < ((this.Y + this.Height) | 0) &&
              this.Y < ((value.Y + value.Height) | 0);
    }
  );

  $.Method({Static:false, Public:true }, "OffsetPoint",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point")], [])),
    function OffsetPoint (amount) {
      this.X = (this.X + amount.X) | 0;
      this.Y = (this.Y + amount.Y) | 0;
    }
  );

  $.Method({Static:false, Public:true }, "Offset",
    (new JSIL.MethodSignature(null, [$.Int32, $.Int32], [])),
    function Offset (offsetX, offsetY) {
      this.X = (this.X + offsetX) | 0;
      this.Y = (this.Y + offsetY) | 0;
    }
  );

  $.Method({Static:false, Public:true }, "set_Location",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point")], [])),
    function set_Location (value) {
      this.X = value.X | 0;
      this.Y = value.Y | 0;

      return value;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Point", function ($) {
  $.RawMethod(true, ".cctor2", function () {
    Microsoft.Xna.Framework.Point._zero = new Microsoft.Xna.Framework.Point();
  });

  $.Method({Static:true , Public:true }, "get_Zero",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point"), [], [])),
    function get_Zero () {
      return Microsoft.Xna.Framework.Point._zero;
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Int32, $.Int32], [])),
    function _ctor (x, y) {
      this.X = x | 0;
      this.Y = y | 0;
    }
  );

  var equalsImpl = function (lhs, rhs) {
    return lhs.X === rhs.X && lhs.Y === rhs.Y;
  };

  $.Method({Static:true , Public:true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point")], [])),
    equalsImpl
  );

  $.Method({Static:true , Public:true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point")], [])),
    function op_Inequality (a, b) {
      return !equalsImpl(a, b);
    }
  );

  $.Method({Static:false, Public:true }, "Object.Equals",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Point")], [])),
    function Equals (other) {
      return equalsImpl(this, other);
    }
  );
});

$jsilxna.makeColor = function (proto, r, g, b, a) {
  var result = JSIL.CreateInstanceObject(proto);
  result.r = r;
  result.g = g;
  result.b = b;

  if (typeof (a) === "number")
    result.a = a;
  else
    result.a = 255;

  return result;
};

$jsilxna.makeColorInstance = ( function () {
  var tColor = null;
  var ctor = null;

  return function makeColorInstance () {
    if (tColor === null) {
      var typeName1 = JSIL.ParseTypeName("Microsoft.Xna.Framework.Color,Microsoft.Xna.Framework");
      var typeName2 = JSIL.ParseTypeName("Microsoft.Xna.Framework.Graphics.Color,Microsoft.Xna.Framework");

      tColor = JSIL.GetTypeInternal(typeName1, $jsilxna, false) || JSIL.GetTypeInternal(typeName2, $jsilxna, false);
      var prototype = tColor.__PublicInterface__.prototype;

      ctor = function Color () {
      };
      ctor.prototype = prototype;
    }

    return new ctor();
  }
})();

$jsilxna.ColorFromPremultipliedInts = function (result, r, g, b, a) {
  if (!result)
    result = $jsilxna.makeColorInstance();

  result.r = $jsilxna.ClampByte(r);
  result.g = $jsilxna.ClampByte(g);
  result.b = $jsilxna.ClampByte(b);

  if (arguments.length === 5)
    result.a = $jsilxna.ClampByte(a);
  else
    result.a = 255;

  return result;
};

$jsilxna.ColorFromPremultipliedFloats = function (result, r, g, b, a) {
  if (!result)
    result = $jsilxna.makeColorInstance();

  result.r = $jsilxna.ClampByte(r * 255);
  result.g = $jsilxna.ClampByte(g * 255);
  result.b = $jsilxna.ClampByte(b * 255);

  if (arguments.length === 5)
    result.a = $jsilxna.ClampByte(a * 255);
  else
    result.a = 255;

  return result;
};

$jsilxna.Color = function ($) {
  (function BindColorExternals () {
    var makeColor = $jsilxna.makeColor;
    var colors = $jsilxna.colors || [];

    var makeLazyColor = function (r, g, b, a) {
      var state = null;
      return function () {
        if (state === null) {
          state = $jsilxna.makeColorInstance();
          state.a = a;
          state.r = r;
          state.g = g;
          state.b = b;
        }

        return state;
      };
    };

    for (var i = 0, l = colors.length; i < l; i++) {
      var colorName = colors[i][0];

      $.RawMethod(
        true, "get_" + colorName, makeLazyColor(colors[i][1], colors[i][2], colors[i][3], colors[i][4])
      );
    }
  }) ();

  $.RawMethod(false, "__CopyMembers__", function Color_CopyMembers (source, target) {
    target.a = source.a;
    target.r = source.r;
    target.g = source.g;
    target.b = source.b;
  });

  $.RawMethod(true, ".cctor2", function () {
    var self = this;
    var proto = this.prototype;
    var makeColor = $jsilxna.makeColor;
    var colors = $jsilxna.colors || [];

    var bindColor = function (c) {
      return function () {
        return c;
      };
    };

    var typeName1 = JSIL.ParseTypeName("Microsoft.Xna.Framework.Color,Microsoft.Xna.Framework");
    var typeName2 = JSIL.ParseTypeName("Microsoft.Xna.Framework.Graphics.Color,Microsoft.Xna.Framework");

    var context = JSIL.GetTypeInternal(typeName1, $jsilxna, false) || JSIL.GetTypeInternal(typeName2, $jsilxna, false);

    var publicInterface = context.__PublicInterface__;

    for (var i = 0, l = colors.length; i < l; i++) {
      var colorName = colors[i][0];
      var color = makeColor(proto, colors[i][1], colors[i][2], colors[i][3], colors[i][4]);
      var bound = bindColor(color);

      Object.defineProperty(publicInterface, "get_" + colorName, {
        value: bound,
        enumerable: true,
        configurable: true,
        writable: false
      });

      Object.defineProperty(publicInterface, colorName, {
        value: color,
        enumerable: true,
        configurable: true,
        writable: false
      });
    }
  });

  var ctorRgba = function (_, r, g, b, a) {
    _.a = a;
    _.r = r;
    _.g = g;
    _.b = b;
  };

  var ctorRgbaFloat = function (_, r, g, b, a) {
    _.a = $jsilxna.ClampByte(a * 255);
    _.r = $jsilxna.ClampByte(r * 255);
    _.g = $jsilxna.ClampByte(g * 255);
    _.b = $jsilxna.ClampByte(b * 255);
  };

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.Int32, $.Int32,
          $.Int32
        ], [])),
    function _ctor (r, g, b) {
      ctorRgba(this, r, g, b, 255);
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.Int32, $.Int32,
          $.Int32, $.Int32
        ], [])),
    function _ctor (r, g, b, a) {
      ctorRgba(this, r, g, b, a);
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.Single, $.Single,
          $.Single
        ], [])),
    function _ctor (r, g, b) {
      ctorRgbaFloat(this, r, g, b, 1);
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.Single, $.Single,
          $.Single, $.Single
        ], [])),
    function _ctor (r, g, b, a) {
      ctorRgbaFloat(this, r, g, b, a);
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms.xna.TypeRef("Microsoft.Xna.Framework.Vector3")], []), function (v) {
    ctorRgbaFloat(this, v.X, v.Y, v.Z, 1.0);
  });

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms.xna.TypeRef("Microsoft.Xna.Framework.Vector4")], []), function (v) {
    ctorRgbaFloat(this, v.X, v.Y, v.Z, v.W);
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_A", new JSIL.MethodSignature($.Byte, [], []), function () {
    return this.a;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_B", new JSIL.MethodSignature($.Byte, [], []), function () {
    return this.b;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_G", new JSIL.MethodSignature($.Byte, [], []), function () {
    return this.g;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_R", new JSIL.MethodSignature($.Byte, [], []), function () {
    return this.r;
  });

  $.Method({Static:false, Public:true }, "set_PackedValue",
    (new JSIL.MethodSignature(null, [$.UInt32], [])),
    function set_PackedValue (value) {
      this._cachedCss = null;
      this.a = (value >> 24) & 0xFF;
      this.b = (value >> 16) & 0xFF;
      this.g = (value >> 8) & 0xFF;
      this.r = value & 0xFF;
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, "set_A", new JSIL.MethodSignature(null, [$.Byte], []), function (value) {
    this.a = $jsilxna.ClampByte(value);
    this._cachedCss = null;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_B", new JSIL.MethodSignature(null, [$.Byte], []), function (value) {
    this.b = $jsilxna.ClampByte(value);
    this._cachedCss = null;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_G", new JSIL.MethodSignature(null, [$.Byte], []), function (value) {
    this.g = $jsilxna.ClampByte(value);
    this._cachedCss = null;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_R", new JSIL.MethodSignature(null, [$.Byte], []), function (value) {
    this.r = $jsilxna.ClampByte(value);
    this._cachedCss = null;
  });

  var equalsImpl = function (lhs, rhs) {
    return (lhs.r === rhs.r) && (lhs.g === rhs.g) &&
      (lhs.b === rhs.b) && (lhs.a === rhs.a);
  };

  $.Method({Static:true , Public:true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [$jsilxna.colorRef(), $jsilxna.colorRef()], [])),
    function op_Equality (a, b) {
      return equalsImpl(a, b);
    }
  );

  $.Method({Static:true , Public:true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [$jsilxna.colorRef(), $jsilxna.colorRef()], [])),
    function op_Inequality (a, b) {
      return !equalsImpl(a, b);
    }
  );

  $.Method({
    Static: true,
    Public: true
  }, "op_Multiply", new JSIL.MethodSignature($jsilxna.colorRef(), [$jsilxna.colorRef(), $.Single], []), function (color, multiplier) {
    var result = $jsilxna.makeColorInstance();
    result.r = $jsilxna.ClampByte(color.r * multiplier);
    result.g = $jsilxna.ClampByte(color.g * multiplier);
    result.b = $jsilxna.ClampByte(color.b * multiplier);
    result.a = $jsilxna.ClampByte(color.a * multiplier);
    return result;
  });

  $.Method({
    Static: true,
    Public: true
  }, "Lerp", new JSIL.MethodSignature($jsilxna.colorRef(), [$jsilxna.colorRef(), $jsilxna.colorRef(), $.Single], []),
    function Color_Lerp (a, b, amount) {
      var result = $jsilxna.makeColorInstance();
      result.r = $jsilxna.ClampByte(a.r + (b.r - a.r) * amount);
      result.g = $jsilxna.ClampByte(a.g + (b.g - a.g) * amount);
      result.b = $jsilxna.ClampByte(a.b + (b.b - a.b) * amount);
      result.a = $jsilxna.ClampByte(a.a + (b.a - a.a) * amount);
      return result;
    }
  );

  $.RawMethod(false, "toCss", function (alpha) {
    if ((typeof(this._cachedCss) === "string") && (this._cachedAlpha === alpha)) {
      return this._cachedCss;
    }

    var a = alpha || this.a;
    if (a < 255) {
      this._cachedAlpha = a;
      return this._cachedCss = "rgba(" + this.r + "," + this.g + "," + this.b + "," + a + ")";
    } else {
      this._cachedAlpha = a;
      return this._cachedCss = "rgb(" + this.r + "," + this.g + "," + this.b + ")";
    }
  });

  $.Method({Static:true , Public:true }, "FromNonPremultiplied",
    (new JSIL.MethodSignature($jsilxna.colorRef(), [$xnaasms.xna.TypeRef("Microsoft.Xna.Framework.Vector4")], [])),
    function FromNonPremultiplied (vector) {
      var result = $jsilxna.makeColorInstance();

      result.r = $jsilxna.ClampByte(vector.X * 255);
      result.g = $jsilxna.ClampByte(vector.Y * 255);
      result.b = $jsilxna.ClampByte(vector.Z * 255);
      result.a = $jsilxna.ClampByte(vector.W * 255);

      return result;
    }
  );

  $.Method({Static:true , Public:true }, "FromNonPremultiplied",
    (new JSIL.MethodSignature($jsilxna.colorRef(), [
          $.Int32, $.Int32,
          $.Int32, $.Int32
        ], [])),
    function FromNonPremultiplied (r, g, b, a) {
      var result = $jsilxna.makeColorInstance();

      result.r = $jsilxna.ClampByte(r);
      result.g = $jsilxna.ClampByte(g);
      result.b = $jsilxna.ClampByte(b);
      result.a = $jsilxna.ClampByte(a);

      return result;
    }
  );

  $.Method({Static:false, Public:true }, "ToVector4",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4"), [], [])),
    function ToVector4 () {
      return new Microsoft.Xna.Framework.Vector4(
        this.r / 255,
        this.g / 255,
        this.b / 255,
        this.a / 255
      );
    }
  );
};

$jsilxna.ClampByte = function (v) {
  v = (v | 0);

  if (v < 0)
    return 0;
  else if (v > 255)
    return 255;
  else
    return v;
};

(function () {
  // XNA3 doesn't have a BlendState class, so we substitute one.

  var graphicsAsm = JSIL.GetAssembly("Microsoft.Xna.Framework.Graphics", true);
  if (graphicsAsm === null) {
    JSIL.DeclareNamespace("Microsoft");
    JSIL.DeclareNamespace("Microsoft.Xna");
    JSIL.DeclareNamespace("Microsoft.Xna.Framework");
    JSIL.DeclareNamespace("Microsoft.Xna.Framework.Graphics");

    JSIL.MakeClass($jsilcore.TypeRef("System.Object"), "Microsoft.Xna.Framework.Graphics.BlendState", true, [], function ($) {
      $.Field({Static:true , Public:true }, "Additive", $.Type, function ($) {
          return null;
        });
      $.Field({Static:true , Public:true }, "AlphaBlend", $.Type, function ($) {
          return null;
        });
      $.Field({Static:true , Public:true }, "NonPremultiplied", $.Type, function ($) {
          return null;
        });
      $.Field({Static:true , Public:true }, "Opaque", $.Type, function ($) {
          return null;
        });
    });
  }
}) ();

JSIL.ImplementExternals("Microsoft.Xna.Framework.MathHelper", function ($) {
  $.Method({Static:true , Public:true }, "Clamp",
    (new JSIL.MethodSignature($.Single, [
          $.Single, $.Single,
          $.Single
        ], [])),
    function Clamp (value, min, max) {
      if (max < min) max = min;

      if (value < min)
        return min;
      else if (value > max)
        return max;
      else
        return value;
    }
  );

  $.Method({Static:true , Public:true }, "Lerp",
    (new JSIL.MethodSignature($.Single, [
          $.Single, $.Single,
          $.Single
        ], [])),
    function Lerp (value1, value2, amount) {
      return value1 + (value2 - value1) * amount;
    }
  );

  $.Method({Static:true , Public:true }, "Max",
    (new JSIL.MethodSignature($.Single, [$.Single, $.Single], [])),
    Math.max
  );

  $.Method({Static:true , Public:true }, "Min",
    (new JSIL.MethodSignature($.Single, [$.Single, $.Single], [])),
    Math.min
  );

  $.Method({Static:true , Public:true }, "ToDegrees",
    (new JSIL.MethodSignature($.Single, [$.Single], [])),
    function ToDegrees (radians) {
      return radians / (Math.PI / 180);
    }
  );

  $.Method({Static:true , Public:true }, "ToRadians",
    (new JSIL.MethodSignature($.Single, [$.Single], [])),
    function ToRadians (degrees) {
      return degrees * (Math.PI / 180);
    }
  );

  $.Method({Static:true , Public:true }, "WrapAngle",
    (new JSIL.MethodSignature($.Single, [$.Single], [])),
    function WrapAngle (angle) {
      var pi2 = Math.PI * 2;

      angle = System.Math.IEEERemainder(angle, pi2);

      if (angle <= -Math.PI)
        angle += pi2;
      else if (angle > Math.PI)
        angle -= pi2;

      return angle;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.TitleContainer", function ($) {

  $.Method({Static:true , Public:true }, "OpenStream",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.IO.Stream"), [$.String], [])),
    function OpenStream (name) {
      return new System.IO.FileStream(name, System.IO.FileMode.Open);
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GamerServices.Gamer", function ($) {
  var signedInGamers = null;

  $.Method({Static:true , Public:true }, "get_SignedInGamers",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.SignedInGamerCollection"), [], [])),
    function get_SignedInGamers () {
      // FIXME
      if (signedInGamers === null)
        signedInGamers = new $xnaasms[2].Microsoft.Xna.Framework.GamerServices.SignedInGamerCollection();

      return signedInGamers;
    }
  );

  $.Method({Static:false, Public:true }, "get_DisplayName",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_DisplayName () {
      // FIXME
      return "Player";
    }
  );

  $.Method({Static:false, Public:true }, "get_Gamertag",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_Gamertag () {
      // FIXME
      return "Player";
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GamerServices.GamerCollection`1", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      // FIXME
      this.gamer = new $xnaasms[2].Microsoft.Xna.Framework.GamerServices.SignedInGamer();
      this.tEnumerator = JSIL.ArrayEnumerator.Of(this.T);
    }
  );

  $.Method({Static:false, Public:true }, "GetEnumerator",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.GamerCollection`1+GamerCollectionEnumerator", [new JSIL.GenericParameter("T", "Microsoft.Xna.Framework.GamerServices.GamerCollection`1")]), [], [])),
    function GetEnumerator () {
      return new (tEnumerator)([this.gamer]);
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GamerServices.SignedInGamerCollection", function ($) {
  $.InheritDefaultConstructor();

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.SignedInGamer"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex")], [])),
    function get_Item (index) {
      // FIXME
      return this.gamer;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GamerServices.SignedInGamer", function ($) {
  $.Method({Static:false, Public:true }, "get_GameDefaults",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.GameDefaults"), [], [])),
    function get_GameDefaults () {
      // FIXME
      return null;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsGuest",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsGuest () {
      // FIXME
      return true;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsSignedInToLive",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsSignedInToLive () {
      // FIXME
      return false;
    }
  );

  $.Method({Static:false, Public:true }, "get_PartySize",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_PartySize () {
      // FIXME
      return 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_PlayerIndex",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex"), [], [])),
    function get_PlayerIndex () {
      // FIXME
      return $xnaasms[0].Microsoft.Xna.Framework.PlayerIndex.One;
    }
  );

  $.Method({Static:false, Public:true }, "get_Presence",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.GamerPresence"), [], [])),
    function get_Presence () {
      // FIXME
      return null;
    }
  );

  $.Method({Static:false, Public:true }, "get_Privileges",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.GamerPrivileges"), [], [])),
    function get_Privileges () {
      // FIXME
      return null;
    }
  );

  $.Method({Static:false, Public:true }, "GetAchievements",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.AchievementCollection"), [], [])),
    function GetAchievements () {
      // FIXME
      return null;
    }
  );

  $.Method({Static:false, Public:true }, "GetFriends",
    (new JSIL.MethodSignature($xnaasms[2].TypeRef("Microsoft.Xna.Framework.GamerServices.FriendCollection"), [], [])),
    function GetFriends () {
      // FIXME
      return null;
    }
  );

});

JSIL.MakeClass("Microsoft.Xna.Framework.Graphics.DisplayMode", "CurrentDisplayMode", true, [], function ($) {
  $.Method({Static:false, Public:true}, ".ctor",
    (new JSIL.MethodSignature(null, [$.Object], [])),
    function _ctor (device) {
      this.device = device;
    }
  );

  $.Method({Static:false, Public:true }, "get_AspectRatio",
    (new JSIL.MethodSignature($.Single, [], [])),
    function get_AspectRatio () {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "get_Format",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SurfaceFormat"), [], [])),
    function get_Format () {
      return Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color;
    }
  );

  $.Method({Static:false, Public:true }, "get_Height",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Height () {
      return this.device.canvas.height;
    }
  );

  $.Method({Static:false, Public:true }, "get_TitleSafeArea",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [], [])),
    function get_TitleSafeArea () {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "get_Width",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Width () {
      return this.device.canvas.width;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Storage.StorageDevice", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$.UInt32, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex")], [])),
    function _ctor (deviceIndex, playerIndex) {
      this.deviceIndex = deviceIndex;
      this.playerIndex = playerIndex;
    }
  );

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$.UInt32], [])),
    function _ctor (deviceIndex) {
      this.deviceIndex = deviceIndex;
    }
  );

  $.Method({Static:true , Public:true }, "add_DeviceChanged",
    (new JSIL.MethodSignature(null, [$xnaasms[5].TypeRef("System.EventHandler`1", [$xnaasms[5].TypeRef("System.EventArgs")])], [])),
    function add_DeviceChanged (value) {
      throw new Error('Not implemented');
    }
  );

  var callAsyncCallback = function (callback, state, data) {
    var asyncResult = new JSIL.FakeAsyncResult(state);
    asyncResult.data = data;

    if (typeof (callback) === "function")
      callback(asyncResult);

    return asyncResult;
  };

  $.Method({Static:false, Public:true }, "BeginOpenContainer",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.IAsyncResult"), [
          $.String, $xnaasms[5].TypeRef("System.AsyncCallback"),
          $.Object
        ], [])),
    function BeginOpenContainer (displayName, callback, state) {
      return callAsyncCallback(callback, state, {device: this, displayName: displayName});
    }
  );

  $.Method({Static:true , Public:true }, "BeginShowSelector",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.IAsyncResult"), [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex"), $xnaasms[5].TypeRef("System.AsyncCallback"),
          $.Object
        ], [])),
    function BeginShowSelector (player, callback, state) {
      return callAsyncCallback(callback, state, {player: player});
    }
  );

  $.Method({Static:true , Public:true }, "BeginShowSelector",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.IAsyncResult"), [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex"), $.Int32,
          $.Int32, $xnaasms[5].TypeRef("System.AsyncCallback"),
          $.Object
        ], [])),
    function BeginShowSelector (player, sizeInBytes, directoryCount, callback, state) {
      return callAsyncCallback(callback, state, {player: player});
    }
  );

  $.Method({Static:true , Public:true }, "BeginShowSelector",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.IAsyncResult"), [$xnaasms[5].TypeRef("System.AsyncCallback"), $.Object], [])),
    function BeginShowSelector (callback, state) {
      return callAsyncCallback(callback, state, {});
    }
  );

  $.Method({Static:true , Public:true }, "BeginShowSelector",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.IAsyncResult"), [
          $.Int32, $.Int32,
          $xnaasms[5].TypeRef("System.AsyncCallback"), $.Object
        ], [])),
    function BeginShowSelector (sizeInBytes, directoryCount, callback, state) {
      return callAsyncCallback(callback, state, {});
    }
  );

  $.Method({Static:false, Public:true }, "DeleteContainer",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function DeleteContainer (titleName) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "EndOpenContainer",
    (new JSIL.MethodSignature(getXnaStorage().TypeRef("Microsoft.Xna.Framework.Storage.StorageContainer"), [$xnaasms[5].TypeRef("System.IAsyncResult")], [])),
    function EndOpenContainer (result) {
      return new Microsoft.Xna.Framework.Storage.StorageContainer(
        result.data.device, 0, result.data.displayName
      );
    }
  );

  $.Method({Static:true , Public:true }, "EndShowSelector",
    (new JSIL.MethodSignature(getXnaStorage().TypeRef("Microsoft.Xna.Framework.Storage.StorageDevice"), [$xnaasms[5].TypeRef("System.IAsyncResult")], [])),
    function EndShowSelector (result) {
      return new Microsoft.Xna.Framework.Storage.StorageDevice(
        0, result.data.player || 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "get_FreeSpace",
    (new JSIL.MethodSignature($.Int64, [], [])),
    function get_FreeSpace () {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "get_IsConnected",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsConnected () {
      return true;
    }
  );

  $.Method({Static:false, Public:true }, "get_TotalSpace",
    (new JSIL.MethodSignature($.Int64, [], [])),
    function get_TotalSpace () {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:false}, "OnDeviceChanged",
    (new JSIL.MethodSignature(null, [$.Object, $xnaasms[5].TypeRef("System.EventArgs")], [])),
    function OnDeviceChanged (sender, args) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "remove_DeviceChanged",
    (new JSIL.MethodSignature(null, [$xnaasms[5].TypeRef("System.EventHandler`1", [$xnaasms[5].TypeRef("System.EventArgs")])], [])),
    function remove_DeviceChanged (value) {
      throw new Error('Not implemented');
    }
  );

});


JSIL.ImplementExternals("Microsoft.Xna.Framework.GameWindow", function ($) {
  $.Method({Static:false, Public:false}, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      var canvas = JSIL.Host.getCanvas();

      this._clientBounds = new Microsoft.Xna.Framework.Rectangle(
        0, 0, canvas.width | 0, canvas.height | 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "get_ClientBounds",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [], [])),
    function get_ClientBounds () {
      var canvas = JSIL.Host.getCanvas();
      this._clientBounds._ctor(
        0, 0, canvas.width, canvas.height
      );

      return this._clientBounds;
    }
  );

  $.Method({Static:false, Public:false}, "get_IsMinimized",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsMinimized () {
      // FIXME
      return false;
    }
  );

  $.Method({Static:false, Public:true }, "get_Title",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_Title () {
      // FIXME
      return document.title;
    }
  );

  $.Method({Static:false, Public:false}, "get_Handle",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.IntPtr"), [], [])),
    function get_Handle () {
      // FIXME
      return null;
    }
  );

  $.Method({Static:false, Public:true }, "set_AllowUserResizing",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_AllowUserResizing (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_Title",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function set_Title (value) {
      document.title = value;
    }
  );
});


JSIL.ImplementExternals("Microsoft.Xna.Framework.GamerServices.Guide", function ($) {
  $.Method({Static:true , Public:true }, "get_IsScreenSaverEnabled",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsScreenSaverEnabled () {
      // FIXME
    }
  );

  $.Method({Static:true , Public:true }, "get_IsTrialMode",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsTrialMode () {
      // FIXME
      return false;
    }
  );

  $.Method({Static:true , Public:true }, "get_IsVisible",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsVisible () {
      // FIXME
      return false;
    }
  );

  $.Method({Static:true , Public:false}, "get_IsVisibleNoThrow",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsVisibleNoThrow () {
      // FIXME
      return false;
    }
  );

  $.Method({Static:true , Public:true }, "get_SimulateTrialMode",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_SimulateTrialMode () {
      // FIXME
    }
  );

  $.Method({Static:true , Public:false}, "set_IsTrialMode",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_IsTrialMode (value) {
      // FIXME
    }
  );

  $.Method({Static:true , Public:false}, "set_IsVisible",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_IsVisible (value) {
      // FIXME
    }
  );

  $.Method({Static:true , Public:true }, "set_SimulateTrialMode",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_SimulateTrialMode (value) {
      // FIXME
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GameServiceContainer", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      this._services = {};
    }
  );

  $.Method({Static:false, Public:true }, "AddService",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Type"), $.Object], [])),
    function AddService (type, provider) {
      this._services[type.__TypeId__] = provider;
    }
  );

  $.Method({Static:false, Public:true }, "GetService",
    (new JSIL.MethodSignature($.Object, [$jsilcore.TypeRef("System.Type")], [])),
    function GetService (type) {
      var result = this._services[type.__TypeId__];

      if (!result)
        return null;
      else
        return result;
    }
  );

  $.Method({Static:false, Public:true }, "RemoveService",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Type")], [])),
    function RemoveService (type) {
      delete this._services[type.__TypeId__];
    }
  );

});

JSIL.MakeClass("System.Object", "JSIL.FakeAsyncResult", true, [], function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.RawMethod(false, ".ctor", function (state) {
    this._state = state;
  });

  $.Method({Static:false, Public:true , Virtual:true }, "get_AsyncState",
    new JSIL.MethodSignature($.Object, [], []),
    function () {
      return this._state;
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "get_AsyncWaitHandle",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.WaitHandle"), [], []),
    function () {
      return new JSIL.FakeWaitHandle();
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "get_CompletedSynchronously",
    new JSIL.MethodSignature($.Boolean, [], []),
    function () {
      return true;
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "get_IsCompleted",
    new JSIL.MethodSignature($.Boolean, [], []),
    function () {
      return true;
    }
  );

  $.Property({Static:false, Public:true , Virtual:true }, "IsCompleted", $.Boolean);
  $.Property({Static:false, Public:true , Virtual:true }, "AsyncWaitHandle", $jsilcore.TypeRef("System.Threading.WaitHandle"));
  $.Property({Static:false, Public:true , Virtual:true }, "AsyncState", $.Object);
  $.Property({Static:false, Public:true , Virtual:true }, "CompletedSynchronously", $.Boolean);

  $.ImplementInterfaces(
    /* 0 */ $jsilcore.TypeRef("System.IAsyncResult")
  );
});

JSIL.MakeClass("System.Object", "JSIL.FakeWaitHandle", true, [], function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.Method({Static:false, Public:true , Virtual:true }, "Close",
    JSIL.MethodSignature.Void,
    function Close () {
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "Dispose",
    JSIL.MethodSignature.Void,
    function Dispose () {
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "WaitOne",
    new JSIL.MethodSignature($.Boolean, [$.Int32, $.Boolean], []),
    function WaitOne (millisecondsTimeout, exitContext) {
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "WaitOne",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.TimeSpan"), $.Boolean], []),
    function WaitOne (timeout, exitContext) {
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "WaitOne",
    new JSIL.MethodSignature($.Boolean, [], []),
    function WaitOne () {
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "WaitOne",
    new JSIL.MethodSignature($.Boolean, [$.Int32], []),
    function WaitOne (millisecondsTimeout) {
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "WaitOne",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.TimeSpan")], []),
    function WaitOne (timeout) {
    }
  );

  $.Method({Static:false, Public:false}, "WaitOne",
    new JSIL.MethodSignature($.Boolean, [$.Int64, $.Boolean], []),
    function WaitOne (timeout, exitContext) {
      throw new Error('Not implemented');
    }
  );

  $.ImplementInterfaces(
    /* 0 */ $jsilcore.TypeRef("System.IDisposable")
  );
});

JSIL.ImplementExternals(
  "Microsoft.Xna.Framework.Color", $jsilxna.Color
);