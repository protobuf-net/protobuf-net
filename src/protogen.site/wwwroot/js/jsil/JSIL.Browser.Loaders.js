/* It is auto-generated file. Do not modify it. */
JSIL.loadGlobalScript = function (uri, onComplete, dllName) {
  var anchor = document.createElement("a");
  anchor.href = uri;
  var absoluteUri = anchor.href;

  var done = false;

  var body = document.getElementsByTagName("body")[0];

  var scriptTag = document.createElement("script");

  JSIL.Browser.RegisterOneShotEventListener(
    scriptTag, "load", true,
    function ScriptTag_Load (e) {
      if (dllName)
        JSIL.EndLoadNativeLibrary(dllName);

      if (done)
        return;

      done = true;
      onComplete(scriptTag, null);
    }
  );
  JSIL.Browser.RegisterOneShotEventListener(
    scriptTag, "error", true,
    function ScriptTag_Error (e) {
      if (dllName)
        JSIL.EndLoadNativeLibrary(dllName);

      if (done)
        return;

      done = true;
      onComplete(null, e);
    }
  );

  scriptTag.type = "text/javascript";
  scriptTag.src = absoluteUri;

  try {
    if (dllName)
      JSIL.BeginLoadNativeLibrary(dllName);

    body.appendChild(scriptTag);
  } catch (exc) {
    done = true;
    onComplete(null, exc);
  }
};

var warnedAboutOpera = false;
var warnedAboutCORS = false;
var warnedAboutCORSImage = false;
var hasCORSXhr = false, hasCORSImage = false;

function getAbsoluteUrl (localUrl) {
  var temp = document.createElement("a");
  temp.href = localUrl;
  return temp.href;
};

function doXHR (uri, asBinary, onComplete) {
  var req = null, isXDR = false;

  var needCORS = jsilConfig.CORS;
  var urlPrefix = window.location.protocol + "//" + window.location.host + "/";

  var absoluteUrl = getAbsoluteUrl(uri);
  var sameHost = (absoluteUrl.indexOf(urlPrefix) >= 0);

  needCORS = needCORS && !sameHost;

  if (location.protocol === "file:") {
    var errorText = "Loading assets from file:// is not possible in modern web browsers. You must host your application on a web server.";

    if (console && console.error) {
      console.error(errorText + "\nFailed to load: " + uri);
      onComplete(null, errorText);
      return;
    } else {
      throw new Error(errorText);
    }
  } else {
    req = new XMLHttpRequest();

    if (needCORS && !("withCredentials" in req)) {
      if ((!asBinary) && (typeof (XDomainRequest) !== "undefined")) {
        isXDR = true;
        req = new XDomainRequest();
      } else {
        if (!warnedAboutCORS) {
          JSIL.Host.logWriteLine("WARNING: This application requires support for CORS, and your browser does not appear to have it. Loading may fail.");
          warnedAboutCORS = true;
        }

        onComplete(null, "CORS unavailable");
        return;
      }
    }
  }

  var isDone = false;
  var releaseEventListeners = function () {
    req.onprogress = null;
    req.onload = null;
    req.onerror = null;
    req.ontimeout = null;
    req.onreadystatechange = null;
  };
  var succeeded = function (response, status, statusText) {
    if (isDone)
      return;

    isDone = true;
    releaseEventListeners();

    if (status >= 400) {
      onComplete(
        {
          response: response,
          status: status,
          statusText: statusText
        },
        statusText || status
      );
    } else {
      onComplete(
        {
          response: response,
          status: status,
          statusText: statusText
        }, null
      );
    }
  };
  var failed = function (error) {
    if (isDone)
      return;

    isDone = true;
    releaseEventListeners();

    onComplete(null, error);
  };

  if (isXDR) {
    // http://social.msdn.microsoft.com/Forums/en-US/iewebdevelopment/thread/30ef3add-767c-4436-b8a9-f1ca19b4812e
    req.onprogress = function () {};

    req.onload = function () {
      succeeded(req.responseText);
    };

    req.onerror = function () {
      failed("Unknown error");
    };

    req.ontimeout = function () {
      failed("Timed out");
    };
  } else {
    req.onreadystatechange = function (evt) {
      if (req.readyState != 4)
        return;

      if (isDone)
        return;

      if (asBinary) {
        var bytes;
        var ieResponseBody = null;

        try {
          if (
            (typeof (ArrayBuffer) === "function") &&
            (typeof (req.response) === "object") &&
            (req.response !== null)
          ) {
            var buffer = req.response;
            bytes = new Uint8Array(buffer);
          } else if (
            (typeof (VBArray) !== "undefined") &&
            ("responseBody" in req) &&
            ((ieResponseBody = new VBArray(req.responseBody).toArray()) != null)
          ) {
            bytes = ieResponseBody;
          } else if (req.responseText) {
            var text = req.responseText;
            bytes = JSIL.StringToByteArray(text);
          } else {
            failed("Unknown error");
            return;
          }
        } catch (exc) {
          failed(exc);
          return;
        }

        succeeded(bytes, req.status, req.statusText);
      } else {
        try {
          var responseText = req.responseText;
        } catch (exc) {
          failed(exc);
          return;
        }

        succeeded(responseText, req.status, req.statusText);
      }
    };
  }

  try {
    if (isXDR) {
      req.open('GET', uri);
    } else {
      req.open('GET', uri, true);
    }
  } catch (exc) {
    failed(exc);
  }

  if (asBinary) {
    if (typeof (ArrayBuffer) === "function") {
      req.responseType = 'arraybuffer';
    }

    if (typeof (req.overrideMimeType) !== "undefined") {
      req.overrideMimeType('application/octet-stream; charset=x-user-defined');
    } else {
      req.setRequestHeader('Accept-Charset', 'x-user-defined');
    }
  } else {
    if (typeof (req.overrideMimeType) !== "undefined") {
      req.overrideMimeType('text/plain; charset=x-user-defined');
    } else {
      req.setRequestHeader('Accept-Charset', 'x-user-defined');
    }
  }

  try {
    if (isXDR) {
      req.send(null);
    } else {
      req.send();
    }
  } catch (exc) {
    failed(exc);
  }
};

