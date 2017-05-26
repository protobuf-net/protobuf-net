/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core is required");

if (!$jsilcore)
  throw new Error("JSIL.Core is required");

JSIL.ParseCustomNumberFormat = function (customFormat) {
  var inQuotedString = false, quoteCharacter = null, stringStartOffset = -1;
  var containsDecimal = false;

  var commands = [];

  var digit = function (state) {
    var digits = state.digits;
    var result = digits.shift();

    if (state.leadingMinus) {
      state.leadingMinus = false;

      if (result !== null)
        result = "-" + result;
      else
        result = "-";
    }

    return result;
  };

  var zeroOrDigit = function (state) {
    var digits = state.digits;
    var digit = digits.shift();

    var result;
    if (digit === null)
      result = "0";
    else
      result = digit;

    if (state.leadingMinus) {
      state.leadingMinus = false;
      result = "-" + result;
    }

    return result;
  };

  var decimal = function (state) {
    state.afterDecimal = true;

    if (state.omitDecimal)
      return null;
    else
      return ".";
  };

  var rawCharacter = function (state) {
    var character = this;

    return character;
  };

  var quotedString = function (state) {
    var text = this;

    return text;
  };

  var includePlaceSeparators = false;
  var digitCount = 0, digitsBeforeDecimal = 0, digitsAfterDecimal = 0, zeroesAfterDecimal = 0;

  for (var i = 0, l = customFormat.length; i < l; i++) {
    var ch = customFormat[i];

    if (inQuotedString) {
      if (ch === quoteCharacter) {
        inQuotedString = false;

        var quotedText = customFormat.substr(stringStartOffset, i - stringStartOffset);
        commands.push(quotedString.bind(quotedText));
      }

      continue;
    }

    switch (ch) {
      case "\t":
      case " ":
        commands.push(rawCharacter.bind(ch));
        break;

      case ",":
        includePlaceSeparators = true;
        break;

      case "'":
      case '"':
        quoteCharacter = ch;
        inQuotedString = true;
        stringStartOffset = i + 1;
        break;

      case '#':
        digitCount++;

        commands.push(digit);
        continue;

      case '0':
        digitCount++;
        if (containsDecimal)
          zeroesAfterDecimal++;

        commands.push(zeroOrDigit);
        continue;

      case '.':
        if (containsDecimal)
          JSIL.RuntimeError("Multiple decimal places in format string");
        else
          containsDecimal = true;

        digitsBeforeDecimal = digitCount;
        digitCount = 0;
        commands.push(decimal);

        continue;

      default:
        return null;
    }
  }

  if (containsDecimal)
    digitsAfterDecimal = digitCount;
  else
    digitsBeforeDecimal = digitCount;

  var formatter = function (value) {
    var formatted = value.toString(10);
    var pieces = formatted.split(".");

    var preDecimal = Array.prototype.slice.call(pieces[0]), postDecimal;
    var actualDigitsAfterDecimal = 0;

    var leadingMinus = preDecimal[0] === "-";
    if (leadingMinus)
      preDecimal.shift();

    if (pieces.length > 1) {
      // If we have too few places after the decimal for all the digits,
      //  we need to recreate the string using toFixed so that it gets rounded.
      if (pieces[1].length > digitsAfterDecimal) {
        pieces = value.toFixed(digitsAfterDecimal).split(".");
      }

      if (digitsAfterDecimal) {
        postDecimal = Array.prototype.slice.call(pieces[1]);
        actualDigitsAfterDecimal = postDecimal.length;
      } else {
        postDecimal = [];
        actualDigitsAfterDecimal = 0;
      }

    } else
      postDecimal = [];

    while (preDecimal.length < digitsBeforeDecimal)
      preDecimal.unshift(null);

    while (postDecimal.length < digitsAfterDecimal)
      postDecimal.push(null);

    // To properly emulate place separators in integer formatting,
    //  we need to insert the commas into the digits array.
    if (includePlaceSeparators) {
      for (var l = preDecimal.length, i = l - 4; i >= 0; i -= 3) {
        var digit = preDecimal[i];

        if (digit !== null)
          preDecimal[i] = digit + ",";
      }
    }

    // If we don't have enough place markers for all our digits,
    //  we turn the extra digits into a single 'digit' entry so
    //  that they are still outputted.

    if (preDecimal.length > digitsBeforeDecimal) {
      var toRemove = preDecimal.length - digitsBeforeDecimal;
      var removed = preDecimal.splice(digitsBeforeDecimal, toRemove).join("");

      var preDecimalDigit = preDecimal[preDecimal.length - 1];

      if (preDecimalDigit !== null)
        preDecimal[preDecimal.length - 1] = preDecimalDigit + removed;
      else
        preDecimal[preDecimal.length - 1] = removed;
    }

    var state = {
      afterDecimal: false,
      omitDecimal: (actualDigitsAfterDecimal <= 0) && (zeroesAfterDecimal <= 0),
      leadingMinus: leadingMinus
    };

    Object.defineProperty(
      state, "digits", {
        configurable: false,
        enumerable: true,

        get: function () {
          if (state.afterDecimal)
            return postDecimal;
          else
            return preDecimal;
        }
      }
    );

    var result = "";

    for (var i = 0, l = commands.length; i < l; i++) {
      var command = commands[i];

      var item = command(state);
      if (item)
        result += item;
    }

    return result;
  };

  return formatter;
};

JSIL.NumberToFormattedString = function (value, alignment, valueFormat, formatProvider) {
  // FIXME: formatProvider

  if (
    (arguments.length === 1) ||
    ((typeof (alignment) === "undefined") && (typeof (valueFormat) === "undefined"))
  )
    return value.toString();

  var formatInteger = function (value, radix, digits) {
    digits = parseInt(digits);
    if (isNaN(digits))
      digits = 0;

    var result = parseInt(value).toString(radix);

    while (result.length < digits)
      result = "0" + result;

    return result;
  };

  var formatFloat = function (value, digits) {
    digits = parseInt(digits);
    if (isNaN(digits))
      digits = 2;

    return parseFloat(value).toFixed(digits);
  };

  var insertPlaceSeparators = function (valueString) {
    var pieces = valueString.split(".");

    var newIntegralPart = "";

    for (var i = 0, l = pieces[0].length; i < l; i++) {
      var ch = pieces[0][i];
      var p = (l - i) % 3;

      if ((i > 0) && (p === 0))
        newIntegralPart += ",";

      newIntegralPart += ch;
    }

    pieces[0] = newIntegralPart;

    return pieces.join(".");
  };

  var parsedCustomFormat = null;
  var result;

  if (valueFormat)
    parsedCustomFormat = JSIL.ParseCustomNumberFormat(valueFormat);

  if (parsedCustomFormat) {
    result = parsedCustomFormat(value);

  } else if (valueFormat) {
    switch (valueFormat[0]) {
      case 'd':
      case 'D':
        result = formatInteger(value, 10, valueFormat.substr(1));
        break;

      case 'x':
        result = formatInteger(value >>> 0, 16, valueFormat.substr(1)).toLowerCase();
        break;

      case 'X':
        result = formatInteger(value >>> 0, 16, valueFormat.substr(1)).toUpperCase();
        break;

      case 'f':
      case 'F':
        result = formatFloat(value, valueFormat.substr(1));
        break;

      case 'n':
      case 'N':
        result = formatFloat(value, valueFormat.substr(1));
        result = insertPlaceSeparators(result);
        break;

      default:
        JSIL.RuntimeError("Unsupported format string: " + valueFormat);

    }

  } else {
    result = String(value);
  }

  if (typeof (alignment) === "string")
    alignment = parseInt(alignment);

  if (typeof (alignment) === "number") {
    var absAlignment = Math.abs(alignment);
    if (result.length >= absAlignment)
      return result;

    var paddingSize = absAlignment - result.length;
    var padding = "";
    for (var i = 0; i < paddingSize; i++)
      padding += " ";

    if (alignment > 0)
      return padding + result;
    else
      return result + padding;
  }

  return result;
};
JSIL.StringFromByteArray = function (bytes, startIndex, length) {
  var result = "";

  if (arguments.length < 2)
    startIndex = 0;
  if (arguments.length < 3)
    length = bytes.length;

  for (var i = 0; i < length; i++) {
    var ch = bytes[i + startIndex] | 0;

    result += String.fromCharCode(ch);
  }

  return result;
};
JSIL.StringFromNullTerminatedByteArray = function (bytes, startIndex, length) {
  var result = "";

  if (arguments.length < 2)
    startIndex = 0;
  if (arguments.length < 3)
    length = bytes.length;

  for (var i = 0; i < length; i++) {
    var ch = bytes[i + startIndex] | 0;
    if (ch === 0)
      break;

    result += String.fromCharCode(ch);
  }

  return result;
};
JSIL.StringFromCharArray = function (chars, startIndex, length) {
  if (arguments.length < 2)
    startIndex = 0;
  if (arguments.length < 3)
    length = chars.length;

  if (arguments.length > 1) {
    var arr = chars.slice(startIndex, startIndex + length);
    return arr.join("");
  } else {
    return chars.join("");
  }
};
JSIL.StringFromNullTerminatedPointer = function (chars) {
  var result = "";

  var i = 0;
  while (true) {
    var ch = chars.getElement(i++) | 0;
    if (ch === 0)
      break;

    result += String.fromCharCode(ch);
  }

  return result;
};

