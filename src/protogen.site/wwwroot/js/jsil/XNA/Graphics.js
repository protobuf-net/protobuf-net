/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

if (typeof ($jsilxna) === "undefined")
  throw new Error("JSIL.XNACore required");

$jsilxna.nextImageId = 0;

$jsilxna.ImageCache = function (
  capacity, capacityBytes,
  evictionMinimumAge, evictionAutomaticAge, evictionInterval
) {
  this.entries = {};
  this.accessHistory = {};
  this.count = 0;
  this.countBytes = 0;
  this.evictionPending = false;
  this.lastEvicted = this.now = Date.now();

  this.capacity = capacity; // total unique images
  this.capacityBytes = capacityBytes; // total 32bpp image bytes
  this.evictionMinimumAge = evictionMinimumAge; // if the age of an image is less than this (in ms) it is never evicted
  this.evictionAutomaticAge = evictionAutomaticAge; // if the age of an image is over this (in ms) it is automatically evicted
  this.evictionInterval = evictionInterval; // ms
};

$jsilxna.ImageCache.prototype.getItem = function (key) {
  this.accessHistory[key] = this.now;

  this.maybeEvictItems();

  return this.entries[key];
};

$jsilxna.ImageCache.prototype.setItem = function (key, value) {
  if (typeof (this.entries[key]) === "undefined") {
    this.count += 1;
    this.countBytes += value.sizeBytes;
  }

  this.accessHistory[key] = this.now;
  this.entries[key] = value;

  this.maybeEvictItems();
};

$jsilxna.ImageCache.prototype.maybeEvictItems = function () {
  if (this.evictionPending)
    return;

  var nextEviction = this.lastEvicted + this.evictionInterval;

  if (this.now >= nextEviction) {
    this.lastEvicted = this.now;
    this.evictionPending = true;
    JSIL.Host.runLater(this.evictExtraItems.bind(this));
  }
};

$jsilxna.ImageCache.prototype.evictExtraItems = function () {
  this.evictionPending = false;
  var keys = Object.keys(this.accessHistory);

  keys.sort(function (lhs, rhs) {
    var lhsTimestamp = this.accessHistory[lhs];
    var rhsTimestamp = this.accessHistory[rhs];

    if (lhsTimestamp > rhsTimestamp)
      return 1;
    else if (rhsTimestamp > lhsTimestamp)
      return -1;
    else
      return 0;
  }.bind(this));

  for (var i = 0, l = this.count; i < l; i++) {
    var age = this.now - this.accessHistory[keys[i]];
    if (age <= this.evictionMinimumAge)
      continue;

    if (age >= this.evictionAutomaticAge) {
    } else {
      if ((this.count <= this.capacity) && (this.countBytes <= this.capacityBytes))
        continue;
    }

    var item = this.entries[keys[i]];

    delete this.accessHistory[keys[i]];
    delete this.entries[keys[i]];

    this.count -= 1;
    if ((typeof (item) !== "undefined") && (item !== null)) {
      this.countBytes -= item.sizeBytes;
    }
  }
};


$jsilxna.imageChannelCache = new $jsilxna.ImageCache(
  1024,
  (1024 * 1024) * 256,
  2000,
  15000,
  500
);

$jsilxna.textCache = new $jsilxna.ImageCache(
  512,
  (1024 * 1024) * 32,
  250,
  1000,
  250
);


$jsilxna.get2DContext = function get2DContext (canvas, enableWebGL) {
  var result = null;
  var hasWebGL = typeof (WebGL2D) !== "undefined";
  var extraMessage = "";

  if (typeof (document) !== "undefined") {
    var forceCanvas = (document.location.search.indexOf("forceCanvas") >= 0);
    var forceWebGL = (document.location.search.indexOf("forceWebGL") >= 0);

    $textCachingSupported = (window.navigator.userAgent.indexOf("; MSIE ") < 0) &&
      (window.navigator.userAgent.indexOf("IE 11.0") < 0);
  } else {
    var forceCanvas = true;
    var forceWebGL = false;
    $textCachingSupported = false;
  }

  if (forceWebGL && enableWebGL) {
    $jsilxna.testedWebGL = $jsilxna.workingWebGL = true;
  }

  if (
    (hasWebGL && enableWebGL &&
    ($jsilxna.allowWebGL !== false) &&
    !forceCanvas) || (enableWebGL && forceWebGL)
  ) {
    if (!$jsilxna.testedWebGL) {
      try {
        var testCanvas = JSIL.Host.createCanvas(320, 240);
        WebGL2D.enable(testCanvas);
        var testContext = testCanvas.getContext("webgl-2d");

        $jsilxna.workingWebGL = (testContext != null) && (testContext.isWebGL);
      } catch (exc) {
        extraMessage = String(exc);
        $jsilxna.workingWebGL = false;
      }

      $jsilxna.testedWebGL = true;
    }

    // WebGL is broken in Firefox 14.0a1/a2
    if (
      (window.navigator.userAgent.indexOf("Firefox/14.0a1") >= 0) ||
      (window.navigator.userAgent.indexOf("Firefox/14.0a2") >= 0)
    ) {
      $jsilxna.workingWebGL = false;
      extraMessage = "Firefox 14.0 alpha has broken WebGL support.";
    }

    if ($jsilxna.workingWebGL) {
      WebGL2D.enable(canvas);
      result = canvas.getContext("webgl-2d");
    } else {
      var msg = "WARNING: WebGL not available or broken. Using HTML5 canvas instead. " + extraMessage;
      if (window.console && (typeof (window.console.error) === "function"))
        console.error(msg);

      JSIL.Host.logWriteLine(msg);
    }
  }

  if (!result)
    result = canvas.getContext("2d");

  if (!result)
    throw new Error("Failed to initialize HTML5 Canvas.");

  return result;
};

$jsilxna.channelNames = ["_r", "_g", "_b", "_a"];
$jsilxna.channelKeys = ["r", "g", "b", "a"];

$jsilxna.getCachedImageChannels = function (image, key) {
  var result = $jsilxna.imageChannelCache.getItem(key) || null;
  return result;
};

$jsilxna.setCachedImageChannels = function (image, key, value) {
  $jsilxna.imageChannelCache.setItem(key, value);
};

$jsilxna.imageChannels = function (image) {
  this.sourceImage = image;
  this.width = image.naturalWidth || image.width;
  this.height = image.naturalHeight || image.height;
  this.xOffset = 1;
  this.yOffset = 1;
  // 32BPP * one image per channel
  this.sizeBytes = (this.width * this.height * 4) * 4;

  var createChannel = (function (ch) {
    var canvas = this[ch] = JSIL.Host.createCanvas(this.width + 2, this.height + 2);
    var context = this[ch + "Context"] = $jsilxna.get2DContext(canvas, false);

    context.globalCompositeOperation = "copy";
    context.globalCompositeAlpha = 1.0;
  }).bind(this);

  createChannel("r");
  createChannel("g");
  createChannel("b");
  createChannel("a");

  if (image.tagName.toLowerCase() === "canvas") {
    this.sourceImageData = $jsilxna.get2DContext(image, false).getImageData(0, 0, image.width, image.height);
  } else {
    // Workaround for bug in Firefox's canvas implementation that treats the outside of a canvas as solid white
    this.aContext.clearRect(0, 0, this.width + 2, this.height + 2);
    this.aContext.drawImage(image, this.xOffset, this.yOffset);

    this.sourceImageData = this.aContext.getImageData(this.xOffset, this.yOffset, this.width, this.height);
  }

  this.aContext.clearRect(0, 0, this.width + 2, this.height + 2);

  this.makeImageData = (function () {
    return this.aContext.createImageData(this.width, this.height);
  }).bind(this);

  this.putImageData = (function (ch, data) {
    var context = this[ch + "Context"];

    context.putImageData(data, this.xOffset, this.yOffset);
  }).bind(this);
};

$jsilxna.getImageChannels = function (image, key) {
  var cached = $jsilxna.getCachedImageChannels(image, key);
  if (cached !== null)
    return cached;

  var width = image.naturalWidth || image.width;
  var height = image.naturalHeight || image.height;

  // Workaround for chromium bug where sometimes images aren't fully initialized.
  if ((width < 1) || (height < 1))
    return null;

  var result = null;

  // If pre-generated channel images are available, use them instead
  if (image.assetName) {
    result = {
      sourceImage: image,
      width: width,
      height: height,
      xOffset: 0,
      yOffset: 0,
      sizeBytes: 0
    };

    for (var i = 0; i < 4; i++) {
      var channelAssetName = image.assetName + $jsilxna.channelNames[i];
      if (!JSIL.Host.doesAssetExist(channelAssetName)) {
        result = null;
        break;
      }

      var channelAsset = JSIL.Host.getAsset(channelAssetName);
      result[$jsilxna.channelKeys[i]] = channelAsset.image;
    }

    if (result) {
      $jsilxna.imageChannelCache.setItem(key, result);
      return result;
    }
  }

  result = new $jsilxna.imageChannels(image);

  try {
    var rData = result.makeImageData(), gData = result.makeImageData(), bData = result.makeImageData(), aData = result.sourceImageData;
    var rBytes = rData.data, gBytes = gData.data, bBytes = bData.data, aBytes = aData.data;

    for (var i = 0, l = (result.width * result.height * 4); i < l; i += 4) {
      var alpha = aBytes[(i + 3) | 0];

      rBytes[(i + 0) | 0] = alpha;
      rBytes[(i + 3) | 0] = aBytes[(i + 0) | 0];

      gBytes[(i + 1) | 0] = alpha;
      gBytes[(i + 3) | 0] = aBytes[(i + 1) | 0];

      bBytes[(i + 2) | 0] = alpha;
      bBytes[(i + 3) | 0] = aBytes[(i + 2) | 0];

      aBytes[(i + 0) | 0] = aBytes[(i + 1) | 0] = aBytes[(i + 2) | 0] = 0;
      aBytes[(i + 3) | 0] = alpha;
    }

    result.putImageData("r", rData);
    result.putImageData("g", gData);
    result.putImageData("b", bData);
    result.putImageData("a", aData);

    $jsilxna.setCachedImageChannels(image, key, result);
  } catch (exc) {
    return null;
  }

  return result;
};

