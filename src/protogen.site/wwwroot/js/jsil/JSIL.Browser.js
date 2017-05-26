/* It is auto-generated file. Do not modify it. */
"use strict";

var currentLogLine = null;

var webglEnabled = false;

var $jsilbrowserstate = window.$jsilbrowserstate = {
  allFileNames: [],
  allAssetNames: [],
  readOnlyStorage: null,
  heldKeys: [],
  heldButtons: [],
  mousePosition: [0, 0],
  isLoading: false,
  isLoaded: false,
  isMainRunning: false,
  hasMainRun: false,
  mainRunAtTime: 0,
  blockKeyboardInput: false,
  blockGamepadInput: false
};


JSIL.DeclareNamespace("JSIL.Browser", false);


JSIL.Browser.CanvasService = function () {
};

JSIL.Browser.CanvasService.prototype.applySize = function (element, desiredWidth, desiredHeight, isViewport) {
  if (typeof (desiredWidth) !== "number")
    desiredWidth = element.width | 0;
  if (typeof (desiredHeight) !== "number")
    desiredHeight = element.height | 0;

  var scaleFactor = jsilConfig.viewportScale;
  if (typeof (scaleFactor) !== "number")
    scaleFactor = +1.0;

  var width, height;
  if (isViewport) {
    width = Math.ceil(+desiredWidth * +scaleFactor) | 0;
    height = Math.ceil(+desiredHeight * +scaleFactor) | 0;
  } else {
    width = desiredWidth | 0;
    height = desiredHeight | 0;
  }

  element.actualWidth = desiredWidth;
  element.actualHeight = desiredHeight;

  if (element.width !== width)
    element.width = width | 0;

  if (element.height !== height)
    element.height = height | 0;
}

JSIL.Browser.CanvasService.prototype.get = function (desiredWidth, desiredHeight) {
  var e = document.getElementById("canvas");

  if (
    (typeof (desiredWidth) !== "undefined") &&
    (typeof (desiredHeight) !== "undefined")
  )
    this.applySize(e, desiredWidth | 0, desiredHeight | 0, true);

  return e;
};

JSIL.Browser.CanvasService.prototype.create = function (desiredWidth, desiredHeight) {
  var e = document.createElement("canvas");

  this.applySize(e, desiredWidth | 0, desiredHeight | 0, false);

  return e;
};


JSIL.Browser.KeyboardService = function () {
};

JSIL.Browser.KeyboardService.prototype.getHeldKeys = function () {
  return Array.prototype.slice.call($jsilbrowserstate.heldKeys);
};


JSIL.Browser.MouseService = function () {
};

JSIL.Browser.MouseService.prototype.getHeldButtons = function () {
  return Array.prototype.slice.call($jsilbrowserstate.heldButtons);
};

JSIL.Browser.MouseService.prototype.getPosition = function () {
  return Array.prototype.slice.call($jsilbrowserstate.mousePosition);
};


JSIL.Browser.PageVisibilityService = function () {
};

JSIL.Browser.PageVisibilityService.prototype.keys = [ "hidden", "mozHidden", "msHidden", "webkitHidden" ];

JSIL.Browser.PageVisibilityService.prototype.get = function () {
  for (var i = 0, l = this.keys.length; i < l; i++) {
    var key = this.keys[i];
    var value = document[key];

    if (typeof (value) !== "undefined")
      return !value;
  }

  return true;
};


JSIL.Browser.RunLaterService = function () {
  this.queue = [];
  this.pending = false;
  this.boundStep = this.step.bind(this);
};

JSIL.Browser.RunLaterService.prototype.enqueue = function (callback) {
  this.queue.push(callback);

  if (!this.pending) {
    this.pending = true;
    window.setTimeout(this.boundStep, 0);
  }
};

JSIL.Browser.RunLaterService.prototype.step = function () {
  var count = this.queue.length;
  this.pending = false;

  for (var i = 0; i < count; i++) {
    var item = this.queue[i];
    item();
  }

  this.queue.splice(0, count);
};


JSIL.Browser.LogService = function () {
  this.currentLine = null;
};

JSIL.Browser.LogService.prototype.write = function (text) {
  var log = document.getElementById("log");
  if (!log) {
    if (window.console && window.console.log)
      window.console.log(text);

    return;
  }

  var lines = text.split("\n");
  if (lines.length === 1) {
    if (this.currentLine === null) {
      this.currentLine = document.createTextNode(lines[0]);
      log.appendChild(this.currentLine);
    } else {
      this.currentLine.textContent += lines[0];
    }

    return;
  }

  for (var i = 0, l = lines.length; i < l; i++) {
    var line = lines[i];

    if ((i === l - 1) && (line.length === 0)) {
      break;
    }

    if (this.currentLine === null) {
      var logLine = document.createTextNode(line);
      log.appendChild(logLine);
    } else {
      this.currentLine.textContent += line;
      this.currentLine = null;
    }

    log.appendChild(document.createElement("br"));
  }
};


JSIL.Browser.WarningService = function (stream) {
  this.stream = stream;
};

JSIL.Browser.WarningService.prototype.write = function (text) {
  // Quirky behavior, but we suppress warnings from the log if the console is available.
  if (window.console && window.console.warn) {
    if (typeof (text) === "string")
      window.console.warn(text.trim());
    else
      window.console.warn(text);
  } else if (this.stream) {
    this.stream.write(text);
  }
};