JSIL.ImplementExternals(
  "System.String", function ($) {
    $.RawMethod(true, ".cctor2", function () {
      this.Empty = "";
    });

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Char")]), "System.Int32", "System.Int32"], [], $jsilcore),
      function (chars, start, length) {
        return new String(JSIL.StringFromCharArray(chars, start, length));
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Char")])], [], $jsilcore),
      function (chars) {
        return new String(JSIL.StringFromCharArray(chars, 0, chars.length));
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Pointer", [$jsilcore.TypeRef("System.SByte")])], [], $jsilcore),
      function (bytes) {
        return new String(JSIL.StringFromNullTerminatedPointer(bytes));
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Pointer", [$jsilcore.TypeRef("System.Char")])], [], $jsilcore),
      function (chars) {
        // FIXME: Is this correct? Do char pointers yield integers or string literals?
        return new String(JSIL.StringFromNullTerminatedPointer(chars));
      }
    );

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, ["System.Char", "System.Int32"], [], $jsilcore),
      function (ch, length) {
        var arr = new Array(length);
        for (var i = 0; i < length; i++)
          arr[i] = ch;

        return new String(arr.join(""));
      }
    );

    $.RawMethod(true, "CheckType",
      function (value) {
        return (typeof (value) === "string");
      }
    );

    var compareInternal = function (lhs, rhs, comparison) {
      if (lhs == null && rhs == null)
        return 0;
      else if (lhs == null)
        return -1;
      else if (rhs == null)
        return 1;

      switch (comparison.valueOf()) {
        case 1: // System.StringComparison.CurrentCultureIgnoreCase:
        case 3: // System.StringComparison.InvariantCultureIgnoreCase:
        case 5: // System.StringComparison.OrdinalIgnoreCase:
          lhs = lhs.toLowerCase();
          rhs = rhs.toLowerCase();
          break;
      }

      if (lhs < rhs)
        return -1;
      else if (lhs > rhs)
        return 1;
      else
        return 0;
    };

    $.Method({ Static: true, Public: true }, "Compare",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Int32"), [$jsilcore.TypeRef("System.String"), $jsilcore.TypeRef("System.String")], []),
      function (lhs, rhs) {
        return compareInternal(lhs, rhs, System.StringComparison.Ordinal);
      }
    );

    $.Method({ Static: true, Public: true }, "Compare",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Int32"), [
          $jsilcore.TypeRef("System.String"), $jsilcore.TypeRef("System.String"),
          $jsilcore.TypeRef("System.Boolean")
      ], []),
      function (lhs, rhs, ignoreCase) {
        return compareInternal(
          lhs, rhs, ignoreCase ?
            System.StringComparison.OrdinalIgnoreCase :
            System.StringComparison.Ordinal
        );
      }
    );

    $.Method({ Static: true, Public: true }, "Compare",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Int32"), [
          $jsilcore.TypeRef("System.String"), $jsilcore.TypeRef("System.String"),
          $jsilcore.TypeRef("System.StringComparison")
      ], []),
      compareInternal
    );

    var concatInternal = function (firstValue) {
      if (JSIL.IsArray(firstValue) && arguments.length == 1) {
        return JSIL.ConcatString.apply(null, firstValue);
      } else {
        return JSIL.ConcatString(Array.prototype.slice.call(arguments));
      }
    };

    $.Method({ Static: true, Public: true }, "Concat",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.String"), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["T"]),
      concatInternal
    );

    $.Method({ Static: true, Public: true }, "Concat",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.String"), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [$jsilcore.TypeRef("System.String")])], []),
      concatInternal
    );

    $.Method({ Static: true, Public: true }, "EndsWith",
      new JSIL.MethodSignature("System.Boolean", ["System.String", "System.String"], [], $jsilcore),
      function (str, text) {
        return str.lastIndexOf(text) === str.length - text.length;
      }
    );

    $.Method({ Static: true, Public: true }, "Format",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.String"), [$jsilcore.TypeRef("System.Array") /* AnyType[] */], []),
      function (format) {
        format = String(format);

        if (arguments.length === 1)
          return format;

        var values = Array.prototype.slice.call(arguments, 1);

        if ((values.length == 1) && JSIL.IsArray(values[0]))
          values = values[0];

        return JSIL.$FormatStringImpl(format, values);
      }
    );

    $.Method({ Static: true, Public: true }, "IndexOfAny",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Int32"), [$jsilcore.TypeRef("System.Array", [$jsilcore.System.Char]), $jsilcore.TypeRef("System.Int32")], []),
      function (str, chars, startIndex) {
        var result = null;
        for (var i = startIndex || 0; i < chars.length; i++) {
          var index = str.indexOf(chars[i]);
          if ((result === null) || (index < result))
            result = index;
        }

        if (result === null)
          return -1;
        else
          return result;
      }
    );

    $.Method({ Static: true, Public: true }, "Insert",
	  new JSIL.MethodSignature($.String, [$.String, $.Int32, $.String], [], $jsilcore),
	  function (srcStr, index, str) {
	  	return srcStr.substring(0, index) + str + srcStr.substring(index, srcStr.length);
	  }
	);

    $.Method({ Static: true, Public: true }, "IsNullOrEmpty",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.String")], []),
      function (str) {
        if (str === null)
          return true;
        else if (typeof (str) === "undefined")
          return true;
        else if (str.length === 0)
          return true;

        return false;
      }
    );

    $.Method({ Static: true, Public: true }, "IsNullOrWhiteSpace",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.String")], []),
      function (str) {
        if (str === null)
          return true;
        else if (typeof (str) === "undefined")
          return true;
        else if (str.length === 0)
          return true;
        else if (str.trim().length === 0)
          return true;

        return false;
      }
    );

    $.Method({ Static: true, Public: true }, "LastIndexOfAny",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Int32"), [$jsilcore.TypeRef("System.Array", [$jsilcore.System.Char]), $jsilcore.TypeRef("System.Int32")], []),
      function (str, chars) {
        var result = null;
        for (var i = 0; i < chars.length; i++) {
          var index = str.lastIndexOf(chars[i]);
          if ((result === null) || (index > result))
            result = index;
        }

        if (result === null)
          return -1;
        else
          return result;
      }
    );

    $.Method({ Static: true, Public: true }, "Normalize",
      new JSIL.MethodSignature("System.String", [
          "System.String",
          "System.Text.NormalizationForm"
      ], [], $jsilcore),
      function (str, form) {
        if (!str.normalize)
            return str;
        switch (form.name) {
            case "FormC":
                return str.normalize("NFC");
            case "FormD":
                return str.normalize("NFD");
            case "FormKC":
                return str.Normalize("NFKC");
            case "FormKD":
                return str.Normalize("NFKD");
        }
      }
    );

    $.Method({ Static: true, Public: true }, "Remove",
      new JSIL.MethodSignature($.String, [$.String, $.Int32, $.Int32], [], $jsilcore),
      function (str, start, count) {
        return str.substr(0, start) + str.substr(start + count);
      }
    );

    $.Method({ Static: true, Public: true }, "Replace",
      new JSIL.MethodSignature("System.String", ["System.String", "System.String", "System.String"], [], $jsilcore),
      function (str, oldText, newText) {
        return str.split(oldText).join(newText);
      }
    );

    $.Method({ Static: true, Public: true }, "StartsWith",
      new JSIL.MethodSignature("System.Boolean", ["System.String", "System.String"], [], $jsilcore),
      function (str, text) {
        return str.indexOf(text) === 0;
      }
    );

    $.Method({ Static: true, Public: true }, "StartsWith",
      new JSIL.MethodSignature("System.Boolean", ["System.String", "System.String", "System.StringComparison"], [], $jsilcore),
      function (str, text, comp) {
        // localeCompare is better for some of these, but inconsistent
        // enough that it needs to be tested for corners at least first.
        switch (comp) {
          case System.StringComparison.CurrentCultureIgnoreCase:
            return str.toLocaleLowerCase().indexOf(text.toLocaleLowerCase()) == 0;
          case System.StringComparison.InvariantCultureIgnoreCase:
          case System.StringComparison.OrdinalIgnoreCase:
            return str.toLowerCase().indexOf(text.toLowerCase()) == 0;
          default:
            return str.indexOf(text) === 0;
        }
      }
    );

    var makePadding = function (ch, count) {
      var padding = ch;
      for (var i = 1; i < count; i++) {
        padding += ch;
      }

      return padding;
    };

    $.Method({ Static: true, Public: true }, "PadLeft",
      new JSIL.MethodSignature("System.String", ["System.String", "System.Int32", "System.Char"], [], $jsilcore),
      function (str, length, ch) {
        var extraChars = length - str.length;
        if (extraChars <= 0)
          return str;

        return makePadding(ch, extraChars) + str;
      }
    );

    $.Method({ Static: true, Public: true }, "PadRight",
      new JSIL.MethodSignature("System.String", ["System.String", "System.Int32", "System.Char"], [], $jsilcore),
      function (str, length, ch) {
        var extraChars = length - str.length;
        if (extraChars <= 0)
          return str;

        return str + makePadding(ch, extraChars);
      }
    );

    $.Method({ Static: true, Public: true }, "CopyTo",
      new JSIL.MethodSignature(null, ["System.String"], [], $jsilcore),
      function (str, sourceIndex, destination, destinationIndex, count) {
        if (count > 0) {
          for (var i = 0; i < count; i++)
            destination[destinationIndex + i] = str[sourceIndex + i];
        }
      }
    );

    $.Method({ Static: false, Public: true }, "get_Length",
      new JSIL.MethodSignature($.Int32, [], []),
      function() {
        return this.length;
      }
    );

    $.Method({ Static: true, Public: false }, "UseRandomizedHashing",
      new JSIL.MethodSignature($.Boolean, [], []),
      function() {
        return false;
      }
    );

    $.Method({ Static: false, Public: false }, null,
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.IEnumerator"), [], []),
        function() {
          return JSIL.GetEnumerator(this, $jsilcore.System.Char.__Type__, true);
        }
      )
      .Overrides("System.Collections.IEnumerable", "GetEnumerator");

    $.Method({ Static: false, Public: false }, "GetEnumerator",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [$.Char]), [], []),
        function() {
          return JSIL.GetEnumerator(this, $jsilcore.System.Char.__Type__, true);
        }
      )
      .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");
  }
);

