/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

JSIL.DeclareAssembly("JSIL.Windows");

JSIL.DeclareNamespace("JSIL");

var $wfasms = new JSIL.AssemblyCollection({
  5: "mscorlib",
  6: "System",
  8: "System.Core",
  10: "System.Drawing",
  12: "System.Windows.Forms",
  13: "System.Xml"
});

JSIL.ImplementExternals("System.Windows.Forms.Control", function ($) {
  var coreCtor = function _ctor (text) {
    this._controls = new $wfasms[12].System.Windows.Forms.Control_ControlCollection ();
    this.clientSize = new System.Drawing.Size(0, 0);
    this.size = new System.Drawing.Size(0, 0);
    this.location = new System.Drawing.Point(0, 0);

    if (text)
      this.text = text;
    else
      this.text = null;
  };

  $.RawMethod(false, "$coreCtor", coreCtor);

  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    coreCtor
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String], [])),
    coreCtor
  );

  $.Method({Static:false, Public:true }, "Show",
    (JSIL.MethodSignature.Void),
    function Show () {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "get_Controls",
    (new JSIL.MethodSignature($wfasms[12].TypeRef("System.Windows.Forms.Control+ControlCollection"), [], [])),
    function get_Controls () {
      return this._controls;
    }
  );

  $.Method({Static:false, Public:true }, "get_ClientSize",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Size"), [], [])),
    function get_ClientSize () {
      return this.clientSize;
    }
  );

  $.Method({Static:false, Public:true }, "set_ClientSize",
    (new JSIL.MethodSignature(null, [$wfasms[10].TypeRef("System.Drawing.Size")], [])),
    function set_ClientSize (value) {
      this.clientSize = value;
    }
  );

  $.Method({Static:false, Public:true }, "get_Size",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Size"), [], [])),
    function get_Size () {
      return this.size;
    }
  );

  $.Method({Static:false, Public:true }, "set_Size",
    (new JSIL.MethodSignature(null, [$wfasms[10].TypeRef("System.Drawing.Size")], [])),
    function set_Size (value) {
      this.size = value;
    }
  );

  $.Method({Static:false, Public:true }, "get_Text",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_Text () {
      return this.text;
    }
  );

  $.Method({Static:false, Public:true }, "set_Text",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function set_Text (value) {
      this.text = value;
    }
  );

  $.Method({Static:false, Public:true }, "get_Left",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Left () {
      return this.location.X;
    }
  );

  $.Method({Static:false, Public:true }, "get_Location",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Point"), [], [])),
    function get_Location () {
      return this.location;
    }
  );

  $.Method({Static:false, Public:true }, "get_Top",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Top () {
      return this.location.Y;
    }
  );

  $.Method({Static:false, Public:true }, "get_Height",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Height () {
      return this.size.Height;
    }
  );

  $.Method({Static:false, Public:true }, "get_Width",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Width () {
      return this.size.Width;
    }
  );

  $.Method({Static:false, Public:true }, "get_Bottom",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Bottom () {
      return this.location.Y + this.size.Height;
    }
  );

  $.Method({Static:false, Public:true }, "get_Right",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Right () {
      return this.location.X + this.size.Width;
    }
  );

  $.Method({Static:false, Public:true }, "set_Left",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function set_Left (value) {
      this.location.X = value;
    }
  );

  $.Method({Static:false, Public:true }, "set_Location",
    (new JSIL.MethodSignature(null, [$wfasms[10].TypeRef("System.Drawing.Point")], [])),
    function set_Location (value) {
      this.location = value;
    }
  );

  $.Method({Static:false, Public:true }, "set_Top",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function set_Top (value) {
      this.location.Y = value;
    }
  );

  $.Method({Static:false, Public:true }, "set_Height",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function set_Height (value) {
      this.size.Height = value;
    }
  );

  $.Method({Static:false, Public:true }, "set_Width",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function set_Width (value) {
      this.size.Width = value;
    }
  );

  $.Method({Static:false, Public:true }, "set_Dock",
    (new JSIL.MethodSignature(null, [$wfasms[12].TypeRef("System.Windows.Forms.DockStyle")], [])),
    function set_Dock (value) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, "add_Click",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.EventHandler")], [])),
    function add_Click (value) {
      // FIXME
    }
  );

  $.Method({Static: true, Public: true }, "FromHandle",
    (new JSIL.MethodSignature($.Type, [$jsilcore.TypeRef("System.IntPtr")], [])),
    function FromHandle (handle) {
      // FIXME
      return null;
    }
  );
});

