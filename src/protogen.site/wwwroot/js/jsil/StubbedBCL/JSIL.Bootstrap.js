/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

if (!$jsilcore)
    throw new Error("JSIL.Core is required");

JSIL.DeclareNamespace("System.ComponentModel");
JSIL.DeclareNamespace("System.IO");
JSIL.DeclareNamespace("System.Text.RegularExpressions");
JSIL.DeclareNamespace("System.Diagnostics");
JSIL.DeclareNamespace("System.Collections.Generic");
JSIL.DeclareNamespace("System.Collections.ObjectModel");
JSIL.DeclareNamespace("System.Runtime");
JSIL.DeclareNamespace("System.Runtime.InteropServices");

﻿$jsilcore.InitResizableArray = function (target, elementType, initialSize) {
  target._items = new Array();
};

JSIL.$WrapIComparer = function (T, comparer) {
  var compare;
  if (T !== null) {
    var tComparer = System.Collections.Generic.IComparer$b1.Of(T);
    compare = tComparer.Compare;
  } else {
    compare = System.Collections.IComparer.Compare;
  }

  return function (lhs, rhs) {
    return compare.Call(comparer, null, lhs, rhs);
  };
};
$jsilcore.$ListExternals = function ($, T, type) {
  var mscorlib = JSIL.GetCorlib();

  if (typeof (T) === "undefined")
    JSIL.RuntimeError("Invalid use of $ListExternals");

  var getT;

  switch (type) {
    case "ArrayList":
    case "ObjectCollection":
      getT = function () { return System.Object; }
      break;
    default:
      getT = function (self) { return self.T; }
      break;
  }

  $.Method({ Static: false, Public: true }, ".ctor",
    JSIL.MethodSignature.Void,
    function () {
      $jsilcore.InitResizableArray(this, getT(this), 16);
      this._size = 0;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Int32")], []),
    function (size) {
      $jsilcore.InitResizableArray(this, getT(this), size);
      this._size = 0;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Collections.Generic.IEnumerable`1", [T])], []),
    function (values) {
      this._items = JSIL.EnumerableToArray(values, this.T);
      this._capacity = this._items.length;
      this._size = this._items.length;
    }
  );

  var indexOfImpl = function List_IndexOf(value) {
    return JSIL.Array.IndexOf(this._items, 0, this._size, value);
  };

  var findIndexImpl = function List_FindIndex(predicate) {
    for (var i = 0, l = this._size; i < l; i++) {
      if (predicate(this._items[i]))
        return i;
    }

    return -1;
  };

  var addImpl = function (item) {
    this.InsertItem(this._size, item);
    return this._size;
  };

  var rangeCheckImpl = function (index, size) {
    return (index >= 0) && (size > index);
  }

  var getItemImpl = function (index) {
    if (rangeCheckImpl(index, this._size))
      return this._items[index];
    else
      throw new System.ArgumentOutOfRangeException("index");
  };

  var removeImpl = function (item) {
    var index = JSIL.Array.IndexOf(this._items, 0, this._size, item);
    if (index === -1)
      return false;

    this.RemoveAt(index);
    return true;
  };

  var getEnumeratorType = function (self) {
    if (self.$enumeratorType)
      return self.$enumeratorType;

    var T = getT(self);
    return self.$enumeratorType = System.Collections.Generic.List$b1_Enumerator.Of(T);
  };

  var getEnumeratorImpl = function () {
    var enumeratorType = getEnumeratorType(this);

    return new enumeratorType(this);
  };


  switch (type) {
    case "ArrayList":
    case "ObjectCollection":
      $.Method({ Static: false, Public: true }, "Add",
        new JSIL.MethodSignature($.Int32, [T], []),
        addImpl
      );
      break;
    default:
      $.Method({ Static: false, Public: true }, "Add",
        new JSIL.MethodSignature(null, [T], []),
        addImpl
      );
      break;
  }

  $.Method({ Static: false, Public: true }, "AddRange",
    new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Collections.Generic.IEnumerable`1", [T])], []),
    function (items) {
      var e = JSIL.GetEnumerator(items, this.T);
      var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
      var getCurrent = $jsilcore.System.Collections.IEnumerator.get_Current;
      try {
        while (moveNext.Call(e))
          this.Add(getCurrent.Call(e));
      } finally {
        JSIL.Dispose(e);
      }
    }
  );

  $.Method({ Static: false, Public: true }, "AsReadOnly",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.ObjectModel.ReadOnlyCollection`1", [T]), [], []),
    function () {
      // FIXME
      if (typeof (this.tReadOnlyCollection) === "undefined") {
        this.tReadOnlyCollection = System.Collections.ObjectModel.ReadOnlyCollection$b1.Of(this.T).__Type__;
      }

      return JSIL.CreateInstanceOfType(this.tReadOnlyCollection, "$listCtor", [this]);
    }
  );

  $.Method({ Static: false, Public: true }, "Clear",
    JSIL.MethodSignature.Void,
    function () {
      this.ClearItems();
    }
  );

  $.Method({ Static: false, Public: true }, "set_Capacity",
    new JSIL.MethodSignature(null, [$.Int32], []),
    function List_set_Capacity(value) {
      // FIXME
      return;
    }
  );

  $.Method({ Static: false, Public: true }, "Contains",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [T], []),
    function List_Contains(value) {
      return this.IndexOf(value) >= 0;
    }
  );

  $.Method({ Static: false, Public: true }, "Exists",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [mscorlib.TypeRef("System.Predicate`1", [T])], []),
    function List_Exists(predicate) {
      return this.FindIndex(predicate) >= 0;
    }
  );

  $.Method({ Static: false, Public: true }, "ForEach",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action`1", [T])], []),
    function ForEach(action) {
      for (var i = 0, sz = this._size; i < sz; i++) {
        var item = this._items[i];

        action(item);
      }
    }
  );

  $.Method({ Static: false, Public: true }, "Find",
    new JSIL.MethodSignature(T, [mscorlib.TypeRef("System.Predicate`1", [T])], []),
    function List_Find(predicate) {
      var index = this.FindIndex(predicate);
      if (index >= 0)
        return this._items[index];
      else
        return JSIL.DefaultValue(this.T);
    }
  );

  $.Method({ Static: false, Public: true }, "FindAll",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.List`1", [T]), [mscorlib.TypeRef("System.Predicate`1", [T])], []),
    function (predicate) {
      var thisType = this.GetType();

      // Manually initialize the result since we don't want to hassle with overloaded ctors
      var result = JSIL.CreateInstanceOfType(thisType, null);
      result._items = [];

      for (var i = 0, sz = this._size; i < sz; i++) {
        var item = this._items[i];

        if (predicate(item))
          result._items.push(item);
      }

      result._capacity = result._size = result._items.length;
      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "FindIndex",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Int32"), [mscorlib.TypeRef("System.Predicate`1", [T])], []),
    findIndexImpl
  );

  $.Method({ Static: false, Public: true }, "get_Capacity",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Int32"), [], []),
    function () {
      return this._items.length;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Count",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Int32"), [], []),
    function () {
      return this._size;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Item",
    new JSIL.MethodSignature(T, [mscorlib.TypeRef("System.Int32")], []),
    getItemImpl
  );

  if (type != "ArrayList") {
    $.Method(
      { Static: false, Public: false }, null,
      new JSIL.MethodSignature($.Int32, [$.Object], []),
      addImpl
    ).Overrides("System.Collections.IList", "Add");

    $.Method({ Static: false, Public: true }, null,
      new JSIL.MethodSignature($.Object, [mscorlib.TypeRef("System.Int32")], []),
      getItemImpl
    )
      .Overrides("System.Collections.IList", "get_Item");

    $.Method({ Static: false, Public: true }, null,
      new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Int32"), $.Object], []),
      function (index, value) {
        if (rangeCheckImpl(index, this._size))
          this.SetItem(index, this.T.$Cast(value));
        else
          throw new System.ArgumentOutOfRangeException("index");
      }
    )
      .Overrides("System.Collections.IList", "set_Item");

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature(null, [$.Int32, $.Object], []),
      function (index, item) {
        this.InsertItem(index, item);
      }
    ).Overrides("System.Collections.IList", "Insert");

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($.Int32, [$.Object], []),
      indexOfImpl
    ).Overrides("System.Collections.IList", "IndexOf");

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature(null, [$.Object], []),
      removeImpl
    ).Overrides("System.Collections.IList", "Remove");

    $.Method({ Static: false, Public: true }, "InsertRange",
      new JSIL.MethodSignature(null, [$.Int32, mscorlib.TypeRef("System.Collections.Generic.IEnumerable`1", [T])], []),
      function (index, items) {
        var e = JSIL.GetEnumerator(items, this.T);
        var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
        var getCurrent = $jsilcore.System.Collections.IEnumerator.get_Current;

        try {
          var i = index;

          while (moveNext.Call(e))
            this.InsertItem(i++, getCurrent.Call(e));
        } finally {
          JSIL.Dispose(e);
        }
      }
    );

    var reverseImpl = function (index, count) {
      if (arguments.length < 2) {
        index = 0;
        count = this._size | 0;
      } else {
        index |= 0;
        count |= 0;
      }

      if (count < 1)
        return;

      for (var i = index, l = (index + count - 1) | 0; i < l; i++, l--) {
        var a = this._items[i];
        var b = this._items[l];
        this._items[i] = b;
        this._items[l] = a;
      }
    }

    $.Method({ Static: false, Public: true }, "Reverse",
      new JSIL.MethodSignature(null, [], []),
      reverseImpl
    );

    $.Method({ Static: false, Public: true }, "Reverse",
      new JSIL.MethodSignature(null, [$.Int32, $.Int32], []),
      reverseImpl
    );
  }

  $.Method({ Static: false, Public: true }, "set_Item",
    new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Int32"), T], []),
    function (index, value) {
      if (rangeCheckImpl(index, this._size))
        this.SetItem(index, value);
      else
        throw new System.ArgumentOutOfRangeException("index");
    }
  );

  switch (type) {
    case "List":
      $.Method({ Static: false, Public: true }, "GetEnumerator",
        (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.List`1+Enumerator", [T]), [], [])),
        getEnumeratorImpl
      );

      $.Method({ Static: false, Public: true }, null,
        new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.IEnumerator`1", [T]), [], []),
        getEnumeratorImpl
      )
        .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

      break;

    case "ArrayList":
      break;

    default:
      $.Method({ Static: false, Public: true }, "GetEnumerator",
        new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.IEnumerator`1", [T]), [], []),
        getEnumeratorImpl
      )
        .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

      break;
  }

  $.Method({ Static: false, Public: false }, null,
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.IEnumerator"), [], []),
    getEnumeratorImpl
  )
    .Overrides("System.Collections.IEnumerable", "GetEnumerator");

  $.RawMethod(false, "$GetEnumerator", getEnumeratorImpl);

  $.Method({ Static: false, Public: true }, "Insert",
    (new JSIL.MethodSignature(null, [$.Int32, T], [])),
    function Insert(index, item) {
      this.InsertItem(index, item);
    }
  );

  $.Method({ Static: false, Public: true }, "IndexOf",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Int32"), [T], []),
    indexOfImpl
  );

  switch (type) {
    case "ArrayList":
    case "ObjectCollection":
      $.Method({ Static: false, Public: true }, "Remove",
        new JSIL.MethodSignature(null, [T], []),
        removeImpl
      );
      break;
    default:
      $.Method({ Static: false, Public: true }, "Remove",
        new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [T], []),
        removeImpl
      );
      break;
  }

  $.Method({ Static: false, Public: true }, "RemoveAll",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Int32"), [mscorlib.TypeRef("System.Predicate`1", [T])], []),
    function (predicate) {
      var result = 0;

      for (var i = 0; i < this._size; i++) {
        var item = this._items[i];

        if (predicate(item)) {
          this.RemoveItem(i);
          i -= 1;
          result += 1;
        }
      }

      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "RemoveAt",
    new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Int32")], []),
    function (index) {
      if (!rangeCheckImpl(index, this._size))
        throw new System.ArgumentOutOfRangeException("index");

      this.RemoveItem(index);
    }
  );

  $.Method({ Static: false, Public: true }, "RemoveRange",
    new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Int32"), mscorlib.TypeRef("System.Int32")], []),
    function (index, count) {
      if (index < 0)
        throw new System.ArgumentOutOfRangeException("index");
      else if (count < 0)
        throw new System.ArgumentOutOfRangeException("count");
      else if (!rangeCheckImpl(index, this._size))
        throw new System.ArgumentException();
      else if (!rangeCheckImpl(index + count - 1, this._size))
        throw new System.ArgumentException();

      this._items.splice(index, count);
      this._size -= count;
    }
  );

  $.Method({ Static: false, Public: true }, "Sort",
    JSIL.MethodSignature.Void,
    function () {
      this._items.length = this._size;
      this._items.sort(JSIL.CompareValues);
    }
  );

  $.Method({ Static: false, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Comparison`1", [T])], []),
    function (comparison) {
      this._items.length = this._size;
      this._items.sort(comparison);
    }
  );

  $.Method({ Static: false, Public: true }, "Sort",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.IComparer")], [])),
    function Sort(comparer) {
      this._items.length = this._size;
      this._items.sort(JSIL.$WrapIComparer(null, comparer));
    }
  );

  $.Method({ Static: false, Public: true }, "Sort",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.IComparer`1", [T])], [])),
    function Sort(comparer) {
      this._items.length = this._size;
      this._items.sort(JSIL.$WrapIComparer(this.T, comparer));
    }
  );

  $.Method({ Static: false, Public: true }, "BinarySearch",
    (new JSIL.MethodSignature($.Int32, [
          $.Int32, $.Int32,
          T,
          $jsilcore.TypeRef("System.Collections.Generic.IComparer`1", [T])
    ], [])),
    function BinarySearch(index, count, item, comparer) {
      return JSIL.BinarySearch(
        this.T, this._items, index, count,
        item, comparer
      );
    }
  );

  $.Method({ Static: false, Public: true }, "BinarySearch",
    (new JSIL.MethodSignature($.Int32, [T], [])),
    function BinarySearch(item) {
      return JSIL.BinarySearch(
        this.T, this._items, 0, this._size,
        item, null
      );
    }
  );

  $.Method({ Static: false, Public: true }, "BinarySearch",
    (new JSIL.MethodSignature($.Int32, [
      T,
      $jsilcore.TypeRef("System.Collections.Generic.IComparer`1", [T])
    ], [])),
    function BinarySearch(item, comparer) {
      return JSIL.BinarySearch(
        this.T, this._items, 0, this._size,
        item, comparer
      );
    }
  );

  $.Method({ Static: false, Public: true }, "ToArray",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [T]), [], []),
    function () {
      var result = JSIL.Array.New(this.T, this._size);

      for (var i = 0, l = this._size, items = this._items; i < l; i++) {
        result[i] = items[i];
      }

      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "TrueForAll",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [mscorlib.TypeRef("System.Predicate`1", [T])], []),
    function (predicate) {
      for (var i = 0; i < this._size; i++) {
        var item = this._items[i];

        if (!predicate(item))
          return false;
      }

      return true;
    }
  );

  $.Method({ Static: false, Public: false, Virtual: true }, "ClearItems",
    JSIL.MethodSignature.Void,
    function ClearItems() {
      // Necessary to clear any element values.
      var oldLength = this._items.length;
      this._items.length = 0;
      this._items.length = oldLength;

      this._size = 0;
    }
  );

  $.Method({ Static: false, Public: false, Virtual: true }, "InsertItem",
    new JSIL.MethodSignature(null, [$.Int32, new JSIL.GenericParameter("T", "System.Collections.ObjectModel.Collection`1")], []),
    function InsertItem(index, item) {
      index = index | 0;

      if (index >= this._items.length) {
        this._items.push(item);
      } else if (index >= this._size) {
        this._items[index] = item;
      } else {
        this._items.splice(index, 0, item);
      }

      this._size += 1;

      if (this.$OnItemAdded)
        this.$OnItemAdded(item);
    }
  );

  $.Method({ Static: false, Public: false, Virtual: true }, "RemoveItem",
    new JSIL.MethodSignature(null, [$.Int32], []),
    function RemoveItem(index) {
      this._items.splice(index, 1);
      this._size -= 1;
    }
  );

  $.Method({ Static: false, Public: false, Virtual: true }, "SetItem",
    new JSIL.MethodSignature(null, [$.Int32, new JSIL.GenericParameter("T", "System.Collections.ObjectModel.Collection`1")], []),
    function SetItem(index, item) {
      this._items[index] = item;
    }
  );
};
JSIL.Dispose = function (disposable) {
  if (typeof (disposable) === "undefined")
    JSIL.RuntimeError("Disposable is undefined");
  else if (disposable === null)
    return false;

  var tIDisposable = $jsilcore.System.IDisposable;

  if (tIDisposable.$Is(disposable))
    tIDisposable.Dispose.Call(disposable);
  else if (typeof (disposable.Dispose) === "function")
    disposable.Dispose();
  else
    return false;

  return true;
};
JSIL.EnumerableToArray = function (enumerable, elementType) {
  var e = JSIL.GetEnumerator(enumerable, elementType);
  var result = [];

  var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
  var getCurrent = $jsilcore.System.Collections.IEnumerator.get_Current;

  try {
    while (moveNext.Call(e))
      result.push(getCurrent.Call(e));
  } finally {
    JSIL.Dispose(e);
  }

  return result;
};
$jsilcore.$tArrayEnumerator = null;

JSIL.MakeArrayEnumerator = function (array, elementType) {
  var tArrayEnumerator;

  if (!elementType) {
    if ($jsilcore.$tArrayEnumerator === null)
      $jsilcore.$tArrayEnumerator = JSIL.ArrayEnumerator.Of(System.Object);

    tArrayEnumerator = $jsilcore.$tArrayEnumerator;
  } else {
    tArrayEnumerator = JSIL.ArrayEnumerator.Of(elementType);
  }

  return new tArrayEnumerator(array, -1);
};

JSIL.GetEnumerator = function (enumerable, elementType, fallbackMethodInvoke) {
  if ((typeof (enumerable) === "undefined") || (enumerable === null))
    JSIL.RuntimeError("Enumerable is null or undefined");

  var tIEnumerable = $jsilcore.System.Collections.IEnumerable;
  var tIEnumerable$b1 = null;

  if (!elementType)
    elementType = $jsilcore.System.Object.__Type__;
  else
    tIEnumerable$b1 = $jsilcore.System.Collections.Generic.IEnumerable$b1.Of(elementType);

  var result = null;
  if (JSIL.IsArray(enumerable))
    result = JSIL.MakeArrayEnumerator(enumerable, elementType);
  else if (enumerable.__IsArray__)
    result = JSIL.MakeArrayEnumerator(enumerable.Items, elementType);
  else if (typeof (enumerable) === "string")
    result = JSIL.MakeArrayEnumerator(enumerable, elementType);
  else if ((fallbackMethodInvoke !== true) && tIEnumerable$b1 && tIEnumerable$b1.$Is(enumerable))
    result = tIEnumerable$b1.GetEnumerator.Call(enumerable);
  else if ((fallbackMethodInvoke !== true) && tIEnumerable.$Is(enumerable))
    result = tIEnumerable.GetEnumerator.Call(enumerable);
  else if ((fallbackMethodInvoke !== true) && (typeof (enumerable.GetEnumerator) === "function"))
    // HACK: This is gross.
    result = enumerable.GetEnumerator();
  else
    JSIL.RuntimeError("Value is not enumerable");

  if (!result)
    JSIL.RuntimeError("Value's GetEnumerator method did not return an enumerable.");

  return result;
};
JSIL.ParseDataURL = function (dataUrl) {
  var colonIndex = dataUrl.indexOf(":");
  if ((colonIndex != 4) || (dataUrl.substr(0, 5) !== "data:"))
    JSIL.RuntimeError("Invalid Data URL header");

  var semicolonIndex = dataUrl.indexOf(";");
  var mimeType = dataUrl.substr(colonIndex + 1, semicolonIndex - colonIndex - 1);

  var commaIndex = dataUrl.indexOf(",");
  if (commaIndex <= semicolonIndex)
    JSIL.RuntimeError("Invalid Data URL header");

  var encodingType = dataUrl.substr(semicolonIndex + 1, commaIndex - semicolonIndex - 1);
  if (encodingType.toLowerCase() !== "base64")
    JSIL.RuntimeError("Invalid Data URL encoding type: " + encodingType);

  var base64 = dataUrl.substr(commaIndex + 1);
  var bytes = System.Convert.FromBase64String(base64);

  return [mimeType, bytes];
};
JSIL.MakeIConvertibleMethods = function ($) {
  var $T01 = function () {
    return ($T01 = JSIL.Memoize($jsilcore.System.Convert))();
  };

  var $TypeCode = function () {
    return ($TypeCode = JSIL.Memoize($jsilcore.System.Type.GetTypeCode($.Type)))();
  };

  var types = [
    $.Boolean, $.Char,
    $.SByte, $.Byte, $.Int16, $.UInt16, $.Int32, $.UInt32, $.Int64, $.UInt64,
    $.Single, $.Double,
    $jsilcore.TypeRef("System.Decimal"),
    $jsilcore.TypeRef("System.DateTime"),
    $.String
  ];

  var signatures = [];

  var createSignature = function (i) {
    return function () {
      return (signatures[i] = JSIL.Memoize(new JSIL.MethodSignature(types[i], [$.Type])))();
    }
  };

  var createConvertFunction = function (i, name) {
    return function (formatProvider) {
      return signatures[i]().CallStatic($T01(), "To" + name, null, this);
    }
  };

  for (var i = 0; i < types.length; i++) {
    signatures.push(createSignature(i));

    var typeRef = types[i];
    var typeName = typeRef.typeName.substr(typeRef.typeName.indexOf(".") + 1);

    if (typeRef !== $.String) {
      $.Method({ Static: false, Public: false, Virtual: true }, "System.IConvertible.To" + typeName, new JSIL.MethodSignature(typeRef, [$jsilcore.TypeRef("System.IFormatProvider")], []),
          createConvertFunction(i, typeName))
        .Overrides($jsilcore.TypeRef("System.IConvertible"), "To" + typeName);
    } else {
      $.Method({ Static: false, Public: true, Virtual: true }, "ToString", new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.IFormatProvider")], []),
          createConvertFunction(i, typeName));
    }
  }

  $.Method({ Static: false, Public: true, Virtual: true }, "GetTypeCode", new JSIL.MethodSignature($jsilcore.TypeRef("System.TypeCode"), [], []),
    function IConvertible_GetTypeCode() {
      return $TypeCode();
    });

  $.ImplementInterfaces($jsilcore.TypeRef("System.IConvertible"));
}


JSIL.MakeClass("System.Object", "JSIL.ArrayEnumerator", true, ["T"], function ($) {
  var T = new JSIL.GenericParameter("T", "JSIL.ArrayEnumerator");

  $.RawMethod(false, "__CopyMembers__",
    function ArrayEnumerator_CopyMembers(source, target) {
      target._array = source._array;
      target._length = source._length;
      target._index = source._index;
    }
  );

  $.Method({ Public: true, Static: false }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", ["!!0"]), $.Int32]),
    function (array, startPosition) {
      this._array = array;
      this._length = array.length;
      if (typeof (startPosition) !== "number")
        JSIL.RuntimeError("ArrayEnumerator ctor second argument must be number");

      this._index = startPosition;
    }
  );

  $.Method({ Public: true, Static: false }, "Reset",
    new JSIL.MethodSignature(null, []),
    function () {
      if (this._array === null)
        JSIL.RuntimeError("Enumerator is disposed or not initialized");

      this._index = -1;
    }
  );

  $.Method({ Public: true, Static: false }, "MoveNext",
    new JSIL.MethodSignature(System.Boolean, []),
    function () {
      return (++this._index < this._length);
    }
  );

  $.Method({ Public: true, Static: false }, "Dispose",
    new JSIL.MethodSignature(null, []),
    function () {
      this._array = null;
      this._index = 0;
      this._length = -1;
    }
  );

  $.Method({ Public: false, Static: false }, null,
    new JSIL.MethodSignature(System.Object, []),
    function () {
      return this._array[this._index];
    }
  )
    .Overrides("System.Collections.IEnumerator", "get_Current");

  $.Method({ Public: true, Static: false }, "get_Current",
    new JSIL.MethodSignature(T, []),
    function () {
      return this._array[this._index];
    }
  )
    .Overrides("System.Collections.Generic.IEnumerator`1", "get_Current");

  $.Property({ Public: true, Static: false, Virtual: true }, "Current");

  $.ImplementInterfaces(
    /* 0 */ System.IDisposable,
    /* 1 */ System.Collections.IEnumerator,
    /* 2 */ $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("T", "JSIL.ArrayEnumerator")])
  );
});

﻿$jsilcore.$MakeParseExternals = function ($, type, parse, tryParse) {
  $.Method({ Static: true, Public: true }, "Parse",
    (new JSIL.MethodSignature(type, [$.String], [])),
    parse
  );

  $.Method({ Static: true, Public: true }, "Parse",
    (new JSIL.MethodSignature(type, [$.String, $jsilcore.TypeRef("System.Globalization.NumberStyles")], [])),
    parse
  );

  $.Method({ Static: true, Public: true }, "Parse",
    (new JSIL.MethodSignature(type, [$.String, $jsilcore.TypeRef("System.IFormatProvider")], [])),
    function (input, formatProvider) {
      // TODO: Really use fromat provider
      return parse(input, null);
    }
  );

  $.Method({ Static: true, Public: true }, "TryParse",
    (new JSIL.MethodSignature($.Boolean, [$.String, $jsilcore.TypeRef("JSIL.Reference", [type])], [])),
    tryParse
  );
};

$jsilcore.$ParseBoolean = function (text) {
  if (arguments.length !== 1)
    throw new Error("NumberStyles not supported");

  var temp = new JSIL.BoxedVariable(null);
  if ($jsilcore.$TryParseBoolean(text, temp))
    return temp.get();

  throw new System.Exception("Invalid boolean");
};

$jsilcore.$TryParseBoolean = function (text, result) {
  if (text === null) {
      result.set(false);
      return true;
  }

  text = text.toLowerCase().trim();

  if (text === "true") {
    result.set(true);
    return true;
  } else if (text === "false") {
    result.set(false);
    return true;
  }

  return false;
};
JSIL.ImplementExternals(
  "System.Boolean", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return (value === false) || (value === true) || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $jsilcore.$MakeParseExternals($, $.Boolean, $jsilcore.$ParseBoolean, $jsilcore.$TryParseBoolean);
  }
);
JSIL.MakeNumericType(Boolean, "System.Boolean", true, null, JSIL.MakeIConvertibleMethods);
JSIL.ImplementExternals(
  "System.Char", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return ((typeof (value) === "string") && (value.length == 1)) || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $.Constant({ Public: true, Static: true }, "MaxValue", "\uffff");
    $.Constant({ Public: true, Static: true }, "MinValue", "\0");
  }
);
JSIL.MakeNumericType(String, "System.Char", true, null, JSIL.MakeIConvertibleMethods);
JSIL.ImplementExternals(
  "System.Byte", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return ((typeof (value) === "number") && (value >= 0) && (value <= 255))
        || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $.Constant({ Public: true, Static: true }, "MinValue", 0);
    $.Constant({ Public: true, Static: true }, "MaxValue", 255);
  }
);
JSIL.MakeNumericType(Number, "System.Byte", true, "Uint8Array", JSIL.MakeIConvertibleMethods);

