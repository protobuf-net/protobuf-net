/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core required");

var $jsilstorage = JSIL.DeclareAssembly("JSIL.Storage");

JSIL.MakeClass($jsilcore.System.Object, "VirtualVolume", true, [], function ($) {
    $.RawMethod(false, ".ctor", function (name, rootPath, inodes) {
        this.inodes = [];
        this.name = name;
        this.readOnly = false;

        rootPath = this.normalizePath(rootPath);

        if (rootPath[rootPath.length - 1] !== "/")
            rootPath += "/";

        if (JSIL.IsArray(inodes)) {
            this.initFromInodes(rootPath, inodes);
        } else {
            this.rootDirectory = new VirtualDirectory(
              this, null, this.makeInode(null, "directory", rootPath)
            );
        }
    });

    $.RawMethod(false, "initFromInodes", function (rootPath, inodes) {
        // Create local copies of all the source inodes

        for (var i = 0, l = inodes.length; i < l; i++) {
            var inode = inodes[i], resultInode;
            if (!inode) {
                this.inodes.push(null);
                continue;
            }

            if (typeof (inode.parent) === "number") {
                resultInode = this.makeInode(this.inodes[inode.parent], inode.type, inode.name);
            } else {
                resultInode = this.makeInode(null, inode.type, inode.name);
            }

            if (inode.metadata) {
                for (var k in inode.metadata) {
                    if (!inode.metadata.hasOwnProperty(k))
                        continue;

                    resultInode.metadata[k] = inode.metadata[k];
                }
            }
        }

        this.inodes[0].name = rootPath;

        this.rootDirectory = new VirtualDirectory(
          this, null, this.inodes[0]
        );

        for (var i = 1, l = inodes.length; i < l; i++) {
            var inode = this.inodes[i];
            if (!inode)
                continue;

            var parentInode = this.inodes[inode.parent];

            switch (inode.type) {
                case "directory":
                    new VirtualDirectory(
                        this, parentInode.object, inode
                    );

                    break;

                case "file":
                    new VirtualFile(
                        parentInode.object, inode
                    );

                    break;
            }
        }
    });

    $.RawMethod(false, "makeInode", function (parent, type, name) {
        var inode = {
            type: type,
            name: name,
            metadata: {}
        };

        if (parent) {
            Object.defineProperty(
              inode, "parent", {
                  value: parent.index,
                  configurable: false,
                  enumerable: true,
                  writable: false
              }
            );
        }

        Object.defineProperty(
          inode, "index", {
              value: this.inodes.length,
              configurable: false,
              enumerable: false,
              writable: false
          }
        );

        this.inodes.push(inode);

        return inode;
    });

    $.RawMethod(false, "normalizePath", function (path) {
        if (path === null)
            return null;

        var backslashRe = /\\/g;

        path = path.replace(backslashRe, "/");

        if (this.rootDirectory) {
            var indexOfRoot = path.indexOf(this.rootDirectory.path);
            if (indexOfRoot === 0)
                path = path.substr(this.rootDirectory.path.length);
        }

        return path;
    });

    $.RawMethod(false, "unlinkInode", function (inode) {
        var toRemove = [inode.index];

        while (toRemove.length > 0) {
            var toRemoveNext = [];

            for (var i = 0, l = this.inodes.length; i < l; i++) {
                if (toRemove.indexOf(i) >= 0) {
                    this.inodes[i] = null;
                } else {
                    var current = this.inodes[i];

                    if (current) {
                        if (toRemove.indexOf(current.parent) >= 0)
                            toRemoveNext.push(i);
                    }
                }
            }

            toRemove = toRemoveNext;
        }
    });

    $.RawMethod(false, "enumerate", function (nodeType, searchPattern) {
        return this.rootDirectory.enumerate(nodeType, searchPattern);
    });

    $.RawMethod(false, "enumerateFilesRecursive", function (searchPattern) {
        var result = [];

        var step = function (directory) {
            var subdirs = directory.enumerate("directory");
            for (var i = 0; i < subdirs.length; i++)
                step(subdirs[i]);

            var files = directory.enumerate("file", searchPattern);

            for (var i = 0; i < files.length; i++)
                result.push(files[i]);
        };

        step(this.rootDirectory);

        return result;
    });

    $.RawMethod(false, "createDirectory", function (path) {
        path = this.normalizePath(path);

        var pieces = path.split("/");

        for (var i = 0, l = pieces.length; i < l; i++) {
            var containingPath = pieces.slice(0, i).join("/") + "/";

            var containingDirectory = this.rootDirectory.resolvePath(containingPath);
            containingDirectory.createDirectory(pieces[i], true);
        }

        return this.rootDirectory.resolvePath(path);
    });

    $.RawMethod(false, "createJunction", function (path, targetObject, allowExisting) {
        path = this.normalizePath(path);

        while (path[path.length - 1] === "/")
            path = path.substr(0, path.length - 1);

        var pieces = path.split("/"), containingDirectory = null, containingPath = null;

        for (var i = 0, l = pieces.length - 1; i < l; i++) {
            containingPath = pieces.slice(0, i).join("/") + "/";

            containingDirectory = this.rootDirectory.resolvePath(containingPath);
            containingDirectory.createDirectory(pieces[i], true);
        }

        containingPath = pieces.slice(0, pieces.length - 1).join("/");
        containingDirectory = this.rootDirectory.resolvePath(containingPath);
        return containingDirectory.createJunction(pieces[pieces.length - 1], targetObject, allowExisting);
    });

    $.RawMethod(false, "createFile", function (path, allowExisting, createParentDirectory) {
        path = this.normalizePath(path);

        var lastSlash = path.lastIndexOf("/"), parentDirectory, fileName;
        if (lastSlash >= 0) {
            if (createParentDirectory)
                parentDirectory = this.createDirectory(path.substr(0, lastSlash));
            else
                parentDirectory = this.rootDirectory.resolvePath(path.substr(0, lastSlash), true);

            fileName = path.substr(lastSlash + 1);
        } else {
            parentDirectory = this.rootDirectory;
            fileName = path;
        }

        return parentDirectory.createFile(fileName, allowExisting);
    });

    $.RawMethod(false, "resolvePath", function (path, throwOnFail) {
        path = this.normalizePath(path);

        return this.rootDirectory.resolvePath(path, throwOnFail);
    });

    $.RawMethod(false, "flush", function () {
        throw new Error("Not implemented");
    });

    $.RawMethod(false, "deleteFileBytes", function (name) {
        throw new Error("Not implemented");
    });

    $.RawMethod(false, "getFileBytes", function (name) {
        throw new Error("Not implemented");
    });

    $.RawMethod(false, "setFileBytes", function (name, value) {
        throw new Error("Not implemented");
    });

    $.RawMethod(false, "toString", function () {
        return "<Virtual Storage Volume '" + this.name + "'>";
    });
});
JSIL.MakeClass($jsilcore.System.Object, "VirtualDirectory", true, [], function ($) {
    $.RawMethod(false, ".ctor", function (volume, parent, inode) {
        if (inode.type !== "directory")
            throw new Error("Inode is not a directory");

        this.volume = volume;
        this.parent = parent;
        this.inode = inode;

        JSIL.SetValueProperty(
          this, "directories", {}
        );
        JSIL.SetValueProperty(
          this, "files", {}
        );

        Object.defineProperty(
          this, "name", {
              configurable: false,
              get: function () {
                  return inode.name;
              }
          }
        );

        Object.defineProperty(
          this, "type", {
              get: function () {
                  return inode.type;
              }
          }
        );

        JSIL.SetValueProperty(
          this, "path",
          parent ?
            parent.path + (this.name + "/") :
            this.name
        );

        Object.defineProperty(
          this.inode, "object", {
              value: this,
              enumerable: false,
              configurable: false,
              writable: false
          }
        );

        if (!this.inode.metadata.created)
            this.inode.metadata.created = JSIL.Host.getFileTime();

        if (parent)
            parent.directories[this.name.toLowerCase()] = this;
    });

    $.RawMethod(false, "getFile", function (name) {
        var file = this.files[name.toLowerCase()];
        if (!file)
            return null;

        return file;
    });

    $.RawMethod(false, "getDirectory", function (name) {
        if ((name === ".") || (name === ""))
            return this;
        else if (name === "..")
            return this.parent;

        var directory = this.directories[name.toLowerCase()];
        if (!directory)
            return null;

        return directory;
    });

    $.RawMethod(false, "unlink", function () {
        // FIXME: Call .unlink() on child directories/files instead of relying on unlinkInode.
        // Right now this will leak file bytes for child files.

        delete this.parent.directories[this.name.toLowerCase()];

        this.volume.unlinkInode(this.inode);
        this.volume.flush();
    });

    $.RawMethod(false, "createFile", function (name, allowExisting) {
        var existingFile = this.getFile(name);
        if (existingFile) {
            if (allowExisting)
                return existingFile;
            else
                throw new Error("A file named '" + name + "' already exists.");
        }

        if (this.volume.readOnly)
            throw new Error("The volume is read-only.");

        return new VirtualFile(
          this, this.volume.makeInode(this.inode, "file", name)
        );
    });

    $.RawMethod(false, "createDirectory", function (name, allowExisting) {
        var existingDirectory = this.getDirectory(name);
        if (existingDirectory) {
            if (allowExisting)
                return existingDirectory;
            else
                throw new Error("A directory named '" + name + "' already exists.");
        }

        if (this.volume.readOnly)
            throw new Error("The volume is read-only.");

        return new VirtualDirectory(
          this.volume, this, this.volume.makeInode(this.inode, "directory", name)
        );
    });

    $.RawMethod(false, "createJunction", function (name, targetObject, allowExisting) {
        var existingDirectory = this.getDirectory(name);
        if (existingDirectory) {
            if (allowExisting)
                return existingDirectory;
            else
                throw new Error("A directory named '" + name + "' already exists.");
        }

        if ((typeof (targetObject) !== "object") || (targetObject.type !== "directory"))
            throw new Error("Target for junction must be a directory object");

        if (this.volume.readOnly)
            throw new Error("The volume is read-only.");

        return new VirtualJunction(
          this.volume, this, name, targetObject
        );
    });

    $.RawMethod(false, "resolvePath", function (path, throwOnFail) {
        if (typeof (throwOnFail) === "undefined")
            throwOnFail = true;

        if (path === null) {
            if (throwOnFail)
                throw new Error("path was null");
            else
                return null;
        }

        var firstSlash = path.indexOf("/"), itemName, childPath;

        if (firstSlash >= 0) {
            itemName = path.substr(0, firstSlash);
            childPath = path.substr(firstSlash);
        } else {
            itemName = path;
            childPath = null;
        }

        var forceDirectory = childPath === "/";
        if (forceDirectory) {
            childPath = null;
        } else if (childPath) {
            while (childPath[0] === "/")
                childPath = childPath.substr(1);
        }

        var result = this.getDirectory(itemName);
        if (result === null) {
            if (forceDirectory) {
                if (throwOnFail)
                    throw new Error("No directory named '" + itemName + "' could be found in directory '" + this.path + "'.");
                else
                    result = null;
            } else
                result = this.getFile(itemName);
        }

        if (childPath) {
            if (result) {
                return result.resolvePath(childPath, throwOnFail);
            } else {
                if (throwOnFail)
                    throw new Error("No directory named '" + itemName + "' could be found in directory '" + this.path + "'.");
                else
                    return null;
            }
        } else {
            if (!result && throwOnFail)
                throw new Error("No file or directory named '" + itemName + "' could be found in directory '" + this.path + "'.");

            return result;
        }
    });

    $.RawMethod(false, "enumerate", function (nodeType, searchPattern) {
        var result = [];
        var predicate = function (fn) { return true; };

        if (searchPattern) {
            var starRegex = /\*/g;
            var questionMarkRegex = /\?/g;
            var dotRegex = /\./g

            var regexText = searchPattern
              .replace(dotRegex, "\\.")
              .replace(starRegex, "(.*)")
              .replace(questionMarkRegex, ".");

            var regex = new RegExp(regexText, "i");

            predicate = function (fn) {
                return regex.test(fn);
            };
        }

        if (nodeType !== "directory") {
            for (var k in this.files) {
                if (predicate(k))
                    result.push(this.files[k]);
            }
        }

        if (nodeType !== "file") {
            for (var k in this.directories) {
                if ((nodeType === "junction") && (this.directories[k].type !== "junction"))
                    continue;

                if (predicate(k))
                    result.push(this.directories[k])
            }
        }

        return result;
    });

    $.RawMethod(false, "toString", function () {
        return "<Virtual Directory '" + this.path + "' in volume '" + this.volume.name + "'>";
    });
});
JSIL.MakeClass($jsilcore.System.Object, "VirtualFile", true, [], function ($) {
    $.RawMethod(false, ".ctor", function (parent, inode) {
        if (inode.type !== "file")
            throw new Error("Inode is not a file");

        this.parent = parent;
        this.inode = inode;

        Object.defineProperty(
          this, "name", {
              configurable: false,
              get: function () {
                  return inode.name;
              }
          }
        );

        Object.defineProperty(
          this, "type", {
              get: function () {
                  return inode.type;
              }
          }
        );

        JSIL.SetValueProperty(
          this, "path", parent.path + this.name
        );

        Object.defineProperty(
          this.inode, "object", {
              value: this,
              enumerable: false,
              configurable: false,
              writable: false
          }
        );

        Object.defineProperty(
          this, "volume", {
              get: function () {
                  return this.parent.volume;
              },
              enumerable: false,
              configurable: false
          }
        );

        if (!this.inode.metadata.created)
            this.inode.metadata.created = JSIL.Host.getFileTime();

        parent.files[this.name.toLowerCase()] = this;
    });

    $.RawMethod(false, "unlink", function () {
        delete this.parent.files[this.name.toLowerCase()];
        this.volume.unlinkInode(this.inode);
        this.volume.deleteFileBytes(this.path);
        this.volume.flush();
    });

    $.RawMethod(false, "readAllBytes", function () {
        var bytes = this.volume.getFileBytes(this.path);

        this.inode.metadata.lastRead = JSIL.Host.getFileTime();

        if (!bytes)
            return JSIL.Array.New(System.Byte, this.inode.metadata.length || 0);

        return bytes;
    });

    $.RawMethod(false, "writeAllBytes", function (bytes) {
        this.volume.setFileBytes(this.path, bytes);

        this.inode.metadata.lastWritten = JSIL.Host.getFileTime();
        this.inode.metadata.length = bytes.length;
    });

    $.RawMethod(false, "toString", function () {
        return "<Virtual File '" + this.path + "' in volume '" + this.volume.name + "'>";
    });
});
JSIL.MakeClass($jsilcore.System.Object, "VirtualJunction", true, [], function ($) {
    $.RawMethod(false, ".ctor", function (volume, parent, name, targetObject) {
        this.volume = volume;
        this.parent = parent;
        this.target = targetObject;
        this.inode = null;
        this.name = name;

        JSIL.SetValueProperty(
          this, "path",
          parent ?
            parent.path + (this.name + "/") :
            this.name
        );

        if (parent)
            parent.directories[this.name.toLowerCase()] = this;
    });

    $.RawMethod(false, "getFile", function (name) {
        return this.target.getFile(name);
    });

    $.RawMethod(false, "getDirectory", function (name) {
        return this.target.getDirectory(name);
    });

    $.RawMethod(false, "createFile", function (name, allowExisting) {
        return this.target.createFile(name, allowExisting);
    });

    $.RawMethod(false, "createDirectory", function (name, allowExisting) {
        return this.target.createDirectory(name, allowExisting);
    });

    $.RawMethod(false, "resolvePath", function (path, throwOnFail) {
        return this.target.resolvePath(path, throwOnFail);
    });

    $.RawMethod(false, "enumerate", function (nodeType, searchPattern) {
        return this.target.enumerate(nodeType, searchPattern);
    });

    $.RawMethod(false, "toString", function () {
        return "<Virtual Junction '" + this.path + "' in volume '" + this.volume.name + "' pointing to '" + this.target.path + "'>";
    });
});

