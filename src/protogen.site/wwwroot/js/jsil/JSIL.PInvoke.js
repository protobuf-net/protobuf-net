/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core is required");

if (!$jsilcore)
  throw new Error("JSIL.Core is required");

JSIL.DeclareNamespace("JSIL.PInvoke");

// Used to access shared heap
var warnedAboutMultiModule = false;

JSIL.PInvoke.CurrentCallContext = null;

JSIL.PInvoke.GetGlobalModule = function () {
  var module = JSIL.GlobalNamespace.Module || JSIL.__NativeModules__["__global__"];

  if (!module)
    JSIL.RuntimeError("No emscripten modules loaded");

  if (Object.keys(JSIL.__NativeModules__).length > 2) {
    if (!warnedAboutMultiModule) {
      warnedAboutMultiModule = true;
      JSIL.Host.warning("More than one Emscripten module is loaded, so operations that need a global heap will fail. This is due to a limitation of Emscripten.");
    }
  }

  return module;
};

// Used in operations like AllocHGlobal to try and ensure we pick the best possible
//  heap during marshaling operations (when we have a good guess) instead of just choking
JSIL.PInvoke.GetDefaultModule = function () {
  if (JSIL.PInvoke.CurrentCallContext)
    return JSIL.PInvoke.CurrentCallContext.module;
  else
    return JSIL.PInvoke.GetGlobalModule();
};

// Used to access specific entry points
JSIL.PInvoke.GetModule = function (name, throwOnFail) {
  var modules = JSIL.__NativeModules__;

  // HACK
  var key = name.toLowerCase().replace(".so", ".dll");

  var module = modules[key];

  if (!module && (throwOnFail !== false))
    JSIL.RuntimeError("No module named '" + name + "' loaded.");

  return module;
};

// Locates the emscripten module that owns a given heap
JSIL.PInvoke.GetModuleForHeap = function (heap, throwOnFail) {
  var buffer;

  if (!heap)
    JSIL.RuntimeError("No heap provided");
  else if (heap && heap.buffer)
    buffer = heap.buffer;
  else
    buffer = heap;

  var nm = JSIL.__NativeModules__;
  for (var k in nm) {
    if (!nm.hasOwnProperty(k))
      continue;

    var m = nm[k];

    var mBuffer = m.HEAPU8.buffer;

    if (mBuffer === buffer)
      return m;
  }

  if (throwOnFail !== false)
    JSIL.RuntimeError("No module owns the specified heap");

  return null;
};

JSIL.PInvoke.PickModuleForPointer = function (pointer, enableFallback) {
  var result = null;

  if (pointer.pointer && pointer.pointer.memoryRange) {
    result = JSIL.PInvoke.GetModuleForHeap(pointer.pointer.memoryRange.buffer, !enableFallback);
  }

  if (result === null) {
    if (enableFallback) {
      result = JSIL.PInvoke.GetDefaultModule();
    } else {
      JSIL.RuntimeError("No appropriate module available");
    }
  }

  return result;
};

JSIL.PInvoke.CreateBytePointerForModule = function (module, address) {
  var memoryRange = JSIL.GetMemoryRangeForBuffer(module.HEAPU8.buffer);
  var emscriptenMemoryView = memoryRange.getView(System.Byte.__Type__);

  var emscriptenPointer = new JSIL.BytePointer(System.Byte.__Type__, memoryRange, emscriptenMemoryView, address);

  return emscriptenPointer;
};

JSIL.PInvoke.CreateIntPtrForModule = function (module, address) {
  var emscriptenPointer = JSIL.PInvoke.CreateBytePointerForModule(module, address);

  var intPtr = JSIL.CreateInstanceOfType(
    System.IntPtr.__Type__,
    "$fromPointer",
    [emscriptenPointer]
  );

  return intPtr;
};

JSIL.PInvoke.DestroyFunctionPointer = function (fp) {
  var nm = JSIL.__NativeModules__;
  for (var k in nm) {
    if (!nm.hasOwnProperty(k))
      continue;

    if (k === "__global__")
      continue;

    var module = nm[k];

    module.Runtime.removeFunction(fp);
  }

  return result;
};