JSIL.Browser.TickSchedulerService = function () {
  var forceSetTimeout = false ||
    (document.location.search.indexOf("forceSetTimeout") >= 0);

  var requestAnimationFrame = window.requestAnimationFrame ||
    window.mozRequestAnimationFrame ||
    window.webkitRequestAnimationFrame ||
    window.msRequestAnimationFrame ||
    window.oRequestAnimationFrame;

  if (requestAnimationFrame && !forceSetTimeout) {
    this.schedule = function (stepCallback) {
      requestAnimationFrame.call(window, stepCallback);
    };
  } else {
    this.schedule = function (stepCallback) {
      window.setTimeout(stepCallback, 0);
    };
  }
};


JSIL.Browser.LocalStorageService = function (storage) {
  this.storage = storage;
};

JSIL.Browser.LocalStorageService.prototype.getItem = function (key) {
  return this.storage.getItem(key);
};

JSIL.Browser.LocalStorageService.prototype.setItem = function (key, text) {
  return this.storage.setItem(key, text);
};

JSIL.Browser.LocalStorageService.prototype.removeItem = function (key) {
  return this.storage.removeItem(key);
};

JSIL.Browser.LocalStorageService.prototype.getKeys = function () {
  var result = new Array(this.storage.length);

  for (var i = 0, l = result.length; i < l; i++)
    result[i] = this.storage.key(i);

  return result;
};


JSIL.Browser.WindowService = function (window) {
  this.window = window;
};

JSIL.Browser.WindowService.prototype.alert = function () {
  return this.window.alert.apply(this.window, arguments);
};

JSIL.Browser.WindowService.prototype.prompt = function () {
  return this.window.prompt.apply(this.window, arguments);
};

JSIL.Browser.WindowService.prototype.getTitle = function () {
  return this.window.title;
};

JSIL.Browser.WindowService.prototype.setTitle = function (value) {
  return this.window.document.title = this.window.title = value;
};

JSIL.Browser.WindowService.prototype.getLocationHref = function () {
  return this.window.location.href;
};

JSIL.Browser.WindowService.prototype.getLocationHash = function () {
  return this.window.location.hash;
};

JSIL.Browser.WindowService.prototype.getLocationSearch = function () {
  return this.window.location.search;
};

JSIL.Browser.WindowService.prototype.getNavigatorUserAgent = function () {
  return this.window.navigator.userAgent;
};

JSIL.Browser.WindowService.prototype.getNavigatorLanguage = function () {
  return this.window.navigator.language ||
    this.window.navigator.userLanguage ||
    this.window.navigator.systemLanguage ||
    null;
};

JSIL.Browser.WindowService.prototype.getPerformanceUsedJSHeapSize = function () {
  if (
    (typeof (this.window.performance) !== "undefined") &&
    (typeof (this.window.performance.memory) !== "undefined")
  ) {
    return this.window.performance.memory.usedJSHeapSize;
  } else {
    return 0;
  }
};


JSIL.Browser.HistoryService = function (history) {
  this.history = history;
  this.canPushState = typeof (this.history.pushState) === "function";
};

JSIL.Browser.HistoryService.prototype.pushState = function (a, b, c) {
  return this.history.pushState(a, b, c);
};

JSIL.Browser.HistoryService.prototype.replaceState = function (a, b, c) {
  return this.history.replaceState(a, b, c);
};


JSIL.Browser.GamepadService = function (provider) {
  this.provider = provider;
};

JSIL.Browser.GamepadService.prototype.getPreviousState = function (index) {
  var result = this.provider.getPreviousState(index);
  if (!result)
    return null;

  return result;
};

JSIL.Browser.GamepadService.prototype.getState = function (index) {
  var result = this.provider.getState(index);
  if (!result)
    return null;

  return result;
};


JSIL.Browser.NativeGamepadService = function (navigator) {
  if (navigator.webkitGamepads || navigator.mozGamepads || navigator.gamepads) {
    // use attribute

    this.getter = function () {
      return navigator.webkitGetGamepads || navigator.mozGetGamepads || navigator.getGamepads;
    };
  } else {
    // use getter function

    this.getter = navigator.webkitGetGamepads || navigator.mozGetGamepads || navigator.getGamepads || null;
    if (this.getter)
      this.getter = this.getter.bind(navigator);
  }
};

JSIL.Browser.NativeGamepadService.prototype.getState = function () {
  if (this.getter === null)
    return null;

  return this.getter();
};


JSIL.Browser.TraceService = function (console) {
  this.console = console;
};

JSIL.Browser.TraceService.prototype.write = function (text, category) {
  if (this.console) {
    if (arguments.length === 2)
      this.console.log(category + ": " + text);
    else if (arguments.length === 1)
      this.console.log(text);
  }
};

JSIL.Browser.TraceService.prototype.information = function (text) {
  if (this.console)
    this.console.log(text);
};

JSIL.Browser.TraceService.prototype.warning = function (text) {
  if (this.console)
    this.console.warn(text);
};

JSIL.Browser.TraceService.prototype.error = function (text) {
  if (this.console)
    this.console.error(text);
};


(function () {
  var logSvc = new JSIL.Browser.LogService();

  JSIL.Host.registerServices({
    canvas: new JSIL.Browser.CanvasService(),
    mouse: new JSIL.Browser.MouseService(),
    keyboard: new JSIL.Browser.KeyboardService(),
    pageVisibility: new JSIL.Browser.PageVisibilityService(),
    runLater: new JSIL.Browser.RunLaterService(),
    stdout: logSvc,
    stderr: new JSIL.Browser.WarningService(logSvc),
    tickScheduler: new JSIL.Browser.TickSchedulerService(),
    window: new JSIL.Browser.WindowService(window),
    history: new JSIL.Browser.HistoryService(window.history),
    trace: new JSIL.Browser.TraceService(window.console)
  });

  if (typeof (localStorage) !== "undefined")
    JSIL.Host.registerService("localStorage", new JSIL.Browser.LocalStorageService(localStorage));

  if ((typeof (Gamepad) !== "undefined") && Gamepad.supported)
    JSIL.Host.registerService("gamepad", new JSIL.Browser.GamepadService(Gamepad));

  JSIL.Host.registerService("nativeGamepad", new JSIL.Browser.NativeGamepadService(navigator));
})();


