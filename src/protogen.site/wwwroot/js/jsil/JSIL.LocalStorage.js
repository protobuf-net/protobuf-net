/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

if (typeof ($jsilstorage) === "undefined")
  throw new Error("JSIL.Storage required");

var $jsillocalstorage = JSIL.DeclareAssembly("JSIL.LocalStorage");

JSIL.MakeClass($jsilstorage.TypeRef("VirtualVolume"), "LocalStorageVolume", true, [], function ($) {
  $.RawMethod(false, ".ctor", function (name, rootPath) {
    this.service = JSIL.Host.getService("localStorage");

    var existingInodes = this.readInodes(name);

    VirtualVolume.prototype._ctor.call(
      this, name, rootPath, existingInodes
    );
  });

  $.RawMethod(false, "handleFailure", function (exc, fn) {
    var keys = this.service.getKeys();
    JSIL.Host.logWriteLine("Failed to write '" + fn + "' to local storage. Local storage currently contains " + keys.length + " key(s):");
    for (var i = 0, l = keys.length; i < l; i++) {
      var key = keys[i];
      JSIL.Host.logWriteLine(System.String.Format("{0:0000.0}kb '{1}'", this.service.getItem(key).length / 1024, key));
    }

    throw exc;
  });

  var getKey = function (name) {
    return "storage_" + name;
  };

  $.RawMethod(false, "readInodes", function (name) {
    var key = getKey(name) + "_inodes";

    var json = this.service.getItem(key);
    if (json)
      return JSON.parse(json);
    else
      return null;
  });

  $.RawMethod(false, "writeInodes", function (name) {
    var key = getKey(name) + "_inodes";

    try {
      this.service.setItem(key, JSON.stringify(this.inodes));
    } catch (exc) {
      this.handleFailure(exc, "inodes");
    }
  });

  $.RawMethod(false, "flush", function () {
    this.writeInodes(this.name);
  });

  $.RawMethod(false, "deleteFileBytes", function (name) {
    var key = getKey(this.name) + "_blobs_" + name;

    this.service.removeItem(key);

    JSIL.Host.logWriteLine("Deleted '" + name + "' from local storage.");
  });

  $.RawMethod(false, "getFileBytes", function (name) {
    var key = getKey(this.name) + "_blobs_" + name;

    var json = this.service.getItem(key);

    if (json) {
      JSIL.Host.logWriteLine("Loaded '" + name + "' from local storage.");

      var array = JSON.parse(json);
      if (typeof (window.Uint8Array) === "function") {
        return new Uint8Array(array);
      } else {
        return array;
      }
    } else {
      JSIL.Host.logWriteLine("Could not find '" + name + "' in local storage.");
      return null;
    }
  });

  $.RawMethod(false, "setFileBytes", function (name, value) {
    var key = getKey(this.name) + "_blobs_" + name;

    // JSON.stringify turns typed arrays into dictionaries :|

    var json = "[";
    for (var i = 0; i < value.length; i++) {
      if (i !== 0)
        json += ",";

      json += value[i];
    }
    json += "]";

    try {
      this.service.setItem(key, json);
    } catch (exc) {
      this.handleFailure(exc, name);
    }

    JSIL.Host.logWriteLine("Saved '" + name + "' to local storage.");
  });

  $.RawMethod(false, "toString", function () {
    return "<LocalStorage Volume '" + this.name + "'>";
  });
});

JSIL.LocalStorage = {};
JSIL.LocalStorage.Initialize = function (appName) {
  var volume = null;

  JSIL.RegisterStorageProvider({
    getVolumes: function () {
      if (!volume)
        volume = new LocalStorageVolume(appName, "/");

      return [volume];
    }
  });
};