function loadTextAsync (uri, onComplete) {
  return doXHR(uri, false, function (result, error) {
    if (result)
      onComplete(result.response, error);
    else
      onComplete(null, error);
  });
};

function postProcessResultNormal (bytes) {
  return bytes;
};

function postProcessResultOpera (bytes) {
  // Opera sniffs content types on request bodies and if they're text, converts them to 16-bit unicode :|

  if (
    (bytes[1] === 0) &&
    (bytes[3] === 0) &&
    (bytes[5] === 0) &&
    (bytes[7] === 0)
  ) {
    if (!warnedAboutOpera) {
      JSIL.Host.logWriteLine("WARNING: Your version of Opera has a bug that corrupts downloaded file data. Please update to a new version or try a better browser.");
      warnedAboutOpera = true;
    }

    var resultBytes = new Array(bytes.length / 2);
    for (var i = 0, j = 0, l = bytes.length; i < l; i += 2, j += 1) {
      resultBytes[j] = bytes[i];
    }

    return resultBytes;
  } else {
    return bytes;
  }
};

function loadBinaryFileAsync (uri, onComplete) {
  var postProcessResult = postProcessResultNormal;
  if (window.navigator.userAgent.indexOf("Opera/") >= 0) {
    postProcessResult = postProcessResultOpera;
  }

  return doXHR(uri, true, function (result, error) {
    if (result)
      onComplete(postProcessResult(result.response), error);
    else
      onComplete(null, error);
  });
}

var finishLoadingScript = function (state, path, onError, dllName) {
  state.pendingScriptLoads += 1;

  JSIL.loadGlobalScript(path, function (result, error) {
    state.pendingScriptLoads -= 1;

    if (error) {
      var errorText = "Network request failed: " + stringifyLoadError(error);

      state.assetLoadFailures.push(
        [path, errorText]
      );

      if (jsilConfig.onLoadFailure) {
        try {
          jsilConfig.onLoadFailure(path, errorText);
        } catch (exc2) {
        }
      }

      onError(errorText);
    }
  }, dllName);
};

