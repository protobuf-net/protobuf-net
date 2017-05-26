/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

JSIL.Host.isBrowser = (typeof (window) !== "undefined") && (typeof (navigator) !== "undefined");


JSIL.Host.initCallbacks = [];

JSIL.Host.runInitCallbacks = function () {
  for (var i = 0; i < JSIL.Host.initCallbacks.length; i++) {
    var cb = JSIL.Host.initCallbacks[i];
    cb();
  }

  JSIL.Host.initCallbacks = null;
};


JSIL.Host.services = JSIL.CreateDictionaryObject(null);

JSIL.Host.getService = function (key, noThrow) {
  var svc = JSIL.Host.services[key];
  if (!svc) {
    if (noThrow)
      return null;
    else
      JSIL.RuntimeError("Service '" + key + "' not available");
  }

  return svc;
};

JSIL.Host.registerService = function (name, service) {
  JSIL.Host.services[name] = service;
};

JSIL.Host.registerServices = function (services) {
  for (var key in services) {
    if (!services.hasOwnProperty(key))
      continue;

    JSIL.Host.registerService(key, services[key]);
  }
};


// Access services using these methods instead of getService directly.

// Returns UTC time
JSIL.Host.getTime = function () {
  var svc = JSIL.Host.getService("time");
  return svc.getUTC();
};

JSIL.Host.getTimezoneOffsetInMilliseconds = function () {
  var svc = JSIL.Host.getService("time");
  return svc.getTimezoneOffsetInMilliseconds();
};

// Returns elapsed ticks since some starting point (usually page load), high precision.
JSIL.Host.getTickCount = function () {
  var svc = JSIL.Host.getService("time");
  return svc.getTickCount();
};

// Entry point used by storage APIs to get UTC time.
JSIL.Host.getFileTime = function () {
  // FIXME
  return Date.now();
};

JSIL.Host.getCanvas = function (desiredWidth, desiredHeight) {
  var svc = JSIL.Host.getService("canvas");

  if (
    (typeof (desiredWidth) !== "undefined") &&
    (typeof (desiredHeight) !== "undefined")
  )
    return svc.get(desiredWidth, desiredHeight);
  else
    return svc.get();
};

JSIL.Host.createCanvas = function (desiredWidth, desiredHeight) {
  var svc = JSIL.Host.getService("canvas");
  return svc.create(desiredWidth, desiredHeight);
};

JSIL.Host.getHeldKeys = function () {
  var svc = JSIL.Host.getService("keyboard", true);
  if (!svc)
    return [];

  return svc.getHeldKeys();
};

JSIL.Host.getMousePosition = function () {
  var svc = JSIL.Host.getService("mouse", true);
  if (!svc)
    return [0, 0];

  return svc.getPosition();
};

JSIL.Host.getHeldMouseButtons = function () {
  var svc = JSIL.Host.getService("mouse", true);
  if (!svc)
    return [];

  return svc.getHeldButtons();
};

JSIL.Host.isPageVisible = function () {
  var svc = JSIL.Host.getService("pageVisibility", true);
  if (!svc)
    return true;

  return svc.get();
};

JSIL.Host.runLater = function (action) {
  var svc = JSIL.Host.getService("runLater", true);
  if (!svc)
    return false;

  svc.enqueue(action);
  return true;
};

JSIL.Host.runLaterFlush = function () {
  var svc = JSIL.Host.getService("runLater", true);
  if (!svc)
    return false;

  if (svc.flush) {
    svc.flush();
    return true;
  }

  return false;
};

JSIL.Host.logWrite = function (text) {
  var svc = JSIL.Host.getService("stdout");
  svc.write(text);
};

JSIL.Host.logWriteLine = function (text) {
  var svc = JSIL.Host.getService("stdout");
  svc.write(text + "\n");
};

JSIL.Host.warning = function (text) {
  var svc = JSIL.Host.getService("stderr");
  var stack = Error().stack;
  if (stack)
    svc.write(text + "\n" + stack.slice(stack.indexOf("\n", 6)+1, -1));
  else
    svc.write(text + "\n");
}

JSIL.Host.abort = function (exception, extraInfo) {
  var svc = JSIL.Host.getService("stderr");
  if (extraInfo)
    svc.write(extraInfo);

  try {
    svc.write(exception);
  } catch (exc) {
  }

  try {
    if (exception.stack)
      svc.write(exception.stack);
  } catch (exc) {
  }

  var svc = JSIL.Host.getService("error");
  svc.error(exception);
};

JSIL.Host.assertionFailed = function (message) {
  var svc = JSIL.Host.getService("error");
  svc.error(new Error(message || "Assertion Failed"));
};

JSIL.Host.scheduleTick = function (tickCallback) {
  var svc = JSIL.Host.getService("tickScheduler");
  svc.schedule(tickCallback);
};

JSIL.Host.reportPerformance = function (drawDuration, updateDuration, cacheSize, isWebGL) {
  var svc = JSIL.Host.getService("performanceReporter", true);
  if (!svc)
    return;

  svc.report(drawDuration, updateDuration, cacheSize, isWebGL);
};

// Default service implementations that are environment-agnostic

// Don't use for anything that needs to be reproducible in replays!

(function () {
  if (
    (typeof (window) !== "undefined") &&
    (typeof (window.performance) !== "undefined") &&
    (typeof (window.performance.now) === "function")
  )
    JSIL.$GetHighResTime = window.performance.now.bind(window.performance);
  else
    JSIL.$GetHighResTime = Date.now.bind(Date);
})();

JSIL.Host.ES5TimeService = function () {
  this.started = JSIL.$GetHighResTime();
};

JSIL.Host.ES5TimeService.prototype.getTickCount = function () {
  var result = JSIL.$GetHighResTime() - this.started;

  // Round it to a few digits past the decimal.
  result *= 100;
  result = Math.round(result);
  result /= 100;

  return result;
};

JSIL.Host.ES5TimeService.prototype.getUTC = function () {
  return Date.now();
};

JSIL.Host.ES5TimeService.prototype.getTimezoneOffsetInMilliseconds = function () {
  return new Date().getTimezoneOffset() * 60000;
};

JSIL.Host.ThrowErrorService = function () {
};

JSIL.Host.ThrowErrorService.prototype.error = function (exception) {
  throw exception;
};


JSIL.Host.registerServices({
  time: new JSIL.Host.ES5TimeService(),
  error: new JSIL.Host.ThrowErrorService()
});