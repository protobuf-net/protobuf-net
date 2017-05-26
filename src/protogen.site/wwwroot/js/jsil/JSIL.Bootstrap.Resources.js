/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core is required");

if (!$jsilcore)
  throw new Error("JSIL.Core is required");

$jsilcore.getCurrentUICultureImpl = function () {
  var language;
  var svc = JSIL.Host.getService("window", true);

  if (svc) {
    language = svc.getNavigatorLanguage() || "en-US";
  } else {
    language = "en-US";
  }

  return $jsilcore.System.Globalization.CultureInfo.GetCultureInfo(language);
};

JSIL.ImplementExternals("System.Resources.ResourceSet", function ($) {

  $.RawMethod(false, "$fromResources", function (manager, resources) {
    this._manager = manager;
    this._resources = resources;
  });

  $.Method({ Static: false, Public: true }, "Close",
    (JSIL.MethodSignature.Void),
    function Close() {
    }
  );

  $.Method({ Static: false, Public: false }, "Dispose",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function Dispose(disposing) {
    }
  );

  $.Method({ Static: false, Public: true }, "Dispose",
    (JSIL.MethodSignature.Void),
    function Dispose() {
    }
  );

  $.RawMethod(false, "$get", function (key, ignoreCase) {
    if (ignoreCase)
      JSIL.RuntimeError("Case insensitive resource fetches not implemented");

    var result = this._resources[key];
    if (!result)
      return null;

    return result;
  });

  $.Method({ Static: false, Public: true }, "GetObject",
    (new JSIL.MethodSignature($.Object, [$.String], [])),
    function GetObject(name) {
      return this.$get(name, false);
    }
  );

  $.Method({ Static: false, Public: true }, "GetObject",
    (new JSIL.MethodSignature($.Object, [$.String, $.Boolean], [])),
    function GetObject(name, ignoreCase) {
      return this.$get(name, ignoreCase);
    }
  );

  $.Method({ Static: false, Public: true }, "GetString",
    (new JSIL.MethodSignature($.String, [$.String], [])),
    function GetString(name) {
      var result = this.$get(name, false);
      if (typeof (result) !== "string")
        return null;

      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "GetString",
    (new JSIL.MethodSignature($.String, [$.String, $.Boolean], [])),
    function GetString(name, ignoreCase) {
      var result = this.$get(name, ignoreCase);
      if (typeof (result) !== "string")
        return null;

      return result;
    }
  );

});
JSIL.ImplementExternals("System.Resources.ResourceManager", function ($) {
  $.RawMethod(false, "$fromBaseNameAndAssembly", function (baseName, assembly) {
    this._baseName = baseName;
    this._assembly = assembly;
    this._resourceCollection = {};
  });

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String, $jsilcore.TypeRef("System.Reflection.Assembly")], [])),
    function _ctor(baseName, assembly) {
      this.$fromBaseNameAndAssembly(baseName, assembly);
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.String, $jsilcore.TypeRef("System.Reflection.Assembly"),
          $jsilcore.TypeRef("System.Type")
    ], [])),
    function _ctor(baseName, assembly, usingResourceSet) {
      // FIXME: usingResourceSet
      this.$fromBaseNameAndAssembly(baseName, assembly);
    }
  );

  $.Method({ Static: false, Public: true }, "GetObject",
    (new JSIL.MethodSignature($.Object, [$.String], [])),
    function GetObject(name) {
      var set = this.GetResourceSet($jsilcore.getCurrentUICultureImpl(), false, true)
      return set.GetObject(name);
    }
  );

  $.Method({ Static: false, Public: true }, "GetObject",
    (new JSIL.MethodSignature($.Object, [$.String, $jsilcore.TypeRef("System.Globalization.CultureInfo")], [])),
    function GetObject(name, culture) {
      var set = this.GetResourceSet(culture, false, true)
      return set.GetObject(name);
    }
  );

  $.RawMethod(false, "$findResourcesForCulture", function (culture) {
    var key = this._baseName + "." + culture.get_TwoLetterISOLanguageName() + ".resj";
    if (JSIL.Host.doesAssetExist(key))
      return JSIL.Host.getAsset(key);

    key = this._baseName + ".resj";
    if (JSIL.Host.doesAssetExist(key))
      return JSIL.Host.getAsset(key);

    return null;
  });

  $.Method({ Static: false, Public: true }, "GetResourceSet",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Resources.ResourceSet"), [
          $jsilcore.TypeRef("System.Globalization.CultureInfo"), $.Boolean,
          $.Boolean
    ], [])),
    function GetResourceSet(culture, createIfNotExists, tryParents) {
      if (!culture)
        culture = $jsilcore.getCurrentUICultureImpl();

      var resources = this.$findResourcesForCulture(culture);
      if (!resources)
        throw new System.Exception("No resources available for culture '" + culture.get_Name() + "'.");

      var result = this._resourceCollection[culture.get_Name()];
      if (!result) {
        var tSet = System.Resources.ResourceSet.__Type__;
        result = this._resourceCollection[culture.get_Name()] = JSIL.CreateInstanceOfType(
          tSet, "$fromResources", [this, resources]
        );
      }

      return result;
    }
  );

  $.Method({ Static: false, Public: true }, "GetStream",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.IO.UnmanagedMemoryStream"), [$.String], [])),
    function GetStream(name) {
      var set = this.GetResourceSet($jsilcore.getCurrentUICultureImpl(), false, true)
      return set.GetStream(name);
    }
  );

  $.Method({ Static: false, Public: true }, "GetStream",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.IO.UnmanagedMemoryStream"), [$.String, $jsilcore.TypeRef("System.Globalization.CultureInfo")], [])),
    function GetStream(name, culture) {
      var set = this.GetResourceSet(culture, false, true)
      return set.GetStream(name);
    }
  );

  $.Method({ Static: false, Public: true }, "GetString",
    (new JSIL.MethodSignature($.String, [$.String], [])),
    function GetString(name) {
      var set = this.GetResourceSet($jsilcore.getCurrentUICultureImpl(), false, true)
      return set.GetString(name);
    }
  );

  $.Method({ Static: false, Public: true }, "GetString",
    (new JSIL.MethodSignature($.String, [$.String, $jsilcore.TypeRef("System.Globalization.CultureInfo")], [])),
    function GetString(name, culture) {
      var set = this.GetResourceSet(culture, false, true)
      return set.GetString(name);
    }
  );

});
JSIL.ImplementExternals("System.Globalization.CultureInfo", function ($) {
  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function _ctor(name) {
      this.m_name = name;
      this.m_useUserOverride = true;
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String, $.Boolean], [])),
    function _ctor(name, useUserOverride) {
      this.m_name = name;
      this.m_useUserOverride = useUserOverride;
    }
  );

  $.Method({ Static: true, Public: true }, "get_InvariantCulture",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [], []),
    function () {
      if (typeof this.m_invariantCultureInfo == 'undefined') {
        this.m_invariantCultureInfo = new System.Globalization.CultureInfo('', false);
      }
      return this.m_invariantCultureInfo;
    }
  );

  $.Method({ Static: false, Public: true }, "Clone",
    (new JSIL.MethodSignature($.Object, [], [])),
    function get_Name() {
      // FIXME
      return new System.Globalization.CultureInfo(this.m_name, this.m_useUserOverride);
    }
  );

  $.Method({ Static: false, Public: true }, "get_Name",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_Name() {
      return this.m_name;
    }
  );

  $.Method({ Static: false, Public: true }, "get_TwoLetterISOLanguageName",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_TwoLetterISOLanguageName() {
      var parts = this.m_name.split("-");
      return parts[0];
    }
  );

  $.Method({ Static: false, Public: true }, "get_UseUserOverride",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_UseUserOverride() {
      return this.m_useUserOverride;
    }
  );

  $.Method({ Static: true, Public: false }, "GetCultureByName",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [$.String, $.Boolean], [])),
    function GetCultureByName(name, userOverride) {
      return new $jsilcore.System.Globalization.CultureInfo(name, userOverride);
    }
  );

  $.Method({ Static: true, Public: true }, "GetCultureInfo",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [$.String], [])),
    function GetCultureInfo(name) {
      return new $jsilcore.System.Globalization.CultureInfo(name);
    }
  );

  $.Method({ Static: true, Public: true }, "GetCultureInfoByIetfLanguageTag",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [$.String], [])),
    function GetCultureInfoByIetfLanguageTag(name) {
      return $jsilcore.System.Globalization.CultureInfo.GetCultureInfo(name);
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsReadOnly",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsReadOnly() {
      return true;
    }
  );

  $.Method({ Static: false, Public: true }, "get_NumberFormat",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.NumberFormatInfo"), [], [])),
    function get_NumberFormat() {
      if (this.numInfo === null) {
          this.numInfo = new $jsilcore.System.Globalization.NumberFormatInfo();
      }
      return this.numInfo;
    }
  );
});

