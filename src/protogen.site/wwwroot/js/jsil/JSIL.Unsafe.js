/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core is required");

if (!$jsilcore)
  throw new Error("JSIL.Core is required");

JSIL.$StructSizeOverrides = {
  "System.Int64": 8,
  "System.UInt64": 8
};

JSIL.DeclareNamespace("JSIL.Runtime");
JSIL.DeclareNamespace("JSIL.PackedArray");

JSIL.ImplementExternals("System.IntPtr", function ($) {
  var tIntPtr = $.IntPtr;

  $.RawMethod(false, "$fromPointer", function (pointer) {
    this.pointer = pointer;
    this.value = null;
  });

  $.RawMethod(false, "$fromInt32", function (int32) {
    this.pointer = null;
    this.value   = int32 | 0;
  });

  $.RawMethod(false, "$fromInt64", function (int64) {
    this.pointer = null;
    this.value   = int64.ToNumber() | 0;
  });

  $.RawMethod(true, ".cctor", function () {
    System.IntPtr.Zero = new System.IntPtr();
  });

  $.Method({Static:false, Public:false }, ".ctor",
    (new JSIL.MethodSignature(null, [], [])),
    function _ctor () {
      this.pointer = null;
      this.value = 0;
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function _ctor (value) {
      this.pointer = null;
      this.value = value | 0;
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Int64], [])),
    function _ctor (value) {
      this.pointer = null;
      this.value = value.ToNumber() | 0;
    }
  );

  $.RawMethod(false, "__CopyMembers__",
    function IntPtr_CopyMembers (source, target) {
      target.value = source.value;
      if (!source.pointer)
        target.pointer = null;
      else
        target.pointer = source.pointer;
    }
  );

  function isNullPointer(p) {
    return (!p.pointer && p.value === 0) ||
           (p.pointer && p.pointer.offsetInBytes === 0);
  };

  $.Method({Static:true , Public:true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [tIntPtr, tIntPtr], [])),
    function op_Equality (lhs, rhs) {
      // Null pointers always equal, regardless of where they came from
      if (isNullPointer(lhs) && isNullPointer(rhs)) {
        return true;
      }

      if (lhs.pointer) {
        if (!rhs.pointer) // Non-null emscripten pointers can't equal C# ones
          return false;

        return rhs.pointer.equals(lhs.pointer);
      } else {
        return lhs.value === rhs.value;
      }
    }
  );

  $.Method({Static:true , Public:true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [tIntPtr, tIntPtr], [])),
    function op_Inequality (lhs, rhs) {
      if (isNullPointer(lhs) && isNullPointer(rhs)) {
        return false;
      }

      if (lhs.pointer) {
        if (!rhs.pointer)
          return true;

        return !rhs.pointer.equals(lhs.pointer);
      } else {
        return lhs.value !== rhs.value;
      }
    }
  );

  $.Method({Static:true , Public:true }, "op_Addition",
    (new JSIL.MethodSignature(tIntPtr, [tIntPtr, $.Int32], [])),
    function op_Addition (lhs, rhs) {
      if (lhs.pointer) {
        var newPointer = lhs.pointer.add(rhs, false);

        return JSIL.CreateInstanceOfType(
          System.IntPtr.__Type__,
          "$fromPointer",
          [newPointer]
        );
      } else {
        return JSIL.CreateInstanceOfType(
          System.IntPtr.__Type__,
          "$fromInt32",
          [lhs.value + rhs]
        );
      }
    }
  );

  $.Method({Static:false, Public:true }, "ToInt32",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function ToInt32 () {
      if (this.pointer) {
        return this.pointer.offsetInBytes;
      } else {
        return this.value;
      }
    }
  );

  $.Method({Static:false, Public:true }, "ToInt64",
    (new JSIL.MethodSignature($.Int64, [], [])),
    function ToInt64 () {
      if (this.pointer) {
        return $jsilcore.System.Int64.FromInt32(
          this.pointer.offsetInBytes
        );
      } else {
        return $jsilcore.System.Int64.FromInt32(
          this.value
        );
      }
    }
  );

  $.Method({Static:true , Public:true }, "op_Explicit",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.IntPtr"), [$.Int32]),
    function op_Explicit (value) {
      return JSIL.CreateInstanceOfType(
        System.IntPtr.__Type__,
        "$fromInt32",
        [value]
      );
    }
  );

  $.Method({Static:true , Public:true }, "op_Explicit",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.IntPtr"), [$.Int64]),
    function op_Explicit (value) {
      return JSIL.CreateInstanceOfType(
        System.IntPtr.__Type__,
        "$fromInt64",
        [value]
      );
    }
  );

  $.Method({Static:true , Public:true }, "op_Explicit",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.IntPtr"), [$jsilcore.TypeRef("JSIL.Pointer", [$jsilcore.TypeRef("System.Void")])]),
    function op_Explicit (value) {
      return JSIL.CreateInstanceOfType(
        System.IntPtr.__Type__,
        "$fromPointer",
        [value]
      );
    }
  );

  $.Method({Static:true , Public:true }, "op_Explicit",
    new JSIL.MethodSignature($jsilcore.TypeRef("JSIL.Pointer", [$jsilcore.TypeRef("System.Void")]), [$jsilcore.TypeRef("System.IntPtr")]),
    function op_Explicit (value) {
      if (value.pointer) {
        // FIXME: Cast it?
        return value.pointer;
      } else {
        throw new Error('Not implemented');
      }
    }
  );

  $.Method({Static:true , Public:true }, "op_Explicit",
    new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.IntPtr")]),
    function op_Explicit (value) {
      if (value.pointer) {
        return value.pointer.offsetInBytes;
      } else {
        return value.value;
      }
    }
  );

  $.Method({Static:true , Public:true }, "op_Explicit",
    new JSIL.MethodSignature($.Int64, [$jsilcore.TypeRef("System.IntPtr")]),
    function op_Explicit (value) {
      if (value.pointer) {
        return $jsilcore.System.Int64.FromInt32(value.pointer.offsetInBytes);
      } else {
        return $jsilcore.System.Int64.FromInt32(value.value);
      }
    }
  );
});

JSIL.ImplementExternals("System.UIntPtr", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.UInt32], [])),
    function _ctor (value) {
      this.value = value >>> 0;
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.UInt64], [])),
    function _ctor (value) {
      this.value = value.ToNumber() >>> 0;
    }
  );

  $.Method({Static:false, Public:true }, "ToUInt32",
    (new JSIL.MethodSignature($.UInt32, [], [])),
    function ToUInt32 () {
      return this.value;
    }
  );

  $.Method({Static:false, Public:true }, "ToUInt64",
    (new JSIL.MethodSignature($.UInt64, [], [])),
    function ToUInt64 () {
      return $jsilcore.System.UInt64.FromUInt32(this.value);
    }
  );
});

JSIL.MakeStruct("System.ValueType", "System.IntPtr", true, [], function ($) {
  $.Field({Static:false, Public:false }, "value", $.Int32);

  $.Field({Static:true, Public:true }, "Zero", $.Type);
});

JSIL.MakeStruct("System.ValueType", "System.UIntPtr", true, [], function ($) {
  $.Field({Static:false, Public:false }, "value", $.UInt32);

  $.Field({Static:true, Public:true }, "Zero", $.Type);
});

JSIL.MakeStruct("System.ValueType", "System.Void", true, [], function ($) {
});

JSIL.DeclareNamespace("System.Runtime.InteropServices");

JSIL.ImplementExternals("System.Runtime.InteropServices.Marshal", function ($) {
  $.Method({Static:true , Public:true }, "StructureToPtr",
    (new JSIL.MethodSignature(null, [
          $.Object, $.IntPtr,
          $.Boolean
        ], [])),
    function StructureToPtr (structure, ptr, fDeleteOld) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "SizeOf",
    (new JSIL.MethodSignature($.Int32, [$.Object], [])),
    function SizeOf (structure) {
      var type = JSIL.GetType(structure);
      return JSIL.GetNativeSizeOf(type, true);
    }
  )

  $.Method({Static:true , Public:true }, "SizeOf",
    (new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.Type")], [])),
    function SizeOf (type) {
      return JSIL.GetNativeSizeOf(type, true);
    }
  );

  $.Method({Static:true , Public:true }, "OffsetOf",
    (new JSIL.MethodSignature($.IntPtr, [$jsilcore.TypeRef("System.Type"), $.String], [])),
    function OffsetOf (type, fieldName) {
      var fields = JSIL.GetFieldList(type);

      for (var i = 0, l = fields.length; i < l; i++) {
        var field = fields[i];
        if (field.name === fieldName)
          return new System.IntPtr(field.offsetBytes);
      }

      throw new System.Exception("No field named '" + fieldName + "' declared in type");
    }
  );

  $.Method({Static:true , Public:true }, "Copy",
    new JSIL.MethodSignature(null, [
        $.IntPtr, $jsilcore.TypeRef("System.Array", [$.Byte]),
        $.Int32, $.Int32
      ]),
    function Copy (source, destination, startIndex, length) {
      if (!source.pointer)
        JSIL.RuntimeError("Source argument must be a pointer into a pinned buffer, not a raw value");

      var pSource = source.pointer.cast($jsilcore.System.Byte);
      for (var i = 0, l = length | 0, s = startIndex | 0; i < l; i++) {
        var d = (s + i) | 0;

        destination[d] = pSource.getElement(i);
      }
    }
  );

  $.Method({Static:true , Public:true }, "Copy",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32, $.IntPtr, $.Int32
      ]),
    function Copy (source, startIndex, destination, length) {
      if (!destination.pointer)
        JSIL.RuntimeError("Destination argument must be a pointer into a pinned buffer, not a raw value");

      var pDest = destination.pointer.cast($jsilcore.System.Byte);
      for (var i = 0, l = length | 0, s = startIndex | 0; i < l; i++) {
        pDest.setElement(
          i,
          source[(s + i) | 0]
        );
      }
    }
  );

  var mallocImpl = function (sizeBytes) {
    var module = JSIL.PInvoke.GetDefaultModule();
    var address = module._malloc(sizeBytes | 0);
    var result = JSIL.PInvoke.CreateIntPtrForModule(module, address);
    return result;
  };

  $.Method({Static:true , Public:true }, "AllocHGlobal",
    new JSIL.MethodSignature($.IntPtr, [$.IntPtr]),
    function AllocHGlobal (cb) {
      return mallocImpl(cb.ToInt32());
    }
  );

  $.Method({Static:true , Public:true }, "AllocHGlobal",
    new JSIL.MethodSignature($.IntPtr, [$.Int32]),
    function AllocHGlobal (cb) {
      return mallocImpl(cb);
    }
  );

  $.Method({Static:true , Public:true }, "FreeHGlobal",
    JSIL.MethodSignature.Action($.IntPtr),
    function FreeHGlobal (hglobal) {
      var module = JSIL.PInvoke.PickModuleForPointer(hglobal, true);
      module._free(hglobal.ToInt32());
    }
  );

});

JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.ValueType"),
    Name: "System.Runtime.InteropServices.GCHandle",
    IsPublic: true,
    IsReferenceType: false,
    MaximumConstructorArguments: 2,
  }, function ($interfaceBuilder) {
  }
);

JSIL.ImplementExternals("System.Runtime.InteropServices.GCHandle", function ($) {
  $.RawMethod(false, "$internalCtor", function (obj) {
    this._target = obj;

    if (obj && obj.__ThisType__ && obj.__ThisType__.__IsDelegate__) {
      this._pointer = obj.$pin();
    } else {
      this._pointer = JSIL.PinAndGetPointer(obj, 0);
    }
  });

  $.RawMethod(false, "__CopyMembers__",
    function GCHandle_CopyMembers (source, target) {
      target._pointer = source._pointer;
      target._target = source._target;
    }
  );

  $.Method({Static:false, Public:true }, "AddrOfPinnedObject",
    new JSIL.MethodSignature($.IntPtr, [], []),
    function AddrOfPinnedObject () {
      return JSIL.CreateInstanceOfType(
        System.IntPtr.__Type__,
        "$fromPointer",
        [this._pointer]
      );
    }
  );

  $.Method({Static:true , Public:true }, "Alloc",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.InteropServices.GCHandle"), [$.Object], []),
    function Alloc (value) {
      return JSIL.CreateInstanceOfType(
        System.Runtime.InteropServices.GCHandle.__Type__,
        "$internalCtor",
        [value]
      );
    }
  );

  $.Method({Static:true , Public:true }, "Alloc",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.InteropServices.GCHandle"), [$.Object, $jsilcore.TypeRef("System.Runtime.InteropServices.GCHandleType")], []),
    function Alloc (value, type) {
      // FIXME: type

      return JSIL.CreateInstanceOfType(
        System.Runtime.InteropServices.GCHandle.__Type__,
        "$internalCtor",
        [value]
      );
    }
  );

  $.Method({Static:false, Public:true }, "Free",
    JSIL.MethodSignature.Void,
    function Free () {
      if (this._target && this._target.__ThisType__.__IsDelegate__) {
        this._target.$unpin();
      } else {
        // FIXME: Unpin
      }

      this._pointer = null;
      this._target = null;
    }
  );
});

JSIL.MakeEnum(
  {
    FullName: "System.Runtime.InteropServices.GCHandleType",
    BaseType: $jsilcore.TypeRef("System.Int32"),
    IsPublic: true,
    IsFlags: false,
  },
  {
    Weak: 0,
    WeakTrackResurrection: 1,
    Normal: 2,
    Pinned: 3,
  }
);

JSIL.ImplementExternals("System.Buffer", function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.Method({Static:true , Public:true }, "BlockCopy",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array"), $.Int32,
        $jsilcore.TypeRef("System.Array"), $.Int32,
        $.Int32
      ], []),
    function BlockCopy (src, srcOffset, dst, dstOffset, count) {
      var srcBuffer = JSIL.GetArrayBuffer(src);
      srcOffset += JSIL.GetArrayByteOffset(src);

      var dstBuffer = JSIL.GetArrayBuffer(dst);
      dstOffset += JSIL.GetArrayByteOffset(dst);

      var srcView = new Uint8Array(srcBuffer, srcOffset, count);
      var dstView = new Uint8Array(dstBuffer, dstOffset, count);

      dstView.set(srcView);
    }
  );

  $.Method({Static:true , Public:true }, "ByteLength",
    new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.Array")], []),
    function ByteLength (array) {
      return JSIL.GetArrayByteLength(array);
    }
  );

  $.Method({Static:true , Public:true }, "GetByte",
    new JSIL.MethodSignature($.Byte, [$jsilcore.TypeRef("System.Array"), $.Int32], []),
    function GetByte (array, index) {
      var buffer = JSIL.GetArrayBuffer(array);
      index += JSIL.GetArrayByteOffset(array);

      var view = new Uint8Array(buffer, index, 1);
      return view[0];
    }
  );

});

JSIL.MakeStaticClass("System.Runtime.InteropServices.Marshal", true, [], function ($) {
});

JSIL.MakeClass("System.Object", "JSIL.MemoryRange", true, [], function ($) {
  $.RawMethod(false, ".ctor",
    function MemoryRange_ctor (buffer, offset, length) {
      this.buffer = buffer;
      this.offset = offset | 0;

      if (typeof (length) === "number") {
        this.length = length | 0;
      } else {
        this.length = buffer.byteLength;
      }

      if (this.offset < 0)
        JSIL.RuntimeError("MemoryRange offset must be >= 0");
      else if (this.length < 0)
        JSIL.RuntimeError("MemoryRange length must be >= 0");

      if (typeof (Map) !== "undefined") {
        this.viewCache = new Map();
        this.viewCacheIsMap = true;
      } else {
        this.viewCache = JSIL.CreateDictionaryObject(null);
        this.viewCacheIsMap = false;
      }

      this.viewCacheByElementType = JSIL.CreateDictionaryObject(null);
    }
  );

  $.RawMethod(false, "getCachedView",
    function MemoryRange_getCachedView (key) {
      if (this.viewCacheIsMap) {
        return this.viewCache.get(key);
      } else {
        var ctorKey = key.name || String(key.constructor);
        return this.viewCache[ctorKey];
      }
    }
  );

  $.RawMethod(false, "setCachedView",
    function MemoryRange_setCachedView (key, value) {
      if (this.viewCacheIsMap) {
        this.viewCache.set(key, value);
      } else {
        var ctorKey = key.name || String(key.constructor);
        this.viewCache[ctorKey] = value;
      }
    }
  );

  $.RawMethod(false, "storeExistingView",
    function MemoryRange_storeExistingView (view) {
      var arrayCtor = Object.getPrototypeOf(view);

      var cachedView = this.getCachedView(arrayCtor);
      if (cachedView) {
        // FIXME: I don't think this is actually a problem?
        //  I'm not really sure why it happens, though...
        /*
        if (cachedView !== view)
          JSIL.RuntimeError("A different view is already stored for this element type");
        */
      } else {
        this.setCachedView(arrayCtor, view);
      }
    }
  );

  $.RawMethod(false, "getView",
    function MemoryRange_getView (elementTypeObject, byteFallback) {
      var arrayCtor = JSIL.GetTypedArrayConstructorForElementType(elementTypeObject, byteFallback);
      if (!arrayCtor)
        return null;

      var result = this.getCachedView(arrayCtor);
      if (!result) {
        var elementCount = (this.length / arrayCtor.BYTES_PER_ELEMENT) | 0;
        result = new arrayCtor(this.buffer, this.offset, elementCount);
        this.setCachedView(arrayCtor, result);
      }

      return result;
    }
  );
});

