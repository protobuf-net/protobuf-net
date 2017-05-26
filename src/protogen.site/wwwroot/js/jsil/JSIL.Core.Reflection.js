/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core is required");

if (!$jsilcore)
  throw new Error("JSIL.Core is required");

JSIL.ReflectionGetTypeInternal = function (thisAssembly, name, throwOnFail, ignoreCase, onlySpecificAssembly) {
  var parsed = JSIL.ParseTypeName(name);

  var result = JSIL.GetTypeInternal(parsed, thisAssembly, false);

  // HACK: Emulate fallback to global namespace search.
  if (!result && !onlySpecificAssembly) {
    result = JSIL.GetTypeInternal(parsed, JSIL.GlobalNamespace, false);
  }

  if (!result) {
    if (throwOnFail)
      throw new System.TypeLoadException("The type '" + name + "' could not be found in the assembly '" + thisAssembly.toString() + "' or in the global namespace.");
    else
      return null;
  }

  return result;
};
$jsilcore.$MethodGetParameters = function (method) {
  var result = method._cachedParameters;

  if (typeof (result) === "undefined") {
    result = method._cachedParameters = JSIL.Array.New($jsilcore.System.Reflection.ParameterInfo, 0);
    method.InitResolvedSignature();

    var argumentTypes = method._data.resolvedSignature.argumentTypes;
    var parameterInfos = method._data.parameterInfo;
    var tParameterInfo = $jsilcore.System.Reflection.RuntimeParameterInfo.__Type__;

    if (argumentTypes) {
      for (var i = 0; i < argumentTypes.length; i++) {
        var parameterInfo = parameterInfos[i] || null;

        // FIXME: Missing non-type information
        var pi = JSIL.CreateInstanceOfType(tParameterInfo, "$fromArgumentTypeAndPosition", [argumentTypes[i], i]);
        if (parameterInfo)
          pi.$populateWithParameterInfo(parameterInfo);

        result.push(pi);
      }
    }
  }

  return result;
};

$jsilcore.$MethodGetParameterTypes = function (method) {
  var signature = method._data.signature;
  var argumentTypes = signature.argumentTypes;
  var result = JSIL.Array.New($jsilcore.System.Type, 0);

  for (var i = 0, l = argumentTypes.length; i < l; i++) {
    var argumentType = argumentTypes[i];
    result.push(signature.ResolveTypeReference(argumentType)[1]);
  }

  return result;
};

$jsilcore.$MethodGetReturnType = function (method) {
  if (!method._data.signature.returnType)
    return $jsilcore.System.Void.__Type__;
  method.InitResolvedSignature();
  return method._data.resolvedSignature.returnType;
};

$jsilcore.$MemberInfoGetName = function (memberInfo) {
  return memberInfo._descriptor.Name;
};

$jsilcore.$ParameterInfoGetParameterType = function (parameterInfo) {
  return parameterInfo.argumentType;
};