JSIL.MakeClass("System.Object", "System.String", true, [], function ($) {
  $.Field({ Static: true, Public: true }, "Empty", $.String, "");
  $.Property({ Public: true, Static: false }, "Length");
  JSIL.MakeIConvertibleMethods($);
  $.ImplementInterfaces(
     $jsilcore.TypeRef("System.Collections.IEnumerable", []),
     $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [$.Char])
  );
});

JSIL.MakeEnum(
  "System.StringComparison", true, {
    CurrentCulture: 0,
    CurrentCultureIgnoreCase: 1,
    InvariantCulture: 2,
    InvariantCultureIgnoreCase: 3,
    Ordinal: 4,
    OrdinalIgnoreCase: 5
  }, false
);

JSIL.EscapeJSRegex = function (regexText) {
  return regexText.replace(/[-[\]{}()*+?.,\\^$|#\s]/g, "\\$&");
};
JSIL.SplitString = function (str, separators, count, options) {
  if (options && options.value)
    JSIL.RuntimeError("StringSplitOptions other than None are not implemented");
  if (count > 0 && separators.length > 1)
    JSIL.RuntimeError("Split with count and multiple separators is not implemented");

  if (!separators) {
    // Whitespace characters from Unicode 6.0
    separators = [
      "\u0009", "\u000A", "\u000B", "\u000C", "\u000D", "\u0020", "\u0085", "\u00A0",
      "\u1680", "\u180E", "\u2000", "\u2001", "\u2002", "\u2003", "\u2004", "\u2005",
      "\u2006", "\u2007", "\u2008", "\u2009", "\u200A", "\u2028", "\u2029", "\u202F",
      "\u205F", "\u3000"
    ];
  }

  if (separators.length === 1) {
    if (count > 0) {
      var splits = str.split(separators[0]);
      if (splits.length <= count)
        return splits;
      splits.splice(count - 1, splits.length,
          splits.slice(count - 1).join(separators[0]));
      return splits;
    } else {
      return str.split(separators[0]);
    }
  } else {
    var regexText = "";
    for (var i = 0; i < separators.length; i++) {
      if (i > 0)
        regexText += "|"

      regexText += JSIL.EscapeJSRegex(separators[i]);
    }
    var regex = new RegExp(regexText, "g");

    return str.split(regex);
  }
};
JSIL.JoinStrings = function (separator, strings) {
  return strings.join(separator);
};
JSIL.JoinEnumerable = function (separator, values) {
  return JSIL.JoinStrings(separator, JSIL.EnumerableToArray(values));
};
JSIL.ConcatString = function (/* ...values */) {
  var result = "";

  if (arguments[0] !== null)
    result = String(arguments[0]);

  for (var i = 1, l = arguments.length; i < l; i++) {
    var arg = arguments[i];
    if (arg === null)
      ;
    else if (typeof (arg) === "string")
      result += arg;
    else
      result += String(arg);
  }

  return result;
};

$jsilcore.makeByteReader = function (bytes, index, count) {
  var position = (typeof (index) === "number") ? index : 0;
  var endpoint;

  if (typeof (count) === "number")
    endpoint = (position + count);
  else
    endpoint = (bytes.length - position);

  var result = {
    read: function () {
      if (position >= endpoint)
        return false;

      var nextByte = bytes[position];
      position += 1;
      return nextByte;
    }
  };

  Object.defineProperty(result, "eof", {
    get: function () {
      return (position >= endpoint);
    },
    configurable: true,
    enumerable: true
  });

  return result;
};
$jsilcore.charCodeAt = function fixedCharCodeAt(str, idx) {
  // https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/String/charCodeAt

  idx = idx || 0;
  var code = str.charCodeAt(idx);
  var hi, low;

  if (0xD800 <= code && code <= 0xDBFF) {
    // High surrogate (could change last hex to 0xDB7F to treat high private surrogates as single characters)
    hi = code;
    low = str.charCodeAt(idx + 1);
    if (isNaN(low))
      JSIL.RuntimeError("High surrogate not followed by low surrogate");

    return ((hi - 0xD800) * 0x400) + (low - 0xDC00) + 0x10000;
  }

  if (0xDC00 <= code && code <= 0xDFFF) {
    // Low surrogate
    // We return false to allow loops to skip this iteration since should have already handled high surrogate above in the previous iteration
    return false;
  }

  return code;
};

$jsilcore.makeCharacterReader = function (str) {
  var position = 0, length = str.length;
  var cca = $jsilcore.charCodeAt;

  var result = {
    read: function () {
      if (position >= length)
        return false;

      var nextChar = cca(str, position);
      position += 1;
      return nextChar;
    }
  };

  Object.defineProperty(result, "eof", {
    get: function () {
      return (position >= length);
    },
    configurable: true,
    enumerable: true
  });

  return result;
};

$jsilcore.fromCharCode = function fixedFromCharCode(codePt) {
  // https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/String/fromCharCode
  if (codePt > 0xFFFF) {
    codePt -= 0x10000;
    return String.fromCharCode(0xD800 + (codePt >> 10), 0xDC00 + (codePt & 0x3FF));
  } else {
    return String.fromCharCode(codePt);
  }
};

JSIL.ImplementExternals("System.Text.Encoding", function ($) {
  $.RawMethod(false, "$fromCharset", function (charset) {
    this._charset = charset;
    this.fallbackCharacter = "?";
  });

  $.RawMethod(false, "$makeWriter", function (outputBytes, outputIndex) {
    var i = outputIndex;
    var count = 0;

    if (JSIL.IsArray(outputBytes)) {
      return {
        write: function (byte) {
          if (i >= outputBytes.length)
            JSIL.RuntimeError("End of buffer");

          outputBytes[i] = byte;
          i++;
          count++;
        },
        getResult: function () {
          return count;
        }
      };
    } else {
      var resultBytes = new Array();
      return {
        write: function (byte) {
          resultBytes.push(byte);
        },
        getResult: function () {
          if (typeof (Uint8Array) !== "undefined")
            return new Uint8Array(resultBytes);
          else
            return resultBytes;
        }
      };
    }
  });

  $.RawMethod(false, "$fromCharCode", $jsilcore.fromCharCode);

  $.RawMethod(false, "$charCodeAt", $jsilcore.charCodeAt);

  $.RawMethod(false, "$makeCharacterReader", $jsilcore.makeCharacterReader);

  $.RawMethod(false, "$makeByteReader", $jsilcore.makeByteReader);

  $.RawMethod(false, "$encode", function Encoding_Encode_PureVirtual(string, outputBytes, outputIndex) {
    throw new Error("Not implemented");
  });

  $.RawMethod(false, "$decode", function Encoding_Decode_PureVirtual(bytes, index, count) {
    throw new Error("Not implemented");
  });

  $.RawMethod(false, "$charsToString", function (chars, index, count) {
    if (typeof (index) === "undefined")
      index = 0;
    if (typeof (count) === "undefined")
      count = chars.length;

    return JSIL.StringFromByteArray(chars, index, count);
  });

  $.RawMethod(false, "$stringToChars", function (string) {
    return Array.prototype.slice.call(string);
  });

  $.Method({ Static: true, Public: true }, "get_ASCII",
    (new JSIL.MethodSignature($.Type, [], [])),
    function () {
      if (!System.Text.Encoding.asciiEncoding)
        System.Text.Encoding.asciiEncoding = JSIL.CreateInstanceOfType(
          System.Text.ASCIIEncoding.__Type__, "$fromCharset", ["US-ASCII"]
        );

      return System.Text.Encoding.asciiEncoding;
    }
  );

  $.Method({ Static: true, Public: true }, "get_UTF8",
    (new JSIL.MethodSignature($.Type, [], [])),
    function () {
      if (!System.Text.Encoding.utf8Encoding)
        System.Text.Encoding.utf8Encoding = JSIL.CreateInstanceOfType(
          System.Text.UTF8Encoding.__Type__, "$fromCharset", ["UTF-8"]
        );

      return System.Text.Encoding.utf8Encoding;
    }
  );

  $.Method({ Static: true, Public: true }, "get_UTF7",
    (new JSIL.MethodSignature($.Type, [], [])),
    function () {
      if (!System.Text.Encoding.utf7Encoding)
        System.Text.Encoding.utf7Encoding = JSIL.CreateInstanceOfType(
          System.Text.UTF7Encoding.__Type__, "$fromCharset", ["UTF-7"]
        );

      return System.Text.Encoding.utf7Encoding;
    }
  );

  $.Method({ Static: true, Public: true }, "get_Unicode",
    (new JSIL.MethodSignature($.Type, [], [])),
    function () {
      if (!System.Text.Encoding.unicodeEncoding)
        System.Text.Encoding.unicodeEncoding = new $jsilcore.System.Text.UnicodeEncoding(false, true);

      return System.Text.Encoding.unicodeEncoding;
    }
  );

  $.Method({ Static: true, Public: true }, "get_BigEndianUnicode",
    (new JSIL.MethodSignature($.Type, [], [])),
    function () {
      if (!System.Text.Encoding.bigEndianUnicodeEncoding)
        System.Text.Encoding.bigEndianUnicodeEncoding = new $jsilcore.System.Text.UnicodeEncoding(true, true);

      return System.Text.Encoding.bigEndianUnicodeEncoding;
    }
  );

  $.Method({ Static: false, Public: true }, "GetByteCount",
    (new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.Array", [$.Char])], [])),
    function GetByteCount(chars) {
      return this.$encode(this.$charsToString(chars)).length;
    }
  );

  $.Method({ Static: false, Public: true }, "GetByteCount",
    (new JSIL.MethodSignature($.Int32, [$.String], [])),
    function GetByteCount(s) {
      return this.$encode(s).length;
    }
  );

  $.Method({ Static: false, Public: true }, "GetByteCount",
    (new JSIL.MethodSignature($.Int32, [
          $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32,
          $.Int32
    ], [])),
    function GetByteCount(chars, index, count) {
      return this.$encode(this.$charsToString(chars, index, count)).length;
    }
  );

  $.Method({ Static: false, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$jsilcore.TypeRef("System.Array", [$.Char])], [])),
    function GetBytes(chars) {
      return this.$encode(this.$charsToString(chars));
    }
  );

  $.Method({ Static: false, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [
          $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32,
          $.Int32
    ], [])),
    function GetBytes(chars, index, count) {
      return this.$encode(this.$charsToString(chars, index, count));
    }
  );

  $.Method({ Static: false, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($.Int32, [
          $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32,
          $.Int32, $jsilcore.TypeRef("System.Array", [$.Byte]),
          $.Int32
    ], [])),
    function GetBytes(chars, charIndex, charCount, bytes, byteIndex) {
      return this.$encode(this.$charsToString(chars, index, count), bytes, byteIndex);
    }
  );

  $.Method({ Static: false, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Byte]), [$.String], [])),
    function GetBytes(s) {
      return this.$encode(s);
    }
  );

  $.Method({ Static: false, Public: true }, "GetBytes",
    (new JSIL.MethodSignature($.Int32, [
          $.String, $.Int32,
          $.Int32, $jsilcore.TypeRef("System.Array", [$.Byte]),
          $.Int32
    ], [])),
    function GetBytes(s, charIndex, charCount, bytes, byteIndex) {
      return this.$encode(s.substr(charIndex, charCount), bytes, byteIndex);
    }
  );

  $.Method({ Static: false, Public: true }, "GetCharCount",
    (new JSIL.MethodSignature($.Int32, [$jsilcore.TypeRef("System.Array", [$.Byte])], [])),
    function GetCharCount(bytes) {
      return this.$decode(bytes).length;
    }
  );

  $.Method({ Static: false, Public: true }, "GetCharCount",
    (new JSIL.MethodSignature($.Int32, [
          $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
          $.Int32
    ], [])),
    function GetCharCount(bytes, index, count) {
      return this.$decode(bytes, index, count).length;
    }
  );

  $.Method({ Static: false, Public: true }, "GetChars",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Char]), [$jsilcore.TypeRef("System.Array", [$.Byte])], [])),
    function GetChars(bytes) {
      return this.$stringToChars(this.$decode(bytes));
    }
  );

  $.Method({ Static: false, Public: true }, "GetChars",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$.Char]), [
          $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
          $.Int32
    ], [])),
    function GetChars(bytes, index, count) {
      return this.$stringToChars(this.$decode(bytes, index, count));
    }
  );

  $.Method({ Static: false, Public: true }, "GetChars",
    (new JSIL.MethodSignature($.Int32, [
          $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
          $.Int32, $jsilcore.TypeRef("System.Array", [$.Char]),
          $.Int32
    ], [])),
    function GetChars(bytes, byteIndex, byteCount, chars, charIndex) {
      throw new Error("Not implemented");
    }
  );

  $.Method({ Static: false, Public: true }, "GetString",
    (new JSIL.MethodSignature($.String, [$jsilcore.TypeRef("System.Array", [$.Byte])], [])),
    function GetString(bytes) {
      return this.$decode(bytes);
    }
  );

  $.Method({ Static: false, Public: true }, "GetString",
    (new JSIL.MethodSignature($.String, [
          $jsilcore.TypeRef("System.Array", [$.Byte]), $.Int32,
          $.Int32
    ], [])),
    function GetString(bytes, index, count) {
      return this.$decode(bytes, index, count);
    }
  );
});