JSIL.MakeStruct("System.ValueType", "JSIL.Pointer", true, ["T"], function ($) {
  var shiftTable = [];
  for (var i = 0; i < 256; i++) {
    shiftTable[i] = (Math.log(i) / Math.LN2) | 0;
  }

  $.SetValue("__IsPointer__", true);

  function Pointer_ctor (elementType, memoryRange, view, offsetInBytes) {
    if (arguments.length !== 4)
      JSIL.RuntimeError("Pointer ctor expects (elementType, memoryRange, view, offsetInBytes)");

    this.memoryRange = memoryRange;
    this.view = view;
    this.offsetInBytes = offsetInBytes | 0;

    if (this.view) {
      this.shift = shiftTable[this.view.BYTES_PER_ELEMENT] | 0;
    } else {
      this.shift = 0;
    }

    this.offsetInElements = offsetInBytes >> this.shift;
    this.elementType = elementType;
  };

  $.RawMethod(false, ".ctor", Pointer_ctor);

  $.RawMethod(false, "__CopyMembers__",
    function Pointer_CopyMembers (source, target) {
      target.memoryRange = source.memoryRange;
      target.view = source.view;
      target.offsetInBytes = source.offsetInBytes;
      target.offsetInElements = source.offsetInElements;
      target.shift = source.shift;
      target.elementType = source.elementType;
    }
  );

  $.RawMethod(false, "get",
    function Pointer_Get () {
      return this.view[this.offsetInElements];
    }
  );

  $.RawMethod(false, "set",
    function Pointer_Set (value) {
      this.view[this.offsetInElements] = value;
    }
  );

  $.RawMethod(false, "getElement",
    function Pointer_GetElement (offsetInElements) {
      return this.view[(this.offsetInElements + offsetInElements) | 0];
    }
  );

  $.RawMethod(false, "setElement",
    function Pointer_SetElement (offsetInElements, value) {
      this.view[(this.offsetInElements + offsetInElements) | 0] = value;
    }
  );

  $.RawMethod(false, "getOffset",
    function Pointer_GetOffset (offsetInBytes) {
      var index = ((this.offsetInBytes + offsetInBytes) | 0) >> this.shift;
      return this.view[index];
    }
  );

  $.RawMethod(false, "setOffset",
    function Pointer_SetOffset (offsetInBytes, value) {
      var index = ((this.offsetInBytes + offsetInBytes) | 0) >> this.shift;
      this.view[index] = value;
    }
  );

  $.RawMethod(false, "cast",
    function Pointer_Cast (elementType) {
      var typeObject =
        elementType.__Type__ || elementType;

      var view = this.memoryRange.getView(typeObject, true);
      return JSIL.NewPointer(typeObject, this.memoryRange, view, this.offsetInBytes);
    }
  );

  $.RawMethod(false, "asView",
    function Pointer_asView (elementType, sizeInBytes) {
      var underlyingBufferSize = (this.memoryRange.buffer.byteLength - this.offsetInBytes) | 0;

      // FIXME: Maybe make this null instead? Int-only is probably better.
      sizeInBytes = sizeInBytes | 0;
      if (sizeInBytes === -1)
        sizeInBytes = underlyingBufferSize;

      var arrayCtor = JSIL.GetTypedArrayConstructorForElementType(elementType.__Type__, true);
      var offsetInElements = (this.offsetBytes / arrayCtor.BYTES_PER_ELEMENT) | 0;
      var sizeInElements = ((sizeInBytes | 0) / arrayCtor.BYTES_PER_ELEMENT) | 0;

      if ((this.offsetInBytes % arrayCtor.BYTES_PER_ELEMENT) !== 0)
        JSIL.RuntimeError("Pointer must be element-aligned");
      if ((sizeInBytes % arrayCtor.BYTES_PER_ELEMENT) !== 0)
        JSIL.RuntimeError("Size must be an integral multiple of element size");

      // Where possible, return a cached view to avoid creating garbage.
      // FIXME: Maybe don't even apply the size constraint here?
      if ((offsetInElements === 0) && (sizeInBytes === underlyingBufferSize))
        return this.memoryRange.getView(elementType.__Type__, true);

      var view = new arrayCtor(this.memoryRange.buffer, offsetInElements, sizeInElements);
      return view;
    }
  );

  $.RawMethod(false, "add",
    function Pointer_Add (offsetInBytes, modifyInPlace) {
      if (modifyInPlace === true) {
        this.offsetInBytes = (this.offsetInBytes + offsetInBytes) | 0;
        this.offsetInElements = this.offsetInBytes >> this.shift;
      } else {
        var ctor = this.__ThisType__.__PublicInterface__;
        return new (ctor)(
          this.elementType, this.memoryRange, this.view, (this.offsetInBytes + offsetInBytes) | 0
        );
      }
    }
  );

  $.RawMethod(false, "addElements",
    function Pointer_AddElements (offsetInElements, modifyInPlace) {
      if (modifyInPlace === true) {
        this.offsetInElements = (this.offsetInElements + offsetInElements) | 0;
        this.offsetInBytes = (this.offsetInBytes + (offsetInElements << this.shift)) | 0;
      } else {
        var ctor = this.__ThisType__.__PublicInterface__;
        return new (ctor)(
          this.elementType, this.memoryRange, this.view, (this.offsetInBytes + (offsetInElements << this.shift)) | 0
        );
      }
    }
  );

  $.RawMethod(false, "deltaBytes",
    function Pointer_DeltaBytes (otherPointer) {
      if (otherPointer.memoryRange.buffer !== this.memoryRange.buffer)
        JSIL.RuntimeError("Cannot subtract two pointers from different pinned buffers");

      return (this.offsetInBytes - otherPointer.offsetInBytes) | 0;
    }
  );

  function getBuffer (ptr) {
    if (ptr.memoryRange !== null)
      return ptr.memoryRange.buffer;
    else
      return null;
  };

  $.RawMethod(false, "equals",
    function Pointer_Equals (rhs) {
      if (rhs === null)
        return false;
      else if (rhs === this)
        return true;
      else
        return (getBuffer(this) === getBuffer(rhs)) &&
          (this.offsetInBytes === rhs.offsetInBytes);
    }
  );

  $.RawMethod(false, "lessThan",
    function Pointer_LessThan (rhs) {
      if (rhs === null)
        return false;
      else
        return (getBuffer(this) === getBuffer(rhs)) &&
          (this.offsetInBytes < rhs.offsetInBytes);
    }
  );

  $.RawMethod(false, "greaterThan",
    function Pointer_GreaterThan (rhs) {
      if (rhs === null)
        return false;
      else
        return (getBuffer(this) === getBuffer(rhs)) &&
          (this.offsetInBytes > rhs.offsetInBytes);
    }
  );

  $.Method({ Static: false, Public: true }, "Object.Equals",
    new JSIL.MethodSignature($.Boolean, [$.Object]),
    function Pointer_Equals (rhs) {
      return this.equals(rhs);
    }
  );

  $.RawMethod(false, "toString",
    function Pointer_ToString () {
      return "<ptr " + this.view + " + " + this.offsetInBytes + ">";
    }
  );
});

JSIL.MakeStruct("JSIL.Pointer", "JSIL.VoidPointer", true, [], function ($) {
});

JSIL.MakeStruct("JSIL.Pointer", "JSIL.NullPointer", true, [], function ($) {
  $.SetValue("__IsNull__", true);

  function NullPointer_ctor (elementType) {
    if (arguments.length !== 1)
      JSIL.RuntimeError("NullPointer ctor expects (elementType)");

    this.memoryRange = null;
    this.view = null;
    this.offsetInBytes = 0;
    this.shift = 0;
    this.offsetInElements = 0;
    this.elementType = elementType;
  };

  $.RawMethod(false, ".ctor", NullPointer_ctor);

  $.RawMethod(false, "toString",
    function NullPointer_ToString () {
      return "<null ptr>";
    }
  );
});

JSIL.MakeStruct("JSIL.Pointer", "JSIL.WordPointer", true, [], function ($) {
  $.RawMethod(false, "get",
    function WordPointer_Get () {
      return this.view[this.offsetInElements];
    }
  );

  $.RawMethod(false, "set",
    function WordPointer_Set (value) {
      this.view[this.offsetInElements] = value;
    }
  );

  $.RawMethod(false, "getElement",
    function WordPointer_GetElement (offsetInElements) {
      return this.view[(this.offsetInElements + offsetInElements) | 0];
    }
  );

  $.RawMethod(false, "setElement",
    function WordPointer_SetElement (offsetInElements, value) {
      this.view[(this.offsetInElements + offsetInElements) | 0] = value;
    }
  );

  $.RawMethod(false, "getOffset",
    function WordPointer_GetOffset (offsetInBytes) {
      return this.view[((this.offsetInBytes + offsetInBytes) | 0) >> 1];
    }
  );

  $.RawMethod(false, "setOffset",
    function WordPointer_SetOffset (offsetInBytes, value) {
      this.view[((this.offsetInBytes + offsetInBytes) | 0) >> 1] = value;
    }
  );
});

(function () {
  function DoubleWordPointer_Get () {
    return this.view[this.offsetInElements];
  };

  function DoubleWordPointer_Set (value) {
    this.view[this.offsetInElements] = value;
  };

  function DoubleWordPointer_GetElement (offsetInElements) {
    return this.view[(this.offsetInElements + offsetInElements) | 0];
  };

  function DoubleWordPointer_SetElement (offsetInElements, value) {
    this.view[(this.offsetInElements + offsetInElements) | 0] = value;
  };

  function DoubleWordPointer_GetOffset (offsetInBytes) {
    return this.view[((this.offsetInBytes + offsetInBytes) | 0) >> 2];
  };

  function DoubleWordPointer_SetOffset (offsetInBytes, value) {
    this.view[((this.offsetInBytes + offsetInBytes) | 0) >> 2] = value;
  };

  JSIL.MakeStruct("JSIL.Pointer", "JSIL.DoubleWordPointer", true, [], function ($) {
    $.RawMethod(false, "get", DoubleWordPointer_Get);
    $.RawMethod(false, "set", DoubleWordPointer_Set);

    $.RawMethod(false, "getElement", DoubleWordPointer_GetElement);
    $.RawMethod(false, "setElement", DoubleWordPointer_SetElement);

    $.RawMethod(false, "getOffset", DoubleWordPointer_GetOffset);
    $.RawMethod(false, "setOffset", DoubleWordPointer_SetOffset);
  });
})();

JSIL.MakeStruct("JSIL.Pointer", "JSIL.QuadWordPointer", true, [], function ($) {
  $.RawMethod(false, "get",
    function QuadWordPointer_Get () {
      return this.view[this.offsetInElements];
    }
  );

  $.RawMethod(false, "set",
    function QuadWordPointer_Set (value) {
      this.view[this.offsetInElements] = value;
    }
  );

  $.RawMethod(false, "getElement",
    function QuadWordPointer_GetElement (offsetInElements) {
      return this.view[(this.offsetInElements + offsetInElements) | 0];
    }
  );

  $.RawMethod(false, "setElement",
    function QuadWordPointer_SetElement (offsetInElements, value) {
      this.view[(this.offsetInElements + offsetInElements) | 0] = value;
    }
  );

  $.RawMethod(false, "getOffset",
    function QuadWordPointer_GetOffset (offsetInBytes) {
      return this.view[((this.offsetInBytes + offsetInBytes) | 0) >> 3];
    }
  );

  $.RawMethod(false, "setOffset",
    function QuadWordPointer_SetOffset (offsetInBytes, value) {
      this.view[((this.offsetInBytes + offsetInBytes) | 0) >> 3] = value;
    }
  );
});

