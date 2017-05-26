/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

if (!$jsilcore)
    throw new Error("JSIL.Core is required");

JSIL.MakeClass("System.Object", "JSIL.Reference", true, [], function ($) {
    var types = {};

    $.SetValue("__IsReference__", true);

    var checkType = function Reference_CheckType(value) {
        var type = this;

        var isReference = JSIL.Reference.$Is(value, true);
        if (!isReference)
            return false;

        var typeProto = Object.getPrototypeOf(type);
        if (
          (typeProto === JSIL.GenericParameter.prototype) ||
          (typeProto === JSIL.PositionalGenericParameter.prototype)
        ) {
            return true;
        }

        var refValue = value.get();

        if ((type.__IsReferenceType__) && (refValue === null))
            return true;

        return type.$Is(refValue, false);
    };

    var of = function Reference_Of(type) {
        if (typeof (type) === "undefined")
            JSIL.RuntimeError("Undefined reference type");

        var typeObject = JSIL.ResolveTypeReference(type)[1];

        var elementName = JSIL.GetTypeName(typeObject);
        var compositePublicInterface = types[elementName];

        if (typeof (compositePublicInterface) === "undefined") {
            var typeName = "ref " + elementName;

            var compositeTypeObject = JSIL.CreateDictionaryObject($.Type);
            compositePublicInterface = JSIL.CreateDictionaryObject(JSIL.Reference);

            JSIL.SetValueProperty(compositePublicInterface, "__Type__", compositeTypeObject);
            JSIL.SetValueProperty(compositeTypeObject, "__PublicInterface__", compositePublicInterface);

            compositeTypeObject.__IsByRef__ = true;
            compositeTypeObject.__ReferentType__ = type;

            var toStringImpl = function (context) {
                return "ref " + typeObject.toString(context);
            };

            compositePublicInterface.prototype = JSIL.MakeProto(JSIL.Reference, compositeTypeObject, typeName, true, typeObject.__Context__);

            JSIL.SetValueProperty(compositePublicInterface, "CheckType", checkType.bind(type));

            JSIL.SetValueProperty(compositePublicInterface, "toString", function ReferencePublicInterface_ToString() {
                return "<JSIL.Reference.Of(" + typeObject.toString() + ") Public Interface>";
            });
            JSIL.SetValueProperty(compositePublicInterface.prototype, "toString", toStringImpl);
            JSIL.SetValueProperty(compositeTypeObject, "toString", toStringImpl);

            JSIL.SetValueProperty(compositePublicInterface, "__FullName__", typeName);
            JSIL.SetValueProperty(compositeTypeObject, "__FullName__", typeName);

            JSIL.SetTypeId(
              compositePublicInterface, compositeTypeObject, (
                $.Type.__TypeId__ + "[" + JSIL.HashTypeArgumentArray([typeObject], typeObject.__Context__) + "]"
              )
            );

            JSIL.MakeCastMethods(
              compositePublicInterface, compositeTypeObject, "reference"
            );

            types[elementName] = compositePublicInterface;
        }

        return compositePublicInterface;
    };

    $.RawMethod(true, "Of$NoInitialize", of);
    $.RawMethod(true, "Of", of);

    $.RawMethod(false, "get_value",
      function Reference_GetValue() {
          JSIL.RuntimeError("Use of old-style reference.value");
      }
    );

    $.RawMethod(false, "set_value",
      function Reference_SetValue(value) {
          JSIL.RuntimeError("Use of old-style reference.value = x");
      }
    );

    $.Property({ Static: false, Public: true }, "value");
});
JSIL.MakeClass("JSIL.Reference", "JSIL.BoxedVariable", true, [], function ($) {
    $.RawMethod(false, ".ctor",
      function BoxedVariable_ctor(value) {
          this.$value = value;
      }
    );

    $.RawMethod(false, "get",
      function BoxedVariable_Get() {
          return this.$value;
      }
    );

    $.RawMethod(false, "set",
      function BoxedVariable_Set(value) {
          return this.$value = value;
      }
    );
});
JSIL.MakeClass("JSIL.Reference", "JSIL.MemberReference", true, [], function ($) {
    $.RawMethod(false, ".ctor",
      function MemberReference_ctor(object, memberName) {
          this.object = object;
          this.memberName = memberName;
      }
    );

    $.RawMethod(false, "get",
      function MemberReference_Get() {
          return this.object[this.memberName];
      }
    );

    $.RawMethod(false, "set",
      function MemberReference_Set(value) {
          return this.object[this.memberName] = value;
      }
    );
});
JSIL.MakeClass("JSIL.Reference", "JSIL.ArrayElementReference", true, [], function ($) {
    $.RawMethod(false, ".ctor",
      function ArrayElementReference_ctor(array, index) {
          this.array = array;
          this.index = index | 0;
      }
    );

    $.RawMethod(false, "get",
      function ArrayElementReference_Get() {
          return this.array[this.index];
      }
    );

    $.RawMethod(false, "set",
      function ArrayElementReference_Set(value) {
          return this.array[this.index] = value;
      }
    );

    $.RawMethod(false, "retarget",
      function ArrayElementReference_Retarget(array, index) {
          this.array = array;
          this.index = index | 0;
          return this;
      }
    );
});
JSIL.MakeClass("System.Object", "JSIL.Box", true, ["TValue"], function ($) {
  function prepareInterfaceCaller(interfaceMethod) {
    return function() { return interfaceMethod.apply(this.value, arguments); };
  }

  $.SetValue("__IsBox__", true);

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [new JSIL.GenericParameter("TValue", "JSIL.Box")], [])),
    function _ctor(value) {
      this.value = value;
    }
  );

  $.RawMethod(true, "$addTypeMethods",
    function $addTypeMethods(ctor, type) {
      var ctor = ctor.prototype;
      var type = type.prototype;
      //var initType = new ctor(null);

      for (var key in type) {
        if (type.hasOwnProperty(key) && !(key in ctor)) {
          var obj = type[key];
          if (typeof (obj) == "function") {
            ctor[key] = prepareInterfaceCaller(obj);
          }
        }
      }
    }
  );

  $.RawMethod(false, "valueOf",
    function valueOf() {
      return this.value.valueOf();
    }
  );

  $.RawMethod(false, "toString",
    function toString() {
      return this.value.toString();
    }
  );

  $.RawMethod(false, "GetHashCode",
    function Box_GetHashCode() {
      return JSIL.ObjectHashCode(this.value, true, this.TValue);
    }
  );

  $.RawMethod(true, "IsBoxedOfType",
    function isBoxedOfType(value, type) {
      return value !== null && value !== undefined && (value.__IsBox__ || false) && value.TValue.__TypeId__ === type.__TypeId__;
    }
  );

  $.Method({ Static: false, Public: true }, "Object.Equals",
    new JSIL.MethodSignature($.Boolean, [$.Object], []),
    function Box_Equals(other) {
      // TODO: Implement Equals method for primitive types and call that method.
      if (this.value !== null && other == null) {
        return false;
      }

      return this.value.valueOf() === other.valueOf();
    }
  );

  $.Field({ Public: false, Static: false, ReadOnly: true }, "value", new JSIL.GenericParameter("TValue", "JSIL.Box"));
});