JSIL.MakeClass("System.Object", "System.Text.Encoding", true, [], function ($) {
  $.Property({ Static: true, Public: true }, "ASCII");
  $.Property({ Static: true, Public: true }, "UTF8");
  $.Property({ Static: true, Public: true }, "UTF7");
  $.Property({ Static: true, Public: true }, "Unicode");
  $.Property({ Static: true, Public: true }, "BigEndianUnicode");
});
JSIL.ImplementExternals("System.Text.ASCIIEncoding", function ($) {
  $.RawMethod(false, "$encode", function ASCIIEncoding_Encode(string, outputBytes, outputIndex) {
    var writer = this.$makeWriter(outputBytes, outputIndex);

    var fallbackCharacter = this.fallbackCharacter.charCodeAt(0);
    var reader = this.$makeCharacterReader(string), ch;

    while (!reader.eof) {
      ch = reader.read();

      if (ch === false)
        continue;
      else if (ch <= 127)
        writer.write(ch);
      else
        writer.write(fallbackCharacter);
    }

    return writer.getResult();
  });

  $.RawMethod(false, "$decode", function ASCIIEncoding_Decode(bytes, index, count) {
    var reader = this.$makeByteReader(bytes, index, count), byte;
    var result = "";

    while (!reader.eof) {
      byte = reader.read();

      if (byte === false)
        continue;
      else if (byte > 127)
        result += this.fallbackCharacter;
      else
        result += String.fromCharCode(byte);
    }

    return result;
  });
});