JSIL.MakeStruct("JSIL.Pointer", "JSIL.BytePointer", true, [], function ($) {
  $.RawMethod(false, "get",
    function BytePointer_Get () {
      return this.view[this.offsetInBytes];
    }
  );

  $.RawMethod(false, "set",
    function BytePointer_Set (value) {
      this.view[this.offsetInBytes] = value;
    }
  );

  $.RawMethod(false, "getElement",
    function BytePointer_GetElement (offsetInElements) {
      return this.view[(this.offsetInBytes + offsetInElements) | 0];
    }
  );

  $.RawMethod(false, "setElement",
    function BytePointer_SetElement (offsetInElements, value) {
      this.view[(this.offsetInBytes + offsetInElements) | 0] = value;
    }
  );

  $.RawMethod(false, "getOffset",
    function BytePointer_GetOffset (offsetInBytes) {
      return this.view[(this.offsetInBytes + offsetInBytes) | 0];
    }
  );

  $.RawMethod(false, "setOffset",
    function BytePointer_SetOffset (offsetInBytes, value) {
      this.view[(this.offsetInBytes + offsetInBytes) | 0] = value;
    }
  );
});

JSIL.MakeStruct("JSIL.Pointer", "JSIL.StructPointer", true, [], function ($) {
  function ElementTypeRecord (elementType) {
    this.type = elementType;
    this.typeId = elementType.__TypeId__;
    this.nativeSize = elementType.__NativeSize__;
    this.unmarshalConstructor = JSIL.$GetStructUnmarshalConstructor(elementType);
    // this.unmarshaller = JSIL.$GetStructUnmarshaller(structType);
    this.marshaller = JSIL.$GetStructMarshaller(elementType);
  };

  var elementTypeRecords = JSIL.CreateDictionaryObject(null);

  function getElementTypeRecord (elementType) {
    var result = elementTypeRecords[elementType.__TypeId__];
    if (!result)
      result = elementTypeRecords[elementType.__TypeId__] = new ElementTypeRecord(elementType);

    return result;
  };

  $.RawMethod(false, ".ctor",
    function StructPointer_ctor (elementType, memoryRange, view, offsetInBytes) {
      if (arguments.length !== 4)
        JSIL.RuntimeError("Pointer ctor expects (elementType, memoryRange, view, offsetInBytes)");

      this.typeRecord = getElementTypeRecord(elementType);
      this.memoryRange = memoryRange;
      this.view = view;
      this.offsetInBytes = offsetInBytes | 0;
      this.proxy = null;
    }
  );


  $.RawMethod(false, "get_elementType",
    function StructPointer_get_elementType () {
      return this.typeRecord.type;
    }
  );

  $.Property({}, "elementType");


  $.RawMethod(false, "__CopyMembers__",
    function StructPointer_CopyMembers (source, target) {
      target.typeRecord = source.typeRecord;
      target.memoryRange = source.memoryRange;
      target.view = source.view;
      target.offsetInBytes = source.offsetInBytes;
      target.proxy = null;
    }
  );

  $.RawMethod(false, "add",
    function StructPointer_Add (offsetInBytes, modifyInPlace) {
      if (modifyInPlace === true) {
        this.offsetInBytes = (this.offsetInBytes + offsetInBytes) | 0;
      } else {
        return new JSIL.StructPointer(
          this.elementType,
          this.memoryRange, this.view,
          (this.offsetInBytes + offsetInBytes) | 0
        );
      }
    }
  );

  $.RawMethod(false, "addElements",
    function StructPointer_AddElements (offsetInElements, modifyInPlace) {
      if (modifyInPlace === true) {
        this.offsetInBytes = (this.offsetInBytes + ((offsetInElements * this.typeRecord.nativeSize) | 0)) | 0;
      } else {
        return new JSIL.StructPointer(
          this.elementType,
          this.memoryRange, this.view,
          (this.offsetInBytes + ((offsetInElements * this.typeRecord.nativeSize) | 0)) | 0
        );
      }
    }
  );

  $.RawMethod(false, "get",
    function StructPointer_Get () {
      var result = new this.typeRecord.unmarshalConstructor(this.view, this.offsetInBytes);
      return result;
    }
  );

  $.RawMethod(false, "getProxy",
    function StructPointer_GetProxy () {
      if (this.proxy === null)
        this.proxy = JSIL.MakeElementProxy(this.elementType);

      this.proxy.retargetBytes(this.view, this.offsetInBytes);
      return this.proxy;
    }
  );

  $.RawMethod(false, "set",
    function StructPointer_Set (value) {
      this.typeRecord.marshaller(value, this.view, this.offsetInBytes);
      return value;
    }
  );

  $.RawMethod(false, "getElement",
    function StructPointer_GetElement (offsetInElements) {
      var record = this.typeRecord;
      var offsetInBytes = (this.offsetInBytes + (offsetInElements * record.nativeSize) | 0) | 0;

      var result = new record.unmarshalConstructor(this.view, offsetInBytes);
      return result;
    }
  );

  $.RawMethod(false, "setElement",
    function StructPointer_SetElement (offsetInElements, value) {
      var record = this.typeRecord;
      var offsetInBytes = (this.offsetInBytes + (offsetInElements * record.nativeSize) | 0) | 0;

      record.marshaller(value, this.view, offsetInBytes);
      return value;
    }
  );

  $.RawMethod(false, "getOffset",
    function StructPointer_GetOffset (offsetInBytes) {
      var result = new this.typeRecord.unmarshalConstructor(this.view, (this.offsetInBytes + offsetInBytes) | 0);
      return result;
    }
  );

  $.RawMethod(false, "setOffset",
    function StructPointer_SetOffset (offsetInBytes, value) {
      this.typeRecord.marshaller(value, this.view, (this.offsetInBytes + offsetInBytes) | 0);
      return value;
    }
  );
});

JSIL.MakeInterface(
  "JSIL.Runtime.IPackedArray`1", true, ["T"], function ($) {
    var T = new JSIL.GenericParameter("T", "JSIL.Runtime.IPackedArray");
    var TRef = JSIL.Reference.Of(T);

    $.Method(
      {}, "get_Item",
      new JSIL.MethodSignature(T, [$.Int32], [])
    );

    $.Method(
      {}, "GetReference",
      new JSIL.MethodSignature(TRef, [$.Int32], [])
    );

    $.Method(
      {}, "set_Item",
      new JSIL.MethodSignature(null, [$.Int32, T], [])
    );

    $.Method(
      {}, "get_Length",
      new JSIL.MethodSignature($.Int32, [], [])
    );

    $.Property({}, "Length");
  }, []
);

JSIL.MakeClass("JSIL.Reference", "JSIL.PackedStructArrayElementReference", true, [], function ($) {
  $.RawMethod(false, ".ctor",
    function PackedStructArrayElementReference_ctor (array, index) {
      this.array = array;
      this.index = index | 0;
    }
  );

  $.RawMethod(false, "get",
    function PackedStructArrayElementReference_Get () {
      return this.array.get_Item(this.index);
    }
  );

  $.RawMethod(false, "set",
    function PackedStructArrayElementReference_Set (value) {
      return this.array.set_Item(this.index, value);
    }
  );

  $.RawMethod(false, "retarget",
    function PackedStructArrayElementReference_Retarget (array, index) {
      this.array = array;
      this.index = index | 0;
      return this;
    }
  );
});

JSIL.MakeClass("System.Array", "JSIL.PackedStructArray", true, ["T"], function ($) {
  var T = new JSIL.GenericParameter("T", "JSIL.PackedStructArray");
  var TRef = JSIL.Reference.Of(T);

  $.RawMethod(false, ".ctor",
    function PackedStructArray_ctor (byteArray, memoryRange) {
      this.__IsPackedArray__ = true;
      this.bytes = byteArray;
      this.buffer = byteArray.buffer;

      if (memoryRange)
        this.memoryRange = memoryRange;
      else
        this.memoryRange = JSIL.GetMemoryRangeForBuffer(this.buffer);

      this.nativeSize = this.T.__NativeSize__;
      this.elementProxyConstructor = JSIL.$GetStructElementProxyConstructor(this.T);
      this.unmarshalConstructor = JSIL.$GetStructUnmarshalConstructor(this.T);
      this.unmarshaller = JSIL.$GetStructUnmarshaller(this.T);
      this.marshaller = JSIL.$GetStructMarshaller(this.T);
      this.length = (this.memoryRange.length / this.nativeSize) | 0;
      this.byteLength = (this.length * this.nativeSize) | 0;
    }
  );

  $.Method(
    {}, "get_Item",
    new JSIL.MethodSignature(T, [$.Int32], []),
    function PackedStructArray_get_Item (index) {
      var offsetInBytes = (index * this.nativeSize) | 0;
      return new this.unmarshalConstructor(this.bytes, offsetInBytes);
    }
  );

  $.Method(
    {}, "GetItemProxy",
    new JSIL.MethodSignature(T, [$.Int32], []),
    function PackedStructArray_GetItemProxy (index) {
      var offsetInBytes = (index * this.nativeSize) | 0;
      return new this.elementProxyConstructor(this.bytes, offsetInBytes);
    }
  );

  $.Method(
    {}, "GetReference",
    new JSIL.MethodSignature(TRef, [$.Int32], []),
    function PackedStructArray_GetReference (index) {
      return new JSIL.PackedStructArrayElementReference(this, index);
    }
  );

  $.Method(
    {}, "GetItemInto",
    new JSIL.MethodSignature(null, [$.Int32, TRef], []),
    function PackedStructArray_GetItemInto (index, output) {
      var offsetInBytes = (index * this.nativeSize) | 0;
      this.unmarshaller(output.get(), this.bytes, offsetInBytes);
    }
  );

  $.Method(
    {}, "set_Item",
    new JSIL.MethodSignature(null, [$.Int32, T], []),
    function PackedStructArray_set_Item (index, value) {
      var offsetInBytes = (index * this.nativeSize) | 0;
      this.marshaller(value, this.bytes, offsetInBytes);
      return value;
    }
  );

  $.Method(
    {}, "get_Length",
    new JSIL.MethodSignature($.Int32, [], []),
    function PackedStructArray_get_Length () {
      return this.length;
    }
  );

  $.Property({}, "Length");

  $.ImplementInterfaces(
    /* 0 */ $jsilcore.TypeRef("JSIL.Runtime.IPackedArray`1", [T])
  );
});

