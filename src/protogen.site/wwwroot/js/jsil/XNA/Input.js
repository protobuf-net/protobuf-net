/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

if (typeof ($jsilxna) === "undefined")
  throw new Error("JSIL.XNACore required");

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.Keyboard", function ($) {
  var getStateImpl = function (playerIndex) {
    var keys = JSIL.Host.getHeldKeys();
    return new Microsoft.Xna.Framework.Input.KeyboardState(keys);
  };

  $.Method({Static:true , Public:true }, "GetState",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.KeyboardState"), [], [])),
    getStateImpl
  );

  $.Method({Static:true , Public:true }, "GetState",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.KeyboardState"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex")], [])),
    getStateImpl
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.KeyboardState", function ($) {
  $.RawMethod(false, "__CopyMembers__", function (source, target) {
    if (source.keys)
      target.keys = Array.prototype.slice.call(source.keys);
    else
      target.keys = [];
  });

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.Keys")])], [])),
    function _ctor (keys) {
      this.keys = [];

      for (var i = 0; i < keys.length; i++)
        this.keys.push(keys[i].valueOf());
    }
  );

  $.Method({Static:false, Public:true }, "GetPressedKeys",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.Keys")]), [], [])),
    function GetPressedKeys () {
      if (!this.keys)
        return [];

      var result = [];
      var tKeys = $xnaasms[0].Microsoft.Xna.Framework.Input.Keys.__Type__;

      for (var i = 0, l = this.keys.length; i < l; i++)
        result.push(tKeys.$Cast(this.keys[i]));

      return result;
    }
  );

  $.Method({Static:false, Public:true }, "IsKeyDown",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.Keys")], [])),
    function IsKeyDown (key) {
      if (!this.keys)
        return false;

      return this.keys.indexOf(key.valueOf()) !== -1;
    }
  );

  $.Method({Static:false, Public:true }, "IsKeyUp",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.Keys")], [])),
    function IsKeyUp (key) {
      if (!this.keys)
        return true;

      return this.keys.indexOf(key.valueOf()) === -1;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.Mouse", function ($) {
  var getStateImpl = function (playerIndex) {
    var buttons = JSIL.Host.getHeldMouseButtons();
    var position = JSIL.Host.getMousePosition();

    var pressed = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Pressed;
    var released = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Released;

    // FIXME
    return new Microsoft.Xna.Framework.Input.MouseState(
      position[0], position[1], 0,
      (buttons.indexOf(0) >= 0) ? pressed : released,
      (buttons.indexOf(1) >= 0) ? pressed : released,
      (buttons.indexOf(2) >= 0) ? pressed : released,
      released, released
    );
  };

  $.Method({Static:true , Public:true }, "GetState",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.MouseState"), [], [])),
    getStateImpl
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.MouseState", function ($) {

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.Int32, $.Int32,
          $.Int32, $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState")
        ], [])),
    function _ctor (x, y, scrollWheel, leftButton, middleButton, rightButton, xButton1, xButton2) {
      // FIXME
      this.x = x;
      this.y = y;
      this.leftButton = leftButton;
      this.middleButton = middleButton;
      this.rightButton = rightButton;
    }
  );

  $.Method({Static:false, Public:true }, "get_LeftButton",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
    function get_LeftButton () {
      return this.leftButton;
    }
  );

  $.Method({Static:false, Public:true }, "get_MiddleButton",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
    function get_MiddleButton () {
      return this.middleButton;
    }
  );

  $.Method({Static:false, Public:true }, "get_RightButton",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
    function get_RightButton () {
      return this.rightButton;
    }
  );

  $.Method({Static:false, Public:true }, "get_X",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_X () {
      return this.x;
    }
  );

  $.Method({Static:false, Public:true }, "get_Y",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Y () {
      return this.y;
    }
  );
});

$jsilxna.deadZone = function (value, max, deadZoneSize) {
  if (value < -deadZoneSize)
    value += deadZoneSize;
  else if (value <= deadZoneSize)
    return 0;
  else
    value -= deadZoneSize;

  var scaled = value / (max - deadZoneSize);
  if (scaled < -1)
    scaled = -1;
  else if (scaled > 1)
    scaled = 1;

  return scaled;
};