JSIL.Host.translateFilename = function (filename) {
  if (filename === null)
    return null;

  var slashRe = /\\/g;

  var root = JSIL.Host.getRootDirectory().toLowerCase().replace(slashRe, "/");
  var _fileRoot = jsilConfig.fileRoot.toLowerCase().replace(slashRe, "/");
  var _filename = filename.replace(slashRe, "/").toLowerCase();

  while (_filename[0] === "/")
    _filename = _filename.substr(1);

  if (_filename.indexOf(root) === 0)
    _filename = _filename.substr(root.length);

  while (_filename[0] === "/")
    _filename = _filename.substr(1);

  if (_filename.indexOf(_fileRoot) === 0)
    _filename = _filename.substr(_fileRoot.length);

  while (_filename[0] === "/")
    _filename = _filename.substr(1);

  return _filename;
}
JSIL.Host.getImage = function (filename) {
  var key = getAssetName(filename, false);
  if (!allAssets.hasOwnProperty(key))
    throw new System.IO.FileNotFoundException("The image '" + key + "' is not in the asset manifest.", filename);

  return allAssets[key].image;
};
JSIL.Host.doesAssetExist = function (filename, stripRoot) {
  if (filename === null)
    return false;

  if (stripRoot === true) {
    var backslashRe = /\\/g;

    filename = filename.replace(backslashRe, "/").toLowerCase();
    var croot = jsilConfig.contentRoot.replace(backslashRe, "/").toLowerCase();

    filename = filename.replace(croot, "").toLowerCase();
  }

  var key = getAssetName(filename, false);
  if (!allAssets.hasOwnProperty(key))
    return false;

  return true;
};
JSIL.Host.getAsset = function (filename, stripRoot) {
  if (filename === null)
    throw new System.Exception("Filename was null");

  if (stripRoot === true) {
    var backslashRe = /\\/g;

    filename = filename.replace(backslashRe, "/").toLowerCase();
    var croot = jsilConfig.contentRoot.replace(backslashRe, "/").toLowerCase();

    filename = filename.replace(croot, "").toLowerCase();
  }

  var key = getAssetName(filename, false);
  if (!allAssets.hasOwnProperty(key))
    throw new System.IO.FileNotFoundException("The asset '" + key + "' is not in the asset manifest.", filename);

  return allAssets[key];
};
JSIL.Host.getRootDirectory = function () {
  var url = window.location.href;
  var lastSlash = url.lastIndexOf("/");
  if (lastSlash === -1)
    return url;
  else
    return url.substr(0, lastSlash);
};
JSIL.Host.getStorageRoot = function () {
  return $jsilbrowserstate.storageRoot;
};


var $logFps = false;

var allFiles = {};
var allAssets = {};
var allManifestResources = {};

// Handle mismatches between dom key codes and XNA key codes
var keyMappings = {
  16: [160, 161], // Left Shift, Right Shift
  17: [162, 163], // Left Control, Right Control
  18: [164, 165] // Left Alt, Right Alt
};

function pressKeys (keyCodes) {
  for (var i = 0; i < keyCodes.length; i++) {
    var code = keyCodes[i];
    if (Array.prototype.indexOf.call($jsilbrowserstate.heldKeys, code) === -1)
      $jsilbrowserstate.heldKeys.push(code);
  }
};

function releaseKeys (keyCodes) {
  $jsilbrowserstate.heldKeys = $jsilbrowserstate.heldKeys.filter(function (element, index, array) {
    return keyCodes.indexOf(element) === -1;
  });
};