JSIL.MakeClass("System.Text.Encoding", "System.Text.ASCIIEncoding", true, [], function ($) {
});
JSIL.ImplementExternals("System.Text.UTF8Encoding", function ($) {
  var UTF8ByteSwapNotAChar = 0xFFFE;
  var UTF8NotAChar = 0xFFFF;

  $.Method({ Static: false, Public: true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor() {
      this.emitBOM = false;
      this.throwOnInvalid = false;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function _ctor(encoderShouldEmitUTF8Identifier) {
      this.emitBOM = encoderShouldEmitUTF8Identifier;
      this.throwOnInvalid = false;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Boolean, $.Boolean], [])),
    function _ctor(encoderShouldEmitUTF8Identifier, throwOnInvalidBytes) {
      this.emitBOM = encoderShouldEmitUTF8Identifier;
      this.throwOnInvalid = throwOnInvalidBytes;
    }
  );

  $.RawMethod(false, "$encode", function UTF8Encoding_Encode(string, outputBytes, outputIndex) {
    // http://tidy.sourceforge.net/cgi-bin/lxr/source/src/utf8.c

    var writer = this.$makeWriter(outputBytes, outputIndex);
    var reader = this.$makeCharacterReader(string), ch;

    var hasError = false;

    while (!reader.eof) {
      ch = reader.read();

      if (ch === false)
        continue;

      if (ch <= 0x7F) {
        writer.write(ch);
      } else if (ch <= 0x7FF) {
        writer.write(0xC0 | (ch >> 6));
        writer.write(0x80 | (ch & 0x3F));
      } else if (ch <= 0xFFFF) {
        writer.write(0xE0 | (ch >> 12));
        writer.write(0x80 | ((ch >> 6) & 0x3F));
        writer.write(0x80 | (ch & 0x3F));
      } else if (ch <= 0x1FFFF) {
        writer.write(0xF0 | (ch >> 18));
        writer.write(0x80 | ((ch >> 12) & 0x3F));
        writer.write(0x80 | ((ch >> 6) & 0x3F));
        writer.write(0x80 | (ch & 0x3F));

        if ((ch === UTF8ByteSwapNotAChar) || (ch === UTF8NotAChar))
          hasError = true;
      } else if (ch <= 0x3FFFFFF) {
        writer.write(0xF0 | (ch >> 24));
        writer.write(0x80 | ((ch >> 18) & 0x3F));
        writer.write(0x80 | ((ch >> 12) & 0x3F));
        writer.write(0x80 | ((ch >> 6) & 0x3F));
        writer.write(0x80 | (ch & 0x3F));

        hasError = true;
      } else if (ch <= 0x7FFFFFFF) {
        writer.write(0xF0 | (ch >> 30));
        writer.write(0x80 | ((ch >> 24) & 0x3F));
        writer.write(0x80 | ((ch >> 18) & 0x3F));
        writer.write(0x80 | ((ch >> 12) & 0x3F));
        writer.write(0x80 | ((ch >> 6) & 0x3F));
        writer.write(0x80 | (ch & 0x3F));

        hasError = true;
      } else {
        hasError = true;
      }
    }

    return writer.getResult();
  });

  $.RawMethod(false, "$decode", function UTF8Encoding_Decode(bytes, index, count) {
    // http://tidy.sourceforge.net/cgi-bin/lxr/source/src/utf8.c

    var reader = this.$makeByteReader(bytes, index, count), firstByte;
    var result = "";

    while (!reader.eof) {
      var accumulator = 0, extraBytes = 0, hasError = false;
      firstByte = reader.read();

      if (firstByte === false)
        continue;

      if (firstByte <= 0x7F) {
        accumulator = firstByte;
      } else if ((firstByte & 0xE0) === 0xC0) {
        accumulator = firstByte & 31;
        extraBytes = 1;
      } else if ((firstByte & 0xF0) === 0xE0) {
        accumulator = firstByte & 15;
        extraBytes = 2;
      } else if ((firstByte & 0xF8) === 0xF0) {
        accumulator = firstByte & 7;
        extraBytes = 3;
      } else if ((firstByte & 0xFC) === 0xF8) {
        accumulator = firstByte & 3;
        extraBytes = 4;
        hasError = true;
      } else if ((firstByte & 0xFE) === 0xFC) {
        accumulator = firstByte & 3;
        extraBytes = 5;
        hasError = true;
      } else {
        accumulator = firstByte;
        hasError = false;
      }

      while (extraBytes > 0) {
        var extraByte = reader.read();
        extraBytes--;

        if (extraByte === false) {
          hasError = true;
          break;
        }

        if ((extraByte & 0xC0) !== 0x80) {
          hasError = true;
          break;
        }

        accumulator = (accumulator << 6) | (extraByte & 0x3F);
      }

      if ((accumulator === UTF8ByteSwapNotAChar) || (accumulator === UTF8NotAChar))
        hasError = true;

      var characters;
      if (!hasError)
        characters = this.$fromCharCode(accumulator);

      if (hasError || (characters === false)) {
        if (this.throwOnInvalid)
          JSIL.RuntimeError("Invalid character in UTF8 text");
        else
          result += this.fallbackCharacter;
      } else
        result += characters;
    }

    return result;
  });
});

