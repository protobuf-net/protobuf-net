/* It is auto-generated file. Do not modify it. */
"use strict";

var test = window.test = Object.create(null);

test.logText = "";
test.exceptions = [];
test.game = null;


JSIL.DeclareNamespace("JSIL.TestFixture", false);


JSIL.TestFixture.RecordErrorService = function () {
};

JSIL.TestFixture.RecordErrorService.prototype.error = function (exception) {
  var exceptionMessage = "Unknown error";
  var exceptionTimestamp = JSIL.Host.getTime() - (window.$jsilbrowserstate.mainRunAtTime || 0);

  try {
    exceptionMessage = String(exc);
  } catch (_exc) {
  }

  test.exceptions.push([exceptionTimestamp, exceptionMessage]);
};


JSIL.TestFixture.LogService = function () {
};

JSIL.TestFixture.LogService.prototype.write = function (text) {
  test.logText += text;
};


JSIL.TestFixture.GameControlService = function () {
};

JSIL.TestFixture.GameControlService.prototype.started = function (game) {
  test.game = game;
};


JSIL.Host.registerServices({
  error: new JSIL.TestFixture.RecordErrorService(),
  stdout: new JSIL.TestFixture.LogService(),
  stderr: new JSIL.TestFixture.LogService(),
  gameControl: new JSIL.TestFixture.GameControlService()
});


test.pressKeysFor = function (keys, duration) {
  pressKeys(keys);

  window.setTimeout(function () {
    releaseKeys(keys);
  });
};