JSIL.ImplementExternals(
  "System.SByte", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return ((typeof (value) === "number") && (value >= -128) && (value <= 127)) || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $.Constant({ Public: true, Static: true }, "MinValue", -128);
    $.Constant({ Public: true, Static: true }, "MaxValue", 127);
  }
);
JSIL.MakeNumericType(Number, "System.SByte", true, "Int8Array", JSIL.MakeIConvertibleMethods);

﻿
$jsilcore.$ParseInt = function (text, style) {
  var temp = new JSIL.BoxedVariable(null);
  if ($jsilcore.$TryParseInt(text, style, temp))
    return temp.get();

  throw new System.FormatException("Invalid integer");
};

$jsilcore.$TryParseInt = function (text, style, result) {
  if (arguments.length === 2) {
    result = style;
    style = 0;
  }

  var radix = 10;

  if (style & System.Globalization.NumberStyles.AllowHexSpecifier)
    radix = 16;

  var parsed;
  result.set(parsed = parseInt(text, radix));
  return !isNaN(parsed);
};
JSIL.ImplementExternals(
  "System.UInt16", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return ((typeof (value) === "number") && (value >= 0)) || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $jsilcore.$MakeParseExternals($, $.UInt16, $jsilcore.$ParseInt, $jsilcore.$TryParseInt);

    $.Constant({ Public: true, Static: true }, "MaxValue", 65535);
    $.Constant({ Public: true, Static: true }, "MinValue", 0);
  }
);
JSIL.MakeNumericType(Number, "System.UInt16", true, "Uint16Array", JSIL.MakeIConvertibleMethods);

﻿
JSIL.ImplementExternals(
  "System.Int16", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return (typeof (value) === "number") || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $jsilcore.$MakeParseExternals($, $.Int16, $jsilcore.$ParseInt, $jsilcore.$TryParseInt);

    $.Constant({ Public: true, Static: true }, "MaxValue", 32767);
    $.Constant({ Public: true, Static: true }, "MinValue", -32768);
  }
);
JSIL.MakeNumericType(Number, "System.Int16", true, "Int16Array", JSIL.MakeIConvertibleMethods);

﻿
JSIL.ImplementExternals(
  "System.UInt32", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return ((typeof (value) === "number") && (value >= 0)) || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $jsilcore.$MakeParseExternals($, $.UInt32, $jsilcore.$ParseInt, $jsilcore.$TryParseInt);

    $.Constant({ Public: true, Static: true }, "MaxValue", 4294967295);
    $.Constant({ Public: true, Static: true }, "MinValue", 0);
  }
);
JSIL.MakeNumericType(Number, "System.UInt32", true, "Uint32Array", JSIL.MakeIConvertibleMethods);

﻿
JSIL.ImplementExternals(
  "System.Int32", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return (typeof (value) === "number") || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $jsilcore.$MakeParseExternals($, $.Int32, $jsilcore.$ParseInt, $jsilcore.$TryParseInt);

    $.Constant({ Public: true, Static: true }, "MaxValue", 2147483647);
    $.Constant({ Public: true, Static: true }, "MinValue", -2147483648);
  }
);
JSIL.MakeNumericType(Number, "System.Int32", true, "Int32Array", JSIL.MakeIConvertibleMethods);

﻿
$jsilcore.$ParseFloat = function (text, style) {
  var temp = new JSIL.BoxedVariable(null);
  if ($jsilcore.$TryParseFloat(text, style, temp))
    return temp.get();

  throw new System.Exception("Invalid float");
};

$jsilcore.$TryParseFloat = function (text, style, result) {
  if (arguments.length === 2) {
    result = style;
    style = 0;
  }

  var parsed;
  result.set(parsed = parseFloat(text));

  if (isNaN(parsed)) {
    var lowered = text.toLowerCase();

    if (lowered === "nan") {
      result.set(Number.NaN);
      return true;
    } else if (lowered === "-infinity") {
      result.set(Number.NEGATIVE_INFINITY);
      return true;
    } else if ((lowered === "+infinity") || (lowered === "infinity")) {
      result.set(Number.POSITIVE_INFINITY);
      return true;
    } else {
      return false;
    }
  } else {
    return true;
  }
};
JSIL.ImplementExternals(
  "System.Single", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return (typeof (value) === "number") || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $jsilcore.$MakeParseExternals($, $.Single, $jsilcore.$ParseFloat, $jsilcore.$TryParseFloat);

    $.Constant({ Public: true, Static: true }, "MinValue", -3.4028234663852886E+38);
    $.Constant({ Public: true, Static: true }, "Epsilon", 1.4012984643248171E-45);
    $.Constant({ Public: true, Static: true }, "MaxValue", 3.4028234663852886E+38);
    $.Constant({ Public: true, Static: true }, "PositiveInfinity", Infinity);
    $.Constant({ Public: true, Static: true }, "NegativeInfinity", -Infinity);
    $.Constant({ Public: true, Static: true }, "NaN", NaN);
  }
);
JSIL.MakeNumericType(Number, "System.Single", false, "Float32Array", JSIL.MakeIConvertibleMethods);

﻿
JSIL.ImplementExternals(
  "System.Double", function ($) {
    $.RawMethod(true, "CheckType", function (value) {
      return (typeof (value) === "number") || JSIL.Box.IsBoxedOfType(value, $.Type);
    });

    $jsilcore.$MakeParseExternals($, $.Double, $jsilcore.$ParseFloat, $jsilcore.$TryParseFloat);

    $.Constant({ Public: true, Static: true }, "MinValue", -1.7976931348623157E+308);
    $.Constant({ Public: true, Static: true }, "MaxValue", 1.7976931348623157E+308);
    $.Constant({ Public: true, Static: true }, "Epsilon", 4.94065645841247E-324);
    $.Constant({ Public: true, Static: true }, "NegativeInfinity", -Infinity);
    $.Constant({ Public: true, Static: true }, "PositiveInfinity", Infinity);
    $.Constant({ Public: true, Static: true }, "NaN", NaN);
  }
);
JSIL.MakeNumericType(Number, "System.Double", false, "Float64Array", JSIL.MakeIConvertibleMethods);

﻿
JSIL.ImplementExternals("System.Array", function ($) {
  var copyImpl = function (sourceArray, sourceIndex, destinationArray, destinationIndex, length) {
    if (length < 0)
      throw new System.ArgumentException("length");
    if (sourceIndex < 0)
      throw new System.ArgumentException("sourceIndex");
    if (destinationIndex < 0)
      throw new System.ArgumentException("destinationIndex");

    var maxLength = Math.min(
      (sourceArray.length - sourceIndex) | 0,
      (destinationArray.length - destinationIndex) | 0
    );
    if (length > maxLength)
      throw new System.ArgumentException("length");

    length = length | 0;

    if (
      (sourceArray === destinationArray) &&
      (destinationIndex === sourceIndex)
    )
      return;

    if (
      (sourceArray === destinationArray) &&
      (destinationIndex < (sourceIndex + length)) &&
      (destinationIndex > sourceIndex)
    ) {
      for (var i = length - 1; i >= 0; i = (i - 1) | 0) {
        destinationArray[i + destinationIndex] = sourceArray[i + sourceIndex];
      }
    } else {
      for (var i = 0; i < length; i = (i + 1) | 0) {
        destinationArray[i + destinationIndex] = sourceArray[i + sourceIndex];
      }
    }
  };

  var sortImpl = function (array, index, length, comparison) {
    if (length < 2) {
      return;
    }

    if ((index !== 0) || (length !== array.length)) {
      var sortedArrayPart = Array.prototype.slice.call(array, index, index + length).sort(comparison);
      for (var i = 0; i < length; i++)
        array[i + index] = sortedArrayPart[i];
    } else {
      Array.prototype.sort.call(array, comparison);
    }
  };

  var reverseImpl = function (array, index, length) {
    if (length < 2) {
      return;
    }

    if ((index !== 0) || (length !== array.length)) {
      var reversedArrayPart = Array.prototype.slice.call(array, index, index + length).reverse();
      for (var i = 0; i < length; i++)
        array[i + index] = reversedArrayPart[i];
    } else {
      Array.prototype.reverse.call(array);
    }
  };

  $.Method({ Static: true, Public: true }, "Copy",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array"), $jsilcore.TypeRef("System.Array"),
        $.Int32
    ], []),
    function Copy(sourceArray, destinationArray, length) {
      copyImpl(sourceArray, 0, destinationArray, 0, length);
    }
  );

  $.Method({ Static: true, Public: true }, "Copy",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array"), $.Int32,
        $jsilcore.TypeRef("System.Array"), $.Int32,
        $.Int32
    ], []),
    function Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length) {
      copyImpl(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
    }
  );

  $.Method({ Static: true, Public: true }, "Sort",
    JSIL.MethodSignature.Action($jsilcore.TypeRef("System.Array")),
    function Sort(array) {
      sortImpl(array, 0, array.length, JSIL.CompareValues);
    }
  );

  $.Method({ Static: true, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array"), $.Int32,
        $.Int32
    ]),
    function Sort(array, index, length) {
      sortImpl(array, index, length, JSIL.CompareValues);
    }
  )

  $.Method({ Static: true, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array"), $jsilcore.TypeRef("System.Collections.IComparer")]),
    function Sort(array, comparer) {
      sortImpl(array, 0, array.length, JSIL.$WrapIComparer(null, comparer));
    }
  )

  $.Method({ Static: true, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array"), $.Int32,
        $.Int32, $jsilcore.TypeRef("System.Collections.IComparer")
    ]),
    function Sort(array, index, length, comparer) {
      sortImpl(array, index, length, JSIL.$WrapIComparer(null, comparer));
    }
  )

  $.Method({ Static: true, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array", ["!!0"]), $.Int32,
        $.Int32
    ], ["T"]),
    function Sort$b1(T, array, index, length) {
      sortImpl(array, index, length, JSIL.CompareValues);
    }
  )

  $.Method({ Static: true, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", ["!!0"]), $jsilcore.TypeRef("System.Collections.Generic.IComparer`1", ["!!0"])], ["T"]),
    function Sort$b1(T, array, comparer) {
      sortImpl(array, 0, array.length, JSIL.$WrapIComparer(T, comparer));
    }
  )

  $.Method({ Static: true, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [
        $jsilcore.TypeRef("System.Array", ["!!0"]), $.Int32,
        $.Int32, $jsilcore.TypeRef("System.Collections.Generic.IComparer`1", ["!!0"])
    ], ["T"]),
    function Sort$b1(T, array, index, length, comparer) {
      sortImpl(array, index, length, JSIL.$WrapIComparer(T, comparer));
    }
  );

  $.Method({ Static: true, Public: true }, "Sort",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", ["!!0"]), $jsilcore.TypeRef("System.Comparison`1", ["!!0"])], ["T"]),
    function Sort$b1(T, array, comparison) {
      sortImpl(array, 0, array.length, comparison);
    }
  );

  $.Method({ Static: true, Public: true }, "Reverse",
    new JSIL.MethodSignature(null, [
      $jsilcore.TypeRef("System.Array")
    ]),
    function Reverse(array) {
      reverseImpl(array, 0, array.length);
    }
  );

  $.Method({ Static: true, Public: true }, "Reverse",
    new JSIL.MethodSignature(null, [
      $jsilcore.TypeRef("System.Array"), $.Int32, $.Int32
    ]),
    function Reverse(array, index, length) {
      reverseImpl(array, index, length);
    }
  );

  $.Method({ Static: true, Public: true }, "Empty",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", ["!!0"]), [], ["T"]),
    function Empty(T) {
      return $jsilcore.System.Array_EmptyArray$b1.Of(T).Value;
    }
  );
});


JSIL.MakeStaticClass("System.Array+EmptyArray`1", false, ["T"], function($) {
  $.Field({ Static: true, Public: true, ReadOnly: true }, "Value", $jsilcore.TypeRef("System.Array", [$.GenericParameter("T")]));

  $.Method({ Static: true, Public: false }, ".cctor",
    JSIL.MethodSignature.Void,
    function EmptyArray$b1__cctor() {
      this.Value = JSIL.Array.New(this.T, 0);
    }
  );
});


