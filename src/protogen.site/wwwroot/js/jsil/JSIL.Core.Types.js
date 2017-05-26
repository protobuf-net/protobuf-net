/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

if (!$jsilcore)
    throw new Error("JSIL.Core is required");

JSIL.MakeClass("System.ValueType", "System.Enum", true, [], function ($) {
    $.ImplementInterfaces(
      /* 0 */ $jsilcore.TypeRef("System.IConvertible")
    );
});

JSIL.ImplementExternals("System.Enum", function ($) {
    $.Method({ Static: true, Public: true }, "ToObject",
      (new JSIL.MethodSignature($.Object, ["System.Type", $.Int32], [])),
      function ToObject(enumType, value) {
        return enumType.__PublicInterface__[enumType.__ValueToName__[value]];
      }
    );

    $.Method({ Static: true, Public: true }, "ToObject",
      (new JSIL.MethodSignature($.Object, ["System.Type", $.Object], [])),
      function ToObject(enumType, value) {
        return enumType.__PublicInterface__[enumType.__ValueToName__[value.valueOf()]];
      }
    );

    $.Method({ Static: false, Public: false, Virtual: true }, "ToInt32",
      new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.IFormatProvider")], []),
      function (provider) {
        var value = this.value;
        if (typeof (value) == "number") {
          value = this.__ThisType__.__StorageType__.__PublicInterface__.$Box(value);
        }

        return $jsilcore.System.Convert.ToInt32(value, provider);
      }
    );

    $.Method({ Static: false, Public: false, Virtual: true }, "ToInt64",
      new JSIL.MethodSignature($.Int64, [$jsilcore.TypeRef("System.IFormatProvider")], []),
      function (provider) {
        var value = this.value;
        if (typeof (value) == "number") {
          value = this.__ThisType__.__StorageType__.__PublicInterface__.$Box(value);
        }

        return $jsilcore.System.Convert.ToInt64(value, provider);
      }
    );

    $.Method({ Static: false, Public: true }, "Object.Equals",
      new JSIL.MethodSignature(System.Boolean, [System.Object]),
      function (rhs) {
          if (rhs === null)
              return false;

          return (this.__ThisType__ === rhs.__ThisType__) &&
            (this.value === rhs.value);
      }
    );
});
JSIL.ImplementExternals("System.Object", function ($) {
    $.RawMethod(true, "CheckType",
      function (value) {
          var type = typeof (value);
          return value !== null && (type === "object" || type === "number" || type === "string" || type === "boolean");
      }
    );

    // FIXME: Remove this once the expressions stuff doesn't rely on it anymore
    $.RawMethod(false, "__Initialize__",
      function (initializer) {
          var isInitializer = function (v) {
              return (typeof (v) === "object") && (v !== null) &&
                (
                  (Object.getPrototypeOf(v) === JSIL.CollectionInitializer.prototype) ||
                  (Object.getPrototypeOf(v) === JSIL.ObjectInitializer.prototype)
                );
          };

          if (JSIL.IsArray(initializer)) {
              JSIL.ApplyCollectionInitializer(this, initializer);
              return this;
          } else if (isInitializer(initializer)) {
              initializer.Apply(this);
              return this;
          }

          for (var key in initializer) {
              if (!initializer.hasOwnProperty(key))
                  continue;

              var value = initializer[key];

              if (isInitializer(value)) {
                  this[key] = value.Apply(this[key]);
              } else {
                  this[key] = value;
              }
          }

          return this;
      }
    );


    $.Method({ Static: false, Public: true }, "GetType",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], [], $jsilcore),
      function Object_GetType() {
          return this.__ThisType__;
      }
    );

    $.Method({ Static: false, Public: true }, "Object.Equals",
      new JSIL.MethodSignature($.Boolean, [$.Object], [], $jsilcore),
      function Object_Equals(rhs) {
          return this === rhs;
      }
    );

    $.Method({ Static: false, Public: true }, "GetHashCode",
      (new JSIL.MethodSignature($.Int32, [], [])),
      function Object_GetHashCode() {
          return JSIL.HashCodeInternal(this);
      }
    );

    // HACK: Prevent infinite recursion
    var currentMemberwiseCloneInvocation = null;

    $.Method({ Static: false, Public: false }, "MemberwiseClone",
      new JSIL.MethodSignature($.Object, [], [], $jsilcore),
      function Object_MemberwiseClone() {
          var result = null;

          // HACK: Handle Object.MemberwiseClone direct invocation
          if (currentMemberwiseCloneInvocation === this.MemberwiseClone) {
              result = new System.Object();
          } else {
              currentMemberwiseCloneInvocation = this.MemberwiseClone;
              try {
                  result = this.MemberwiseClone();
              } finally {
                  currentMemberwiseCloneInvocation = null;
              }
          }

          return result;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, []),
      function Object__ctor() {
      }
    );

    $.Method({ Static: false, Public: true }, "toString",
      new JSIL.MethodSignature($.String, [], [], $jsilcore),
      function Object_ToString() {
          return JSIL.GetTypeName(this);
      }
    );

    $.Method({ Static: true, Public: true }, "ReferenceEquals",
      (new JSIL.MethodSignature($.Boolean, [$.Object, $.Object], [])),
      function ReferenceEquals(objA, objB) {
          return objA === objB;
      }
    );

});