JSIL.MakeClass("System.Object", "JSIL.Runtime.NativePackedArray`1", true, ["T"], function ($) {
  var T = new JSIL.GenericParameter("T", "JSIL.Runtime.NativePackedArray`1");
  var TArray = System.Array.Of(T);

  $.Field({Public: false, Static: false, ReadOnly: true}, "_Array", TArray);
  $.Field({Public: true , Static: false, ReadOnly: true}, "Length", $.Int32);
  $.Field({Public: false, Static: false}, "IsNotDisposed", $.Boolean);

  $.RawMethod(false, "$innerCtor", function innerCtor (module, size) {
      this.Length = size;
      this.IsNotDisposed = true;

      this.ElementSize = JSIL.GetNativeSizeOf(this.T, false);
      var sizeBytes = this.ElementSize * this.Length;

      // HACK because emscripten malloc is finicky
      if (sizeBytes < 4)
        sizeBytes = 4;

      this.Module = module;
      this.EmscriptenOffset = module._malloc(sizeBytes);

      var tByte = $jsilcore.System.Byte.__Type__;
      this.MemoryRange = new JSIL.MemoryRange(module.HEAPU8.buffer, this.EmscriptenOffset, sizeBytes);

      if (this.T.__IsNativeType__) {
        this._Array = this.MemoryRange.getView(this.T);
      } else {
        var buffer = this.MemoryRange.getView(tByte);

        var arrayType = JSIL.PackedStructArray.Of(this.T);
        this._Array = new arrayType(buffer, this.MemoryRange);
      }
  });

  $.Method({Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$.Int32], []),
    function NativePackedArray_ctor (size) {
      this.$innerCtor(JSIL.PInvoke.GetDefaultModule(), size);
    }
  );

  $.Method({Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$.String, $.Int32], []),
    function NativePackedArray_ctor (dllName, size) {
      this.$innerCtor(JSIL.PInvoke.GetModule(dllName), size);
    }
  );

  $.Method({Static: true, Public: true }, "op_Implicit",
    new JSIL.MethodSignature(TArray, [T], []),
    function (self) {
      return self._Array;
    }
  );

  $.Method(
    {Public: true , Static: false}, "get_Array",
    new JSIL.MethodSignature(TArray, [], []),
    function get_Array () {
      return this._Array;
    }
  );

  $.Method({Static:false, Public:true }, "AllocHandle",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.InteropServices.GCHandle"), [], []),
    function AllocHandle () {
      return System.Runtime.InteropServices.GCHandle.Alloc(this._Array);
    }
  );

  $.Method({Static:false, Public:true }, "AllocHandle",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.InteropServices.GCHandle"), [$jsilcore.TypeRef("System.Runtime.InteropServices.GCHandleType")], []),
    function AllocHandle (type) {
      // FIXME: type
      return System.Runtime.InteropServices.GCHandle.Alloc(
        this._Array, type
      );
    }
  );

  $.Method(
    {Public: true , Static: false}, "Dispose",
    JSIL.MethodSignature.Void,
    function Dispose () {
      if (!this.IsNotDisposed)
        // FIXME: Throw
        return;

      this.IsNotDisposed = false;

      this.Module._free(this.EmscriptenOffset);
    }
  );

  $.Property({}, "Array");

  $.ImplementInterfaces(
    /* 0 */ System.IDisposable
  );
});


JSIL.PInvoke.CallContext = function (module) {
  // HACK: Save and restore the current call context value.
  // This is important so operations like AllocHGlobal can choose the right module.
  this.prior = JSIL.PInvoke.CurrentCallContext;
  JSIL.PInvoke.CurrentCallContext = this;

  this.module = module;
  this.allocations = [];
  this.cleanups = [];
};

JSIL.PInvoke.CallContext.prototype.Allocate = function (sizeBytes) {
  // HACK
  var padding = 64;

  var offset = this.module._malloc(sizeBytes + padding);
  this.allocations.push(offset);

  return offset;
};

JSIL.PInvoke.CallContext.prototype.Dispose = function () {
  JSIL.PInvoke.CurrentCallContext = this.prior;

  if (this.cleanups)
  for (var i = 0, l = this.cleanups.length; i < l; i++) {
    var c = this.cleanups[i];
    c();
  }

  if (this.allocations)
  for (var i = 0, l = this.allocations.length; i < l; i++) {
    var a = this.allocations[i];
    this.module._free(a);
  }

  this.cleanups = null;
  this.allocations = null;
};

// FIXME: Kill this
JSIL.PInvoke.CallContext.prototype.QueueCleanup = function (callback) {
  this.cleanups.push(callback);
}


// JavaScript is garbage
JSIL.PInvoke.SetupMarshallerPrototype = function (t) {
  t.prototype = Object.create(JSIL.PInvoke.BaseMarshallerPrototype);
  t.prototype.constructor = t;
};


JSIL.PInvoke.BaseMarshallerPrototype = Object.create(Object.prototype);

JSIL.PInvoke.BaseMarshallerPrototype.GetSignatureToken = function (type) {
  JSIL.RuntimeError("Marshaller of type '" + this.constructor.name + "' has no signature token implementation");
};


JSIL.PInvoke.ByValueMarshaller = function ByValueMarshaller (type) {
  this.type = type;

  if (type.__IsEnum__)
    JSIL.RuntimeError("ByValueMarshaller must not be used for enums");
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.ByValueMarshaller);

JSIL.PInvoke.ByValueMarshaller.prototype.GetSignatureToken = function () {
  var storageType = this.type.__IsEnum__ ? this.type.__StorageType__ : this.type;
  switch (storageType.__FullName__) {
    case "System.Int32":
    case "System.UInt32":
    case "System.Boolean":
      return "i";
    case "System.Single":
    case "System.Double":
      return "d";
  }

  JSIL.RuntimeError("No signature token for type '" + this.type.__FullName__ + "'");
};

JSIL.PInvoke.ByValueMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  return managedValue;
};

JSIL.PInvoke.ByValueMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  return nativeValue;
};


JSIL.PInvoke.EnumMarshaller = function EnumMarshaller (type) {
  this.type = type;

  if (!type.__IsEnum__)
    JSIL.RuntimeError("Expected enum");
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.EnumMarshaller);