function initBrowserHooks () {
  var canvas = document.getElementById("canvas");
  if (canvas) {
    $jsilbrowserstate.nativeWidth = canvas.width;
    $jsilbrowserstate.nativeHeight = canvas.height;

    canvas.draggable = false;
    canvas.unselectable = true;
  }

  // Be a good browser citizen!
  // Disabling commonly used hotkeys makes people rage.
  var shouldIgnoreEvent = function (evt) {
    if ($jsilbrowserstate.blockKeyboardInput)
      return true;

    if ((document.activeElement !== null)) {
      switch (String(document.activeElement.tagName).toLowerCase()) {
        case "form":
        case "select":
        case "input":
        case "datalist":
        case "option":
        case "textarea":
          return true;
      }
    }

    switch (evt.keyCode) {
      case 116: // F5
      case 122: // F11
        return true;
    }

    if (evt.ctrlKey) {
      switch (evt.keyCode) {
        case 67: // C
        case 78: // N
        case 84: // T
        case 86: // V
        case 88: // X
          return true;
      }
    }

    return false;
  };

  window.addEventListener(
    "keydown", function (evt) {
      if (shouldIgnoreEvent(evt)) {
        return;
      }

      evt.preventDefault();
      var keyCode = evt.keyCode;
      var codes = keyMappings[keyCode] || [keyCode];

      pressKeys(codes);
    }, true
  );

  window.addEventListener(
    "keyup", function (evt) {
      if (!shouldIgnoreEvent(evt))
        evt.preventDefault();

      var keyCode = evt.keyCode;
      var codes = keyMappings[keyCode] || [keyCode];

      releaseKeys(codes);
    }, true
  );

  JSIL.Host.mapClientCoordinates = function (x, y) {
    var localCanvas = canvas || document.getElementById("canvas");
    if (!localCanvas)
      return [x, y];

    // We have to use this to get actual post-CSS3-transform coordinates. HTML/CSS are dumb.
    var canvasRect = localCanvas.getBoundingClientRect();

    var xScale = $jsilbrowserstate.nativeWidth / canvasRect.width;
    var yScale = $jsilbrowserstate.nativeHeight / canvasRect.height;

    var mapped_x = ((x - canvasRect.left) * xScale) | 0;
    var mapped_y = ((y - canvasRect.top) * yScale) | 0;

    return [mapped_x, mapped_y]
  };

  var mapMouseCoords = function (evt) {
    var mapped = JSIL.Host.mapClientCoordinates(evt.clientX, evt.clientY);

    $jsilbrowserstate.mousePosition[0] = mapped[0];
    $jsilbrowserstate.mousePosition[1] = mapped[1];
  };

  if (canvas) {
    canvas.addEventListener(
      "contextmenu", function (evt) {
        evt.preventDefault();
        evt.stopPropagation();
        return false;
      }, true
    );

    canvas.addEventListener(
      "mousedown", function (evt) {
        mapMouseCoords(evt);

        var button = evt.button;
        if (Array.prototype.indexOf.call($jsilbrowserstate.heldButtons, button) === -1)
          $jsilbrowserstate.heldButtons.push(button);

        return false;
      }, true
    );

    canvas.addEventListener(
      "mouseup", function (evt) {
        mapMouseCoords(evt);

        var button = evt.button;
        $jsilbrowserstate.heldButtons = $jsilbrowserstate.heldButtons.filter(function (element, index, array) {
          (element !== button);
        });

        return false;
      }, true
    );

    canvas.addEventListener(
      "onselectstart", function (evt) {
        evt.preventDefault();
        evt.stopPropagation();
        return false;
      }, true
    );

    canvas.addEventListener(
      "ondragstart", function (evt) {
        evt.preventDefault();
        evt.stopPropagation();
        return false;
      }, true
    );
  };

  document.addEventListener(
    "mousemove", function (evt) {
      mapMouseCoords(evt);
    }, true
  );

  if (typeof(initTouchEvents) !== "undefined")
    initTouchEvents();
};

function getAssetName (filename, preserveCase) {
  var backslashRe = /\\/g;
  filename = filename.replace(backslashRe, "/");

  var doubleSlashRe = /\/\//g;
  while (filename.indexOf("//") >= 0)
    filename = filename.replace(doubleSlashRe, "/");

  var lastIndex = filename.lastIndexOf(".");
  var result;
  if (lastIndex === -1)
    result = filename;
  else
    result = filename.substr(0, lastIndex);

  if (preserveCase === true)
    return result;
  else
    return result.toLowerCase();
};

var loadedFontCount = 0;
var loadingPollInterval = 1;
var maxAssetsLoading = 4;
var soundLoadTimeout = 30000;
var fontLoadTimeout = 10000;
var finishStepDuration = 25;

function updateProgressBar (prefix, suffix, bytesLoaded, bytesTotal) {
  if (jsilConfig.updateProgressBar)
    return jsilConfig.updateProgressBar(prefix, suffix, bytesLoaded, bytesTotal);

  var loadingProgress = document.getElementById("loadingProgress");
  var progressBar = document.getElementById("progressBar");
  var progressText = document.getElementById("progressText");

  var w = 0;
  if (loadingProgress) {
    w = (bytesLoaded * loadingProgress.clientWidth) / (bytesTotal);
    if (w < 0)
      w = 0;
    else if (w > loadingProgress.clientWidth)
      w = loadingProgress.clientWidth;
  }

  if (progressBar)
    progressBar.style.width = w.toString() + "px";

  if (progressText) {
    var progressString;
    if (suffix === null) {
      progressString = prefix;
    } else {
      progressString = prefix + Math.floor(bytesLoaded) + suffix + " / " + Math.floor(bytesTotal) + suffix;
    }

    if (jsilConfig.formatProgressText)
      progressString = jsilConfig.formatProgressText(prefix, suffix, bytesLoaded, bytesTotal, progressString);

    progressText.textContent = progressString;
    progressText.style.left = ((loadingProgress.clientWidth - progressText.clientWidth) / 2).toString() + "px";
    progressText.style.top = ((loadingProgress.clientHeight - progressText.clientHeight) / 2).toString() + "px";
  }
};