JSIL.ImplementExternals("System.Windows.Forms.Control+ControlCollection", function ($) {
  $jsilcore.$ListExternals($, $wfasms[12].TypeRef("System.Windows.Forms.Control"), "ArrangedElementCollection");
});

JSIL.ImplementExternals("System.Windows.Forms.StatusBar+StatusBarPanelCollection", function ($) {
  $jsilcore.$ListExternals($, $wfasms[12].TypeRef("System.Windows.Forms.StatusBarPanel"), "ObjectCollection");
});

JSIL.ImplementExternals("System.Windows.Forms.TabControl+TabPageCollection", function ($) {
  $jsilcore.$ListExternals($, $wfasms[12].TypeRef("System.Windows.Forms.TabPage"), "ArrangedElementCollection");
});

JSIL.ImplementExternals("System.Windows.Forms.ListBox+ObjectCollection", function ($) {
  $jsilcore.$ListExternals($, $.Object, "ObjectCollection");
});

JSIL.ImplementExternals("System.Windows.Forms.Form", function ($) {
  $.InheritDefaultConstructor();
});

JSIL.ImplementExternals("System.Windows.Forms.ListBox", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      this.$coreCtor();
      this._items = new $wfasms[12].System.Windows.Forms.ListBox_ObjectCollection ();
    }
  );

  $.Method({Static:false, Public:true }, "get_Items",
    (new JSIL.MethodSignature($wfasms[12].TypeRef("System.Windows.Forms.ListBox+ObjectCollection"), [], [])),
    function get_Items () {
      return this._items;
    }
  );

  $.Method({Static:false, Public:true }, "get_SelectedIndex",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_SelectedIndex () {
      return 0;
    }
  );

  $.Method({Static:false, Public:true }, "get_SelectedItem",
    (new JSIL.MethodSignature($.Object, [], [])),
    function get_SelectedItem () {
      return null;
    }
  );
});

JSIL.ImplementExternals("System.Windows.Forms.StatusBar", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      this.$coreCtor();
      this._panels = new $wfasms[12].System.Windows.Forms.StatusBar_StatusBarPanelCollection ();
    }
  );

  $.Method({Static:false, Public:true }, "get_Panels",
    (new JSIL.MethodSignature($wfasms[12].TypeRef("System.Windows.Forms.StatusBar+StatusBarPanelCollection"), [], [])),
    function get_Panels () {
      return this._panels;
    }
  );
});

JSIL.ImplementExternals("System.Windows.Forms.TabControl", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      this.$coreCtor();
      this._tabPages = new $wfasms[12].System.Windows.Forms.TabControl_TabPageCollection ();
    }
  );

  $.Method({Static:false, Public:true }, "get_TabPages",
    (new JSIL.MethodSignature($wfasms[12].TypeRef("System.Windows.Forms.TabControl+TabPageCollection"), [], [])),
    function get_TabPages () {
      return this._tabPages;
    }
  );
});