JSIL.MakeClass("System.Text.Encoding", "System.Text.UTF8Encoding", true, [], function ($) {
});
JSIL.MakeClass("System.Text.Encoding", "System.Text.UTF7Encoding", true, [], function ($) {
});
JSIL.ImplementExternals("System.Text.UnicodeEncoding", function ($) {
  var writePair = function (writer, a, b) {
    writer.write(a);
    writer.write(b);
  };

  var readPair = function (reader) {
    var a = reader.read();
    var b = reader.read();

    if ((a === false) || (b === false))
      return false;

    return (a << 8) | b;
  };

  var swapBytes = function (word) {
    return ((word & 0xFF) << 8) | ((word >> 8) & 0xFF);
  };

  $.Method({ Static: false, Public: true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor() {
      this.bigEndian = false;
      this.emitBOM = true;
      this.throwOnInvalid = false;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function _ctor(bigEndian) {
      this.bigEndian = bigEndian;
      this.emitBOM = true;
      this.throwOnInvalid = false;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Boolean, $.Boolean], [])),
    function _ctor(bigEndian, byteOrderMark) {
      this.bigEndian = bigEndian;
      this.emitBOM = byteOrderMark;
      this.throwOnInvalid = false;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Boolean, $.Boolean, $.Boolean], [])),
    function _ctor(bigEndian, byteOrderMark, throwOnInvalidBytes) {
      this.bigEndian = bigEndian;
      this.emitBOM = byteOrderMark;
      this.throwOnInvalid = throwOnInvalidBytes;
    }
  );

  $.RawMethod(false, "$encode", function UnicodeEncoding_Encode(string, outputBytes, outputIndex) {
    var writer = this.$makeWriter(outputBytes, outputIndex);
    var reader = this.$makeCharacterReader(string), ch, lowBits, highBits;

    var hasError = false;

    while (!reader.eof) {
      ch = reader.read();

      if (ch === false)
        continue;

      if (ch < 0x10000) {
        if (this.bigEndian)
          ch = swapBytes(ch);

        writePair(writer, ch & 0xFF, (ch >> 8) & 0xFF);
      } else if (ch <= 0x10FFFF) {
        ch -= 0x10000;

        if (this.bigEndian)
          ch = swapBytes(ch);

        lowBits = (ch & 0x3FF) | 0xDC00;
        highBits = ((ch >> 10) & 0x3FF) | 0xD800;

        writePair(writer, highBits & 0xFF, (highBits >> 8) & 0xFF);
        writePair(writer, lowBits & 0xFF, (lowBits >> 8) & 0xFF);
      } else {
        hasError = true;
      }
    }

    return writer.getResult();
  });

  $.RawMethod(false, "$decode", function UnicodeEncoding_Decode(bytes, index, count) {
    var reader = this.$makeByteReader(bytes, index, count);
    var result = "";
    var hasError;
    var firstWord, secondWord, charCode;

    while (!reader.eof) {
      firstWord = readPair(reader);

      if (firstWord === false)
        continue;

      if ((firstWord < 0xD800) || (firstWord > 0xDFFF)) {
        charCode = firstWord;

        if (!this.bigEndian)
          charCode = swapBytes(charCode);

        result += this.$fromCharCode(charCode);
        hasError = false;
      } else if ((firstWord >= 0xD800) && (firstWord <= 0xDBFF)) {
        secondWord = readPair(reader);
        if (secondWord === false) {
          hasError = true;
        } else {
          var highBits = firstWord & 0x3FF;
          var lowBits = secondWord & 0x3FF;
          charCode = ((highBits << 10) | lowBits) + 0x10000;

          if (!this.bigEndian)
            charCode = swapBytes(charCode);

          result += this.$fromCharCode(charCode);
        }
      } else {
        hasError = true;
      }

      if (hasError) {
        if (this.throwOnInvalid)
          JSIL.RuntimeError("Invalid character in UTF16 text");
        else
          result += this.fallbackCharacter;
      }
    }

    return result;
  });
});

JSIL.MakeClass("System.Text.Encoding", "System.Text.UnicodeEncoding", true, [], function ($) {
});
JSIL.ImplementExternals("System.Text.StringBuilder", function ($) {

  $.Method({ Static: false, Public: true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor() {
      this._str = "";
      this._capacity = 0;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function _ctor(capacity) {
      this._str = "";
      this._capacity = capacity;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function _ctor(value) {
      this._str = value;
      this._capacity = value.length;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String, $.Int32], [])),
    function _ctor(value, capacity) {
      this._str = value;
      this._capacity = capacity;
    }
  );

  var appendString = function (self, str, startIndex, length, copies) {
    if (arguments.length === 2) {
      startIndex = 0;
      length = str.length;
      copies = 1;
    }

    if ((startIndex !== 0) || (length !== str.length)) {
      for (var i = 0; i < copies; i++) {
        self._str += str.substr(startIndex, length);
      }

    } else {
      for (var i = 0; i < copies; i++) {
        self._str += str;
      }

    }

    self._capacity = Math.max(self._capacity, self._str.length);
  };

  var appendNumber = function (self, num) {
    self._str += String(num);

    self._capacity = Math.max(self._capacity, self._str.length);
  };

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Char, $.Int32], [])),
    function Append(value, repeatCount) {
      appendString(this, value, 0, value.length, repeatCount);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [
          $jsilcore.TypeRef("System.Array", [$.Char]), $.Int32,
          $.Int32
    ], [])),
    function Append(value, startIndex, charCount) {
      for (var i = 0; i < charCount; i++)
        this._str += value[startIndex + i];

      this._capacity = Math.max(this._capacity, this._str.length);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Object], [])),
    function Append(value) {
      var string = value.toString();
      appendString(this, string, 0, string.length, 1);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.String], [])),
    function Append(value) {
      appendString(this, value, 0, value.length, 1);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [
          $.String, $.Int32,
          $.Int32
    ], [])),
    function Append(value, startIndex, count) {
      appendString(this, value, startIndex, count, 1);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Boolean], [])),
    function Append(value) {
      this._str += (value ? "True" : "False");
      this._capacity = Math.max(this._capacity, this._str.length);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.SByte], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Byte], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Char], [])),
    function Append(value) {
      appendString(this, value, 0, value.length, 1);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Int16], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Int32], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Int64], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Single], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.Double], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.UInt16], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.UInt32], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$.UInt64], [])),
    function Append(value) {
      appendNumber(this, value);
    }
  );

  $.Method({ Static: false, Public: true }, "Append",
    (new JSIL.MethodSignature($.Type, [$jsilcore.TypeRef("System.Array", [$.Char])], [])),
    function Append(value) {
      for (var i = 0; i < value.length; i++)
        this._str += value[i];

      this._capacity = Math.max(this._capacity, this._str.length);
    }
  );

  $.Method({ Static: false, Public: true }, "AppendLine",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Text.StringBuilder"), [], [])),
    function AppendLine() {
      appendString(this, "\r\n", 0, 2, 1);
      return this;
    }
  );

  $.Method({ Static: false, Public: true }, "AppendLine",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Text.StringBuilder"), [$.String], [])),
    function AppendLine(value) {
      appendString(this, value, 0, value.length, 1);
      appendString(this, "\r\n", 0, 2, 1);
      return this;
    }
  );

  $.Method({ Static: false, Public: true }, "AppendFormat",
    (new JSIL.MethodSignature($.Type, [$.String, $.Object], [])),
    function AppendFormat(format, arg0) {
      appendString(this, System.String.Format(format, [arg0]));
      return this;
    }
  );

  $.Method({ Static: false, Public: true }, "AppendFormat",
    (new JSIL.MethodSignature($.Type, [
          $.String, $.Object,
          $.Object
    ], [])),
    function AppendFormat(format, arg0, arg1) {
      appendString(this, System.String.Format(format, [arg0, arg1]));
      return this;
    }
  );

  $.Method({ Static: false, Public: true }, "AppendFormat",
    (new JSIL.MethodSignature($.Type, [
          $.String, $.Object,
          $.Object, $.Object
    ], [])),
    function AppendFormat(format, arg0, arg1, arg2) {
      appendString(this, System.String.Format(format, [arg0, arg1, arg2]));
      return this;
    }
  );

  $.Method({ Static: false, Public: true }, "AppendFormat",
    (new JSIL.MethodSignature($.Type, [$.String, $jsilcore.TypeRef("System.Array", [$.Object])], [])),
    function AppendFormat(format, args) {
      appendString(this, System.String.Format(format, args));
      return this;
    }
  );

  $.Method({ Static: false, Public: true }, "Clear",
    (new JSIL.MethodSignature($.Type, [], [])),
    function Clear() {
      this._str = "";
    }
  );

  $.Method({ Static: false, Public: true }, "get_Length",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Length() {
      return this._str.length;
    }
  );

  $.Method({ Static: false, Public: true }, "set_Capacity",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function set_Capacity(value) {
      // FIXME: What happens if value is lower than the length of the current contents?
      this._capacity = Math.max(value | 0, this._str.length);
    }
  );

  $.Method({ Static: false, Public: true }, "get_Capacity",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Capacity() {
      return this._capacity;
    }
  );

  $.Method({ Static: false, Public: true }, "Remove",
    (new JSIL.MethodSignature($.Type, [$.Int32, $.Int32], [])),
    function Remove(startIndex, length) {
      this._str = this._str.substr(0, startIndex) + this._str.substring(startIndex + length, length);
      return this;
    }
  );

  var replace = function (self, oldText, newText, startIndex, count) {
    var prefix = self._str.substr(0, startIndex);
    var suffix = self._str.substr(startIndex + count);
    var region = self._str.substr(startIndex, count);
    var result = prefix + region.split(oldText).join(newText) + suffix;
    self._str = result;
    self._capacity = Math.max(self._capacity, self._str.length);
    return self;
  };

  $.Method({ Static: false, Public: true }, "Replace",
    (new JSIL.MethodSignature($.Type, [$.String, $.String], [])),
    function Replace(oldValue, newValue) {
      return replace(this, oldValue, newValue, 0, this._str.length);
    }
  );

  $.Method({ Static: false, Public: true }, "Replace",
    (new JSIL.MethodSignature($.Type, [
          $.String, $.String,
          $.Int32, $.Int32
    ], [])),
    function Replace(oldValue, newValue, startIndex, count) {
      return replace(this, oldValue, newValue, startIndex, count);
    }
  );

  $.Method({ Static: false, Public: true }, "Replace",
    (new JSIL.MethodSignature($.Type, [$.Char, $.Char], [])),
    function Replace(oldChar, newChar) {
      return replace(this, oldChar, newChar, 0, this._str.length);
    }
  );

  $.Method({ Static: false, Public: true }, "Replace",
    (new JSIL.MethodSignature($.Type, [
          $.Char, $.Char,
          $.Int32, $.Int32
    ], [])),
    function Replace(oldChar, newChar, startIndex, count) {
      return replace(this, oldChar, newChar, startIndex, count);
    }
  );

  var insert = function (self, string, startIndex, count) {
    while (startIndex > self._str.length - 1 && self._str.length < self._capacity) {
      self._str += "\0";
    }

    var suffix = self._str.substr(startIndex);
    self._str = self._str.substr(0, startIndex);
    for (var i = 0; i < count; i++) {
      self._str += string;
    }
    self._str += suffix;
    self._capacity = Math.max(self._capacity, self._str.length);
    return self;
  };

  $.Method({ Static: false, Public: true }, "Insert",
    (new JSIL.MethodSignature($.Type, [$.Int32, $.String], [])),
    function Insert(index, value) {
      return insert(this, value, index, 1);
    }
  );

  $.Method({ Static: false, Public: true }, "Insert",
    (new JSIL.MethodSignature($.Type, [$.Int32, $.String, $.Int32], [])),
    function Insert(index, value, count) {
      return insert(this, value, index, count);
    }
  );

  $.Method({ Static: false, Public: true }, "set_Length",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function set_Length(value) {
      var delta = value - this._str.length;

      if (delta < 0) {
        this._str = this._str.substr(0, value);
      } else if (delta > 0) {
        var ch = new Array(delta);
        for (var i = 0; i < delta; i++)
          ch[i] = '\0';

        this._str += JSIL.StringFromByteArray(ch);
      }
    }
  );

  $.Method({ Static: false, Public: true }, "get_Chars",
    (new JSIL.MethodSignature($.Char, [$.Int32], [])),
    function get_Chars(i) {
      return this._str[i];
    }
  );

  $.Method({ Static: false, Public: true }, "set_Chars",
    (new JSIL.MethodSignature(null, [$.Int32, $.Char], [])),
    function set_Chars(i, value) {
      while (i > this._str.length - 1) {
        this._str += "\0";
      }
      this._str =
        this._str.substr(0, i) +
        value +
        this._str.substr(i + 1);
    }
  );

  $.Method({ Static: false, Public: true }, "toString",
    (new JSIL.MethodSignature($.String, [], [])),
    function toString() {
      return this._str;
    }
  );
});