JSIL.IsPackedArray = function IsPackedArray (object) {
  return object && !!object.__IsPackedArray__;
};

JSIL.PackedArray.New = function PackedArray_New (elementType, sizeOrInitializer) {
  var elementTypeObject = null, elementTypePublicInterface = null;

  if (typeof (elementType.__Type__) === "object") {
    elementTypeObject = elementType.__Type__;
    elementTypePublicInterface = elementType;
  } else if (typeof (elementType.__PublicInterface__) !== "undefined") {
    elementTypeObject = elementType;
    elementTypePublicInterface = elementType.__PublicInterface__;
  }

  if (!elementTypeObject.__IsStruct__) {
    if (elementTypeObject.__IsNativeType__) {
      var typeCtor = JSIL.GetTypedArrayConstructorForElementType(elementTypeObject);
      return new typeCtor(sizeOrInitializer);
    }

    throw new System.NotImplementedException("Cannot initialize a packed array with non-struct elements");
  }

  var result = null, size = 0;
  var initializerIsArray = JSIL.IsArray(sizeOrInitializer);

  if (initializerIsArray) {
    size = Number(sizeOrInitializer.length) | 0;
  } else {
    size = Number(sizeOrInitializer) | 0;
  }

  var sizeInBytes = (JSIL.GetNativeSizeOf(elementTypeObject, false) * size) | 0;
  var buffer = new Uint8Array(sizeInBytes);
  var arrayType = JSIL.PackedStructArray.Of(elementTypeObject);

  var result = new arrayType(buffer);

  // Copy the initializer elements into the packed array
  if (initializerIsArray) {
    for (var i = 0; i < size; i++) {
      var element = sizeOrInitializer[i];
      result.set_Item(i, element);
    }
  }

  return result;
};

if (typeof (WeakMap) !== "undefined") {
  $jsilcore.MemoryRangeCache = new WeakMap();
} else {
  $jsilcore.MemoryRangeCache = null;
}

JSIL.UnwrapPointerType = function (typeObject) {
  if (typeObject.__Type__)
    typeObject = typeObject.__Type__;

  if (typeObject.__OpenType__ === JSIL.Pointer.__Type__)
    return typeObject.__GenericArgumentValues__[0];
  else
    return typeObject;
}

JSIL.NewPointer = function (elementTypeObject, memoryRange, view, offsetInBytes) {
  if (elementTypeObject) {
    elementTypeObject = JSIL.UnwrapPointerType(elementTypeObject);

    if (elementTypeObject.__IsStruct__)
      return new JSIL.StructPointer(elementTypeObject, memoryRange, view, offsetInBytes);
  }

  switch (view.BYTES_PER_ELEMENT) {
    case 1:
      return new JSIL.BytePointer(elementTypeObject, memoryRange, view, offsetInBytes);
    case 2:
      return new JSIL.WordPointer(elementTypeObject, memoryRange, view, offsetInBytes);
    case 4:
      return new JSIL.DoubleWordPointer(elementTypeObject, memoryRange, view, offsetInBytes);
    case 8:
      return new JSIL.QuadWordPointer(elementTypeObject, memoryRange, view, offsetInBytes);

    default:
      return new JSIL.Pointer(elementTypeObject, memoryRange, view, offsetInBytes);
  }
};

JSIL.GetMemoryRangeForBuffer = function (buffer) {
  var result;

  if ($jsilcore.MemoryRangeCache)
    result = $jsilcore.MemoryRangeCache.get(buffer);

  if (!result) {
    result = new JSIL.MemoryRange(buffer);

    if ($jsilcore.MemoryRangeCache)
      $jsilcore.MemoryRangeCache.set(buffer, result);
  }

  return result;
};

JSIL.PinAndGetPointer = function (objectToPin, offsetInElements, throwOnFail) {
  var isPackedArray = JSIL.IsPackedArray(objectToPin);

  if (!JSIL.IsArray(objectToPin) && !isPackedArray) {
    if (throwOnFail !== false)
      JSIL.RuntimeError("Object being pinned must be an array");
    else
      return null;
  }

  // Pinning a zero-element array always produces a null pointer on the CLR
  if (objectToPin.length === 0)
    // FIXME: Infer the type of the null pointer
    return new JSIL.NullPointer(null);

  var buffer = objectToPin.buffer;
  if (!buffer) {
    if (throwOnFail !== false)
      JSIL.RuntimeError("Object being pinned must have an underlying memory buffer");
    else
      return null;
  }

  offsetInElements = (offsetInElements || 0) | 0;
  if ((offsetInElements < 0) || (offsetInElements >= objectToPin.length)) {
    if (throwOnFail !== false)
      JSIL.RuntimeError("offsetInElements outside the array");
    else
      return null;
  }

  var offsetInBytes;
  var memoryRange = JSIL.GetMemoryRangeForBuffer(buffer);
  var memoryView;

  var pinTargetByteOffset = 0;
  if (typeof (objectToPin.byteOffset) === "number") {
    pinTargetByteOffset = objectToPin.byteOffset | 0;
  }

  if (!isPackedArray) {
    memoryRange.storeExistingView(objectToPin);
    memoryView = objectToPin;

    offsetInBytes = (((offsetInElements * objectToPin.BYTES_PER_ELEMENT) | 0) + pinTargetByteOffset) | 0;
  } else {
    memoryView = memoryRange.getView($jsilcore.System.Byte.__Type__);
    offsetInBytes = (((offsetInElements * objectToPin.nativeSize) | 0) + pinTargetByteOffset) | 0;
  }

  var elementType = null;
  if (isPackedArray)
    elementType = objectToPin.T;

  var pointer = JSIL.NewPointer(
    elementType, memoryRange, memoryView, offsetInBytes
  );

  return pointer;
};

JSIL.StackAlloc = function (sizeInBytes, elementType) {
  var buffer = new ArrayBuffer(sizeInBytes);
  var memoryRange = JSIL.GetMemoryRangeForBuffer(buffer);
  var view = memoryRange.getView(elementType, false);
  if (!view)
    JSIL.RuntimeError("Unable to stack-allocate arrays of type '" + elementType.__FullName__ + "'");

  return JSIL.NewPointer(elementType, memoryRange, view, 0);
};

$jsilcore.PointerLiteralMemoryRange = null;

JSIL.PointerLiteral = function (value) {
  // We carve out a small universal block of memory to act as the 'range' within which pointer literals live.
  // This is enough to let you pass them around and compare them (though obviously, you can't safely read/write).
  if (!$jsilcore.PointerLiteralMemoryRange) {
    var buffer = new ArrayBuffer(16);
    $jsilcore.PointerLiteralMemoryRange = JSIL.GetMemoryRangeForBuffer(buffer);
  }

  var view = $jsilcore.PointerLiteralMemoryRange.getView($jsilcore.System.Byte, false);
  return JSIL.NewPointer(null, $jsilcore.PointerLiteralMemoryRange, view, value);
};

JSIL.$GetStructMarshaller = function (typeObject) {
  var marshaller = typeObject.__Marshaller__;
  if (marshaller === $jsilcore.FunctionNotInitialized)
    marshaller = typeObject.__Marshaller__ = JSIL.$MakeStructMarshaller(typeObject);

  return marshaller;
}

JSIL.MarshalStruct = function Struct_Marshal (struct, bytes, offset) {
  var thisType = struct.__ThisType__;
  var marshaller = JSIL.$GetStructMarshaller(thisType);
  return marshaller(struct, bytes, offset);
};

JSIL.$GetStructUnmarshaller = function (typeObject) {
  var unmarshaller = typeObject.__Unmarshaller__;
  if (unmarshaller === $jsilcore.FunctionNotInitialized)
    unmarshaller = typeObject.__Unmarshaller__ = JSIL.$MakeStructUnmarshaller(typeObject);

  return unmarshaller;
};

JSIL.$GetStructUnmarshalConstructor = function (typeObject) {
  var unmarshalConstructor = typeObject.__UnmarshalConstructor__;
  if (unmarshalConstructor === $jsilcore.FunctionNotInitialized)
    unmarshalConstructor = typeObject.__UnmarshalConstructor__ = JSIL.$MakeStructUnmarshalConstructor(typeObject);

  return unmarshalConstructor;
};