JSIL.PInvoke.EnumMarshaller.prototype.GetSignatureToken = function () {
  // FIXME: Does the emscripten ABI do anything special here?
  return "i";

  /*
  var storageType = this.type.__StorageType__;
  switch (storageType.__FullName__) {
    case "System.Int32":
    case "System.UInt32":
    case "System.Boolean":
      return "i";
  }

  JSIL.RuntimeError("No signature token for type '" + this.type.__FullName__ + "'");
  */
};

JSIL.PInvoke.EnumMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  if (typeof (managedValue) !== "object")
    JSIL.RuntimeError("Expected a managed enum instance");

  return managedValue.value;
};

JSIL.PInvoke.EnumMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  return this.type.$Cast(nativeValue);
};


JSIL.PInvoke.BoxedValueMarshaller = function BoxedValueMarshaller (type) {
  this.type = type;
  this.sizeInBytes = JSIL.GetNativeSizeOf(type, false);
  this.namedReturnValue = true;
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.BoxedValueMarshaller);

JSIL.PInvoke.BoxedValueMarshaller.prototype.AllocateZero = function (callContext) {
  return callContext.Allocate(this.sizeInBytes);
};

JSIL.PInvoke.BoxedValueMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var module = callContext.module;

  var offset = callContext.Allocate(this.sizeInBytes);

  var memoryRange = JSIL.GetMemoryRangeForBuffer(module.HEAPU8.buffer);
  var emscriptenMemoryView = memoryRange.getView(this.type);

  var emscriptenPointer = JSIL.NewPointer(
    this.type, memoryRange, emscriptenMemoryView, offset
  );

  emscriptenPointer.set(managedValue);

  return offset;
};

JSIL.PInvoke.BoxedValueMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  var module = callContext.module;

  var memoryRange = JSIL.GetMemoryRangeForBuffer(module.HEAPU8.buffer);
  var emscriptenMemoryView = memoryRange.getView(this.type);

  var emscriptenPointer = JSIL.NewPointer(
    this.type, memoryRange, emscriptenMemoryView, nativeValue
  );

  return emscriptenPointer.get();
};


JSIL.PInvoke.ByValueStructMarshaller = function ByValueStructMarshaller (type) {
  this.type = type;
  this.sizeInBytes = JSIL.GetNativeSizeOf(type, true);
  this.marshaller = JSIL.$GetStructMarshaller(type);
  this.unmarshalConstructor = JSIL.$GetStructUnmarshalConstructor(type);

  // clang/emscripten optimization for single-member structs as return values
  if (JSIL.GetFieldList(type).length === 1) {
    this.unpackedReturnValue = true;
    this.namedReturnValue = false;
  } else {
    this.unpackedReturnValue = false;
    this.namedReturnValue = true;
  }
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.ByValueStructMarshaller);

JSIL.PInvoke.ByValueStructMarshaller.prototype.GetSignatureToken = function () {
  return "i";
};

JSIL.PInvoke.ByValueStructMarshaller.prototype.AllocateZero = function (callContext) {
  return callContext.Allocate(this.sizeInBytes);
};

JSIL.PInvoke.ByValueStructMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var module = callContext.module;

  var offset = callContext.Allocate(this.sizeInBytes);
  this.marshaller(managedValue, module.HEAPU8, offset);

  return offset;
};

JSIL.PInvoke.ByValueStructMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  var module = callContext.module;

  if (this.unpackedReturnValue) {
    // FIXME: Is this always an int32?
    var scratchBytes = $jsilcore.BytesFromInt32(nativeValue);
    return new (this.unmarshalConstructor)(scratchBytes, 0);
  } else {
    return new (this.unmarshalConstructor)(module.HEAPU8, nativeValue);
  }
};


JSIL.PInvoke.IntPtrMarshaller = function IntPtrMarshaller () {
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.IntPtrMarshaller);

JSIL.PInvoke.IntPtrMarshaller.prototype.GetSignatureToken = function () {
  return "i";
};

JSIL.PInvoke.IntPtrMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  if (managedValue.pointer) {
    if (managedValue.pointer.__IsNull__)
      return 0;

    var sourceBuffer = managedValue.pointer.memoryRange.buffer;
    var destBuffer = callContext.module.HEAPU8.buffer;

    if (destBuffer === sourceBuffer) {
      // Pointer is in the correct heap, so marshal as-is.
      return managedValue.pointer.offsetInBytes | 0;
    } else {
      // HACK: Use the length of the underlying memory range the pointer
      //  is aimed at as the length of the region being marshalled.
      // Best we can do. Will be correct for trivial cases (pinned an array)
      return JSIL.PInvoke.ArrayMarshaller.CreateTemporaryNativeCopy(
        managedValue.pointer, managedValue.pointer.memoryRange.length, callContext,
        // HACK: no way to infer isOut reliably here
        true
      );
    }

  } else {
    // HACK: We have no way to know this address is in the correct heap.

    return managedValue.value | 0;
  }
};

JSIL.PInvoke.IntPtrMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  return JSIL.PInvoke.CreateIntPtrForModule(callContext.module, nativeValue);
};