JSIL.MakeClass(Object, "System.Object", true, [], function ($) {
    $jsilcore.SystemObjectInitialized = true;
});
JSIL.MakeClass("System.Object", "JSIL.CollectionInitializer", true, [], function ($) {
    $.RawMethod(false, ".ctor",
      function () {
          this.values = Array.prototype.slice.call(arguments);
      }
    );

    $.RawMethod(false, "Apply",
      function (previousValue) {
          JSIL.ApplyCollectionInitializer(previousValue, this.values);

          return previousValue;
      }
    );
});
JSIL.MakeClass("System.Object", "JSIL.ObjectInitializer", true, [], function ($) {
    $.RawMethod(false, ".ctor",
      function (newInstance, initializer) {
          this.hasInstance = (newInstance !== null);
          this.instance = newInstance;
          this.initializer = initializer;
      }
    );

    $.RawMethod(false, "Apply",
      function (previousValue) {
          var result = this.hasInstance ? this.instance : previousValue;

          if (result)
              result.__Initialize__(this.initializer);
          else
              JSIL.Host.warning("Object initializer applied to null/undefined!");

          return result;
      }
    );
});
JSIL.MakeClass("System.Object", "System.ValueType", true, [], function ($) {
  var makeComparerCore = function(typeObject, context, body) {
    var fields = JSIL.GetFieldList(typeObject);

    if (context.prototype.__CompareMembers__) {
      context.comparer = context.prototype.__CompareMembers__;
      body.push("  return context.comparer(lhs, rhs);");
    } else {
      for (var i = 0; i < fields.length; i++) {
        var field = fields[i];
        var fieldType = field.type;

        if (fieldType.__IsNumeric__ || fieldType.__IsEnum__) {
          body.push("  if (" + JSIL.FormatMemberAccess("lhs", field.name) + " !== " + JSIL.FormatMemberAccess("rhs", field.name) + ")");
        } else {
          body.push("  if (!JSIL.ObjectEquals(" + JSIL.FormatMemberAccess("lhs", field.name) + ", " + JSIL.FormatMemberAccess("rhs", field.name) + "))");
        }

        body.push("    return false;");
      }

      body.push("  return true;");
    }
  };

  var makeStructComparer = function (typeObject, publicInterface) {
    var prototype = publicInterface.prototype;
    var context = {
      prototype: prototype
    };

    var body = [];

    makeComparerCore(typeObject, context, body);

    return JSIL.CreateNamedFunction(
      typeObject.__FullName__ + ".StructComparer",
      ["lhs", "rhs"],
      body.join("\r\n")
    );
  };

  var structEquals = function Struct_Equals(lhs, rhs) {
    if (lhs === rhs)
      return true;

    if ((rhs === null) || (rhs === undefined))
      return false;

    var thisType = lhs.__ThisType__;
    var comparer = thisType.__Comparer__;
    if (comparer === $jsilcore.FunctionNotInitialized)
      comparer = thisType.__Comparer__ = makeStructComparer(thisType, thisType.__PublicInterface__);

    return comparer(lhs, rhs);
  };

  var makeGetHashCode = function (typeObject, publicInterface) {
    var body = [];

    var fields = JSIL.GetFieldList(typeObject);
    body.push("  var hash = 17;");
    for (var i = 0; i < fields.length; i++) {
      var field = fields[i];
      var fieldAccess = JSIL.FormatMemberAccess("thisReference", field.name);
      body.push("  hash =( Math.imul(hash * 23) + ((" + fieldAccess + " === null ? 17 : JSIL.ObjectHashCode(" + fieldAccess + ", true, $jsilcore.System.Object)) | 0)) | 0;");
    }
    body.push("  return hash;");

    return JSIL.CreateNamedFunction(
      typeObject.__FullName__ + ".GetHashCode",
      ["thisReference"],
      body.join("\r\n")
    );
  };

  var structGetHashCode = function Struct_GetHashCode(thisReference) {
    var thisType = thisReference.__ThisType__;
    var getHashCode = thisType.__GetHashCode__;
    if (getHashCode === $jsilcore.FunctionNotInitialized)
      getHashCode = thisType.__GetHashCode__ = makeGetHashCode(thisType, thisType.__PublicInterface__);

    return getHashCode(thisReference);
  };

  $.Method({ Static: false, Public: true }, "Object.Equals",
    new JSIL.MethodSignature($.Boolean, [System.Object]),
    function(rhs) {
      return structEquals(this, rhs);
    }
  );

  $.Method({ Static: false, Public: true }, "GetHashCode",
    new JSIL.MethodSignature($.Int32, []),
    function() {
      return structGetHashCode(this);
    }
  );
});