JSIL.$GetStructElementProxyConstructor = function (typeObject) {
  var elementProxyConstructor = typeObject.__ElementProxyConstructor__;
  if (elementProxyConstructor === $jsilcore.FunctionNotInitialized)
    elementProxyConstructor = typeObject.__ElementProxyConstructor__ = JSIL.$MakeElementProxyConstructor(typeObject);

  return elementProxyConstructor;
};

JSIL.UnmarshalStruct = function Struct_Unmarshal (struct, bytes, offset) {
  var thisType = struct.__ThisType__;
  var unmarshaller = JSIL.$GetStructUnmarshaller(thisType);
  return unmarshaller(struct, bytes, offset);
};

JSIL.GetNativeSizeOf = function GetNativeSizeOf (typeObject, forPInvoke) {
  if (!typeObject) {
    return -1;
  } if (typeObject.__IsNativeType__) {
    var arrayCtor = JSIL.GetTypedArrayConstructorForElementType(typeObject, false);
    if (arrayCtor)
      return arrayCtor.BYTES_PER_ELEMENT;
    else
      return -1;
  } else if (typeObject.__IsStruct__) {
    var result = typeObject.__NativeSize__;
    if (typeof (result) !== "number")
      return -1;

    return result;
  } else if (typeObject.__IsEnum__) {
    return JSIL.GetNativeSizeOf(typeObject.__StorageType__, forPInvoke);
  } else if (typeObject.__IsDelegate__) {
    if (forPInvoke)
      return 4;
    else
      return -1;
  } else {
    return -1;
  }
};

JSIL.GetNativeAlignmentOf = function GetNativeAlignmentOf (typeObject, forPInvoke) {
  if (typeObject.__IsNativeType__) {
    var arrayCtor = JSIL.GetTypedArrayConstructorForElementType(typeObject, false);
    if (arrayCtor)
      return arrayCtor.BYTES_PER_ELEMENT;
    else
      return -1;
  } else if (typeObject.__IsStruct__) {
    var result = typeObject.__NativeAlignment__;
    if (typeof (result) !== "number")
      return -1;

    return result;
  } else if (typeObject.__IsEnum__) {
    return JSIL.GetNativeAlignmentOf(typeObject.__StorageType__, forPInvoke);
  } else if (typeObject.__IsDelegate__) {
    if (forPInvoke)
      return 4;
    else
      return -1;
  } else {
    return -1;
  }
};

JSIL.ComputeNativeAlignmentOfStruct = function ComputeNativeAlignmentOfStruct (typeObject) {
  var fcs = JSIL.$StructSizeOverrides[typeObject.__FullName__];
  if (fcs)
    return fcs;

  var fields = JSIL.GetFieldList(typeObject);
  var maxAlignment = 0;

  for (var i = 0, l = fields.length; i < l; i++) {
    var field = fields[i];
    var maxAlignment = Math.max(maxAlignment, field.alignmentBytes);
  }

  return maxAlignment;
};

JSIL.ComputeNativeSizeOfStruct = function ComputeNativeSizeOfStruct (typeObject) {
  var fields = JSIL.GetFieldList(typeObject);
  var maxAlignment = 0;
  // Structs are always at least one byte in size
  var resultSize = 1;

  var fcs = JSIL.$StructSizeOverrides[typeObject.__FullName__];
  if (fcs)
    return fcs;

  for (var i = 0, l = fields.length; i < l; i++) {
    var field = fields[i];

    var maxAlignment = Math.max(maxAlignment, field.alignmentBytes);

    if (field.sizeBytes >= 0)
      resultSize = Math.max(resultSize, field.offsetBytes + field.sizeBytes);
  }

  if (typeof (typeObject.__CustomSize__) === "number")
    resultSize = Math.max(typeObject.__CustomSize__, resultSize);

  if (maxAlignment > 0) {
    var resultSizeAligned = (((resultSize + maxAlignment - 1) / maxAlignment) | 0) * maxAlignment;
    // JSIL.Host.logWriteLine("Native size of '" + typeObject.__FullName__ + "' expanded from " + resultSize + " to " + resultSizeAligned + " by alignment");
    return resultSizeAligned;
  } else {
    return resultSize;
  }
};

$jsilcore.MarshallingMemoryRange = null;

JSIL.GetMarshallingScratchBuffer = function (minimumSize) {
  var requestedBufferSize = Math.max(minimumSize, 8) | 0;

  var memoryRange = $jsilcore.MarshallingMemoryRange;

  // If the current scratch buffer is too small, make a bigger one
  if (memoryRange && memoryRange.buffer.byteLength < requestedBufferSize)
    memoryRange = null;

  if (!memoryRange) {
    var scratchBuffer = new ArrayBuffer(requestedBufferSize);
    memoryRange = $jsilcore.MarshallingMemoryRange = JSIL.GetMemoryRangeForBuffer(scratchBuffer);
  }

  return memoryRange;
};

JSIL.$MakeStructMarshaller = function (typeObject) {
  return JSIL.$MakeStructMarshalFunctionCore(typeObject, true);
};

JSIL.$MakeStructUnmarshaller = function (typeObject) {
  return JSIL.$MakeStructMarshalFunctionCore(typeObject, false);
};

JSIL.$MakeStructUnmarshalConstructor = function (typeObject) {
  var closure = {};
  var body = [];

  JSIL.$MakeStructMarshalFunctionSource(typeObject, false, true, closure, body);

  var constructor =  JSIL.CreateNamedFunction(
    typeObject.__FullName__ + ".UnmarshalledInstance",
    ["bytes", "offset"],
    body.join('\n'),
    closure
  );

  constructor.prototype = typeObject.__PublicInterface__.prototype;

  return constructor;
};

JSIL.$MakeStructMarshalFunctionCore = function (typeObject, marshal) {
  var closure = {};
  var body = [];

  JSIL.$MakeStructMarshalFunctionSource(typeObject, marshal, false, closure, body);

  return JSIL.CreateNamedFunction(
    typeObject.__FullName__ + (marshal ? ".Marshal" : ".Unmarshal"),
    ["struct", "bytes", "offset"],
    body.join('\n'),
    closure
  );
};

JSIL.$EmitMemcpyIntrinsic = function (body, destToken, sourceToken, destOffsetToken, sourceOffsetToken, sizeOrSizeToken) {
  var unrollThreshold = 16;

  if (false) {
    // This is what you're SUPPOSED to do, but it's incredibly slow in both V8 and SpiderMonkey. Blah.
    body.push(destToken + ".set(new Uint8Array(" + sourceToken + ".buffer, " + sourceOffsetToken + ", " + sizeOrSizeToken + "), 0);");
  } else {
    // Unroll small copies when size is known
    if ((typeof (sizeOrSizeToken) === "number") && (sizeOrSizeToken <= unrollThreshold)) {
      for (var i = 0; i < sizeOrSizeToken; i++) {
        var localDest, localSource;
        if (typeof (destOffsetToken) === "number")
          localDest = (destOffsetToken + i) | 0;
        else
          localDest = "(" + destOffsetToken + " + " + i + ") | 0";

        if (typeof (sourceOffsetToken) === "number")
          localSource = (sourceOffsetToken + i) | 0;
        else
          localSource = "(" + sourceOffsetToken + " + " + i + ") | 0";

        body.push("  " + destToken + "[" + localDest + "] = " + sourceToken + "[" + localSource + "];");
      }
      body.push("");
    } else {
      body.push("for (var sourceEnd = (" + sourceOffsetToken + " + " + sizeOrSizeToken + ") | 0, i = " + sourceOffsetToken + ", j = " + destOffsetToken + "; i < sourceEnd; i++, j++)");
      body.push("  " + destToken + "[j] = " + sourceToken + "[i];");
      body.push("");
    }
  }
};

JSIL.$MakeInt64MarshalFunctionSource = function (typeObject, marshal, isConstructor, closure, body) {
  var targetVar = isConstructor ? "this" : "struct";
  var triplet;

  if (marshal) {
    triplet = function (targetField, offset, count) {
      var f = "struct." + targetField;

      body.push("bytes[(offset + " + offset + ") | 0] = " + f + " & 0xFF;");
      if (count > 1)
        body.push("bytes[(offset + " + (offset + 1) + ") | 0] = (" + f + " >> 8) & 0xFF;");
      if (count > 2)
        body.push("bytes[(offset + " + (offset + 2) + ") | 0] = (" + f + " >> 16) & 0xFF;");
    };
  } else {
    triplet = function (targetField, offset, count) {
      body.push(targetVar + "." + targetField + " = (");
      body.push("  bytes[(offset + " + offset + ") | 0] ");
      if (count > 1)
        body.push("  | (bytes[(offset + " + (offset + 1) + ") | 0] << 8)");
      if (count > 2)
        body.push("  | (bytes[(offset + " + (offset + 2) + ") | 0] << 16)");
      body.push(");");
    };
  }

  triplet("a", 0, 3);
  triplet("b", 3, 3);
  triplet("c", 6, 2);
};

