/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

if (!$jsilcore)
    throw new Error("JSIL.Core is required");

JSIL.ImplementExternals(
  "System.TimeSpan", function ($) {
      var TicksPerMillisecond, TicksPerSecond, TicksPerMinute, TicksPerHour, TicksPerDay;
      var TempI64A, TempI64B;

      var fromTicks = function (ticks) {
          return JSIL.CreateInstanceOfType(
            $jsilcore.System.TimeSpan.__Type__, "$fromTicks",
            [ticks]
          );
      };

      var fromDoubleTicks = function (ticks) {
          return $jsilcore.System.TimeSpan.FromTicks(
            $jsilcore.System.Int64.FromNumber(ticks)
          );
      };

      $.RawMethod(true, ".cctor", function () {
          var tInt64 = $jsilcore.System.Int64;
          TicksPerMillisecond = tInt64.FromNumber(10000);
          TicksPerSecond = tInt64.FromNumber(10000000);
          TicksPerMinute = tInt64.FromNumber(600000000);
          TicksPerHour = tInt64.FromNumber(36000000000);
          TicksPerDay = tInt64.FromNumber(864000000000);

          TempI64A = new tInt64();
          TempI64B = new tInt64();
      });

      $.RawMethod(false, "__CopyMembers__",
        function TimeSpan_CopyMembers(source, target) {
            target._ticks = source._ticks.MemberwiseClone();
            target._cachedTotalMs = source._cachedTotalMs || 0;
            target._cachedTotalS = source._cachedTotalS || 0;
        }
      );

      $.Method({ Static: true, Public: true }, "FromMilliseconds",
        (new JSIL.MethodSignature($.Type, [$.Double], [])),
        function FromMilliseconds(value) {
            return fromDoubleTicks(value * TicksPerMillisecond.valueOf());
        }
      );

      $.Method({ Static: true, Public: true }, "FromSeconds",
        (new JSIL.MethodSignature($.Type, [$.Double], [])),
        function FromSeconds(value) {
            return fromDoubleTicks(value * TicksPerSecond.valueOf());
        }
      );

      $.Method({ Static: true, Public: true }, "FromMinutes",
        (new JSIL.MethodSignature($.Type, [$.Double], [])),
        function FromMinutes(value) {
            return fromDoubleTicks(value * TicksPerMinute.valueOf());
        }
      );

      $.Method({ Static: true, Public: true }, "FromHours",
        (new JSIL.MethodSignature($.Type, [$.Double], [])),
        function FromHours(value) {
            return fromDoubleTicks(value * TicksPerHour.valueOf());
        }
      );

      $.Method({ Static: true, Public: true }, "FromDays",
        (new JSIL.MethodSignature($.Type, [$.Double], [])),
        function FromDays(value) {
            return fromDoubleTicks(value * TicksPerDay.valueOf());
        }
      );

      $.Method({ Static: true, Public: true }, "FromTicks",
        (new JSIL.MethodSignature($.Type, [$.Int64], [])),
        fromTicks
      );

      $.Method({ Static: true, Public: true }, "op_Addition",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function op_Addition(t1, t2) {
            return fromTicks($jsilcore.System.Int64.op_Addition(t1._ticks, t2._ticks, TempI64A));
        }
      );

      $.Method({ Static: true, Public: true }, "op_Equality",
        (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
        function op_Equality(t1, t2) {
            return $jsilcore.System.Int64.op_Equality(t1._ticks, t2._ticks);
        }
      );

      $.Method({ Static: true, Public: true }, "op_GreaterThan",
        (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
        function op_GreaterThan(t1, t2) {
            return $jsilcore.System.Int64.op_GreaterThan(t1._ticks, t2._ticks);
        }
      );

      $.Method({ Static: true, Public: true }, "op_GreaterThanOrEqual",
        (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
        function op_GreaterThanOrEqual(t1, t2) {
            return $jsilcore.System.Int64.op_GreaterThanOrEqual(t1._ticks, t2._ticks);
        }
      );

      $.Method({ Static: true, Public: true }, "op_Inequality",
        (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
        function op_Inequality(t1, t2) {
            return $jsilcore.System.Int64.op_Inequality(t1._ticks, t2._ticks);
        }
      );

      $.Method({ Static: true, Public: true }, "op_LessThan",
        (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
        function op_LessThan(t1, t2) {
            return $jsilcore.System.Int64.op_LessThan(t1._ticks, t2._ticks);
        }
      );

      $.Method({ Static: true, Public: true }, "op_LessThanOrEqual",
        (new JSIL.MethodSignature($.Boolean, [$.Type, $.Type], [])),
        function op_LessThanOrEqual(t1, t2) {
            return $jsilcore.System.Int64.op_LessThanOrEqual(t1._ticks, t2._ticks);
        }
      );

      $.Method({ Static: true, Public: true }, "op_Subtraction",
        (new JSIL.MethodSignature($.Type, [$.Type, $.Type], [])),
        function op_Subtraction(t1, t2) {
            return fromTicks($jsilcore.System.Int64.op_Subtraction(t1._ticks, t2._ticks, TempI64A));
        }
      );

      $.Method({ Static: true, Public: true }, "op_UnaryNegation",
        (new JSIL.MethodSignature($.Type, [$.Type], [])),
        function op_UnaryNegation(self) {
            return fromTicks($jsilcore.System.Int64.op_UnaryNegation(self._ticks));
        }
      );

      $.RawMethod(false, "$accumulate", function (multiplier, amount) {
          // FIXME: unnecessary garbage
          var tInt64 = $jsilcore.System.Int64;
          var amount64 = tInt64.FromNumber(amount);
          var scaled;

          if (multiplier)
              scaled = tInt64.op_Multiplication(multiplier, amount64);
          else
              scaled = amount64;

          if (!this._ticks)
              this._ticks = scaled;
          else
              this._ticks = tInt64.op_Addition(this._ticks, scaled);

          this.$invalidate();
      });

      $.RawMethod(false, "$fromTicks",
        function fromTicks(ticks) {
            if (typeof (ticks) === "number")
                JSIL.RuntimeError("Argument must be an Int64");

            this._ticks = ticks.MemberwiseClone();
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
        (new JSIL.MethodSignature(null, [$.Int64], [])),
        function _ctor(ticks) {
            if (typeof (ticks) === "number")
                JSIL.RuntimeError("Argument must be an Int64");

            this._ticks = ticks.MemberwiseClone();
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
        (new JSIL.MethodSignature(null, [
              $.Int32, $.Int32,
              $.Int32
        ], [])),
        function _ctor(hours, minutes, seconds) {
            this.$accumulate(TicksPerHour, hours);
            this.$accumulate(TicksPerMinute, minutes);
            this.$accumulate(TicksPerSecond, seconds);
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
        (new JSIL.MethodSignature(null, [
              $.Int32, $.Int32,
              $.Int32, $.Int32
        ], [])),
        function _ctor(days, hours, minutes, seconds) {
            this.$accumulate(TicksPerDay, days);
            this.$accumulate(TicksPerHour, hours);
            this.$accumulate(TicksPerMinute, minutes);
            this.$accumulate(TicksPerSecond, seconds);
        }
      );

      $.Method({ Static: false, Public: true }, ".ctor",
        (new JSIL.MethodSignature(null, [
              $.Int32, $.Int32,
              $.Int32, $.Int32,
              $.Int32
        ], [])),
        function _ctor(days, hours, minutes, seconds, milliseconds) {
            this.$accumulate(TicksPerDay, days);
            this.$accumulate(TicksPerHour, hours);
            this.$accumulate(TicksPerMinute, minutes);
            this.$accumulate(TicksPerSecond, seconds);
            this.$accumulate(TicksPerMillisecond, milliseconds);
        }
      );

      $.Method({ Static: false, Public: true }, "get_Days",
        (new JSIL.MethodSignature($.Int32, [], [])),
        function get_Days() {
            return this.$divide(TicksPerDay);
        }
      );

      $.Method({ Static: false, Public: true }, "get_Hours",
        (new JSIL.MethodSignature($.Int32, [], [])),
        function get_Hours() {
            return this.$modDiv(TicksPerDay, TicksPerHour);
        }
      );

      $.Method({ Static: false, Public: true }, "get_Milliseconds",
        (new JSIL.MethodSignature($.Int32, [], [])),
        function get_Milliseconds() {
            return this.$modDiv(TicksPerSecond, TicksPerMillisecond);
        }
      );

      $.Method({ Static: false, Public: true }, "get_Minutes",
        (new JSIL.MethodSignature($.Int32, [], [])),
        function get_Minutes() {
            return this.$modDiv(TicksPerHour, TicksPerMinute);
        }
      );

      $.Method({ Static: false, Public: true }, "get_Seconds",
        (new JSIL.MethodSignature($.Int32, [], [])),
        function get_Seconds() {
            return this.$modDiv(TicksPerMinute, TicksPerSecond);
        }
      );

      $.Method({ Static: false, Public: true }, "get_Ticks",
        (new JSIL.MethodSignature($.Int64, [], [])),
        function get_Ticks() {
            return this._ticks;
        }
      );

      $.RawMethod(false, "$modDiv", function $modulus(modulus, divisor) {
          var tInt64 = $jsilcore.System.Int64;
          var result = tInt64.op_Modulus(this._ticks, modulus, TempI64A);

          if (divisor)
              result = tInt64.op_Division(result, divisor, TempI64B);

          return result.ToInt32();
      });

      $.RawMethod(false, "$divide", function $divide(divisor) {
          var tInt64 = $jsilcore.System.Int64;
          var result = tInt64.op_Division(this._ticks, divisor, TempI64A);
          return result.ToInt32();
      });

      $.RawMethod(false, "$toNumberDivided", function $toNumberDivided(divisor) {
          var tInt64 = $jsilcore.System.Int64;
          var integral = tInt64.op_Division(this._ticks, divisor, TempI64A);
          var remainder = tInt64.op_Modulus(this._ticks, divisor, TempI64B);
          var scaledRemainder = remainder.ToNumber() / divisor.valueOf();

          var result = integral.ToNumber() + scaledRemainder;
          return result;
      });

      var invalidCachedTotal = 0;

      $.RawMethod(false, "$invalidate", function () {
          this._cachedTotalMs = invalidCachedTotal;
          this._cachedTotalS = invalidCachedTotal;
      });

      $.Method({ Static: false, Public: true }, "get_TotalMilliseconds",
        (new JSIL.MethodSignature($.Double, [], [])),
        function get_TotalMilliseconds() {
            if (!this._ticks.a && !this._ticks.b && !this._ticks.c)
                return 0;
            else if (this._cachedTotalMs)
                return this._cachedTotalMs;
            else
                return this._cachedTotalMs = this.$toNumberDivided(TicksPerMillisecond);
        }
      );

      $.Method({ Static: false, Public: true }, "get_TotalSeconds",
        (new JSIL.MethodSignature($.Double, [], [])),
        function get_TotalSeconds() {
            if (!this._ticks.a && !this._ticks.b && !this._ticks.c)
                return 0;
            else if (this._cachedTotalS)
                return this._cachedTotalS;
            else
                return this._cachedTotalS = this.$toNumberDivided(TicksPerSecond);
        }
      );

      $.Method({ Static: false, Public: true }, "get_TotalMinutes",
        (new JSIL.MethodSignature($.Double, [], [])),
        function get_TotalMinutes() {
            return this.$toNumberDivided(TicksPerMinute);
        }
      );

      $.Method({ Static: false, Public: true }, "get_TotalHours",
        (new JSIL.MethodSignature($.Double, [], [])),
        function get_TotalHours() {
            return this.$toNumberDivided(TicksPerHour);
        }
      );

      $.Method({ Static: false, Public: true }, "get_TotalDays",
        (new JSIL.MethodSignature($.Double, [], [])),
        function get_TotalDays() {
            return this.$toNumberDivided(TicksPerDay);
        }
      );

      $.Method({ Static: true, Public: true }, "Parse",
        (new JSIL.MethodSignature($.Type, [$.String], [])),
        function TimeSpan_Parse(text) {
            var pieces = (text || "").split(":");
            var days = 0, hours = 0, minutes = 0, seconds = 0, ticks = 0;

            if (pieces[0].indexOf(".") >= 0) {
                var temp = pieces[0].split(".");
                days = parseInt(temp[0], 10);
                hours = parseInt(temp[1], 10);
            } else {
                hours = parseInt(pieces[0], 10);
            }

            minutes = parseInt(pieces[1], 10);

            if (pieces[2].indexOf(".") >= 0) {
                var temp = pieces[2].split(".");
                seconds = parseInt(temp[0], 10);
                ticks = parseInt(temp[1], 10);
            } else {
                seconds = parseInt(pieces[2], 10);
            }

            var result = new System.TimeSpan();
            result.$accumulate(TicksPerDay, days);
            result.$accumulate(TicksPerHour, hours);
            result.$accumulate(TicksPerMinute, minutes);
            result.$accumulate(TicksPerSecond, seconds);
            result.$accumulate(null, ticks);
            return result;
        }
      );

      $.Method({ Static: false, Public: true }, "toString",
        (new JSIL.MethodSignature($.String, [], [])),
        function TimeSpan_toString() {
            var ticks = this.$modDiv(TicksPerSecond);
            var seconds = this.get_Seconds();
            var minutes = this.get_Minutes();
            var hours = this.get_Hours();
            var days = this.get_Days();

            var formatString;

            if (days) {
                formatString = "{0}.{1:00}:{2:00}:";
            } else {
                formatString = "{1:00}:{2:00}:";
            }

            if (ticks) {
                formatString += "{3:00}.{4:0000000}";
            } else {
                formatString += "{3:00}";
            }

            return System.String.Format(
              formatString,
              days, hours, minutes, seconds, ticks
            );
        }
      );
  }
);

JSIL.MakeStruct("System.ValueType", "System.TimeSpan", true, [], function ($) {
    $.Field({ Static: false, Public: false }, "_ticks", $.Int64);

    $.Property({ Public: true, Static: false }, "Ticks");

    $.Property({ Public: true, Static: false }, "Milliseconds");

    $.Property({ Public: true, Static: false }, "TotalMilliseconds");

    $.Property({ Public: true, Static: false }, "Seconds");

    $.Property({ Public: true, Static: false }, "Minutes");

    $.Property({ Public: true, Static: false }, "Hours");

    $.Property({ Public: true, Static: false }, "Days");

    $.Property({ Public: true, Static: false }, "TotalSeconds");

    $.Property({ Public: true, Static: false }, "TotalMinutes");
});
JSIL.ImplementExternals("System.DateTime", function ($) {
    $.RawMethod(false, "$fromUnixMilliseconds", function (msSince1970, boolIsUtc) {
      this.dateData = $jsilcore.System.UInt64.op_Multiplication(
        $jsilcore.System.UInt64.op_Addition(
          $jsilcore.System.UInt64.FromNumber(msSince1970),
          $jsilcore.System.UInt64.FromNumber(62135596800000)),
        $jsilcore.System.UInt64.FromInt32(10000));
        this.kind = boolIsUtc ? $jsilcore.System.DateTimeKind.Utc : $jsilcore.System.DateTimeKind.Local;
    });

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Int64], [])),
      function _ctor(ticks) {
          this.dateData = ticks.ToUInt64();
          this.kind = $jsilcore.System.DateTimeKind.Unspecified;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Int64, $jsilcore.TypeRef("System.DateTimeKind")], [])),
      function _ctor(ticks, kind) {
          this.dateData = ticks.ToUInt64();
          this.kind = kind;
      }
    );

    $.Method({ Static: false, Public: false }, ".ctor",
      (new JSIL.MethodSignature(null, [$.UInt64], [])),
      function _ctor(dateData) {
          this.dateData = dateData;
          this.kind = $jsilcore.System.DateTimeKind.Unspecified;
      }
    );

    $.Method({ Static: false, Public: true }, "ToBinary",
      (new JSIL.MethodSignature($.Int64, [], [])),
      function ToBinary() {
          // FIXME: this.kind is not properly copied on MemberwiseClone
          if (this.kind === undefined)
              this.kind = $jsilcore.System.DateTimeKind.Unspecified;
          return ($jsilcore.System.Int64.op_BitwiseOr(this.dateData, $jsilcore.System.Int64.op_LeftShift(this.kind, 62)));
      }
    );

    $.Method({ Static: true, Public: true }, "FromBinary",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.DateTime"), [$.Int64], [])),
      function FromBinary(dateData) {
          var ticks = $jsilcore.System.Int64.op_BitwiseAnd(dateData,
                                                           $jsilcore.System.Int64.Create(16777215, 16777215, 16383) /* 0x3FFFFFFFFFFFFFFF */);
          var kind = $jsilcore.System.Int64.op_RightShift(dateData, 62);

          return new System.DateTime(ticks, $jsilcore.System.DateTimeKind.$Cast(kind));
      }
    );

    var normalYearDays = [
      0, 31, 59,
      90, 120, 151,
      181, 212, 243,
      273, 304, 334,
      365
    ];
    var leapYearDays = [
      0, 31, 60,
      91, 121, 152,
      182, 213, 244,
      274, 305, 335,
      366
    ];

    function isLeapYear(year) {
        if (((year % 100) !== 0) || ((year % 400) === 0)) {
            return (year % 4) === 0;
        }
        return false;
    };

    function ymdToTicks(year, month, day) {
        if ((year < 1) || (year > 9999))
            throw new System.ArgumentException("year");

        if ((month < 1) || (month > 12))
            throw new System.ArgumentException("month");

        var days = isLeapYear(year) ? leapYearDays : normalYearDays;
        var daysThisMonth = days[month] - days[month - 1];

        if ((day < 1) || (day > daysThisMonth))
            throw new System.ArgumentException("day");

        year -= 1;

        var yearDays = year * 365;
        var leapYearDayOffset = ((year / 4) | 0) - ((year / 100) | 0) + ((year / 400) | 0);
        var monthDays = days[month - 1];

        var totalDays = $jsilcore.System.UInt64.FromInt32(
          (yearDays + leapYearDayOffset + monthDays + (day - 1)) | 0
        );
        var ticksPerDay = $jsilcore.System.UInt64.Parse("864000000000");

        var result = $jsilcore.System.UInt64.op_Multiplication(
          totalDays,
          ticksPerDay
        );

        return result;
    };

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [
            $.Int32, $.Int32,
            $.Int32
      ], [])),
      function _ctor(year, month, day) {
          this.dateData = ymdToTicks(year, month, day);
          this.kind = $jsilcore.System.DateTimeKind.Unspecified;
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      (new JSIL.MethodSignature(null, [
            $.Int32, $.Int32,
            $.Int32, $jsilcore.TypeRef("System.Globalization.Calendar")
      ], [])),
      function _ctor(year, month, day, calendar) {
          // FIXME: calendar
          this.dateData = ymdToTicks(year, month, day);
          this.kind = $jsilcore.System.DateTimeKind.Unspecified;
      }
    );

    $.Method({ Static: true, Public: true }, "get_Now",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.DateTime"), [], [])),
      function get_Now() {
          // FIXME
          return JSIL.CreateInstanceOfType(
            $jsilcore.System.DateTime.__Type__, "$fromUnixMilliseconds", [JSIL.Host.getTime() - JSIL.Host.getTimezoneOffsetInMilliseconds(), false]
          );
      }
    );

    $.Method({ Static: true, Public: true }, "get_UtcNow",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.DateTime"), [], [])),
      function get_UtcNow() {
          // FIXME
          return JSIL.CreateInstanceOfType(
            $jsilcore.System.DateTime.__Type__, "$fromUnixMilliseconds", [JSIL.Host.getTime(), true]
          );
      }
    );

    $.Method({ Static: false, Public: true }, "ToLongTimeString",
      (new JSIL.MethodSignature($.String, [], [])),
      function ToLongTimeString() {
          // FIXME
          return "ToLongTimeString";
      }
    );

});

JSIL.MakeStruct($jsilcore.TypeRef("System.ValueType"), "System.DateTime", true, [], function ($) {
});

JSIL.MakeEnum(
  "System.DateTimeKind", true, {
      Unspecified: 0,
      Utc: 1,
      Local: 2
  }, false
);