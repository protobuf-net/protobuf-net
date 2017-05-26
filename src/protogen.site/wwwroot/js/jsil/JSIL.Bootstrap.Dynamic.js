/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

if (!$jsilcore)
    throw new Error("JSIL.Core is required");

JSIL.DeclareNamespace("System.Runtime.CompilerServices");
JSIL.DeclareNamespace("Microsoft");
JSIL.DeclareNamespace("Microsoft.CSharp");
JSIL.DeclareNamespace("Microsoft.CSharp.RuntimeBinder");
JSIL.DeclareNamespace("System.Linq");
JSIL.DeclareNamespace("System.Linq.Expressions");

JSIL.MakeClass("System.Object", "System.Runtime.CompilerServices.CallSite", true, [], function ($) {
});

JSIL.MakeClass("System.Object", "System.Runtime.CompilerServices.CallSite`1", true, ["T"], function ($) {
  $.Method({ Static: true, Public: true }, "Create",
    new JSIL.MethodSignature($.Type, [$jsilcore.TypeRef("System.Runtime.CompilerServices.CallSiteBinder")], []),
    function(binder) {
      var callSite = new this();
      callSite.Target = binder.Method;
      return callSite;
    }
  );

  $.Field({ Public: false, Static: false }, "Target", new JSIL.GenericParameter("T", "System.Runtime.CompilerServices.CallSite`1"));
});
JSIL.MakeClass("System.Object", "System.Runtime.CompilerServices.CallSiteBinder", true, [], function($) {
});
JSIL.MakeStaticClass("Microsoft.CSharp.RuntimeBinder.Binder", true, [], function($) {
  $.RawMethod(true, "BinaryOperation",
    function(flags, operation, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Add || operation === $jsilcore.System.Linq.Expressions.ExpressionType.AddChecked) {
        binder.Method = function (callSite, left, right) {
          return left+right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.And) {
        binder.Method = function (callSite, left, right) {
          return left & right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.AndAlso) {
        binder.Method = function (callSite, left, right) {
          return left && right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Divide) {
        binder.Method = function (callSite, left, right) {
          return left / right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Equal) {
        binder.Method = function (callSite, left, right) {
          return left == right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.ExclusiveOr) {
        binder.Method = function (callSite, left, right) {
          return left ^ right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.GreaterThan) {
        binder.Method = function (callSite, left, right) {
          return left > right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.GreaterThanOrEqual) {
        binder.Method = function (callSite, left, right) {
          return left >= right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.LeftShift) {
        binder.Method = function (callSite, left, right) {
          return left << right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.LessThan) {
        binder.Method = function (callSite, left, right) {
          return left < right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.LessThanOrEqual) {
        binder.Method = function (callSite, left, right) {
          return left <= right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Modulo) {
        binder.Method = function (callSite, left, right) {
          return left % right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Multiply || operation === $jsilcore.System.Linq.Expressions.ExpressionType.MultiplyChecked) {
        binder.Method = function (callSite, left, right) {
          return left * right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.NotEqual) {
        binder.Method = function (callSite, left, right) {
          return left != right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Or) {
        binder.Method = function (callSite, left, right) {
          return left | right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.OrElse) {
        binder.Method = function (callSite, left, right) {
          return left || right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.RightShift) {
        binder.Method = function (callSite, left, right) {
          return left >> right;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Subtract || operation === $jsilcore.System.Linq.Expressions.ExpressionType.SubtractChecked) {
        binder.Method = function (callSite, left, right) {
          return left - right;
        };
      } else {
        throw new Error("Binary operator is not supported.");
      }
      return binder;
    });

  $.RawMethod(true, "Convert",
    function(flags, type, context) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      binder.Method = function (callSite, target) {
        return type.__PublicInterface__.$Cast(target);
      };
      return binder;
    });

  $.RawMethod(true, "GetIndex",
    function(flags, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      var isStaticCall = (argumentInfo[0].Flags & $jsilcore.Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.IsStaticType) > 0;
      binder.Method = function (callSite, target) {
        var realTarget = (isStaticCall ? target.__PublicInterface__ : target);
        if ("get_Item" in realTarget) {
          return realTarget["get_Item"].apply(realTarget, Array.prototype.slice.call(arguments, 2));
        } else {
          // TODO: Jagged arrays support
          if (arguments.length === 3) {
            return realTarget[arguments[2]];
          } else {
            throw new Error("Cannot use multi-dimensional indexer for object without indexed property.");
          }
        }
      };
      return binder;
    });

  $.RawMethod(true, "GetMember",
    function (flags, name, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      var isStaticCall = (argumentInfo[0].Flags & $jsilcore.Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.IsStaticType) > 0;
      binder.Method = function (callSite, target) {
        var realTarget = (isStaticCall ? target.__PublicInterface__ : target);
        if (("get_" + name) in realTarget) {
          return realTarget["get_" + name]();
        } else {
          return realTarget[name];
        }
      };
      return binder;
    });

  $.RawMethod(true, "Invoke",
    function (flags, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      binder.Method = function (callSite, target) {
        return target.apply(null, Array.prototype.slice.call(arguments, 2));
      };
      return binder;
    });

  $.RawMethod(true, "InvokeConstructor",
    function(flags, context, argumentInfo) {
      throw new Error("Not implemented");
    });

  $.RawMethod(true, "InvokeMember",
    function (flags, name, typeArguments, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      var isStaticCall = (argumentInfo[0].Flags & $jsilcore.Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.IsStaticType) > 0;
      if (typeArguments !== null) {
        var useMemberName = name + "$b" + typeArguments.length;
        binder.Method = function (callSite, target) {
          var realTarget = (isStaticCall ? target.__PublicInterface__ : target);
          return realTarget[useMemberName].apply(realTarget, typeArguments).apply(realTarget, Array.prototype.slice.call(arguments, 2));
        };
      } else {
        binder.Method = function (callSite, target) {
          var realTarget = (isStaticCall ? target.__PublicInterface__ : target);
          return realTarget[name].apply(realTarget, Array.prototype.slice.call(arguments, 2));
        };
      }
      return binder;
    });

  $.RawMethod(true, "IsEvent",
    function(flags, name, context) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      binder.Method = function () {
        return false;
      };
      return binder;
    });

  $.RawMethod(true, "SetIndex",
    function(flags, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      var isStaticCall = (argumentInfo[0].Flags & $jsilcore.Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.IsStaticType) > 0;
      binder.Method = function (callSite, target) {
        var realTarget = (isStaticCall ? target.__PublicInterface__ : target);
        if ("set_Item" in realTarget) {
          return realTarget["set_Item"].apply(realTarget, Array.prototype.slice.call(arguments, 2));
        } else {
          // TODO: Jagged arrays support
          if (arguments.length === 4) {
            realTarget[arguments[2]] = arguments[3];
          } else {
            throw new Error("Cannot use multi-dimensional indexer for object without indexed property.");
          }
        }
      };
      return binder;
    });

  $.RawMethod(true, "SetMember",
    function(flags, name, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      var isStaticCall = (argumentInfo[0].Flags & $jsilcore.Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.IsStaticType) > 0;
      binder.Method = function(callSite, target, value) {
        var realTarget = (isStaticCall ? target.__PublicInterface__ : target);
        if (("set_" + name) in realTarget) {
          return realTarget["set_" + name](value);
        } else {
          realTarget[name] = value;
        }
      };
      return binder;
    });

  $.RawMethod(true, "UnaryOperation",
    function(flags, operation, context, argumentInfo) {
      var binder = new $jsilcore.System.Runtime.CompilerServices.CallSiteBinder();
      if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.UnaryPlus) {
        binder.Method = function(callSite, target) {
          return target;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Negate || operation === $jsilcore.System.Linq.Expressions.ExpressionType.NegateChecked) {
        binder.Method = function (callSite, target) {
          return -target;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.Not) {
        binder.Method = function (callSite, target) {
          if (typeof(target) === "boolean") {
            return ~target;
          }
          return ~target;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.IsTrue) {
        binder.Method = function (callSite, target) {
          return target === true;
        };
      } else if (operation === $jsilcore.System.Linq.Expressions.ExpressionType.IsFalse) {
        binder.Method = function (callSite, target) {
          return target === false;
        };
      } else {
        throw new Error("Unary operator is not supported.");
      }
      return binder;
    });
});
JSIL.MakeClass("System.Object", "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo", true, [], function ($) {
  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [], []),
    function () {
    }
  );

  $.Method({ Static: true, Public: true }, "Create",
    new JSIL.MethodSignature($.Type, [$jsilcore.TypeRef("Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags"), $.String], []),
    function CSharpArgumentInfo_Create(flags, name) {
      var info = new $jsilcore.Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo();
      info.Flags = flags;
      info.Name = name;
      return info;
    }
  );

  $.Field({ Public: false, Static: false }, "Flags", $jsilcore.TypeRef("Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags"));
  $.Field({ Public: false, Static: false }, "Name", $.String);

});

JSIL.MakeEnum(
  "Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags", true, {
    None: 0,
    CheckedContext: 1,
    InvokeSimpleName: 2,
    InvokeSpecialName: 4,
    BinaryOperationLogical: 8,
    ConvertExplicit: 16,
    ConvertArrayIndex: 32,
    ResultIndexed: 64,
    ValueFromCompoundAssignment: 128,
    ResultDiscarded: 256
  }, true
);
JSIL.MakeEnum(
  "Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags", true, {
   None: 0,
   UseCompileTimeType: 1,
   Constant: 2,
   NamedArgument: 4,
   IsRef: 8,
   IsOut: 16,
   IsStaticType: 32
  }, true
);
JSIL.MakeEnum(
  "System.Linq.Expressions.ExpressionType", true, {
    Add: 0,
    AddChecked: 1,
    And: 2,
    AndAlso: 3,
    ArrayLength: 4,
    ArrayIndex: 5,
    Call: 6,
    Coalesce: 7,
    Conditional: 8,
    Constant: 9,
    Convert: 10,
    ConvertChecked: 11,
    Divide: 12,
    Equal: 13,
    ExclusiveOr: 14,
    GreaterThan: 15,
    GreaterThanOrEqual: 16,
    Invoke: 17,
    Lambda: 18,
    LeftShift: 19,
    LessThan: 20,
    LessThanOrEqual: 21,
    ListInit: 22,
    MemberAccess: 23,
    MemberInit: 24,
    Modulo: 25,
    Multiply: 26,
    MultiplyChecked: 27,
    Negate: 28,
    UnaryPlus: 29,
    NegateChecked: 30,
    New: 31,
    NewArrayInit: 32,
    NewArrayBounds: 33,
    Not: 34,
    NotEqual: 35,
    Or: 36,
    OrElse: 37,
    Parameter: 38,
    Power: 39,
    Quote: 40,
    RightShift: 41,
    Subtract: 42,
    SubtractChecked: 43,
    TypeAs: 44,
    TypeIs: 45,
    Assign: 46,
    Block: 47,
    DebugInfo: 48,
    Decrement: 49,
    Dynamic: 50,
    Default: 51,
    Extension: 52,
    Goto: 53,
    Increment: 54,
    Index: 55,
    Label: 56,
    RuntimeVariables: 57,
    Loop: 58,
    Switch: 59,
    Throw: 60,
    Try: 61,
    Unbox: 62,
    AddAssign: 63,
    AndAssign: 64,
    DivideAssign: 65,
    ExclusiveOrAssign: 66,
    LeftShiftAssign: 67,
    ModuloAssign: 68,
    MultiplyAssign: 69,
    OrAssign: 70,
    PowerAssign: 71,
    RightShiftAssign: 72,
    SubtractAssign: 73,
    AddAssignChecked: 74,
    MultiplyAssignChecked: 75,
    SubtractAssignChecked: 76,
    PreIncrementAssign: 77,
    PreDecrementAssign: 78,
    PostIncrementAssign: 79,
    PostDecrementAssign: 80,
    TypeEqual: 81,
    OnesComplement: 82,
    IsTrue: 83,
    IsFalse: 84
  }, true
);