JSIL.PInvoke.PointerMarshaller = function PointerMarshaller (type) {
  this.type = type;
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.PointerMarshaller);

JSIL.PInvoke.PointerMarshaller.prototype.GetSignatureToken = function () {
  return "i";
};
JSIL.PInvoke.PointerMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var module = callContext.module;

  if (managedValue.__IsNull__)
    return 0;

  if (managedValue.memoryRange.buffer !== module.HEAPU8.buffer)
    JSIL.RuntimeError("Pointer is not pinned inside the emscripten heap");

  return managedValue.offsetInBytes;
};

JSIL.PInvoke.PointerMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  JSIL.RuntimeError("Not implemented");
};


JSIL.PInvoke.ByRefMarshaller = function ByRefMarshaller (type) {
  this.type = type;
  // XXX (Mispy): should this be necessary? the first is null for ref System.String
  this.innerType = type.__ReferentType__.__Type__ || type.__ReferentType__;
  this.innerMarshaller = JSIL.PInvoke.GetMarshallerForType(this.innerType, true);
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.ByRefMarshaller);

JSIL.PInvoke.ByRefMarshaller.prototype.GetSignatureToken = function () {
  return "i";
};

JSIL.PInvoke.ByRefMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var emscriptenOffset = this.innerMarshaller.ManagedToNative(managedValue.get(), callContext);

  var innerMarshaller = this.innerMarshaller;

  callContext.QueueCleanup(function () {
    managedValue.set(innerMarshaller.NativeToManaged(emscriptenOffset, callContext));
  });

  return emscriptenOffset;
};

JSIL.PInvoke.ByRefMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  JSIL.RuntimeError("Not valid for byref arguments");
};


JSIL.PInvoke.ByRefStringMarshaller = function ByRefStringMarshaller () {
  this.innerMarshaller = JSIL.PInvoke.GetMarshallerForType(System.String.__Type__);
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.ByRefStringMarshaller);

JSIL.PInvoke.ByRefStringMarshaller.prototype.GetSignatureToken = function () {
  return "i";
};

JSIL.PInvoke.ByRefStringMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var module = callContext.module;
  var addressOffset = callContext.Allocate(4);

  var emscriptenOffset = this.innerMarshaller.ManagedToNative(managedValue.get(), callContext);
  module.HEAPU32[addressOffset / 4] = emscriptenOffset;

  var innerMarshaller = this.innerMarshaller;

  callContext.QueueCleanup(function () {
    var newOffset = module.HEAPU32[addressOffset / 4];

    if (newOffset != emscriptenOffset)
      managedValue.set(innerMarshaller.NativeToManaged(newOffset, callContext));
  });

  return addressOffset;
};

JSIL.PInvoke.ByRefStringMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  // FIXME: Is this right?
  var resultString = innerMarshaller.NativeToManaged(nativeValue, callContext);
  return new JSIL.BoxedValue(resultString);
};


JSIL.PInvoke.ArrayMarshaller = function ArrayMarshaller (type, isOut) {
  this.type = type;
  this.elementType = type.__ElementType__;
  this.isOut = isOut;
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.ArrayMarshaller);

JSIL.PInvoke.ArrayMarshaller.prototype.GetSignatureToken = function () {
  return "i";
};

JSIL.PInvoke.ArrayMarshaller.CreateTemporaryNativeCopy = function (pointer, sizeBytes, callContext, isOut) {
  var module = callContext.module;

  if (pointer.__IsNull__)
    return 0;

  if (pointer.memoryRange.buffer === module.HEAPU8.buffer) {
    return pointer.offsetInBytes | 0;
  } else {
    // Copy to temporary storage on the emscripten heap, then copy back after the call
    var emscriptenOffset = callContext.Allocate(sizeBytes);

    var sourceView = pointer.asView($jsilcore.System.Byte, sizeBytes);
    var destView = new Uint8Array(module.HEAPU8.buffer, emscriptenOffset, sizeBytes);

    destView.set(sourceView, 0);

    if (isOut) {
      callContext.QueueCleanup(function () {
        sourceView.set(destView, 0);
      });
    }

    return emscriptenOffset;
  }
};

JSIL.PInvoke.ArrayMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var module = callContext.module;

  if (this.elementType.__FullName__ == "System.String") {
    // for string arrays, just marshal a pointer to each of them individually
    var pointers = new Uint32Array(managedValue.length);
    var stringMarshaller = new JSIL.PInvoke.StringMarshaller();
    for (var i = 0; i < managedValue.length; i++) {
      pointers[i] = stringMarshaller.ManagedToNative(managedValue[i], callContext);
    }

    var nPointerBytes = pointers.length * pointers.BYTES_PER_ELEMENT;
    var pointerPtr = module._malloc(nPointerBytes);

    var pointerHeap = new Uint8Array(module.HEAPU8.buffer, pointerPtr, nPointerBytes);
    pointerHeap.set(new Uint8Array(pointers.buffer));

    return pointerHeap.byteOffset;
  }

  var pointer = JSIL.PinAndGetPointer(managedValue, 0, false);

  if (pointer === null) {
    // Array can't be pinned, so copy to temporary storage one item at a time, then back
    // FIXME: Generate a one-time performance warning if this array is big
    var arrayLength = managedValue.length;
    var itemSizeBytes = JSIL.GetNativeSizeOf(this.elementType);
    var sizeBytes = arrayLength * itemSizeBytes;

    var emscriptenOffset = callContext.Allocate(sizeBytes);
    var destPointer = JSIL.PInvoke.CreateBytePointerForModule(module, emscriptenOffset).cast(this.elementType);

    for (var i = 0; i < arrayLength; i++)
      destPointer.setElement(i, managedValue[i]);

    if (this.isOut) {
      callContext.QueueCleanup(function () {
        for (var i = 0; i < arrayLength; i++)
          managedValue[i] = destPointer.getElement(i);
      });
    }

    return emscriptenOffset;
  }

  return JSIL.PInvoke.ArrayMarshaller.CreateTemporaryNativeCopy(
    pointer, managedValue.byteLength, callContext, this.isOut
  );
};