$jsilxna.getImageTopLeftPixel = function (image) {
  var cached = image.topLeftPixel;
  if (typeof (cached) === "string")
    return cached;

  var canvas = JSIL.Host.createCanvas(1, 1);
  var context = $jsilxna.get2DContext(canvas, false);

  var imageData;
  if (image.tagName.toLowerCase() === "canvas") {
    imageData = $jsilxna.get2DContext(image, false).getImageData(0, 0, 1, 1);
  } else {
    context.globalCompositeOperation = "copy";
    context.globalCompositeAlpha = 1.0;
    context.clearRect(0, 0, 1, 1);
    context.drawImage(image, 0, 0);
    imageData = context.getImageData(0, 0, 1, 1);
  }

  var result = "0,0,0,0";
  try {
    var r = imageData.data[0];
    var g = imageData.data[1];
    var b = imageData.data[2];
    var a = imageData.data[3] / 255;

    image.topLeftPixel = result = r + "," + g + "," + b + "," + a;
  } catch (exc) {}

  return result;
};

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.BasicEffect", function ($) {
  $.RawMethod(false, "$coreCtor", function () {
    this.light0 = new Microsoft.Xna.Framework.Graphics.DirectionalLight();
    this.light1 = new Microsoft.Xna.Framework.Graphics.DirectionalLight();
    this.light2 = new Microsoft.Xna.Framework.Graphics.DirectionalLight();
    this.world = Microsoft.Xna.Framework.Matrix._identity;
    this.view = Microsoft.Xna.Framework.Matrix._identity;
    this.projection = Microsoft.Xna.Framework.Matrix._identity;
  });

  $.Method({Static:false, Public:true }, ".ctor",
    new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice")], []),
    function _ctor (device) {
      this.$coreCtor();
    }
  );

  $.Method({Static:false, Public:false}, ".ctor",
    new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.BasicEffect")], []),
    function _ctor (cloneSource) {
      this.$coreCtor();
    }
  );

  $.Method({Static:false, Public:true }, "set_Alpha",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function set_Alpha (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_AmbientLightColor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function set_AmbientLightColor (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_DiffuseColor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function set_DiffuseColor (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_EmissiveColor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function set_EmissiveColor (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_FogColor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function set_FogColor (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_FogEnabled",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_FogEnabled (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_FogEnd",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function set_FogEnd (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_FogStart",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function set_FogStart (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_LightingEnabled",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_LightingEnabled (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_PreferPerPixelLighting",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_PreferPerPixelLighting (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_SpecularColor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function set_SpecularColor (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_SpecularPower",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function set_SpecularPower (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_Texture",
    (new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Texture2D")], [])),
    function set_Texture (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_TextureEnabled",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_TextureEnabled (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_VertexColorEnabled",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_VertexColorEnabled (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "get_View",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [], [])),
    function get_View (value) {
      return this.view;
    }
  );

  $.Method({Static:false, Public:true }, "set_View",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function set_View (value) {
      this.view = value.MemberwiseClone();
    }
  );

  $.Method({Static:false, Public:true }, "get_World",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [], [])),
    function get_World (value) {
      return this.world;
    }
  );

  $.Method({Static:false, Public:true }, "set_World",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function set_World (value) {
      this.world = value.MemberwiseClone();
    }
  );

  $.Method({Static:false, Public:true }, "get_Projection",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix"), [], [])),
    function get_Projection (value) {
      return this.projection;
    }
  );

  $.Method({Static:false, Public:true }, "set_Projection",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Matrix")], [])),
    function set_Projection (value) {
      this.projection = value.MemberwiseClone();
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "get_DirectionalLight0",
    new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DirectionalLight"), [], []),
    function get_DirectionalLight0 () {
      return this.light0;
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "get_DirectionalLight1",
    new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DirectionalLight"), [], []),
    function get_DirectionalLight1 () {
      return this.light1;
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "get_DirectionalLight2",
    new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DirectionalLight"), [], []),
    function get_DirectionalLight2 () {
      return this.light2;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.Effect", function ($) {
  $.Method({Static:false, Public:true }, "get_CurrentTechnique",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectTechnique"), [], [])),
    function get_CurrentTechnique () {
      // FIXME
      return new Microsoft.Xna.Framework.Graphics.EffectTechnique();
    }
  );

  $.Method({Static:false, Public:true }, "get_Techniques",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectTechniqueCollection"), [], [])),
    function get_Techniques () {
      // FIXME
      return new Microsoft.Xna.Framework.Graphics.EffectTechniqueCollection();
    }
  );

  $.Method({Static:false, Public:true }, "get_Parameters",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectParameterCollection"), [], [])),
    function get_Parameters () {
      // FIXME
      return new Microsoft.Xna.Framework.Graphics.EffectParameterCollection();
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.EffectParameterCollection", function ($) {
  $.Method({Static:false, Public:true }, "get_Count",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Count () {
      // FIXME
      return 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectParameter"), [$.Int32], [])),
    function get_Item (index) {
      // FIXME
      return new Microsoft.Xna.Framework.Graphics.EffectParameter();
    }
  );

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectParameter"), [$.String], [])),
    function get_Item (name) {
      // FIXME
      return new Microsoft.Xna.Framework.Graphics.EffectParameter();
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.EffectParameter", function ($) {
  $.Method({Static:false, Public:true }, "get_Elements",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectParameterCollection"), [], [])),
    function get_Elements () {
      // FIXME
      return new Microsoft.Xna.Framework.Graphics.EffectParameterCollection();
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4")])], [])),
    function SetValue (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector4")], [])),
    function SetValue (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")])], [])),
    function SetValue (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")], [])),
    function SetValue (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")])], [])),
    function SetValue (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")], [])),
    function SetValue (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$.Single])], [])),
    function SetValue (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "SetValue",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function SetValue (value) {
      // FIXME
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.EffectTechnique", function ($) {
  $.Method({Static:false, Public:true }, "get_Passes",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectPassCollection"), [], [])),
    function get_Passes () {
      // FIXME
      return new Microsoft.Xna.Framework.Graphics.EffectPassCollection();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.EffectPass", function ($) {
  $.Method({Static:false, Public:true }, "Apply",
    (JSIL.MethodSignature.Void),
    function Apply () {
      // FIXME
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.GraphicsDeviceManager", function ($) {
  var mscorlib = JSIL.GetCorlib();

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms.xnaGame.TypeRef("Microsoft.Xna.Framework.Game")], [])),
    function _ctor (game) {
      this.game = game;
      this.device = new Microsoft.Xna.Framework.Graphics.GraphicsDevice(this);
      game.graphicsDeviceService = this;
      game.graphicsDeviceManager = this;
      game.Services.AddService(Microsoft.Xna.Framework.IGraphicsDeviceManager.__Type__, this);
      game.Services.AddService(Microsoft.Xna.Framework.Graphics.IGraphicsDeviceService.__Type__, this);

      var oc = this.device.originalCanvas;
      this._width = oc.width;
      this._height = oc.height;
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, "get_GraphicsDevice", new JSIL.MethodSignature($jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), [], []), function () {
    return this.device;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_PreferredBackBufferWidth", new JSIL.MethodSignature($.Int32, [], []), function () {
    return this._width;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_PreferredBackBufferHeight", new JSIL.MethodSignature($.Int32, [], []), function () {
    return this._height;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_PreferredBackBufferWidth", new JSIL.MethodSignature(null, [$.Int32], []), function (value) {
    this._width = value;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_PreferredBackBufferHeight", new JSIL.MethodSignature(null, [$.Int32], []), function (value) {
    this._height = value;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_IsFullScreen", new JSIL.MethodSignature($.Boolean, [], []), function (value) {
    return true;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_IsFullScreen", new JSIL.MethodSignature(null, [$.Boolean], []), function (value) {
    // FIXME
  });

  $.Method({
    Static: false,
    Public: true
  }, "ApplyChanges", JSIL.MethodSignature.Void, function () {
    var oc = this.device.originalCanvas;

    $jsilbrowserstate.nativeWidth = this.device.originalWidth = this._width;
    $jsilbrowserstate.nativeHeight = this.device.originalHeight = this._height;

    var svc = JSIL.Host.getService("canvas");
    svc.applySize(oc, this._width, this._height, true);
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.Viewport", function ($) {
  var mscorlib = JSIL.GetCorlib();

  $.Method({
    Static: false,
    Public: true
  }, "get_X", new JSIL.MethodSignature($.Int32, [], []), function () {
    return this._x;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_Y", new JSIL.MethodSignature($.Int32, [], []), function () {
    return this._y;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_Width", new JSIL.MethodSignature($.Int32, [], []), function () {
    return this._width;
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_Height", new JSIL.MethodSignature($.Int32, [], []), function () {
    return this._height;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_X", new JSIL.MethodSignature(null, [$.Int32], []), function (value) {
    this._x = value | 0;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_Y", new JSIL.MethodSignature(null, [$.Int32], []), function (value) {
    this._y = value | 0;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_Width", new JSIL.MethodSignature(null, [$.Int32], []), function (value) {
    this._width = value | 0;
  });

  $.Method({
    Static: false,
    Public: true
  }, "set_Height", new JSIL.MethodSignature(null, [$.Int32], []), function (value) {
    this._height = value | 0;
  });

  $.Method({Static:false, Public:true }, "get_Bounds",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [], [])),
    function get_Bounds () {
      return new Microsoft.Xna.Framework.Rectangle(this._x | 0, this._y | 0, this._width | 0, this._height | 0);
    }
  );

  $.Method({Static:false, Public:true }, "get_TitleSafeArea",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"), [], [])),
    function get_TitleSafeArea () {
      return new Microsoft.Xna.Framework.Rectangle(this._x | 0, this._y | 0, this._width | 0, this._height | 0);
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Int32, $.Int32, $.Int32, $.Int32], []), function Viewport_ctor (x, y, width, height) {
    this._x = x | 0;
    this._y = y | 0;
    this._width = width | 0;
    this._height = height | 0;
  });

  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")], []), function Viewport_ctor (rectangle) {
    this._x = rectangle.X | 0;
    this._y = rectangle.Y | 0;
    this._width = rectangle.Width | 0;
    this._height = rectangle.Height | 0;
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.SpriteBatch", function ($) {
  if (false) {
    var $canvasDrawImage = function canvasDrawImage (image, sourceX, sourceY, sourceW, sourceH, positionX, positionY, destW, destH) {
      try {
        this.device.context.drawImage(
          image, sourceX, sourceY, sourceW, sourceH, positionX, positionY, destW, destH
        );
      } catch (exc) {
        console.log("Error calling drawImage with arguments ", Array.prototype.slice.call(arguments), ": ", exc);
      }
    }
  } else {
    var $canvasDrawImage = function canvasDrawImage (image, sourceX, sourceY, sourceW, sourceH, positionX, positionY, destW, destH) {
      this.device.context.drawImage(
        image, sourceX, sourceY, sourceW, sourceH, positionX, positionY, destW, destH
      );
    }
  }

  $.RawMethod(false, "$canvasDrawImage", $canvasDrawImage);

  $.RawMethod(false, "$save", function canvasSave () {
    this.saveCount += 1;
    this.device.context.save();
  });

  $.RawMethod(false, "$restore", function canvasRestore () {
    this.restoreCount += 1;
    this.device.context.restore();
  });

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice")], [])),
    function _ctor (graphicsDevice) {
      this.device = graphicsDevice;
      this.defer = false;
      this.deferSorter = null;

      this.deferredPoolSize = 1024;

      this.deferredDrawPool = [];
      this.deferredDrawStringPool = [];

      this.deferredDraws = [];

      this.oldBlendState = null;
      this.isWebGL = false;
      this.spriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects;
      this.flipHorizontally = this.spriteEffects.FlipHorizontally.value;
      this.flipVertically = this.spriteEffects.FlipVertically.value;

      this.saveCount = 0;
      this.restoreCount = 0;
    }
  );

  $.RawMethod(false, "$cloneExisting", function (spriteBatch) {
    this.device = spriteBatch.device;
    this.defer = false;
    this.deferSorter = null;
    this.oldBlendState = null;
    this.isWebGL = spriteBatch.isWebGL;
    this.spriteEffects = spriteBatch.spriteEffects;
    this.flipHorizontally = spriteBatch.flipHorizontally;
    this.flipVertically = spriteBatch.flipVertically;

    this.saveCount = 0;
    this.restoreCount = 0;
  });

  $.RawMethod(false, "$applyBlendState", function () {
    if ((typeof (this.blendState) === "object") && (this.blendState !== null))
      this.device.set_BlendState(this.blendState);
    else
      this.device.set_BlendState(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend);
  });

  $.RawMethod(false, "$applySamplerState", function () {
    if ((typeof (this.samplerState) === "object") && (this.samplerState !== null))
      this.device.SamplerStates.set_Item(0, this.samplerState);
    else
      this.device.SamplerStates.set_Item(0, Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp);
  });

  $.RawMethod(false, "$updateMatrices", function () {
    var viewport = this.device.get_Viewport();
    var xTranslation = 0, yTranslation = 0;
    var xScale = 1, yScale = 1;

    if ((typeof (this.transformMatrix) === "object") && (this.transformMatrix !== null)) {
      xTranslation += this.transformMatrix.xTranslation;
      yTranslation += this.transformMatrix.yTranslation;
      xScale *= this.transformMatrix.xScale;
      yScale *= this.transformMatrix.yScale;
    }

    this.device.$UpdateViewport();
    this.device.context.translate(xTranslation, yTranslation);
    this.device.context.scale(xScale, yScale);
  });

  $.Method({Static:false, Public:true }, "Begin",
    (new JSIL.MethodSignature(null, [$xnaasms[5].TypeRef("System.Array") /* AnyType[] */ ], [])),
    function SpriteBatch_Begin (sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix) {
      this.saveCount = 0;
      this.restoreCount = 0;

      $jsilxna.imageChannelCache.now = Date.now();
      $jsilxna.textCache.now = Date.now();

      this.isWebGL = this.device.context.isWebGL || false;

      this.$save();
      this.deferSorter = null;

      this.blendState = blendState;
      this.samplerState = samplerState;

      var textureIndex = 0;

      if (sortMode === Microsoft.Xna.Framework.Graphics.SpriteSortMode.Immediate) {
        this.defer = false;
        this.$applyBlendState();
        this.$applySamplerState();
      } else if (sortMode === Microsoft.Xna.Framework.Graphics.SpriteSortMode.BackToFront) {
        this.defer = true;
        this.deferSorter = function Sort_BackToFront (lhs, rhs) {
          var result = rhs.depth - lhs.depth;
          if (result === 0)
            result = rhs.index - lhs.index;

          return result;
        };
      } else if (sortMode === Microsoft.Xna.Framework.Graphics.SpriteSortMode.FrontToBack) {
        this.defer = true;
        this.deferSorter = function Sort_FrontToBack (lhs, rhs) {
          var result = lhs.depth - rhs.depth;
          if (result === 0)
            result = rhs.index - lhs.index;

          return result;
        };
      } else if (sortMode === Microsoft.Xna.Framework.Graphics.SpriteSortMode.Texture) {
        this.defer = true;
        this.deferSorter = function Sort_Texture (lhs, rhs) {
          var result = JSIL.CompareValues(lhs.texture.id, rhs.texture.id);
          if (result === 0)
            result = JSIL.CompareValues(lhs.index, rhs.index);

          return result;
        };
      } else if (sortMode === Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred) {
        this.defer = true;
      }

      this.transformMatrix = transformMatrix;
      this.$updateMatrices();
    }
  );

  $.Method({Static:false, Public:true }, "End",
    (JSIL.MethodSignature.Void),
    function SpriteBatch_End () {
      if (this.defer) {
        this.defer = false;

        this.$applyBlendState();
        this.$applySamplerState();
        this.$updateMatrices();

        if (this.deferSorter !== null)
          this.deferredDraws.sort(this.deferSorter);

        for (var i = 0, l = this.deferredDraws.length; i < l; i++) {
          var draw = this.deferredDraws[i];
          draw.function.apply(this, draw.arguments);

          // FIXME: Leaks references to textures, fonts, and colors.
          if (draw.pool.length < this.deferredPoolSize)
            draw.pool.push(draw);
        }
      }

      this.deferredDraws.length = 0;

      this.$restore();

      this.$applyBlendState();
      this.$applySamplerState();

      this.device.$UpdateViewport();

      if (this.saveCount !== this.restoreCount)
        JSIL.RuntimeError("Unbalanced canvas save/restore");
    }
  );

  $.RawMethod(false, "DeferBlit",
    function SpriteBatch_DeferBlit (
      texture, positionX, positionY, width, height,
      sourceX, sourceY, sourceW, sourceH,
      color, rotation, originX, originY,
      scaleX, scaleY, effects, depth
    ) {
      var entry = null, deferArguments = null;

      var pool = this.deferredDrawPool;
      var dd = this.deferredDraws;

      if (pool.length > 0) {
        entry = pool.pop();

        deferArguments = entry.arguments;
        deferArguments[9].__CopyMembers__(color, deferArguments[9]);
      } else {
        entry = {
          function: null,
          index: 0,
          depth: 0.0,
          texture: null,
          pool: null,
          arguments: new Array(17)
        };

        deferArguments = entry.arguments;
        deferArguments[9] = color.MemberwiseClone();
      }

      entry.function = this.InternalDraw;
      entry.index = dd.length;
      entry.pool = pool;

      entry.depth = depth;
      entry.texture = texture;

      deferArguments[0] = texture;
      deferArguments[1] = positionX;
      deferArguments[2] = positionY;
      deferArguments[3] = width;
      deferArguments[4] = height;
      deferArguments[5] = sourceX;
      deferArguments[6] = sourceY;
      deferArguments[7] = sourceW;
      deferArguments[8] = sourceH;
      // deferArguments[9] = color.MemberwiseClone();
      deferArguments[10] = rotation;
      deferArguments[11] = originX;
      deferArguments[12] = originY;
      deferArguments[13] = scaleX;
      deferArguments[14] = scaleY;
      deferArguments[15] = effects;
      deferArguments[16] = depth;

      dd.push(entry);
    }
  );

  $.RawMethod(false, "DeferDrawString",
    function SpriteBatch_DeferDrawString (
      font, text,
      positionX, positionY,
      color, rotation,
      originX, originY,
      scaleX, scaleY,
      effects, depth
    ) {
      var entry = null, deferArguments = null;

      var pool = this.deferredDrawStringPool;
      var dd = this.deferredDraws;

      if (pool.length > 0) {
        entry = pool.pop();

        deferArguments = entry.arguments;
        deferArguments[4].__CopyMembers__(color, deferArguments[4]);
      } else {
        entry = {
          function: null,
          index: 0,
          depth: 0.0,
          texture: null,
          pool: null,
          arguments: new Array(12)
        };

        deferArguments = entry.arguments;
        deferArguments[4] = color.MemberwiseClone();
      }

      entry.function = this.InternalDrawString;
      entry.index = dd.length;
      entry.pool = pool;

      entry.depth = depth;
      entry.texture = font.texture || null;

      deferArguments[0] = font;
      deferArguments[1] = text;
      deferArguments[2] = positionX;
      deferArguments[3] = positionY;
      // deferArguments[4] = color.MemberwiseClone();
      deferArguments[5] = rotation;
      deferArguments[6] = originX;
      deferArguments[7] = originY;
      deferArguments[8] = scaleX;
      deferArguments[9] = scaleY;
      deferArguments[10] = effects;
      deferArguments[11] = depth;

      dd.push(entry);
    }
  );

  $.Method({Static:false, Public:true }, "Draw",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $jsilxna.colorRef()
        ], [])),
    function Draw (texture, position, color) {
      var w = texture.get_Width();
      var h = texture.get_Height();

      this.InternalDraw(
        texture, position.X, position.Y, w, h,
        0, 0, w, h,
        color, 0,
        0, 0,
        1, 1,
        null, 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "Draw",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[5].TypeRef("System.Nullable`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $jsilxna.colorRef()
        ], [])),
    function Draw (texture, position, sourceRectangle, color) {
      var sourceX = 0, sourceY = 0, sourceWidth = 0, sourceHeight = 0;
      if (sourceRectangle !== null) {
        sourceX = sourceRectangle.X;
        sourceY = sourceRectangle.Y;
        sourceWidth = sourceRectangle.Width;
        sourceHeight = sourceRectangle.Height;
      } else {
        sourceWidth = texture.get_Width();
        sourceHeight = texture.get_Height();
      }

      this.InternalDraw(
        texture, position.X, position.Y, sourceWidth, sourceHeight,
        sourceX, sourceY, sourceWidth, sourceHeight,
        color, 0,
        0, 0,
        1, 1,
        null, 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawScaleF",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[5].TypeRef("System.Nullable`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $jsilxna.colorRef(),
          $.Single, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $.Single, $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteEffects"),
          $.Single
        ], [])),
    function DrawScaleF (texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth) {
      var sourceX = 0, sourceY = 0, sourceWidth = 0, sourceHeight = 0;
      if (sourceRectangle !== null) {
        sourceX = sourceRectangle.X;
        sourceY = sourceRectangle.Y;
        sourceWidth = sourceRectangle.Width;
        sourceHeight = sourceRectangle.Height;
      } else {
        sourceWidth = texture.get_Width();
        sourceHeight = texture.get_Height();
      }

      this.InternalDraw(
        texture, position.X, position.Y, sourceWidth, sourceHeight,
        sourceX, sourceY, sourceWidth, sourceHeight,
        color, rotation,
        origin.X, origin.Y,
        scale, scale,
        effects, layerDepth
      );
    }
  );

  $.Method({Static:false, Public:true }, "Draw",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[5].TypeRef("System.Nullable`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $jsilxna.colorRef(),
          $.Single, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteEffects"),
          $.Single
        ], [])),
    function Draw (texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth) {
      var sourceX = 0, sourceY = 0, sourceWidth = 0, sourceHeight = 0;
      if (sourceRectangle !== null) {
        sourceX = sourceRectangle.X;
        sourceY = sourceRectangle.Y;
        sourceWidth = sourceRectangle.Width;
        sourceHeight = sourceRectangle.Height;
      } else {
        sourceWidth = texture.get_Width();
        sourceHeight = texture.get_Height();
      }

      this.InternalDraw(
        texture, position.X, position.Y, sourceWidth, sourceHeight,
        sourceX, sourceY, sourceWidth, sourceHeight,
        color, rotation,
        origin.X, origin.Y,
        scale.X, scale.Y,
        effects, layerDepth
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawRect",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"),
          $jsilxna.colorRef()
        ], [])),
    function DrawRect (texture, destinationRectangle, color) {
      this.InternalDraw(
        texture, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height,
        0, 0, texture.get_Width(), texture.get_Height(),
        color, 0,
        0, 0,
        1, 1,
        null, 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawRect",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"),
          $xnaasms[5].TypeRef("System.Nullable`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $jsilxna.colorRef()
        ], [])),
    function DrawRect (texture, destinationRectangle, sourceRectangle, color) {
      var sourceX = 0, sourceY = 0, sourceWidth = 0, sourceHeight = 0;
      if (sourceRectangle !== null) {
        sourceX = sourceRectangle.X;
        sourceY = sourceRectangle.Y;
        sourceWidth = sourceRectangle.Width;
        sourceHeight = sourceRectangle.Height;
      } else {
        sourceWidth = texture.get_Width();
        sourceHeight = texture.get_Height();
      }

      this.InternalDraw(
        texture, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height,
        sourceX, sourceY, sourceWidth, sourceHeight,
        color, 0,
        0, 0,
        1, 1,
        null, 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawRect",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle"),
          $xnaasms[5].TypeRef("System.Nullable`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $jsilxna.colorRef(),
          $.Single, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteEffects"), $.Single
        ], [])),
    function DrawRect (texture, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth) {
      var sourceX = 0, sourceY = 0, sourceWidth = 0, sourceHeight = 0;
      if (sourceRectangle !== null) {
        sourceX = sourceRectangle.X;
        sourceY = sourceRectangle.Y;
        sourceWidth = sourceRectangle.Width;
        sourceHeight = sourceRectangle.Height;
      } else {
        sourceWidth = texture.get_Width();
        sourceHeight = texture.get_Height();
      }

      this.InternalDraw(
        texture, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height,
        sourceX, sourceY, sourceWidth, sourceHeight,
        color, rotation,
        origin.X, origin.Y,
        1, 1,
        effects, layerDepth
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawString",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont"), $.String,
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.colorRef()
        ], [])),
    function DrawString (spriteFont, text, position, color) {
      this.InternalDrawString(
        spriteFont, text,
        position.X, position.Y,
        color, 0,
        0, 0,
        1, 1,
        null, 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawStringBuilder",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont"), $xnaasms[5].TypeRef("System.Text.StringBuilder"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.colorRef()
        ], [])),
    function DrawStringBuilder (spriteFont, text, position, color) {
      this.InternalDrawString(
        spriteFont, text.toString(),
        position.X, position.Y,
        color, 0,
        0, 0,
        1, 1,
        null, 0
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawStringScaleF",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont"), $.String,
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.colorRef(),
          $.Single, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $.Single, $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteEffects"),
          $.Single
        ], [])),
    function DrawStringScaleF (spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth) {
      this.InternalDrawString(
        spriteFont, text,
        position.X, position.Y,
        color, rotation,
        origin.X, origin.Y,
        scale, scale,
        effects, layerDepth
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawStringBuilderScaleF",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont"), $xnaasms[5].TypeRef("System.Text.StringBuilder"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.colorRef(),
          $.Single, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $.Single, $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteEffects"),
          $.Single
        ], [])),
    function DrawStringBuilderScaleF (spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth) {
      this.InternalDrawString(
        spriteFont, text.toString(),
        position.X, position.Y,
        color, rotation,
        origin.X, origin.Y,
        scale, scale,
        effects, layerDepth
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawString",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont"), $.String,
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.colorRef(),
          $.Single, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteEffects"),
          $.Single
        ], [])),
    function DrawString (spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth) {
      this.InternalDrawString(
        spriteFont, text,
        position.X, position.Y,
        color, rotation,
        origin.X, origin.Y,
        scale.X, scale.Y,
        effects, layerDepth
      );
    }
  );

  $.Method({Static:false, Public:true }, "DrawStringBuilder",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteFont"), $xnaasms[5].TypeRef("System.Text.StringBuilder"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.colorRef(),
          $.Single, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SpriteEffects"),
          $.Single
        ], [])),
    function DrawStringBuilder (spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth) {
      this.InternalDrawString(
        spriteFont, text.toString(),
        position.X, position.Y,
        color, rotation,
        origin.X, origin.Y,
        scale.X, scale.Y,
        effects, layerDepth
      );
    }
  );

  var blitSinglePixel = function InternalDraw_BlitSinglePixel (
    context, originalImage,
    width, height,
    colorR, colorG, colorB, colorA
  ) {
    colorR = (colorR | 0) / +255;
    colorG = (colorG | 0) / +255;
    colorB = (colorB | 0) / +255;
    colorA = (colorA | 0) / +255;

    var topLeftPixelText = $jsilxna.getImageTopLeftPixel(originalImage);
    var topLeftPixel = topLeftPixelText.split(",");

    var unpremultiplyFactor = +1 / colorA;

    var imageColor = "rgba(" +
      $jsilxna.ClampByte(parseFloat(topLeftPixel[0]) * colorR * unpremultiplyFactor) + ", " +
      $jsilxna.ClampByte(parseFloat(topLeftPixel[1]) * colorG * unpremultiplyFactor) + ", " +
      $jsilxna.ClampByte(parseFloat(topLeftPixel[2]) * colorB * unpremultiplyFactor) + ", " +
      (parseFloat(topLeftPixel[3]) * colorA) +
    ")";

    context.globalAlpha = colorA;
    context.fillStyle = imageColor;
    context.fillRect(
      0, 0, width | 0, height | 0
    );
  };

  var blitChannels = function InternalDraw_BlitChannels (
    context, channels,
    sourceX, sourceY, sourceW, sourceH,
    width, height,
    colorR, colorG, colorB, colorA
  ) {
    colorR = (colorR | 0) / +255;
    colorG = (colorG | 0) / +255;
    colorB = (colorB | 0) / +255;
    colorA = (colorA | 0) / +255;

    sourceX += channels.xOffset | 0;
    sourceY += channels.yOffset | 0;

    var compositeOperation = context.globalCompositeOperation;

    if (compositeOperation !== "lighter") {
      context.globalCompositeOperation = "source-over";
      context.globalAlpha = colorA;
      context.drawImage(
        channels.a, sourceX, sourceY, sourceW, sourceH,
        0, 0, width, height
      );
    }

    context.globalCompositeOperation = "lighter";

    if (colorR > 0) {
      context.globalAlpha = colorR;
      context.drawImage(
        channels.r, sourceX, sourceY, sourceW, sourceH,
        0, 0, width, height
      );
    }

    if (colorG > 0) {
      context.globalAlpha = colorG;
      context.drawImage(
        channels.g, sourceX, sourceY, sourceW, sourceH,
        0, 0, width, height
      );
    }

    if (colorB > 0) {
      context.globalAlpha = colorB;
      context.drawImage(
        channels.b, sourceX, sourceY, sourceW, sourceH,
        0, 0, width, height
      );
    }
  };

  $.RawMethod(false, "InternalDraw$SetupAndExecute",
    function InternalDraw_SetupAndExecute (
      context,
      texture, textureWidth, textureHeight,
      originalImage, image,
      positionX, positionY, width, height,
      scaleX, scaleY,
      sourceX, sourceY, sourceW, sourceH,
      originX, originY,
      rotation, color, effects
    ) {
      if (sourceX < 0) {
        sourceW += sourceX;
        sourceX = 0;
      }
      if (sourceY < 0) {
        sourceH += sourceY;
        sourceY = 0;
      }

      var maxWidth = (textureWidth - sourceX),
        maxHeight = (textureHeight - sourceY);

      if (sourceW > maxWidth)
        sourceW = maxWidth;
      if (sourceH > maxHeight)
        sourceH = maxHeight;

      var isSinglePixel = ((sourceW === 1) && (sourceH === 1) && (sourceX === 0) && (sourceY === 0));
      var isWebGL = this.isWebGL;
      var channels = null;

      var colorA = color.a | 0;
      if (colorA < 1)
        return;

      var colorR = color.r | 0,
        colorG = color.g | 0,
        colorB = color.b | 0;

      if (!isSinglePixel && !isWebGL) {
        // Since the color is premultiplied, any r/g/b value >= alpha is basically white.
        if ((colorR < colorA) || (colorG < colorA) || (colorB < colorA)) {
          channels = $jsilxna.getImageChannels(image, texture.id);
        }
      }

      // Negative width/height cause an exception in Firefox
      if (width < 0) {
        scaleX *= -1;
        width = -width;
      }
      if (height < 0) {
        scaleY *= -1;
        height = -height;
      }

      this.InternalDraw$ApplyTransform(
        context,
        +positionX, +positionY,
        +rotation,
        +scaleX, +scaleY,
        +originX, +originY
      );

      if (effects) {
        var e = effects.value;
        this.InternalDraw$ApplySpriteEffect(context, e, +width, +height);
      }

      if (
        (width > 0) && (height > 0) &&
        (sourceW > 0) && (sourceH > 0)
      ) {
        this.InternalDraw$Execute(
          context,
          isSinglePixel, isWebGL,
          originalImage, image, channels,
          sourceX, sourceY, sourceW, sourceH,
          width, height,
          colorR, colorG, colorB, colorA
        );
      }
    }
  );

  $.RawMethod(false, "InternalDraw$ApplyTransform",
    function InternalDraw_ApplyTransform (
      context,
      positionX, positionY,
      rotation,
      scaleX, scaleY,
      originX, originY
    ) {
      context.translate(+positionX, +positionY);
      context.rotate(+rotation);
      context.scale(+scaleX, +scaleY);
      context.translate(-originX, -originY);
    }
  );

  $.RawMethod(false, "InternalDraw$ApplySpriteEffect",
    function InternalDraw_ApplySpriteEffect (context, e, width, height) {
      if (e & this.flipHorizontally) {
        context.translate(+width, 0);
        context.scale(-1, 1);
      }

      if (e & this.flipVertically) {
        context.translate(0, +height);
        context.scale(1, -1);
      }
    }
  );

  $.RawMethod(false, "InternalDraw$ExecuteDebugDraw",
    function InternalDraw_ExecuteDebugDraw (context, width, height) {
      if ($drawDebugRects) {
        context.fillStyle = "rgba(255, 0, 0, 0.33)";
        context.fillRect(
          0, 0, width, height
        );
      }

      if ($drawDebugBoxes) {
        context.strokeStyle = "rgba(255, 255, 0, 0.66)";
        context.strokeRect(
          0, 0, width, height
        );
      }
    }
  );

  $.RawMethod(false, "InternalDraw$Execute",
    function InternalDraw_Execute (
      context,
      isSinglePixel, isWebGL,
      originalImage, image, channels,
      sourceX, sourceY, sourceW, sourceH,
      width, height,
      colorR, colorG, colorB, colorA
    ) {
      this.InternalDraw$ExecuteDebugDraw(
        context,
        width, height
      );

      if (isSinglePixel) {
        blitSinglePixel(context, originalImage, width, height, colorR, colorG, colorB, colorA);
      } else if (channels !== null) {
        blitChannels(
          context, channels,
          sourceX, sourceY, sourceW, sourceH,
          width, height,
          colorR, colorG, colorB, colorA
        );
      } else if (this.isWebGL) {
        context.drawImage(
          image, sourceX, sourceY, sourceW, sourceH,
          0, 0, width, height,
          colorR / 255, colorG / 255, colorB / 255, colorA / 255
        );
      } else {
        if (colorA < 255)
          context.globalAlpha = colorA / 255;

        this.$canvasDrawImage(
          image, sourceX, sourceY, sourceW, sourceH, 0, 0, width, height
        );
      }
    }
  );

  $.RawMethod(false, "InternalDraw",
    function SpriteBatch_InternalDraw (
      texture, positionX, positionY, width, height,
      sourceX, sourceY, sourceW, sourceH,
      color, rotation,
      originX, originY,
      scaleX, scaleY,
      effects, depth
    ) {
      if (this.defer) {
        this.DeferBlit(
          texture, positionX, positionY, width, height,
          sourceX, sourceY, sourceW, sourceH,
          color, rotation, originX, originY,
          scaleX, scaleY, effects, depth
        );

        return;
      }

      var image = texture.image;
      var originalImage = image;
      var context = this.device.context;
      var textureWidth = texture.get_Width() | 0;
      var textureHeight = texture.get_Height() | 0;

      this.$save();

      this.InternalDraw$SetupAndExecute(
        context,
        texture, textureWidth, textureHeight,
        originalImage, image,
        positionX, positionY, width, height,
        scaleX, scaleY,
        sourceX, sourceY, sourceW, sourceH,
        originX, originY,
        rotation, color, effects
      );

      this.$restore();
    }
  );

  $.RawMethod(false, "InternalDrawString",
  function SpriteBatch_InternalDrawString (
    font, text,
    positionX, positionY,
    color, rotation,
    originX, originY,
    scaleX, scaleY,
    effects, depth
  ) {
    if (text.length <= 0)
      return;

    if (this.defer) {
      this.DeferDrawString(
        font, text,
        positionX, positionY,
        color, rotation,
        originX, originY,
        scaleX, scaleY,
        effects, depth
      );

      return;
    }

    var asmGraphics = $xnaasms.xnaGraphics || $xnaasms.xna;
    var tSpriteFont = asmGraphics.Microsoft.Xna.Framework.Graphics.SpriteFont;

    if (Object.getPrototypeOf(font) === tSpriteFont.prototype) {
      return font.InternalDraw(
        text, this,
        positionX, positionY,
        color, rotation,
        originX, originY,
        scaleX, scaleY,
        effects, depth
      );
    }

    var needRestore = false;

    effects = effects || Microsoft.Xna.Framework.Graphics.SpriteEffects.None;

    if ((effects & Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally) == Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally) {
      if (!needRestore) {
        this.$save();
        needRestore = true;
      }

      this.device.context.scale(-1, 1);
      positionX = -positionX;
    }

    if ((effects & Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipVertically) == Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipVertically) {
      if (!needRestore) {
        this.$save();
        needRestore = true;
      }

      this.device.context.scale(1, -1);
      positionY = -positionY;
    }

    this.device.context.textBaseline = "top";
    this.device.context.textAlign = "start";

    var fontCss = font.toCss(scaleX || 1.0);
    this.device.context.font = fontCss;

    if (this.device.context.font != fontCss) {
      // We failed to set the font onto the context; this may mean that the font failed to load.
      var hasWarned = font.$warnedAboutSetFailure || false;
      if (!hasWarned) {
        font.$warnedAboutSetFailure = true;
        JSIL.Host.warning("Failed to set font '" + font + "' onto canvas context for rendering.");
      }
    }

    this.device.context.fillStyle = color.toCss();

    var lines = text.split("\n");
    for (var i = 0, l = lines.length; i < l; i++) {
      this.device.context.fillText(lines[i], positionX, positionY);
      positionY += font.LineSpacing;
    }

    if (needRestore)
      this.$restore();
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.GraphicsDevice", function ($) {
  $.Method({
    Static: false,
    Public: true
  }, ".ctor", new JSIL.MethodSignature(null, [$.Object], []), function (gdm) {
    var preferredWidth = gdm.get_PreferredBackBufferWidth();
    var preferredHeight = gdm.get_PreferredBackBufferHeight();

    this.originalCanvas = this.canvas = JSIL.Host.getCanvas(preferredWidth, preferredHeight);
    this.renderTarget = null;

    this.originalWidth = this.canvas.actualWidth || this.canvas.width;
    this.originalHeight = this.canvas.actualHeight || this.canvas.height;

    this.originalContext = this.context = $jsilxna.get2DContext(this.canvas, true);

    this.viewport = new Microsoft.Xna.Framework.Graphics.Viewport();
    this.viewport.Width = this.canvas.actualWidth || this.canvas.width;
    this.viewport.Height = this.canvas.actualHeight || this.canvas.height;
    this.blendState = Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;
    this.samplerStates = new Microsoft.Xna.Framework.Graphics.SamplerStateCollection(this, 0, 4);
    this.vertexSamplerStates = new Microsoft.Xna.Framework.Graphics.SamplerStateCollection(this, 0, 4);
    this.textures = new Microsoft.Xna.Framework.Graphics.TextureCollection(this, 0, 4);
    this.vertexTextures = new Microsoft.Xna.Framework.Graphics.TextureCollection(this, 0, 4);
    this.presentationParameters = JSIL.CreateInstanceOfType(
      Microsoft.Xna.Framework.Graphics.PresentationParameters.__Type__,
      "$internalCtor", [this]
    );

    this.displayMode = new $jsilxna.CurrentDisplayMode(this);

    this.$UpdateBlendState();
    this.$UpdateViewport();
  });

  $.Method({Static:false, Public:true }, "get_Viewport",
    (new JSIL.MethodSignature($jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Viewport"), [], [])),
    function get_Viewport () {
      return this.viewport;
    }
  );

  $.Method({Static:false, Public:true }, "set_Viewport",
    (new JSIL.MethodSignature(null, [$jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Viewport")], [])),
    function set_Viewport (value) {
      this.viewport = value.MemberwiseClone();

      this.$UpdateViewport();
    }
  );

  $.Method({Static:false, Public:true }, "get_BlendState",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.BlendState"), [], [])),
    function get_BlendState () {
      return this.blendState;
    }
  );

  $.Method({Static:false, Public:true }, "get_DisplayMode",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DisplayMode"), [], [])),
    function get_DisplayMode () {
      return this.displayMode;
    }
  );

  $.Method({Static:false, Public:true }, "get_PresentationParameters",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.PresentationParameters"), [], [])),
    function get_PresentationParameters () {
      return this.presentationParameters;
    }
  );

  $.Method({Static:false, Public:true }, "get_SamplerStates",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SamplerStateCollection"), [], [])),
    function get_SamplerStates () {
      return this.samplerStates;
    }
  );

  $.Method({Static:false, Public:true }, "get_VertexSamplerStates",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SamplerStateCollection"), [], [])),
    function get_VertexSamplerStates () {
      return this.vertexSamplerStates;
    }
  );

  $.Method({Static:false, Public:true }, "get_Textures",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.TextureCollection"), [], [])),
    function get_Textures () {
      return this.textures;
    }
  );

  $.Method({Static:false, Public:true }, "get_VertexTextures",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.TextureCollection"), [], [])),
    function get_VertexTextures () {
      return this.vertexTextures;
    }
  );

  $.Method({Static:false, Public:true }, "set_BlendState",
    (new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.BlendState")], [])),
    function set_BlendState (value) {
      this.blendState = value;
      this.$UpdateBlendState();
    }
  );

  $.Method({Static:false, Public:true }, "set_DepthStencilState",
    (new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DepthStencilState")], [])),
    function set_DepthStencilState (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "set_RasterizerState",
    (new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.RasterizerState")], [])),
    function set_RasterizerState (value) {
      // FIXME
    }
  );

  $.RawMethod(false, "$UpdateBlendState", function GraphicsDevice_$UpdateBlendState () {
    if (!this.blendState) {
      // XNA 3
      this.context.globalCompositeOperation = "source-over";
    } else if (this.blendState === Microsoft.Xna.Framework.Graphics.BlendState.Opaque) {
      this.context.globalCompositeOperation = "copy";
    } else if (this.blendState === Microsoft.Xna.Framework.Graphics.BlendState.Additive) {
      this.context.globalCompositeOperation = "lighter";
    } else {
      this.context.globalCompositeOperation = "source-over";
    }
  });

  $.RawMethod(false, "$UpdateViewport", function GraphicsDevice_$UpdateViewport () {
    var scaleX = 1.0, scaleY = 1.0;
    var viewport = this.viewport;
    var context = this.context;

    context.setTransform(1, 0, 0, 1, 0, 0);

    if (this.canvas === this.originalCanvas) {
      scaleX *= viewport.get_Width() / this.originalWidth;
      scaleY *= viewport.get_Height() / this.originalHeight;
    } else {
      scaleX *= viewport.get_Width() / this.canvas.width;
      scaleY *= viewport.get_Height() / this.canvas.height;
    }

    context.translate(viewport.get_X(), viewport.get_Y());

    if (this.canvas === this.originalCanvas) {
      if (context.isWebGL) {
        context.viewport(0, 0, this.canvas.width, this.canvas.height);
      } else {
        scaleX *= (this.canvas.width / this.originalWidth);
        scaleY *= (this.canvas.height / this.originalHeight);
      }
    }

    context.scale(scaleX, scaleY);

    if (jsilConfig.disableFiltering)
      context.mozImageSmoothingEnabled = context.webkitImageSmoothingEnabled = false;
  });

  $.RawMethod(false, "$Clear", function GraphicsDevice_$Clear (colorCss) {
    var colorText = null;
    if (typeof (colorCss) === "string")
      colorText = colorCss;
    else
      colorText = "rgba(0, 0, 0, 1)";

    var w = this.canvas.width | 0;
    var h = this.canvas.height | 0;

    this.context.save();
    this.context.setTransform(1, 0, 0, 1, 0, 0);
    this.context.globalCompositeOperation = "source-over";
    this.context.globalAlpha = 1.0;
    this.context.fillStyle = colorText;
    this.context.clearRect(0, 0, w, h);
    this.context.fillRect(0, 0, w, h);
    this.context.restore();
  });

  $.RawMethod(false, "InternalClear", function GraphicsDevice_InternalClear (color) {
    this.$Clear(color.toCss());
  });

  var warnedTypes = {};

  $.RawMethod(false, "InternalDrawUserPrimitives", function GraphicsDevice_InternalDrawUserPrimitives (T, primitiveType, vertices, vertexOffset, primitiveCount) {
    switch (primitiveType) {
    case Microsoft.Xna.Framework.Graphics.PrimitiveType.LineList:
      for (var i = 0; i < primitiveCount; i++) {
        var j = i * 2;
        this.context.lineWidth = 1;
        this.context.strokeStyle = vertices[j].Color.toCss();
        this.context.beginPath();
        this.context.moveTo(vertices[j].Position.X + 0.5, vertices[j].Position.Y + 0.5);
        this.context.lineTo(vertices[j + 1].Position.X + 0.5, vertices[j + 1].Position.Y + 0.5);
        this.context.closePath();
        this.context.stroke();
      }

      break;

    default:
      var ptype = primitiveType.toString();
      if (warnedTypes[ptype])
        return;

      warnedTypes[ptype] = true;
      JSIL.Host.warning("The primitive type " + ptype + " is not implemented.");
      return;
    }
  });

  $.Method({Static:false, Public:true }, "DrawUserPrimitives",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.PrimitiveType"), $jsilcore.TypeRef("System.Array", ["!!0"]),
          $.Int32, $.Int32
        ], ["T"])),
    function DrawUserPrimitives$b1 (T, primitiveType, vertexData, vertexOffset, primitiveCount) {
      return this.InternalDrawUserPrimitives(T, primitiveType, vertexData, vertexOffset, primitiveCount);
    }
  );

  $.Method({Static:false, Public:true }, "DrawUserPrimitives",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.PrimitiveType"), $jsilcore.TypeRef("System.Array", ["!!0"]),
          $.Int32, $.Int32,
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.VertexDeclaration")
        ], ["T"])),
    function DrawUserPrimitives$b1 (T, primitiveType, vertexData, vertexOffset, primitiveCount, vertexDeclaration) {
      return this.InternalDrawUserPrimitives(T, primitiveType, vertexData, vertexOffset, primitiveCount);
    }
  );

  $.Method({Static:false, Public:true }, "GetRenderTargets",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.RenderTargetBinding")]), [], [])),
    function GetRenderTargets () {
      var tRenderTargetBinding = getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.RenderTargetBinding").get();

      if (this.renderTarget === null)
        return [];
      else
        return [ new tRenderTargetBinding(this.renderTarget) ];
    }
  );

  $.Method({Static:false, Public:true }, "SetRenderTarget",
    (new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.RenderTarget2D")], [])),
    function SetRenderTarget (renderTarget) {
      if (this.renderTarget === renderTarget)
        return;

      var oldRenderTarget = this.renderTarget;
      this.renderTarget = renderTarget;

      if (renderTarget !== null) {
        this.canvas = renderTarget.canvas;
        this.context = renderTarget.context;
      } else {
        this.canvas = this.originalCanvas;
        this.context = this.originalContext;
      }

      this.context.setTransform(1, 0, 0, 1, 0, 0);
      this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);

      this.viewport.X = 0;
      this.viewport.Y = 0;

      if (this.canvas === this.originalCanvas) {
        this.viewport.Width = this.originalWidth;
        this.viewport.Height = this.originalHeight;
      } else {
        this.viewport.Width = this.canvas.width;
        this.viewport.Height = this.canvas.height;
      }

      this.$UpdateBlendState();
      this.$UpdateViewport();

      if (oldRenderTarget !== null)
        oldRenderTarget.$ResynthesizeImage();
    }
  );
});


JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.BlendState", function ($) {
  $.RawMethod(true, ".cctor2", function () {
      Microsoft.Xna.Framework.Graphics.BlendState.Opaque = new Microsoft.Xna.Framework.Graphics.BlendState();
      Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend = new Microsoft.Xna.Framework.Graphics.BlendState();
      Microsoft.Xna.Framework.Graphics.BlendState.Additive = new Microsoft.Xna.Framework.Graphics.BlendState();
      Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied = new Microsoft.Xna.Framework.Graphics.BlendState();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.PresentationParameters", function ($) {
  $.RawMethod(false, "__CopyMembers__", function (source, target) {
    target._device = source._device;
  });

  $.RawMethod(false, "$internalCtor", function (graphicsDevice) {
    this._device = graphicsDevice;
  });

  $.Method({Static:false, Public:true }, "get_BackBufferFormat",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SurfaceFormat"), [], [])),
    function get_BackBufferFormat () {
      return Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color;
    }
  );

  $.Method({Static:false, Public:true }, "get_BackBufferHeight",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_BackBufferHeight () {
      return this._device.originalHeight;
    }
  );

  $.Method({Static:false, Public:true }, "get_BackBufferWidth",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_BackBufferWidth () {
      return this._device.originalWidth;
    }
  );

  $.Method({Static:false, Public:true }, "get_DepthStencilFormat",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DepthFormat"), [], [])),
    function get_DepthStencilFormat () {
      return Microsoft.Xna.Framework.Graphics.DepthFormat.None;
    }
  );

  $.Method({Static:false, Public:true }, "get_MultiSampleCount",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_MultiSampleCount () {
      return 0;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.TextureCollection", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $.Int32,
          $.Int32
        ], [])),
    function _ctor (parent, textureOffset, maxTextures) {
      this.textures = new Array(maxTextures);

      for (var i = 0; i < maxTextures; i++)
        this.textures[i] = null;
    }
  );

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Texture"), [$.Int32], [])),
    function get_Item (index) {
      return this.textures[index];
    }
  );

  $.Method({Static:false, Public:true }, "set_Item",
    (new JSIL.MethodSignature(null, [$.Int32, getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Texture")], [])),
    function set_Item (index, value) {
      this.textures[index] = value;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.SamplerStateCollection", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $.Int32,
          $.Int32
        ], [])),
    function _ctor (pParent, samplerOffset, maxSamplers) {
      // FIXME
      this.parent = pParent;
      this.states = new Array(maxSamplers);

      var tState = Microsoft.Xna.Framework.Graphics.SamplerState.__Type__;

      for (var i = 0; i < maxSamplers; i++) {
        this.states = JSIL.CreateInstanceOfType(tState, null);
      }
    }
  );

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SamplerState"), [$.Int32], [])),
    function get_Item (index) {
      return this.states[index];
    }
  );

  $.Method({Static:false, Public:true }, "set_Item",
    (new JSIL.MethodSignature(null, [$.Int32, getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SamplerState")], [])),
    function set_Item (index, value) {
      this.states[index] = value;

      var enableSmoothing = true;
      if (value) {
        enableSmoothing = value.get_Filter() != Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
      }

      if (jsilConfig.disableFiltering)
        enableSmoothing = false;

      this.parent.context.mozImageSmoothingEnabled = this.parent.context.webkitImageSmoothingEnabled = enableSmoothing;
    }
  );

});

$jsilxna.CachedText = function (canvas, id) {
  this.image = canvas;
  this.id = id;
  this.width = this.image.width | 0;
  this.height = this.image.height | 0;
  this.sizeBytes = canvas.sizeBytes = (canvas.width * canvas.height * 4) | 0;
};

$jsilxna.CachedText.prototype = JSIL.CreatePrototypeObject(null);

$jsilxna.CachedText.prototype.get_Width = function () {
  return this.width;
};

$jsilxna.CachedText.prototype.get_Height = function () {
  return this.height;
};

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.SpriteFont", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.Texture2D"), $xnaasms[5].TypeRef("System.Collections.Generic.List`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]),
          $xnaasms[5].TypeRef("System.Collections.Generic.List`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]), $xnaasms[5].TypeRef("System.Collections.Generic.List`1", [$.Char]),
          $.Int32, $.Single,
          $xnaasms[5].TypeRef("System.Collections.Generic.List`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3")]), $xnaasms[5].TypeRef("System.Nullable`1", [$.Char])
        ], [])),
    function _ctor (texture, glyphs, cropping, charMap, lineSpacing, spacing, kerning, defaultCharacter) {
      this.textureValue = texture;
      this.glyphData = glyphs;
      this.croppingData = cropping;
      this.characterMap = charMap;
      this.lineSpacing = lineSpacing;
      this.spacing = spacing;
      this.kerning = kerning;
      this.defaultCharacter = defaultCharacter;
      this.characters = this.characterMap.AsReadOnly();
      this.charToIndex = {};

      for (var i = 0; i < charMap.Count; i++) {
        var ch = charMap.get_Item(i);
        this.charToIndex[ch.charCodeAt(0)] = i;
      }
    }
  );

  $.Method({Static:false, Public:true }, "get_Characters",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.Collections.ObjectModel.ReadOnlyCollection`1", [$.Char]), [], [])),
    function get_Characters () {
      return this.characters;
    }
  );

  $.Method({Static:false, Public:true }, "get_DefaultCharacter",
    (new JSIL.MethodSignature($xnaasms[5].TypeRef("System.Nullable`1", [$.Char]), [], [])),
    function get_DefaultCharacter () {
      return this.defaultCharacter;
    }
  );

  $.Method({Static:false, Public:true }, "get_LineSpacing",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_LineSpacing () {
      return this.lineSpacing;
    }
  );

  $.Method({Static:false, Public:true }, "set_LineSpacing",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function set_LineSpacing (value) {
      this.lineSpacing = value;
    }
  );

  $.Method({Static:false, Public:true }, "get_Spacing",
    (new JSIL.MethodSignature($.Single, [], [])),
    function get_Spacing () {
      return this.spacing;
    }
  );

  $.Method({Static:false, Public:true }, "set_Spacing",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function set_Spacing (value) {
      return this.spacing = value;
    }
  );

  $.Method({Static:false, Public:false}, "GetIndexForCharacter",
    (new JSIL.MethodSignature($.Int32, [$.Char], [])),
    function GetIndexForCharacter (character) {
      var result = this.charToIndex[character.charCodeAt(0)];

      if ((typeof (result) === "undefined") && (this.defaultCharacter !== null))
        result = this.charToIndex[this.defaultCharacter.charCodeAt(0)];

      if (typeof (result) === "undefined")
        result = -1;

      return result;
    }
  );

  $.RawMethod(false, "InternalWalkString",
    function InternalWalkString (text, characterCallback) {
      var positionX = 0, positionY = 0;
      var lineIndex = 0;

      for (var i = 0, l = text.length; i < l; i++) {
        var ch = text[i];

        var lineBreak = false;
        if (ch === "\r") {
          if (text[i + 1] === "\n")
            i += 1;

          lineBreak = true;
        } else if (ch === "\n") {
          lineBreak = true;
        }

        if (lineBreak) {
          positionX = 0;
          positionY += this.lineSpacing;
          lineIndex += 1;
        }

        positionX += this.spacing;

        var charIndex = this.GetIndexForCharacter(ch);
        if (charIndex < 0)
          continue;

        var kerning = this.kerning.get_Item(charIndex);
        var beforeGlyph = kerning.X;
        var glyphWidth = kerning.Y;
        var afterGlyph = kerning.Z;

        positionX += beforeGlyph;

        var glyphRect = this.glyphData.get_Item(charIndex);
        var cropRect = this.croppingData.get_Item(charIndex);

        characterCallback(
          ch, glyphRect,
          positionX, positionY,
          cropRect.X, cropRect.Y,
          lineIndex
        );

        positionX += glyphWidth;
        positionX += afterGlyph;
      }
    }
  );

  $.Method({Static:false, Public:true }, "MeasureString",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [$.String], [])),
    function MeasureString (text) {
      return this.InternalMeasure(text);
    }
  );

  $.Method({Static:false, Public:true }, "MeasureString",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [$xnaasms[5].TypeRef("System.Text.StringBuilder")], [])),
    function MeasureString (text) {
      return this.InternalMeasure(stringBuilder.toString());
    }
  );

  $.RawMethod(false, "InternalDrawCachedText",
    function InternalDrawCachedText (
      text, spriteBatch, textblockPositionX, textblockPositionY,
      color, rotation,
      originX, originY,
      scaleX, scaleY,
      spriteEffects, layerDepth
    ) {
      var cacheKey = this.textureValue.id + ":" + text;

      var cachedTexture = $jsilxna.textCache.getItem(cacheKey);

      var xPad = 2;
      var yPad = 8;

      if (!cachedTexture) {
        var measured = this.InternalMeasure(text);

        var asmGraphics = $xnaasms.xnaGraphics || $xnaasms.xna;
        var tSpriteBatch = asmGraphics.Microsoft.Xna.Framework.Graphics.SpriteBatch.__Type__;

        var tColor;
        if (JSIL.GetAssembly("Microsoft.Xna.Framework.Graphics", true))
          tColor = $xnaasms.xna.Microsoft.Xna.Framework.Color;
        else
          tColor = $xnaasms.xna.Microsoft.Xna.Framework.Graphics.Color;

        var tempCanvas = JSIL.Host.createCanvas(
          Math.ceil(measured.X + xPad + xPad),
          Math.ceil(measured.Y + yPad + yPad)
        );
        var tempSpriteBatch = JSIL.CreateInstanceOfType(tSpriteBatch, "$cloneExisting", [spriteBatch]);
        // Force the isWebGL flag to false since the temporary canvas isn't using webgl-2d
        tempSpriteBatch.isWebGL = false;

        // FIXME: Terrible hack
        tempSpriteBatch.device = {
          context: tempCanvas.getContext("2d")
        };

        this.InternalDraw(
          text, tempSpriteBatch, xPad, yPad,
          tColor.White, 0,
          0, 0, 1, 1,
          null, 0,
          true
        );

        cachedTexture = new $jsilxna.CachedText(
          tempCanvas, cacheKey
        );

        $jsilxna.textCache.setItem(cacheKey, cachedTexture);
      }

      var cachedTextureWidth = cachedTexture.width;
      var cachedTextureHeight = cachedTexture.height;

      spriteBatch.InternalDraw(
        cachedTexture, textblockPositionX, textblockPositionY, cachedTextureWidth, cachedTextureHeight,
        0, 0, cachedTextureWidth, cachedTextureHeight,
        color, rotation,
        originX + xPad, originY + yPad,
        scaleX, scaleY,
        spriteEffects, layerDepth
      );

      return;
    }
  );

  var measureResult = [0, 0, 0];
  var measureCallback = function (character, characterRect, x, y, xOffset, yOffset, lineIndex) {
    var x2 = x + characterRect.Width + xOffset;
    var y2 = y + characterRect.Height + yOffset;
    measureResult[0] = Math.max(measureResult[0], x2);

    if (character !== " ") {
      if (measureResult[2] !== lineIndex) {
        measureResult[1] = y2 - y;
      } else {
        measureResult[1] = Math.max(measureResult[1], y2 - y);
      }
    }

    measureResult[2] = lineIndex;
  };

  $.RawMethod(false, "InternalMeasure",
    function InternalMeasure (text) {
      measureResult[0] = 0;
      measureResult[1] = 0;
      measureResult[2] = 0;

      this.InternalWalkString(text, measureCallback);

      var lineHeights = (measureResult[2] * this.lineSpacing) + measureResult[1];

      return new Microsoft.Xna.Framework.Vector2(measureResult[0], lineHeights);
    }
  );

  var drawState = {
    thisReference: null,
    spriteBatch: null,
    offsetX: 0,
    offsetY: 0,
    color: null,
    rotation: 0,
    scaleX: 1,
    scaleY: 1,
    spriteEffects: null,
    layerDepth: 0
  };

  var drawCallback = function (character, characterRect, x, y, xOffset, yOffset, lineIndex) {
    if (character === " ")
      return;

    var texture = drawState.thisReference.textureValue;
    var spriteBatch = drawState.spriteBatch;

    var scaleX = drawState.scaleX, scaleY = drawState.scaleY;
    var drawX = drawState.offsetX + (x * scaleX) + (xOffset * scaleX);
    var drawY = drawState.offsetY + (y * scaleY) + (yOffset * scaleY);

    spriteBatch.InternalDraw(
      texture,
      drawX, drawY,
      characterRect.Width, characterRect.Height,
      characterRect.X, characterRect.Y,
      characterRect.Width, characterRect.Height,
      drawState.color, drawState.rotation,
      0, 0, scaleX, scaleY,
      drawState.spriteEffects, drawState.layerDepth
    );
  };

  $.RawMethod(false, "InternalDraw",
    function SpriteFont_InternalDraw (
      text, spriteBatch, textblockPositionX, textblockPositionY,
      color, rotation,
      originX, originY,
      scaleX, scaleY,
      spriteEffects, layerDepth,
      forCache
    ) {

      // Draw calls are really expensive, so cache entire strings as single textures.

      if ($useTextCaching && $textCachingSupported && (forCache !== true)) {
        this.InternalDrawCachedText(
          text, spriteBatch, textblockPositionX, textblockPositionY,
          color, rotation,
          originX, originY,
          scaleX, scaleY,
          spriteEffects, layerDepth
        );

        return;
      }

      textblockPositionX -= (originX * scaleX);
      textblockPositionY -= (originY * scaleY);

      drawState.thisReference = this;
      drawState.spriteBatch = spriteBatch;
      drawState.offsetX = textblockPositionX;
      drawState.offsetY = textblockPositionY;
      drawState.color = color;
      drawState.scaleX = scaleX;
      drawState.scaleY = scaleY;

      // FIXME: Should apply to entire string
      drawState.spriteEffects = spriteEffects;

      // FIXME: Should apply to entire string
      drawState.rotation = rotation;

      drawState.layerDepth = layerDepth;

      this.InternalWalkString(text, drawCallback);
  });
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.Texture2D", function ($) {
  $.RawMethod(false, "$internalCtor", function (graphicsDevice, width, height, mipMap, format) {
    this._parent = graphicsDevice;
    this.width = width | 0;
    this.height = height | 0;
    this.mipMap = mipMap;
    this.format = format;
    this.isDisposed = false;
    this.id = String(++$jsilxna.nextImageId);

    if (typeof ($jsilxna.ImageFormats[format.name]) === "undefined")
      throw new System.NotImplementedException("The pixel format '" + format.name + "' is not supported.");

    if (typeof (document) !== "undefined") {
      this.image = document.createElement("img");

      if (!this.image.id)
        this.image.id = this.id;

      var textures = document.getElementById("textures");
      if (textures)
        textures.appendChild(this.image);
    } else {
      // Headless mode
      this.image = JSIL.CreateDictionaryObject(null);
      this.image.id = this.id;
    }
  });

  $.RawMethod(false, "$fromUri", function (graphicsDevice, uri) {
    this._parent = graphicsDevice;
    this.mipMap = false;
    this.format = Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color;
    this.isDisposed = false;
    this.id = String(++$jsilxna.nextImageId);

    this.image = document.createElement("img");
    var self = this;
    JSIL.Browser.RegisterOneShotEventListener(
      this.image, "load", true,
      function Texture2D_FromURI_RecordNaturalSize () {
        self.width = self.image.naturalWidth | 0;
        self.height = self.image.naturalHeight | 0;
      }
    );
    this.image.src = uri;

    if (!this.image.id)
      this.image.id = this.id;

    self.width = self.image.naturalWidth | 0;
    self.height = self.image.naturalHeight | 0;
  });

  $.RawMethod(false, "$fromImage", function (graphicsDevice, image) {
    this._parent = graphicsDevice;
    this.mipMap = false;
    this.format = Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color;
    this.isDisposed = false;
    this.id = String(++$jsilxna.nextImageId);

    this.image = image;

    if (!this.image.id)
      this.image.id = this.id;

    this.width = image.naturalWidth | 0;
    this.height = image.naturalHeight | 0;
  });

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $.Int32,
          $.Int32
        ], [])),
    function _ctor (graphicsDevice, width, height) {
      this.$internalCtor(graphicsDevice, width, height, false, Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color);
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $.Int32,
          $.Int32, $.Boolean,
          $jsilxna.graphicsRef("Microsoft.Xna.Framework.Graphics.SurfaceFormat")
        ], [])),
    function _ctor (graphicsDevice, width, height, mipMap, format) {
      this.$internalCtor(graphicsDevice, width, height, mipMap, format);
    }
  );

  var fromStreamImpl = function (graphicsDevice, stream) {
    var streamPosition = stream.get_Position().ToInt32();
    var streamLength = stream.get_Length().ToInt32();
    var url = stream.$GetURI();

    // The stream is associated with a file at a given URI, so we can cheat
    //  and construct an <img> from that URI.
    if (url && (streamPosition === 0)) {
      return JSIL.CreateInstanceOfType(
        Microsoft.Xna.Framework.Graphics.Texture2D.__Type__,
        "$fromUri", [graphicsDevice, url]
      );
    }

    // Gotta do this the hard way. HTML5 sucks. :(
    // Read the rest of the stream into a buffer.
    var bytesToRead = streamLength - streamPosition;
    var bytes = JSIL.Array.New($jsilcore.System.Byte, bytesToRead);
    var bytesRead = stream.Read(bytes, 0, bytesToRead);
    if (bytesRead < bytesToRead)
      throw new Error("Unable to read entire stream");

    // Rewind the stream
    stream.set_Position($jsilcore.System.Int64.FromInt32(streamPosition));

    var headerMatches = function (header) {
      for (var i = 0, l = Math.min(header.length, bytes.length); i < l; i++) {
        if (bytes[i] !== header[i])
          return false;
      }

      return true;
    };

    // Detect the image format
    var mimeType;
    var pngHeader = [137, 80, 78, 71, 13, 10, 26, 10];
    var jpegHeader = [0xFF, 0xD8];

    if (headerMatches(pngHeader))
      mimeType = "image/png";
    else if (headerMatches(jpegHeader))
      mimeType = "image/jpeg";
    else
      throw new Error("Unable to detect image file type");

    // FIXME: If this fails, provide data URL fallback?
    url = JSIL.GetObjectURLForBytes(bytes, mimeType);

    // FIXME: Memory leak if we never release the URL.
    return JSIL.CreateInstanceOfType(
      Microsoft.Xna.Framework.Graphics.Texture2D.__Type__,
      "$fromUri", [graphicsDevice, url]
    );
  };

  $.Method({Static:true , Public:true }, "FromStream",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Texture2D"), [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $xnaasms[5].TypeRef("System.IO.Stream"),
          $.Int32, $.Int32,
          $.Boolean
        ], [])),
    function FromStream (graphicsDevice, stream, width, height, zoom) {
      // FIXME: width, height, zoom
      return fromStreamImpl(graphicsDevice, stream);
    }
  );

  $.Method({Static:true , Public:true }, "FromStream",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Texture2D"), [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $xnaasms[5].TypeRef("System.IO.Stream")], [])),
    function FromStream (graphicsDevice, stream) {
      return fromStreamImpl(graphicsDevice, stream);
    }
  );

  $.Method({Static:false, Public:true }, "get_Height",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Height () {
      return this.height;
    }
  );

  $.Method({Static:false, Public:true }, "get_Width",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Width () {
      return this.width;
    }
  );

  $.Method({Static:false, Public:true }, "get_Bounds",
    (new JSIL.MethodSignature($xnaasms.xna.TypeRef("Microsoft.Xna.Framework.Rectangle"), [], [])),
    function get_Bounds () {
      if (!this._bounds)
        this._bounds = new Microsoft.Xna.Framework.Rectangle(0, 0, this.width | 0, this.height | 0);
      else
        this._bounds._ctor(0, 0, this.width | 0, this.height | 0);

      return this._bounds;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsContentLost",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsContentLost () {
      return false;
    }
  );

  $.Method({Static:false, Public:true }, "SetData",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", ["!!0"])], ["T"])),
    function SetData$b1 (T, data) {
      this.$setDataInternal(T, null, data, 0, data.length);
    }
  );

  $.Method({Static:false, Public:true }, "SetData",
    (new JSIL.MethodSignature(null, [
          $.Int32, $xnaasms[5].TypeRef("System.Nullable`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]),
          $jsilcore.TypeRef("System.Array", ["!!0"]), $.Int32,
          $.Int32
        ], ["T"])),
    function SetData$b1 (T, level, rect, data, startIndex, elementCount) {
      if (level !== 0)
        return;

      this.$setDataInternal(T, rect, data, startIndex, elementCount);
    }
  );

  $.Method({Static:false, Public:true }, "GetData",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", ["!!0"])], ["T"])),
    function GetData$b1 (T, data) {
      this.$getDataInternal(T, null, data, 0, data.length);
    }
  );

  $.Method({Static:false, Public:true }, "GetData",
    (new JSIL.MethodSignature(null, [
          $jsilcore.TypeRef("System.Array", ["!!0"]), $.Int32,
          $.Int32
        ], ["T"])),
    function GetData$b1 (T, data, startIndex, elementCount) {
      this.$getDataInternal(T, null, data, startIndex, elementCount);
    }
  );

  $.Method({Static:false, Public:true }, "GetData",
    (new JSIL.MethodSignature(null, [
          $.Int32, $xnaasms[5].TypeRef("System.Nullable`1", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Rectangle")]),
          $jsilcore.TypeRef("System.Array", ["!!0"]), $.Int32,
          $.Int32
        ], ["T"])),
    function GetData$b1 (T, level, rect, data, startIndex, elementCount) {
      if (level !== 0)
        throw new System.NotImplementedException("Mip level must be 0");

      this.$getDataInternal(T, rect, data, startIndex, elementCount);
    }
  );

  $.RawMethod(false, "$makeMutable", function () {
    if (this.image && this.image.getContext)
      return;

    var oldImage = this.image;
    this.image = JSIL.Host.createCanvas(this.width, this.height);

    // Firefox's canvas implementation is incredibly fragile.
    if (
      oldImage &&
      oldImage.src &&
      (oldImage.src.trim().length > 0)
    ) {
      var ctx = $jsilxna.get2DContext(this.image, false);
      ctx.globalCompositeOperation = "copy";
      ctx.drawImage(oldImage, 0, 0);
    }
  });

  var fastArrayCopy = function (destArray, destOffset, sourceArray, sourceOffset, count) {
    for (var i = 0, d = destOffset, s = sourceOffset; i < count; i++, d++, s++)
      destArray[d] = sourceArray[s];
  };

  $.RawMethod(false, "$getDataInternal", function $getDataInternal (T, rect, data, startIndex, elementCount) {
    this.$makeMutable();

    var ctx = this.cached2DContext;
    if (!ctx)
      ctx = this.cached2DContext = $jsilxna.get2DContext(this.image, false);

    var imageData;

    if (rect) {
      imageData = ctx.getImageData(rect.X, rect.Y, rect.Width, rect.Height);
    } else {
      imageData = ctx.getImageData(0, 0, this.width, this.height);
    }

    // FIXME: Need to repremultiply the image bytes... yuck.

    switch (T.__FullName__) {
      case "System.Byte":
        var target = JSIL.IsPackedArray(data) ? data.bytes : data;
        fastArrayCopy(target, startIndex, imageData.data, 0, elementCount);
        break;

      case "Microsoft.Xna.Framework.Color":
      case "Microsoft.Xna.Framework.Graphics.Color":
        $jsilxna.PackColorsFromColorBytesRGBA(data, startIndex, imageData.data, 0, elementCount, true);
        break;

      default:
        throw new System.Exception("Pixel format '" + T.__FullName__ + "' not implemented");
    }
  });

  $.RawMethod(false, "$setDataInternal", function $setDataInternal (T, rect, data, startIndex, elementCount) {
    var bytes = null;

    switch (T.__FullName__) {
      case "System.Byte":
        if (JSIL.IsPackedArray(data)) {
          bytes = data.bytes;
        } else {
          bytes = data;
        }

        break;

      case "Microsoft.Xna.Framework.Color":
      case "Microsoft.Xna.Framework.Graphics.Color":
        bytes = $jsilxna.UnpackColorsToColorBytesRGBA(data, startIndex, elementCount);
        startIndex = 0;
        elementCount = bytes.length;
        break;

      default:
        throw new System.Exception("Pixel format '" + T.__FullName__ + "' not implemented");
    }

    this.$makeMutable();

    var shouldUnpremultiply = true;
    var width = this.width, height = this.height;
    if (rect) {
      width = rect.Width;
      height = rect.Height;
    }
    var imageData = this.$makeImageDataForBytes(
      width, height,
      bytes, startIndex, elementCount,
      shouldUnpremultiply, false
    );

    var ctx = this.cached2DContext;
    if (!ctx)
      ctx = this.cached2DContext = $jsilxna.get2DContext(this.image, false);

    ctx.globalCompositeOperation = "copy";

    if (rect)
      ctx.putImageData(imageData, rect.X, rect.Y);
    else
      ctx.putImageData(imageData, 0, 0);

    /*
    var trace = false;
    if (trace) {
      var traceCanvas = document.createElement("canvas");
      traceCanvas.width = width;
      traceCanvas.height = height;
      var traceCtx = traceCanvas.getContext("2d");
      traceCtx.globalCompositeOperation = "copy";
      traceCtx.putImageData(imageData, 0, 0);

      var traceContainer = document.getElementById("traceContainer");
      if (!traceContainer) {
        traceContainer = document.createElement("div");
        traceContainer.id = "traceContainer";
        traceContainer.style.cssText = "width: 1920px; overflow: scroll;position:  absolute;top:  0px;top:  0px;left:  0px;background-color: rgb(32,96,128);height: 1080px;";

        document.getElementsByTagName("body")[0].appendChild(traceContainer);
      }

      if ((!this.traced) && ((width != this.width) || (height != this.height))) {
        this.traced = true;
        traceContainer.appendChild(this.image);
      }

      traceContainer.appendChild(traceCanvas);
    }
    */
  });

  var getScratchImageData = function getScratchImageData (ctx, width, height) {
    var result = ctx.$scratch;

    // FIXME: This is sort of a leak. Maybe free this after ~1000ms if not reused?
    if (!result || (result.width !== width) || (result.height !== height)) {
      result = ctx.$scratch = ctx.createImageData(width, height);
    }

    return result;
  };

  var doUnpremultiply = function (dataBytes, bytes, startIndex, elementCount) {
    var pixelCount = (elementCount / 4) | 0;
    for (var i = 0; i < pixelCount; i++) {
      var p = (i * 4) | 0;
      var p1 = (p + 1) | 0;
      var p2 = (p + 2) | 0;
      var p3 = (p + 3) | 0;

      var a = bytes[p3] | 0;

      if (a <= 0) {
        continue;
      } else if (a > 254) {
        dataBytes[p] = bytes[p];
        dataBytes[p1] = bytes[p1];
        dataBytes[p2] = bytes[p2];
        dataBytes[p3] = a;
      } else {
        var m = 255 / a;

        dataBytes[p] = (bytes[p] * m) | 0;
        dataBytes[p1] = (bytes[p1] * m) | 0;
        dataBytes[p2] = (bytes[p2] * m) | 0;
        dataBytes[p3] = a;
      }
    }
  };

  $.RawMethod(false, "$makeImageDataForBytes", function $makeImageDataForBytes (
    width, height,
    bytes, startIndex, elementCount,
    unpremultiply, swapRedAndBlue
  ) {
    var ctx = this.cached2DContext;
    if (!ctx)
      ctx = this.cached2DContext = $jsilxna.get2DContext(this.image, false);

    var decoder = $jsilxna.ImageFormats[this.format.name];
    if (decoder !== null) {
      bytes = decoder(width, height, bytes, startIndex, elementCount, swapRedAndBlue);
      startIndex = 0;
      elementCount = bytes.length;
    }

    var imageData = getScratchImageData(ctx, width, height);
    var dataBytes = imageData.data;

    // XNA texture colors are premultiplied, but canvas pixels aren't, so we need to try
    //  to reverse the premultiplication.
    if (unpremultiply) {
      doUnpremultiply(dataBytes, bytes, startIndex, elementCount);
    } else {
      fastArrayCopy(dataBytes, 0, bytes, startIndex, elementCount);
    }

    return imageData;
  });

  $.RawMethod(false, "$saveToStream", function saveToStream (stream, width, height, mimeType) {
    var temporaryCanvas = JSIL.Host.createCanvas(width, height);

    var temporaryCtx = temporaryCanvas.getContext("2d");
    temporaryCtx.drawImage(this.image, 0, 0, width, height);

    var dataUrl = temporaryCanvas.toDataURL(mimeType);
    var parsed = JSIL.ParseDataURL(dataUrl);
    if (parsed[0].toLowerCase() !== mimeType.toLowerCase())
      throw new Error("Your browser cannot save images in the format '" + mimeType + "'; got back '" + parsed[0] + "'!");

    var bytes = parsed[1];
    stream.Write(bytes, 0, bytes.length);
  });

  $.Method({Static:false, Public:true }, "SaveAsJpeg",
    (new JSIL.MethodSignature(null, [
          $jsilcore.TypeRef("System.IO.Stream"), $.Int32,
          $.Int32
        ], [])),
    function SaveAsJpeg (stream, width, height) {
      this.$saveToStream(stream, width, height, "image/jpeg");
    }
  );

  $.Method({Static:false, Public:true }, "SaveAsPng",
    (new JSIL.MethodSignature(null, [
          $jsilcore.TypeRef("System.IO.Stream"), $.Int32,
          $.Int32
        ], [])),
    function SaveAsPng (stream, width, height) {
      this.$saveToStream(stream, width, height, "image/png");
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, "Dispose", JSIL.MethodSignature.Void, function () {
  });
});

$jsilxna.renderTargetTotalBytes = 0;

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.RenderTarget2D", function ($) {
  $.RawMethod(false, "$internalCtor", function (graphicsDevice, width, height, mipMap, format) {
    this._parent = graphicsDevice;
    this.width = width | 0;
    this.height = height | 0;
    this.mipMap = mipMap;
    this.format = format;
    this.isDisposed = false;
    this.id = String(++$jsilxna.nextImageId);

    this.image = this.canvas = JSIL.Host.createCanvas(width, height);
    this.canvas.naturalWidth = width | 0;
    this.canvas.naturalHeight = height | 0;

    if (!this.image.id)
      this.image.id = this.id;

    // Can't use WebGL here since it'll disable the ability to copy from the RT to the framebuffer.
    this.context = $jsilxna.get2DContext(this.canvas, false);

    $jsilxna.renderTargetTotalBytes += (this.width * this.height * 4) | 0;
  });

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $.Int32,
          $.Int32, $.Boolean,
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SurfaceFormat"), getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DepthFormat"),
          $.Int32, getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.RenderTargetUsage")
        ], [])),
    function _ctor (graphicsDevice, width, height, mipMap, colorFormat, preferredDepthFormat, preferredMultiSampleCount, usage) {
      this.$internalCtor(graphicsDevice, width, height, mipMap, colorFormat);
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $.Int32,
          $.Int32, $.Boolean,
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.SurfaceFormat"), getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.DepthFormat")
        ], [])),
    function _ctor (graphicsDevice, width, height, mipMap, colorFormat, preferredDepthFormat) {
      this.$internalCtor(graphicsDevice, width, height, mipMap, colorFormat);
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), $.Int32,
          $.Int32
        ], [])),
    function _ctor (graphicsDevice, width, height) {
      this.$internalCtor(graphicsDevice, width, height, false, Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color);
    }
  );

  $.Method({Static:false, Public:true }, "get_IsContentLost",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsContentLost () {
      return false;
    }
  );

  $.Method({
    Static: false,
    Public: true
  }, "SetData", new JSIL.MethodSignature(null, [], ["T"]), function (T, data) {
    throw new System.NotImplementedException();
  });
  $.Method({
    Static: false,
    Public: true
  }, "SetData", JSIL.MethodSignature.Void, function (T, level, rect, data, startIndex, elementCount) {
    throw new System.NotImplementedException();
  });
  $.Method({
    Static: false,
    Public: true
  }, "$ResynthesizeImage", JSIL.MethodSignature.Void, function () {
    this.image.isDirty = true;
  });
  $.Method({
    Static: false,
    Public: true
  }, "Dispose", JSIL.MethodSignature.Void, function () {
    if (!this.canvas)
      return;

    $jsilxna.renderTargetTotalBytes -= (this.width * this.height * 4) | 0;

    this.canvas = null;
    this.context = null;
  });
});