$jsilcore.$GetInvocationList = function (delegate) {
  if (delegate === null) {
    return [];
  } else if (typeof (delegate.__delegates__) !== "undefined") {
    return delegate.__delegates__;
  } else if (typeof (delegate) === "function") {
    return [delegate];
  } else {
    return null;
  }
};
$jsilcore.$CompareSinglecastDelegate = function (lhs, rhs) {
  if (lhs.__object__ !== rhs.__object__)
    return false;

  if (lhs.get_Method() !== rhs.get_Method())
    return false;

  return true;
};
$jsilcore.$CompareMulticastDelegate = function (lhs, rhs) {
  var lhsInvocationList = $jsilcore.$GetInvocationList(lhs);
  var rhsInvocationList = $jsilcore.$GetInvocationList(rhs);

  if (lhsInvocationList.length !== rhsInvocationList.length)
    return false;

  for (var i = 0, l = lhsInvocationList.length; i < l; i++) {
    if (!$jsilcore.$AreDelegatesEqual(lhsInvocationList[i], rhsInvocationList[i]))
      return false;
  }

  return true;
};
$jsilcore.$AreDelegatesEqual = function (lhs, rhs) {
  if (lhs === rhs)
    return true;

  var singleMethod, otherMethod;
  if (!lhs.__isMulticast__)
    return $jsilcore.$CompareSinglecastDelegate(lhs, rhs);
  else if (!rhs.__isMulticast__)
    return $jsilcore.$CompareSinglecastDelegate(rhs, lhs);
  else
    return $jsilcore.$CompareMulticastDelegate(lhs, rhs);
};
$jsilcore.$CombineDelegates = function (lhs, rhs) {
  if (rhs === null) {
    return lhs;
  } else if (lhs === null) {
    return rhs;
  }

  var newList = Array.prototype.slice.call($jsilcore.$GetInvocationList(lhs));
  newList.push.apply(newList, $jsilcore.$GetInvocationList(rhs));
  var result = JSIL.MulticastDelegate.New(newList);
  return result;
};
$jsilcore.$RemoveDelegate = function (lhs, rhs) {
  if (rhs === null)
    return lhs;
  if (lhs === null)
    return null;

  var newList = Array.prototype.slice.call($jsilcore.$GetInvocationList(lhs));
  var rightList = $jsilcore.$GetInvocationList(rhs);

  if (newList.length >= rightList.length) {
    for (var i = newList.length - rightList.length; i >= 0; i--) {
      var equal = true;
      for (var j = 0; j < rightList.length; j++) {
        if (!$jsilcore.$AreDelegatesEqual(newList[i + j], rightList[j])) {
          equal = false;
          break;
        }
      }
      if (equal) {
        newList.splice(i, rightList.length);
        break;
      }
    }
  }

  if (newList.length == 0)
    return null;
  else if (newList.length == 1)
    return newList[0];
  else
    return JSIL.MulticastDelegate.New(newList);
};

JSIL.ImplementExternals("System.Delegate", function ($) {
  var tDelegate = $jsilcore.TypeRef("System.Delegate");

  $.Method({ Static: false, Public: true }, "GetInvocationList",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [tDelegate]), [], [])),
    function GetInvocationList() {
      return [this];
    }
  );

  $.Method({ Static: true, Public: true }, "CreateDelegate",
    (new JSIL.MethodSignature(tDelegate, [
          $jsilcore.TypeRef("System.Type"), $.Object,
          $jsilcore.TypeRef("System.Reflection.MethodInfo")
    ], [])),
    function CreateDelegate(delegateType, firstArgument, method) {
      var delegatePublicInterface = delegateType.__PublicInterface__;
      if (typeof (delegatePublicInterface.New) !== "function")
        JSIL.Host.abort(new Error("Invalid delegate type"));

      return delegatePublicInterface.New(firstArgument, null, JSIL.MethodPointerInfo.FromMethodInfo(method));
    }
  );

  $.Method({ Static: true, Public: true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [tDelegate, tDelegate], [])),
    $jsilcore.$AreDelegatesEqual
  );

  $.Method({ Static: true, Public: true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [tDelegate, tDelegate], [])),
    function op_Inequality(d1, d2) {
      return !$jsilcore.$AreDelegatesEqual(d1, d2);
    }
  );

  $.Method({ Static: true, Public: true }, "Combine",
    (new JSIL.MethodSignature(tDelegate, [tDelegate, tDelegate], [])),
    $jsilcore.$CombineDelegates
  );

  $.Method({ Static: true, Public: true }, "Remove",
    (new JSIL.MethodSignature(tDelegate, [tDelegate, tDelegate], [])),
    $jsilcore.$RemoveDelegate
  );

  $.Method({ Static: false, Public: true }, "get_Method",
    (new JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Reflection.MethodInfo"))),
    function get_Method() {
      if (this.__isMulticast__) {
        return this.get_Method();
      }
      if (this.__methodPointerInfo__) {
        // TODO: find better solution for resolving MethodInfo in class by MethodInfo in base class.
        // Currently it will not find proper derived implementation MethodInfo for virtual method and interface methods.
        return  this.__methodPointerInfo__.resolveMethodInfo();
      }
      return null;
    }
  );
});

JSIL.MakeClass("System.Object", "System.Delegate", true, [], function ($) {
  $.Property({ Public: true, Static: false }, "Method");
});
JSIL.ImplementExternals("System.MulticastDelegate", function ($) {
  $.Method({ Static: false, Public: true }, "GetInvocationList",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Delegate")]), [], [])),
    function GetInvocationList() {
      return this.__delegates__;
    }
  );
});

JSIL.MulticastDelegate.New = function (delegates) {
  var delegatesCopy = Array.prototype.slice.call(delegates);
  var delegateCount = delegates.length;

  var resultDelegate = function MulticastDelegate_Invoke() {
    var result;

    for (var i = 0; i < delegateCount; i++) {
      var d = delegatesCopy[i];
      // FIXME: bind, call and apply suck
      result = d.apply(d.__object__ || null, arguments);
    }

    return result;
  };

  JSIL.SetValueProperty(resultDelegate, "__delegates__", delegatesCopy);
  JSIL.SetValueProperty(resultDelegate, "__isMulticast__", true);
  JSIL.SetValueProperty(resultDelegate, "__ThisType__", delegatesCopy[0].__ThisType__);
  JSIL.SetValueProperty(resultDelegate, "toString", delegatesCopy[0].toString);
  JSIL.SetValueProperty(resultDelegate, "__method__", resultDelegate);
  JSIL.SetValueProperty(resultDelegate, "Invoke", resultDelegate);
  JSIL.SetValueProperty(resultDelegate, "get_Method", function () { return delegatesCopy[delegateCount - 1].get_Method(); });

  return resultDelegate;
};

JSIL.MakeClass("System.Delegate", "System.MulticastDelegate", true, []);

JSIL.MakeStruct("System.ValueType", "System.Decimal", true, [], function ($) {
  var mscorlib = JSIL.GetCorlib();

  var ctorImpl = function (value) {
    this.value = value.valueOf();
  };

  var decimalToNumber = function (decimal) {
    return decimal.valueOf();
  };

  var numberToDecimal = function (value) {
    var result = JSIL.CreateInstanceOfType($.Type, null);
    result.value = value.valueOf();
    return result;
  };

  $.RawMethod(false, "valueOf", function () {
    return this.value;
  });

  $.Method({ Static: false, Public: true }, "toString",
    new JSIL.MethodSignature("System.String", []),
    function (format) {
      return this.value.toString();
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Int32")], [])),
    ctorImpl
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.UInt32")], [])),
    ctorImpl
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Int64")], [])),
    ctorImpl
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.UInt64")], [])),
    ctorImpl
  );

  $.Method({ Static: true, Public: true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
    function (lhs, rhs) {
      return decimalToNumber(lhs) === decimalToNumber(rhs);
    }
  );

  $.Method({ Static: true, Public: true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
    function (lhs, rhs) {
      return decimalToNumber(lhs) !== decimalToNumber(rhs);
    }
  );

  $.Method({ Static: true, Public: true }, "op_Addition",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function (lhs, rhs) {
      return numberToDecimal(decimalToNumber(lhs) + decimalToNumber(rhs));
    }
  );

  $.Method({ Static: true, Public: true }, "op_Division",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function (lhs, rhs) {
      return numberToDecimal(decimalToNumber(lhs) / decimalToNumber(rhs));
    }
  );

  $.Method({ Static: true, Public: true }, "op_Multiply",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function (lhs, rhs) {
      return numberToDecimal(decimalToNumber(lhs) * decimalToNumber(rhs));
    }
  );

  $.Method({ Static: true, Public: true }, "op_Subtraction",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function (lhs, rhs) {
      return numberToDecimal(decimalToNumber(lhs) - decimalToNumber(rhs));
    }
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature($.Type, [mscorlib.TypeRef("System.Single")], [])),
    numberToDecimal
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature($.Type, [mscorlib.TypeRef("System.Double")], [])),
    numberToDecimal
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Byte"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.SByte"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Int16"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.UInt16"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Int32"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.UInt32"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Int64"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.UInt64"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Single"), [$.Type], [])),
    decimalToNumber
  );

  $.Method({ Static: true, Public: true }, "op_Explicit",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Double"), [$.Type], [])),
    decimalToNumber
  );

  $.Field({ Static: false, Public: false }, "value", mscorlib.TypeRef("System.Double"), function () {
    return 0;
  });

  var $formatSignature = function () {
    return ($formatSignature = JSIL.Memoize(new JSIL.MethodSignature($jsilcore.TypeRef("System.String"), [
        $jsilcore.TypeRef("System.String"), $jsilcore.TypeRef("System.Double"),
        $jsilcore.TypeRef("System.IFormatProvider")
    ])))();
  };

  $.RawMethod(
    true, "$ToString",
    function $ToString(self, format, formatProvider) {
      return $formatSignature().CallStatic($jsilcore.JSIL.System.NumberFormatter, "NumberToString", null, format, decimalToNumber(self), formatProvider).toString();
    }
  );

  JSIL.MakeIConvertibleMethods($);
});

// HACK: Unfortunately necessary :-(
String.prototype.Object_Equals = function (rhs) {
  return this === rhs;
};

String.prototype.GetHashCode = function () {
  var h = 0;

  for (var i = 0; i < this.length; i++) {
    h = ((h << 5) - h + this.charCodeAt(i)) & ~0;
  }

  return h;
};

JSIL.ImplementExternals(
  "System.Enum", function ($) {
    $.RawMethod(true, "CheckType",
      function (value) {
        if (typeof (value) === "object") {
          if ((value !== null) && (typeof (value.GetType) === "function"))
            return value.GetType().IsEnum;
        }

        return false;
      }
    );

    var internalTryParse;

    var internalTryParseFlags = function (TEnum, text, ignoreCase, result) {
      var items = text.split(",");

      var resultValue = 0;
      var temp = new JSIL.BoxedVariable();

      var publicInterface = TEnum.__PublicInterface__;

      for (var i = 0, l = items.length; i < l; i++) {
        var item = items[i].trim();
        if (item.length === 0)
          continue;

        if (internalTryParse(TEnum, item, ignoreCase, temp)) {
          resultValue = resultValue | temp.get();
        } else {
          return false;
        }
      }

      var name = TEnum.__ValueToName__[resultValue];

      if (typeof (name) === "undefined") {
        result.set(publicInterface.$MakeValue(resultValue, null));
        return true;
      } else {
        result.set(publicInterface[name]);
        return true;
      }
    };

    internalTryParse = function (TEnum, text, ignoreCase, result) {
      // Detect and handle flags enums
      var commaPos = text.indexOf(",");
      if (commaPos >= 0)
        return internalTryParseFlags(TEnum, text, ignoreCase, result);

      var num = parseInt(text, 10);

      var publicInterface = TEnum.__PublicInterface__;

      if (isNaN(num)) {
        if (ignoreCase) {
          var names = TEnum.__Names__;
          for (var i = 0; i < names.length; i++) {
            var isMatch = (names[i].toLowerCase() == text.toLowerCase());

            if (isMatch) {
              result.set(publicInterface[names[i]]);
              break;
            }
          }
        } else {
          result.set(publicInterface[text]);
        }

        return (typeof (result.get()) !== "undefined");
      } else {
        var name = TEnum.__ValueToName__[num];

        if (typeof (name) === "undefined") {
          result.set(publicInterface.$MakeValue(num, null));
          return true;
        } else {
          result.set(publicInterface[name]);
          return true;
        }
      }
    };

    var internalParse = function (enm, text, ignoreCase) {
      var result = new JSIL.BoxedVariable();
      if (internalTryParse(enm, text, ignoreCase, result))
        return result.get();

      throw new System.Exception("Failed to parse enum");
    };

    $.Method({ Static: true, Public: true }, "Parse",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Object"), [$jsilcore.TypeRef("System.Type"), $jsilcore.TypeRef("System.String")], []),
      function (enm, text) {
        return internalParse(enm, text, false);
      }
    );

    $.Method({ Static: true, Public: true }, "Parse",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Object"), [
          $jsilcore.TypeRef("System.Type"), $jsilcore.TypeRef("System.String"),
          $jsilcore.TypeRef("System.Boolean")
      ], []),
      internalParse
    );

    $.Method({ Static: true, Public: true }, "TryParse",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.String"), "JSIL.Reference" /* !!0& */], ["TEnum"]),
      function (TEnum, text, result) {
        return internalTryParse(TEnum, text, result);
      }
    );

    $.Method({ Static: true, Public: true }, "TryParse",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [
          $jsilcore.TypeRef("System.String"), $jsilcore.TypeRef("System.Boolean"),
          "JSIL.Reference" /* !!0& */
      ], ["TEnum"]),
      internalTryParse
    );

    $.Method({ Static: true, Public: true }, "GetNames",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.String]), [$jsilcore.TypeRef("System.Type")], []),
      function (enm) {
        return enm.__Names__;
      }
    );

    $.Method({ Static: true, Public: true }, "GetValues",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Array"), [$jsilcore.TypeRef("System.Type")], []),
      function (enm) {
        var names = enm.__Names__;
        var publicInterface = enm.__PublicInterface__;
        var result = new Array(names.length);

        for (var i = 0; i < result.length; i++)
          result[i] = publicInterface[names[i]];

        return result;
      }
    );

    $.Method({ Static: true, Public: true }, "GetUnderlyingType",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [$jsilcore.TypeRef("System.Type")], []),
      function (enm) {
        return enm.__StorageType__;
      }
    );
  }
);
JSIL.ImplementExternals("System.Activator", function ($) {
  var mscorlib = JSIL.GetCorlib();

  $.Method({ Static: true, Public: true }, "CreateInstance",
    (new JSIL.MethodSignature($.Object, [mscorlib.TypeRef("System.Type")], [])),
    function CreateInstance(type) {
      return JSIL.CreateInstanceOfType(type, "_ctor", []);
    }
  );

  $.Method({ Static: true, Public: true }, "CreateInstance",
    (new JSIL.MethodSignature($.Object, [mscorlib.TypeRef("System.Type"), mscorlib.TypeRef("System.Array", [$.Object])], [])),
    function CreateInstance(type, args) {
      if (!args)
        args = [];

      return JSIL.CreateInstanceOfType(type, "_ctor", args);
    }
  );

  $.Method({ Static: true, Public: true }, "CreateInstance",
    (new JSIL.MethodSignature("!!0", [], ["T"])),
    function CreateInstance(T) {
      return JSIL.CreateInstanceOfType(T, "_ctor", []);
    }
  );

  $.Method({ Static: true, Public: true }, "CreateInstance",
    (new JSIL.MethodSignature("!!0", [mscorlib.TypeRef("System.Array", [$.Object])], ["T"])),
    function CreateInstance(T, args) {
      if (!args)
        args = [];

      return JSIL.CreateInstanceOfType(T, "_ctor", args);
    }
  );

  $.Method({ Static: true, Public: true }, "CreateInstance",
    (new JSIL.MethodSignature($.Object, [
          $jsilcore.TypeRef("System.Type"), $jsilcore.TypeRef("System.Reflection.BindingFlags"),
          $jsilcore.TypeRef("System.Reflection.Binder"), $jsilcore.TypeRef("System.Array", [$.Object]),
          $jsilcore.TypeRef("System.Globalization.CultureInfo")
    ], [])),
    function CreateInstance(type, bindingAttr, binder, args, culture) {
      // FIXME
      if (!args)
        args = [];

      return JSIL.CreateInstanceOfType(type, "_ctor", args);
    }
  );

});


JSIL.ImplementExternals("System.Nullable", function ($) {
  var mscorlib = JSIL.GetCorlib();

  $.Method({ Static: true, Public: true }, "GetUnderlyingType",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Type"), [mscorlib.TypeRef("System.Type")], [])),
    function GetUnderlyingType(nullableType) {
      if (nullableType.__FullName__.indexOf("System.Nullable`1") !== 0) {
        return null;
      } else {
        return nullableType.__PublicInterface__.T;
      }
    }
  );
});

JSIL.ImplementExternals("System.Nullable`1", function ($) {
  $.RawMethod(true, "CheckType", function (value) {
    if (this.T.$Is(value))
      return true;

    return false;
  });
});


JSIL.ImplementExternals("System.WeakReference", function ($) {
  var warnedAboutWeakReferences = false;

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Object], [])),
    function _ctor(target) {
      // FIXME
      if (!warnedAboutWeakReferences) {
        warnedAboutWeakReferences = true;
        JSIL.Host.warning("Weak references are not supported by JavaScript");
      }
    }
  );
});