function finishLoading () {
  var state = this;

  var started = Date.now();
  var endBy = started + finishStepDuration;

  var initFileStorage = function (volume) {
    for (var i = 0, l = $jsilbrowserstate.allFileNames.length; i < l; i++) {
      var filename = $jsilbrowserstate.allFileNames[i];
      var file = volume.createFile(filename, false, true);
      file.writeAllBytes(allFiles[filename.toLowerCase()]);
    }
  };

  var initIfNeeded = function () {
    if (!state.jsilInitialized) {
      state.jsilInitialized = true;
      JSIL.Initialize();
    }

    if (state.initFailed) {
      return;
    }

    try {
      if (typeof ($jsilreadonlystorage) !== "undefined") {
        var prefixedFileRoot;

        if (jsilConfig.fileVirtualRoot[0] !== "/")
          prefixedFileRoot = "/" + jsilConfig.fileVirtualRoot;
        else
          prefixedFileRoot = jsilConfig.fileVirtualRoot;

        $jsilbrowserstate.readOnlyStorage = new ReadOnlyStorageVolume("files", prefixedFileRoot, initFileStorage);
      }

      JSIL.SetLazyValueProperty($jsilbrowserstate, "storageRoot", function InitStorageRoot () {
        var root;
        if (JSIL.GetStorageVolumes) {
          var volumes = JSIL.GetStorageVolumes();

          if (volumes.length) {
            root = volumes[0];
          }
        }

        if (!root && typeof(VirtualVolume) === "function") {
          root = new VirtualVolume("root", "/");
        }

        if (root) {
          if ($jsilbrowserstate.readOnlyStorage) {
            var trimmedRoot = jsilConfig.fileVirtualRoot.trim();

            if (trimmedRoot !== "/" && trimmedRoot)
              root.createJunction(jsilConfig.fileVirtualRoot, $jsilbrowserstate.readOnlyStorage.rootDirectory, false);
            else
              root = $jsilbrowserstate.readOnlyStorage;
          }

          return root;
        }

        return null;
      });

      state.initFailed = false;
    } catch (exc) {
      state.initFailed = true;

      throw exc;
    }
  };

  while (Date.now() <= endBy) {
    if (state.pendingScriptLoads > 0)
      return;

    if (state.finishIndex < state.finishQueue.length) {
      try {
        var item = state.finishQueue[state.finishIndex];
        var cb = item[2];

        // Ensure that we initialize the JSIL runtime before constructing asset objects.
        if (
          (item[0] != "Script") &&
          (item[0] != "Library") &&
          (item[0] != "NativeLibrary")
        ) {
          initIfNeeded();
        }

        updateProgressBar("Loading " + item[3], null, state.assetsFinished, state.assetCount);

        if (typeof (cb) === "function") {
          cb(state);
        }
      } catch (exc) {
        state.assetLoadFailures.push(
          [item[3], exc]
        );

        if (jsilConfig.onLoadFailure) {
          try {
            jsilConfig.onLoadFailure(item[3], exc);
          } catch (exc2) {
          }
        }
      } finally {
        state.finishIndex += 1;
        state.assetsFinished += 1;
      }
    } else {
      initIfNeeded();

      updateProgressBar("Starting", null, 1, 1);

      var allFailures = $jsilloaderstate.loadFailures.concat(state.assetLoadFailures);

      window.clearInterval(state.interval);
      state.interval = null;
      window.setTimeout(
        state.onDoneLoading.bind(window, allFailures), 10
      );
      return;
    }
  }
};

function pollAssetQueue () {
  var state = this;

  var w = 0;
  updateProgressBar("Downloading: ", "kb", state.bytesLoaded / 1024, state.assetBytes / 1024);

  var makeStepCallback = function (state, type, sizeBytes, i, name) {
    return function (finish) {
      var realName = name;

      var lastDot = name.lastIndexOf(".");
      if (lastDot >= 0)
        name = name.substr(0, lastDot);

      var firstComma = name.indexOf(",");
      if (firstComma >= 0)
        name = name.substr(0, firstComma);

      if (typeof (finish) === "function")
        state.finishQueue.push([type, i, finish, name]);

      delete state.assetsLoadingNames[realName];
      state.assetsLoading -= 1;
      state.assetsLoaded += 1;

      state.bytesLoaded += sizeBytes;
    };
  };

  var makeErrorCallback = function (assetPath, assetSpec) {
    return function (e) {
      delete state.assetsLoadingNames[getAssetName(assetPath)];
      state.assetsLoading -= 1;
      state.assetsLoaded += 1;

      allAssets[getAssetName(assetPath)] = null;

      var errorText = stringifyLoadError(e);

      state.assetLoadFailures.push(
        [assetPath, errorText]
      );

      if (jsilConfig.onLoadFailure) {
        try {
          jsilConfig.onLoadFailure(item[3], errorText);
        } catch (exc2) {
        }
      }

      JSIL.Host.logWriteLine("The asset '" + assetPath + "' could not be loaded: " + errorText);
    };
  };

  while ((state.assetsLoading < maxAssetsLoading) && (state.loadIndex < state.assetCount)) {
    try {
      var assetSpec = state.assets[state.loadIndex];

      var assetType = assetSpec[0];
      var assetPath = assetSpec[1];
      var assetData = assetSpec[2] || null;
      var assetLoader = assetLoaders[assetType];

      var sizeBytes = 1;
      if (assetData !== null)
        sizeBytes = assetData.sizeBytes || 1;

      var stepCallback = makeStepCallback(state, assetType, sizeBytes, state.loadIndex, assetPath);
      var errorCallback = makeErrorCallback(assetPath, assetSpec);

      if (typeof (assetLoader) !== "function") {
        errorCallback("No asset loader registered for type '" + assetType + "'.");
      } else {
        state.assetsLoading += 1;
        state.assetsLoadingNames[assetPath] = assetLoader;
        assetLoader(assetPath, assetData, errorCallback, stepCallback, state);
      }
    } finally {
      state.loadIndex += 1;
    }
  }

  if (state.assetsLoaded >= state.assetCount) {
    window.clearInterval(state.interval);
    state.interval = null;

    state.assetsLoadingNames = {};

    state.finishQueue.sort(function (lhs, rhs) {
      var lhsTypeIndex = 2, rhsTypeIndex = 2;
      var lhsIndex = lhs[1];
      var rhsIndex = rhs[1];

      switch (lhs[0]) {
        case "Library":
        case "NativeLibrary":
          lhsTypeIndex = 0;
          break;
        case "NativeLibrary":
          lhsTypeIndex = 0;
          break;
        case "Script":
          lhsTypeIndex = 1;
          break;
      }

      switch (rhs[0]) {
        case "Library":
        case "NativeLibrary":
          rhsTypeIndex = 0;
          break;
        case "NativeLibrary":
          rhsTypeIndex = 0;
          break;
        case "Script":
          rhsTypeIndex = 1;
          break;
      }

      var result = JSIL.CompareValues(lhsTypeIndex, rhsTypeIndex);
      if (result === 0)
        result = JSIL.CompareValues(lhsIndex, rhsIndex);

      return result;
    });

    state.interval = window.setInterval(finishLoading.bind(state), 1);

    return;
  }
};