JSIL.MakeClass($jsilcore.TypeRef("System.Object"), "System.Text.StringBuilder", true, [], function ($) {
});
JSIL.MakeEnum(
  "System.Text.RegularExpressions.RegexOptions", true, {
    None: 0,
    IgnoreCase: 1,
    Multiline: 2,
    ExplicitCapture: 4,
    Compiled: 8,
    Singleline: 16,
    IgnorePatternWhitespace: 32,
    RightToLeft: 64,
    ECMAScript: 256,
    CultureInvariant: 512
  }, true
);
JSIL.ImplementExternals("System.Text.RegularExpressions.Regex", function ($) {
  var system = JSIL.GetAssembly("System", true);

  var makeRegex = function (pattern, options) {
    var tRegexOptions = system.System.Text.RegularExpressions.RegexOptions;
    if ((options & tRegexOptions.ECMAScript) === 0) {
      JSIL.RuntimeError("Non-ECMAScript regexes are not currently supported.");
    }

    var flags = "g";

    if ((options & tRegexOptions.IgnoreCase) !== 0) {
      flags += "i";
    }

    if ((options & tRegexOptions.Multiline) !== 0) {
      flags += "m";
    }

    return new RegExp(pattern, flags);
  };

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function _ctor(pattern) {
      this._regex = makeRegex(pattern, System.Text.RegularExpressions.RegexOptions.None);
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String, system.TypeRef("System.Text.RegularExpressions.RegexOptions")], [])),
    function _ctor(pattern, options) {
      this._regex = makeRegex(pattern, options);
    }
  );

  $.Method({ Static: false, Public: true }, "Matches",
    (new JSIL.MethodSignature(system.TypeRef("System.Text.RegularExpressions.MatchCollection"), [$.String], [])),
    function Matches(input) {
      var matchObjects = [];
      var tMatch = system.System.Text.RegularExpressions.Match.__Type__;
      var tGroup = system.System.Text.RegularExpressions.Group.__Type__;
      var tGroupCollection = system.System.Text.RegularExpressions.GroupCollection.__Type__;

      var current = null;
      while ((current = this._regex.exec(input)) !== null) {
        var groupObjects = [];
        for (var i = 0, l = current.length; i < l; i++) {
          var groupObject = JSIL.CreateInstanceOfType(
            tGroup, "$internalCtor", [
              current[i],
              (current[i] !== null) && (current[i].length > 0)
            ]
          );
          groupObjects.push(groupObject);
        }

        var groupCollection = JSIL.CreateInstanceOfType(
          tGroupCollection, "$internalCtor", [groupObjects]
        );

        var matchObject = JSIL.CreateInstanceOfType(
          tMatch, "$internalCtor", [
            current[0], groupCollection
          ]
        );
        matchObjects.push(matchObject);
      }

      var result = JSIL.CreateInstanceOfType(
        System.Text.RegularExpressions.MatchCollection.__Type__,
        "$internalCtor", [matchObjects]
      );

      return result;
    }
  );

  $.Method({ Static: true, Public: true }, "Replace",
    (new JSIL.MethodSignature($.String, [
          $.String, $.String,
          $.String, system.TypeRef("System.Text.RegularExpressions.RegexOptions")
    ], [])),
    function Replace(input, pattern, replacement, options) {
      var re = makeRegex(pattern, options);

      return input.replace(re, replacement);
    }
  );

  $.Method({ Static: true, Public: true }, "Replace",
    (new JSIL.MethodSignature($.String, [
          $.String, $.String, $.String], [])),
    function Replace(input, pattern, replacement) {
      var re = makeRegex(pattern, System.Text.RegularExpressions.RegexOptions.ECMAScript);

      return input.replace(re, replacement);
    }
  );

  $.Method({ Static: false, Public: true }, "Replace",
    (new JSIL.MethodSignature($.String, [$.String, $.String], [])),
    function Replace(input, replacement) {
      return input.replace(this._regex, replacement);
    }
  );

  $.Method({ Static: false, Public: true }, "IsMatch",
    (new JSIL.MethodSignature($.Boolean, [$.String], [])),
    function IsMatch(input) {
      var matchCount = 0;

      var current = null;
      // Have to exec() until done because JS RegExp is stateful for some stupid reason
      while ((current = this._regex.exec(input)) !== null) {
        matchCount += 1;
      }

      return (matchCount > 0);
    }
  );
});