JSIL.MakeInterface(
  "System.IDisposable", true, [], function ($) {
      $.Method({}, "Dispose", (JSIL.MethodSignature.Void));
  }, []);

JSIL.MakeInterface(
  "System.IEquatable`1", true, ["T"], function ($) {
      $.Method({}, "Equals", (new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [new JSIL.GenericParameter("T", "System.IEquatable`1")], [])));
  }, []);
JSIL.MakeInterface(
  "System.Collections.IEnumerator", true, [], function ($) {
      $.Method({}, "MoveNext", (new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [], [])));
      $.Method({}, "get_Current", (new JSIL.MethodSignature($jsilcore.TypeRef("System.Object"), [], [])));
      $.Method({}, "Reset", (JSIL.MethodSignature.Void));
      $.Property({}, "Current");
  }, []);
JSIL.MakeInterface(
  "System.Collections.IDictionaryEnumerator", true, [], function ($) {
      $.Method({}, "get_Key", new JSIL.MethodSignature($.Object, [], []));
      $.Method({}, "get_Value", new JSIL.MethodSignature($.Object, [], []));
      // FIXME
      // $.Method({}, "get_Entry", new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.DictionaryEntry"), [], []));
      $.Property({}, "Key");
      $.Property({}, "Value");
      $.Property({}, "Entry");
  }, [$jsilcore.TypeRef("System.Collections.IEnumerator")]);