function loadAssets (assets, onDoneLoading) {
  var state = {
    assetBytes: 0,
    assetCount: assets.length,
    bytesLoaded: 0,
    assetsLoaded: 0,
    assetsFinished: 0,
    assetsLoading: 0,
    onDoneLoading: onDoneLoading,
    assets: assets,
    interval: null,
    finishQueue: [],
    loadIndex: 0,
    finishIndex: 0,
    pendingScriptLoads: 0,
    jsilInitialized: false,
    assetsLoadingNames: {},
    assetLoadFailures: [],
    failedFinishes: 0
  };

  for (var i = 0, l = assets.length; i < l; i++) {
    var properties = assets[i][2];

    if (typeof (properties) !== "object") {
      state.assetBytes += 1;
      continue;
    }

    var sizeBytes = properties.sizeBytes || 1;
    state.assetBytes += sizeBytes;
  }

  state.interval = window.setInterval(pollAssetQueue.bind(state), 1);
};

function beginLoading () {
  initAssetLoaders();

  $jsilbrowserstate.isLoading = true;

  var progressBar = document.getElementById("progressBar");
  var loadButton = document.getElementById("loadButton");
  var fullscreenButton = document.getElementById("fullscreenButton");
  var loadingProgress = document.getElementById("loadingProgress");
  var stats = document.getElementById("stats");

  if (progressBar)
    progressBar.style.width = "0px";
  if (loadButton)
    loadButton.style.display = "none";
  if (loadingProgress)
    loadingProgress.style.display = "";

  var seenFilenames = {};

  var pushAsset = function (assetSpec) {
    var filename = assetSpec[1];
    if (seenFilenames[filename])
      return;

    seenFilenames[filename] = true;
    allAssetsToLoad.push(assetSpec);
  }

  var allAssetsToLoad = [];
  if (typeof (window.assetsToLoad) !== "undefined") {
    for (var i = 0, l = assetsToLoad.length; i < l; i++)
      pushAsset(assetsToLoad[i]);
  }

  if (typeof (contentManifest) === "object") {
    for (var k in contentManifest) {
      var subManifest = contentManifest[k];

      for (var i = 0, l = subManifest.length; i < l; i++)
        pushAsset(subManifest[i]);

    }
  }

  JSIL.Host.logWrite("Loading data ... ");
  loadAssets(allAssetsToLoad, browserFinishedLoadingCallback);
};

function browserFinishedLoadingCallback (loadFailures) {
  var progressBar = document.getElementById("progressBar");
  var loadButton = document.getElementById("loadButton");
  var fullscreenButton = document.getElementById("fullscreenButton");
  var loadingProgress = document.getElementById("loadingProgress");
  var stats = document.getElementById("stats");

  $jsilbrowserstate.isLoading = false;
  $jsilbrowserstate.isLoaded = true;

  if (loadFailures && (loadFailures.length > 0)) {
    JSIL.Host.logWriteLine("failed.");
  } else {
    JSIL.Host.logWriteLine("done.");
  }
  try {
    if (fullscreenButton && canGoFullscreen)
      fullscreenButton.style.display = "";

    if (stats)
      stats.style.display = "";

    if (jsilConfig.onLoadFailed && loadFailures && (loadFailures.length > 0)) {
      jsilConfig.onLoadFailed(loadFailures);
    } else {
      JSIL.Host.runInitCallbacks();

      if (typeof (runMain) === "function") {
        $jsilbrowserstate.mainRunAtTime = Date.now();
        $jsilbrowserstate.isMainRunning = true;
        runMain();
        $jsilbrowserstate.isMainRunning = false;
        $jsilbrowserstate.hasMainRun = true;
      }
    }

    // Main doesn't block since we're using the browser's event loop
  } finally {
    $jsilbrowserstate.isMainRunning = false;

    if (loadingProgress)
      loadingProgress.style.display = "none";
  }
};

var canGoFullscreen = false;
var integralFullscreenScaling = false;
var overrideFullscreenBaseSize = null;

function generateHTML () {
  var body = document.getElementsByTagName("body")[0];

  if (jsilConfig.showFullscreenButton) {
    if (document.getElementById("fullscreenButton") === null) {
      var button = document.createElement("button");
      button.id = "fullscreenButton";
      button.appendChild(document.createTextNode("Full Screen"));
      body.appendChild(button);
    }
  }

  if (jsilConfig.showStats) {
    if (document.getElementById("stats") === null) {
      var statsDiv = document.createElement("div");
      statsDiv.id = "stats";
      body.appendChild(statsDiv);
    }
  }

  if (jsilConfig.showProgressBar) {
    var progressDiv = document.getElementById("loadingProgress");
    if (progressDiv === null) {
      progressDiv = document.createElement("div");
      progressDiv.id = "loadingProgress";

      var progressContainer = body;
      if (jsilConfig.getProgressContainer)
        progressContainer = jsilConfig.getProgressContainer();

      progressContainer.appendChild(progressDiv);
    }

    progressDiv.innerHTML = (
      '  <div id="progressBar"></div>' +
      '  <span id="progressText"></span>'
    );
  }
};

