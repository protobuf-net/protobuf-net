/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

if (!$jsilcore)
    throw new Error("JSIL.Core is required");

﻿//  Since alot of operators are shared between Int64 and UInt64, we construct both types using this function
JSIL.Make64BitInt = function ($, _me) {
  var mscorlib = JSIL.GetCorlib();

  function lazy(f) {
    var state = null;
    return function getLazyValue() {
      if (state === null)
        state = f();
      return state;
    };
  };

  var me = lazy(_me);

  var ctor = function ctor(a, b, c) {
    return new (me())(a, b, c);
  };

  var mktemp = function () {
    return lazy(function () {
      return ctor(0, 0, 0);
    });
  };

  var maxValue = lazy(function () {
    return ctor(0xFFFFFF, 0xFFFFFF, 0xFFFF);
  });

  var zero = lazy(function () {
    return ctor(0, 0, 0);
  });

  var one = lazy(function () {
    return ctor(1, 0, 0);
  });

  var tempLS = mktemp();
  var tempMul1 = mktemp();
  var tempMul2 = mktemp();

  var makeOrReturn = function makeOrReturn(result, a, b, c) {
    if (result) {
      result.a = a & 0xffffff;
      result.b = b & 0xffffff;
      result.c = c & 0xffff;
      return result;
    }

    return ctor(a & 0xffffff, b & 0xffffff, c & 0xffff);
  };

  var tryParse =
    function xInt64_TryParse(text, style, result) {
      var r = zero();

      var radix = 10;

      if (style & System.Globalization.NumberStyles.AllowHexSpecifier)
        radix = 16;

      var rdx = ctor(radix, 0, 0);
      var neg = false;

      for (var i = 0; i < text.length; i++) {
        if (i == 0 && text[i] == '-') {
          neg = true;
          continue;
        }
        var c = parseInt(text[i], radix);
        if (isNaN(c)) {
          result.set(zero());
          return false;
        }
        r = me().op_Addition(ctor(c, 0, 0), me().op_Multiplication(rdx, r));
      }

      if (neg)
        r = me().op_UnaryNegation(r);

      result.set(r);

      return true;
    };

  $.RawMethod(true, "Create", function xInt64_Create(a, b, c) {
    return new (me())(a, b, c);
  });

  $.RawMethod(true, "FromBytes", function xInt64_FromBytes(bytes, offset) {
    var a = (bytes[offset + 0] << 0) |
      (bytes[offset + 1] << 8) |
      (bytes[offset + 2] << 16);
    var b = (bytes[offset + 3] << 0) |
      (bytes[offset + 4] << 8) |
      (bytes[offset + 5] << 16);
    var c = (bytes[offset + 6] << 0) |
      (bytes[offset + 7] << 8);
    return new (me())(a, b, c);
  });

  $.RawMethod(false, ".ctor", function xInt64__ctor(a, b, c) {
    this.a = a | 0;
    this.b = b | 0;
    this.c = c | 0;
  });

  $.Method({ Static: true, Public: true }, "Parse",
    (new JSIL.MethodSignature($.Type, ["System.String"], [])),
    function xInt64_Parse(text) {
      var result = new JSIL.BoxedVariable(null);
      if (!tryParse(text, 0, result))
        throw new System.Exception("NumberParseException");

      return result.get();
    });

  $.Method({ Static: true, Public: true }, "op_Addition",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function xInt64_op_Addition(a, b, result) {
          var ca = a.a + b.a;
          var ra = (ca & 0xffffff000000) >> 24;
          var cb = ra + a.b + b.b;
          var rb = (cb & 0xffffff000000) >> 24;
          var cc = rb + a.c + b.c;

          return makeOrReturn(result, ca, cb, cc);
        });

  $.Method({ Static: true, Public: true }, "op_Subtraction",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function xInt64_op_Subtraction(a, b, result) {
          var ca = (a.a - b.a) | 0;
          var ra = 0;
          if (ca < 0) {
            ca = 0x1000000 + ca;
            ra = -1;
          }
          var cb = (ra + ((a.b - b.b) | 0)) | 0;
          var rb = 0;
          if (cb < 0) {
            cb = 0x1000000 + cb;
            rb = -1;
          }
          var cc = (rb + ((a.c - b.c) | 0)) | 0;
          if (cc < 0) {
            cc = 0x10000 + cc;
          }

          return makeOrReturn(result, ca, cb, cc);
        });

  $.Method({ Static: true, Public: true }, "op_LeftShift",
        (new JSIL.MethodSignature($.Type, [$.Type, mscorlib.TypeRef("System.Int32")], [])),
         function xInt64_op_LeftShift(a, n, result) { // a is UInt64, n is Int32
           if (!result)
             result = ctor(0, 0, 0);

           n = n & 0x3f;

           var maxShift = 8;
           if (n > 8) {
             return me().op_LeftShift(me().op_LeftShift(a, maxShift, tempLS()), n - maxShift, result);
           }

           var bat = a.a << n;
           var ba = bat & 0xffffff;
           var ra = (bat >>> 24) & 0xffffff;
           var bbt = (a.b << n) | ra;
           var bb = bbt & 0xffffff;
           var rb = (bbt >>> 24) & 0xffff;
           var bct = a.c << n;
           var bc = (bct & 0xffff) | rb;

           return makeOrReturn(result, ba, bb, bc);
         });

  $.Method({ Static: true, Public: true }, "op_OnesComplement",
        (new JSIL.MethodSignature($.Type, [$.Type], [])),
        function xInt64_op_OnesComplement(a, result) {
          return makeOrReturn(result, ~a.a, ~a.b, ~a.c);
        });

  $.Method({ Static: true, Public: true }, "op_ExclusiveOr",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function xInt64_op_ExclusiveOr(a, b, result) {
          return makeOrReturn(result, a.a ^ b.a, a.b ^ b.b, a.c ^ b.c);
        });

  $.Method({ Static: true, Public: true }, "op_BitwiseAnd",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function xInt64_op_BitwiseAnd(a, b, result) {
          return makeOrReturn(result, a.a & b.a, a.b & b.b, a.c & b.c);
        });

  $.Method({ Static: true, Public: true }, "op_BitwiseOr",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function xInt64_op_BitwiseOr(a, b, result) {
          return makeOrReturn(result, a.a | b.a, a.b | b.b, a.c | b.c);
        });


  $.Method({ Static: true, Public: true }, "op_Equality",
        (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
        function xInt64_op_Equality(a, b) {
          return a.a === b.a && a.b === b.b && a.c === b.c;
        });

  $.Method({ Static: true, Public: true }, "op_Inequality",
        (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
        function xInt64_op_Inequality(a, b) {
          return a.a !== b.a || a.b !== b.b || a.c !== b.c;
        });

  $.Method({ Static: true, Public: true }, "op_Decrement",
        (new JSIL.MethodSignature($.Type, [$.Type], [])),
        function xInt64_op_Decrement(a, result) {
          if (a.a > 0)
            return makeOrReturn(result, a.a - 1, a.b, a.c);
          else
            return me().op_Subtraction(a, one(), result);
        });

  $.Method({ Static: true, Public: true }, "op_Increment",
        (new JSIL.MethodSignature($.Type, [$.Type], [])),
        function xInt64_op_Increment(a, result) {
          if (a.a < 0xffffff)
            return makeOrReturn(result, a.a + 1, a.b, a.c);
          else
            return me().op_Addition(a, one(), result);
        });

  $.Method({ Static: true, Public: true }, "op_Multiplication",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function xInt64_op_Multiplication(a, b, result) {
          if (me().op_Equality(a, zero()) || me().op_Equality(b, zero()))
            return zero();

          if (mscorlib.System.UInt64.op_GreaterThan(a, b))
            return me().op_Multiplication(b, a, result);

          var s = result;
          if (!s)
            s = ctor(0, 0, 0);

          if (a.a & 1 == 1) {
            s.a = b.a;
            s.b = b.b;
            s.c = b.c;
          }

          var l = one();

          while (!me().op_Equality(a, l)) {
            a = mscorlib.System.UInt64.op_RightShift(a, 1, tempMul1());
            b = me().op_LeftShift(b, 1, tempMul2());

            if (a.a & 1 == 1)
              s = me().op_Addition(b, s, s);
          }

          return s;
        });

  $.RawMethod(true, "CheckType", function xInt64_CheckType(value) {
    return value &&
      (typeof value.a === "number") &&
      (typeof value.b === "number") &&
      (typeof value.c === "number");
  });

  $.RawMethod(false, "valueOf", function xInt64_valueOf() {
    return this.ToNumber();
  });

  $.RawMethod(true, "FromNumberImpl", function (n, makeResult) {
    if (n < 0)
      JSIL.RuntimeError("cannot construct UInt64 from negative number");

    var bits24 = 0xffffff;

    var floorN = Math.floor(n);
    var n0 = floorN | 0;
    var n1 = (floorN / 0x1000000) | 0;
    var n2 = (floorN / 0x1000000000000) | 0;

    return makeResult(
        (n0 & bits24),
        (n1 & bits24),
        (n2 & bits24)
    );
  });

  var $formatSignature = function () {
    return ($formatSignature = JSIL.Memoize(new JSIL.MethodSignature($jsilcore.TypeRef("System.String"), [
        $jsilcore.TypeRef("System.String"), $jsilcore.TypeRef($.Type.__FullName__),
        $jsilcore.TypeRef("System.IFormatProvider")
    ])))();
  };

  $.RawMethod(
    true, "$ToString",
    function $ToString(self, format, formatProvider) {
      return $formatSignature().CallStatic($jsilcore.JSIL.System.NumberFormatter, "NumberToString", null, format, self, formatProvider).toString();
    }
  );

  return {
    lazy: lazy,
    me: me,
    ctor: ctor,
    mktemp: mktemp
  };
};


JSIL.ImplementExternals("System.UInt64", function ($) {
    var mscorlib = JSIL.GetCorlib();

    var locals = JSIL.Make64BitInt($, function () {
        return mscorlib.System.UInt64;
    });
    var lazy = locals.lazy;
    var me = locals.me;
    var ctor = locals.ctor;
    var mktemp = locals.mktemp;

    var maxValue = lazy(function () {
        return ctor(0xFFFFFF, 0xFFFFFF, 0xFFFF);
    });

    var minValue = lazy(function () {
        return ctor(0, 0, 0);
    });

    var zero = lazy(function () {
        return ctor(0, 0, 0);
    });

    var one = lazy(function () {
        return ctor(1, 0, 0);
    });

    var tempRS = mktemp();
    var tempDiv = mktemp();

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_Division",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function UInt64_op_Division(n, d, result) {
        var equality = me().op_Equality,
          greaterThanOrEqual = me().op_GreaterThanOrEqual,
          subtraction = me().op_Subtraction,
          leftShift = me().op_LeftShift;

        if (equality(d, minValue()))
            throw new Error("System.DivideByZeroException");

        var q = result;
        if (q)
            q.a = q.b = q.c = 0;
        else
            q = ctor(0, 0, 0);

        var r = tempDiv();
        r.a = r.b = r.c = 0;

        for (var i = 63; i >= 0; i--) {
            r = leftShift(r, 1, r);

            var li = i < 24 ? 0 :
                      i < 48 ? 1 : 2;
            var lk = i < 24 ? "a" :
                      i < 48 ? "b" : "c";
            var s = (i - 24 * li);

            r.a |= (n[lk] & (1 << s)) >>> s;

            if (greaterThanOrEqual(r, d)) {
                r = subtraction(r, d, r);
                q[lk] |= 1 << s;
            }
        }

        return q;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_Modulus",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function UInt64_op_Modulus(n, d, result) {
        var equality = me().op_Equality,
          greaterThanOrEqual = me().op_GreaterThanOrEqual,
          subtraction = me().op_Subtraction,
          leftShift = me().op_LeftShift;

        if (equality(d, minValue()))
            throw new Error("System.DivideByZeroException");

        var r = result || ctor(0, 0, 0);
        r.a = r.b = r.c = 0;

        for (var i = 63; i >= 0; i--) {
            r = leftShift(r, 1, r);

            var li = i < 24 ? 0 :
                      i < 48 ? 1 : 2;
            var lk = i < 24 ? "a" :
                      i < 48 ? "b" : "c";
            var s = (i - 24 * li);

            r.a |= (n[lk] & (1 << s)) >>> s;

            if (greaterThanOrEqual(r, d)) {
                r = subtraction(r, d, r);
            }
        }

        return r;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_RightShift",
      (new JSIL.MethodSignature($.Type, [$.Type, mscorlib.TypeRef("System.Int32")], [])),
      function UInt64_op_RightShift(a, n, result) {

          n = n & 0x3f;

          if (n > 24) {
              return mscorlib.System.UInt64.op_RightShift(
                mscorlib.System.UInt64.op_RightShift(a, 24, tempRS()), n - 24, result
              );
          }

          var m = (1 << n) - 1;
          var cr = (a.c & m) << (24 - n);
          var ct = a.c >>> n;
          var br = (a.b & m) << (24 - n);
          var bt = a.b >>> n;
          var at = a.a >>> n;

          if (!result) {
              result = ctor(at | br, bt | cr, ct);
          } else {
              result.a = at | br;
              result.b = bt | cr;
              result.c = ct;
          }

          return result;
      });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_LessThan",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function UInt64_op_LessThan(a, b) {
        var adiff = a.c - b.c;
        if (adiff < 0)
            return true;

        if (adiff > 0)
            return false;

        var bdiff = a.b - b.b;
        if (bdiff < 0)
            return true;

        if (bdiff > 0)
            return false;

        return a.a < b.a;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_LessThanOrEqual",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function UInt64_op_LessThanOrEqual(a, b) {
        var adiff = a.c - b.c;
        if (adiff < 0)
            return true;

        if (adiff > 0)
            return false;

        var bdiff = a.b - b.b;
        if (bdiff < 0)
            return true;

        if (bdiff > 0)
            return false;

        return a.a <= b.a;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_GreaterThan",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function UInt64_op_GreaterThan(a, b) {
        var adiff = a.c - b.c;
        if (adiff > 0)
            return true;

        if (adiff < 0)
            return false;

        var bdiff = a.b - b.b;
        if (bdiff > 0)
            return true;

        if (bdiff < 0)
            return false;

        return a.a > b.a;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_GreaterThanOrEqual",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function UInt64_op_GreaterThanOrEqual(a, b) {
        var adiff = a.c - b.c;
        if (adiff > 0)
            return true;

        if (adiff < 0)
            return false;

        var bdiff = a.b - b.b;
        if (bdiff > 0)
            return true;

        if (bdiff < 0)
            return false;

        return a.a >= b.a;
    });

    $.Method({ Static: false, Public: true }, "toString",
    new JSIL.MethodSignature("System.String", []),
    function UInt64_toString() {
        var a = this;
        var ten = me().FromNumber(10);

        var s = "";

        do {
            var r = me().op_Modulus(a, ten);
            s = r.a.toString() + s;
            a = me().op_Division(a, ten);
        } while (me().op_GreaterThan(a, minValue()));

        return s;
    });

    $.Method({ Static: false, Public: true }, "ToString",
      new JSIL.MethodSignature("System.String", []),
      function UInt64_ToString() {
          return this.toString();
      }
    );

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "ToHex",
    new JSIL.MethodSignature("System.String", []),
    function UInt64_ToHex() {
        var s = this.a.toString(16);

        if (this.b > 0 || this.c > 0) {
            if (s.length < 6)
                s = (new Array(6 - s.length + 1)).join('0') + s;

            s = this.b.toString(16) + s;

            if (this.c > 0) {
                if (s.length < 12)
                    s = (new Array(12 - s.length + 1)).join('0') + s;

                s = this.c.toString(16) + s;
            }
        }

        return s;
    });

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "ToInt64",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Int64"), []),
    function UInt64_ToInt64() {
        return new mscorlib.System.Int64(this.a, this.b, this.c);
    });

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "Clone",
    new JSIL.MethodSignature($.Type, []),
    function UInt64_Clone() {
        return ctor(this.a, this.b, this.c);
    });

    $.Method({ Static: false, Public: true }, "Object.Equals",
    new JSIL.MethodSignature(System.Boolean, [System.Object]),
    function UInt64_Equals(rhs) {
        return UInt64.op_Equality(this, rhs);
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: false }, "FromInt32",
    (new JSIL.MethodSignature($.Type, [$.Int32], [])),
    function UInt64_FromInt32(n) {
        if (n < 0)
            JSIL.RuntimeError("cannot construct UInt64 from negative number");

        // only using 48 bits

        var n0 = Math.floor(n);
        return ctor(
            (n0 & 0xffffff),
            (n0 >>> 24) & 0xffffff,
            0);
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: false }, "FromNumber",
    (new JSIL.MethodSignature($.Type, [$.Double], [])),
    function UInt64_FromNumber(n) {
        var sum = n < 0 ? 0x100000000 : 0;
        return me().FromNumberImpl(sum + n, ctor);
    });

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "ToUInt32",
    (new JSIL.MethodSignature($.UInt32, [], [])),
    function UInt64_FromUInt32() {
        return ((0x1000000 * this.b) + this.a) >>> 0;
    });

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "ToNumber",
    (new JSIL.MethodSignature($.Double, [], [])),
    function UInt64_ToNumber(maxValue, signed) {
        if (arguments.length === 0 || maxValue == -1)
            return 0x1000000 * (0x1000000 * this.c + this.b) + this.a;

        var truncated = me()
          .op_BitwiseAnd(this, me().FromNumber(maxValue))
          .ToNumber();

        if (signed) {
            var maxPlusOne = maxValue + 1;
            var signedMaxValue = maxValue >>> 1;
            if (truncated > signedMaxValue)
                return truncated - signedMaxValue;
            else
                return truncated;
        }
        else {
            return truncated;
        }
    });

    $.Method({ Static: false, Public: true }, "GetHashCode",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function UInt64_GetHashCode() {
      return this.a | ((this.b & 0xff) << 24);
    });
});