JSIL.ImplementExternals(
  "System.Type", function ($) {
    var typeReference = $jsilcore.TypeRef("System.Type");
    var memberArray = new JSIL.TypeRef($jsilcore, "System.Array", ["System.Reflection.MemberInfo"]);
    var fieldArray = new JSIL.TypeRef($jsilcore, "System.Array", ["System.Reflection.FieldInfo"]);
    var propertyArray = new JSIL.TypeRef($jsilcore, "System.Array", ["System.Reflection.PropertyInfo"]);
    var methodArray = new JSIL.TypeRef($jsilcore, "System.Array", ["System.Reflection.MethodInfo"]);
    var constructorArray = new JSIL.TypeRef($jsilcore, "System.Array", ["System.Reflection.ConstructorInfo"]);
    var eventArray = new JSIL.TypeRef($jsilcore, "System.Array", ["System.Reflection.EventInfo"]);
    var typeArray = new JSIL.TypeRef($jsilcore, "System.Array", ["System.Type"]);

    $.Method({ Public: true, Static: true }, "op_Equality",
      new JSIL.MethodSignature($.Boolean, [$.Type, $.Type]),
      function (lhs, rhs) {
        if (lhs === rhs)
          return true;

        return String(lhs) == String(rhs);
      }
    );

    $.Method({ Public: true, Static: true }, "op_Inequality",
      new JSIL.MethodSignature($.Boolean, [$.Type, $.Type]),
      function (lhs, rhs) {
        if (lhs !== rhs)
          return true;

        return String(lhs) != String(rhs);
      }
    );

    $.Method({ Static: false, Public: true }, "get_DeclaringType",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], []),
      function () {
        var lastPlusIndex = this.FullName.lastIndexOf("+");
        if (lastPlusIndex < 0) {
          return null;
        }
        return this.Assembly.GetType(this.FullName.substring(0, lastPlusIndex));
      }
    );

    $.Method({ Static: false, Public: true }, "get_IsGenericType",
      new JSIL.MethodSignature($.Boolean, []),
      JSIL.TypeObjectPrototype.get_IsGenericType
    );

    $.Method({ Static: false, Public: true }, "get_IsGenericTypeDefinition",
      new JSIL.MethodSignature($.Boolean, []),
      JSIL.TypeObjectPrototype.get_IsGenericTypeDefinition
    );

    $.Method({ Static: false, Public: true }, "get_ContainsGenericParameters",
      new JSIL.MethodSignature($.Boolean, []),
      JSIL.TypeObjectPrototype.get_ContainsGenericParameters
    );

    $.Method({ Static: false, Public: true }, "GetGenericTypeDefinition",
      (new JSIL.MethodSignature($.Type, [], [])),
      function () {
        if (this.get_IsGenericType() === false)
          throw new System.Exception("The current type is not a generic type.");
        return this.__OpenType__ || this;
      }
    );

    $.Method({ Static: false, Public: true }, "GetGenericArguments",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Type]), [], [])),
      function GetGenericArguments() {
        return JSIL.Array.New(typeReference.get(), this.__GenericArgumentValues__);
      }
    );

    $.Method({ Static: false, Public: true }, "MakeGenericType",
      (new JSIL.MethodSignature($.Type, [$jsilcore.TypeRef("System.Array", [$.Type])], [])),
      function (typeArguments) {
        return this.__PublicInterface__.Of.apply(this.__PublicInterface__, typeArguments).__Type__;
      }
    );

    $.Method({ Static: false, Public: true }, "get_IsArray",
      new JSIL.MethodSignature($.Boolean, []),
      JSIL.TypeObjectPrototype.get_IsArray
    );

    $.Method({ Public: true, Static: false }, "get_IsValueType",
      new JSIL.MethodSignature($.Boolean, []),
      JSIL.TypeObjectPrototype.get_IsValueType
    );

    $.Method({ Public: true, Static: false }, "get_IsEnum",
      new JSIL.MethodSignature($.Boolean, []),
      JSIL.TypeObjectPrototype.get_IsEnum
    );

    $.Method({ Static: false, Public: true }, "GetElementType",
      new JSIL.MethodSignature($.Type, []),
      function () {
        return this.__ElementType__;
      }
    );

    $.Method({ Public: true, Static: false }, "get_BaseType",
      new JSIL.MethodSignature($.Type, []),
      JSIL.TypeObjectPrototype.get_BaseType
    );

    $.Method({ Public: true, Static: false }, "get_Name",
      new JSIL.MethodSignature($.String, []),
      JSIL.TypeObjectPrototype.get_Name
    );

    $.Method({ Public: true, Static: false }, "get_FullName",
      new JSIL.MethodSignature($.String, []),
      JSIL.TypeObjectPrototype.get_FullName
    );

    $.Method({ Public: true, Static: false }, "get_Assembly",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.Assembly"), []),
      JSIL.TypeObjectPrototype.get_Assembly
    );

    $.Method({ Public: true, Static: false }, "get_Namespace",
      new JSIL.MethodSignature($.String, []),
      JSIL.TypeObjectPrototype.get_Namespace
    );

    $.Method({ Public: true, Static: false }, "get_AssemblyQualifiedName",
      new JSIL.MethodSignature($.String, []),
      JSIL.TypeObjectPrototype.get_AssemblyQualifiedName
    );

    $.Method({ Public: true, Static: false }, "get_IsClass",
      new JSIL.MethodSignature($.Boolean, []),
      function () {
        return this === $jsilcore.System.Object.__Type__ || this.get_BaseType() !== null;
      }
    );

    $.Method({ Public: true, Static: false }, "toString",
      new JSIL.MethodSignature($.String, []),
      function () {
        return this.__FullName__;
      }
    );

    $.Method({ Public: true, Static: false }, "IsSubclassOf",
      new JSIL.MethodSignature($.Boolean, [$.Type]),
      function (type) {
        var needle = type.__PublicInterface__.prototype;
        var haystack = this.__PublicInterface__.prototype;
        return JSIL.CheckDerivation(haystack, needle);
      }
    );

    $.Method({ Public: true, Static: false }, "IsAssignableFrom",
      new JSIL.MethodSignature($.Boolean, [$.Type]),
      function (type) {
        if (type === this)
          return true;

        if (this._IsAssignableFrom)
          return this._IsAssignableFrom.call(this, type);
        else
          return false;
      }
    );

    $.Method({ Public: true, Static: false }, "IsInstanceOfType",
      new JSIL.MethodSignature($.Boolean, [$.Object]),
      function (obj) {
        if (obj === null)
          return false;

        return this.IsAssignableFrom(JSIL.GetType(obj));
      }
    );

    $.Method({ Public: true, Static: false }, "GetMembers",
      new JSIL.MethodSignature(memberArray, []),
      function () {
        return JSIL.GetMembersInternal(
          this,
          defaultFlags(),
          null,
          null,
          true,
          $jsilcore.System.Array.Of($jsilcore.System.Reflection.MemberInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetMembers",
      new JSIL.MethodSignature(memberArray, [$jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (flags) {
        return JSIL.GetMembersInternal(
          this,
          flags,
          null,
          null,
          true,
          $jsilcore.System.Array.Of($jsilcore.System.Reflection.MemberInfo).__Type__
        );
      }
    );

    var getMatchingMethodsImpl = function (type, name, flags, argumentTypes, returnType, allMethods) {
      var methods = JSIL.GetMembersInternal(
        type, flags, allMethods ? "$AllMethods" : "MethodInfo", name
      );

      if (argumentTypes)
        JSIL.$FilterMethodsByArgumentTypes(methods, argumentTypes, returnType);

      JSIL.$ApplyMemberHiding(type, methods, type.__PublicInterface__.prototype);

      return methods;
    }

    var getMethodImpl = function (type, name, flags, argumentTypes) {
      var methods = getMatchingMethodsImpl(type, name, flags, argumentTypes);

      if (methods.length > 1) {
        throw new System.Exception("Multiple methods named '" + name + "' were found.");
      } else if (methods.length < 1) {
        return null;
      }

      return methods[0];
    };

    $.RawMethod(false, "$GetMatchingInstanceMethods", function (name, argumentTypes, returnType) {
      var bindingFlags = $jsilcore.BindingFlags;
      var flags = bindingFlags.Public | bindingFlags.NonPublic | bindingFlags.Instance;

      return getMatchingMethodsImpl(
        this, name, flags,
        argumentTypes, returnType, true
      );
    });

    $.Method({ Public: true, Static: false }, "GetMethod",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.String]),
      function (name) {
        return getMethodImpl(this, name, defaultFlags(), null);
      }
    );

    $.Method({ Public: true, Static: false }, "GetMethod",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.String, $jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (name, flags) {
        return getMethodImpl(this, name, flags, null);
      }
    );

    $.Method({ Public: true, Static: false }, "GetMethod",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.String, typeArray]),
      function (name, argumentTypes) {
        return getMethodImpl(this, name, defaultFlags(), argumentTypes);
      }
    );

    $.Method({ Public: true, Static: false }, "GetMethod",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.String, $jsilcore.TypeRef("System.Reflection.BindingFlags"), $jsilcore.TypeRef("System.Reflection.Binder"), typeArray, $jsilcore.TypeRef("System.Array", ["System.Reflection.ParameterModifier"])]),
      function (name, flags, binder, argumentTypes, modifiers) {
        if (binder !== null || modifiers !== null) {
          throw new System.NotImplementedException("Binder and ParameterModifier are not supported yet.");
        }
        return getMethodImpl(this, name, flags, argumentTypes);
      }
    );

    $.Method({ Public: true, Static: false }, "GetMethods",
      new JSIL.MethodSignature(methodArray, []),
      function () {
        return JSIL.GetMembersInternal(
          this,
          System.Reflection.BindingFlags.Instance |
          System.Reflection.BindingFlags.Static |
          System.Reflection.BindingFlags.Public,
          "MethodInfo",
          null,
          false,
          $jsilcore.System.Array.Of($jsilcore.System.Reflection.MethodInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetMethods",
      new JSIL.MethodSignature(methodArray, [$jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (flags) {
        return JSIL.GetMembersInternal(
          this, flags, "MethodInfo", null, false, $jsilcore.System.Array.Of($jsilcore.System.Reflection.MethodInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetEvents",
      new JSIL.MethodSignature(eventArray, []),
      function () {
        return JSIL.GetMembersInternal(
          this,
          System.Reflection.BindingFlags.Instance |
          System.Reflection.BindingFlags.Static |
          System.Reflection.BindingFlags.Public,
          "EventInfo",
          null,
          true,
          $jsilcore.System.Array.Of($jsilcore.System.Reflection.EventInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetEvents",
      new JSIL.MethodSignature(eventArray, [$jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (flags) {
        return JSIL.GetMembersInternal(
          this, flags, "EventInfo", null, true, $jsilcore.System.Array.Of($jsilcore.System.Reflection.EventInfo).__Type__
        );
      }
    );

    var getConstructorImpl = function (self, flags, argumentTypes) {
      var constructors = JSIL.GetMembersInternal(
        self, flags, "ConstructorInfo", null, true
      );

      JSIL.$FilterMethodsByArgumentTypes(constructors, argumentTypes);

      JSIL.$ApplyMemberHiding(self, constructors, self.__PublicInterface__.prototype);

      if (constructors.length > 1) {
        throw new System.Exception("Multiple constructors were found.");
      } else if (constructors.length < 1) {
        return null;
      }

      return constructors[0];
    };

    $.Method({ Public: true, Static: false }, "GetConstructor",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.ConstructorInfo"), [typeArray]),
      function (argumentTypes) {
        var flags =
          System.Reflection.BindingFlags.Instance |
          System.Reflection.BindingFlags.Static |
          System.Reflection.BindingFlags.Public |
          // FIXME: I think this is necessary to avoid pulling in inherited constructors,
          //  since calling the System.Object constructor to create an instance of String
          //  is totally insane.
          System.Reflection.BindingFlags.DeclaredOnly;
        return getConstructorImpl(this, flags, argumentTypes);
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "GetConstructor",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.ConstructorInfo"), [
          $jsilcore.TypeRef("System.Reflection.BindingFlags"), $jsilcore.TypeRef("System.Reflection.Binder"),
          $jsilcore.TypeRef("System.Reflection.CallingConventions"), $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Type")]),
          $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.ParameterModifier")])
      ], []),
      function GetConstructor(bindingAttr, binder, callConvention, types, modifiers) {
        return getConstructorImpl(this, bindingAttr | System.Reflection.BindingFlags.DeclaredOnly, types);
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "GetConstructor",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.ConstructorInfo"), [
          $jsilcore.TypeRef("System.Reflection.BindingFlags"), $jsilcore.TypeRef("System.Reflection.Binder"),
          $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Type")]), $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.ParameterModifier")])
      ], []),
      function GetConstructor(bindingAttr, binder, types, modifiers) {
        return getConstructorImpl(this, bindingAttr | System.Reflection.BindingFlags.DeclaredOnly, types);
      }
    );

    $.Method({ Public: true, Static: false }, "GetConstructors",
      new JSIL.MethodSignature(constructorArray, []),
      function () {
        return JSIL.GetMembersInternal(
          this,
          System.Reflection.BindingFlags.Instance |
          System.Reflection.BindingFlags.Static |
          System.Reflection.BindingFlags.Public |
          // FIXME: I think this is necessary to avoid pulling in inherited constructors,
          //  since calling the System.Object constructor to create an instance of String
          //  is totally insane.
          System.Reflection.BindingFlags.DeclaredOnly,
          "ConstructorInfo",
          null,
          false,
          $jsilcore.System.Array.Of($jsilcore.System.Reflection.ConstructorInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetConstructors",
      new JSIL.MethodSignature(methodArray, [$jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (flags) {
        return JSIL.GetMembersInternal(
          this, flags | System.Reflection.BindingFlags.DeclaredOnly, "ConstructorInfo", null, false, $jsilcore.System.Array.Of($jsilcore.System.Reflection.ConstructorInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetFields",
      new JSIL.MethodSignature(fieldArray, []),
      function () {
        return JSIL.GetMembersInternal(
          this,
          System.Reflection.BindingFlags.Instance |
          System.Reflection.BindingFlags.Static |
          System.Reflection.BindingFlags.Public,
          "FieldInfo",
          null,
          false,
          $jsilcore.System.Array.Of($jsilcore.System.Reflection.FieldInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetFields",
      new JSIL.MethodSignature(fieldArray, [$jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (flags) {
        return JSIL.GetMembersInternal(
          this, flags, "FieldInfo", null, false, $jsilcore.System.Array.Of($jsilcore.System.Reflection.FieldInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetProperties",
      new JSIL.MethodSignature(propertyArray, []),
      function () {
        return JSIL.GetMembersInternal(
          this,
          System.Reflection.BindingFlags.Instance |
          System.Reflection.BindingFlags.Static |
          System.Reflection.BindingFlags.Public,
          "PropertyInfo",
          null,
          true,
          $jsilcore.System.Array.Of($jsilcore.System.Reflection.PropertyInfo).__Type__
        );
      }
    );

    $.Method({ Public: true, Static: false }, "GetProperties",
      new JSIL.MethodSignature(propertyArray, [$jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (flags) {
        return JSIL.GetMembersInternal(
          this, flags, "PropertyInfo", null, true, $jsilcore.System.Array.Of($jsilcore.System.Reflection.PropertyInfo).__Type__
        );
      }
    );

    var getSingleFiltered = function (self, name, flags, type) {
      var members = JSIL.GetMembersInternal(self, flags, type, null, true);
      var result = null;

      for (var i = 0, l = members.length; i < l; i++) {
        var member = members[i];
        if ($jsilcore.$MemberInfoGetName(member) === name) {
          if (!result)
            result = member;
          else
            throw new System.Reflection.AmbiguousMatchException("Multiple matches found");
        }
      }

      return result;
    };

    var defaultFlags = function () {
      var bindingFlags = $jsilcore.BindingFlags;
      var result = bindingFlags.Public | bindingFlags.Instance | bindingFlags.Static;
      return result;
      // return System.Reflection.BindingFlags.$Flags("Public", "Instance", "Static");
    };

    $.Method({ Public: true, Static: false }, "GetField",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.FieldInfo"), [$.String]),
      function (name) {
        return getSingleFiltered(this, name, defaultFlags(), "FieldInfo");
      }
    );

    $.Method({ Public: true, Static: false }, "GetField",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.FieldInfo"), [$.String, $jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (name, flags) {
        return getSingleFiltered(this, name, flags, "FieldInfo");
      }
    );

    $.Method({ Public: true, Static: false }, "GetProperty",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.PropertyInfo"), [$.String]),
      function (name) {
        return getSingleFiltered(this, name, defaultFlags(), "PropertyInfo");
      }
    );

    $.Method({ Public: true, Static: false }, "GetProperty",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.PropertyInfo"), [$.String, $jsilcore.TypeRef("System.Reflection.BindingFlags")]),
      function (name, flags) {
        return getSingleFiltered(this, name, flags, "PropertyInfo");
      }
    );

    $.Method({ Public: true, Static: false }, "GetProperty",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.PropertyInfo"), [
        $.String,
        $jsilcore.TypeRef("System.Reflection.BindingFlags"),
        $jsilcore.TypeRef("System.Reflection.Binder"),
        $jsilcore.TypeRef("System.Type"),
        $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Type")]),
        $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.ParameterModifier")])]),
      function (name, flags) {
        //TODO: Implement it.
        return getSingleFiltered(this, name, flags, "PropertyInfo");
      }
    );

    $.Method({ Public: true, Static: false }, "get_IsGenericParameter",
      new JSIL.MethodSignature($.Type, []),
      JSIL.TypeObjectPrototype.get_IsGenericParameter
    );

    $.Method({ Public: true, Static: false }, "get_IsInterface",
      new JSIL.MethodSignature($.Type, []),
      JSIL.TypeObjectPrototype.get_IsInterface
    );

    $.Method({ Public: true, Static: false }, "get_IsByRef",
      new JSIL.MethodSignature($.Type, []),
      JSIL.TypeObjectPrototype.get_IsByRef
    );

    $.Method({ Public: true, Static: false }, "GetInterfaces",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Type]), []),
      function () {
        return JSIL.GetInterfacesImplementedByType(this, true, false, false, $jsilcore.System.Array.Of($jsilcore.System.Type).__Type__);
      }
    );

    var $T00 = function () {
      return ($T00 = JSIL.Memoize($jsilcore.System.Type))();
    };
    var $T01 = function () {
      return ($T01 = JSIL.Memoize($jsilcore.System.TypeCode))();
    };
    var $T02 = function () {
      return ($T02 = JSIL.Memoize($jsilcore.System.Boolean))();
    };
    var $T03 = function () {
      return ($T03 = JSIL.Memoize($jsilcore.System.Byte))();
    };
    var $T04 = function () {
      return ($T04 = JSIL.Memoize($jsilcore.System.Char))();
    };
    var $T05 = function () {
      return ($T05 = JSIL.Memoize($jsilcore.System.DateTime))();
    };
    var $T06 = function () {
      return ($T06 = JSIL.Memoize($jsilcore.System.Decimal))();
    };
    var $T07 = function () {
      return ($T07 = JSIL.Memoize($jsilcore.System.Double))();
    };
    var $T08 = function () {
      return ($T08 = JSIL.Memoize($jsilcore.System.Int16))();
    };
    var $T09 = function () {
      return ($T09 = JSIL.Memoize($jsilcore.System.Int32))();
    };
    var $T0A = function () {
      return ($T0A = JSIL.Memoize($jsilcore.System.Int64))();
    };
    var $T0B = function () {
      return ($T0B = JSIL.Memoize($jsilcore.System.SByte))();
    };
    var $T0C = function () {
      return ($T0C = JSIL.Memoize($jsilcore.System.Single))();
    };
    var $T0D = function () {
      return ($T0D = JSIL.Memoize($jsilcore.System.String))();
    };
    var $T0E = function () {
      return ($T0E = JSIL.Memoize($jsilcore.System.UInt16))();
    };
    var $T0F = function () {
      return ($T0F = JSIL.Memoize($jsilcore.System.UInt32))();
    };
    var $T10 = function () {
      return ($T10 = JSIL.Memoize($jsilcore.System.UInt64))();
    };

    $.Method({ Static: true, Public: true }, "GetTypeCode",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.TypeCode"), [$jsilcore.TypeRef("System.Type")]),
      function Type_GetTypeCode(type) {
        if ($T00().op_Equality(type, null)) {
          var result = $T01().Empty;
        } else if ($T00().op_Equality(type, $T02().__Type__)) {
          result = $T01().Boolean;
        } else if ($T00().op_Equality(type, $T03().__Type__)) {
          result = $T01().Byte;
        } else if ($T00().op_Equality(type, $T04().__Type__)) {
          result = $T01().Char;
        } else if ($T00().op_Equality(type, $T05().__Type__)) {
          result = $T01().DateTime;
        } else if ($T00().op_Equality(type, $T06().__Type__)) {
          result = $T01().Decimal;
        } else if ($T00().op_Equality(type, $T07().__Type__)) {
          result = $T01().Double;
        } else if ($T00().op_Equality(type, $T08().__Type__)) {
          result = $T01().Int16;
        } else if (!(!$T00().op_Equality(type, $T09().__Type__) && !type.get_IsEnum())) {
          result = $T01().Int32;
        } else if ($T00().op_Equality(type, $T0A().__Type__)) {
          result = $T01().Int64;
        } else if ($T00().op_Equality(type, $T0B().__Type__)) {
          result = $T01().SByte;
        } else if ($T00().op_Equality(type, $T0C().__Type__)) {
          result = $T01().Single;
        } else if ($T00().op_Equality(type, $T0D().__Type__)) {
          result = $T01().String;
        } else if ($T00().op_Equality(type, $T0E().__Type__)) {
          result = $T01().UInt16;
        } else if ($T00().op_Equality(type, $T0F().__Type__)) {
          result = $T01().UInt32;
        } else if ($T00().op_Equality(type, $T10().__Type__)) {
          result = $T01().UInt64;
        } else {
          result = $T01().Object;
        }
        return result;
      }
    );

    $.Method({ Static: true, Public: true }, "get_EmptyTypes",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Type")]), []),
      function get_EmptyTypes() {
        return JSIL.Array.New($jsilcore.System.Type, 0);
      }
    );

  }
);

JSIL.MakeClass("System.Reflection.MemberInfo", "System.Type", true, [], function ($) {
  $.Property({ Public: true, Static: false, Virtual: true }, "Module");
  $.Property({ Public: true, Static: false, Virtual: true }, "Assembly");
  $.Property({ Public: true, Static: false, Virtual: true }, "FullName");
  $.Property({ Public: true, Static: false, Virtual: true }, "Namespace");
  $.Property({ Public: true, Static: false, Virtual: true }, "AssemblyQualifiedName");
  $.Property({ Public: true, Static: false, Virtual: true }, "BaseType");
  $.Property({ Public: true, Static: false, Virtual: true }, "IsGenericType");
  $.Property({ Public: true, Static: false, Virtual: true }, "IsGenericTypeDefinition");
  $.Property({ Public: true, Static: false, Virtual: true }, "ContainsGenericParameters");
  $.Property({ Public: true, Static: false }, "IsArray");
  $.Property({ Public: true, Static: false }, "IsValueType");
  $.Property({ Public: true, Static: false }, "IsEnum");
  $.Property({ Public: true, Static: false }, "IsClass");

  // HACK - it should really be field.
  $.Property({ Public: true, Static: true }, "EmptyTypes");
});
﻿$jsilcore.MemberInfoExternals = function ($) {
  $.Method({ Static: false, Public: true }, "get_DeclaringType",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], []),
    function () {
      return this._typeObject;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Name",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.String"), [], []),
    function () {
      return $jsilcore.$MemberInfoGetName(this);
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsSpecialName",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [], []),
    function () {
      return this._descriptor.SpecialName === true;
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsPublic",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [], []),
    function () {
      return this._descriptor.Public;
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsPrivate",
  new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [], []),
  function () {
    return !this._descriptor.Public;
  }
);

  $.Method({ Static: false, Public: true }, "get_IsStatic",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [], []),
    function () {
      return this._descriptor.Static;
    }
  );

  $.Method({ Static: false, Public: true }, "GetCustomAttributes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Object]), [$.Boolean], [])),
    function GetCustomAttributes(inherit) {
      return JSIL.GetMemberAttributes(this, inherit, null);
    }
  );

  $.Method({ Static: false, Public: true }, "GetCustomAttributes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Object]), [$jsilcore.TypeRef("System.Type"), $.Boolean], [])),
    function GetCustomAttributes(attributeType, inherit) {
      return JSIL.GetMemberAttributes(this, inherit, attributeType);
    }
  );

  $.Method({ Static: false, Public: true }, "IsDefined",
    (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Type"), $.Boolean], [])),
    function GetCustomAttributes(attributeType, inherit) {
      return JSIL.GetMemberAttributes(this, inherit, attributeType).length > 0;
    }
  );

  $.Method({ Static: false, Public: true }, "GetCustomAttributesData",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IList`1", [$jsilcore.TypeRef("System.Reflection.CustomAttributeData")]), [], [])),
    function GetCustomAttributesData() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Public: true, Static: true }, "op_Equality",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.MemberInfo"), $jsilcore.TypeRef("System.Reflection.MemberInfo")]),
    JSIL.ObjectEquals
  );

  $.Method({ Public: true, Static: true }, "op_Inequality",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.MemberInfo"), $jsilcore.TypeRef("System.Reflection.MemberInfo")]),
    function (lhs, rhs) {
      return !JSIL.ObjectEquals(lhs, rhs);
    }
  );
};


JSIL.ImplementExternals(
  "System.Reflection.MemberInfo", $jsilcore.MemberInfoExternals
);

JSIL.MakeClass("System.Object", "System.Reflection.MemberInfo", true, [], function ($) {
  $.Property({ Public: true, Static: false, Virtual: true }, "DeclaringType");
  $.Property({ Public: true, Static: false, Virtual: true }, "Name");
  $.Property({ Public: true, Static: false, Virtual: true }, "IsPublic");
  $.Property({ Public: true, Static: false, Virtual: true }, "IsStatic");
  $.Property({ Public: true, Static: false, Virtual: true }, "IsSpecialName");
  $.Property({ Public: true, Static: false, Virtual: true }, "MemberType");

  $.ImplementInterfaces(
    $jsilcore.TypeRef("System.Reflection.ICustomAttributeProvider")
  );
});
JSIL.ImplementExternals("System.Reflection.PropertyInfo", function ($) {
  var getGetMethodImpl = function (nonPublic) {
    var methodName = "get_" + this.get_Name();
    var bf = System.Reflection.BindingFlags;
    var instanceOrStatic = this.get_IsStatic() ? "Static" : "Instance";
    var bindingFlags = (nonPublic
      ? bf.$Flags("DeclaredOnly", instanceOrStatic, "Public", "NonPublic")
      : bf.$Flags("DeclaredOnly", instanceOrStatic, "Public")
    );
    return this.get_DeclaringType().GetMethod(methodName, bindingFlags);
  };

  var getValueImpl = function(obj, index) {
    return getGetMethodImpl.call(this, true).Invoke(obj, index);
  };

  var getSetMethodImpl = function(nonPublic) {
    var methodName = "set_" + this.get_Name();
    var bf = System.Reflection.BindingFlags;
    var instanceOrStatic = this.get_IsStatic() ? "Static" : "Instance";
    var bindingFlags = (nonPublic
      ? bf.$Flags("DeclaredOnly", instanceOrStatic, "Public", "NonPublic")
      : bf.$Flags("DeclaredOnly", instanceOrStatic, "Public")
    );
    return this.get_DeclaringType().GetMethod(methodName, bindingFlags);
  };

  var setValueImpl = function(obj, value, index) {
    return getSetMethodImpl.call(this, true).Invoke(obj, index !== null ? Array.prototype.concat(index, value) : [value]);
  };

  var getAccessorsImpl = function (nonPublic) {
    var result = [];

    var getMethod = this.GetGetMethod(nonPublic || false);
    var setMethod = this.GetSetMethod(nonPublic || false);

    if (getMethod)
      result.push(getMethod);
    if (setMethod)
      result.push(setMethod);

    return result;
  };

  $.Method({ Static: false, Public: true }, "GetGetMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [], [])),
    getGetMethodImpl
  );

  $.Method({ Static: false, Public: true }, "GetGetMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.Boolean], [])),
    getGetMethodImpl
  );

  $.Method({ Static: false, Public: true }, "GetSetMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [], [])),
    getSetMethodImpl
  );

  $.Method({ Static: false, Public: true }, "GetSetMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.Boolean], [])),
    getSetMethodImpl
  );

  $.Method({ Static: false, Public: true }, "GetAccessors",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.MethodInfo")]), [$.Boolean], [])),
    getAccessorsImpl
  );

  $.Method({ Static: false, Public: true }, "GetAccessors",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.MethodInfo")]), [], [])),
    getAccessorsImpl
  );

  $.Method({ Static: false, Public: true }, "GetIndexParameters",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.ParameterInfo")]), [], [])),
    function GetIndexParameters() {
      var getMethod = this.GetGetMethod(true);
      if (getMethod)
        return getMethod.GetParameters();

      var setMethod = this.GetSetMethod(true);
      if (setMethod) {
        var parameters = setMethod.GetParameters();
        var result = JSIL.Array.New($jsilcore.System.Reflection.ParameterInfo, parameters.length - 1);
        for (var i = 0; i < result.length - 1; i++) {
          result[i] = parameters[i];
        }
        return result;
      }

      return JSIL.Array.New($jsilcore.System.Reflection.ParameterInfo, 0);
    }
  );

  $.Method({ Static: false, Public: true }, "GetValue",
    (new JSIL.MethodSignature($.Object, [$.Object], [], [])),
    function GetValue(obj) {
      return getValueImpl.call(this, obj, null);
    }
  );

  $.Method({ Static: false, Public: true }, "GetValue",
    (new JSIL.MethodSignature($.Object, [$.Object, $jsilcore.TypeRef("System.Array", [$.Object])], [], [])),
    getValueImpl
  );

  $.Method({ Static: false, Public: true }, "SetValue",
    (new JSIL.MethodSignature($.Object, [$.Object, $.Object], [], [])),
    function GetValue(obj, value) {
      return setValueImpl.call(this, obj, value, null);
    }
  );

  $.Method({ Static: false, Public: true }, "SetValue",
    (new JSIL.MethodSignature($.Object, [$.Object, $.Object, $jsilcore.TypeRef("System.Array", [$.Object])], [], [])),
    setValueImpl
  );

  $.Method({ Static: false, Public: true }, "get_PropertyType",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], [])),
    function get_PropertyType() {
      var result = this._cachedPropertyType;

      if (!result) {
        var getMethod = this.GetGetMethod(true);
        if (getMethod) {
          result = getMethod.get_ReturnType();
        } else {
          var setMethod = this.GetSetMethod(true);
          if (setMethod) {
            var argumentTypes = setMethod._data.signature.argumentTypes;
            var lastArgumentType = argumentTypes[argumentTypes.length - 1];
            result = JSIL.ResolveTypeReference(lastArgumentType, this._typeObject.__Context__)[1];
          }
        }

        this._cachedPropertyType = result;
      }

      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "get_CanRead",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_CanRead() {
      return getGetMethodImpl.call(this, true) !== null;
    }
  );

  $.Method({ Static: false, Public: true }, "get_CanWrite",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_CanWrite() {
      return getSetMethodImpl.call(this, true) !== null;
    }
  );

  var equalsImpl = function (lhs, rhs) {
    if (lhs === rhs)
      return true;

    return JSIL.ObjectEquals(lhs, rhs);
  };

  $.Method({ Static: true, Public: true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.PropertyInfo"), $jsilcore.TypeRef("System.Reflection.PropertyInfo")], [])),
    function op_Equality(left, right) {
      return equalsImpl(left, right);
    }
  );

  $.Method({ Static: true, Public: true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.PropertyInfo"), $jsilcore.TypeRef("System.Reflection.PropertyInfo")], [])),
    function op_Inequality(left, right) {
      return !equalsImpl(left, right);
    }
  );
});

JSIL.MakeClass("System.Reflection.MemberInfo", "System.Reflection.PropertyInfo", true, [], function ($) {
  $.Property({ Public: true, Static: false, Virtual: true }, "MemberType");
  $.Method({ Public: true, Static: false, Virtual: true }, "get_MemberType",
    JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Reflection.MemberTypes")),
    function get_MemberType() {
      return $jsilcore.System.Reflection.MemberTypes.Property;
    }
  );
});
JSIL.ImplementExternals(
  "System.Reflection.FieldInfo", function ($) {
    $.Method({ Static: false, Public: true }, "get_FieldType",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], [])),
      function get_FieldType() {
        var result = this._cachedFieldType;

        if (typeof (result) === "undefined") {
          result = this._cachedFieldType = JSIL.ResolveTypeReference(
            this._data.fieldType, this._typeObject.__Context__
          )[1];
        }

        return result;
      }
    );

    $.Method({ Static: false, Public: true }, "get_IsInitOnly",
      (new JSIL.MethodSignature($.Boolean, [], [])),
      function get_IsInitOnly() {
        return this._descriptor.IsReadOnly;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "GetRawConstantValue",
      new JSIL.MethodSignature($.Object, [], []),
      function GetRawConstantValue() {
        return this._data.constant;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "GetValue",
      (new JSIL.MethodSignature($.Object, [$.Object], [])),
      function GetValue(obj) {
        if (this.IsStatic) {
          return this.DeclaringType.__PublicInterface__[this._descriptor.Name];
        }

        if (obj === null) {
          throw new System.Exception("Non-static field requires a target.");
        }

        if (!this.DeclaringType.IsAssignableFrom(obj.__ThisType__)) {
          throw new System.Exception("Field is not defined on the target object.");
        }

        return obj[this._descriptor.Name];
      }
    );

    var equalsImpl = function (lhs, rhs) {
      if (lhs === rhs)
        return true;

      return JSIL.ObjectEquals(lhs, rhs);
    };

    $.Method({ Static: true, Public: true }, "op_Equality",
      (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.FieldInfo"), $jsilcore.TypeRef("System.Reflection.FieldInfo")], [])),
      function op_Equality(left, right) {
        return equalsImpl(left, right);
      }
    );

    $.Method({ Static: true, Public: true }, "op_Inequality",
      (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.FieldInfo"), $jsilcore.TypeRef("System.Reflection.FieldInfo")], [])),
      function op_Inequality(left, right) {
        return !equalsImpl(left, right);
      }
    );

    $.Method({ Static: false, Public: true }, "get_IsLiteral",
      (new JSIL.MethodSignature($.Boolean, [], [])),
      function get_IsLiteral() {
        return false;
      }
    );

    $.Method({ Static: false, Public: true, Virtual: true }, "SetValue",
      (new JSIL.MethodSignature($.Object, [$.Object, $.Object], [])),
      function SetValue(obj, value) {
        var fieldType = this.get_FieldType();
        if (!fieldType.$Is(value))
          throw new System.ArgumentException("value");

        if (this.IsStatic) {
          this.DeclaringType.__PublicInterface__[this._descriptor.Name] = value;
          return;
        }

        if (obj === null) {
          throw new System.Exception("Non-static field requires a target.");
        }

        if (!this.DeclaringType.IsAssignableFrom(obj.__ThisType__)) {
          throw new System.Exception("Field is not defined on the target object.");
        }

        if (fieldType.IsValueType) {
          value = fieldType.__PublicInterface__.$Cast(value);
        }

        obj[this._descriptor.Name] = value;
      }
    );
  }
);

JSIL.MakeClass("System.Reflection.MemberInfo", "System.Reflection.FieldInfo", true, [], function ($) {
  $.Property({ Public: true, Static: false }, "FieldType");
  $.Property({ Public: true, Static: false, Virtual: true }, "MemberType");
  $.Method({ Public: true, Static: false, Virtual: true }, "get_MemberType",
    JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Reflection.MemberTypes")),
    function get_MemberType() {
      return $jsilcore.System.Reflection.MemberTypes.Field;
    }
  );
});

JSIL.ImplementExternals(
  "System.Reflection.CustomAttributeExtensions", function ($) {
      $.Method({ Static: true, Public: true }, "GetCustomAttribute",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Attribute"), [$jsilcore.TypeRef("System.Reflection.MemberInfo"), $jsilcore.TypeRef("System.Type"), $.Boolean], []),
        function GetCustomAttribute(element, attributeType, inherit) {
            var attributes = element.GetCustomAttributes(attributeType, inherit);
            if (attributes.length === 0) {
                return null;
            } else {
                return attributes[0];
            }
        }
      );
  }
);
JSIL.ImplementExternals(
  "System.Reflection.IntrospectionExtensions", function ($) {
    $.Method({ Static: true, Public: true }, "GetTypeInfo",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.TypeInfo"), [$jsilcore.TypeRef("System.Type")], []),
      function GetTypeInfo(targetType) {
      	return targetType;
      }
    );
  }
);

JSIL.ImplementExternals(
  "System.Reflection.MethodBase", function ($) {
    $.RawMethod(false, "InitResolvedSignature",
      function InitResolvedSignature() {
        if (this.resolvedSignature === undefined) {
          this._data.resolvedSignature = this._data.signature.Resolve($jsilcore.$MemberInfoGetName(this));
          if (this._data.signature.genericArgumentValues !== undefined) {
            this._data.resolvedSignature = this._data.resolvedSignature.ResolvePositionalGenericParameters(this._data.signature.genericArgumentValues)
          }
        }
      }
    );

    $.Method({ Static: false, Public: false }, "GetParameterTypes",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Type")]), [], [])),
      function GetParameterTypes() {
        return $jsilcore.$MethodGetParameterTypes(this);
      }
    );

    $.Method({ Static: false, Public: true }, "toString",
      new JSIL.MethodSignature($.String, [], []),
      function () {
        // FIXME: Types are encoded as long names, not short names, which is incompatible with .NET
        // i.e. 'System.Int32 Foo()' instead of 'Int32 Foo()'
        return this._data.signature.toString(this.Name);
      }
    );

    $.Method({ Public: true, Static: false }, "get_IsConstructor",
      new JSIL.MethodSignature($.Boolean, []),
      function get_IsConstructor() {
        return $jsilcore.System.Reflection.ConstructorInfo.$Is(this) && !this.get_IsStatic();
      }
    );
  }
);

JSIL.MakeClass("System.Reflection.MemberInfo", "System.Reflection.MethodBase", true, [], function ($) {
  $.Property({ Public: true, Static: false, Virtual: true }, "IsGenericMethod");
  $.Property({ Public: true, Static: false, Virtual: true }, "IsGenericMethodDefinition");
  $.Property({ Public: true, Static: false, Virtual: true }, "ContainsGenericParameters");
});
JSIL.ImplementExternals("System.Reflection.MethodInfo", function ($) {
  $.Method({ Static: false, Public: true }, "GetParameters",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.ParameterInfo")]), [], [])),
    function GetParameters() {
      return $jsilcore.$MethodGetParameters(this);
    }
  );

  $.Method({ Static: false, Public: true }, "get_ReturnType",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], [])),
    function get_ReturnType() {
      return $jsilcore.$MethodGetReturnType(this);
    }
  );

  var equalsImpl = function (lhs, rhs) {
    if (lhs === rhs)
      return true;

    return JSIL.ObjectEquals(lhs, rhs);
  };

  $.Method({ Static: true, Public: true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.MethodInfo"), $jsilcore.TypeRef("System.Reflection.MethodInfo")], [])),
    function op_Equality(left, right) {
      return equalsImpl(left, right);
    }
  );

  $.Method({ Static: true, Public: true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.MethodInfo"), $jsilcore.TypeRef("System.Reflection.MethodInfo")], [])),
    function op_Inequality(left, right) {
      return !equalsImpl(left, right);
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "Invoke",
    new JSIL.MethodSignature($.Object, [
        $.Object, $jsilcore.TypeRef("System.Reflection.BindingFlags"),
        $jsilcore.TypeRef("System.Reflection.Binder"), $jsilcore.TypeRef("System.Array", [$.Object]),
        $jsilcore.TypeRef("System.Globalization.CultureInfo")
    ], []),
    function Invoke(obj, invokeAttr, binder, parameters, culture) {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "Invoke",
    new JSIL.MethodSignature($.Object, [$.Object, $jsilcore.TypeRef("System.Array", [$.Object])], []),
    function Invoke(obj, parameters) {
      var impl = JSIL.$GetMethodImplementation(this, obj);

      if (typeof (impl) !== "function")
        throw new System.Exception("Failed to find constructor");

      var parameterTypes = this.GetParameterTypes();
      var parametersCount = 0;
      if (parameters !== null)
        parametersCount = parameters.length;

      if (parameterTypes.length !== parametersCount)
        throw new System.Exception("Parameters count mismatch.");

      if (parameters !== null) {
        parameters = parameters.slice();
        for (var i = 0; i < parametersCount; i++) {
          if (parameterTypes[i].IsValueType) {
            if (parameters[i] === null) {
              parameters[i] = JSIL.DefaultValue(parameterTypes[i]);
            } else {
              parameters[i] = parameterTypes[i].__PublicInterface__.$Cast(parameters[i]);
            }
          }
        }
      }

      if (this.IsStatic) {
        obj = this._typeObject.__PublicInterface__;
      }

      return impl.apply(obj, parameters);
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "MakeGenericMethod",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Type")])]),
    function MakeGenericMethod(typeArguments) {
      if (this._data.signature.genericArgumentNames.length === 0)
        throw new System.Exception("Method is not Generic");
      if (this._data.signature.genericArgumentValues !== undefined)
        throw new System.Exception("Method is closed Generic");

      var cacheKey = JSIL.HashTypeArgumentArray(typeArguments, this._data.signature.context);
      var ofCache = this.__OfCache__;
      if (!ofCache)
        this.__OfCache__ = ofCache = {};

      var result = ofCache[cacheKey];
      if (result)
        return result;

      var parsedTypeName = JSIL.ParseTypeName("System.Reflection.RuntimeMethodInfo");
      var infoType = JSIL.GetTypeInternal(parsedTypeName, $jsilcore, true);
      var info = JSIL.CreateInstanceOfType(infoType, null);
      info._typeObject = this._typeObject;
      info._descriptor = this._descriptor;
      info.__Attributes__ = this.__Attributes__;
      info.__Overrides__ = this.__Overrides__;

      info._data = {};
      info._data.parameterInfo = this._data.parameterInfo;

      if (this._data.genericSignature)
        info._data.genericSignature = this._data.genericSignature;

      var source = this._data.signature;
      info._data.signature = new JSIL.MethodSignature(source.returnType, source.argumentTypes, source.genericArgumentNames, source.context, source, typeArguments.slice())

      ofCache[cacheKey] = info;
      return info;
    }
  );

  $.Method({ Public: true, Static: false }, "get_IsGenericMethod",
    new JSIL.MethodSignature($.Type, []),
    function get_IsGenericMethod() {
      return this._data.signature.genericArgumentNames.length !== 0;
    }
  );

  $.Method({ Public: true, Static: false }, "get_IsVirtual",
    new JSIL.MethodSignature($.Type, []),
    function get_IsGenericMethod() {
      return this._descriptor.Virtual;
    }
  );

  $.Method({ Public: true, Static: false }, "get_IsGenericMethodDefinition",
    new JSIL.MethodSignature($.Type, []),
    function get_IsGenericMethodDefinition() {
      return this._data.signature.genericArgumentNames.length !== 0 && this._data.signature.genericArgumentValues === undefined;
    }
  );

  $.Method({ Public: true, Static: false }, "get_ContainsGenericParameters",
    new JSIL.MethodSignature($.Type, []),
    function get_IsGenericMethodDefinition() {
      return this.DeclaringType.get_ContainsGenericParameters() || (this._data.signature.genericArgumentNames.length !== 0 && this._data.signature.genericArgumentValues === undefined);
    }
  );

  $.Method({ Static: false, Public: true }, "GetBaseDefinition",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [], [])),
    function getBaseDefinition() {
      var previous;
      var current = this;

      do {
        previous = current;
        current = current.GetParentDefinition();
      } while (current !== null)

      return previous;
    }
  );
});

JSIL.MakeClass("System.Reflection.MethodBase", "System.Reflection.MethodInfo", true, [], function ($) {
  $.Property({ Public: true, Static: false }, "ReturnType");
  $.Property({ Public: true, Static: false, Virtual: true }, "MemberType");
  $.Method({ Public: true, Static: false, Virtual: true }, "get_MemberType",
    JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Reflection.MemberTypes")),
    function get_MemberType() {
      return $jsilcore.System.Reflection.MemberTypes.Method;
    }
  );
});


JSIL.MakeClass("System.Reflection.TypeInfo", "System.RuntimeType", false, [], function ($) {
  $jsilcore.RuntimeTypeInitialized = true;

  $.Method({ Public: true, Static: true }, "op_Equality",
    new JSIL.MethodSignature($.Boolean, [$.Type, $.Type]),
    function(lhs, rhs) {
      if (lhs === rhs)
        return true;

      return String(lhs) == String(rhs);
    }
  );
});
JSIL.MakeClass("System.Type", "System.Reflection.TypeInfo", false, [], function ($) {
});

JSIL.ImplementExternals(
  "System.Reflection.TypeInfo", function ($) {
      $.Method({ Static: false, Public: true }, "AsType",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], []),
        function AsType() {
            return this;
        }
      );
  }
);

JSIL.MakeClass("System.Reflection.MethodBase", "System.Reflection.ConstructorInfo", true, [], function ($) {
  $.Property({ Public: true, Static: false, Virtual: true }, "MemberType");
  $.Method({ Public: true, Static: false, Virtual: true }, "get_MemberType",
    JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Reflection.MemberTypes")),
    function get_MemberType() {
      return $jsilcore.System.Reflection.MemberTypes.Constructor;
    }
  );
});

JSIL.ImplementExternals("System.Reflection.ConstructorInfo", function ($) {
  $.Method({ Static: false, Public: true }, "GetParameters",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Reflection.ParameterInfo")]), [], [])),
    function GetParameters() {
      return $jsilcore.$MethodGetParameters(this);
    }
  );

  $.Method({ Static: false, Public: true }, "Invoke",
    new JSIL.MethodSignature($.Object, [$jsilcore.TypeRef("System.Array", [$.Object])], []),
    function Invoke(parameters) {
      var impl = JSIL.$GetMethodImplementation(this, null);

      if (typeof (impl) !== "function")
        throw new System.Exception("Failed to find constructor");

      return JSIL.CreateInstanceOfType(this.get_DeclaringType(), impl, parameters);
    }
  );

  $.Method({ Static: true, Public: true }, "op_Inequality",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.ConstructorInfo"), $jsilcore.TypeRef("System.Reflection.ConstructorInfo")], []),
    function op_Inequality(left, right) {
      return left !== right;
    }
  );

  $.Method({ Static: true, Public: true }, "op_Equality",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Reflection.ConstructorInfo"), $jsilcore.TypeRef("System.Reflection.ConstructorInfo")], []),
    function op_Equality(left, right) {
      return left === right;
    }
  );
});
JSIL.MakeClass("System.Reflection.MemberInfo", "System.Reflection.EventInfo", true, [], function ($) {
  $.Property({ Public: true, Static: false, Virtual: true }, "MemberType");
  $.Method({ Public: true, Static: false, Virtual: true }, "get_MemberType",
    JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Reflection.MemberTypes")),
    function get_MemberType() {
      return $jsilcore.System.Reflection.MemberTypes.Event;
    }
  );
});

JSIL.ImplementExternals("System.Reflection.EventInfo", function ($) {
  var getAddMethodImpl = function (nonPublic) {
    var methodName = "add_" + this.get_Name();
    var bf = System.Reflection.BindingFlags;
    var bindingFlags = (nonPublic
      ? bf.$Flags("DeclaredOnly", "Instance", "Public", "NonPublic")
      : bf.$Flags("DeclaredOnly", "Instance", "Public")
    );
    return this.get_DeclaringType().GetMethod(methodName, bindingFlags);
  };

  var getRemoveMethodImpl = function (nonPublic) {
    var methodName = "remove_" + this.get_Name();
    var bf = System.Reflection.BindingFlags;
    var bindingFlags = (nonPublic
      ? bf.$Flags("DeclaredOnly", "Instance", "Public", "NonPublic")
      : bf.$Flags("DeclaredOnly", "Instance", "Public")
    );
    return this.get_DeclaringType().GetMethod(methodName, bindingFlags);
  };

  $.Method({ Static: false, Public: true }, "GetAddMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [], [])),
    getAddMethodImpl
  );

  $.Method({ Static: false, Public: true }, "GetAddMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.Boolean], [])),
    getAddMethodImpl
  );

  $.Method({ Static: false, Public: true }, "GetRemoveMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [], [])),
    getRemoveMethodImpl
  );

  $.Method({ Static: false, Public: true }, "GetRemoveMethod",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MethodInfo"), [$.Boolean], [])),
    getRemoveMethodImpl
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "AddEventHandler",
    new JSIL.MethodSignature(null, [$.Object, $jsilcore.TypeRef("System.Delegate")], []),
    function AddEventHandler(target, handler) {
      var method = this.GetAddMethod();
      method.Invoke(target, [handler]);
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "RemoveEventHandler",
    new JSIL.MethodSignature(null, [$.Object, $jsilcore.TypeRef("System.Delegate")], []),
    function RemoveEventHandler(target, handler) {
      var method = this.GetRemoveMethod();
      method.Invoke(target, [handler]);
    }
  );

  $.Method({ Static: false, Public: true }, "get_EventType",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], [])),
    function get_EventType() {
      var result = this._cachedEventType;

      if (!result) {
        var method = this.GetAddMethod() || this.GetRemoveMethod();

        if (method) {
          var argumentTypes = method._data.signature.argumentTypes;
          var argumentType = argumentTypes[0];
          result = JSIL.ResolveTypeReference(argumentType, this._typeObject.__Context__)[1];

          this._cachedEventType = result;
        }
      }

      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "toString",
    new JSIL.MethodSignature($.String, [], []),
    function () {
      // FIXME: Types are encoded as long names, not short names, which is incompatible with .NET
      // i.e. 'System.Int32 Foo()' instead of 'Int32 Foo()'
      return this.get_EventType().toString() + " " + this.Name;
    }
  );
});

JSIL.MakeClass("System.Object", "System.Reflection.Assembly", true, [], function ($) {
  $.RawMethod(false, ".ctor", function (publicInterface, fullName) {
    JSIL.SetValueProperty(this, "__PublicInterface__", publicInterface);
    JSIL.SetValueProperty(this, "__FullName__", fullName);
  });

  $.Method({ Static: true, Public: true }, "op_Equality",
    (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
    function op_Equality(left, right) {
      return left === right;
    }
  );

  $.Method({ Static: true, Public: true }, "op_Inequality",
    (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
    function op_Inequality(left, right) {
      return left !== right;
    }
  );

  $.Method({ Static: false, Public: true }, "get_CodeBase",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_CodeBase() {
      // FIXME
      return "CodeBase";
    }
  );

  $.Method({ Static: false, Public: true }, "get_FullName",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_FullName() {
      return this.__FullName__;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Location",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_Location() {
      // FIXME
      return "Location";
    }
  );

  $.Method({ Static: false, Public: true }, "GetName",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.AssemblyName"), [], [])),
    function GetName() {
      if (!this._assemblyName)
        this._assemblyName = new System.Reflection.AssemblyName(this.__FullName__);

      return this._assemblyName;
    }
  );

  $.Method({ Static: false, Public: true }, "GetType",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [$.String], [])),
    function GetType(name) {
      return JSIL.ReflectionGetTypeInternal(this, name, false, false, true);
    }
  );

  $.Method({ Static: false, Public: true }, "GetType",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [$.String, $.Boolean], [])),
    function GetType(name, throwOnError) {
      return JSIL.ReflectionGetTypeInternal(this, name, throwOnError, false, true);
    }
  );

  $.Method({ Static: true, Public: true }, "Load",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.Assembly"), [$.String], [])),
    function Load(assemblyName) {
      return JSIL.GetAssembly(assemblyName).__Assembly__;
    }
  );

  $.Method({ Static: false, Public: true }, "GetType",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [
          $.String, $.Boolean,
          $.Boolean
    ], [])),
    function GetType(name, throwOnError, ignoreCase) {
      if (ignoreCase)
        throw new Error("ignoreCase not implemented");

      return JSIL.ReflectionGetTypeInternal(this, name, throwOnError, ignoreCase, true);
    }
  );

  $.Method({ Static: false, Public: true }, "GetTypes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Type")]), [], [])),
    function GetTypes() {
      return JSIL.GetTypesFromAssembly(this.__PublicInterface__);
    }
  );

  $.Method({ Static: false, Public: true }, "get_DefinedTypes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.IEnumerable", [$jsilcore.TypeRef("System.TypeInfo")]), [], [])),
    function get_DefinedTypes() {
        return JSIL.GetTypesFromAssembly(this.__PublicInterface__);
    }
  );


  $.Method({ Static: true, Public: true }, "GetEntryAssembly",
    (new JSIL.MethodSignature($.Type, [], [])),
    function GetEntryAssembly() {
      // FIXME: Won't work if multiple loaded assemblies contain entry points.
      for (var k in JSIL.$EntryPoints) {
        var ep = JSIL.$EntryPoints[k];
        return ep[0].__Assembly__;
      }

      return null;
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "GetManifestResourceStream",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.IO.Stream"), [$.String], []),
    function GetManifestResourceStream(name) {
      var assemblyKey = this.__FullName__;
      var firstComma = assemblyKey.indexOf(",");
      if (firstComma)
        assemblyKey = assemblyKey.substr(0, firstComma);

      var files = allManifestResources[assemblyKey];
      if (!files)
        throw new Error("Assembly '" + assemblyKey + "' has no manifest resources");

      var fileKey = name.toLowerCase();

      var bytes = files[fileKey];
      if (!bytes)
        throw new Error("No stream named '" + name + "'");

      var result = new System.IO.MemoryStream(bytes, false);
      return result;
    }
  );

  $.Property({ Static: false, Public: true }, "CodeBase");
  $.Property({ Static: false, Public: true }, "Location");
  $.Property({ Static: false, Public: true }, "FullName");
  $.Property({ Static: false, Public: true }, "DefinedTypes");
});

JSIL.MakeClass("System.Reflection.Assembly", "System.Reflection.RuntimeAssembly", true, [], function ($) {
});

JSIL.ImplementExternals("System.Reflection.ParameterInfo", function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.Method({ Static: false, Public: true }, "get_Attributes",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.ParameterAttributes"), [], []),
    function get_Attributes() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "get_CustomAttributes",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [$jsilcore.TypeRef("System.Reflection.CustomAttributeData")]), [], []),
    function get_CustomAttributes() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "get_DefaultValue",
    new JSIL.MethodSignature($.Object, [], []),
    function get_DefaultValue() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "get_HasDefaultValue",
    new JSIL.MethodSignature($.Boolean, [], []),
    function get_HasDefaultValue() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "get_Member",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.MemberInfo"), [], []),
    function get_Member() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "get_Name",
    new JSIL.MethodSignature($.String, [], []),
    function get_Name() {
      if (this._name) {
        return this._name;
      } else {
        return "<unnamed parameter #" + this.position + ">";
      }
    }
  );

  $.Method({ Static: false, Public: true }, "get_ParameterType",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], []),
    function get_ParameterType() {
      return $jsilcore.$ParameterInfoGetParameterType(this);
    }
  );

  $.Method({ Static: false, Public: true }, "get_Position",
    new JSIL.MethodSignature($.Int32, [], []),
    function get_Position() {
      return this.position;
    }
  );

  $.Method({ Static: false, Public: true }, "GetCustomAttributes",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Object]), [$.Boolean], []),
    function GetCustomAttributes(inherit) {
      return JSIL.GetMemberAttributes(this, inherit, null);
    }
  );

  $.Method({ Static: false, Public: true }, "GetCustomAttributes",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Object]), [$jsilcore.TypeRef("System.Type"), $.Boolean], []),
    function GetCustomAttributes(attributeType, inherit) {
      return JSIL.GetMemberAttributes(this, inherit, attributeType);
    }
  );

  $.Method({ Static: false, Public: true }, "toString",
    new JSIL.MethodSignature($.String, [], []),
    function toString() {
      return this.argumentType.toString() + " " + this.get_Name();
    }
  );
});

JSIL.MakeClass("System.Object", "System.Reflection.ParameterInfo", true, [], function ($) {
  $.Property({ Public: true, Static: false, Virtual: true }, "Name");
  $.Property({ Public: true, Static: false, Virtual: true }, "ParameterType");
  $.Property({ Public: true, Static: false, Virtual: true }, "Position");
});

JSIL.MakeEnum(
  "System.Reflection.MemberTypes", true, {
    Constructor: 1,
    Event: 2,
    Field: 4,
    Method: 8,
    Property: 16,
    TypeInfo: 32,
    Custom: 64,
    NestedType: 128,
    All: 191
  }, true
);

JSIL.ImplementExternals("System.Attribute", function ($) {
});

(function AssemblyName$Members() {
  var $, $thisType;
  JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.Object"),
    Name: "System.Reflection.AssemblyName",
    IsPublic: true,
    IsReferenceType: true,
    MaximumConstructorArguments: 2,
  }, function ($interfaceBuilder) {
    $ = $interfaceBuilder;

    $.ExternalMethod({ Static: false, Public: true }, ".ctor",
      JSIL.MethodSignature.Void
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, ".ctor",
      JSIL.MethodSignature.Action($.String)
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, "get_Flags",
      JSIL.MethodSignature.Return($asm02.TypeRef("System.Reflection.AssemblyNameFlags"))
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, "get_FullName",
      JSIL.MethodSignature.Return($.String)
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, "get_Name",
      JSIL.MethodSignature.Return($.String)
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, "get_Version",
      JSIL.MethodSignature.Return($asm02.TypeRef("System.Version"))
    )
    ;

    $.ExternalMethod({ Static: true, Public: true }, "GetAssemblyName",
      new JSIL.MethodSignature($.Type, [$.String])
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, "set_Flags",
      JSIL.MethodSignature.Action($asm02.TypeRef("System.Reflection.AssemblyNameFlags"))
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, "set_Name",
      JSIL.MethodSignature.Action($.String)
    )
    ;

    $.ExternalMethod({ Static: false, Public: true }, "set_Version",
      JSIL.MethodSignature.Action($asm02.TypeRef("System.Version"))
    )
    ;

    $.ExternalMethod({ Static: false, Public: true, Virtual: true }, "toString",
      JSIL.MethodSignature.Return($.String)
    )
    ;

    $.Property({ Static: false, Public: true }, "Name", $.String)
    ;

    $.Property({ Static: false, Public: true }, "Version", $asm02.TypeRef("System.Version"))
    ;

    $.Property({ Static: false, Public: true }, "Flags", $asm02.TypeRef("System.Reflection.AssemblyNameFlags"))
    ;

    $.Property({ Static: false, Public: true }, "FullName", $.String)
    ;

    $.ImplementInterfaces(
      /* 0 */ $asm02.TypeRef("System.Runtime.InteropServices._AssemblyName"),
      /* 1 */ $asm02.TypeRef("System.ICloneable"),
      /* 2 */ $asm02.TypeRef("System.Runtime.Serialization.ISerializable"),
      /* 3 */ $asm02.TypeRef("System.Runtime.Serialization.IDeserializationCallback")
    );

    return function (newThisType) { $thisType = newThisType; };
  });

})();

JSIL.ImplementExternals("System.Reflection.AssemblyName", function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.Method({ Static: false, Public: true }, ".ctor",
    JSIL.MethodSignature.Void,
    function _ctor() {
      this.set_Name(null);
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    JSIL.MethodSignature.Action($.String),
    function _ctor(assemblyName) {
      this.set_Name(assemblyName);
    }
  );

  $.Method({ Static: false, Public: true }, "get_Flags",
    JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Reflection.AssemblyNameFlags")),
    function get_Flags() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "get_FullName",
    JSIL.MethodSignature.Return($.String),
    function get_FullName() {
      return this._FullName;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Name",
    JSIL.MethodSignature.Return($.String),
    function get_Name() {
      return this._Name;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Version",
    JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Version")),
    function get_Version() {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: true, Public: true }, "GetAssemblyName",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.AssemblyName"), [$.String]),
    function GetAssemblyName(assemblyFile) {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "set_Flags",
    JSIL.MethodSignature.Action($jsilcore.TypeRef("System.Reflection.AssemblyNameFlags")),
    function set_Flags(value) {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "set_Name",
    JSIL.MethodSignature.Action($.String),
    function set_Name(value) {
      this._Name = value;
    }
  );

  $.Method({ Static: false, Public: true }, "set_Version",
    JSIL.MethodSignature.Action($jsilcore.TypeRef("System.Version")),
    function set_Version(value) {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "toString",
    JSIL.MethodSignature.Return($.String),
    function toString() {
      throw new Error('Not implemented');
    }
  );

  ;
});

JSIL.MakeInterface(
  "System.Reflection.ICustomAttributeProvider", true, [], function ($) {
    $.Method({}, "GetCustomAttributes", new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Object]), [$jsilcore.TypeRef("System.Type"), $.Boolean]));
    $.Method({}, "GetCustomAttributes", new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Object]), [$.Boolean]));
    $.Method({}, "IsDefined", new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Type"), $.Boolean]));
  }, [])

JSIL.MakeClass("System.Reflection.ConstructorInfo", "System.Reflection.RuntimeConstructorInfo", false, [], function ($) {
});
JSIL.MakeClass("System.Reflection.EventInfo", "System.Reflection.RuntimeEventInfo", false, [], function ($) {
});
JSIL.MakeClass("System.Reflection.FieldInfo", "System.Reflection.RuntimeFieldInfo", false, [], function ($) {
});
JSIL.MakeClass("System.Reflection.MethodInfo", "System.Reflection.RuntimeMethodInfo", false, [], function ($) {
  $.Method({ Static: false, Public: false }, "GetParentDefinition",
  (new JSIL.MethodSignature($jsilcore.TypeRef("System.Reflection.RuntimeMethodInfo"), [], [])),
  function get_ReturnType() {
    if (!this._descriptor.Virtual || this._descriptor.Static) {
      return null;
    }

    var currentType = this.get_DeclaringType();
    while (true) {
      currentType = currentType.__BaseType__;
      if (!(currentType && currentType.GetType)) {
        return null;
      }
      var foundMethod = JSIL.GetMethodInfo(currentType.__PublicInterface__, this.get_Name(), this._data.signature, false, null);
      if (foundMethod != null) {
        return foundMethod;
      }
    }
  }
);
});
JSIL.MakeClass("System.Reflection.ParameterInfo", "System.Reflection.RuntimeParameterInfo", false, [], function ($) {
  $.RawMethod(false, "$fromArgumentTypeAndPosition", function (argumentType, position) {
    this.argumentType = argumentType;
    this.position = position;
    this._name = null;
    this.__Attributes__ = [];
  });

  $.RawMethod(false, "$populateWithParameterInfo", function (parameterInfo) {
    this._name = parameterInfo.name || null;

    if (parameterInfo.attributes) {
      var mb = new JSIL.MemberBuilder(null);
      parameterInfo.attributes(mb);
      this.__Attributes__ = mb.attributes;
    }
  });
});
JSIL.MakeClass("System.Reflection.PropertyInfo", "System.Reflection.RuntimePropertyInfo", false, [], function ($) {
});