$jsilxna.deadZoneToPressed = function (value, max, deadZoneSize) {
  var pressed = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Pressed;
  var released = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Released;

  var scaled = $jsilxna.deadZone(value, max, deadZoneSize);
  if (Math.abs(scaled) > 0)
    return pressed;
  else
    return released;
};

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.GamePad", function ($) {
  var buttons = $xnaasms[0].Microsoft.Xna.Framework.Input.Buttons;
  var pressed = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Pressed;
  var released = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Released;

  var buttonsFromGamepadState = function (state) {
    var buttonStates = 0;

    if (state.faceButton0)
      buttonStates |= buttons.A;
    if (state.faceButton1)
      buttonStates |= buttons.B;
    if (state.faceButton2)
      buttonStates |= buttons.X;
    if (state.faceButton3)
      buttonStates |= buttons.Y;

    if (state.leftShoulder0)
      buttonStates |= buttons.LeftShoulder;
    if (state.rightShoulder0)
      buttonStates |= buttons.RightShoulder;

    if (state.select)
      buttonStates |= buttons.Back;
    if (state.start)
      buttonStates |= buttons.Start;

    if (state.leftStickButton)
      buttonStates |= buttons.LeftStick;
    if (state.rightStickButton)
      buttonStates |= buttons.RightStick;

    var result = buttons.$Cast(buttonStates);
    return result;
  };

  var getStateImpl = function (playerIndex) {
    var connected = false;
    var buttonStates = 0;
    var leftThumbstick = new Microsoft.Xna.Framework.Vector2(0, 0);
    var rightThumbstick = new Microsoft.Xna.Framework.Vector2(0, 0);
    var leftTrigger = 0, rightTrigger = 0;
    var dpadUp = released, dpadDown = released, dpadLeft = released, dpadRight = released;

    var svc = JSIL.Host.getService("gamepad", true);
    var state = null;
    if (svc)
      state = svc.getState(playerIndex.value);

    if (state) {
      connected = true;

      var blockInput = $jsilbrowserstate && $jsilbrowserstate.blockGamepadInput;

      if (!blockInput) {
        buttonStates = buttonsFromGamepadState(state);

        // FIXME: This is IndependentAxes mode. Maybe handle Circular too?
        var leftStickDeadZone = 7849 / 32767;
        var rightStickDeadZone = 8689 / 32767;

        leftThumbstick.X  = $jsilxna.deadZone(state.leftStickX, 1, leftStickDeadZone);
        rightThumbstick.X = $jsilxna.deadZone(state.rightStickX, 1, rightStickDeadZone);

        // gamepad.js returns inverted Y compared to XInput... weird.
        leftThumbstick.Y  = -$jsilxna.deadZone(state.leftStickY, 1, leftStickDeadZone);
        rightThumbstick.Y = -$jsilxna.deadZone(state.rightStickY, 1, rightStickDeadZone);

        leftTrigger  = state.leftShoulder1;
        rightTrigger = state.rightShoulder1;

        dpadUp    = state.dpadUp    ? pressed : released;
        dpadDown  = state.dpadDown  ? pressed : released;
        dpadLeft  = state.dpadLeft  ? pressed : released;
        dpadRight = state.dpadRight ? pressed : released;
      }
    }

    var buttons = new Microsoft.Xna.Framework.Input.GamePadButtons(
      buttonStates
    );

    var thumbs = new Microsoft.Xna.Framework.Input.GamePadThumbSticks(
      leftThumbstick, rightThumbstick
    );

    var triggers = new Microsoft.Xna.Framework.Input.GamePadTriggers(
      leftTrigger, rightTrigger
    );

    var dpad = new Microsoft.Xna.Framework.Input.GamePadDPad(
      dpadUp, dpadDown, dpadLeft, dpadRight
    );

    var result = JSIL.CreateInstanceOfType(
      Microsoft.Xna.Framework.Input.GamePadState.__Type__,
      "$internalCtor",
      [connected, thumbs, triggers, buttons, dpad]
    );
    return result;
  };

  $.Method({Static:true , Public:true }, "GetState",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadState"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex")], [])),
    getStateImpl
  );

  $.Method({Static:true , Public:true }, "GetState",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadState"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadDeadZone")], [])),
    getStateImpl
  );

  $.Method({Static:true , Public:true }, "SetVibration",
    (new JSIL.MethodSignature($.Boolean, [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex"), $.Single,
          $.Single
        ], [])),
    function SetVibration (playerIndex, leftMotor, rightMotor) {
      // FIXME
    }
  );

  $.Method({Static:true , Public:true }, "GetCapabilities",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadCapabilities"), [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.PlayerIndex")], [])),
    function GetCapabilities (playerIndex) {
      var svc = JSIL.Host.getService("gamepad", true);
      var state = null;
      if (svc)
        state = svc.getState(playerIndex.value);

      var result = JSIL.CreateInstanceOfType(
        Microsoft.Xna.Framework.Input.GamePadCapabilities.__Type__,
        "$internalCtor",
        [Boolean(state)]
      );
      return result;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.GamePadCapabilities", function ($) {

  $.RawMethod(false, "$internalCtor", function (connected) {
    this._connected = connected;
  });

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.XINPUT_CAPABILITIES")]), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.ErrorCodes")], [])),
    function _ctor (/* ref */ caps, result) {
      this._connected = false;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsConnected",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsConnected () {
      return this._connected;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.GamePadState", function ($) {
  var buttons = $xnaasms[0].Microsoft.Xna.Framework.Input.Buttons;
  var pressed = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Pressed;
  var released = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Released;

  $.RawMethod(false, "$internalCtor", function GamePadState_internalCtor (connected, thumbSticks, triggers, buttons, dPad) {
    this._connected = connected;
    this._thumbs = thumbSticks;
    this._buttons = buttons;
    this._triggers = triggers;
    this._dpad = dPad;
  });

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadThumbSticks"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadTriggers"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadButtons"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadDPad")
        ], [])),
    function _ctor (thumbSticks, triggers, buttons, dPad) {
      this.$internalCtor(false, thumbSticks, triggers, buttons, dPad);
    }
  );

  $.Method({Static:false, Public:true }, "get_Buttons",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadButtons"), [], [])),
    function get_Buttons () {
      return this._buttons;
    }
  );

  $.Method({Static:false, Public:true }, "get_DPad",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadDPad"), [], [])),
    function get_DPad () {
      return this._dpad;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsConnected",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsConnected () {
      // FIXME
      return this._connected;
    }
  );

  $.Method({Static:false, Public:true }, "get_ThumbSticks",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadThumbSticks"), [], [])),
    function get_ThumbSticks () {
      return this._thumbs;
    }
  );

  $.Method({Static:false, Public:true }, "get_Triggers",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.GamePadTriggers"), [], [])),
    function get_Triggers () {
      return this._triggers;
    }
  );

  var getButtonState = function (self, button) {
    var flags = self._buttons._flags;
    var key = button.valueOf();

    if ((flags & key) === key)
      return pressed;

    var triggerDeadZone = 30 / 255;

    switch (key) {
      // DPad
      case 1:
        return self._dpad._up;
      case 2:
        return self._dpad._down;
      case 4:
        return self._dpad._left;
      case 8:
        return self._dpad._right;

      // Triggers
      case 8388608:
        return $jsilxna.deadZoneToPressed(self._triggers._left, 1, triggerDeadZone);
      case 4194304:
        return $jsilxna.deadZoneToPressed(self._triggers._right, 1, triggerDeadZone);
    }

    return released;
  };

  $.Method({Static:false, Public:true }, "IsButtonDown",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.Buttons")], [])),
    function IsButtonDown (button) {
      return (getButtonState(this, button).valueOf() !== 0);
    }
  );

  $.Method({Static:false, Public:true }, "IsButtonUp",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.Buttons")], [])),
    function IsButtonUp (button) {
      return (getButtonState(this, button).valueOf() === 0);
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.GamePadButtons", function ($) {
  var buttonNames = [
    "A", "B", "Back", "BigButton",
    "LeftShoulder", "LeftStick", "RightShoulder", "RightStick",
    "Start", "X", "Y"
  ];

  var buttons = $xnaasms[0].Microsoft.Xna.Framework.Input.Buttons;
  var pressed = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Pressed;
  var released = $xnaasms[0].Microsoft.Xna.Framework.Input.ButtonState.Released;

  var makeButtonGetter = function (buttonName) {
    var buttonFlag = buttons[buttonName].valueOf();

    return function getButtonState () {
      if ((this._flags & buttonFlag) === buttonFlag)
        return pressed;

      return released;
    };
  }

  for (var i = 0; i < buttonNames.length; i++) {
    var buttonName = buttonNames[i];

    $.Method({Static:false, Public:true }, "get_" + buttonName,
      (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
      makeButtonGetter(buttonName)
    );
  }

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.Buttons")], [])),
    function _ctor (buttonState) {
      this._flags = buttonState;
    }
  );

  $.RawMethod(false, "__CopyMembers__",
    function GamePadButtons_CopyMembers (source, target) {
      target._flags = source._flags;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.GamePadThumbSticks", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2")], [])),
    function _ctor (leftThumbstick, rightThumbstick) {
      this._left = leftThumbstick;
      this._right = rightThumbstick;
    }
  );

  $.Method({Static:false, Public:true }, "get_Left",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [], [])),
    function get_Left () {
      return this._left;
    }
  );

  $.Method({Static:false, Public:true }, "get_Right",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [], [])),
    function get_Right () {
      return this._right;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.GamePadDPad", function ($) {

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"),
          $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), $xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState")
        ], [])),
    function _ctor (upValue, downValue, leftValue, rightValue) {
      this._up = upValue;
      this._down = downValue;
      this._left = leftValue;
      this._right = rightValue;
    }
  );

  $.Method({Static:false, Public:true }, "get_Down",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
    function get_Down () {
      return this._down;
    }
  );

  $.Method({Static:false, Public:true }, "get_Left",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
    function get_Left () {
      return this._left;
    }
  );

  $.Method({Static:false, Public:true }, "get_Right",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
    function get_Right () {
      return this._right;
    }
  );

  $.Method({Static:false, Public:true }, "get_Up",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Input.ButtonState"), [], [])),
    function get_Up () {
      return this._up;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.GamePadTriggers", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.Single, $.Single], [])),
    function _ctor (leftTrigger, rightTrigger) {
      this._left = leftTrigger;
      this._right = rightTrigger;
    }
  );

  $.Method({Static:false, Public:true }, "get_Left",
    (new JSIL.MethodSignature($.Single, [], [])),
    function get_Left () {
      return this._left;
    }
  );

  $.Method({Static:false, Public:true }, "get_Right",
    (new JSIL.MethodSignature($.Single, [], [])),
    function get_Right () {
      return this._right;
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.Touch.TouchPanel", function ($) {
  $.Method({Static:true , Public:true }, "get_DisplayHeight",
    new JSIL.MethodSignature($.Int32, [], []),
    function get_DisplayHeight () {
      // FIXME
      var canvas = JSIL.Host.getCanvas();
      return canvas.height;
    }
  );

  $.Method({Static:true , Public:true }, "get_DisplayWidth",
    new JSIL.MethodSignature($.Int32, [], []),
    function get_DisplayWidth () {
      // FIXME
      var canvas = JSIL.Host.getCanvas();
      return canvas.width;
    }
  );

  $.Method({Static:true , Public:true }, "get_IsGestureAvailable",
    new JSIL.MethodSignature($.Boolean, [], []),
    function get_IsGestureAvailable () {
      return false;
    }
  );

  $.Method({Static:true , Public:true }, "GetState",
    (new JSIL.MethodSignature($xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchCollection"), [], [])),
    function GetState () {
      var tCollection = Microsoft.Xna.Framework.Input.Touch.TouchCollection.__Type__;
      var result = JSIL.CreateInstanceOfType(tCollection, "$fromHostState", [this.prevState]);
      this.prevState = result;
      return result;
    }
  );

  $.Method({Static:true , Public:true }, "ReadGesture",
    (new JSIL.MethodSignature($xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.GestureSample"), [], [])),
    function ReadGesture () {
      throw new Error('Not implemented');
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.Touch.TouchCollection", function ($) {
  $.RawMethod(false, "$fromHostState", function (previousState) {
    this.isConnected = JSIL.Host.isTouchInUse;
    this.touches = [];

    var nativeTouches = JSIL.Host.currentNativeTouches;

    if (!nativeTouches)
      return;

    if (!previousState)
      previousState = new $xnaasms[4].Microsoft.Xna.Framework.Input.Touch.TouchCollection();

    var tTouchLocation = $xnaasms[4].Microsoft.Xna.Framework.Input.Touch.TouchLocation.__Type__;

    for (var i = 0, l = nativeTouches.length; i < l; i++) {
      var nativeTouch = nativeTouches[i];
      var touch = JSIL.CreateInstanceOfType(tTouchLocation, "$fromNativeTouch", [previousState, nativeTouch]);

      this.touches.push(touch);
    }
  });

  $.RawMethod(false, "__CopyMembers__", function (source, target) {
    target.isConnected = source.isConnected;
    target.touches = Array.prototype.slice.call(source.touches);
  });

  $.RawMethod(false, "$getTouchById", function (id) {
    for (var i = 0, l = this.touches.length; i < l; i++) {
      var touch = this.touches[i];
      if (touch.id === id)
        return touch;
    }

    return null;
  });

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")])], [])),
    function _ctor (touches) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "Add",
    (new JSIL.MethodSignature(null, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], [])),
    function Add (item) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "Clear",
    (JSIL.MethodSignature.Void),
    function Clear () {
      this.touches.length = 0;
    }
  );

  $.Method({Static:false, Public:true }, "Contains",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], [])),
    function Contains (item) {
      // FIXME
      return false;
    }
  );

  $.Method({Static:false, Public:true }, "CopyTo",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Array", [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")]), $.Int32], [])),
    function CopyTo (array, arrayIndex) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "FindById",
    (new JSIL.MethodSignature($.Boolean, [$.Int32, $jsilcore.TypeRef("JSIL.Reference", [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")])], [])),
    function FindById (id, /* ref */ touchLocation) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "get_Count",
    (new JSIL.MethodSignature($.Int32, [], [])),
    function get_Count () {
      return this.touches.length;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsConnected",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsConnected () {
      return this.isConnected;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsReadOnly",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsReadOnly () {
      return true;
    }
  );

  $.Method({Static:false, Public:true }, "get_Item",
    (new JSIL.MethodSignature($xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation"), [$.Int32], [])),
    function get_Item (index) {
      return this.touches[index];
    }
  );

  $.Method({Static:false, Public:true }, "GetEnumerator",
    (new JSIL.MethodSignature($xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchCollection+Enumerator"), [], [])),
    function GetEnumerator () {
      var result = new $xnaasms[4].Microsoft.Xna.Framework.Input.Touch.TouchCollection_Enumerator(this);
      return result;
    }
  );

  $.Method({Static:false, Public:true }, "IndexOf",
    (new JSIL.MethodSignature($.Int32, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], [])),
    function IndexOf (item) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "Insert",
    (new JSIL.MethodSignature(null, [$.Int32, $xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], [])),
    function Insert (index, item) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "Remove",
    (new JSIL.MethodSignature($.Boolean, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], [])),
    function Remove (item) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "RemoveAt",
    (new JSIL.MethodSignature(null, [$.Int32], [])),
    function RemoveAt (index) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "set_Item",
    (new JSIL.MethodSignature(null, [$.Int32, $xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], [])),
    function set_Item (index, value) {
      throw new Error('Not implemented');
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.Touch.TouchCollection+Enumerator", function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.Method({Static:false, Public:false}, ".ctor",
    new JSIL.MethodSignature(null, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchCollection")], []),
    function _ctor (collection) {
      this.collection = collection;
      this.position = -1;
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "Dispose",
    JSIL.MethodSignature.Void,
    function Dispose () {
      this.position = -1;
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "get_Current",
    new JSIL.MethodSignature($xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation"), [], []),
    function get_Current () {
      return this.collection.touches[this.position];
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "MoveNext",
    new JSIL.MethodSignature($.Boolean, [], []),
    function MoveNext () {
      this.position += 1;
      return (this.position < this.collection.touches.length);
    }
  );

  $.Method({Static:false, Public:false }, null,
    new JSIL.MethodSignature($.Object, [], []),
    function System_Collections_IEnumerator_get_Current () {
      return this.collection.touches[this.position];
    }
  )
    .Overrides("System.Collections.IEnumerator", "get_Current");

  $.Method({Static:false, Public:false, Virtual:true }, "Reset",
    JSIL.MethodSignature.Void,
    function System_Collections_IEnumerator_Reset () {
      this.position = -1;
    }
  )
    .Overrides("System.Collections.IEnumerator", "Reset");
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Input.Touch.TouchLocation", function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.RawMethod(false, "$asPrevious", function (source) {
    this.id = source.id;
    this.x = source.prevX;
    this.y = source.prevY;
    this.state = source.prevState;
  });

  $.RawMethod(false, "$fromNativeTouch", function (previousState, nativeTouch) {
    var canvas = JSIL.Host.getCanvas();

    var mappedPosition = JSIL.Host.mapClientCoordinates(nativeTouch.clientX, nativeTouch.clientY);

    this.id = nativeTouch.identifier;
    this.x = mappedPosition[0];
    this.y = mappedPosition[1];
    // FIXME
    this.state = Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Pressed;

    var previousState = previousState.$getTouchById(this.id);
    if (previousState) {
      this.prevX = previousState.x;
      this.prevY = previousState.y;
      this.prevState = previousState.state;
    } else {
      this.prevX = 0;
      this.prevY = 0;
      this.prevState = null;
    }
  });

  $.Method({Static:false, Public:true , Virtual:true }, "Equals",
    new JSIL.MethodSignature($.Boolean, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], []),
    function Equals (other) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "Object.Equals",
    new JSIL.MethodSignature($.Boolean, [$.Object], []),
    function Object_Equals (obj) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "get_Id",
    new JSIL.MethodSignature($.Int32, [], []),
    function get_Id () {
      return this.id;
    }
  );

  $.Method({Static:false, Public:true }, "get_Position",
    new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Vector2"), [], []),
    function get_Position () {
      return new $xnaasms[0].Microsoft.Xna.Framework.Vector2(this.x, this.y);
    }
  );

  $.Method({Static:false, Public:true }, "get_State",
    new JSIL.MethodSignature($xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocationState"), [], []),
    function get_State () {
      return this.state;
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "GetHashCode",
    new JSIL.MethodSignature($.Int32, [], []),
    function GetHashCode () {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "op_Equality",
    new JSIL.MethodSignature($.Boolean, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation"), $xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], []),
    function op_Equality (value1, value2) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:true , Public:true }, "op_Inequality",
    new JSIL.MethodSignature($.Boolean, [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation"), $xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")], []),
    function op_Inequality (value1, value2) {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true , Virtual:true }, "toString",
    new JSIL.MethodSignature($.String, [], []),
    function toString () {
      throw new Error('Not implemented');
    }
  );

  $.Method({Static:false, Public:true }, "TryGetPreviousLocation",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("JSIL.Reference", [$xnaasms[4].TypeRef("Microsoft.Xna.Framework.Input.Touch.TouchLocation")])], []),
    function TryGetPreviousLocation (/* ref */ previousLocation) {
      if (!this.prevState)
        return false;

      var tTouchLocation = $xnaasms[4].Microsoft.Xna.Framework.Input.Touch.TouchLocation.__Type__;
      previousLocation.set(
        JSIL.CreateInstanceOfType(tTouchLocation, "$asPrevious", [this])
      );

      return true;
    }
  );

  ;
});