var loadScriptInternal = function (uri, onError, onDoneLoading, state, dllName) {
  var absoluteUrl = getAbsoluteUrl(uri);

  var finisher = function () {
    finishLoadingScript(state, uri, onError, dllName);
  };

  if (absoluteUrl.indexOf("file://") === 0) {
    // No browser properly supports XHR against file://
    onDoneLoading(finisher);
  } else {
    loadTextAsync(uri, function (result, error) {
      if ((result !== null) && (!error))
        onDoneLoading(finisher);
      else
        onError(error);
    });
  }
};

var assetLoaders = {
  "Library": function loadLibrary (filename, data, onError, onDoneLoading, state) {
    if (state.jsilInitialized)
      throw new Error("A library was loaded after JSIL initialization");

    var uri = jsilConfig.libraryRoot + filename;
    loadScriptInternal(uri, onError, onDoneLoading, state);
  },
  "NativeLibrary": function loadNativeLibrary (filename, data, onError, onDoneLoading, state) {
    if (state.jsilInitialized)
      throw new Error("A script was loaded after JSIL initialization");

    var uri = jsilConfig.libraryRoot + filename;
    loadScriptInternal(uri, onError, onDoneLoading, state, filename);
  },
  "Script": function loadScript (filename, data, onError, onDoneLoading, state) {
    if (state.jsilInitialized)
      throw new Error("A script was loaded after JSIL initialization");

    var uri = jsilConfig.scriptRoot + filename;
    loadScriptInternal(uri, onError, onDoneLoading, state);
  },
  "Image": function loadImage (filename, data, onError, onDoneLoading) {
    var e = document.createElement("img");
    if (jsilConfig.CORS) {
      if (hasCORSImage) {
        e.crossOrigin = "";
      } else if (hasCORSXhr && ($blobBuilderInfo.hasBlobBuilder || $blobBuilderInfo.hasBlobCtor)) {
        if (!warnedAboutCORSImage) {
          JSIL.Host.logWriteLine("WARNING: This application requires support for CORS, and your browser does not support it for images. Using workaround...");
          warnedAboutCORSImage = true;
        }

        return loadImageCORSHack(filename, data, onError, onDoneLoading);
      } else {
        if (!warnedAboutCORSImage) {
          JSIL.Host.logWriteLine("WARNING: This application requires support for CORS, and your browser does not support it.");
          warnedAboutCORSImage = true;
        }

        onError("CORS unavailable");
        return;
      }
    }

    var finisher = function () {
      $jsilbrowserstate.allAssetNames.push(filename);
      allAssets[getAssetName(filename)] = new HTML5ImageAsset(getAssetName(filename, true), e);
    };

    JSIL.Browser.RegisterOneShotEventListener(e, "error", true, onError);
    JSIL.Browser.RegisterOneShotEventListener(e, "load", true, onDoneLoading.bind(null, finisher));
    e.src = jsilConfig.contentRoot + filename;
  },
  "File": function loadFile (filename, data, onError, onDoneLoading) {
    loadBinaryFileAsync(jsilConfig.fileRoot + filename, function (result, error) {
      if ((result !== null) && (!error)) {
        $jsilbrowserstate.allFileNames.push(filename);
        allFiles[filename.toLowerCase()] = result;
        onDoneLoading(null);
      } else {
        onError(error);
      }
    });
  },
  "SoundBank": function loadSoundBank (filename, data, onError, onDoneLoading) {
    loadTextAsync(jsilConfig.contentRoot + filename, function (result, error) {
      if ((result !== null) && (!error)) {
        var finisher = function () {
          $jsilbrowserstate.allAssetNames.push(filename);
          allAssets[getAssetName(filename)] = JSON.parse(result);
        };
        onDoneLoading(finisher);
      } else {
        onError(error);
      }
    });
  },
  "Resources": function loadResources (filename, data, onError, onDoneLoading) {
    loadTextAsync(jsilConfig.scriptRoot + filename, function (result, error) {
      if ((result !== null) && (!error)) {
        var finisher = function () {
          $jsilbrowserstate.allAssetNames.push(filename);
          allAssets[getAssetName(filename)] = JSON.parse(result);
        };
        onDoneLoading(finisher);
      } else {
        onError(error);
      }
    });
  },
  "ManifestResource": function loadManifestResourceStream (filename, data, onError, onDoneLoading) {
    loadBinaryFileAsync(jsilConfig.scriptRoot + filename, function (result, error) {
      if ((result !== null) && (!error)) {
        var dict = allManifestResources[data.assembly];
        if (!dict)
          dict = allManifestResources[data.assembly] = Object.create(null);

        dict[filename.toLowerCase()] = result;

        onDoneLoading(null);
      } else {
        onError(error);
      }
    });
  }
};