// Based on Benjamin Dobell's DXTC decompressors.
// http://www.glassechidna.com.au
$jsilxna.Unpack565 = function (sourceBuffer, sourceOffset) {
  var color565 = (sourceBuffer[sourceOffset + 1] << 8) + (sourceBuffer[sourceOffset]);
  if (color565 === 0) return [0, 0, 0];
  else if (color565 === 65535) return [255, 255, 255];

  var result = [];

  var temp = (color565 >> 11) * 255 + 16;
  result[0] = Math.floor((temp / 32 + temp) / 32);
  temp = ((color565 & 0x07E0) >> 5) * 255 + 32;
  result[1] = Math.floor((temp / 64 + temp) / 64);
  temp = (color565 & 0x001F) * 255 + 16;
  result[2] = Math.floor((temp / 32 + temp) / 32);

  return result;
};

$jsilxna.DecompressBlockBC12 = function (source, sourceOffset, writePixel, alphaSource) {
  var color0 = $jsilxna.Unpack565(source, sourceOffset);
  var color1 = $jsilxna.Unpack565(source, sourceOffset + 2);

  var r0 = color0[0],
    g0 = color0[1],
    b0 = color0[2];
  var r1 = color1[0],
    g1 = color1[1],
    b1 = color1[2];

  var bc2Mode = typeof (alphaSource) === "function";
  var readPosition = sourceOffset + 4;
  var finalColor;

  for (var y = 0; y < 4; y++) {
    var currentByte = source[readPosition];

    for (var x = 0; x < 4; x++) {
      var positionCode = (currentByte >> (2 * x)) & 0x03;
      var alpha = 255;
      if (bc2Mode) alpha = alphaSource(x, y);

      if (bc2Mode || (color0 > color1)) {
        // BC2 block mode or a BC1 block where (color0 > color1)
        switch (positionCode) {
        case 0:
          finalColor = [r0, g0, b0, alpha];
          break;
        case 1:
          finalColor = [r1, g1, b1, alpha];
          break;
        case 2:
          finalColor = [(2 * r0 + r1) / 3, (2 * g0 + g1) / 3, (2 * b0 + b1) / 3, alpha];
          break;
        case 3:
          finalColor = [(r0 + 2 * r1) / 3, (g0 + 2 * g1) / 3, (b0 + 2 * b1) / 3, alpha];
          break;
        }
      } else {
        // BC1 block mode
        switch (positionCode) {
        case 0:
          finalColor = [r0, g0, b0, 255];
          break;
        case 1:
          finalColor = [r1, g1, b1, 255];
          break;
        case 2:
          finalColor = [(r0 + r1) / 2, (g0 + g1) / 2, (b0 + b1) / 2, 255];
          break;
        case 3:
          finalColor = [0, 0, 0, 255];
          break;
        }
      }

      writePixel(x, y, finalColor);
    }

    readPosition += 1;
  }
}