function setupStats () {
  var statsElement = document.getElementById("stats");
  var performanceReporterFunction;

  if (statsElement !== null) {
    var statsHtml;
    if (jsilConfig.graphicalStats) {
      statsHtml = '<label for="fpsIndicator">Performance: </label><div id="fpsIndicator"></div>';
    } else {
      statsHtml = '<span title="Frames Per Second"><span id="framesPerSecond">0</span> fps</span><br>' +
        '<span title="Texture Cache Size" id="cacheSpan"><span id="cacheSize">0.0</span >mb <span id="usingWebGL" style="display: none">(WebGL)</span></span><br>';

      if (!jsilConfig.replayURI && !jsilConfig.replayName) {
        statsHtml +=
          '<input type="checkbox" checked="checked" id="balanceFramerate" name="balanceFramerate"> <label for="balanceFramerate">Balance FPS</label>';
      }
    }

    if (jsilConfig.replayURI || jsilConfig.replayName) {
      statsHtml +=
        '<span id="replayState"></span><br>';

      if (!jsilConfig.fastReplay) {
        statsHtml +=
          '<input type="checkbox" id="fastReplay" name="fastReplay"> <label for="fastReplay">Fast Playback</label>';
      }
    }

    if (jsilConfig.record) {
      statsHtml +=
        '<br><span id="recordState"></span><br>' +
        '<button id="saveRecording">Save Recording</button>';
    }

    statsElement.innerHTML = statsHtml;

    if (jsilConfig.record) {
      document.getElementById("saveRecording").addEventListener(
        "click", showSaveRecordingDialog, true
      );
    }

    performanceReporterFunction = function (drawDuration, updateDuration, cacheSize, isWebGL) {
      var duration = drawDuration + updateDuration;
      if (duration <= 0)
        duration = 0.01;

      var effectiveFramerate = 1000 / duration;

      if (jsilConfig.graphicalStats) {
        var e = document.getElementById("fpsIndicator");
        var color, legend;

        if (effectiveFramerate >= 50) {
          color = "green";
          legend = "Great";
        } else if (effectiveFramerate >= 25) {
          color = "yellow";
          legend = "Acceptable";
        } else {
          color = "red";
          legend = "Poor";
        }

        e.style.backgroundColor = color;
        e.title = "Performance: " + legend + " (~" + effectiveFramerate.toFixed(1) + " frames/s)";
      } else {
        var e = document.getElementById("framesPerSecond");
        e.textContent = effectiveFramerate.toFixed(2);

        var cacheSizeMb = (cacheSize / (1024 * 1024)).toFixed(1);

        if (isWebGL) {
          e = document.getElementById("usingWebGL");
          e.title = "Using WebGL for rendering";
          e.style.display = "inline-block";
        }

        e = document.getElementById("cacheSize");
        e.innerHTML = cacheSizeMb;
      }

      if (jsilConfig.reportPerformance)
        jsilConfig.reportPerformance(drawDuration, updateDuration, cacheSize, isWebGL);

      if ($logFps)
        console.log("draw ms:", drawDuration, " update ms:", updateDuration);
    };
  } else {
    performanceReporterFunction = function (drawDuration, updateDuration) {
      if ($logFps)
        console.log("draw ms:", drawDuration, " update ms:", updateDuration);
    };
  }

  JSIL.Host.registerService("performanceReporter", {
    report: performanceReporterFunction
  });
};

function onLoad () {
  registerErrorHandler();

  initBrowserHooks();

  generateHTML();
  setupStats();

  var log = document.getElementById("log");
  var loadButton = document.getElementById("loadButton");
  var loadingProgress = document.getElementById("loadingProgress");
  var fullscreenButton = document.getElementById("fullscreenButton");
  var statsElement = document.getElementById("stats");

  if (log)
    log.value = "";

  if (statsElement)
    statsElement.style.display = "none";

  if (fullscreenButton) {
    fullscreenButton.style.display = "none";

    var canvas = document.getElementById("canvas");
    var originalWidth = canvas.width;
    var originalHeight = canvas.height;

    var fullscreenElement = canvas;
    if (jsilConfig.getFullscreenElement)
      fullscreenElement = jsilConfig.getFullscreenElement();

    var reqFullscreen = fullscreenElement.requestFullScreenWithKeys ||
      fullscreenElement.mozRequestFullScreenWithKeys ||
      fullscreenElement.webkitRequestFullScreenWithKeys ||
      fullscreenElement.requestFullscreen ||
      fullscreenElement.mozRequestFullScreen ||
      fullscreenElement.webkitRequestFullScreen ||
      null;

    if (reqFullscreen) {
      canGoFullscreen = true;

      var goFullscreen = function () {
        reqFullscreen.call(fullscreenElement, Element.ALLOW_KEYBOARD_INPUT);
      };

      var onFullscreenChange = function () {
        var isFullscreen = document.fullscreen ||
          document.fullScreen ||
          document.mozFullScreen ||
          document.webkitIsFullScreen ||
          fullscreenElement.fullscreen ||
          fullscreenElement.fullScreen ||
          fullscreenElement.mozFullScreen ||
          fullscreenElement.webkitIsFullScreen ||
          false;

        $jsilbrowserstate.isFullscreen = isFullscreen;

        if (isFullscreen) {
          var ow = originalWidth, oh = originalHeight;
          if (overrideFullscreenBaseSize) {
            ow = overrideFullscreenBaseSize[0];
            oh = overrideFullscreenBaseSize[1];
          }

          var scaleRatio = Math.min(screen.width / ow, screen.height / oh);
          if (integralFullscreenScaling)
            scaleRatio = Math.floor(scaleRatio);

          canvas.width = ow * scaleRatio;
          canvas.height = oh * scaleRatio;
        } else {
          canvas.width = originalWidth;
          canvas.height = originalHeight;
        }

        if (jsilConfig.onFullscreenChange)
          jsilConfig.onFullscreenChange(isFullscreen);
      };

      document.addEventListener("fullscreenchange", onFullscreenChange, false);
      document.addEventListener("mozfullscreenchange", onFullscreenChange, false);
      document.addEventListener("webkitfullscreenchange", onFullscreenChange, false);

      fullscreenButton.addEventListener(
        "click", goFullscreen, true
      );
    }
  };

  if (loadButton && !jsilConfig.autoPlay) {
    loadButton.addEventListener(
      "click", beginLoading, true
    );

    if (loadingProgress)
      loadingProgress.style.display = "none";
  } else {
    beginLoading();
  }
}