JSIL.PInvoke.ArrayMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  JSIL.RuntimeError("Not valid for array arguments");
};


JSIL.PInvoke.StringBuilderMarshaller = function StringBuilderMarshaller (charSet) {
  if (charSet)
    JSIL.RuntimeError("Not implemented");
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.StringBuilderMarshaller);

JSIL.PInvoke.StringBuilderMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var sizeInBytes = managedValue.get_Capacity();
  var emscriptenOffset = callContext.Allocate(sizeInBytes + 1);

  var module = callContext.module;

  var tByte = $jsilcore.System.Byte.__Type__;
  var memoryRange = JSIL.GetMemoryRangeForBuffer(module.HEAPU8.buffer);
  var emscriptenMemoryView = memoryRange.getView(tByte);

  for (var i = 0, l = sizeInBytes + 1; i < l; i++)
    module.HEAPU8[(i + emscriptenOffset) | 0] = 0;

  System.Text.Encoding.ASCII.GetBytes(
    managedValue._str, 0, managedValue._str.length, module.HEAPU8, emscriptenOffset
  );

  callContext.QueueCleanup(function () {
    managedValue._str = JSIL.StringFromNullTerminatedByteArray(
      module.HEAPU8, emscriptenOffset, managedValue._capacity
    );
  });

  return emscriptenOffset;
};

JSIL.PInvoke.StringBuilderMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  JSIL.RuntimeError("Not valid");
};


JSIL.PInvoke.StringMarshaller = function StringMarshaller (charSet) {
  if (charSet)
    JSIL.RuntimeError("Not implemented");

  this.allocates = true;
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.StringMarshaller);

JSIL.PInvoke.StringMarshaller.prototype.GetSignatureToken = function() {
  return "i";
}

JSIL.PInvoke.StringMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var sizeInBytes = managedValue.length;
  var emscriptenOffset = callContext.Allocate(sizeInBytes + 1);

  var module = callContext.module;

  var tByte = $jsilcore.System.Byte.__Type__;
  var memoryRange = JSIL.GetMemoryRangeForBuffer(module.HEAPU8.buffer);
  var emscriptenMemoryView = memoryRange.getView(tByte);

  for (var i = 0, l = sizeInBytes + 1; i < l; i++)
    module.HEAPU8[(i + emscriptenOffset) | 0] = 0;

  System.Text.Encoding.ASCII.GetBytes(
    managedValue, 0, managedValue.length, module.HEAPU8, emscriptenOffset
  );

  return emscriptenOffset;
};

JSIL.PInvoke.StringMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  var module = callContext.module;

  if (nativeValue < 0) {
      JSIL.RuntimeErrorFormat("StringMarshaller NativeToManaged got a negative nativeValue ({0})", [nativeValue]);
      return null;
  }

  var length = 0;
  while (true) {
    if (module.HEAPU8[(nativeValue + length) | 0] == 0) {
      break;
    }
    length += 1;
  }

  var memoryRange = new JSIL.MemoryRange(module.HEAPU8.buffer, nativeValue, length);

  var tByte = $jsilcore.System.Byte.__Type__;
  var view = memoryRange.getView(tByte);

  var s = System.Text.Encoding.ASCII.GetString(view);
  JSIL.WarningFormat("assuming string is ASCII-encoded: {0}", [s]);
  return s;
}


JSIL.PInvoke.DelegateMarshaller = function DelegateMarshaller (type) {
  this.type = type;
};

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.DelegateMarshaller);

JSIL.PInvoke.DelegateMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var module = callContext.module;

  var wrapper = JSIL.PInvoke.CreateNativeToManagedWrapper(
    module, managedValue, this.type.__Signature__
  );

  var functionPointer = module.Runtime.addFunction(wrapper);

  callContext.QueueCleanup(function () {
    module.Runtime.removeFunction(functionPointer);
  });

  return functionPointer;
};

JSIL.PInvoke.DelegateMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  var module = callContext.module;

  var wrapper = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer$b1(
    this.type
  )(
    JSIL.PInvoke.CreateIntPtrForModule(callContext.module, nativeValue)
  );

  return wrapper;
};


// Fallback UnimplementedMarshaller delays error until the actual point of attempted marshalling
JSIL.PInvoke.UnimplementedMarshaller = function UnimplementedMarshaller (type, errorMsg) {
  this.type = type;
}

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.UnimplementedMarshaller);