$jsilxna.DecompressAlphaBlockBC2 = function (source, sourceOffset) {
  return function (x, y) {
    var offset = Math.floor(((y * 4) + x) / 2) + sourceOffset;
    var byte = source[offset];
    var bits;

    if ((x & 1) == 1) {
      bits = (byte >> 4) & 0x0F;
    } else {
      bits = byte & 0x0F;
    }

    return bits * 0x11;
  };
};

$jsilxna.DecompressAlphaBlockBC3 = function (source, sourceOffset) {
  var result = new Array(4 * 4);

  var alpha0 = source[sourceOffset];
  var alpha1 = source[sourceOffset + 1];

  var readPosition = sourceOffset + 2;
  var readPositionBits = 0;
  var finalAlpha;

  // I'm too lazy to get the math for this right using JS integer arithmetic.
  // It's slower, but it works.
  var bits = "";
  for (var i = 0; i < 6; i++) {
    var byte = source[readPosition + i].toString(2);
    while (byte.length < 8)
    byte = "0" + byte;

    bits = byte + bits;
  }

  for (var y = 0; y < 4; y++) {
    for (var x = 0; x < 4; x++) {
      var currentBits = bits.substr(readPositionBits, 3);
      var positionCode = parseInt(currentBits, 2);

      if (alpha0 > alpha1) {
        switch (positionCode) {
        case 0:
          finalAlpha = alpha0;
          break;
        case 1:
          finalAlpha = alpha1;
          break;
        case 2:
          finalAlpha = ((6 * alpha0) + (1 * alpha1)) / 7;
          break;
        case 3:
          finalAlpha = ((5 * alpha0) + (2 * alpha1)) / 7;
          break;
        case 4:
          finalAlpha = ((4 * alpha0) + (3 * alpha1)) / 7;
          break;
        case 5:
          finalAlpha = ((3 * alpha0) + (4 * alpha1)) / 7;
          break;
        case 6:
          finalAlpha = ((2 * alpha0) + (5 * alpha1)) / 7;
          break;
        case 7:
          finalAlpha = ((1 * alpha0) + (6 * alpha1)) / 7;
          break;
        }
      } else {
        switch (positionCode) {
        case 0:
          finalAlpha = alpha0;
          break;
        case 1:
          finalAlpha = alpha1;
          break;
        case 2:
          finalAlpha = ((4 * alpha0) + (1 * alpha1)) / 5
          break;
        case 3:
          finalAlpha = ((3 * alpha0) + (2 * alpha1)) / 5
          break;
        case 4:
          finalAlpha = ((2 * alpha0) + (3 * alpha1)) / 5
          break;
        case 5:
          finalAlpha = ((1 * alpha0) + (4 * alpha1)) / 5
          break;
        case 6:
          finalAlpha = 0;
          break;
        case 7:
          finalAlpha = 255;
          break;
        }
      }

      readPositionBits += 3;

      var _x = 3 - x;
      var _y = 3 - y;
      result[_x + (_y * 4)] = finalAlpha;
    }
  }

  return function (x, y) {
    return result[x + (y * 4)];
  };
};