JSIL.ImplementExternals("System.Windows.Forms.Screen", function ($) {
  var getBoundsImpl = function () {
    var canvas = JSIL.Host.getCanvas();

    if (jsilConfig.getScreenDimensions) {
      var sd = jsilConfig.getScreenDimensions();
      return new System.Drawing.Rectangle(
        0, 0, sd[0], sd[1]
      );
    } else {
      return new System.Drawing.Rectangle(
        0, 0, canvas.width, canvas.height
      );
    }
  };

  $.Method({Static:false, Public:true }, "get_Bounds",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [], [])),
    function get_Bounds () {
      return getBoundsImpl();
    }
  );

  $.Method({Static:true , Public:true }, "get_PrimaryScreen",
    (new JSIL.MethodSignature($wfasms[12].TypeRef("System.Windows.Forms.Screen"), [], [])),
    function get_PrimaryScreen () {
      return JSIL.CreateInstanceObject(System.Windows.Forms.Screen.prototype);
    }
  );

  $.Method({Static:false, Public:true }, "get_WorkingArea",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [], [])),
    function get_WorkingArea () {
      return getBoundsImpl();
    }
  );

  $.Method({Static:true , Public:true }, "GetBounds",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [$wfasms[10].TypeRef("System.Drawing.Point")], [])),
    function GetBounds (pt) {
      return getBoundsImpl();
    }
  );

  $.Method({Static:true , Public:true }, "GetBounds",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [$wfasms[10].TypeRef("System.Drawing.Rectangle")], [])),
    function GetBounds (rect) {
      return getBoundsImpl();
    }
  );

  $.Method({Static:true , Public:true }, "GetBounds",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [$wfasms[12].TypeRef("System.Windows.Forms.Control")], [])),
    function GetBounds (ctl) {
      return getBoundsImpl();
    }
  );

  $.Method({Static:true , Public:true }, "GetWorkingArea",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [$wfasms[10].TypeRef("System.Drawing.Point")], [])),
    function GetWorkingArea (pt) {
      return getBoundsImpl();
    }
  );

  $.Method({Static:true , Public:true }, "GetWorkingArea",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [$wfasms[10].TypeRef("System.Drawing.Rectangle")], [])),
    function GetWorkingArea (rect) {
      return getBoundsImpl();
    }
  );

  $.Method({Static:true , Public:true }, "GetWorkingArea",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Rectangle"), [$wfasms[12].TypeRef("System.Windows.Forms.Control")], [])),
    function GetWorkingArea (ctl) {
      return getBoundsImpl();
    }
  );
});


JSIL.ImplementExternals("System.Windows.Forms.MenuItem", function ($) {
  var $thisType = $.publicInterface;

  $.Method({Static:false, Public:true }, ".ctor",
    (JSIL.MethodSignature.Void),
    function _ctor () {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function _ctor (text) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String, $jsilcore.TypeRef("System.EventHandler")], [])),
    function _ctor (text, onClick) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.String, $jsilcore.TypeRef("System.EventHandler"),
          $wfasms[12].TypeRef("System.Windows.Forms.Shortcut")
        ], [])),
    function _ctor (text, onClick, shortcut) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String, $jsilcore.TypeRef("System.Array", [$wfasms[12].TypeRef("System.Windows.Forms.MenuItem")])], [])),
    function _ctor (text, items) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$wfasms[12].TypeRef("System.Windows.Forms.MenuItem+MenuItemData")], [])),
    function _ctor (data) {
      // FIXME
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $wfasms[12].TypeRef("System.Windows.Forms.MenuMerge"), $.Int32,
          $wfasms[12].TypeRef("System.Windows.Forms.Shortcut"), $.String,
          $jsilcore.TypeRef("System.EventHandler"), $jsilcore.TypeRef("System.EventHandler"),
          $jsilcore.TypeRef("System.EventHandler"), $jsilcore.TypeRef("System.Array", [$wfasms[12].TypeRef("System.Windows.Forms.MenuItem")])
        ], [])),
    function _ctor (mergeType, mergeOrder, shortcut, text, onClick, onPopup, onSelect, items) {
      // FIXME
    }
  );
});


JSIL.ImplementExternals("System.Windows.Forms.SystemInformation", function ($) {
  $.Method({Static:true , Public:true }, "get_Border3DSize",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Size"), [], [])),
    function get_Border3DSize () {
      return new $wfasms[10].System.Drawing.Size(0, 0);
    }
  );

  $.Method({Static:true , Public:true }, "get_BorderSize",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Size"), [], [])),
    function get_BorderSize () {
      return new $wfasms[10].System.Drawing.Size(0, 0);
    }
  );

  $.Method({Static:true , Public:true }, "get_CaptionHeight",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_CaptionHeight () {
      return new $wfasms[10].System.Drawing.Size(0, 0);
    }
  );

  $.Method({Static:true , Public:true }, "get_FixedFrameBorderSize",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Size"), [], [])),
    function get_FixedFrameBorderSize () {
      return new $wfasms[10].System.Drawing.Size(0, 0);
    }
  );

  $.Method({Static:true , Public:true }, "get_FrameBorderSize",
    (new JSIL.MethodSignature($wfasms[10].TypeRef("System.Drawing.Size"), [], [])),
    function get_FrameBorderSize () {
      return new $wfasms[10].System.Drawing.Size(0, 0);
    }
  );

});