JSIL.PInvoke.UnimplementedMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  JSIL.RuntimeErrorFormat("Type '{0}' has no marshalling implementation", [this.type.__FullName__]);
}

JSIL.PInvoke.UnimplementedMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  JSIL.RuntimeErrorFormat("Type '{0}' has no marshalling implementation", [this.type.__FullName__]);
}


JSIL.PInvoke.ManagedMarshaller = function ManagedMarshaller (type, customMarshalerType, cookie) {
  this.type = type;
  this.customMarshalerPublicInterface = JSIL.ResolveTypeReference(customMarshalerType)[0];

  if (typeof (cookie) === "string")
    this.cookie = cookie;
  else
    this.cookie = null;

  this.cachedInstance = null;
}

JSIL.PInvoke.SetupMarshallerPrototype(JSIL.PInvoke.ManagedMarshaller);

JSIL.PInvoke.ManagedMarshaller.prototype.GetInstance = function () {
  if (!this.cachedInstance)
    this.cachedInstance = this.customMarshalerPublicInterface.GetInstance(this.cookie);

  return this.cachedInstance;
};

JSIL.PInvoke.ManagedMarshaller.prototype.GetSignatureToken = function () {
  return "i";
};

JSIL.PInvoke.ManagedMarshaller.prototype.ManagedToNative = function (managedValue, callContext) {
  var instance = this.GetInstance();

  var ptr = instance.MarshalManagedToNative(managedValue);

  callContext.QueueCleanup(function () {
    instance.CleanUpNativeData(ptr);
  });

  var emscriptenOffset = ptr.ToInt32();
  return emscriptenOffset;
}

JSIL.PInvoke.ManagedMarshaller.prototype.NativeToManaged = function (nativeValue, callContext) {
  var instance = this.GetInstance();

  var ptr = JSIL.PInvoke.CreateIntPtrForModule(callContext.module, nativeValue);

  var managedValue = instance.MarshalNativeToManaged(ptr);

  callContext.QueueCleanup(function () {
    instance.CleanUpManagedData(managedValue);
  });

  return managedValue;
}


JSIL.PInvoke.WrapManagedCustomMarshaler = function (type, customMarshaler, cookie) {
  return new JSIL.PInvoke.ManagedMarshaller(type, customMarshaler, cookie);
};

JSIL.PInvoke.GetMarshallerForType = function (type, box, isOut) {
  // FIXME: Caching

  if (type.__IsByRef__) {
    var rt = type.__ReferentType__.__Type__ || type.__ReferentType__;
    if (rt && rt.__FullName__ === "System.String")
      return new JSIL.PInvoke.ByRefStringMarshaller();
    else
      return new JSIL.PInvoke.ByRefMarshaller(type);
  }

  var typeName = type.__FullNameWithoutArguments__ || type.__FullName__;

  switch (typeName) {
    case "System.IntPtr":
    case "System.UIntPtr":
      return new JSIL.PInvoke.IntPtrMarshaller();

    case "JSIL.Pointer":
      return new JSIL.PInvoke.PointerMarshaller(type);

    case "System.Text.StringBuilder":
      return new JSIL.PInvoke.StringBuilderMarshaller();

    case "System.String":
      return new JSIL.PInvoke.StringMarshaller();
  }

  if (type.__IsDelegate__) {
    return new JSIL.PInvoke.DelegateMarshaller(type);
  } else if (type.__IsNativeType__) {
    if (box)
      return new JSIL.PInvoke.BoxedValueMarshaller(type);
    else
      return new JSIL.PInvoke.ByValueMarshaller(type);
  } else if (type.__IsStruct__) {
    return new JSIL.PInvoke.ByValueStructMarshaller(type);
  } else if (type.__IsEnum__) {
    return new JSIL.PInvoke.EnumMarshaller(type);
  } else if (type.__IsArray__) {
    return new JSIL.PInvoke.ArrayMarshaller(type, isOut);
  } else {
    return new JSIL.PInvoke.UnimplementedMarshaller(type);
  }
};

JSIL.PInvoke.FindNativeMethod = function (module, methodName) {
  var key = "_" + methodName;

  return module[key];
};

JSIL.PInvoke.GetMarshallersForSignature = function (methodSignature, pInvokeInfo) {
  var argumentMarshallers = new Array(methodSignature.argumentTypes.length);
  for (var i = 0, l = argumentMarshallers.length; i < l; i++) {
    var argumentType = methodSignature.argumentTypes[i];
    var resolvedArgumentType = JSIL.ResolveTypeReference(argumentType)[1];
    var marshalInfo = null;
    if (pInvokeInfo && pInvokeInfo.Parameters)
      marshalInfo = pInvokeInfo.Parameters[i];

    if (marshalInfo && marshalInfo.CustomMarshaler) {
      argumentMarshallers[i] = JSIL.PInvoke.WrapManagedCustomMarshaler(resolvedArgumentType, marshalInfo.CustomMarshaler, marshalInfo.Cookie);
    } else {
      var isOut = false;
      if (marshalInfo)
        isOut = marshalInfo.Out || false;
      argumentMarshallers[i] = JSIL.PInvoke.GetMarshallerForType(resolvedArgumentType, false, isOut);
    }
  }

  var resolvedReturnType = null, resultMarshaller = null;

  if (methodSignature.returnType) {
    resolvedReturnType = JSIL.ResolveTypeReference(methodSignature.returnType)[1];

    if (pInvokeInfo && pInvokeInfo.Result && pInvokeInfo.Result.CustomMarshaler) {
      resultMarshaller = JSIL.PInvoke.WrapManagedCustomMarshaler(resolvedReturnType, pInvokeInfo.Result.CustomMarshaler, pInvokeInfo.Result.Cookie);
    } else {
      resultMarshaller = JSIL.PInvoke.GetMarshallerForType(resolvedReturnType);
    }
  }

  return {
    arguments: argumentMarshallers,
    result: resultMarshaller
  };
};

