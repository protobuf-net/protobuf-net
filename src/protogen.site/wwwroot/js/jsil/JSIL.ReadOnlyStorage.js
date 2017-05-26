/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

if (typeof ($jsilstorage) === "undefined")
  throw new Error("JSIL.Storage required");

var $jsilreadonlystorage = JSIL.DeclareAssembly("JSIL.ReadOnlyStorage");

JSIL.MakeClass($jsilstorage.TypeRef("VirtualVolume"), "ReadOnlyStorageVolume", true, [], function ($) {
  $.RawMethod(false, ".ctor", function (name, rootPath, initializer) {
    this.fileBytes = {};

    VirtualVolume.prototype._ctor.call(
      this, name, rootPath
    );

    initializer(this);
    this.readOnly = true;
  });

  $.RawMethod(false, "flush", function () {
  });

  $.RawMethod(false, "deleteFileBytes", function (name) {
    if (this.readOnly)
      throw new Error("Volume is read-only");

    delete this.fileBytes[name];
  });

  $.RawMethod(false, "getFileBytes", function (name) {
    return this.fileBytes[name];
  });

  $.RawMethod(false, "setFileBytes", function (name, value) {
    if (this.readOnly)
      throw new Error("Volume is read-only");

    this.fileBytes[name] = value;
  });

  $.RawMethod(false, "toString", function () {
    return "<ReadOnlyStorage Volume '" + this.name + "'>";
  });
});

JSIL.ReadOnlyStorage = {};