JSIL.MakeStruct("System.ValueType", "System.UInt64", true, [], function ($) {
    $.Field({ Static: false, Public: false }, "a", $.Int32);
    $.Field({ Static: false, Public: false }, "b", $.Int32);
    $.Field({ Static: false, Public: false }, "c", $.Int32);

    JSIL.MakeCastMethods(
      $.publicInterface, $.typeObject, "int64"
    );

    JSIL.MakeIConvertibleMethods($);
});
﻿

JSIL.ImplementExternals("System.Int64", function ($) {

    // The unsigned range 0 to 0x7FFFFFFFFFFFFFFF (= Int64.MaxValue) is positive: 0 to 9223372036854775807
    // The directly following unsigned range 0x8000000000000000 (= Int64.MaxValue + 1 = Int64.MinValue) to 0xFFFFFFFFFFFFFFFF is negative: -9223372036854775808 to -1
    //
    //  signed value
    //  ^
    //  |      /
    //  |    /
    //  |  /
    //  |/z
    //  ------------------> unsigned value
    //  |              /
    //  |            /
    //  |          /
    //  |        /
    //

    var mscorlib = JSIL.GetCorlib();

    var locals = JSIL.Make64BitInt($, function () {
        return mscorlib.System.Int64;
    });
    var lazy = locals.lazy;
    var me = locals.me;
    var ctor = locals.ctor;
    var mktemp = locals.mktemp;

    var zero = lazy(function () {
        return ctor(0, 0, 0);
    });

    var one = lazy(function () {
        return ctor(1, 0, 0);
    });

    var minusOne = lazy(function () {
        return ctor(0xFFFFFF, 0xFFFFFF, 0xFFFF);
    });

    var signedMaxValue = lazy(function () {
        return ctor(0xFFFFFF, 0xFFFFFF, 0x7FFF);
    });

    var unsignedMaxValue = lazy(function () {
        return ctor(0xFFFFFF, 0xFFFFFF, 0xFFFF);
    });

    var tempRS = mktemp();
    var tempRS1 = mktemp();
    var tempRS2 = mktemp();
    var tempRS3 = mktemp();

    var tempTS = mktemp();
    var tempDiv = mktemp();
    var tempDiv2 = mktemp();
    var tempDiv3 = mktemp();
    var tempDiv4 = mktemp();
    var tempMod = mktemp();
    var tempMod2 = mktemp();
    var tempMod3 = mktemp();
    var tempMod4 = mktemp();
    var tempN = mktemp();

    var isNegative = function Int64_IsNegative(a) {
        return mscorlib.System.UInt64.op_GreaterThan(a, signedMaxValue());
    };

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_UnaryNegation",
    (new JSIL.MethodSignature($.Type, [$.Type], [])),
    function Int64_op_UnaryNegation(a, result) {
        var complement = me().op_Subtraction(unsignedMaxValue(), a, tempN());
        return me().op_Addition(complement, one(), result);
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_Division",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function Int64_op_Division(n, d, result) {
        if (me().op_Equality(d, zero()))
            throw new Error("System.DivideByZeroException");

        if (!result)
            result = ctor(0, 0, 0);
        else
            result.a = result.b = result.c = 0;

        if (isNegative(d))
            return me().op_Division(
              me().op_UnaryNegation(n, tempDiv()), me().op_UnaryNegation(d, tempDiv2()), result
            );
        else if (isNegative(n))
            return me().op_UnaryNegation(
              me().op_Division(
                me().op_UnaryNegation(n, tempDiv3()), d, tempDiv4()
              ), result
            );
        else
            return mscorlib.System.UInt64.op_Division(n, d, result);
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_Modulus",
    (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
    function Int64_op_Modulus(n, d, result) {
        if (me().op_Equality(d, zero()))
            throw new Error("System.DivideByZeroException");

        if (!result)
            result = ctor(0, 0, 0);

        if (isNegative(d))
            return me().op_Modulus(
              me().op_UnaryNegation(n, tempMod()), me().op_UnaryNegation(d, tempMod2()), result
            );
        else if (isNegative(n))
            return me().op_UnaryNegation(
              me().op_Modulus(
                me().op_UnaryNegation(n, tempMod3()), d, tempMod4()
              ), result
            );
        else
            // fix return type error
            return mscorlib.System.UInt64.op_Modulus(n, d, result);
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_RightShift",
    (new JSIL.MethodSignature($.Type, [$.Type, mscorlib.TypeRef("System.Int32")], [])),
    function Int64_op_RightShift(a, n, result) {
        // Int64 (signed) uses arithmetic shift, UIn64 (unsigned) uses logical shift
        if (!result)
            result = ctor(0, 0, 0);

        if (n === 0) {
            var result2 = a;
        } else if (n > 32) {
            result2 = me().op_RightShift(me().op_RightShift(a, 32), n - 32);
        } else {
            var unsignedShift = mscorlib.System.UInt64.op_RightShift(a, n);

            if (isNegative(a)) {
                var outshift = mscorlib.System.UInt64.op_RightShift(mscorlib.System.UInt64.Create(16777215, 16777215, 65535), n);
                var inshift = mscorlib.System.UInt64.op_LeftShift(outshift, 64 - n);
                var uresult = mscorlib.System.UInt64.op_BitwiseOr(unsignedShift, inshift);
            } else {
                uresult = unsignedShift;
            }
            result2 = (uresult).ToInt64();
        }
        return result2;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_GreaterThan",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function Int64_op_GreaterThan(a, b) {
        var an = isNegative(a);
        var bn = isNegative(b);

        if (an === bn)
            return mscorlib.System.UInt64.op_GreaterThan(a, b);
        else
            return bn;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_GreaterThanOrEqual",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function Int64_op_GreaterThanOrEqual(a, b) {
        var an = isNegative(a);
        var bn = isNegative(b);

        if (an === bn)
            return mscorlib.System.UInt64.op_GreaterThanOrEqual(a, b);
        else
            return bn;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_LessThan",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function Int64_op_LessThan(a, b) {
        var an = isNegative(a);
        var bn = isNegative(b);

        if (an === bn)
            return mscorlib.System.UInt64.op_LessThan(a, b);
        else
            return an;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: true }, "op_LessThanOrEqual",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), [$.Type, $.Type], [])),
    function Int64_op_LessThanOrEqual(a, b) {
        var an = isNegative(a);
        var bn = isNegative(b);

        if (an === bn)
            return mscorlib.System.UInt64.op_LessThanOrEqual(a, b);
        else
            return an;
    });

    // Might need to be implemented externally
    $.Method({ Static: false, Public: true }, "Equals",
    new JSIL.MethodSignature(mscorlib.TypeRef("System.Boolean"), []),
    function Int64_Equals(a) {
        return me().op_Equality(this, a);
    });

    // Might need to be implemented externally
    $.Method({ Static: false, Public: true }, "toString",
    new JSIL.MethodSignature("System.String", []),
    function Int64_toString() {
        var s = "";
        var a = this;
        if (isNegative(this)) {
            s += "-";
            a = me().op_UnaryNegation(this, tempTS());
        }
        s += mscorlib.System.UInt64.prototype.toString.apply(a);
        return s;
    });

    $.Method({ Static: false, Public: true }, "ToString",
      new JSIL.MethodSignature("System.String", []),
      function Int64_ToString() {
          return this.toString();
      }
    );

    // Not present in mscorlib
    $.Method({ Static: true, Public: false }, "FromInt32",
    (new JSIL.MethodSignature($.Type, [$.Int32], [])),
    function Int64_FromInt32(n) {
        var sign = n < 0 ? -1 : 1;
        n = Math.abs(n);

        var n0 = Math.floor(n);
        var r = ctor(n0 & 0xffffff, (n0 >>> 24) & 0xffffff, 0);

        if (sign == -1)
            return me().op_UnaryNegation(r);
        else
            return r;
    });

    // Not present in mscorlib
    $.Method({ Static: true, Public: false }, "FromNumber",
    (new JSIL.MethodSignature($.Type, [$.Double], [])),
    function Int64_FromNumber(n) {
        var sign = n < 0 ? -1 : 1;
        var r = me().FromNumberImpl(Math.abs(n), ctor);

        if (sign == -1)
            return me().op_UnaryNegation(r, r);
        else
            return r;
    });

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "ToUInt64",
    new JSIL.MethodSignature($.UInt64, []),
    function Int64_ToUInt64() {
        return new mscorlib.System.UInt64(this.a, this.b, this.c);
    });

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "ToInt32",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function Int64_ToInt32() {
        var neg = isNegative(this);
        var n = neg ? me().op_UnaryNegation(this) : this;
        var r = (0x1000000 * (n.b) + n.a) >>> 0;

        if (neg)
            return -r;
        else
            return r;
    });

    $.Method({ Static: false, Public: true }, "GetHashCode",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function Int64_GetHashCode() {
      return this.a | ((this.b & 0xff) << 24);
    });

    // Not present in mscorlib
    $.Method({ Static: false, Public: true }, "ToNumber",
    (new JSIL.MethodSignature($.Double, [], [])),
    function Int64_ToNumber(maxValue, signed) {
        if (arguments.length === 0 || maxValue == -1) {
            var neg = isNegative(this);
            var n = neg ? me().op_UnaryNegation(this) : this;
            var r = 0x1000000 * (0x1000000 * n.c + n.b) + n.a;

            if (neg)
                return -r;
            else
                return r;
        }

        var truncated = me()
          .op_BitwiseAnd(this, me().FromNumber(maxValue))
          .ToNumber();

        if (signed) {
            var signedMaxValue = maxValue >>> 1;
            if (truncated > signedMaxValue)
                return (truncated - maxValue) - 1;
            else
                return truncated;
        }
        else {
            return truncated;
        }
    });
});

JSIL.MakeStruct("System.ValueType", "System.Int64", true, [], function ($) {
    $.Field({ Static: false, Public: false }, "a", $.Int32);
    $.Field({ Static: false, Public: false }, "b", $.Int32);
    $.Field({ Static: false, Public: false }, "c", $.Int32);

    JSIL.MakeCastMethods(
      $.publicInterface, $.typeObject, "int64"
    );

    JSIL.MakeIConvertibleMethods($);
});