function $makeXNBAssetLoader (key, typeName) {
  assetLoaders[key] = function (filename, data, onError, onDoneLoading) {
    loadBinaryFileAsync(jsilConfig.contentRoot + filename, function (result, error) {
      if ((result !== null) && (!error)) {
        var finisher = function () {
          $jsilbrowserstate.allAssetNames.push(filename);
          var key = getAssetName(filename, false);
          var assetName = getAssetName(filename, true);
          var parsedTypeName = JSIL.ParseTypeName(typeName);
          var type = JSIL.GetTypeInternal(parsedTypeName, JSIL.GlobalNamespace, true);
          allAssets[key] = JSIL.CreateInstanceOfType(type, "_ctor", [assetName, result]);
        };
        onDoneLoading(finisher);
      } else {
        onError(error);
      }
    });
  };
};

function loadImageCORSHack (filename, data, onError, onDoneLoading) {
  var sourceURL = jsilConfig.contentRoot + filename;

  // FIXME: Pass mime type through from original XHR somehow?
  var mimeType = "application/octet-stream";
  var sourceURLLower = sourceURL.toLowerCase();
  if (sourceURLLower.indexOf(".png") >= 0) {
    mimeType = "image/png";
  } else if (
    (sourceURLLower.indexOf(".jpg") >= 0) ||
    (sourceURLLower.indexOf(".jpeg") >= 0)
  ) {
    mimeType = "image/jpeg";
  }

  loadBinaryFileAsync(sourceURL, function (result, error) {
    if ((result !== null) && (!error)) {
      var objectURL = null;
      try {
        objectURL = JSIL.GetObjectURLForBytes(result, mimeType);
      } catch (exc) {
        onError(exc);
        return;
      }

      var e = document.createElement("img");
      var finisher = function () {
        $jsilbrowserstate.allAssetNames.push(filename);
        allAssets[getAssetName(filename)] = new HTML5ImageAsset(getAssetName(filename, true), e);
      };
      JSIL.Browser.RegisterOneShotEventListener(e, "error", true, onError);
      JSIL.Browser.RegisterOneShotEventListener(e, "load", true, onDoneLoading.bind(null, finisher));
      e.src = objectURL;
    } else {
      onError(error);
    }
  });
};

function initCORSHack () {
  hasCORSXhr = false;
  hasCORSImage = false;

  try {
    var xhr = new XMLHttpRequest();
    hasCORSXhr = xhr && ("withCredentials" in xhr);
  } catch (exc) {
  }

  try {
    var img = document.createElement("img");
    hasCORSImage = img && ("crossOrigin" in img);
  } catch (exc) {
  }
}

function initAssetLoaders () {
  JSIL.InitBlobBuilder();
  initCORSHack();
  initSoundLoader();

  $makeXNBAssetLoader("XNB", "RawXNBAsset");
  $makeXNBAssetLoader("SpriteFont", "SpriteFontAsset");
  $makeXNBAssetLoader("Texture2D", "Texture2DAsset");
};