JSIL.MakeInterface(
  "System.Collections.IEnumerable", true, [], function($) {
    $.Method({}, "GetEnumerator", (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.IEnumerator"), [], [])));
  }, [],
  JSIL.$TypeAssignableFromExpression,
  function(interfaceTypeObject, signature, thisReference) {
    return JSIL.GetType(thisReference).__PublicInterface__.prototype[signature.methodKey];
  });
JSIL.MakeInterface(
  "System.Collections.Generic.IEnumerator`1", true, ["out T"], function ($) {
      $.Method({}, "get_Current", (new JSIL.MethodSignature(new JSIL.GenericParameter("T", "System.Collections.Generic.IEnumerator`1"), [], [])));
      $.Property({}, "Current");
  }, [$jsilcore.TypeRef("System.IDisposable"), $jsilcore.TypeRef("System.Collections.IEnumerator")]);
JSIL.MakeInterface(
  "System.Collections.Generic.IEnumerable`1", true, ["out T"], function ($) {
      $.Method({}, "GetEnumerator", (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.IEnumerable`1")]), [], [])));
  }, [$jsilcore.TypeRef("System.Collections.IEnumerable")],
  JSIL.$TypeAssignableFromExpression,
  function (interfaceTypeObject, signature, thisReference) {
    var typeProto = JSIL.GetType(thisReference).__PublicInterface__.prototype;
    return typeProto[signature.LookupVariantMethodKey(typeProto)];
  });
JSIL.MakeInterface(
  "System.Collections.ICollection", true, [], function($) {
    $.Method({}, "CopyTo", (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array"), $.Int32], [])));
    $.Method({}, "get_Count", (new JSIL.MethodSignature($.Int32, [], [])));
    $.Method({}, "get_SyncRoot", (new JSIL.MethodSignature($.Object, [], [])));
    $.Method({}, "get_IsSynchronized", (new JSIL.MethodSignature($.Boolean, [], [])));
    $.Property({}, "Count");
    $.Property({}, "SyncRoot");
    $.Property({}, "IsSynchronized");
  }, [$jsilcore.TypeRef("System.Collections.IEnumerable")],
  JSIL.$TypeAssignableFromExpression,
  function(interfaceTypeObject, signature, thisReference) {
    return JSIL.GetType(thisReference).__PublicInterface__.prototype[signature.methodKey];
  });
JSIL.MakeInterface(
  "System.Collections.IList", true, [], function($) {
    $.Method({}, "get_Item", (new JSIL.MethodSignature($.Object, [$.Int32], [])));
    $.Method({}, "set_Item", (new JSIL.MethodSignature(null, [$.Int32, $.Object], [])));
    $.Method({}, "Add", (new JSIL.MethodSignature($.Int32, [$.Object], [])));
    $.Method({}, "Contains", (new JSIL.MethodSignature($.Boolean, [$.Object], [])));
    $.Method({}, "Clear", (JSIL.MethodSignature.Void));
    $.Method({}, "get_IsReadOnly", (new JSIL.MethodSignature($.Boolean, [], [])));
    $.Method({}, "get_IsFixedSize", (new JSIL.MethodSignature($.Boolean, [], [])));
    $.Method({}, "IndexOf", (new JSIL.MethodSignature($.Int32, [$.Object], [])));
    $.Method({}, "Insert", (new JSIL.MethodSignature(null, [$.Int32, $.Object], [])));
    $.Method({}, "Remove", (new JSIL.MethodSignature(null, [$.Object], [])));
    $.Method({}, "RemoveAt", (new JSIL.MethodSignature(null, [$.Int32], [])));
    $.Property({}, "Item");
    $.Property({}, "IsReadOnly");
    $.Property({}, "IsFixedSize");
  }, [$jsilcore.TypeRef("System.Collections.ICollection"), $jsilcore.TypeRef("System.Collections.IEnumerable")],
  JSIL.$TypeAssignableFromExpression,
  function(interfaceTypeObject, signature, thisReference) {
    return JSIL.GetType(thisReference).__PublicInterface__.prototype[signature.methodKey];
  });
JSIL.MakeInterface(
  "System.Collections.Generic.ICollection`1", true, ["T"], function($) {
    $.Method({}, "get_Count", (new JSIL.MethodSignature($.Int32, [], [])));
    $.Method({}, "get_IsReadOnly", (new JSIL.MethodSignature($.Boolean, [], [])));
    $.Method({}, "Add", (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("T", "System.Collections.Generic.ICollection`1")], [])));
    $.Method({}, "Clear", (JSIL.MethodSignature.Void));
    $.Method({}, "Contains", (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("T", "System.Collections.Generic.ICollection`1")], [])));
    $.Method({}, "CopyTo", (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [new JSIL.GenericParameter("T", "System.Collections.Generic.ICollection`1")]), $.Int32], [])));
    $.Method({}, "Remove", (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("T", "System.Collections.Generic.ICollection`1")], [])));
    $.Property({}, "Count");
    $.Property({}, "IsReadOnly");
  }, [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.ICollection`1")]), $jsilcore.TypeRef("System.Collections.IEnumerable")],
  function (expression, type) {
    if (expression === null) {
      return false;
    }
    var expressionType = JSIL.GetType(expression);
    if (expressionType.__IsArray__ && !expressionType.__Dimensions__) {
      return $jsilcore.System.Array.Of(type.T).$Is(expression);
    }
    return JSIL.$TypeAssignableFromTypeId(expressionType.__TypeId__, type);
  },
  function(interfaceTypeObject, signature, thisReference) {
    var type = JSIL.GetType(thisReference);
    if (type.__IsArray__)
      return type.__PublicInterface__.prototype[signature.LookupVariantMethodKey(type.__PublicInterface__.prototype)];
    return type.__PublicInterface__.prototype[signature.methodKey];
  });
JSIL.MakeInterface(
  "System.Collections.Generic.IList`1", true, ["T"], function($) {
    $.Method({}, "get_Item", (new JSIL.MethodSignature(new JSIL.GenericParameter("T", "System.Collections.Generic.IList`1"), [$.Int32], [])));
    $.Method({}, "set_Item", (new JSIL.MethodSignature(null, [$.Int32, new JSIL.GenericParameter("T", "System.Collections.Generic.IList`1")], [])));
    $.Method({}, "IndexOf", (new JSIL.MethodSignature($.Int32, [new JSIL.GenericParameter("T", "System.Collections.Generic.IList`1")], [])));
    $.Method({}, "Insert", (new JSIL.MethodSignature(null, [$.Int32, new JSIL.GenericParameter("T", "System.Collections.Generic.IList`1")], [])));
    $.Method({}, "RemoveAt", (new JSIL.MethodSignature(null, [$.Int32], [])));
    $.Property({}, "Item");
  },
  [
    $jsilcore.TypeRef("System.Collections.Generic.ICollection`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.IList`1")]),
    $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [new JSIL.GenericParameter("T", "System.Collections.Generic.IList`1")]),
    $jsilcore.TypeRef("System.Collections.IEnumerable")
  ],
  function (expression, type) {
    if (expression === null) {
      return false;
    }
    var expressionType = JSIL.GetType(expression);
    if (expressionType.__IsArray__ && !expressionType.__Dimensions__) {
      return $jsilcore.System.Array.Of(type.T).$Is(expression);
    }
    return JSIL.$TypeAssignableFromTypeId(expressionType.__TypeId__, type);
  },
  function(interfaceTypeObject, signature, thisReference) {
    var type = JSIL.GetType(thisReference);
    if (type.__IsArray__)
      return type.__PublicInterface__.prototype[signature.LookupVariantMethodKey(type.__PublicInterface__.prototype)];
    return type.__PublicInterface__.prototype[signature.methodKey];
  });