JSIL.ImplementExternals("System.IO.FileStream", function ($) {
  $.RawMethod(false, "$fromVirtualFile", function (virtualFile, fileMode, autoFlush) {
    System.IO.Stream.prototype._ctor.call(this);

    this._fileName = virtualFile.path;
    this._buffer = JSIL.Array.Clone(virtualFile.readAllBytes());

    this._pos = 0;
    this._length = this._buffer.length;

    this._canRead = true;
    this._canWrite = true;

    this._onClose = function () {
      if (this._modified && this._buffer) {
        var resultBuffer = JSIL.Array.New(System.Byte, this._length);
        JSIL.Array.CopyTo(this._buffer, resultBuffer, 0);

        virtualFile.writeAllBytes(resultBuffer);

        if (autoFlush)
          virtualFile.volume.flush();

        this._buffer = null;
      }
    };

    this.$applyMode(fileMode);
  });
});

$jsilstorage.providers = [];

JSIL.RegisterStorageProvider = function (provider) {
    $jsilstorage.providers.push(provider);
};

JSIL.GetStorageVolumes = function () {
    var result = [];

    for (var i = 0, l = $jsilstorage.providers.length; i < l; i++) {
        var provider = $jsilstorage.providers[i];

        var volumes = provider.getVolumes();

        for (var j = 0, m = volumes.length; j < m; j++)
            result.push(volumes[j]);
    }

    return result;
};