JSIL.MakeClass($jsilcore.TypeRef("System.Object"), "System.Text.RegularExpressions.Regex", true, [], function ($) {
  var $thisType = $.publicInterface;
});
JSIL.ImplementExternals("System.Text.RegularExpressions.MatchCollection", function ($) {
  var system = JSIL.GetAssembly("System", true);
  var mscorlib = JSIL.GetCorlib();
  var tEnumerator = JSIL.ArrayEnumerator.Of(system.System.Text.RegularExpressions.Match);

  $.RawMethod(false, "$internalCtor", function (matches) {
    this._matches = matches;
  });

  $.Method({ Static: false, Public: true }, "get_Count",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Count() {
      return this._matches.length;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Item",
    (new JSIL.MethodSignature(system.TypeRef("System.Text.RegularExpressions.Match"), [$.Int32], [])),
    function get_Item(i) {
      return this._matches[i];
    }
  );

  $.Method({ Static: false, Public: false }, "GetMatch",
    (new JSIL.MethodSignature(system.TypeRef("System.Text.RegularExpressions.Match"), [$.Int32], [])),
    function GetMatch(i) {
      return this._matches[i];
    }
  );

  $.Method({ Static: false, Public: true }, "GetEnumerator",
    (new JSIL.MethodSignature(mscorlib.TypeRef("System.Collections.IEnumerator"), [], [])),
    function GetEnumerator() {
      return new tEnumerator(this._matches, -1);
    }
  );
});

JSIL.MakeClass($jsilcore.TypeRef("System.Object"), "System.Text.RegularExpressions.MatchCollection", true, [], function ($) {
});
JSIL.ImplementExternals("System.Text.RegularExpressions.Capture", function ($) {
  $.Method({ Static: false, Public: true }, "get_Length",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Length() {
      return this._length;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Value",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_Value() {
      return this._text;
    }
  );

  $.Method({ Static: false, Public: true }, "toString",
    (new JSIL.MethodSignature($.String, [], [])),
    function toString() {
      return this._text;
    }
  );
});
JSIL.ImplementExternals("System.Text.RegularExpressions.Group", function ($) {
  $.RawMethod(false, "$internalCtor", function (text, success) {
    this._text = text;
    this._success = success;
    this._length = text.length;
  });

  $.Method({ Static: false, Public: true }, "get_Success",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_Success() {
      return this._success;
    }
  );
});
JSIL.ImplementExternals("System.Text.RegularExpressions.Match", function ($) {
  $.RawMethod(false, "$internalCtor", function (text, groups) {
    this._text = text;
    this._groupcoll = groups;
    this._length = text.length;
  });

  $.Method({ Static: false, Public: true }, "get_Groups",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Text.RegularExpressions.GroupCollection"), [], [])),
    function get_Groups() {
      return this._groupcoll;
    }
  );
});
JSIL.ImplementExternals("System.Text.RegularExpressions.GroupCollection", function ($) {
  var system = JSIL.GetAssembly("System", true);
  var $thisType = $.publicInterface;
  var tEnumerator = JSIL.ArrayEnumerator.Of(system.System.Text.RegularExpressions.Group);

  $.RawMethod(false, "$internalCtor", function (groups) {
    this._groups = groups;
  });

  $.Method({ Static: false, Public: true }, "get_Count",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Count() {
      return this._groups.length;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Item",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Text.RegularExpressions.Group"), [$.Int32], [])),
    function get_Item(groupnum) {
      return this._groups[groupnum];
    }
  );

  $.Method({ Static: false, Public: true }, "get_Item",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Text.RegularExpressions.Group"), [$.String], [])),
    function get_Item(groupname) {
      throw new Error('Not implemented');
    }
  );

  $.Method({ Static: false, Public: true }, "GetEnumerator",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.IEnumerator"), [], [])),
    function GetEnumerator() {
      return new tEnumerator(this._groups, -1);
    }
  );
});
JSIL.ImplementExternals("System.Char", function ($) {
  $.Method({ Static: true, Public: true }, "IsControl",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsControl(c) {
      // FIXME: Unicode
      var charCode = c.charCodeAt(0);
      return (charCode <= 0x1F) || (charCode === 0x7F);
    }
  );

  $.Method({ Static: true, Public: true }, "IsDigit",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsDigit(c) {
      // FIXME: Unicode
      var charCode = c.charCodeAt(0);
      return (charCode >= 48) && (charCode <= 57);
    }
  );

  $.Method({ Static: true, Public: true }, "IsLetter",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsLetter(c) {
      // FIXME: Unicode
      var charCode = c.charCodeAt(0);
      return (
        ((charCode >= 65) && (charCode <= 90)) ||
        ((charCode >= 97) && (charCode <= 122)));
    }
  );

  $.Method({ Static: true, Public: true }, "IsNumber",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsNumeric(c) {
      // FIXME: Unicode
      var charCode = c.charCodeAt(0);
      return (charCode >= 48) && (charCode <= 57);
    }
  );

  $.Method({ Static: true, Public: true }, "IsLetterOrDigit",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsLetterOrDigit(c) {
      return $jsilcore.System.Char.IsLetter(c) || $jsilcore.System.Char.IsDigit(c);
    }
  );

  $.Method({ Static: true, Public: true }, "IsSurrogate",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsSurrogate(c) {
      var charCode = c.charCodeAt(0);
      return (charCode >= 0xD800) && (charCode <= 0xDFFF);
    }
  );

  $.Method({ Static: true, Public: true }, "IsHighSurrogate",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsSurrogate(c) {
      var charCode = c.charCodeAt(0);
      return (charCode >= 0xD800) && (charCode <= 0xDBFF);
    }
  );

  $.Method({ Static: true, Public: true }, "IsWhiteSpace",
    new JSIL.MethodSignature($.Boolean, [$.Char], []),
    function IsWhiteSpace(c) {
      // FIXME: Unicode
      var charCode = c.charCodeAt(0);
      return (
        ((charCode >= 0x09) && (charCode <= 0x13)) ||
        (charCode === 0x20) ||
        (charCode === 0xA0) ||
        (charCode === 0x85));
    }
  );

  $.Method({ Static: true, Public: true }, "ToLowerInvariant",
    new JSIL.MethodSignature($.Char, [$.Char], []),
    function ToLowerInvariant(c) {
      return c.toLowerCase();
    }
  );

  $.Method({ Static: true, Public: true }, "ToUpperInvariant",
    new JSIL.MethodSignature($.Char, [$.Char], []),
    function ToLowerInvariant(c) {
      return c.toUpperCase();
    }
  );

  $.Method({ Static: true, Public: true }, "ConvertToUtf32",
    new JSIL.MethodSignature($.Int32, [$.String, $.Int32], []),
    function ConvertToUtf32(s, i) {
      return $jsilcore.charCodeAt(s, i);
    }
  );

  $.Method({ Static: true, Public: true }, "ConvertFromUtf32",
    new JSIL.MethodSignature($.String, [$.Int32], []),
    function ConvertFromUtf32(i) {
        return $jsilcore.fromCharCode(i);
    }
  );

  $.Method({ Static: true, Public: true }, "ToString",
    new JSIL.MethodSignature($.String, [$.Char], []),
    function ToString(c) {
      return c;
    }
  );
});