$jsilxna.makePixelWriter = function (buffer, width, x, y) {
  return function (_x, _y, color) {
    var offset = (((_y + y) * width) + (_x + x)) * 4;

    buffer[offset] = color[0];
    buffer[offset + 1] = color[1];
    buffer[offset + 2] = color[2];
    buffer[offset + 3] = color[3];
  };
};

$jsilxna.DecodeDxt1 = function (width, height, bytes, offset, count) {
  var totalSizeBytes = width * height * 4;
  var result = JSIL.Array.New(System.Byte, totalSizeBytes);

  var blockCountX = Math.floor((width + 3) / 4);
  var blockCountY = Math.floor((height + 3) / 4);
  var blockWidth = (width < 4) ? width : 4;
  var blockHeight = (height < 4) ? height : 4;

  var sourceOffset = offset;

  for (var y = 0; y < blockCountY; y++) {
    for (var x = 0; x < blockCountX; x++) {
      // Decode color data
      $jsilxna.DecompressBlockBC12(
      bytes, sourceOffset, $jsilxna.makePixelWriter(result, width, x * blockWidth, y * blockHeight), null);

      sourceOffset += 8;
    }
  }

  return result;
}

$jsilxna.DecodeDxt3 = function (width, height, bytes, offset, count) {
  var totalSizeBytes = width * height * 4;
  var result = JSIL.Array.New(System.Byte, totalSizeBytes);

  var blockCountX = Math.floor((width + 3) / 4);
  var blockCountY = Math.floor((height + 3) / 4);
  var blockWidth = (width < 4) ? width : 4;
  var blockHeight = (height < 4) ? height : 4;

  var sourceOffset = offset;

  for (var y = 0; y < blockCountY; y++) {
    for (var x = 0; x < blockCountX; x++) {
      // Decode alpha data
      var alphaSource = $jsilxna.DecompressAlphaBlockBC2(
      bytes, sourceOffset);

      sourceOffset += 8;

      // Decode color data
      $jsilxna.DecompressBlockBC12(
      bytes, sourceOffset, $jsilxna.makePixelWriter(result, width, x * blockWidth, y * blockHeight), alphaSource);

      sourceOffset += 8;
    }
  }

  return result;
};