JSIL.$MakeStructMarshalFunctionSource = function (typeObject, marshal, isConstructor, closure, body) {
  // FIXME
  var forPInvoke = false;

  var serializationScratchBuffers = $jsilcore.GetSerializationScratchBuffers();

  var fields = JSIL.GetFieldList(typeObject);
  var nativeSize = JSIL.GetNativeSizeOf(typeObject, forPInvoke);
  var nativeAlignment = JSIL.GetNativeAlignmentOf(typeObject, forPInvoke);
  if (nativeSize < 0)
    JSIL.RuntimeError("Type '" + typeObject.__FullName__ + "' cannot be marshalled");

  if (typeObject.__IsUnion__) {
    if (isConstructor) {
      // FIXME: Duplication
      body.push("this.$backingStore = new Uint8Array(" + nativeSize + ");");

      for (var i = 0; i < fields.length; i++) {
        if (!fields[i].type.__IsStruct__)
          continue;

        body.push("this.cached$" + fields[i].name + " = null;");
      }
    }

    var selfStore =
      isConstructor
        ? "this.$backingStore"
        : "struct.$backingStore";

    if (marshal)
      JSIL.$EmitMemcpyIntrinsic(body, "bytes", selfStore, "offset", 0, nativeSize);
    else
      JSIL.$EmitMemcpyIntrinsic(body, selfStore, "bytes", 0, "offset", nativeSize);

    return;
  }

  // HACK: Compensate for our int64/uint64 having unusual memory layout
  if (
    (typeObject.__FullName__ === "System.Int64") ||
    (typeObject.__FullName__ === "System.UInt64")
  ) {
    return JSIL.$MakeInt64MarshalFunctionSource(typeObject, marshal, isConstructor, closure, body);
  }

  var scratchBuffer = new ArrayBuffer(nativeSize);
  var scratchRange = JSIL.GetMemoryRangeForBuffer(scratchBuffer);

  closure.scratchBytes = new Uint8Array(scratchBuffer, 0, nativeSize);

  var numMarshallers = 0;

  var structArgName = isConstructor ? "this" : "struct";

  /*
  body.push("var isAligned = ((offset % " + nativeAlignment + ") | 0) === 0;");
  body.push("if (!isAligned) throw new Error('Unaligned marshal');");
  */

  var sortedFields = Array.prototype.slice.call(fields);
  sortedFields.sort(function (lhs, rhs) {
    return lhs.offsetBytes - rhs.offsetBytes;
  });

  // Where possible, reuse the same closure variable for marshaller functions.
  function makeStructMarshallerAndGetKey (type) {
    var funcValue = null;

    if (marshal)
      funcValue = JSIL.$GetStructMarshaller(field.type);
    else if (isConstructor)
      funcValue = JSIL.$GetStructUnmarshalConstructor(field.type);
    else
      funcValue = JSIL.$GetStructUnmarshaller(field.type);

    if (!funcValue)
      return null;

    for (var k in closure)
      if (closure[k] === funcValue)
        return k;

    var funcKey = "struct" + (numMarshallers++);
    closure[funcKey] = funcValue;
    return funcKey;
  }

  body.push("offset = offset | 0;");
  body.push("");

  // For structs only containing other structs we can generate a specialized marshalling function
  //  that avoids some extra work
  if (sortedFields.every(function (field) {
    return field.type.__IsStruct__;
  })) {
    // FIXME: duplication

    for (var i = 0, l = sortedFields.length; i < l; i++) {
      var field = sortedFields[i];
      var offset = field.offsetBytes;

      // Try to marshal the struct
      var funcKey = makeStructMarshallerAndGetKey(field.type);

      if (!marshal && isConstructor)
        body.push(
          structArgName + "." + field.name + " = new " + funcKey + "(bytes, (offset + " + offset + ") | 0);"
        );
      else
        body.push(
          funcKey + "(" + structArgName + "." + field.name + ", bytes, (offset + " + offset + ") | 0);"
        );
    }

    return;
  }

  if (!marshal)
    JSIL.$EmitMemcpyIntrinsic(body, "scratchBytes", "bytes", 0, "offset", nativeSize);

  var numEnumFields = 0, numDelegateFields = 0;

  for (var i = 0, l = sortedFields.length; i < l; i++) {
    var field = sortedFields[i];
    var offset = field.offsetBytes;
    var size = field.sizeBytes;

    if (size <= 0)
      JSIL.RuntimeErrorFormat(
        "Field '{0}' of type '{1}' cannot be marshalled (field.sizeBytes <= 0)",
        [field.name, typeObject.__FullName__]
      );

    var fieldConstructor = JSIL.GetTypedArrayConstructorForElementType(field.type, false);
    if (!fieldConstructor && field.type.__IsEnum__)
      fieldConstructor = JSIL.GetTypedArrayConstructorForElementType(field.type.__StorageType__, false);
    else if (!fieldConstructor && field.type.__IsDelegate__)
      fieldConstructor = JSIL.GetTypedArrayConstructorForElementType($jsilcore.System.Int32.__Type__, false);

    if (fieldConstructor) {
      var nativeAlignment = fieldConstructor.BYTES_PER_ELEMENT;

      var slowPath = false;
      if ((offset % nativeAlignment) !== 0) {
        // Unaligned offset. Use slow path involving another buffer to compensate for alignment.
        // Sigh...
        slowPath = true;
        var key = fieldConstructor.name.toLowerCase().replace("array", "");
        closure["scratch_" + field.name] = serializationScratchBuffers[key];

        if (marshal)
          closure["unalignedScratchBytes"] = serializationScratchBuffers.uint8;
        else
          closure["fillUnalignedScratchBytes"] = serializationScratchBuffers.fillFrom;

      } else {
        var fieldArray = new fieldConstructor(scratchBuffer, offset, 1);
        closure["scratch_" + field.name] = fieldArray;
      }

      if (slowPath && !marshal) {
        body.push("fillUnalignedScratchBytes(scratchBytes, " + offset + ", " + nativeAlignment + ");");
      }

      if (field.type.__IsEnum__) {
        var closureKey = "enum_" + String(numEnumFields++);
        closure[closureKey] = field.type.$Cast;

        if (marshal) {
          body.push("scratch_" + field.name + "[0] = (" + structArgName + "." + field.name + ").value;");
        } else {
          body.push(structArgName + "." + field.name + " = " + closureKey + "(scratch_" + field.name + "[0]);");
        }
      } else if (field.type.__IsDelegate__) {
        var closureKey = "delegate_" + String(numDelegateFields++);
        closure[closureKey] = function FailDelegate () {
          JSIL.RuntimeError("Delegate out marshalling in structs not implemented");
        };

        if (marshal) {
          body.push("scratch_" + field.name + "[0] = (" + structArgName + "." + field.name + ").$asIntPtr().ToInt32() | 0;");
        } else {
          body.push(structArgName + "." + field.name + " = " + closureKey + "(scratch_" + field.name + "[0]);");
        }
      } else {
        if (marshal) {
          body.push("scratch_" + field.name + "[0] = " + structArgName + "." + field.name + ";");
        } else {
          body.push(structArgName + "." + field.name + " = scratch_" + field.name + "[0];");
        }
      }

      if (slowPath && marshal) {
        JSIL.$EmitMemcpyIntrinsic(body, "scratchBytes", "unalignedScratchBytes", offset, 0, nativeAlignment);
      }
    } else if (field.type.__IsStruct__) {
      // Try to marshal the struct
      var funcKey = makeStructMarshallerAndGetKey(field.type);

      if (!marshal && isConstructor)
        body.push(
          structArgName + "." + field.name + " = new " + funcKey + "(scratchBytes, " + offset + ");"
        );
      else
        body.push(
          funcKey + "(" + structArgName + "." + field.name + ", scratchBytes, " + offset + ");"
        );
    } else {
      JSIL.RuntimeErrorFormat(
        "Field '{0}' of type '{1}' cannot be marshalled (unknown constructor type)",
        [field.name, typeObject.__FullName__]
      );
    }
  }

  if (marshal)
    JSIL.$EmitMemcpyIntrinsic(body, "bytes", "scratchBytes", "offset", 0, nativeSize);
};

JSIL.$MakeUnmarshallableFieldAccessor = function (fieldName) {
  return function UnmarshallableField () {
    JSIL.RuntimeError("Field '" + fieldName + "' cannot be marshalled");
  };
};

JSIL.$MakeFieldMarshaller = function (typeObject, field, viewBytes, nativeView, makeSetter, isElementProxy) {
  var fieldOffset = field.offsetBytes | 0;
  var fieldSize = field.sizeBytes | 0;

  var prefix =
    isElementProxy
      ? ".Proxy."
      : ".Union.";

  var adapterSource = [];

  if (isElementProxy) {
    adapterSource.push("var bytes = this.$bytes;");
    adapterSource.push("var offset = ((this.$offset | 0) + " + fieldOffset + ") | 0;");
  } else {
    adapterSource.push("var bytes = this.$backingStore;");
    adapterSource.push("var offset = " + fieldOffset + ";");
  }

  if (nativeView) {
    var clampedByteView = viewBytes.subarray(0, nativeView.BYTES_PER_ELEMENT);
    var closure = {
      nativeView: nativeView,
      clampedByteView: clampedByteView
    };

    if (makeSetter) {
      if (field.type.__IsEnum__) {
        adapterSource.push("nativeView[0] = value.value;");
      } else {
        adapterSource.push("nativeView[0] = value;");
      }

      JSIL.$EmitMemcpyIntrinsic(
        adapterSource, "bytes", "clampedByteView", "offset", 0, nativeView.BYTES_PER_ELEMENT
      );

      return JSIL.CreateNamedFunction(
        typeObject.__FullName__ + prefix + "set_" + field.name, ["value"],
        adapterSource.join("\n"),
        { nativeView: nativeView, clampedByteView: clampedByteView }
      );
    } else {
      JSIL.$EmitMemcpyIntrinsic(
        adapterSource, "clampedByteView", "bytes", 0, "offset", fieldSize
      );

      if (field.type.__IsEnum__) {
        closure.enumType = field.type;
        adapterSource.push("return enumType.$Cast(nativeView[0]);");
      } else {
        adapterSource.push("return nativeView[0];");
      }

      return JSIL.CreateNamedFunction(
        typeObject.__FullName__ + prefix + "get_" + field.name, [],
        adapterSource.join("\n"),
        closure
      );
    }

  } else if (field.type.__IsStruct__) {
    if (makeSetter) {
      var marshaller = JSIL.$GetStructMarshaller(field.type);

      adapterSource.push("marshaller(value, bytes, offset);");

      return JSIL.CreateNamedFunction(
        typeObject.__FullName__ + prefix + "set_" + field.name, ["value"],
        adapterSource.join("\n"),
        { marshaller: marshaller }
      );
    } else {
      var unmarshalConstructor = JSIL.$GetStructUnmarshalConstructor(field.type);
      var unmarshaller = JSIL.$GetStructUnmarshaller(field.type);
      var cachedInstanceKey = "this.cached$" + field.name;

      adapterSource.push("var cachedInstance = " + cachedInstanceKey + ";");
      adapterSource.push("if (cachedInstance !== null) {");
      adapterSource.push("  unmarshaller(cachedInstance, bytes, offset);");
      adapterSource.push("  return cachedInstance;");
      adapterSource.push("}");
      adapterSource.push("");
      adapterSource.push("return " + cachedInstanceKey + " = new unmarshalConstructor(bytes, offset);");

      return JSIL.CreateNamedFunction(
        typeObject.__FullName__ + prefix + "get_" + field.name, [],
        adapterSource.join("\n"),
        {
          unmarshaller: unmarshaller,
          unmarshalConstructor: unmarshalConstructor
        }
      );
    }

  } else {
    return JSIL.$MakeUnmarshallableFieldAccessor(field.name);
  }
};