JSIL.PInvoke.CreateManagedToNativeWrapper = function (module, nativeMethod, methodName, methodSignature, pInvokeInfo, marshallers) {
  if (!marshallers)
    marshallers = JSIL.PInvoke.GetMarshallersForSignature(methodSignature, pInvokeInfo);

  var structResult = marshallers.result && marshallers.result.namedReturnValue;
  var convertOffset = structResult ? 1 : 0;
  var argc = methodSignature.argumentTypes.length;

  var argumentNames = new Array(argc + 1);

  argumentNames[0] = "context";
  for (var i = 0; i < argc; i++)
    argumentNames[i + 1] = "arg" + i;

  var closure = {
    nativeMethod: nativeMethod,
    resultMarshaller: marshallers.result
  };

  var body = [];

  for (var i = 0; i < argc; i++) {
    closure["marshaller" + i] = marshallers.arguments[i];

    body.push("arg" + i + " = marshaller" + i + ".ManagedToNative(arg" + i + ", context);");
  }

  if (structResult)
    body.push("var result = resultMarshaller.AllocateZero(context);");

  body.push("var nativeResult = nativeMethod(");

  if (structResult)
    body.push("  result" + ((argc !== 0) ? ", " : ""));
  for (var i = 0; i < argc; i++)
    body.push("  arg" + i + ((i === argc - 1) ? "" : ", "));

  body.push(");");

  if (structResult)
    body.push("return resultMarshaller.NativeToManaged(result, context);");
  else if (marshallers.result)
    body.push("return resultMarshaller.NativeToManaged(nativeResult, context);");
  else
    body.push("return nativeResult");

  var wrapper = JSIL.CreateNamedFunction(
    methodName + ".PInvokeWrapper", argumentNames,
    body.join("\n"),
    closure
  );

  // Now generate an exception handling wrapper.
  // We split the context creation/disposal out from the rest,
  //  because try-catch and try-finally prevent optimization in
  //  some JS runtimes (V8 :-( )

  body.length = 0;

  body.push("var context = new callContext(module);");
  body.push("try {");
  body.push("  return invoke(");

  body.push("    context" + ((argc !== 0) ? ", " : ""));
  for (var i = 0; i < argc; i++)
    body.push("    arg" + i + ((i === argc - 1) ? "" : ", "));

  body.push("  );");
  body.push("} finally {");
  body.push("  context.Dispose();");
  body.push("}");

  var wrapperArgumentNames = argumentNames.slice(1);
  var tryCatchWrapper = JSIL.CreateNamedFunction(
    "JSIL.PInvoke.ErrorHandler[" + argc + "]", wrapperArgumentNames,
    body.join("\n"),
    {
      callContext: JSIL.PInvoke.CallContext,
      module: module,
      invoke: wrapper
    }
  );

  return tryCatchWrapper;
};

JSIL.PInvoke.CreateNativeToManagedWrapper = function (module, managedFunction, methodSignature, pInvokeInfo) {
  var marshallers = JSIL.PInvoke.GetMarshallersForSignature(methodSignature, pInvokeInfo);

  if (marshallers.result && marshallers.result.allocates)
    JSIL.RuntimeError("Return type '" + methodSignature.returnType + "' is not valid because it allocates on the native heap");

  var structResult = marshallers.result && marshallers.result.namedReturnValue;

  var wrapper = function SimplePInvokeWrapper () {
    var context = new JSIL.PInvoke.CallContext(module);

    var argc = arguments.length | 0;
    var convertOffset = structResult ? 1 : 0;

    var convertedArguments = new Array(argc);
    for (var i = 0, l = argc - convertOffset; i < l; i++)
      convertedArguments[i] = marshallers.arguments[i].NativeToManaged(arguments[i + convertOffset], context);

    try {
      var managedResult;

      managedResult = managedFunction.apply(this, convertedArguments);

      if (structResult) {
        // HACK: FIXME: Oh god
        var structMarshaller = marshallers.result.marshaller;
        structMarshaller(managedResult, module.HEAPU8, arguments[0]);
        return;
      } else if (marshallers.result) {
        // FIXME: What happens if the return value has to get pinned into the emscripten heap?
        return marshallers.result.ManagedToNative(managedResult, context);
      } else {
        return managedResult;
      }
    } finally {
      context.Dispose();
    }
  };

  return wrapper;
};