JSIL.ImplementExternals("System.Globalization.CultureInfo", function ($) {
  $.Method({ Static: true, Public: true }, "get_CurrentUICulture",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [], [])),
    function get_CurrentUICulture() {
      return $jsilcore.getCurrentUICultureImpl();
    }
  );

  $.Method({ Static: true, Public: true }, "get_CurrentCulture",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [], [])),
    function get_CurrentCulture() {
      // FIXME
      return $jsilcore.getCurrentUICultureImpl();
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "GetFormat",
    (new JSIL.MethodSignature($.Object, [$jsilcore.TypeRef("System.Type")], [])),
    function CultureInfo_GetFormat(formatType) {
      if ($jsilcore.System.Type.op_Equality(formatType, $jsilcore.System.Globalization.NumberFormatInfo.__Type__)) {
        return this.get_NumberFormat();
      }
      if ($jsilcore.System.Type.op_Equality(formatType, $jsilcore.System.Globalization.DateTimeFormatInfo.__Type__)) {
        return this.get_DateTimeFormat();
      }
      return null;
    }
  );
});

JSIL.MakeClass("System.Object", "System.Globalization.CultureInfo", true, [], function ($) {
  $.ExternalMethod({ Static: false, Public: true, Virtual: true }, "GetFormat", (new JSIL.MethodSignature($.Object, [$jsilcore.TypeRef("System.Type")], [])));
  $.Field({ Public: false, Static: false }, "numInfo", $jsilcore.TypeRef("System.Globalization.NumberFormatInfo"));

  $.ImplementInterfaces($jsilcore.TypeRef("System.IFormatProvider"));
})
JSIL.ImplementExternals("System.Threading.Thread", function ($) {
  $.Method({ Static: false, Public: true }, "get_CurrentUICulture",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [], [])),
    function get_CurrentUICulture() {
      return $jsilcore.getCurrentUICultureImpl();
    }
  );

  $.Method({ Static: false, Public: true }, "get_CurrentCulture",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Globalization.CultureInfo"), [], [])),
    function get_CurrentCulture() {
      // FIXME
      return $jsilcore.getCurrentUICultureImpl();
    }
  );
});