// HACK: Nasty compatibility shim for JS Error <-> C# Exception
Error.prototype.get_Message = function () {
  return String(this);
};

Error.prototype.get_StackTrace = function () {
  return this.stack || "";
};

JSIL.ImplementExternals(
  "System.Exception", function ($) {
    var mscorlib = JSIL.GetCorlib();

    function captureStackTrace() {
      var e = new Error();
      var stackText = e.stack || "";
      return stackText;
    };

    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
        this._message = null;
        this._stackTrace = captureStackTrace();
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.String], [])),
      function _ctor(message) {
        this._message = message;
        this._stackTrace = captureStackTrace();
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.String, mscorlib.TypeRef("System.Exception")], [])),
      function _ctor(message, innerException) {
        this._message = message;
        this._innerException = innerException;
        this._stackTrace = captureStackTrace();
      }
    );

    $.Method({ Static: false, Public: true }, "get_InnerException",
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Exception"), [], [])),
      function get_InnerException() {
        return this._innerException;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Message",
      new JSIL.MethodSignature($.String, []),
      function () {
        if ((typeof (this._message) === "undefined") || (this._message === null))
          return System.String.Format("Exception of type '{0}' was thrown.", JSIL.GetTypeName(this));
        else
          return this._message;
      }
    );

    $.Method({ Static: false, Public: true }, "get_StackTrace",
      new JSIL.MethodSignature($.String, []),
      function () {
        return this._stackTrace || "";
      }
    );

    $.Method({ Static: false, Public: true }, "toString",
      new JSIL.MethodSignature($.String, []),
      function () {
        var message = this.Message;
        var result = System.String.Format("{0}: {1}", JSIL.GetTypeName(this), message);

        if (this._innerException) {
          result += "\n-- Inner exception follows --\n";
          result += this._innerException.toString();
        }

        return result;
      }
    );
  }
);



  ﻿JSIL.ImplementExternals(
    "System.SystemException", function ($) {
      $.Method({ Static: false, Public: true }, ".ctor",
        JSIL.MethodSignature.Void,
        function () {
          System.Exception.prototype._ctor.call(this);
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
        new JSIL.MethodSignature(null, [$.String], []),
        function (message) {
          System.Exception.prototype._ctor.call(this, message);
        }
      );
    }
  );


  ﻿JSIL.ImplementExternals(
    "System.InvalidCastException", function ($) {
      $.Method({ Static: false, Public: true }, ".ctor",
        new JSIL.MethodSignature(null, [$.String], []),
        function (message) {
          System.Exception.prototype._ctor.call(this, message);
        }
      );
    }
  );


  ﻿JSIL.ImplementExternals(
    "System.InvalidOperationException", function ($) {
      $.Method({ Static: false, Public: true }, ".ctor",
        new JSIL.MethodSignature(null, [$.String], []),
        function (message) {
          System.Exception.prototype._ctor.call(this, message);
        }
      );
    }
  );


  ﻿JSIL.ImplementExternals(
    "System.IO.FileNotFoundException", function ($) {
      $.Method({ Static: false, Public: true }, ".ctor",
        new JSIL.MethodSignature(null, [$.String], []),
        function (message) {
          System.Exception.prototype._ctor.call(this, message);
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
        (new JSIL.MethodSignature(null, [$.String, $.String], [])),
        function _ctor(message, fileName) {
          System.Exception.prototype._ctor.call(this, message);
          this._fileName = fileName;
        }
      );
    }
  );


  ﻿JSIL.ImplementExternals(
    "System.FormatException", function ($) {
      $.Method({ Static: false, Public: true }, ".ctor",
        new JSIL.MethodSignature(null, [$.String], []),
        function (message) {
          System.Exception.prototype._ctor.call(this, message);
        }
      );
    }
  );



JSIL.ImplementExternals("System.Environment", function ($) {
  $.Method({ Static: true, Public: true }, "GetFolderPath",
    (new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.Environment+SpecialFolder")], [])),
    function GetFolderPath(folder) {
      // FIXME
      return folder.name;
    }
  );

  $.Method({ Static: true, Public: true }, "get_NewLine",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_NewLine() {
      // FIXME: Maybe this should just be \n?
      return "\r\n";
    }
  );

  $.Method({ Static: true, Public: true }, "get_TickCount",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_TickCount() {
      return JSIL.Host.getTickCount() | 0;
    }
  );

});


JSIL.ImplementExternals("System.Console", function ($) {
  $.RawMethod(true, "WriteLine", function () {
    var text = "";
    if ((arguments.length > 0) && (arguments[0] !== null)) {
      text = System.String.Format.apply(System.String, arguments);
    }

    JSIL.Host.logWriteLine(text);
  });

  $.RawMethod(true, "Write", function () {
    var text = "";
    if ((arguments.length > 0) && (arguments[0] !== null)) {
      text = System.String.Format.apply(System.String, arguments);
    }

    JSIL.Host.logWrite(text);
  });
});


JSIL.$MathSign = function (value) {
  if (value > 0)
    return 1;
  else if (value < 0)
    return -1;
  else
    return 0;
};

JSIL.ImplementExternals("System.Math", function ($) {
  $.RawMethod(true, "Max", Math.max);
  $.RawMethod(true, "Min", Math.min);
  $.RawMethod(true, "Exp", Math.exp);

  $.Method({ Static: true, Public: true }, "Round",
    (new JSIL.MethodSignature($.Double, [$.Double, $.Int32], [])),
    function Round(value, digits) {
      var multiplier = Math.pow(10, digits);
      var result = Math.round(value * multiplier) / multiplier;
      return result;
    }
  );

  $.Method({ Static: true, Public: true }, "Atan2",
    (new JSIL.MethodSignature($.Double, [$.Double, $.Double], [])),
    Math.atan2
  );

  $.Method({ Static: true, Public: true }, "Sign",
    (new JSIL.MethodSignature($.Int32, [$.SByte], [])),
    JSIL.$MathSign
  );

  $.Method({ Static: true, Public: true }, "Sign",
    (new JSIL.MethodSignature($.Int32, [$.Int16], [])),
    JSIL.$MathSign
  );

  $.Method({ Static: true, Public: true }, "Sign",
    (new JSIL.MethodSignature($.Int32, [$.Int32], [])),
    JSIL.$MathSign
  );

  $.Method({ Static: true, Public: true }, "Sign",
    (new JSIL.MethodSignature($.Int32, [$.Single], [])),
    JSIL.$MathSign
  );

  $.Method({ Static: true, Public: true }, "Sign",
    (new JSIL.MethodSignature($.Int32, [$.Double], [])),
    JSIL.$MathSign
  );

  $.Method({ Static: true, Public: true }, "IEEERemainder",
    (new JSIL.MethodSignature($.Double, [$.Double, $.Double], [])),
    function IEEERemainder(x, y) {
      if (y === 0.0)
        return NaN;

      var result = x - y * Math.round(x / y);
      if (result !== 0.0)
        return result;

      if (x <= 0.0)
        // FIXME: -0?
        return 0;
      else
        return 0;
    }
  );
});


JSIL.MakeInterface(
  "System.IConvertible", true, [], function ($) {
      $.Method({}, "GetTypeCode", new JSIL.MethodSignature($jsilcore.TypeRef("System.TypeCode"), [], []));
      $.Method({}, "ToBoolean", new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToChar", new JSIL.MethodSignature($.Char, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToSByte", new JSIL.MethodSignature($.SByte, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToByte", new JSIL.MethodSignature($.Byte, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToInt16", new JSIL.MethodSignature($.Int16, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToUInt16", new JSIL.MethodSignature($.UInt16, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToInt32", new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToUInt32", new JSIL.MethodSignature($.UInt32, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToInt64", new JSIL.MethodSignature($.Int64, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToUInt64", new JSIL.MethodSignature($.UInt64, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToSingle", new JSIL.MethodSignature($.Single, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToDouble", new JSIL.MethodSignature($.Double, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToDecimal", new JSIL.MethodSignature($jsilcore.TypeRef("System.Decimal"), [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToDateTime", new JSIL.MethodSignature($jsilcore.TypeRef("System.DateTime"), [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToString", new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.IFormatProvider")], []));
      $.Method({}, "ToType", new JSIL.MethodSignature($.Object, [$jsilcore.TypeRef("System.Type"), $jsilcore.TypeRef("System.IFormatProvider")], []));
  }, [],
  function (input) {
    return typeof(input) === "string";
  },
  function (interfaceTypeObject, signature, thisReference) {
    return JSIL.GetType(thisReference).__PublicInterface__.prototype[signature.methodKey];
  });
﻿
﻿JSIL.ImplementExternals("System.Convert", function ($) {
  var base64IgnoredCodepoints = [
    9, 10, 13, 32
  ];

  var base64Table = [
    'A', 'B', 'C', 'D',
    'E', 'F', 'G', 'H',
    'I', 'J', 'K', 'L',
    'M', 'N', 'O', 'P',
    'Q', 'R', 'S', 'T',
    'U', 'V', 'W', 'X',
    'Y', 'Z',
    'a', 'b', 'c', 'd',
    'e', 'f', 'g', 'h',
    'i', 'j', 'k', 'l',
    'm', 'n', 'o', 'p',
    'q', 'r', 's', 't',
    'u', 'v', 'w', 'x',
    'y', 'z',
    '0', '1', '2', '3',
    '4', '5', '6', '7',
    '8', '9',
    '+', '/'
  ];

  var base64CodeTable = new Array(base64Table.length);
  for (var i = 0; i < base64Table.length; i++)
    base64CodeTable[i] = base64Table[i].charCodeAt(0);

  var toBase64StringImpl = function ToBase64String(inArray, offset, length, options) {
    if (options)
      JSIL.RuntimeError("Base64FormattingOptions not implemented");

    var reader = $jsilcore.makeByteReader(inArray, offset, length);
    var result = "";
    var ch1 = 0, ch2 = 0, ch3 = 0, bits = 0, equalsCount = 0, sum = 0;
    var mask1 = (1 << 24) - 1, mask2 = (1 << 18) - 1, mask3 = (1 << 12) - 1, mask4 = (1 << 6) - 1;
    var shift1 = 18, shift2 = 12, shift3 = 6, shift4 = 0;

    while (true) {
      ch1 = reader.read();
      ch2 = reader.read();
      ch3 = reader.read();

      if (ch1 === false)
        break;
      if (ch2 === false) {
        ch2 = 0;
        equalsCount += 1;
      }
      if (ch3 === false) {
        ch3 = 0;
        equalsCount += 1;
      }

      // Seems backwards, but is right!
      sum = (ch1 << 16) | (ch2 << 8) | (ch3 << 0);

      bits = (sum & mask1) >> shift1;
      result += base64Table[bits];
      bits = (sum & mask2) >> shift2;
      result += base64Table[bits];

      if (equalsCount < 2) {
        bits = (sum & mask3) >> shift3;
        result += base64Table[bits];
      }

      if (equalsCount === 2) {
        result += "==";
      } else if (equalsCount === 1) {
        result += "=";
      } else {
        bits = (sum & mask4) >> shift4;
        result += base64Table[bits];
      }
    }

    return result;
  };

  function toBase64CharArray(inArray, offsetIn, length, outArray, offsetOut, options) {
      var tempArray = JSIL.StringToCharArray(toBase64StringImpl(inArray, offsetIn, length, options));
      for (var i = 0, len = tempArray.length; i < len; ++i) {
          outArray[i] = tempArray[i];
      }
      return tempArray.length;
    }


  $.Method({ Static: true, Public: true }, "ToBase64String",
    (new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.Array", [$.Byte])], [])),
    function ToBase64String(inArray) {
      return toBase64StringImpl(inArray, 0, inArray.length, 0);
    }
  );

  $.Method({ Static: true, Public: true }, "ToBase64String",
    (new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.Array", [$.Byte]), $jsilcore.TypeRef("System.Base64FormattingOptions")], [])),
    function ToBase64String(inArray, options) {
      return toBase64StringImpl(inArray, 0, inArray.length, options);
    }
  );

  $.Method({ Static: true, Public: true }, "ToBase64String",
    (new JSIL.MethodSignature($.String, [
          $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
          $.Int32
    ], [])),
    function ToBase64String(inArray, offset, length) {
      return toBase64StringImpl(inArray, offset, length, 0);
    }
  );

  $.Method({ Static: true, Public: true }, "ToBase64String",
    (new JSIL.MethodSignature($.String, [
          $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
          $.Int32, $jsilcore.TypeRef("System.Base64FormattingOptions")
    ], [])),
    toBase64StringImpl
  );

    $.Method({ Static: true, Public: true }, "ToBase64CharArray",
        (new JSIL.MethodSignature($.Int32, [
            $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
            $.Int32, $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32, $jsilcore.TypeRef("System.Base64FormattingOptions")
        ], [])),
        function(inArray, offsetIn, length, outArray, offsetOut, options) {
            return toBase64CharArray(inArray, offsetIn, length, outArray, offsetOut, options);
        }
    );

    $.Method({ Static: true, Public: true }, "ToBase64CharArray",
        (new JSIL.MethodSignature($.Int32, [
            $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
            $.Int32, $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32
        ], [])),
        function(inArray, offsetIn, length, outArray, offsetOut) {
            return toBase64CharArray(inArray, offsetIn, length, outArray, offsetOut, 0);
        }
    );

  $.Method({ Static: true, Public: true }, "FromBase64String",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.String], [])),
    function FromBase64String(s) {
      var lengthErrorMessage = "Invalid length for a Base-64 char array.";
      var contentErrorMessage = "The input is not a valid Base-64 string as it contains a non-base 64 character, more than two padding characters, or a non-white space character among the padding characters.";

      var result = [];
      var reader = $jsilcore.makeCharacterReader(s);
      var sum = 0;
      var ch0 = 0, ch1 = 0, ch2 = 0, ch3 = 0;
      var index0 = -1, index1 = -1, index2 = -1, index3 = -1;
      var equals = "=".charCodeAt(0);

      while (true) {
        ch0 = reader.read();
        if (ch0 === false)
          break;
        if (base64IgnoredCodepoints.indexOf(ch0) >= 0)
          continue;

        ch1 = reader.read();
        ch2 = reader.read();
        ch3 = reader.read();

        if ((ch1 === false) || (ch2 === false) || (ch3 === false))
          throw new System.FormatException(lengthErrorMessage);

        index0 = base64CodeTable.indexOf(ch0);
        index1 = base64CodeTable.indexOf(ch1);
        index2 = base64CodeTable.indexOf(ch2);
        index3 = base64CodeTable.indexOf(ch3);

        if (
          (index0 < 0) || (index0 > 63) ||
          (index1 < 0) || (index1 > 63)
        )
          throw new System.FormatException(contentErrorMessage);

        sum = (index0 << 18) | (index1 << 12);

        if (index2 >= 0)
          sum |= (index2 << 6);
        else if (ch2 !== equals)
          throw new System.FormatException(contentErrorMessage);

        if (index3 >= 0)
          sum |= (index3 << 0);
        else if (ch3 !== equals)
          throw new System.FormatException(contentErrorMessage);

        result.push((sum >> 16) & 0xFF);
        if (index2 >= 0)
          result.push((sum >> 8) & 0xFF);
        if (index3 >= 0)
          result.push(sum & 0xFF);
      }

      return JSIL.Array.New($jsilcore.System.Byte, result);
    }
  );
});
$jsilcore.SerializationScratchBuffers = null;

$jsilcore.GetSerializationScratchBuffers = function () {
  if (!$jsilcore.SerializationScratchBuffers) {
    var uint8 = new Uint8Array(32);
    var buffer = uint8.buffer;

    $jsilcore.SerializationScratchBuffers = {
      uint8: uint8,
      uint16: new Uint16Array(buffer),
      uint32: new Uint32Array(buffer),
      int8: new Int8Array(buffer),
      int16: new Int16Array(buffer),
      int32: new Int32Array(buffer),
      float32: new Float32Array(buffer),
      float64: new Float64Array(buffer),
      slice: function (byteCount) {
        byteCount = byteCount | 0;

        var result = new Uint8Array(byteCount);
        for (var i = 0; i < byteCount; i++)
          result[i] = uint8[i];

        return result;
      },
      fillFrom: function (bytes, offset, count) {
        offset = offset | 0;
        count = count | 0;

        if (!bytes)
          JSIL.RuntimeError("bytes cannot be null");

        for (var i = 0; i < count; i++)
          uint8[i] = bytes[offset + i];
      }
    };
  }

  return $jsilcore.SerializationScratchBuffers;
};


$jsilcore.BytesFromBoolean = function (value) {
  return [value ? 1 : 0];
};


$jsilcore.BytesFromSingle = function (value) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.float32[0] = value;
  return bufs.slice(4);
};

$jsilcore.BytesFromDouble = function (value) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.float64[0] = value;
  return bufs.slice(8);
};

$jsilcore.BytesFromInt16 = function (value) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.int16[0] = value;
  return bufs.slice(2);
};

$jsilcore.BytesFromInt32 = function (value) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.int32[0] = value;
  return bufs.slice(4);
};

$jsilcore.BytesFromInt64 = function (value) {
  return [
    (value.a >> 0) & 0xFF,
    (value.a >> 8) & 0xFF,
    (value.a >> 16) & 0xFF,
    (value.b >> 0) & 0xFF,
    (value.b >> 8) & 0xFF,
    (value.b >> 16) & 0xFF,
    (value.c >> 0) & 0xFF,
    (value.c >> 8) & 0xFF
  ];
};

// FIXME: Are these unsigned versions right?

$jsilcore.BytesFromUInt16 = function (value) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.uint16[0] = value;
  return bufs.slice(2);
};

$jsilcore.BytesFromUInt32 = function (value) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.uint32[0] = value;
  return bufs.slice(4);
};

$jsilcore.BytesFromUInt64 = function (value) {
  return [
    (value.a >>> 0) & 0xFF,
    (value.a >>> 8) & 0xFF,
    (value.a >>> 16) & 0xFF,
    (value.b >>> 0) & 0xFF,
    (value.b >>> 8) & 0xFF,
    (value.b >>> 16) & 0xFF,
    (value.c >>> 0) & 0xFF,
    (value.c >>> 8) & 0xFF
  ];
};


$jsilcore.BytesToBoolean = function (bytes, offset) {
  return bytes[offset] !== 0;
};

$jsilcore.BytesToInt16 = function (bytes, offset) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.fillFrom(bytes, offset, 2);
  return bufs.int16[0];
};

$jsilcore.BytesToInt32 = function (bytes, offset) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.fillFrom(bytes, offset, 4);
  return bufs.int32[0];
};

$jsilcore.BytesToInt64 = function (bytes, offset) {
  return $jsilcore.System.Int64.FromBytes(bytes, offset);
};

$jsilcore.BytesToUInt16 = function (bytes, offset) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.fillFrom(bytes, offset, 2);
  return bufs.uint16[0];
};