JSIL.$MakeProxyFieldGetter = function (typeObject, field, viewBytes, nativeView, isElementProxy) {
  var fieldOffset = field.offsetBytes | 0;
  var fieldSize = field.sizeBytes | 0;
  var proxyConstructor = JSIL.$GetStructElementProxyConstructor(field.type);

  if (!field.type.__IsStruct__)
    JSIL.RuntimeError("Field must be a struct");

  var adapterSource = [];

  if (isElementProxy) {
    adapterSource.push("var bytes  = this.$bytes;");
    adapterSource.push("var offset = ((this.$offset | 0) + " + fieldOffset + ") | 0;");
  } else {
    adapterSource.push("var bytes  = this.$backingStore;");
    adapterSource.push("var offset = " + fieldOffset + ";");
  }

  var unmarshalConstructor = JSIL.$GetStructUnmarshalConstructor(field.type);
  var unmarshaller = JSIL.$GetStructUnmarshaller(field.type);
  var cachedInstanceKey = "this.cached$" + field.name;

  adapterSource.push("var cachedInstance = " + cachedInstanceKey + ";");

  adapterSource.push("if (cachedInstance !== null) {");
  if (isElementProxy)
    adapterSource.push("  cachedInstance.retargetBytes(bytes, offset);");
  adapterSource.push("  return cachedInstance;");
  adapterSource.push("}");

  adapterSource.push("");
  adapterSource.push("return " + cachedInstanceKey + " = new proxyConstructor(bytes, offset);");

  var prefix =
    isElementProxy
      ? ".Proxy."
      : ".Union.";

  return JSIL.CreateNamedFunction(
    typeObject.__FullName__ + prefix + "get_" + field.name, [],
    adapterSource.join("\n"),
    {
      proxyConstructor: proxyConstructor
    }
  );
};

JSIL.$MakeProxylikeConstructorBody = function (
  typeObject, fields, constructorBody,
  prototype,
  isElementProxy, targetToken
) {
  // FIXME
  var forPInvoke = false;

  var nativeSize = JSIL.GetNativeSizeOf(typeObject, forPInvoke) | 0;
  var marshallingScratchBuffer = JSIL.GetMarshallingScratchBuffer(nativeSize);
  var viewBytes = marshallingScratchBuffer.getView($jsilcore.System.Byte, false);

  for (var i = 0, l = fields.length; i < l; i++) {
    var field = fields[i];
    var offset = field.offsetBytes;
    var size = field.sizeBytes;

    var getter, setter;
    var storageType = field.type;

    if (field.type.__IsEnum__)
      storageType = field.type.__StorageType__;

    if (size <= 0) {
      getter = setter = JSIL.$MakeUnmarshallableFieldAccessor(field.name);
    } else {
      var nativeView = marshallingScratchBuffer.getView(storageType, false);
      if (field.type.__IsStruct__) {
        // HACK: Struct fields must be element proxies themselves so writes like this work:
        // proxy.Field.Field += 1
        // TODO: Maybe hoist this into the compiler to make it cheaper for non-write scenarios?
        getter = JSIL.$MakeProxyFieldGetter(typeObject, field, viewBytes, nativeView, isElementProxy);
        setter = JSIL.$MakeFieldMarshaller(typeObject, field, viewBytes, nativeView, true, isElementProxy);
        constructorBody.push(targetToken + ".cached$" + field.name + " = null;");

      } else {
        getter = JSIL.$MakeFieldMarshaller(typeObject, field, viewBytes, nativeView, false, isElementProxy);
        setter = JSIL.$MakeFieldMarshaller(typeObject, field, viewBytes, nativeView, true, isElementProxy);
      }
    }

    // FIXME: The use of get/set functions here will really degrade performance in some JS engines
    Object.defineProperty(
      prototype, field.name,
      {
        get: getter,
        set: setter,
        configurable: false,
        enumerable: true
      }
    );
  }

  return nativeSize;
};

JSIL.$MakeElementProxyConstructor = function (typeObject) {
  // var elementProxyPrototype = JSIL.CreatePrototypeObject(typeObject.__PublicInterface__.prototype);
  // HACK: This makes a big difference
  var elementProxyPrototype = JSIL.$CreateCrockfordObject(typeObject.__PublicInterface__.prototype);
  var fields = JSIL.GetFieldList(typeObject);

  var constructorBody = [];
  constructorBody.push("this.$bytes = bytes;");
  constructorBody.push("this.$offset = offsetInBytes | 0;");
  constructorBody.push("");

  var nativeSize = JSIL.$MakeProxylikeConstructorBody(
    typeObject, fields, constructorBody,
    elementProxyPrototype,
    true, "this"
  );

  var constructor = JSIL.CreateNamedFunction(
    typeObject.__FullName__ + ".Proxy._ctor", ["bytes", "offsetInBytes"],
    constructorBody.join("\n"), null
  );

  elementProxyPrototype.retarget = function (array, offsetInElements) {
    this.$bytes = array.bytes;
    this.$offset = ((offsetInElements | 0) * nativeSize) | 0;
    return this;
  };

  elementProxyPrototype.retargetBytes = function (bytes, offsetInBytes) {
    this.$bytes = bytes;
    this.$offset = offsetInBytes | 0;
    return this;
  };

  constructor.prototype = elementProxyPrototype;

  return constructor;
};

JSIL.MakeElementProxy = function (typeObject) {
  var constructor = JSIL.$GetStructElementProxyConstructor(typeObject);
  if (!constructor)
    JSIL.RuntimeError("No element proxy constructor available for type '" + typeObject.__FullName__ + "'");

  return new constructor(null, -1);
};

JSIL.GetBackingTypedArray = function (array) {
  var isPackedArray = JSIL.IsPackedArray(array);

  if (!JSIL.IsTypedArray(array) && !isPackedArray) {
    JSIL.RuntimeError("Object has no backing typed array");
  }

  if (isPackedArray) {
    return array.bytes;
  } else {
    return array;
  }
};

JSIL.GetArrayBuffer = function (array) {
  var isPackedArray = JSIL.IsPackedArray(array);

  if (!JSIL.IsTypedArray(array) && !isPackedArray) {
    JSIL.RuntimeError("Object has no array buffer");
  }

  if (isPackedArray) {
    return array.bytes.buffer;
  } else {
    return array.buffer;
  }
};

JSIL.GetArrayByteOffset = function (array) {
  var isPackedArray = JSIL.IsPackedArray(array);

  if (!JSIL.IsTypedArray(array) && !isPackedArray) {
    JSIL.RuntimeError("Object has no array buffer");
  }

  if (isPackedArray) {
    return array.bytes.byteOffset | 0;
  } else {
    return array.byteOffset | 0;
  }
};

JSIL.GetArrayByteLength = function (array) {
  var isPackedArray = JSIL.IsPackedArray(array);

  if (!JSIL.IsTypedArray(array) && !isPackedArray) {
    JSIL.RuntimeError("Object has no array buffer");
  }

  if (isPackedArray) {
    return array.bytes.byteLength | 0;
  } else {
    return array.byteLength | 0;
  }
};

// Note that this does not let you mutate valueToPin by modifying the pinned pointer! This is read-only.
JSIL.PinValueAndGetPointer = function (valueToPin, sourceType, targetType) {
  targetType = JSIL.UnwrapPointerType(targetType);

  var sourceCtor = JSIL.GetTypedArrayConstructorForElementType(sourceType);
  var targetCtor = JSIL.GetTypedArrayConstructorForElementType(targetType);

  var temporaryArray = new (sourceCtor)(1);
  temporaryArray[0] = valueToPin;

  if (sourceCtor == targetCtor) {
    return JSIL.PinAndGetPointer(temporaryArray);
  } else {
    var resultArray = new (targetCtor)(temporaryArray.buffer, 0, temporaryArray.buffer.byteLength);
    return JSIL.PinAndGetPointer(resultArray);
  }
};

// FIXME: Implement unpin operation? Probably not needed yet.

JSIL.$GenerateUnionAccessors = function (typeObject, fields, constructorBody, targetToken) {
  JSIL.$MakeProxylikeConstructorBody(
    typeObject, fields, constructorBody,
    typeObject.__PublicInterface__.prototype,
    false, targetToken
  );
};