JSIL.ImplementExternals("System.Array", function ($) {
  $.RawMethod(true, "CheckType", JSIL.IsSystemArray);

  $.RawMethod(true, "Of", function Array_Of() {
    // Ensure System.Array is initialized.
    var _unused = $jsilcore.System.Array.Of;

    return $jsilcore.ArrayOf.apply(null, arguments);
  });
});

JSIL.MakeClass("System.Object", "System.Array", true, [], function ($) {
  $.SetValue("__IsArray__", true);

  $.RawMethod(false, "GetLength", function (dimension) {
    if (!JSIL.IsArray(this)) {
      return this.DimensionLength[dimension];
    }

    return this.length;
  });
  $.RawMethod(false, "GetLowerBound", function (dimension) {
    if (!JSIL.IsArray(this)) {
      return this.LowerBounds[dimension];
    }

    return 0;
  });
  $.RawMethod(false, "GetUpperBound", function (dimension) {
    if (!JSIL.IsArray(this)) {
      return this.LowerBounds[dimension] + this.DimensionLength[dimension] - 1;
    }

    return this.length - 1;
  });

  var types = {};

  var checkType = function Array_CheckType(value) {
    return JSIL.IsSystemArray(value);
  };

  $.RawMethod(true, "CheckType", checkType);

  var createVectorType = function (elementType) {
    var _ = JSIL.ResolveTypeReference(elementType);
    var elementTypePublicInterface = _[0];
    var elementTypeObject = _[1];

    var name, assembly;
    if (elementTypeObject.GetType || false) {
      name = elementTypeObject.get_FullName() + "[]";
      assembly = elementTypeObject.get_Assembly().__PublicInterface__;
    } else {
      name = "System.ArrayOneDZeroBased" + elementTypeObject.__TypeId__;
      assembly = $jsilcore;
    }


    JSIL.MakeType(
    {
      BaseType: $jsilcore.TypeRef("System.Array"),
      Name: name,
      GenericParameters: [],
      IsReferenceType: true,
      IsPublic: true,
      ConstructorAcceptsManyArguments: true,
      Assembly: assembly,
      $TypeId: $jsilcore.System.Array.__TypeId__ + "[" + elementTypeObject.__TypeId__ + "]"
    }, function ($) {
      $.SetValue("__IsArray__", true);
      $.SetValue("__ElementType__", elementTypeObject);

      $.Method({ Static: false, Public: true }, "GetEnumerator",
          new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [elementType]), [], []),
          function () {
            return JSIL.GetEnumerator(this, elementType);
          }
        )
        .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

      $.Method({ Static: false, Public: true }, "CopyTo",
        new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [elementType]), $.Int32], []),
        function CopyTo(array, arrayIndex) {
          JSIL.Array.CopyTo(this, array, arrayIndex);
        }
      );

      $.Method({ Static: false, Public: true }, "get_Item",
        new JSIL.MethodSignature(elementType, [$.Int32], []),
        function get_Item(index) {
          return this[index];
        }
      );

      $.Method({ Static: false, Public: true }, "set_Item",
        new JSIL.MethodSignature(null, [$.Int32, elementType], []),
        function set_Item(index, value) {
          this[index] = value;
        }
      );

      $.Method({ Static: false, Public: true }, "Contains",
        new JSIL.MethodSignature($.Boolean, [elementType], []),
        function Contains(value) {
          return JSIL.Array.IndexOf(this, 0, this.length, value) >= 0;
        }
      );

      $.Method({ Static: false, Public: true }, "IndexOf",
        new JSIL.MethodSignature($.Int32, [elementType], []),
        function IndexOf(value) {
          return JSIL.Array.IndexOf(this, 0, this.length, value);
        }
      );

      $.RawMethod(true, "CheckType",
        function (value) {
          if (value === null)
            return false;
          var type = JSIL.GetType(value);
          return type.__IsArray__ && !type.__Dimensions__
            && ((type.__ElementType__.__TypeId__ === this.__ElementType__.__TypeId__)
              || (type.__ElementType__.__IsReferenceType__ && this.__ElementType__.__AssignableFromTypes__[type.__ElementType__.__TypeId__]));
        }
      );

      $.ImplementInterfaces(
        $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [elementTypeObject]),
        $jsilcore.TypeRef("System.Collections.Generic.ICollection`1", [elementTypeObject]),
        $jsilcore.TypeRef("System.Collections.Generic.IList`1", [elementTypeObject])
      );
    });

    var publicInterface = assembly.TypeRef(name).get();
    if (!elementTypeObject.get_Assembly || !elementTypeObject.__IsClosed__) {
      publicInterface.__Type__.__IsClosed__ = false;
    }

    return publicInterface;
  }

  var createArrayType = function (elementType, size) {
    var _ = JSIL.ResolveTypeReference(elementType);
    var elementTypePublicInterface = _[0];
    var elementTypeObject = _[1];

    var name, assembly;
    if (elementTypeObject.GetType || false) {
      name = elementTypeObject.get_FullName() + "[" + (size.__Dimensions__ === 1 ? "*" : Array(size.__Dimensions__).join(",")) + "]";
      assembly = elementTypeObject.get_Assembly().__PublicInterface__;
    } else {
      name = "System.Array" + size.__Dimensions__ + "D" + elementTypeObject.__TypeId__;
      assembly = $jsilcore;
    }

    JSIL.MakeType(
    {
      BaseType: $jsilcore.TypeRef("System.Array"),
      Name: name,
      GenericParameters: [],
      IsReferenceType: true,
      IsPublic: true,
      ConstructorAcceptsManyArguments: true,
      Assembly: assembly,
      $TypeId: $jsilcore.System.Array.__TypeId__ + "[" + elementTypeObject.__TypeId__ + "," + size.__TypeId__ + "]"
    }, function ($) {
      $.SetValue("__IsArray__", true);
      $.SetValue("__ElementType__", elementTypeObject);
      $.SetValue("__Dimensions__", size.__Dimensions__);

      var shortCtorArgs = [];
      var longCtorArgs = [];
      for (var i = 0; i < size.__Dimensions__; i++) {
        shortCtorArgs.push($.Int32);
        longCtorArgs.push($.Int32);
        longCtorArgs.push($.Int32);
      }

      $.Method({ Static: false, Public: true }, ".ctor",
        new JSIL.MethodSignature(null, longCtorArgs, [], $jsilcore),
        function () {
          if (arguments.length < 2)
            throw new Error("Must have at least two dimensions: " + String(arguments));

          var lowerBounds = JSIL.Array.New($jsilcore.System.Int32, arguments.length / 2);
          var dWeight = JSIL.Array.New($jsilcore.System.Int32, arguments.length / 2);
          var dimensionLength = JSIL.Array.New($jsilcore.System.Int32, arguments.length / 2);
          var currentWeight = 1;
          for (var i = (arguments.length / 2) - 1; i >= 0; i--) {
            lowerBounds[i] = arguments[2 * i];
            dimensionLength[i] = arguments[2 * i + 1];
            dWeight[i] = currentWeight;
            currentWeight *= dimensionLength[i];
          }

          var items = JSIL.Array.New(elementTypeObject, currentWeight);

          JSIL.SetValueProperty(this, "LowerBounds", lowerBounds);
          JSIL.SetValueProperty(this, "DimensionLength", dimensionLength);
          JSIL.SetValueProperty(this, "DWeight", dWeight);
          JSIL.SetValueProperty(this, "Items", items);
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
        new JSIL.MethodSignature(null, shortCtorArgs, [], $jsilcore),
        function () {
          if (arguments.length < 1)
            throw new Error("Must have at least one dimension: " + String(arguments));

          var lowerBounds = JSIL.Array.New($jsilcore.System.Int32, arguments.length);
          var dWeight = JSIL.Array.New($jsilcore.System.Int32, arguments.length);
          var dimensionLength = JSIL.Array.New($jsilcore.System.Int32, arguments.length);
          var currentWeight = 1;
          for (var i = arguments.length - 1; i >= 0; i--) {
            lowerBounds[i] = arguments[i];
            dimensionLength[i] = 0;
            dWeight[i] = currentWeight;
            currentWeight *= dimensionLength[i];
          }

          var items = JSIL.Array.New(elementTypeObject, currentWeight);

          JSIL.SetValueProperty(this, "LowerBounds", lowerBounds);
          JSIL.SetValueProperty(this, "DimensionLength", dimensionLength);
          JSIL.SetValueProperty(this, "DWeight", dWeight);
          JSIL.SetValueProperty(this, "Items", items);
        }
      );

      $.RawMethod(false, "GetReference",
        function GetReference() {
          var index = 0;

          for (var i = this.LowerBounds.length - 1; i >= 0; i--)
            index += (arguments[i] - this.LowerBounds[i]) * this.DWeight[i];

          return new JSIL.MemberReference(this.Items, index);
        }
      );

      $.Method({ Static: false, Public: true }, "Get",
        new JSIL.MethodSignature(elementTypeObject, shortCtorArgs, []),
        function Get() {
          var index = 0;

          for (var i = this.LowerBounds.length - 1; i >= 0; i--)
            index += (arguments[i] - this.LowerBounds[i]) * this.DWeight[i];

          return this.Items[index];
        }
      );

      $.Method({ Static: false, Public: true }, "Set",
        new JSIL.MethodSignature(null, shortCtorArgs.concat(elementTypeObject), []),
        function Set() {
          var index = 0;

          for (var i = this.LowerBounds.length - 1; i >= 0; i--)
            index += (arguments[i] - this.LowerBounds[i]) * this.DWeight[i];

          return this.Items[index] = arguments[arguments.length - 1];
        }
      );

      $.Method({ Static: false, Public: true }, "get_length",
        JSIL.MethodSignature.Return($.Int32),
        function get_length() {
          return this.Items.length;
        }
      );

      $.Method({ Static: false, Public: true }, "get_Length",
        JSIL.MethodSignature.Return($.Int32),
        function get_Length() {
          return this.Items.length;
        }
      );

      $.Property({ Static: false, Public: true }, "length", $.Int32);
      $.Property({ Static: false, Public: true }, "Length", $.Int32);

      $.RawMethod(true, "CheckType",
        function (value) {
          if (value === null)
            return false;
          var type = JSIL.GetType(value);
          return type.__IsArray__ && type.__Dimensions__ === this.__Dimensions__
            && ((type.__ElementType__.__TypeId__ === this.__ElementType__.__TypeId__)
              || (type.__ElementType__.__IsReferenceType__ && this.__ElementType__.__AssignableFromTypes__[type.__ElementType__.__TypeId__]));
        });
    });

    var publicInterface = assembly.TypeRef(name).get();
    if (!elementTypeObject.get_Assembly || !elementTypeObject.__IsClosed__) {
      publicInterface.__Type__.__IsClosed__ = false;
    }

    return publicInterface;
  }

  var of = function Array_Of(elementType, dimensions) {
    if (typeof (elementType) === "undefined")
      JSIL.RuntimeError("Attempting to create an array of an undefined type");

    var _ = JSIL.ResolveTypeReference(elementType);
    var elementTypeObject = _[1];

    if (typeof (elementTypeObject.__TypeId__) === "undefined")
      JSIL.RuntimeError("Element type missing type ID");

    var key;
    var creator;

    if (dimensions || false) {
      if (typeof (dimensions.__TypeId__) === "undefined")
        JSIL.RuntimeError("Dimensions arg missing type ID");

      key = elementTypeObject.__TypeId__ + "," + dimensions.__TypeId__;
      creator = createArrayType;
    } else {
      key = elementTypeObject.__TypeId__.toString();
      creator = createVectorType;
    }

    var compositePublicInterface = types[key];

    if (typeof (compositePublicInterface) === "undefined") {
      compositePublicInterface = creator(elementType, dimensions);

      types[key] = compositePublicInterface;
      JSIL.InitializeType(compositePublicInterface);
      if (compositePublicInterface.__Type__.__IsClosed__)
        JSIL.RunStaticConstructors(compositePublicInterface, compositePublicInterface.__Type__);
    }

    return compositePublicInterface;
  };

  $jsilcore.ArrayOf = of;

  $.RawMethod(true, "Of$NoInitialize", of);
  $.RawMethod(true, "Of", of);

  $.RawMethod(true, "CheckType",
    function(value) {
      return value !== null && JSIL.GetType(value).__IsArray__;
    }
  );

  $.ImplementInterfaces(
    $jsilcore.TypeRef("System.Collections.IEnumerable"),
    $jsilcore.TypeRef("System.Collections.ICollection"),
    $jsilcore.TypeRef("System.Collections.IList")
  );
});