$jsilcore.BytesToUInt32 = function (bytes, offset) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.fillFrom(bytes, offset, 4);
  return bufs.uint32[0];
};

$jsilcore.BytesToUInt64 = function (bytes, offset) {
  return $jsilcore.System.UInt64.FromBytes(bytes, offset);
};

$jsilcore.BytesToSingle = function (bytes, offset) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.fillFrom(bytes, offset, 4);
  return bufs.float32[0];
};

$jsilcore.BytesToDouble = function (bytes, offset) {
  var bufs = $jsilcore.GetSerializationScratchBuffers();
  bufs.fillFrom(bytes, offset, 8);
  return bufs.float64[0];
};

JSIL.ImplementExternals("System.BitConverter", function ($) {
  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.Boolean], [])),
    $jsilcore.BytesFromBoolean
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.Int16], [])),
    $jsilcore.BytesFromInt16
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.Int32], [])),
    $jsilcore.BytesFromInt32
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.Int64], [])),
    $jsilcore.BytesFromInt64
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.UInt16], [])),
    $jsilcore.BytesFromUInt16
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.UInt32], [])),
    $jsilcore.BytesFromUInt32
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.UInt64], [])),
    $jsilcore.BytesFromUInt64
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.Single], [])),
    $jsilcore.BytesFromSingle
  );

  $.Method({ Static: true, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.Double], [])),
    $jsilcore.BytesFromDouble
  );

  /*

  $.Method({Static:true , Public:false}, "GetHexValue",
    (new JSIL.MethodSignature($.Char, [$.Int32], [])),
    function GetHexValue (i) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "Int64BitsToDouble",
    (new JSIL.MethodSignature($.Double, [$.Int64], [])),
    function Int64BitsToDouble (value) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "ToChar",
    (new JSIL.MethodSignature($.Char, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    function ToChar (value, startIndex) {
      throw new Error('Not implemented');
    }
  );

  */

  $.Method({ Static: true, Public: true }, "ToBoolean",
    (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToBoolean
  );

  $.Method({ Static: true, Public: true }, "ToInt16",
    (new JSIL.MethodSignature($.Int16, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToInt16
  );

  $.Method({ Static: true, Public: true }, "ToInt32",
    (new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToInt32
  );

  $.Method({ Static: true, Public: true }, "ToInt64",
    (new JSIL.MethodSignature($.Int64, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToInt64
  );

  /*

  $.Method({Static:true , Public:true }, "ToString",
    (new JSIL.MethodSignature($.String, [
          $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
          $.Int32
        ], [])),
    function ToString (value, startIndex, length) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "ToString",
    (new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.Array", [$.Byte])], [])),
    function ToString (value) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "ToString",
    (new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    function ToString (value, startIndex) {
      throw new Error('Not implemented');
    }
  );

  */

  $.Method({ Static: true, Public: true }, "ToUInt16",
    (new JSIL.MethodSignature($.UInt16, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToUInt16
  );

  $.Method({ Static: true, Public: true }, "ToUInt32",
    (new JSIL.MethodSignature($.UInt32, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToUInt32
  );

  $.Method({ Static: true, Public: true }, "ToUInt64",
    (new JSIL.MethodSignature($.UInt64, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToUInt64
  );

  $.Method({ Static: true, Public: true }, "ToSingle",
    (new JSIL.MethodSignature($.Single, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToSingle
  );

  $.Method({ Static: true, Public: true }, "ToDouble",
    (new JSIL.MethodSignature($.Double, [$jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32], [])),
    $jsilcore.BytesToDouble
  );

  $.Method({ Static: true, Public: true }, "DoubleToInt64Bits",
    (new JSIL.MethodSignature($.Int64, [$.Double], [])),
    function DoubleToInt64Bits(double) {
      return $jsilcore.BytesToInt64($jsilcore.BytesFromDouble(double), 0);
    }
  );
});



  ﻿JSIL.ImplementExternals("System.Random", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
        this.mt = new MersenneTwister();
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Int32], [])),
      function _ctor(Seed) {
        this.mt = new MersenneTwister(Seed);
      }
    );

    $.Method({ Static: false, Public: true }, "Next",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function Next() {
        var unsigned32 = this.mt.genrand_int32();
        return unsigned32 << 0;
      }
    );

    $.Method({ Static: false, Public: true }, "Next",
      (new JSIL.MethodSignature($.Int32, [$.Int32, $.Int32], [])),
      function Next(minValue, maxValue) {
        var real = this.mt.genrand_real1();
        return Math.floor(real * (maxValue - minValue)) + minValue;
      }
    );

    $.Method({ Static: false, Public: true }, "Next",
      (new JSIL.MethodSignature($.Int32, [$.Int32], [])),
      function Next(maxValue) {
        var real = this.mt.genrand_real1();
        return Math.floor(real * maxValue);
      }
    );

    $.Method({ Static: false, Public: true }, "NextDouble",
      (new JSIL.MethodSignature($.Double, [], [])),
      function NextDouble() {
        return this.mt.genrand_real1();
      }
    );
  });



// modified from http://snipplr.com/view/6889/regular-expressions-for-uri-validationparsing/
var regexUri = /^([a-z][a-z0-9+.-]*):(?:\/\/((?:(?=((?:[a-z0-9-._~!$&'()*+,;=:]|%[0-9A-F]{2})*))(\3)@)?(?=(\[[0-9A-F:.]{2,}\]|(?:[a-z0-9-._~!$&'()*+,;=]|%[0-9A-F]{2})*))\5(?::(?=(\d*))\6)?)(\/(?=((?:[a-z0-9-._~!$&'()*+,;=:@\/]|%[0-9A-F]{2})*))\8)?|(\/?(?!\/)(?=((?:[a-z0-9-._~!$&'()*+,;=:@\/]|%[0-9A-F]{2})*))\10)?)(?:\?(?=((?:[a-z0-9-._~!$&'()*+,;=:@\/?]|%[0-9A-F]{2})*))\11)?(?:#(?=((?:[a-z0-9-._~!$&'()*+,;=:@\/?]|%[0-9A-F]{2})*))\12)?$/i;

function parseURI(uriString) {
  uriString = uriString.replace(/\\/g, '/');
  var uri = {};
  var match = uriString.match(regexUri);
  if (match === null) {
    // it's not a uri
    uri.path = uriString;
  } else {
    uri.scheme = match[1] || match[6];
    uri.userinfo = match[2]
    uri.host = match[3]
    uri.port = match[4]
    uri.path = match[5] || match[7]
    uri.query = match[8]
    uri.fragment = match[9]
  }
  return uri;
}

function pathSplit(s) {
  return s.split(/\//g).filter(function (x) { return x.length > 0 });
}

JSIL.ImplementExternals("System.Uri", function ($) {
  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$.String], []),
    function _ctor(uriString) {
      var uri = parseURI(uriString);
      this._scheme = uri.scheme;
      this._path = pathSplit(uri.path);
    }
  );

  // Join a path onto the end of an existing uri
  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Uri"), $.String], []),
    function _ctor(baseUri, relativeUriStr) {
      this._scheme = baseUri._scheme;
      var relativeUri = new System.Uri(relativeUriStr);
      this._path = baseUri._path.concat(relativeUri._path);
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsAbsoluteUri",
    new JSIL.MethodSignature($.Boolean, [], []),
    function get_IsAbsoluteUri() {
      return this._scheme !== undefined;
    }
  );

  $.Method({ Static: false, Public: true }, "get_LocalPath",
    new JSIL.MethodSignature($.String, [], []),
    function get_LocalPath() {
      if (this.IsAbsoluteUri) {
        return "\\\\" + this._path.join("\\");
      } else {
        throw new System.InvalidOperationException("only an absolute uri has a LocalPath");
      }
      return this._length;
    }
  );
});



JSIL.ImplementExternals(
  "System.Diagnostics.Debug", function ($) {
    $.Method({ Static: true, Public: true }, "WriteLine",
      (new JSIL.MethodSignature(null, [$.String], [])),
      function WriteLine(message) {
        JSIL.Host.logWriteLine(message);
      }
    );

    $.Method({ Static: true, Public: true }, "Write",
      (new JSIL.MethodSignature(null, [$.String], [])),
      function Write(message) {
        JSIL.Host.logWrite(message);
      }
    );
  }
);

JSIL.ImplementExternals("System.Diagnostics.Debug", function ($) {

  $.Method({ Static: true, Public: true }, "Assert",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function Assert(condition) {
      if (!condition)
        JSIL.Host.assertionFailed("Assertion Failed");
    }
  );

  $.Method({ Static: true, Public: true }, "Assert",
    (new JSIL.MethodSignature(null, [$.Boolean, $.String], [])),
    function Assert(condition, message) {
      if (!condition)
        JSIL.Host.assertionFailed(message);
    }
  );

});
JSIL.ImplementExternals("System.Diagnostics.StackTrace", function ($) {
  var mscorlib = JSIL.GetCorlib();

  $.Method({ Static: false, Public: true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor() {
      this.CaptureStackTrace(0, false, null, null);
    }
  );

  $.Method({ Static: false, Public: false }, "CaptureStackTrace",
    (new JSIL.MethodSignature(null, [
          $.Int32, $.Boolean,
          mscorlib.TypeRef("System.Threading.Thread"), mscorlib.TypeRef("System.Exception")
    ], [])),
    function CaptureStackTrace(iSkip, fNeedFileInfo, targetThread, e) {
      // FIXME
      this.frames = [];
    }
  );

  $.Method({ Static: false, Public: true }, "GetFrame",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Diagnostics.StackFrame"), [$.Int32], [])),
    function GetFrame(index) {
      // FIXME
      return new System.Diagnostics.StackFrame();
    }
  );

});
JSIL.ImplementExternals("System.Diagnostics.StackFrame", function ($) {
  var mscorlib = JSIL.GetCorlib();

  $.Method({ Static: false, Public: true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor() {
      // FIXME
    }
  );

  $.Method({ Static: false, Public: true }, "GetMethod",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Reflection.MethodBase"), [], [])),
    function GetMethod() {
      // FIXME
      return new System.Reflection.MethodBase();
    }
  );
});
JSIL.ImplementExternals("System.Diagnostics.Stopwatch", function ($) {
  var mscorlib = JSIL.GetCorlib();

  $.Method({ Static: false, Public: true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor() {
      this.Reset();
    }
  );

  $.Method({ Static: false, Public: true }, "get_Elapsed",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.TimeSpan"), [], [])),
    function get_Elapsed() {
      return System.TimeSpan.FromMilliseconds(this.get_ElapsedMilliseconds());
    }
  );

  $.Method({ Static: false, Public: true }, "get_ElapsedMilliseconds",
    (new JSIL.MethodSignature($.Int64, [], [])),
    function get_ElapsedMilliseconds() {
      var result = this.elapsed;
      if (this.isRunning)
        result += JSIL.Host.getTickCount() - this.startedWhen;

      return $jsilcore.System.Int64.FromNumber(result);
    }
  );

  $.Method({ Static: false, Public: true }, "get_ElapsedTicks",
    (new JSIL.MethodSignature($.Int64, [], [])),
    function get_ElapsedTicks() {
      var result = this.elapsed;
      if (this.isRunning)
        result += JSIL.Host.getTickCount() - this.startedWhen;

      result *= 10000;

      return $jsilcore.System.Int64.FromNumber(result);
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsRunning",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsRunning() {
      return this.isRunning;
    }
  );

  $.Method({ Static: false, Public: true }, "Reset",
    (JSIL.MethodSignature.Void),
    function Reset() {
      this.elapsed = 0;
      this.isRunning = false;
      this.startedWhen = 0;
    }
  );

  $.Method({ Static: false, Public: true }, "Restart",
    (JSIL.MethodSignature.Void),
    function Restart() {
      this.elapsed = 0;
      this.isRunning = true;
      this.startedWhen = JSIL.Host.getTickCount();
    }
  );

  $.Method({ Static: false, Public: true }, "Start",
    (JSIL.MethodSignature.Void),
    function Start() {
      if (!this.isRunning) {
        this.startedWhen = JSIL.Host.getTickCount();
        this.isRunning = true;
      }
    }
  );

  $.Method({ Static: true, Public: true }, "StartNew",
    (new JSIL.MethodSignature($.Type, [], [])),
    function StartNew() {
      var result = new System.Diagnostics.Stopwatch();
      result.Start();
      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "Stop",
    (JSIL.MethodSignature.Void),
    function Stop() {
      if (this.isRunning) {
        this.isRunning = false;

        var now = JSIL.Host.getTickCount();
        var elapsed = now - this.startedWhen;

        this.elapsed += elapsed;
        if (this.elapsed < 0)
          this.elapsed = 0;
      }
    }
  );

});


JSIL.ImplementExternals("System.Diagnostics.Trace", function ($) {
  $.Method({ Static: true, Public: true }, "TraceError",
    new JSIL.MethodSignature(null, [$.String], []),
    function TraceError(message) {
      var svc = JSIL.Host.getService("trace", true);
      if (svc)
        svc.error(message);
    }
  );

  $.Method({ Static: true, Public: true }, "TraceError",
    new JSIL.MethodSignature(null, [$.String, $jsilcore.TypeRef("System.Array", [$.Object])], []),
    function TraceError(format, args) {
      var svc = JSIL.Host.getService("trace", true);
      var message = System.String.Format(format, args);
      if (svc)
        svc.error(message);
    }
  );

  $.Method({ Static: true, Public: true }, "TraceInformation",
    new JSIL.MethodSignature(null, [$.String], []),
    function TraceInformation(message) {
      var svc = JSIL.Host.getService("trace", true);
      if (svc)
        svc.information(message);
    }
  );

  $.Method({ Static: true, Public: true }, "TraceInformation",
    new JSIL.MethodSignature(null, [$.String, $jsilcore.TypeRef("System.Array", [$.Object])], []),
    function TraceInformation(format, args) {
      var svc = JSIL.Host.getService("trace", true);
      var message = System.String.Format(format, args);
      if (svc)
        svc.information(message);
    }
  );

  $.Method({ Static: true, Public: true }, "TraceWarning",
    new JSIL.MethodSignature(null, [$.String], []),
    function TraceWarning(message) {
      var svc = JSIL.Host.getService("trace", true);
      if (svc)
        svc.warning(message);
    }
  );

  $.Method({ Static: true, Public: true }, "TraceWarning",
    new JSIL.MethodSignature(null, [$.String, $jsilcore.TypeRef("System.Array", [$.Object])], []),
    function TraceWarning(format, args) {
      var svc = JSIL.Host.getService("trace", true);
      var message = System.String.Format(format, args);
      if (svc)
        svc.warning(message);
    }
  );

  $.Method({ Static: true, Public: true }, "WriteLine",
    new JSIL.MethodSignature(null, [$.String], []),
    function WriteLine(message) {
      var svc = JSIL.Host.getService("trace", true);
      if (svc)
        svc.write(message);
    }
  );

  $.Method({ Static: true, Public: true }, "WriteLine",
    new JSIL.MethodSignature(null, [$.String, $.String], []),
    function WriteLine(message, category) {
      var svc = JSIL.Host.getService("trace", true);
      if (svc)
        svc.write(message, category);
    }
  );
});



JSIL.ImplementExternals("System.GC", function ($) {
  var getMemoryImpl = function () {
    var svc = JSIL.Host.getService("window");
    return svc.getPerformanceUsedJSHeapSize();
  };

  $.Method({ Static: true, Public: false }, "GetTotalMemory",
    (new JSIL.MethodSignature($.Int64, [], [])),
    function GetTotalMemory() {
      return getMemoryImpl();
    }
  );

  $.Method({ Static: true, Public: true }, "GetTotalMemory",
    (new JSIL.MethodSignature($.Int64, [$.Boolean], [])),
    function GetTotalMemory(forceFullCollection) {
      // FIXME: forceFullCollection

      return getMemoryImpl();
    }
  );

  $.Method({ Static: true, Public: false }, "IsServerGC",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function IsServerGC() {
      return false;
    }
  );
});
JSIL.ImplementExternals(
  "System.Threading.Interlocked", function ($) {
    var cmpxchg = function (targetRef, value, comparand) {
      var currentValue = targetRef.get();

      if (currentValue === comparand)
        targetRef.set(value);

      return currentValue;
    };

    $.Method({ Public: true, Static: true }, "CompareExchange",
      new JSIL.MethodSignature("!!0", [JSIL.Reference.Of("!!0"), "!!0", "!!0"], ["T"]),
      function (T, targetRef, value, comparand) {
        return cmpxchg(targetRef, value, comparand);
      }
    );

    $.Method({ Static: true, Public: true }, "CompareExchange",
      (new JSIL.MethodSignature($.Int32, [
            $jsilcore.TypeRef("JSIL.Reference", [$.Int32]), $.Int32,
            $.Int32
      ], [])),
      function CompareExchange(/* ref */ location1, value, comparand) {
        return cmpxchg(location1, value, comparand);
      }
    );
  }
);

JSIL.ImplementExternals("System.Threading.Interlocked", function ($) {
  $.Method({ Public: true, Static: true }, "CompareExchange",
    new JSIL.MethodSignature("!!0", [JSIL.Reference.Of("!!0"), "!!0", "!!0"], ["T"]),
    function CompareExchange$b1(T, /* ref */ location1, value, comparand) {
      var result = JSIL.CloneParameter(T, location1.get());
      if (JSIL.ObjectEquals(location1.get(), comparand)) {
        location1.set(JSIL.CloneParameter(T, value));
      }
      return result;
    }
  );

  $.Method({ Public: true, Static: true }, "CompareExchange",
    new JSIL.MethodSignature($.Object, [JSIL.Reference.Of($.Object), $.Object, $.Object], []),
    function CompareExchange(/* ref */ location1, value, comparand) {
      var result = location1.get();
      if (JSIL.ObjectEquals(location1.get(), comparand)) {
        location1.set(value);
      }
      return result;
    }
  );
});


JSIL.ImplementExternals("System.Threading.Monitor", function ($) {
  var enterImpl = function (obj) {
    var current = (obj.__LockCount__ || 0);
    if (current >= 1)
      JSIL.Host.warning("Warning: lock recursion " + obj);

    obj.__LockCount__ = current + 1;

    return true;
  };

  $.Method({ Static: true, Public: true }, "Enter",
    (new JSIL.MethodSignature(null, [$.Object], [])),
    function Enter(obj) {
      enterImpl(obj);
    }
  );

  $.Method({ Static: true, Public: true }, "Enter",
    (new JSIL.MethodSignature(null, [$.Object, $jsilcore.TypeRef("JSIL.Reference", [$.Boolean])], [])),
    function Enter(obj, /* ref */ lockTaken) {
      lockTaken.set(enterImpl(obj));
    }
  );

  $.Method({ Static: true, Public: true }, "Exit",
    (new JSIL.MethodSignature(null, [$.Object], [])),
    function Exit(obj) {
      var current = (obj.__LockCount__ || 0);
      if (current <= 0)
        JSIL.Host.warning("Warning: unlocking an object that is not locked " + obj);

      obj.__LockCount__ = current - 1;
    }
  );

});


JSIL.ImplementExternals(
  "System.Threading.Thread", function ($) {
    $.Method({ Static: true, Public: true }, ".cctor2",
      (JSIL.MethodSignature.Void),
      function () {
        // This type already has a cctor, so we add a second one.
        System.Threading.Thread._currentThread = JSIL.CreateInstanceOfType(
          System.Threading.Thread.__Type__,
          null
        );
      }
    );

    $.Method({ Static: true, Public: true }, "get_CurrentThread",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Thread"), [], [])),
      function get_CurrentThread() {
        return System.Threading.Thread._currentThread;
      }
    );

    $.Method({ Static: false, Public: true }, "get_ManagedThreadId",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_ManagedThreadId() {
        return 0;
      }
    );

    $.Method({ Static: true, Public: true }, "MemoryBarrier",
      JSIL.MethodSignature.Void,
      function thread_MemoryBarrier() {
      }
    );
  }
);


JSIL.ImplementExternals("System.Threading.Volatile", function ($) {
  $.Method({ Static: true, Public: true }, "Write",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), "!!0"], ["T"]),
    function Write(T, /* ref */ location, value) {
      location.set(JSIL.CloneParameter(T, value));
    }
  );

  $.Method({ Static: true, Public: true }, "Read",
    new JSIL.MethodSignature("!!0", [$jsilcore.TypeRef("JSIL.Reference", ["!!0"])], ["T"]),
    function Read(T, /* ref */ location) {
      return location.get();
    }
  );
});


JSIL.ImplementExternals(
  "System.Threading.SemaphoreSlim", function ($) {
      $.Method({ Static: false, Public: true }, ".ctor",
        (new JSIL.MethodSignature(null, [$.Int32], [])),
        function _ctor(initialCount) {
            this._count = initialCount;

            this._tcs_queue = new (System.Collections.Generic.Queue$b1.Of(System.Threading.Tasks.TaskCompletionSource$b1.Of(System.Boolean)))();
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
          (new JSIL.MethodSignature(null, [$.Int32, $.Int32], [])),
          function _ctor(initialCount, maxCount) {
              // FIXME: Implement MaxCount ctor for SemaphoreSlim
              this._count = initialCount;
              this._max_count = maxCount;

              this._tcs_queue = new (System.Collections.Generic.Queue$b1.Of(System.Threading.Tasks.TaskCompletionSource$b1.Of(System.Boolean)))();
          }
      );

      $.Method({ Static: false, Public: true }, "WaitAsync",
        (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [], [])),
        function WaitAsync() {
            var tcs = new (System.Threading.Tasks.TaskCompletionSource$b1.Of(System.Boolean))();
            if (this._count > 0) {
                tcs.TrySetResult(true);
            } else {
                this._tcs_queue.Enqueue(tcs);
            }
            this._count--;
            return tcs.Task;
        }
      );

      $.Method({ Static: false, Public: true }, "Release",
        (new JSIL.MethodSignature($.Int32, [], [])),
        function Release() {
            this._count++;
            if (this._tcs_queue.Count > 0) {
                var tcs = this._tcs_queue.Dequeue();
                tcs.TrySetResult(true);
            }
        }
      );

      $.Method({ Static: false, Public: true }, "get_CurrentCount",
        (new JSIL.MethodSignature($.Int32, [], [])),
        function get_CurrentCount() {
            return this._count;
        }
      );
  }
);

﻿
JSIL.ImplementExternals("System.Collections.Concurrent.ConcurrentQueue`1", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
          $jsilcore.InitResizableArray(this, this.T, 16);
          this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "Clear",
      (JSIL.MethodSignature.Void),
      function Clear() {
          this._items.length = this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "TryDequeue",
      (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("JSIL.Reference", [new JSIL.GenericParameter("T", "System.Collections.Concurrent.ConcurrentQueue`1")])], [])),
      function Dequeue(result) {
          if (this._size > 0) {
              var item = this._items.shift();
              this._size -= 1;
              result.set(item);
              return true;
          } else {
              result.set(JSIL.DefaultValue(T));
              return false;
          }
      }
    );

    $.Method({ Static: false, Public: true }, "Enqueue",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("T", "System.Collections.Concurrent.ConcurrentQueue`1")], [])),
      function Enqueue(item) {
          this._items.push(item);
          this._size += 1;
      }
    );

    $.Method({ Static: false, Public: true }, "ToArray",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("T", "System.Collections.Concurrent.ConcurrentQueue`1"), [], [])),
      function ToArray(item) {
          return this._items.slice(0, this._size);
      }
    );

    $.Method({ Static: false, Public: true }, "get_Count",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_Count() {
          return this._size;
      }
    );

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("T", "System.Collections.Concurrent.ConcurrentQueue`1")]), [], [])),
      function GetEnumerator() {
          return this.$GetEnumerator();
      }
    )
      .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

});

  ﻿JSIL.ImplementExternals("System.Collections.Generic.List`1", function ($) {
    var T = new JSIL.GenericParameter("T", "System.Collections.Generic.List`1");

    $jsilcore.$ListExternals($, T, "List");

    $.Method({ Static: false, Public: true }, "CopyTo",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [T]), $.Int32], []),
      function (array, arrayindex) {
        if (arrayindex != 0) {
          JSIL.RuntimeError("List<T>.CopyTo not supported for non-zero indexes");
        }

        JSIL.Array.ShallowCopy(array, this._items);
      }
    );

    $.Method({ Static: false, Public: true }, "get_IsReadOnly",
      new JSIL.MethodSignature($.Boolean, [], []),
      function () {
        return false;
      }
    );

  });


  JSIL.MakeStruct(
    "System.ValueType", "System.Collections.Generic.List`1+Enumerator", true, ["T"],
    function ($) {
      $.Field({ Public: false, Static: false }, "_array", Array, function ($) { return null; });
      $.Field({ Public: false, Static: false }, "_length", Number, function ($) { return 0; });
      $.Field({ Public: false, Static: false }, "_index", Number, function ($) { return -1; });

      $.Method({ Public: true, Static: false }, ".ctor",
        new JSIL.MethodSignature(null, ["System.Collections.Generic.List`1"]),
        function (list) {
          if (!list)
            throw new Error("List must be specified");

          this._array = list._items;
          this._length = list._size;
          this._index = -1;
        }
      );

      $.RawMethod(false, "__CopyMembers__",
        function __CopyMembers__(source, target) {
          target._array = source._array;
          target._length = source._length;
          target._index = source._index;
        }
      );

      $.Method({ Static: false, Public: true, Virtual: true }, "Dispose",
        JSIL.MethodSignature.Void,
        function Dispose() {
          this._array = null;
        }
      );

      $.Method({ Static: false, Public: true, Virtual: true }, "get_Current",
        new JSIL.MethodSignature($.GenericParameter("T"), [], []),
        function get_Current() {
          return this._array[this._index];
        }
      )
          .Overrides("System.Collections.Generic.IEnumerator`1", "get_Current");

      $.Method({ Static: false, Public: true, Virtual: true }, "MoveNext",
        new JSIL.MethodSignature($.Boolean, [], []),
        function MoveNext() {
          this._index += 1;
          return (this._index < this._length);
        }
      );

      $.Method({ Static: false, Public: false }, null,
        new JSIL.MethodSignature($.Object, [], []),
        function System_Collections_IEnumerator_get_Current() {
          return this._array[this._index];
        }
      )
        .Overrides("System.Collections.IEnumerator", "get_Current");

      $.Method({ Static: false, Public: false, Virtual: true }, "Reset",
        JSIL.MethodSignature.Void,
        function Reset() {
          this._index = -1;
        }
      )
        .Overrides("System.Collections.IEnumerator", "Reset");

      $.ImplementInterfaces(
        $jsilcore.TypeRef("System.Collections.IEnumerator"),
        $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.List`1+Enumerator")])
      );
    }
  );
  ﻿$jsilcore.$ArrayListExternals = function ($) {
    $jsilcore.$ListExternals($, $.Object, "ArrayList");

    var mscorlib = JSIL.GetCorlib();
    var toArrayImpl = function () {
      return Array.prototype.slice.call(this._items, 0, this._size);
    };

    $.Method({ Static: false, Public: true }, "ToArray",
      new JSIL.MethodSignature(mscorlib.TypeRef("System.Array"), [$.Object], []),
      toArrayImpl
    );
  };

  // Lazy way of sharing method implementations between ArrayList, Collection<T> and List<T>.
  JSIL.ImplementExternals("System.Collections.ArrayList", $jsilcore.$ArrayListExternals);


  ﻿$jsilcore.$CollectionExternals = function ($) {
    var T = new JSIL.GenericParameter("T", "System.Collections.ObjectModel.Collection`1");
    $jsilcore.$ListExternals($, T, "Collection");

    var mscorlib = JSIL.GetCorlib();

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Collections.Generic.IList`1", [T])], []),
      function (list) {
        this._items = JSIL.EnumerableToArray(list, this.T);
        this._capacity = this._size = this._items.length;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "CopyTo",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [new JSIL.GenericParameter("T", "System.Collections.ObjectModel.Collection`1")]), $.Int32], []),
      function CopyTo(array, index) {
        JSIL.Array.CopyTo(this._items, array, index);
      }
    );
  };

  JSIL.ImplementExternals("System.Collections.ObjectModel.Collection`1", $jsilcore.$CollectionExternals);
  ﻿$jsilcore.$ReadOnlyCollectionExternals = function ($) {
    var T = new JSIL.GenericParameter("T", "System.Collections.ObjectModel.ReadOnlyCollection`1");
    $jsilcore.$ListExternals($, T, "ReadOnlyCollection");

    var mscorlib = JSIL.GetCorlib();

    var IListCtor = function (list) {
      this._list = list;

      if (JSIL.IsArray(list._array)) {
        Object.defineProperty(this, "_items", {
          get: function () {
            return list._array;
          }
        });

        Object.defineProperty(this, "_size", {
          get: function () {
            return list._array.length;
          }
        });
      } else {
        if (!list._items || (typeof (list._size) !== "number"))
          JSIL.RuntimeError("argument must be a list");

        Object.defineProperty(this, "_items", {
          get: function () {
            return list._items;
          }
        });

        Object.defineProperty(this, "_size", {
          get: function () {
            return list._size;
          }
        });
      }
    };

    $.RawMethod(false, "$listCtor", IListCtor);

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [mscorlib.TypeRef("System.Collections.Generic.IList`1", [T])], []),
      IListCtor
    );

    $.SetValue("Add", null);
    $.SetValue("Clear", null);
    $.SetValue("Remove", null);
    $.SetValue("RemoveAt", null);
    $.SetValue("RemoveAll", null);
    $.SetValue("Sort", null);
  };

  JSIL.ImplementExternals("System.Collections.ObjectModel.ReadOnlyCollection`1", $jsilcore.$ReadOnlyCollectionExternals);
  ﻿  ﻿
  JSIL.ImplementExternals("System.Collections.Generic.Stack`1", function ($) {
    var system = JSIL.GetAssembly("System", true);

    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
        $jsilcore.InitResizableArray(this, this.T, 16);
        this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Int32], [])),
      function _ctor(capacity) {
        $jsilcore.InitResizableArray(this, this.T, capacity);
        this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "Clear",
      (JSIL.MethodSignature.Void),
      function Clear() {
        this._items.length = this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Count",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_Count() {
        return this._size;
      }
    );

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      (new JSIL.MethodSignature(system.TypeRef("System.Collections.Generic.Stack`1+Enumerator", [new JSIL.GenericParameter("T", "System.Collections.Generic.Stack`1")]), [], [])),
      function GetEnumerator() {
        return this.$GetEnumerator();
      }
    );

    $.Method({ Static: false, Public: true }, "Peek",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("T", "System.Collections.Generic.Stack`1"), [], [])),
      function Peek() {
        if (this._size <= 0)
          throw new System.InvalidOperationException("Stack is empty");

        return this._items[this._size - 1];
      }
    );

    $.Method({ Static: false, Public: true }, "Pop",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("T", "System.Collections.Generic.Stack`1"), [], [])),
      function Pop() {
        var result = this._items.pop();
        this._size -= 1;

        return result;
      }
    );

    $.Method({ Static: false, Public: true }, "Push",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("T", "System.Collections.Generic.Stack`1")], [])),
      function Push(item) {
        this._items.push(item)
        this._size += 1;
      }
    );

  });


  ﻿  ﻿
  JSIL.ImplementExternals("System.Collections.Generic.Queue`1", function ($) {
    var system = JSIL.GetAssembly("System", true);

    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
        $jsilcore.InitResizableArray(this, this.T, 16);
        this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Int32], [])),
      function _ctor(capacity) {
        $jsilcore.InitResizableArray(this, this.T, capacity);
        this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "Clear",
      (JSIL.MethodSignature.Void),
      function Clear() {
        this._items.length = this._size = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "Dequeue",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("T", "System.Collections.Generic.Queue`1"), [], [])),
      function Dequeue() {
        var result = this._items.shift();
        this._size -= 1;
        return result;
      }
    );

    $.Method({ Static: false, Public: true }, "Enqueue",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("T", "System.Collections.Generic.Queue`1")], [])),
      function Enqueue(item) {
        this._items.push(item);
        this._size += 1;
      }
    );

    $.Method({ Static: false, Public: true }, "Contains",
      new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("T", "System.Collections.Generic.Queue`1")], []),
      function Contains(value) {
        return JSIL.Array.IndexOf(this._items, 0, this._items.length, value) >= 0;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Count",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_Count() {
        return this._size;
      }
    );

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      (new JSIL.MethodSignature(system.TypeRef("System.Collections.Generic.Queue`1+Enumerator", [new JSIL.GenericParameter("T", "System.Collections.Generic.Queue`1")]), [], [])),
      function GetEnumerator() {
        return this.$GetEnumerator();
      }
    );

  });
  ﻿  ﻿  $jsilcore.hashContainerBase = function ($) {
    var mscorlib = JSIL.GetCorlib();

    var BucketEntry = function (key, value) {
      this.key = key;
      this.value = value;
    };

    $.RawMethod(false, "$areEqual", function HashContainer_AreEqual(lhs, rhs) {
      if (lhs === rhs)
        return true;

      return JSIL.ObjectEquals(lhs, rhs);
    });

    $.RawMethod(false, "$searchBucket", function HashContainer_SearchBucket(key) {
      var hashCode = JSIL.ObjectHashCode(key, true);
      var bucket = this._dict[hashCode];
      if (!bucket)
        return null;

      for (var i = 0, l = bucket.length; i < l; i++) {
        var bucketEntry = bucket[i];

        if (this.$areEqual(bucketEntry.key, key))
          return bucketEntry;
      }

      return null;
    });

    $.RawMethod(false, "$removeByKey", function HashContainer_Remove(key) {
      var hashCode = JSIL.ObjectHashCode(key, true);
      var bucket = this._dict[hashCode];
      if (!bucket)
        return false;

      for (var i = 0, l = bucket.length; i < l; i++) {
        var bucketEntry = bucket[i];

        if (this.$areEqual(bucketEntry.key, key)) {
          bucket.splice(i, 1);
          this._count -= 1;
          return true;
        }
      }

      return false;
    });

    $.RawMethod(false, "$addToBucket", function HashContainer_Add(key, value) {
      var hashCode = JSIL.ObjectHashCode(key, true);
      var bucket = this._dict[hashCode];
      if (!bucket)
        this._dict[hashCode] = bucket = [];

      bucket.push(new BucketEntry(key, value));
      this._count += 1;
      return value;
    });
  };

  JSIL.ImplementExternals("System.Collections.Generic.Dictionary`2", $jsilcore.hashContainerBase);

  JSIL.ImplementExternals("System.Collections.Generic.Dictionary`2", function ($) {
    var mscorlib = JSIL.GetCorlib();

    function initFields(self) {
      self._dict = JSIL.CreateDictionaryObject(null);
      self._count = 0;
      self.tKeyCollection = null;
      self.tValueCollection = null;
      self.tKeyEnumerator = null;
      self.tValueEnumerator = null;
      self.tEnumerator = null;
      self.tKeyValuePair = System.Collections.Generic.KeyValuePair$b2.Of(self.TKey, self.TValue).__Type__;
    };

    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
        initFields(this);
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Int32], [])),
      function _ctor(capacity) {
        initFields(this);
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.IDictionary`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")])], [])),
      function _ctor(dictionary) {
        initFields(this);

        var enumerator = JSIL.GetEnumerator(dictionary);
        while (enumerator.MoveNext())
          this.Add(enumerator.Current.Key, enumerator.Current.Value);
        enumerator.Dispose();
      }
    );

    $.Method({ Static: false, Public: true }, "Add",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")], [])),
      function Add(key, value) {
        var bucketEntry = this.$searchBucket(key);

        if (bucketEntry !== null)
          throw new System.ArgumentException("Key already exists");

        return this.$addToBucket(key, value);
      }
    );

    $.Method({ Static: false, Public: true }, "Clear",
      (JSIL.MethodSignature.Void),
      function Clear() {
        this._dict = {}
        this._count = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "ContainsKey",
      (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2")], [])),
      function ContainsKey(key) {
        return this.$searchBucket(key) !== null;
      }
    );

    $.Method({ Static: false, Public: true }, "Remove",
      (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2")], [])),
      function Remove(key) {
        return this.$removeByKey(key);
      }
    );

    $.Method({ Static: false, Public: true }, "get_Count",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_Count() {
        return this._count;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Item",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2"), [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2")], [])),
      function get_Item(key) {
        var bucketEntry = this.$searchBucket(key);
        if (bucketEntry !== null)
          return bucketEntry.value;
        else
          throw new System.Collections.Generic.KeyNotFoundException("Key not found");
      }
    );

    var getKeysImpl = function GetKeys() {
      if (this.tKeyCollection === null) {
        this.tKeyCollection = $jsilcore.System.Collections.Generic.Dictionary$b2_KeyCollection.Of(this.TKey, this.TValue).__Type__;
        this.tKeyEnumerator = $jsilcore.System.Collections.Generic.Dictionary$b2_KeyCollection_Enumerator.Of(this.TKey, this.TValue).__Type__;
      }

      return JSIL.CreateInstanceOfType(this.tKeyCollection, "_ctor", [this]);
    };

    $.Method({ Static: false, Public: true }, "get_Keys",
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.Dictionary`2+KeyCollection", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")]), [], [])),
      getKeysImpl
    );

    $.Method({ Static: false, Public: false }, null,
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.ICollection", []), [], [])),
      getKeysImpl
    )
        .Overrides("System.Collections.IDictionary", "get_Keys");

    $.Method({ Static: false, Public: false }, null,
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.ICollection`1", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2")]), [], [])),
      getKeysImpl
    )
        .Overrides("System.Collections.Generic.IDictionary`2", "get_Keys");

    var getValuesImpl = function GetValues() {
      if (this.tValueCollection === null) {
        this.tValueCollection = $jsilcore.System.Collections.Generic.Dictionary$b2_ValueCollection.Of(this.TKey, this.TValue).__Type__;
        this.tValueEnumerator = $jsilcore.System.Collections.Generic.Dictionary$b2_ValueCollection_Enumerator.Of(this.TKey, this.TValue).__Type__;
      }

      return JSIL.CreateInstanceOfType(this.tValueCollection, "_ctor", [this]);
    };

    $.Method({ Static: false, Public: true }, "get_Values",
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.Dictionary`2+ValueCollection", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")]), [], [])),
      getValuesImpl
    );

    $.Method({ Static: false, Public: false }, null,
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.ICollection", []), [], [])),
      getValuesImpl
    )
        .Overrides("System.Collections.IDictionary", "get_Values");

    $.Method({ Static: false, Public: false }, null,
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.Generic.ICollection`1", [new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")]), [], [])),
      getValuesImpl
    )
        .Overrides("System.Collections.Generic.IDictionary`2", "get_Values");

    var getEnumeratorImpl = function GetEnumerator() {
      if (this.tEnumerator === null) {
        this.tEnumerator = $jsilcore.System.Collections.Generic.Dictionary$b2_Enumerator.Of(this.TKey, this.TValue).__Type__;
      }

      return JSIL.CreateInstanceOfType(this.tEnumerator, "_ctor", [this]);
    };

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      (new JSIL.MethodSignature(
        mscorlib.TypeRef(
          "System.Collections.Generic.Dictionary`2+Enumerator", [
            new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"),
            new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")
          ]
        ), [], [])
      ),
      getEnumeratorImpl
    );

    $.Method({ Static: false, Public: false }, null,
      (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.IEnumerator"), [], [])),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.IEnumerable", "GetEnumerator");

    $.Method({ Static: false, Public: false }, null,
      (new JSIL.MethodSignature(
        mscorlib.TypeRef(
          "System.Collections.Generic.IEnumerator`1", [
            mscorlib.TypeRef(
              "System.Collections.Generic.KeyValuePair`2", [
                new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"),
                new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")
              ]
            )
          ]
        ), [], [])
      ),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

    $.Method({ Static: false, Public: true }, "set_Item",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")], [])),
      function set_Item(key, value) {
        var bucketEntry = this.$searchBucket(key);
        if (bucketEntry !== null)
          return bucketEntry.value = value;
        else
          return this.$addToBucket(key, value);
      }
    );

    $.Method({ Static: false, Public: true }, "TryGetValue",
      (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2"), $jsilcore.TypeRef("JSIL.Reference", [new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2")])], [])),
      function TryGetValue(key, /* ref */ value) {
        var bucketEntry = this.$searchBucket(key);
        if (bucketEntry !== null) {
          value.set(bucketEntry.value);
          return true;
        } else {
          value.set(JSIL.DefaultValue(this.TValue));
        }

        return false;
      }
    );

  });

  JSIL.ImplementExternals("System.Collections.Generic.Dictionary`2+KeyCollection", function ($interfaceBuilder) {
    var $ = $interfaceBuilder;

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.Dictionary`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+KeyCollection")])], []),
      function _ctor(dictionary) {
        this.dictionary = dictionary;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "get_Count",
      new JSIL.MethodSignature($.Int32, [], []),
      function get_Count() {
        return this.dictionary.get_Count();
      }
    );

    var getEnumeratorImpl = function GetEnumerator() {
      return JSIL.CreateInstanceOfType(this.dictionary.tKeyEnumerator, "_ctor", [this.dictionary]);
    };

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+KeyCollection")]), [], []),
      getEnumeratorImpl
    );

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection")]), [], []),
      getEnumeratorImpl
    )
       .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.IEnumerator"), [], []),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.IEnumerable", "GetEnumerator");
  });

  JSIL.ImplementExternals("System.Collections.Generic.Dictionary`2+ValueCollection", function ($interfaceBuilder) {
    var $ = $interfaceBuilder;

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.Dictionary`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+ValueCollection"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+ValueCollection")])], []),
      function _ctor(dictionary) {
        this.dictionary = dictionary;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "get_Count",
      new JSIL.MethodSignature($.Int32, [], []),
      function get_Count() {
        return this.dictionary.get_Count();
      }
    );

    var getEnumeratorImpl = function GetEnumerator() {
      return JSIL.CreateInstanceOfType(this.dictionary.tValueEnumerator, "_ctor", [this.dictionary]);
    };

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.Dictionary`2+ValueCollection+Enumerator", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+ValueCollection"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+ValueCollection")]), [], []),
      getEnumeratorImpl
    );

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+ValueCollection")]), [], []),
      getEnumeratorImpl
    )
       .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.IEnumerator"), [], []),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.IEnumerable", "GetEnumerator");
  });

  JSIL.ImplementExternals("System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator", function ($interfaceBuilder) {
    var $ = $interfaceBuilder;

    $.Method({ Static: false, Public: false }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.Dictionary`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator")])], []),
      function _ctor(dictionary) {
        this.dictionary = dictionary;
        this.kvpEnumerator = null;
        this.Reset();
      }
    );

    $.RawMethod(false, "__CopyMembers__",
      function __CopyMembers__(source, target) {
        target.dictionary = source.dictionary;
        if (source.kvpEnumerator)
          target.kvpEnumerator = source.kvpEnumerator.MemberwiseClone();
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "Dispose",
      JSIL.MethodSignature.Void,
      function Dispose() {
        if (this.kvpEnumerator)
          this.kvpEnumerator.Dispose();

        this.kvpEnumerator = null;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "get_Current",
      new JSIL.MethodSignature(new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator"), [], []),
      function get_Current() {
        return this.kvpEnumerator.get_Current().key;
      }
    )
        .Overrides("System.Collections.Generic.IEnumerator`1", "get_Current");

    $.Method({ Static: false, Public: true, Virtual: true }, "MoveNext",
      new JSIL.MethodSignature($.Boolean, [], []),
      function MoveNext() {
        return this.kvpEnumerator.MoveNext();
      }
    );

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($.Object, [], []),
      function System_Collections_IEnumerator_get_Current() {
        return this.kvpEnumerator.get_Current().key;
      }
    )
      .Overrides("System.Collections.IEnumerator", "get_Current");

    $.Method({ Static: false, Public: false, Virtual: true }, "Reset",
      JSIL.MethodSignature.Void,
      function Reset() {
        this.kvpEnumerator = this.dictionary.GetEnumerator();
      }
    )
      .Overrides("System.Collections.IEnumerator", "Reset");
  });

  JSIL.ImplementExternals("System.Collections.Generic.Dictionary`2+ValueCollection+Enumerator", function ($interfaceBuilder) {
    var $ = $interfaceBuilder;

    $.Method({ Static: false, Public: false }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.Dictionary`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+ValueCollection+Enumerator"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+ValueCollection+Enumerator")])], []),
      function _ctor(dictionary) {
        this.dictionary = dictionary;
        this.kvpEnumerator = null;
        this.Reset();
      }
    );

    $.RawMethod(false, "__CopyMembers__",
      function __CopyMembers__(source, target) {
        target.dictionary = source.dictionary;
        if (source.kvpEnumerator)
          target.kvpEnumerator = source.kvpEnumerator.MemberwiseClone();
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "Dispose",
      JSIL.MethodSignature.Void,
      function Dispose() {
        if (this.kvpEnumerator)
          this.kvpEnumerator.Dispose();

        this.kvpEnumerator = null;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "get_Current",
      new JSIL.MethodSignature(new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+ValueCollection+Enumerator"), [], []),
      function get_Current() {
        return this.kvpEnumerator.get_Current().value;
      }
    )
        .Overrides("System.Collections.Generic.IEnumerator`1", "get_Current");

    $.Method({ Static: false, Public: true, Virtual: true }, "MoveNext",
      new JSIL.MethodSignature($.Boolean, [], []),
      function MoveNext() {
        return this.kvpEnumerator.MoveNext();
      }
    );

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($.Object, [], []),
      function System_Collections_IEnumerator_get_Current() {
        return this.kvpEnumerator.get_Current().value;
      }
    )
      .Overrides("System.Collections.IEnumerator", "get_Current");

    $.Method({ Static: false, Public: false, Virtual: true }, "Reset",
      JSIL.MethodSignature.Void,
      function System_Collections_IEnumerator_Reset() {
        this.kvpEnumerator = this.dictionary.GetEnumerator();
      }
    )
      .Overrides("System.Collections.IEnumerator", "Reset");
  });

  JSIL.ImplementExternals("System.Collections.Generic.Dictionary`2+Enumerator", function ($interfaceBuilder) {
    var $ = $interfaceBuilder;

    $.RawMethod(false, "__CopyMembers__",
      function __CopyMembers__(source, target) {
        target.dictionary = source.dictionary;
        target.state = source.state;
      }
    );

    $.Method({ Static: false, Public: false }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.Dictionary`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+Enumerator"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+Enumerator")])], []),
      function _ctor(dictionary) {
        this.dictionary = dictionary;

        var tKey = dictionary.TKey, tValue = dictionary.TValue;
        var tKvp = dictionary.tKeyValuePair;

        this.state = {
          tKey: tKey,
          tValue: tValue,
          tKvp: tKvp,
          bucketIndex: 0,
          valueIndex: -1,
          keys: Object.keys(dictionary._dict),
          current: JSIL.CreateInstanceOfType(tKvp, "_ctor", [JSIL.DefaultValue(tKey), JSIL.DefaultValue(tValue)])
        };
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "Dispose",
      JSIL.MethodSignature.Void,
      function Dispose() {
        this.state = null;
        this.dictionary = null;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "get_Current",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.KeyValuePair`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+Enumerator"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+Enumerator")]), [], []),
      function get_Current() {
        return this.state.current.MemberwiseClone();
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "MoveNext",
      new JSIL.MethodSignature($.Boolean, [], []),
      function MoveNext() {
        var state = this.state;
        var dict = this.dictionary._dict;
        var keys = state.keys;
        var valueIndex = ++(state.valueIndex);
        var bucketIndex = state.bucketIndex;

        while ((bucketIndex >= 0) && (bucketIndex < keys.length)) {
          var bucketKey = keys[state.bucketIndex];
          var bucket = dict[bucketKey];

          if ((valueIndex >= 0) && (valueIndex < bucket.length)) {
            var current = state.current;
            current.key = bucket[valueIndex].key;
            current.value = bucket[valueIndex].value;
            return true;
          } else {
            bucketIndex = ++(state.bucketIndex);
            valueIndex = 0;
          }
        }

        return false;
      }
    );

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($.Object, [], []),
      function System_Collections_IEnumerator_get_Current() {
        return this.state.current.MemberwiseClone();
      }
    )
      .Overrides("System.Collections.IEnumerator", "get_Current");

    $.Method({ Static: false, Public: false, Virtual: true }, "Reset",
      JSIL.MethodSignature.Void,
      function System_Collections_IEnumerator_Reset() {
        this.state.bucketIndex = 0;
        this.state.valueIndex = -1;
      }
    )
      .Overrides("System.Collections.IEnumerator", "Reset");
  });


  JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.Object"),
    Name: "System.Collections.Generic.Dictionary`2+KeyCollection",
    IsPublic: true,
    IsReferenceType: true,
    GenericParameters: ["TKey", "TValue"],
    MaximumConstructorArguments: 1,
  }, function ($) {
    $.ImplementInterfaces(
        $jsilcore.TypeRef("System.Collections.Generic.ICollection`1", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection")]),
        $jsilcore.TypeRef("System.Collections.ICollection"),
        $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection")]),
        $jsilcore.TypeRef("System.Collections.IEnumerable")
    );
  });

  JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.Object"),
    Name: "System.Collections.Generic.Dictionary`2+ValueCollection",
    IsPublic: true,
    IsReferenceType: true,
    GenericParameters: ["TKey", "TValue"],
    MaximumConstructorArguments: 1,
  }, function ($) {
    $.ImplementInterfaces(
        $jsilcore.TypeRef("System.Collections.Generic.ICollection`1", [new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+ValueCollection")]),
        $jsilcore.TypeRef("System.Collections.ICollection"),
        $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+ValueCollection")]),
        $jsilcore.TypeRef("System.Collections.IEnumerable")
    );
  });

  JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.ValueType"),
    Name: "System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator",
    IsPublic: true,
    IsReferenceType: false,
    GenericParameters: ["TKey", "TValue"],
    MaximumConstructorArguments: 1,
  }, function ($) {
    $.ImplementInterfaces(
        $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator")]),
        $jsilcore.TypeRef("System.IDisposable"),
        $jsilcore.TypeRef("System.Collections.IEnumerator")
    );
  });

  JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.ValueType"),
    Name: "System.Collections.Generic.Dictionary`2+ValueCollection+Enumerator",
    IsPublic: true,
    IsReferenceType: false,
    GenericParameters: ["TKey", "TValue"],
    MaximumConstructorArguments: 1,
  }, function ($) {
    $.ImplementInterfaces(
        $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+KeyCollection+Enumerator")]),
        $jsilcore.TypeRef("System.IDisposable"),
        $jsilcore.TypeRef("System.Collections.IEnumerator")
    );
  });

  JSIL.MakeStruct($jsilcore.TypeRef("System.ValueType"), "System.Collections.Generic.Dictionary`2+Enumerator", false, ["TKey", "TValue"], function ($) {
    $.ImplementInterfaces(
        /* 0 */ $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [$jsilcore.TypeRef("System.Collections.Generic.KeyValuePair`2", [new JSIL.GenericParameter("TKey", "System.Collections.Generic.Dictionary`2+Enumerator"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.Dictionary`2+Enumerator")])]),
        /* 1 */ $jsilcore.TypeRef("System.IDisposable"),
        /* 2 */ $jsilcore.TypeRef("System.Collections.IDictionaryEnumerator"),
        /* 3 */ $jsilcore.TypeRef("System.Collections.IEnumerator")
    );
  });
  ﻿JSIL.ImplementExternals("System.Collections.Generic.KeyValuePair`2", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("TKey", "System.Collections.Generic.KeyValuePair`2"), new JSIL.GenericParameter("TValue", "System.Collections.Generic.KeyValuePair`2")], [])),
      function _ctor(key, value) {
        this.key = key;
        this.value = value;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Key",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("TKey", "System.Collections.Generic.KeyValuePair`2"), [], [])),
      function get_Key() {
        return this.key;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Value",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("TValue", "System.Collections.Generic.KeyValuePair`2"), [], [])),
      function get_Value() {
        return this.value;
      }
    );

    $.Method({ Static: false, Public: true }, "toString",
      (new JSIL.MethodSignature($.String, [], [])),
      function toString() {
        return "[" + String(this.key) + ", " + String(this.value) + "]";
      }
    );

  });


  ﻿  ﻿
  JSIL.ImplementExternals("System.Collections.Generic.HashSet`1+Enumerator", function ($interfaceBuilder) {
    var $ = $interfaceBuilder;

    $.RawMethod(false, "__CopyMembers__",
      function __CopyMembers__(source, target) {
        target.hashSet = source.hashSet;
        target.state = source.state;
      }
    );

    $.Method({ Static: false, Public: false }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.HashSet`1", [$.GenericParameter("T")])], []),
      function _ctor(hashSet) {
        this.hashSet = hashSet;

        var t = hashSet.T;

        this.state = {
          t: t,
          bucketIndex: 0,
          valueIndex: -1,
          keys: Object.keys(hashSet._dict),
          current: null
        };
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "Dispose",
      JSIL.MethodSignature.Void,
      function Dispose() {
        this.state = null;
        this.hashSet = null;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "get_Current",
      new JSIL.MethodSignature($.GenericParameter("T"), [], []),
      function get_Current() {
        return this.state.current;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "MoveNext",
      new JSIL.MethodSignature($.Boolean, [], []),
      function MoveNext() {
        var state = this.state;
        var dict = this.hashSet._dict;
        var keys = state.keys;
        var valueIndex = ++(state.valueIndex);
        var bucketIndex = state.bucketIndex;

        while ((bucketIndex >= 0) && (bucketIndex < keys.length)) {
          var bucketKey = keys[state.bucketIndex];
          var bucket = dict[bucketKey];

          if ((valueIndex >= 0) && (valueIndex < bucket.length)) {
            state.current = bucket[valueIndex].key;
            return true;
          } else {
            bucketIndex = ++(state.bucketIndex);
            valueIndex = state.valueIndex = 0;
          }
        }

        return false;
      }
    );

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($.Object, [], []),
      function System_Collections_IEnumerator_get_Current() {
        return this.state.current;
      }
    )
      .Overrides("System.Collections.IEnumerator", "get_Current");

    $.Method({ Static: false, Public: false, Virtual: true }, "Reset",
      JSIL.MethodSignature.Void,
      function System_Collections_IEnumerator_Reset() {
        this.state.bucketIndex = 0;
        this.state.valueIndex = -1;
      }
    )
      .Overrides("System.Collections.IEnumerator", "Reset");
  });

  JSIL.ImplementExternals("System.Collections.Generic.HashSet`1", $jsilcore.hashContainerBase);

  JSIL.ImplementExternals("System.Collections.Generic.HashSet`1", function ($) {
    var mscorlib = JSIL.GetCorlib();
    var T = new JSIL.GenericParameter("T", "System.Collections.Generic.HashSet`1");

    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
        this._dict = {};
        this._count = 0;
        this._comparer = null;
        this.tEnumerator = null;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.IEqualityComparer`1", [T])], [])),
      function _ctor(comparer) {
        this._dict = {};
        this._count = 0;
        this._comparer = comparer;
        this.tEnumerator = null;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [T])], [])),
      function _ctor(collection) {
        this._dict = {};
        this._count = 0;
        this._comparer = null;
        this.$addRange(collection);
        this.tEnumerator = null;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [T]), $jsilcore.TypeRef("System.Collections.Generic.IEqualityComparer`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.HashSet`1")])], [])),
      function _ctor(collection, comparer) {
        this._dict = {};
        this._count = 0;
        this._comparer = comparer;
        this.$addRange(collection);
        this.tEnumerator = null;
      }
    );

    var addImpl = function Add(item) {
      var bucketEntry = this.$searchBucket(item);

      if (bucketEntry !== null)
        return false;

      this.$addToBucket(item, true);
      return true;
    };

    $.Method({ Static: false, Public: true }, "Add",
      (new JSIL.MethodSignature($.Boolean, [T], [])),
      addImpl
    );

    $.Method({ Static: false, Public: false }, null,
      (new JSIL.MethodSignature(null, [T], [])),
      addImpl
    )
      .Overrides("System.Collections.Generic.ICollection`1", "Add");

    $.RawMethod(false, "$addRange", function (enumerable) {
      var values = JSIL.EnumerableToArray(enumerable, this.T);

      for (var i = 0; i < values.length; i++)
        this.Add(values[i]);
    });

    $.Method({ Static: false, Public: true }, "Clear",
      (JSIL.MethodSignature.Void),
      function Clear() {
        this._dict = {};
        this._count = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "Contains",
      (new JSIL.MethodSignature($.Boolean, [T], [])),
      function Contains(item) {
        return this.$searchBucket(item) !== null;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Count",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_Count() {
        return this._count;
      }
    );

    $.Method({ Static: false, Public: true }, "Remove",
      (new JSIL.MethodSignature($.Boolean, [T], [])),
      function Remove(item) {
        return this.$removeByKey(item);
      }
    );

    var getEnumeratorImpl = function GetEnumerator() {
      if (this.tEnumerator === null) {
        this.tEnumerator = $jsilcore.System.Collections.Generic.HashSet$b1_Enumerator.Of(this.T).__Type__;
      }

      return JSIL.CreateInstanceOfType(this.tEnumerator, "_ctor", [this]);
    };

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      new JSIL.MethodSignature(
        $jsilcore.TypeRef("System.Collections.Generic.HashSet`1+Enumerator", [T]), [], []
      ),
      getEnumeratorImpl
    )

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature(
        $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [T]), [], []
      ),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature(
        $jsilcore.TypeRef("System.Collections.IEnumerator", []), [], []
      ),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.IEnumerable", "GetEnumerator");

    $.Method({ Static: false, Public: true }, "UnionWith",
      new JSIL.MethodSignature(
        null, [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.HashSet`1")])], []
      ),
      function UnionWith(other) {
        this.$addRange(other);
      });
  });


  JSIL.MakeStruct($jsilcore.TypeRef("System.ValueType"), "System.Collections.Generic.HashSet`1+Enumerator", false, ["T"], function ($) {
    var T = new JSIL.GenericParameter("T", "System.Collections.Generic.HashSet`1+Enumerator");

    $.ImplementInterfaces(
        /* 0 */ $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [T]),
        /* 1 */ $jsilcore.TypeRef("System.IDisposable"),
        /* 2 */ $jsilcore.TypeRef("System.Collections.IEnumerator")
    );
  });
  ﻿JSIL.ImplementExternals("System.Collections.Generic.LinkedList`1", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function _ctor() {
        this.Clear();
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")])], [])),
      function _ctor(collection) {
        this.Clear();

        throw new Error('Not implemented');
      }
    );

    var makeNode = function (self, value) {
      var tNode = System.Collections.Generic.LinkedListNode$b1.Of(self.T).__Type__;
      return JSIL.CreateInstanceOfType(tNode, "_ctor", [self, value]);
    };

    var addIntoEmptyImpl = function (self, node) {
      if ((!self._head) && (!self._tail)) {
        node._list = self;
        self._head = self._tail = node;
        self._count = 1;
        return true;
      }

      return false;
    }

    var addBeforeImpl = function (self, beforeNode, node) {
      if (addIntoEmptyImpl(self, node))
        return;

      node._list = self;
      node._next = beforeNode;

      if (beforeNode)
        beforeNode._previous = node;

      if (self._head === beforeNode)
        self._head = node;

      self._count += 1;
    };

    var addAfterImpl = function (self, afterNode, node) {
      if (addIntoEmptyImpl(self, node))
        return;

      node._list = self;
      node._previous = afterNode;

      if (afterNode)
        afterNode._next = node;

      if (self._tail === afterNode)
        self._tail = node;

      self._count += 1;
    };

    var addFirstImpl = function (self, node) {
      addBeforeImpl(self, self._head, node);
    };

    var addLastImpl = function (self, node) {
      addAfterImpl(self, self._tail, node);
    };

    $.Method({ Static: false, Public: true }, "AddAfter",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [$jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function AddAfter(node, value) {
        var newNode = makeNode(self, value);
        addAfterImpl(this, node, newNode);
        return newNode;
      }
    );

    $.Method({ Static: false, Public: true }, "AddAfter",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), $jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")])], [])),
      function AddAfter(node, newNode) {
        addAfterImpl(this, node, newNode);
      }
    );

    $.Method({ Static: false, Public: true }, "AddBefore",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [$jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function AddBefore(node, value) {
        var newNode = makeNode(self, value);
        addBeforeImpl(this, node, newNode);
        return newNode;
      }
    );

    $.Method({ Static: false, Public: true }, "AddBefore",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), $jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")])], [])),
      function AddBefore(node, newNode) {
        addBeforeImpl(this, node, newNode);
      }
    );

    $.Method({ Static: false, Public: true }, "AddFirst",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function AddFirst(value) {
        var node = makeNode(this, value);
        addFirstImpl(this, node);
        return node;
      }
    );

    $.Method({ Static: false, Public: true }, "AddFirst",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")])], [])),
      function AddFirst(node) {
        addFirstImpl(this, node);
      }
    );

    $.Method({ Static: false, Public: true }, "AddLast",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function AddLast(value) {
        var node = makeNode(this, value);
        addLastImpl(this, node);
        return node;
      }
    );

    $.Method({ Static: false, Public: true }, "AddLast",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")])], [])),
      function AddLast(node) {
        addLastImpl(this, node);
      }
    );

    $.Method({ Static: false, Public: true }, "Clear",
      (JSIL.MethodSignature.Void),
      function Clear() {
        this._head = null;
        this._tail = null;
        this._count = 0;
      }
    );

    $.Method({ Static: false, Public: true }, "Contains",
      (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function Contains(value) {
        throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "CopyTo",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), $.Int32], [])),
      function CopyTo(array, index) {
        throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "Find",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function Find(value) {
        throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "FindLast",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function FindLast(value) {
        throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "get_Count",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function get_Count() {
        return this._count;
      }
    );

    $.Method({ Static: false, Public: true }, "get_First",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [], [])),
      function get_First() {
        return this._head;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Last",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [], [])),
      function get_Last() {
        return this._tail;
      }
    );

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedList`1+Enumerator", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")]), [], [])),
      function GetEnumerator() {
        throw new Error('Not implemented');
      }
    );

    $.RawMethod(false, "$removeNode", function Remove_Node(node) {
      if (node._list !== this)
        JSIL.RuntimeError("Node is not a member of this list");

      var previous = node._previous || null;
      var next = node._next || null;

      if (previous)
        previous._next = next;
      if (next)
        next._previous = previous;

      if (this._head === node)
        this._head = next;
      else if (this._tail === node)
        this._tail = previous;

      node._list = null;
      node._count -= 1;
    });

    $.Method({ Static: false, Public: true }, "Remove",
      (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")], [])),
      function Remove(value) {
        throw new Error('Not implemented');
      }
    );

    $.Method({ Static: false, Public: true }, "Remove",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedList`1")])], [])),
      function Remove(node) {
        this.$removeNode(node);
      }
    );

    $.Method({ Static: false, Public: true }, "RemoveFirst",
      (JSIL.MethodSignature.Void),
      function RemoveFirst() {
        this.$removeNode(this._head);
      }
    );

    $.Method({ Static: false, Public: true }, "RemoveLast",
      (JSIL.MethodSignature.Void),
      function RemoveLast() {
        this.$removeNode(this._tail);
      }
    );
  });
  ﻿JSIL.ImplementExternals("System.Collections.Generic.LinkedListNode`1", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1")], [])),
      function _ctor(value) {
        this._list = null;
        this._value = value;
        this._previous = null;
        this._next = null;
      }
    );

    $.Method({ Static: false, Public: false }, ".ctor",
      (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Collections.Generic.LinkedList`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1")]), new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1")], [])),
      function _ctor(list, value) {
        this._list = list;
        this._value = value;
        this._previous = null;
        this._next = null;
      }
    );

    $.Method({ Static: false, Public: true }, "get_List",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedList`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1")]), [], [])),
      function get_List() {
        return this._list;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Next",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1")]), [], [])),
      function get_Next() {
        return this._next;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Previous",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.LinkedListNode`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1")]), [], [])),
      function get_Previous() {
        return this._previous;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Value",
      (new JSIL.MethodSignature(new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1"), [], [])),
      function get_Value() {
        return this._value;
      }
    );

    $.Method({ Static: false, Public: true }, "set_Value",
      (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("T", "System.Collections.Generic.LinkedListNode`1")], [])),
      function set_Value(value) {
        this._value = value;
      }
    );

  });
  ﻿function BitArray(length) {
    this._bytes = new Uint8Array(Math.ceil(length / 8));
  }

  BitArray.prototype.get = function (i) {
    var _byte = Math.floor(i / 8);
    var mask = 1 << (((i / 8) - _byte) * 8);

    return (this._bytes[_byte] & mask) != 0;
  }

  BitArray.prototype.set = function (i, bool) {
    var _byte = Math.floor(i / 8);
    var mask = 1 << (((i / 8) - _byte) * 8);

    if (bool) {
      this._bytes[_byte] |= mask;
    } else {
      this._bytes[_byte] &= ~mask;
    }
  }

  JSIL.ImplementExternals("System.Collections.BitArray", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [$.Int32], []),
      function _ctor(length) {
        this._length = length;
        this._bitarray = new BitArray(length);
      }
    );

    $.Method({ Static: false, Public: true }, "get_Length",
      new JSIL.MethodSignature($.Int32, [], []),
      function get_Length() {
        return this._length;
      }
    );

    $.Method({ Static: false, Public: true }, "Get",
      new JSIL.MethodSignature($.Boolean, [$.Int32], []),
      function Get(index) {
        return this._bitarray.get(index);
      }
    );

    $.Method({ Static: false, Public: true }, "get_Item",
      new JSIL.MethodSignature($.Boolean, [$.Int32], []),
      function get_Item(index) {
        return this._bitarray.get(index);
      }
    );

    $.Method({ Static: false, Public: true }, "Set",
      new JSIL.MethodSignature(null, [$.Int32, $.Boolean], []),
      function Set(index, bool) {
        return this._bitarray.set(index, bool);
      }
    );

    $.Method({ Static: false, Public: true }, "set_Item",
      new JSIL.MethodSignature(null, [$.Int32, $.Boolean], []),
      function set_Item(index, bool) {
        return this._bitarray.set(index, bool);
      }
    );
  });



