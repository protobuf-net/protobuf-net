/* It is auto-generated file. Do not modify it. */
"use strict";

//
// JSIL loader. Synchronously loads all core JSIL scripts, adds essential libraries to the content manifest,
//  and loads your manifest scripts.
// Load this at the top of your document (optionally after declaring a jsilConfig dict in a previous script tag).
// Asset loading (after page load) is provided by JSIL.Browser.js.
//

(function (globalNamespace) {
  if (typeof (globalNamespace.JSIL) !== "undefined")
    throw new Error("JSIL.js loaded twice");

  var JSIL = {
    __FullName__: "JSIL",
    __NativeModules__: {}
  };

  Object.defineProperty(
    globalNamespace, "JSIL",
    {
      value: JSIL,
      enumerable: true,
      writable: false
    }
  );

  JSIL.GlobalNamespace = globalNamespace;

  if (typeof (globalNamespace.jsilConfig) !== "object")
    globalNamespace.jsilConfig = {};

  if (typeof (globalNamespace.contentManifest) !== "object")
    globalNamespace.contentManifest = {};
})(this);

contentManifest["JSIL"] = [];

var $jsilloaderstate = {
  environment: null,
  loadFailures: []
};

(function loadJSIL (config) {

  function Environment_Browser (config) {
    var self = this;
    this.config = config;
    this.scriptIndex = 0;
    this.scriptURIs = {};

    window.$scriptLoadFailed = function (i) {
      var uri = self.scriptURIs[i];

      if (window.JSIL && window.JSIL.Host && window.JSIL.Host.logWriteLine)
        JSIL.Host.logWriteLine("JSIL.js failed to load script '" + uri + "'");
      else if (window.console && window.console.log)
        console.error("JSIL.js failed to load script '" + uri + "'");

      $jsilloaderstate.loadFailures.push([uri]);

      if (config.onLoadFailure) {
        try {
          config.onLoadFailure(uri);
        } catch (exc) {
        }
      }
    }

    var libraryPrefix;
    if (config.bclMode === "translated") {
      libraryPrefix = "TranslatedBCL/";
    } else if (config.bclMode === "stubbed") {
      libraryPrefix = "StubbedBCL/";
    } else {
      libraryPrefix = "IgnoredBCL/";
    }

    contentManifest["JSIL"].push(["Library", "JSIL.Storage.js"]);
    contentManifest["JSIL"].push(["Library", libraryPrefix + "JSIL.IO.js"]);
    contentManifest["JSIL"].push(["Library", "JSIL.JSON.js"]);
    contentManifest["JSIL"].push(["Library", libraryPrefix + "JSIL.XML.js"]);
  };

  Environment_Browser.prototype.getUserSetting = function (key) {
    key = key.toLowerCase();

    var query = window.location.search.substring(1);
    var vars = query.split('&');

    for (var i = 0; i < vars.length; i++) {
      var pair = vars[i].split('=');

      if (decodeURIComponent(pair[0]).toLowerCase() === key) {
        if (pair.length > 1)
          return decodeURIComponent(pair[1]);
        else
          return true;
      }
    }

    return false;
  };

  Environment_Browser.prototype.loadScript = function (uri) {
    if (window.console && window.console.log)
      window.console.log("Loading '" + uri + "'...");

    this.scriptIndex += 1;
    this.scriptURIs[this.scriptIndex] = uri;

    document.write(
      "<script type=\"text/javascript\" src=\"" + uri + "\" onerror=\"$scriptLoadFailed(" +
      this.scriptIndex +
      ")\"></script>"
    );
  };

  Environment_Browser.prototype.loadEnvironmentScripts = function () {
    var libraryRoot = this.config.libraryRoot;

    this.loadScript(libraryRoot + "JSIL.Browser.js");
    this.loadScript(libraryRoot + "JSIL.Browser.Audio.js");
    this.loadScript(libraryRoot + "JSIL.Browser.Loaders.js");
    this.loadScript(libraryRoot + "JSIL.Browser.Touch.js");
  };


  function Environment_SpidermonkeyShell (config) {
    var self = this;
    this.config = config;

    var libraryPrefix;
    if (config.bclMode === "translated") {
      libraryPrefix = "TranslatedBCL/";
    } else if (config.bclMode === "stubbed") {
      libraryPrefix = "StubbedBCL/";
    } else {
      libraryPrefix = "IgnoredBCL/";
    }

    contentManifest["JSIL"].push(["Library", "JSIL.Storage.js"]);
    contentManifest["JSIL"].push(["Library", libraryPrefix + "JSIL.IO.js"]);
    contentManifest["JSIL"].push(["Library", libraryPrefix + "JSIL.XML.js"]);
  };

  Environment_SpidermonkeyShell.prototype.getUserSetting = function (key) {
    // FIXME
    if (typeof (jsilEnvironmentSettings) === "object" && key in jsilEnvironmentSettings) {
      return jsilEnvironmentSettings[key];
    }
    return false;
  };

  Environment_SpidermonkeyShell.prototype.loadScript = function (uri) {
    load(uri);
  };

  Environment_SpidermonkeyShell.prototype.loadEnvironmentScripts = function () {
    this.loadScript(libraryRoot + "JSIL.Shell.js");
    this.loadScript(libraryRoot + "JSIL.Shell.Loaders.js");
  };

  var priorModule = JSIL.GlobalNamespace.Module;

  JSIL.BeginLoadNativeLibrary = function (name) {
    var priorModule = JSIL.GlobalNamespace.Module;
    JSIL.GlobalNamespace.Module = Object.create(null);
  };

  JSIL.EndLoadNativeLibrary = function (name) {
    var key = name;
    var lastIndex = Math.max(key.lastIndexOf("/"), key.lastIndexOf("\\"));
    if (lastIndex >= 0)
      key = key.substr(lastIndex + 1);

    // HACK
    key = key.replace(".emjs", ".dll").replace(".js", ".dll").toLowerCase();

    if (JSIL.__NativeModules__[key])
      throw new Error("A module named '" + key + "' is already loaded.");

    var module;
    if (typeof(JSIL.GlobalNamespace.Module) == "function") {
      // compiled with emcc -s MODULARIZE=1
      module = JSIL.GlobalNamespace.Module();
    } else {
      module = JSIL.GlobalNamespace.Module;
    }

    // HACK: FIXME: We need a global module to use as a fallback for scenarios
    //  where we don't know which module a call is interacting with
    if (!JSIL.__NativeModules__["__global__"])
      JSIL.__NativeModules__["__global__"] = module;

    JSIL.__NativeModules__[key] = module;

    JSIL.GlobalNamespace.Module = priorModule;
    priorModule = null;
  };


  var environments = {
    "browser": Environment_Browser,
    "spidermonkey_shell": Environment_SpidermonkeyShell
  }

  if (!config.environment) {
    if (typeof (window) !== "undefined")
      config.environment = "browser";
    else
      throw new Error("jsilConfig.environment not set and no default available");
  }

  var environment;

  if (typeof (config.environment) === "function") {
    environment = $jsilloaderstate.environment = new (config.environment)(config);
  } else if (typeof (config.environment) === "string") {
    var environmentType = environments[config.environment];
    if (!environmentType)
      throw new Error("No environment named '" + config.environment + "' available.");

    environment = $jsilloaderstate.environment = new (environmentType)(config);
  }

  if (typeof (config.libraryRoot) === "undefined")
    config.libraryRoot = "../Libraries/";

  var libraryRoot = config.libraryRoot;
  var manifestRoot = (config.manifestRoot = config.manifestRoot || "");
  config.scriptRoot = config.scriptRoot || "";
  config.fileRoot = config.fileRoot || "";
  config.assetRoot = config.assetRoot || "";

  if (typeof (config.contentRoot) === "undefined")
    config.contentRoot = "Content/";

  if (typeof (config.fileVirtualRoot) === "undefined")
    config.fileVirtualRoot = config.fileRoot || "";

  if (config.printStackTrace)
    environment.loadScript(libraryRoot + "printStackTrace.js");

  if (config.webgl2d)
    environment.loadScript(libraryRoot + "webgl-2d.js");

  if (config.gamepad)
    environment.loadScript(libraryRoot + "gamepad.js");

  environment.loadScript(libraryRoot + "Polyfills.js");
  environment.loadScript(libraryRoot + "mersenne.js");

  if (config.typedObjects || false) {
    environment.loadScript(libraryRoot + "typedobjects.js");
    environment.loadScript(libraryRoot + "JSIL.TypedObjects.js");
  }

  environment.loadScript(libraryRoot + "JSIL.Core.js");
  environment.loadScript(libraryRoot + "JSIL.Host.js");

  environment.loadEnvironmentScripts();

  environment.loadScript(libraryRoot + "JSIL.Core.Types.js");
  environment.loadScript(libraryRoot + "JSIL.Core.Reflection.js");
  environment.loadScript(libraryRoot + "JSIL.References.js");
  environment.loadScript(libraryRoot + "JSIL.Unsafe.js");
  environment.loadScript(libraryRoot + "JSIL.PInvoke.js");

  if (!config.bclMode) {
    config.bclMode = environment.getUserSetting("bclMode");
  }

  if (config.bclMode === "translated") {
    environment.loadScript(libraryRoot + "TranslatedBCL/JSIL.Bootstrap.js");
  } else if (config.bclMode === "stubbed") {
    environment.loadScript(libraryRoot + "StubbedBCL/JSIL.Bootstrap.js");
  } else {
    environment.loadScript(libraryRoot + "IgnoredBCL/JSIL.Bootstrap.js");
  }

  environment.loadScript(libraryRoot + "JSIL.mscorlib.js");
  environment.loadScript(libraryRoot + "JSIL.Bootstrap.Int64.js");
  environment.loadScript(libraryRoot + "JSIL.Bootstrap.DateTime.js");
  environment.loadScript(libraryRoot + "JSIL.Bootstrap.Text.js");
  environment.loadScript(libraryRoot + "JSIL.Bootstrap.Resources.js");
  if (config.bclMode !== "translated") {
    environment.loadScript(libraryRoot + "JSIL.Bootstrap.Linq.js");
  }
  environment.loadScript(libraryRoot + "JSIL.Bootstrap.Async.js");
  environment.loadScript(libraryRoot + "JSIL.Bootstrap.Dynamic.js");

  if (config.xml || environment.getUserSetting("xml"))
    environment.loadScript(libraryRoot + "JSIL.XML.js");

  if (config.interpreter || environment.getUserSetting("interpreter"))
    environment.loadScript(libraryRoot + "JSIL.ExpressionInterpreter.js");

  if (config.testFixture || environment.getUserSetting("testFixture"))
    environment.loadScript(libraryRoot + "JSIL.TestFixture.js");

  config.record |= Boolean(environment.getUserSetting("record"));
  config.replayURI = environment.getUserSetting("replayURI") || config.replayURI;
  config.replayName = environment.getUserSetting("replayName") || config.replayName;
  config.fastReplay = config.fastReplay || environment.getUserSetting("fastReplay") || false;
  config.autoPlay = config.autoPlay || environment.getUserSetting("autoPlay") || config.replayURI || config.replayName || false;

  if (
    config.record ||
    config.replayURI ||
    config.replayName
  ) {
    environment.loadScript(libraryRoot + "JSIL.Replay.js");
  }

  config.disableSound = config.disableSound || environment.getUserSetting("disableSound") || false;

  config.viewportScale = parseFloat((config.viewportScale || environment.getUserSetting("viewportScale") || 1.0).toString());

  config.disableFiltering = config.disableFiltering || environment.getUserSetting("disableFiltering") || false;

  config.enableFreezeAndSeal = config.enableFreezeAndSeal || environment.getUserSetting("enableFreezeAndSeal") || false;

  var manifests = config.manifests || [];

  for (var i = 0, l = manifests.length; i < l; i++)
    environment.loadScript(manifestRoot + manifests[i] + ".manifest.js");

  if (config.winForms || config.monogame) {
    contentManifest["JSIL"].push(["Library", "System.Drawing.js"]);
    contentManifest["JSIL"].push(["Library", "System.Windows.js"]);
  }

  if (config.xna) {
    contentManifest["JSIL"].push(["Library", "XNA/XNA4.js"]);

    switch (Number(config.xna)) {
      case 4:
        break;
      default:
        throw new Error("Unsupported XNA version");
    }

    contentManifest["JSIL"].push(["Library", "XNA/Content.js"]);
    contentManifest["JSIL"].push(["Library", "XNA/Graphics.js"]);
    contentManifest["JSIL"].push(["Library", "XNA/Input.js"]);
    contentManifest["JSIL"].push(["Library", "XNA/Audio.js"]);
    contentManifest["JSIL"].push(["Library", "XNA/Storage.js"]);
  }

  if (config.monogame) {
    contentManifest["JSIL"].push(["Library", "MonoGame/OpenTK.js"]);
    contentManifest["JSIL"].push(["Library", "MonoGame/OpenTK.GL.js"]);
    contentManifest["JSIL"].push(["Library", "MonoGame/OpenTK.AL.js"]);
    contentManifest["JSIL"].push(["Library", "MonoGame/SDL.js"]);
  }

  if (config.readOnlyStorage)
    contentManifest["JSIL"].push(["Library", "JSIL.ReadOnlyStorage.js"]);

  if (config.localStorage)
    contentManifest["JSIL"].push(["Library", "JSIL.LocalStorage.js"]);

})(jsilConfig);