$jsilxna.DecodeDxt5 = function (width, height, bytes, offset, count) {
  var totalSizeBytes = width * height * 4;
  var result = JSIL.Array.New(System.Byte, totalSizeBytes);

  var blockCountX = Math.floor((width + 3) / 4);
  var blockCountY = Math.floor((height + 3) / 4);
  var blockWidth = (width < 4) ? width : 4;
  var blockHeight = (height < 4) ? height : 4;

  var sourceOffset = offset;

  for (var y = 0; y < blockCountY; y++) {
    for (var x = 0; x < blockCountX; x++) {
      // Decode alpha data
      var alphaSource = $jsilxna.DecompressAlphaBlockBC3(
      bytes, sourceOffset);

      sourceOffset += 8;

      // Decode color data
      $jsilxna.DecompressBlockBC12(
      bytes, sourceOffset, $jsilxna.makePixelWriter(result, width, x * blockWidth, y * blockHeight), alphaSource);

      sourceOffset += 8;
    }
  }

  return result;
};

$jsilxna.UnpackColorsToColorBytesRGBA = function UnpackColorsToColorBytesRGBA (colors, startIndex, elementCount) {
  if (JSIL.IsPackedArray(colors)) {
    var offsetBytes = (startIndex * 4) | 0;
    var countBytes = (elementCount * 4) | 0;

    if ((offsetBytes === 0) && (countBytes === colors.bytes.length)) {
      // We can safely return the input array if the size and offset match.
      return colors.bytes;
    } else {
      var resultBuffer = colors.bytes.buffer.slice(offsetBytes, offsetBytes + countBytes);
      var resultArray = new Uint8Array(resultBuffer);
      return resultArray;
    }
  } else {
    var result = JSIL.Array.New(System.Byte, colors.length * 4);

    for (var i = 0, l = elementCount | 0; i < l; i = (i + 1) | 0) {
      var item = colors[(startIndex + i) | 0];

      var p = i * 4;
      result[p + 0] = item.r & 0xFF;
      result[p + 1] = item.g & 0xFF;
      result[p + 2] = item.b & 0xFF;
      result[p + 3] = item.a & 0xFF;
    }
  }

  return result;
};