JSIL.ImplementExternals("System.Collections.Generic.Comparer`1", function ($) {
  $.Method({ Static: true, Public: true }, "get_Default",
    new JSIL.MethodSignature($.Type, [], []),
    function get_Default() {
      // HACK
      return new (JSIL.DefaultComparer$b1.Of(this.T));
    }
  );
});


(function EqualityComparer$b1$Members() {
  var $S00 = function() {
    return ($S00 = JSIL.Memoize(new JSIL.ConstructorSignature($jsilcore.TypeRef("System.ArgumentOutOfRangeException"), [$jsilcore.TypeRef("System.String")])))();
  };

  JSIL.ImplementExternals("System.Collections.Generic.EqualityComparer`1", function($) {
    $.Method({ Static: false, Public: false }, ".ctor",
      JSIL.MethodSignature.Void,
      function EqualityComparer$b1__ctor() {}
    );

    $.Method({ Static: true, Public: false }, "CreateComparer",
      new JSIL.MethodSignature($.Type, null),
      function EqualityComparer$b1_CreateComparer() {
        return new ($jsilcore.JSIL.ObjectEqualityComparer$b1.Of(this.T))();
      }
    );

    $.Method({ Static: true, Public: true }, "get_Default",
      new JSIL.MethodSignature($.Type, null),
      function EqualityComparer$b1_get_Default() {
        var equalityComparer = this.__Type__.__PublicInterface__.defaultComparer;
        if (!equalityComparer) {
          equalityComparer = this.__Type__.__PublicInterface__.CreateComparer();
          this.__Type__.__PublicInterface__.defaultComparer = equalityComparer;
        }
        return equalityComparer;
      }
    );

    $.Method({ Static: false, Public: false, Virtual: true }, "System.Collections.IEqualityComparer.Equals",
      new JSIL.MethodSignature($.Boolean, [$.Object, $.Object]),
      function EqualityComparer$b1_System_Collections_IEqualityComparer_Equals(x, y) {
        var $s00 = new JSIL.MethodSignature($jsilcore.System.Boolean, [this.T, this.T]);
        if (x === y) {
          var result = true;
        } else if (!((x !== null) && (y !== null))) {
          result = false;
        } else {
          if ((this.T.$As(x) === null) || !this.T.$Is(y)) {
            throw $S00().Construct("Invalid type of some arguments");
          }
          result = $s00.CallVirtual("Equals", null, this, JSIL.CloneParameter(this.T, this.T.$Cast(x)), JSIL.CloneParameter(this.T, this.T.$Cast(y)));
        }
        return result;
      }).Overrides($jsilcore.TypeRef("System.Collections.IEqualityComparer"), "Equals");

    $.Method({ Static: false, Public: false, Virtual: true }, "System.Collections.IEqualityComparer.GetHashCode",
      new JSIL.MethodSignature($.Int32, [$.Object]),
      function EqualityComparer$b1_System_Collections_IEqualityComparer_GetHashCode(obj) {
        if (obj === null) {
          var result = 0;
        } else {
          if (!this.T.$Is(obj)) {
            throw $S00().Construct("Invalid argument type");
          }
          result = this.GetHashCode(JSIL.CloneParameter(this.T, this.T.$Cast(obj)));
        }
        return result;
      }
    ).Overrides($jsilcore.TypeRef("System.Collections.IEqualityComparer"), "GetHashCode");
  });

})();

