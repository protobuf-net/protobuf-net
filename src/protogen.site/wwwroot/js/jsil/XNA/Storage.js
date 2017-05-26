/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

if (typeof ($jsilxna) === "undefined")
  throw new Error("JSIL.XNACore required");

if (typeof ($jsilstorage) === "undefined")
  throw new Error("JSIL.Storage required");

JSIL.ImplementExternals("Microsoft.Xna.Framework.Storage.StorageContainer", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [
          getXnaStorage().TypeRef("Microsoft.Xna.Framework.Storage.StorageDevice"), $xnaasms.xna.TypeRef("Microsoft.Xna.Framework.PlayerIndex"),
          $.String
        ], [])),
    function _ctor (device, index, displayName) {
      this.device = device;
      this.index = index;
      this.displayName = displayName;

      var volumes = JSIL.GetStorageVolumes();
      if (volumes.length > 0) {
        this.volume = volumes[0];
      }
    }
  );

  $.Method({Static:false, Public:true }, "add_Disposing",
    (new JSIL.MethodSignature(null, [$xnaasms.corlib.TypeRef("System.EventHandler`1", [$xnaasms.corlib.TypeRef("System.EventArgs")])], [])),
    function add_Disposing (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "CreateDirectory",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function CreateDirectory (directory) {
      if (!this.volume)
        throw new Error("No storage providers loaded");

      this.volume.createDirectory(directory);
    }
  );

  $.Method({Static:false, Public:true }, "CreateFile",
    (new JSIL.MethodSignature($xnaasms.corlib.TypeRef("System.IO.Stream"), [$.String], [])),
    function CreateFile (file) {
      if (!this.volume)
        throw new Error("No storage providers loaded");

      return this.OpenFileInternal(file, true);
    }
  );

  $.Method({Static:false, Public:true }, "DeleteDirectory",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function DeleteDirectory (directory) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "DeleteFile",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function DeleteFile (file) {
      if (!this.volume)
        throw new Error("No storage providers loaded");

      var resolved = this.volume.resolvePath(file, false);

      if (resolved && resolved.type === "file")
        resolved.unlink();
    }
  );

  $.Method({Static:false, Public:true }, "DirectoryExists",
    (new JSIL.MethodSignature($.Boolean, [$.String], [])),
    function DirectoryExists (directory) {
      if (this.volume) {
        var directory = this.volume.resolvePath(directory, false);
        return ((directory !== null) && (directory.type !== "file"));
      }

      return false;
    }
  );

  $.Method({Static:false, Public:true }, "Dispose",
    (JSIL.MethodSignature.Void),
    function Dispose () {
      // FIXME

      if (this.volume)
        this.volume.flush();
    }
  );

  $.Method({Static:false, Public:false}, "Dispose",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function Dispose (disposing) {
      // FIXME

      if (this.volume)
        this.volume.flush();
    }
  );

  $.Method({Static:false, Public:false}, "DisposeOverride",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function DisposeOverride (disposing) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "FileExists",
    (new JSIL.MethodSignature($.Boolean, [$.String], [])),
    function FileExists (file) {
      if (this.volume) {
        var file = this.volume.resolvePath(file, false);
        return ((file !== null) && (file.type === "file"));
      }

      // FIXME
      return false;
    }
  );

  $.Method({Static:false, Public:true }, "get_DisplayName",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_DisplayName () {
      return this.displayName;
    }
  );

  $.Method({Static:true, Public:true }, "get_TitleLocation",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_TitleLocation () {
      var storageRoot = JSIL.Host.getStorageRoot();
      if (!storageRoot)
        throw new System.Exception("Storage implementation required");

      return storageRoot.path;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsDisposed",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsDisposed () {
      // FIXME
      return false;
    }
  );

  $.Method({Static:false, Public:true }, "get_StorageDevice",
    (new JSIL.MethodSignature(getXnaStorage().TypeRef("Microsoft.Xna.Framework.Storage.StorageDevice"), [], [])),
    function get_StorageDevice () {
      return this.device;
    }
  );

  $.RawMethod(false, "$getNames", function (nodeType, searchPattern) {
      if (!this.volume)
        return [];

      var nodes = this.volume.enumerate(nodeType, searchPattern);
      var result = [];

      for (var i = 0; i < nodes.length; i++)
        result.push(nodes[i].name);

      return result;
  });

  $.Method({Static:false, Public:true }, "GetDirectoryNames",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.String]), [], [])),
    function GetDirectoryNames () {
      return this.$getNames("directory");
    }
  );

  $.Method({Static:false, Public:true }, "GetDirectoryNames",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.String]), [$.String], [])),
    function GetDirectoryNames (searchPattern) {
      return this.$getNames("directory", searchPattern);
    }
  );

  $.Method({Static:false, Public:true }, "GetFileNames",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.String]), [], [])),
    function GetFileNames () {
      return this.$getNames("file");
    }
  );

  $.Method({Static:false, Public:true }, "GetFileNames",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.String]), [$.String], [])),
    function GetFileNames (searchPattern) {
      return this.$getNames("file", searchPattern);
    }
  );

  $.RawMethod(false, "OpenFileInternal", function (filename, fileMode) {
    if (!this.volume)
      throw new Error("No storage providers loaded");

    var createNew = (fileMode == System.IO.FileMode.Create) ||
      (fileMode == System.IO.FileMode.CreateNew) ||
      (fileMode == System.IO.FileMode.OpenOrCreate);

    var file = this.volume.resolvePath(filename, !createNew);

    if (createNew && !file)
      file = this.volume.createFile(filename, true);

    var fileStream = JSIL.CreateInstanceOfType(
      System.IO.FileStream.__Type__, "$fromVirtualFile", [file, fileMode, false]
    );

    return fileStream;
  });

  $.Method({Static:false, Public:true }, "OpenFile",
    (new JSIL.MethodSignature($xnaasms.corlib.TypeRef("System.IO.Stream"), [$.String, $xnaasms.corlib.TypeRef("System.IO.FileMode")], [])),
    function OpenFile (file, fileMode) {
      return this.OpenFileInternal(
        file, fileMode
      );
    }
  );

  $.Method({Static:false, Public:true }, "OpenFile",
    (new JSIL.MethodSignature($xnaasms.corlib.TypeRef("System.IO.Stream"), [
          $.String, $xnaasms.corlib.TypeRef("System.IO.FileMode"),
          $xnaasms.corlib.TypeRef("System.IO.FileAccess")
        ], [])),
    function OpenFile (file, fileMode, fileAccess) {
      return this.OpenFileInternal(
        file, fileMode
      );
    }
  );

  $.Method({Static:false, Public:true }, "OpenFile",
    (new JSIL.MethodSignature($xnaasms.corlib.TypeRef("System.IO.Stream"), [
          $.String, $xnaasms.corlib.TypeRef("System.IO.FileMode"),
          $xnaasms.corlib.TypeRef("System.IO.FileAccess"), $xnaasms.corlib.TypeRef("System.IO.FileShare")
        ], [])),
    function OpenFile (file, fileMode, fileAccess, fileShare) {
      return this.OpenFileInternal(
        file, fileMode
      );
    }
  );

  $.Method({Static:false, Public:true }, "remove_Disposing",
    (new JSIL.MethodSignature(null, [$xnaasms.corlib.TypeRef("System.EventHandler`1", [$xnaasms.corlib.TypeRef("System.EventArgs")])], [])),
    function remove_Disposing (value) {
      // FIXME
    }
  );

});