$jsilxna.PackColorsFromColorBytesRGBA = function PackColorsFromColorBytesRGBA (destArray, destOffset, sourceArray, sourceOffset, count) {
  if (JSIL.IsPackedArray(destArray)) {
    var rawArray = destArray.bytes;
    var offsetBytes = (destOffset * 4) | 0;
    var countBytes = (count * 4) | 0;

    if ((countBytes === sourceArray.length) && (sourceOffset === 0)) {
      rawArray.set(sourceArray, destOffset);
    } else {
      for (var i = 0; i < countBytes; i++) {
        rawArray[(i + offsetBytes) | 0] = sourceArray[(i + sourceOffset) | 0];
      }
    }
  } else {
    for (var i = 0, d = destOffset, s = sourceOffset; i < count; i++, d++, s+=4) {
      // Destination array already has plenty of color instances for us to use.
      var color = destArray[d];
      color.r = sourceArray[s];
      color.g = sourceArray[(s + 1) | 0];
      color.b = sourceArray[(s + 2) | 0];
      color.a = sourceArray[(s + 3) | 0];
    }
  }
};

$jsilxna.ImageFormats = {
  "Color": null,
  "Dxt1": $jsilxna.DecodeDxt1,
  "Dxt2": $jsilxna.DecodeDxt3,
  "Dxt3": $jsilxna.DecodeDxt3,
  "Dxt4": $jsilxna.DecodeDxt5,
  "Dxt5": $jsilxna.DecodeDxt5,
};

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.SamplerState", function ($) {
  $.RawMethod(true, ".cctor2", function () {
      Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp = new Microsoft.Xna.Framework.Graphics.SamplerState(
        Microsoft.Xna.Framework.Graphics.TextureFilter.Point,
        Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp,
        "SamplerState.PointClamp"
      );

      Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp = new Microsoft.Xna.Framework.Graphics.SamplerState(
        Microsoft.Xna.Framework.Graphics.TextureFilter.Point,
        Microsoft.Xna.Framework.Graphics.TextureAddressMode.Wrap,
        "SamplerState.PointWrap"
      );
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      this.cachedFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
      this.name = null;
    }
  );

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [
          getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.TextureFilter"), getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.TextureAddressMode"),
          $.String
        ], [])),
    function _ctor (filter, address, name) {
      this.cachedFilter = filter;
      this.name = name;
    }
  );

  $.Method({Static:false, Public:true }, "get_Filter",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.TextureFilter"), [], [])),
    function get_Filter () {
      return this.cachedFilter;
    }
  );

  $.Method({Static:false, Public:true }, "set_Filter",
    (new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.TextureFilter")], [])),
    function set_Filter (value) {
      this.cachedFilter = value;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.VertexPositionColor", function ($) {

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), $jsilxna.colorRef()], [])),
    function _ctor (position, color) {
      this.Position = position;
      this.Color = color;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.VertexPositionTexture", function ($) {

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")], [])),
    function _ctor (position, textureCoordinate) {
      this.Position = position;
      this.TextureCoordinate = textureCoordinate;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.VertexPositionColorTexture", function ($) {

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector3"), $jsilxna.colorRef(),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")
        ], [])),
    function _ctor (position, color, textureCoordinate) {
      this.Position = position;
      this.Color = color;
      this.TextureCoordinate = textureCoordinate;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.RenderTargetBinding", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.RenderTarget2D")], [])),
    function _ctor (renderTarget) {
      this._renderTarget = renderTarget;
    }
  );

  $.Method({Static:false, Public:true }, "get_RenderTarget",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.Texture"), [], [])),
    function get_RenderTarget () {
      return this._renderTarget;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.GraphicsResource", function ($) {
  $.Method({Static:false, Public:true }, "get_GraphicsDevice",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.GraphicsDevice"), [], [])),
    function get_GraphicsDevice () {
      return this.device;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Graphics.EffectPassCollection", function ($) {
  var temporaryPass = null;
  var getTemporaryPass = function () {
    if (!temporaryPass)
      temporaryPass = new Microsoft.Xna.Framework.Graphics.EffectPass();

    return temporaryPass;
  };

  $.Method({Static:false, Public:true }, "get_Count",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Count () {
      // FIXME
      return 1;
    }
  );

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectPass"), [$.Int32], [])),
    function get_Item (index) {
      // FIXME
      return getTemporaryPass();
    }
  );

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature(getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectPass"), [$.String], [])),
    function get_Item (name) {
      // FIXME
      return getTemporaryPass();
    }
  );

  $.Method({Static:false, Public:true }, "GetEnumerator",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.List`1+Enumerator", [getXnaGraphics().TypeRef("Microsoft.Xna.Framework.Graphics.EffectPass")]), [], [])),
    function GetEnumerator () {
      // FIXME
      return JSIL.GetEnumerator([getTemporaryPass()]);
    }
  );

})