(function ObjectEqualityComparer$b1$Members() {
  var $, $thisType;
  var $T00 = function() {
    return ($T00 = JSIL.Memoize($jsilcore.System.Boolean))();
  };
  var $T01 = function() {
    return ($T01 = JSIL.Memoize($jsilcore.System.Object))();
  };
  var $T02 = function() {
    return ($T02 = JSIL.Memoize($jsilcore.System.Int32))();
  };

  JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.Collections.Generic.EqualityComparer`1", [new JSIL.GenericParameter("T", "ObjectEqualityComparer`1")]),
    Name: "JSIL.ObjectEqualityComparer`1",
    IsPublic: false,
    IsReferenceType: true,
    GenericParameters: ["T"],
    MaximumConstructorArguments: 0,
  }, function($interfaceBuilder) {
    $ = $interfaceBuilder;

    $.Method({ Static: false, Public: true }, ".ctor",
      JSIL.MethodSignature.Void,
      function ObjectEqualityComparer$b1__ctor() {
        $jsilcore.System.Collections.Generic.EqualityComparer$b1.Of($thisType.T.get(this)).prototype._ctor.call(this);
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "Equals",
      new JSIL.MethodSignature($.Boolean, [$.GenericParameter("T"), $.GenericParameter("T")]),
      function ObjectEqualityComparer$b1_Equals$00(x, y) {
        if (x !== null) {
          var result = ((y !== null) &&
          (JSIL.ObjectEquals(x, y)));
        } else {
          result = (y === null);
        }
        return result;
      }
    ).Overrides($jsilcore.TypeRef("System.Collections.Generic.IEqualityComparer`1", [$.GenericParameter("T")]), "Equals");

    $.Method({ Static: false, Public: true, Virtual: true }, "GetHashCode",
      new JSIL.MethodSignature($.Int32, [$.GenericParameter("T")]),
      function ObjectEqualityComparer$b1_GetHashCode(obj) {
        if (obj === null) {
          var result = 0;
        } else {
          result = (JSIL.ObjectHashCode(obj, true));
        }
        return result;
      }
    ).Overrides($jsilcore.TypeRef("System.Collections.Generic.IEqualityComparer`1", [$.GenericParameter("T")]), "GetHashCode");

    $.ImplementInterfaces(
    );

    return function(newThisType) { $thisType = newThisType; };
  });

})();
JSIL.MakeClass(
  $jsilcore.TypeRef("System.Collections.Generic.Comparer`1", [new JSIL.GenericParameter("T", "JSIL.DefaultComparer`1")]),
  "JSIL.DefaultComparer`1", true, ["T"],
  function ($) {
    var T = new JSIL.GenericParameter("T", "JSIL.DefaultComparer`1");

    $.Method({}, "Compare",
      new JSIL.MethodSignature($.Int32, [T, T], []),
      function Compare(lhs, rhs) {
        if (lhs === null) {
          if (rhs === null)
            return 0;
          else
            return -1;
        } else if (rhs === null)
          return 1;

        if (typeof (lhs.CompareTo) === "function")
          return lhs.CompareTo(rhs);

        if (lhs < rhs)
          return -1;
        else if (lhs > rhs)
          return 1;
        else
          return 0;
      }
    );
  }
);
JSIL.MakeClass("System.Object", "JSIL.AbstractEnumerator", true, ["T"], function ($) {
  var T = new JSIL.GenericParameter("T", "JSIL.AbstractEnumerator");

  $.RawMethod(false, "__CopyMembers__",
    function AbstractEnumerator_CopyMembers(source, target) {
      target._getNextItem = source._getNextItem;
      target._reset = source._reset;
      target._dispose = source._dispose;
      target._first = source._first;
      target._needDispose = source._needDispose;
      target._current = new JSIL.BoxedVariable(source._current.get());
      target._state = source._state;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [JSIL.AnyType, JSIL.AnyType, JSIL.AnyType]),
    function (getNextItem, reset, dispose) {
      this._getNextItem = getNextItem;
      this._reset = reset;
      this._dispose = dispose;
      this._first = true;
      this._needDispose = false;
      this._current = new JSIL.BoxedVariable(null);
    }
  );

  $.Method({ Static: false, Public: true }, "Reset",
    new JSIL.MethodSignature(null, []),
    function () {
      if (this._needDispose)
        this._dispose();

      this._first = false;
      this._needDispose = true;
      this._reset();
    }
  );

  $.Method({ Static: false, Public: true }, "MoveNext",
    new JSIL.MethodSignature("System.Boolean", []),
    function () {
      if (this._first) {
        this._reset();
        this._needDispose = true;
        this._first = false;
      }

      return this._getNextItem(this._current);
    }
  );

  $.Method({ Static: false, Public: true }, "Dispose",
    new JSIL.MethodSignature(null, []),
    function () {
      if (this._needDispose)
        this._dispose();

      this._needDispose = false;
    }
  );

  $.Method({ Static: false, Public: false }, null,
    new JSIL.MethodSignature($.Object, []),
    function () {
      return this._current.get();
    }
  )
    .Overrides("System.Collections.IEnumerator", "get_Current");

  $.Method({ Static: false, Public: true }, "get_Current",
    new JSIL.MethodSignature(T, []),
    function () {
      return this._current.get();
    }
  )
    .Overrides("System.Collections.Generic.IEnumerator`1", "get_Current");

  $.Property({ Static: false, Public: true, Virtual: true }, "Current");

  $.ImplementInterfaces(
    /* 0 */ $jsilcore.TypeRef("System.Collections.IEnumerator"),
    /* 1 */ $jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [T]),
    /* 2 */ $jsilcore.TypeRef("System.IDisposable")
  );
});

  ﻿JSIL.ImplementExternals("System.EventArgs", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
  	new JSIL.MethodSignature(null, [], []),
  	function () {
  	}
    );
  });


  ﻿JSIL.ImplementExternals("System.ComponentModel.PropertyChangedEventArgs", function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
  	new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.String")], []),
  	function (propertyName) {
  	  this.propertyName = propertyName;
  	}
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "get_PropertyName",
  	new JSIL.MethodSignature($.String, [], []),
  	function () {
  	  return this.propertyName;
  	}
    );
  });