JSIL.ImplementExternals(
  "System.Array", function ($) {
    $.Method({ Static: true, Public: true }, "Resize",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", [$jsilcore.TypeRef("System.Array", ["!!0"])]), $.Int32], ["T"]),
      function (type, arr, newSize) {
        var oldArray = arr.get(), newArray = null;
        var oldLength = oldArray.length;

        if (Array.isArray(oldArray)) {
          newArray = oldArray;
          newArray.length = newSize;

          for (var i = oldLength; i < newSize; i++)
            newArray[i] = JSIL.DefaultValue(type);
        } else {
          newArray = JSIL.Array.New(type, newSize);

          JSIL.Array.CopyTo(oldArray, newArray, 0);
        }

        arr.set(newArray);
      }
    );

    $.Method({ Static: false, Public: false }, null,
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.IEnumerator"), [], []),
        function () {
          return JSIL.GetEnumerator(this, this.__ElementType__);
        }
      )
      .Overrides("System.Collections.IEnumerable", "GetEnumerator");

    // FIXME: Implement actual members of IList.

    $.Method({ Static: false, Public: true }, "get_Count",
      new JSIL.MethodSignature($.Int32, [], []),
      function get_Count() {
        return this.length;
      }
    );
  }
);

JSIL.MakeStaticClass("JSIL.MultidimensionalArray", true, [], function ($) {
  $.RawMethod(true, "New",
    function (type, dimensions, initializer) {
      var arrayType = new $jsilcore.System.Array.Of(type, JSIL.ArrayDimensionParameter(dimensions.length / 2));
      var ctorArgs = [];
      for (var i = 0; i < dimensions.length; i++) {
        ctorArgs.push($jsilcore.TypeRef("System.Int32"));
      }
      var ctorSignature = new JSIL.ConstructorSignature(arrayType, ctorArgs);
      var createdArray = ctorSignature.Construct.apply(ctorSignature, dimensions);
      if (JSIL.IsArray(initializer)) {
        JSIL.Array.ShallowCopy(createdArray.Items, initializer);
      }

      return createdArray;
    }
  );

  $.RawMethod(true, "CreateInstance",
    function (type, dimensions) {
      if (dimensions.length === 1) {
        return JSIL.Array.New(type, dimensions[0]);
      }

      var sizeWithLowerBound = [];
      for (var i = 0; i < dimensions.length; i++) {
        sizeWithLowerBound.push(0);
        sizeWithLowerBound.push(dimensions[i]);
      }

      return JSIL.MultidimensionalArray.New(type, dimensions[0]);
    }
  );
});
JSIL.MakeClass($jsilcore.TypeRef("System.Object"), "System.Attribute", true, [], function ($) {
    $.Method({ Static: false, Public: true }, ".ctor",
      (JSIL.MethodSignature.Void),
      function () {
      }
    );
});

JSIL.MakeEnum(
  "System.TypeCode", true, {
      Empty: 0,
      Object: 1,
      DBNull: 2,
      Boolean: 3,
      Char: 4,
      SByte: 5,
      Byte: 6,
      Int16: 7,
      UInt16: 8,
      Int32: 9,
      UInt32: 10,
      Int64: 11,
      UInt64: 12,
      Single: 13,
      Double: 14,
      Decimal: 15,
      DateTime: 16,
      String: 18
  }, false
);