function registerErrorHandler () {
  var oldErrorHandler = window.onerror;

  window.onerror = function JSIL_OnUnhandledException (errorMsg, url, lineNumber) {
    JSIL.Host.logWriteLine("Unhandled exception at " + url + " line " + lineNumber + ":");
    JSIL.Host.logWriteLine(errorMsg);

    if (typeof (oldErrorHandler) === "function")
      return oldErrorHandler(errorMsg, url, lineNumber);
    else
      return false;
  };
};

function stringifyLoadError (error) {
  if (error && error.statusText)
    return error.statusText;
  else if (
    error &&
    (typeof (error) === "object") &&
    (error.toString().indexOf("[object") === 0)
  )
    return "Unknown error";
  else
    return String(error);
};

function showSaveRecordingDialog () {
  try {
    Microsoft.Xna.Framework.Game.ForcePause();
  } catch (exc) {
  }

  var theDialog = document.getElementById("saveRecordingDialog");
  if (!theDialog) {
    var dialog = document.createElement("div");
    dialog.id = "saveRecordingDialog";

    dialog.innerHTML =
      '<label for="recordingName">Recording Name:</label> ' +
      '<input type="text" id="recordingName" value="test" style="background: white; color: black"><br>' +
      '<a id="saveRecordingToLocalStorage" href="#" style="color: black">Save to Local Storage</a> | ' +
      '<a id="saveRecordingAsFile" download="test.replay" target="_blank" href="#" style="color: black">Download</a> | ' +
      '<a id="cancelSaveRecording" href="#" style="color: black">Close</a>';

    dialog.style.position = "absolute";
    dialog.style.background = "rgba(240, 240, 240, 0.9)";
    dialog.style.color = "black";
    dialog.style.padding = "24px";
    dialog.style.borderRadius = "8px 8px 8px 8px";
    dialog.style.boxShadow = "2px 2px 4px rgba(0, 0, 0, 0.75)";

    var body = document.getElementsByTagName("body")[0];

    body.appendChild(dialog);
    theDialog = dialog;

    document.getElementById("saveRecordingToLocalStorage").addEventListener("click", saveRecordingToLocalStorage, true);
    document.getElementById("cancelSaveRecording").addEventListener("click", hideSaveRecordingDialog, true);

    var inputField = document.getElementById("recordingName")
    inputField.addEventListener("input", updateSaveLinkDownloadAttribute, true);
    inputField.addEventListener("change", updateSaveLinkDownloadAttribute, true);
    inputField.addEventListener("blur", updateSaveLinkDownloadAttribute, true);
  }

  var saveLink = document.getElementById("saveRecordingAsFile");

  try {
    // FIXME: Memory leak
    var json = JSIL.Replay.SaveAsJSON();
    var bytes = JSIL.StringToByteArray(json);

    saveLink.href = JSIL.GetObjectURLForBytes(bytes, "application/json");
  } catch (exc) {
  }

  var x = (document.documentElement.clientWidth - theDialog.clientWidth) / 2;
  var y = (document.documentElement.clientHeight - theDialog.clientHeight) / 2;
  theDialog.style.left = x + "px";
  theDialog.style.top = y + "px";
  theDialog.style.display = "block";
};

function updateSaveLinkDownloadAttribute (evt) {
  var saveLink = document.getElementById("saveRecordingAsFile");
  var recordingName = document.getElementById("recordingName").value.trim() || "untitled";

  saveLink.download = recordingName + ".replay";
};

function saveRecordingToLocalStorage (evt) {
  if (evt) {
    evt.preventDefault();
    evt.stopPropagation();
  }

  JSIL.Replay.SaveToLocalStorage(document.getElementById("recordingName").value.trim() || "untitled");
};

function hideSaveRecordingDialog (evt) {
  if (evt) {
    evt.preventDefault();
    evt.stopPropagation();
  }

  var theDialog = document.getElementById("saveRecordingDialog");
  theDialog.style.display = "none";

  try {
    Microsoft.Xna.Framework.Game.ForceUnpause();
  } catch (exc) {
  }
};

JSIL.Browser.OneShotEventListenerCount = 0;

JSIL.Browser.$MakeWrappedListener = function (listener, notification) {
  return function WrappedEventListener () {
    notification();

    return listener.apply(this, arguments);
  };
};

JSIL.Browser.RegisterOneShotEventListener = function (element, eventName, capture, listener) {
  var registered = true;
  var unregister, wrappedListener;

  unregister = function () {
    if (registered) {
      registered = false;
      element.removeEventListener(eventName, wrappedListener, capture);
      JSIL.Browser.OneShotEventListenerCount -= 1;

      wrappedListener = null;
      element = null;
    }
  };

  wrappedListener = JSIL.Browser.$MakeWrappedListener(listener, unregister);
  listener = null;

  JSIL.Browser.OneShotEventListenerCount += 1;
  element.addEventListener(eventName, wrappedListener, capture);

  return {
    eventName: eventName,
    unregister: unregister
  }
};