JSIL.PInvoke.CreateUniversalFunctionPointerForDelegate = function (delegate, signature) {
  var result = null;

  var nm = JSIL.__NativeModules__;
  for (var k in nm) {
    if (!nm.hasOwnProperty(k))
      continue;

    if (k === "__global__")
      continue;

    var module = nm[k];

    var wrappedFunction = JSIL.PInvoke.CreateNativeToManagedWrapper(module, delegate, signature);

    var localFp = module.Runtime.addFunction(wrappedFunction);
    if (result === null)
      result = localFp;
    else if (result !== localFp)
      JSIL.RuntimeError("Emscripten function pointer tables are desynchronized between modules");
  }

  return result;
};

JSIL.ImplementExternals("System.Runtime.InteropServices.Marshal", function ($) {
  var warnedAboutFunctionTable = false;

  $.Method({Static: true , Public: true }, "GetFunctionPointerForDelegate",
    (new JSIL.MethodSignature($.IntPtr, ["!!0"], ["T"])),
    function GetFunctionPointerForDelegate (T, delegate) {
      if (!T.__IsDelegate__)
        JSIL.RuntimeError("Type argument must be a delegate");

      var signature = T.__Signature__;
      if (!signature)
        JSIL.RuntimeError("Delegate type must have a signature");

      var functionPointer = JSIL.PInvoke.CreateUniversalFunctionPointerForDelegate(delegate, signature);

      // FIXME
      var result = new System.IntPtr(functionPointer);
      return result;
    }
  );

  function _GetDelegateForFunctionPointer(T, ptr) {
    if (!T.__IsDelegate__) {
      JSIL.WarningFormat("Cannot get delegate of type {0}: Not a delegate type", [T.__FullName__]);
      return null;
    }

    var signature = T.__Signature__;
    if (!signature) {
      JSIL.WarningFormat("Cannot get delegate of type {0}: Delegate type must have a signature", [T.__FullName__]);
      return null;
    }

    var pInvokeInfo = T.__PInvokeInfo__;
    if (!pInvokeInfo)
      pInvokeInfo = null;

    var methodIndex = ptr.ToInt32();
    var module = JSIL.PInvoke.PickModuleForPointer(ptr, false);

    if (methodIndex === 0) {
      JSIL.WarningFormat("Cannot get delegate of type {0}: Null function pointer", [T.__FullName__]);
      return null;
    }

    var marshallers = JSIL.PInvoke.GetMarshallersForSignature(signature, pInvokeInfo);

    var invokeImplementation = null;

    // Build signature
    var dynCallSignature = "";
    var rm = marshallers.result;

    if (rm) {
      if (rm.namedReturnValue)
        dynCallSignature += "v";
      dynCallSignature += rm.GetSignatureToken(signature.returnType);
    } else {
      dynCallSignature += "v";
    }

    for (var i = 0, l = signature.argumentTypes.length; i < l; i++) {
      var m = marshallers.arguments[i];
      dynCallSignature += m.GetSignatureToken(signature.argumentTypes[i]);
    }

    var functionTable = module["FUNCTION_TABLE_" + dynCallSignature];
    var wrapperName;
    if (functionTable) {
      invokeImplementation = functionTable[methodIndex];
      wrapperName = invokeImplementation.name || "GetDelegateForFunctionPointer_Result";
    } else {
      var dynCallImplementation = module["dynCall_" + dynCallSignature];
      if (!dynCallImplementation) {
        JSIL.WarningFormat("Cannot get delegate of type {0}: No dynCall implementation or function table for signature '{1}'", [T.__FullName__, dynCallSignature]);
        return null;
      }

      if (!warnedAboutFunctionTable) {
        warnedAboutFunctionTable = true;
        JSIL.Host.warning("This emscripten module was compiled without '-s EXPORT_FUNCTION_TABLES=1'. Performance will be compromised.");
      }

      var boundDynCall = function (/* arguments... */) {
        var argc = arguments.length | 0;
        var argumentsList = new Array(argc + 1);
        argumentsList[0] = methodIndex;

        for (var i = 0; i < argc; i++)
          argumentsList[i + 1] = arguments[i];

        return dynCallImplementation.apply(this, argumentsList);
      };

      invokeImplementation = boundDynCall;
      wrapperName = "GetDelegateForFunctionPointer_Slow_Result";
    }

    var wrappedMethod = JSIL.PInvoke.CreateManagedToNativeWrapper(
      module, invokeImplementation, wrapperName,
      signature, pInvokeInfo, marshallers
    );
    return wrappedMethod;
  }

  $.Method({Static: true , Public: true }, "GetDelegateForFunctionPointer",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Delegate"), [$.IntPtr, $jsilcore.TypeRef("System.Type")])),
    function GetDelegateForFunctionPointer (ptr, T) {
      return _GetDelegateForFunctionPointer(T, ptr);
    }
  );


  $.Method({Static: true , Public: true }, "GetDelegateForFunctionPointer",
    (new JSIL.MethodSignature("!!0", [$.IntPtr], ["T"])),
    function GetDelegateForFunctionPointer (T, ptr) {
      return _GetDelegateForFunctionPointer(T, ptr);
    }
  );
});