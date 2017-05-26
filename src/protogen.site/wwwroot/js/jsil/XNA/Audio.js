/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core required");

if (typeof ($jsilxna) === "undefined")
  throw new Error("JSIL.XNACore required");

JSIL.ImplementExternals("Microsoft.Xna.Framework.Audio.AudioEngine", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function _ctor (settingsFile) {
      this.categories = JSIL.CreateDictionaryObject(null);
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $.String, $xnaasms[5].TypeRef("System.TimeSpan"),
          $.String
        ], [])),
    function _ctor (settingsFile, lookAheadTime, rendererId) {
      this.categories = JSIL.CreateDictionaryObject(null);
    }
  );

  $.Method({Static:false, Public:true }, "Update",
    (JSIL.MethodSignature.Void),
    function Update () {
    }
  );

  $.Method({Static:false, Public:true }, "GetCategory",
    (new JSIL.MethodSignature($xnaasms[18].TypeRef("Microsoft.Xna.Framework.Audio.AudioCategory"), [$.String], [])),
    function GetCategory (name) {
      // FIXME
      return new Microsoft.Xna.Framework.Audio.AudioCategory(this, name);
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Audio.AudioCategory", function ($) {
  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[18].TypeRef("Microsoft.Xna.Framework.Audio.AudioEngine"), $.String], [])),
    function _ctor (engine, name) {
      this._parent = engine;
      this._name = name;
    }
  );

  $.Method({Static:false, Public:true }, "get_Name",
    (new JSIL.MethodSignature($.String, [], [])),
    function get_Name () {
      return this._name;
    }
  );

  $.Method({Static:false, Public:true }, "SetVolume",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function SetVolume (volume) {
      var categories = this._parent.categories;
      var category = categories[this._name];

      if (!category) {
        JSIL.Host.warning("No category named '" + this._name + "' exists in this audio engine.");
        return;
      }

      category.volumeMultiplier = volume;
      category.$gc();

      var wavesPlaying = category.wavesPlaying;
      for (var i = 0, l = wavesPlaying.length; i < l; i++)
        wavesPlaying[i].set_volumeMultiplier(volume);
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Audio.WaveBank", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[18].TypeRef("Microsoft.Xna.Framework.Audio.AudioEngine"), $.String], [])),
    function _ctor (audioEngine, nonStreamingWaveBankFilename) {
    }
  );

  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [
          $xnaasms[18].TypeRef("Microsoft.Xna.Framework.Audio.AudioEngine"), $.String,
          $.Int32, $.Int16
        ], [])),
    function _ctor (audioEngine, streamingWaveBankFilename, offset, packetsize) {
    }
  );

});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Audio.Cue", function ($) {
  $.RawMethod(false, "internalCtor", function (name, soundBank, sounds, audioEngine) {
    this._name = name;
    this.soundBank = soundBank;
    this.sounds = sounds;
    this.audioEngine = audioEngine;
    this.wavesPlaying = [];
  });

  $.Method({
    Static: false,
    Public: true
  }, "get_Name", new JSIL.MethodSignature($.String, [], []), function () {
    return this._name;
  });

  var warnedAboutVariables = JSIL.CreateDictionaryObject(null);

  $.Method({Static:false, Public:true }, "SetVariable",
    (new JSIL.MethodSignature(null, [$.String, $.Single], [])),
    function SetVariable (name, value) {
      // FIXME
      if (name === "Volume") {
        for (var i = 0; i < this.wavesPlaying.length; i++) {
          var wave = this.wavesPlaying[i];
          wave.set_volume(value);
        }
      } else {
        if (name in warnedAboutVariables)
          return;

        warnedAboutVariables[name] = true;

        JSIL.Host.warning("XACT audio variable '" + name + "' not supported.");
      }
    }
  );

  $.Method({Static:false, Public:true }, "Pause",
    (JSIL.MethodSignature.Void),
    function Pause () {
      this.$gc();

      for (var i = 0; i < this.wavesPlaying.length; i++) {
        var wave = this.wavesPlaying[i];
        wave.pause();
      }
    }
  );

  $.Method({Static:false, Public:true }, "Play",
    (JSIL.MethodSignature.Void),
    function Play () {
      this.$gc();

      var soundName = this.sounds[0];
      var sound = this.soundBank.sounds[soundName];
      var categoryName = sound.Category || null;
      var category = this.audioEngine.categories[categoryName] || null;

      for (var i = 0; i < sound.Tracks.length; i++) {
        var track = sound.Tracks[i];
        for (var j = 0; j < track.Events.length; j++) {
          var evt = track.Events[j];

          switch (evt.Type) {
          case "PlayWaveEvent":
            var waveName = evt.Wave;
            var wave = this.soundBank.waves[waveName];

            // Handle broken audio implementations
            if (wave !== null) {
              var instance = wave.$createInstance(evt.LoopCount > 0);

              if (category) {
                instance.set_volumeMultiplier(category.volumeMultiplier);
                category.$gc();
                category.wavesPlaying.push(instance);
              }

              instance.play();

              this.wavesPlaying.push(instance);
            }

            break;
          }
        }
      }
    }
  );

  $.RawMethod(false, "$gc", function () {
    for (var i = 0; i < this.wavesPlaying.length; i++) {
      var w = this.wavesPlaying[i];

      if (!w.get_isPlaying() && !w.get_isPaused()) {
        w.dispose();
        this.wavesPlaying.splice(i, 1);
        i--;
      }
    }
  });

  $.Method({Static:false, Public:true }, "get_IsPlaying",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsPlaying () {
      this.$gc();

      return (this.wavesPlaying.length > 0) && this.wavesPlaying[0].get_isPlaying();
    }
  );

  $.Method({Static:false, Public:true }, "get_IsPaused",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsPaused () {
      this.$gc();

      return (this.wavesPlaying.length > 0) && this.wavesPlaying[0].get_isPaused();
    }
  );

  $.Method({Static:false, Public:true }, "get_IsStopped",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsStopped () {
      this.$gc();

      return (this.wavesPlaying.length === 0);
    }
  );

  $.Method({Static:false, Public:true }, "Resume",
    (JSIL.MethodSignature.Void),
    function Resume () {
      this.$gc();

      for (var i = 0; i < this.wavesPlaying.length; i++) {
        var wave = this.wavesPlaying[i];
        wave.resume();
      }
    }
  );

  $.Method({Static:false, Public:true }, "Stop",
    (new JSIL.MethodSignature(null, [$xnaasms[18].TypeRef("Microsoft.Xna.Framework.Audio.AudioStopOptions")], [])),
    function Stop (options) {
      while (this.wavesPlaying.length > 0) {
        var wave = this.wavesPlaying.shift();
        wave.stop();
      }

      this.$gc();
    }
  );

  $.Method({Static:false, Public:true }, "Dispose",
    (JSIL.MethodSignature.Void),
    function Dispose () {
      this.Stop();
      this.$gc();
    }
  );
});

$jsilxna.SoundCategory = function (name) {
  this.name = name;
  this.sounds = [];
  this.wavesPlaying = [];
  this.volumeMultiplier = 1.0;
};

$jsilxna.SoundCategory.prototype.$gc = function () {
  for (var i = 0; i < this.wavesPlaying.length; i++) {
    var w = this.wavesPlaying[i];

    if (!w.get_isPlaying() && !w.get_isPaused()) {
      this.wavesPlaying.splice(i, 1);
      i--;
    }
  }
};

JSIL.ImplementExternals("Microsoft.Xna.Framework.Audio.SoundBank", function ($) {
  $.Method({Static:false, Public:true }, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[18].TypeRef("Microsoft.Xna.Framework.Audio.AudioEngine"), $.String], [])),
    function _ctor (audioEngine, filename) {
      var json = JSIL.Host.getAsset(filename, true);

      this.name = json.Name;
      this.audioEngine = audioEngine;

      this.cues = JSIL.CreateDictionaryObject(null);
      this.sounds = JSIL.CreateDictionaryObject(null);
      this.waves = JSIL.CreateDictionaryObject(null);

      var categories = audioEngine.categories;

      for (var i = 0, l = json.Cues.length; i < l; i++) {
        var cue = json.Cues[i];

        this.cues[cue.Name] = cue;
      }

      for (var i = 0, l = json.Sounds.length; i < l; i++) {
        var sound = json.Sounds[i];
        var categoryName = sound.Category || null;

        if (categoryName) {
          var category = categories[categoryName];
          if (!category)
            categories[categoryName] = category = new $jsilxna.SoundCategory(categoryName);

          category.sounds.push(sound);
        }

        this.sounds[sound.Name] = sound;
      }

      for (var name in json.Waves) {
        var filename = json.Waves[name];
        var waveAsset = JSIL.Host.getAsset(filename);

        this.waves[name] = waveAsset;
      }
    }
  );

  $.Method({Static:false, Public:true }, "GetCue",
    (new JSIL.MethodSignature($xnaasms[18].TypeRef("Microsoft.Xna.Framework.Audio.Cue"), [$.String], [])),
    function GetCue (name) {
      var cue = this.cues[name];
      var result = JSIL.CreateInstanceOfType(
        Microsoft.Xna.Framework.Audio.Cue.__Type__, "internalCtor", [cue.Name, this, cue.Sounds, this.audioEngine]
      );
      return result;
    }
  );

  $.Method({Static:false, Public:true }, "PlayCue",
    (new JSIL.MethodSignature(null, [$.String], [])),
    function PlayCue (name) {
      var cue = this.GetCue(name);
      cue.Play();
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Media.MediaPlayer", function ($) {
  $.RawMethod(true, ".cctor2", function () {
    Microsoft.Xna.Framework.Media.MediaPlayer.repeat = false;
    Microsoft.Xna.Framework.Media.MediaPlayer.currentSong = null;
  });

  $.Method({Static:true , Public:true }, "get_IsRepeating",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsRepeating () {
      return Microsoft.Xna.Framework.Media.MediaPlayer.repeat;
    }
  );

  var playImpl = function MediaPlayer_Play (song) {
    var oldInstance = Microsoft.Xna.Framework.Media.MediaPlayer.currentSong;
    var newInstance = null;

    if (song) {
      newInstance = song.$createInstance(Microsoft.Xna.Framework.Media.MediaPlayer.repeat);
    }

    if (oldInstance !== null)
      oldInstance.pause();

    if (newInstance !== null)
      newInstance.play();

    Microsoft.Xna.Framework.Media.MediaPlayer.currentSong = newInstance;
  };

  $.Method({Static:true , Public:true }, "Play",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Media.Song")], [])),
    playImpl
  );

  $.Method({Static:true , Public:true }, "Stop",
    (JSIL.MethodSignature.Void),
    function MediaPlayer_Stop () {
      if (Microsoft.Xna.Framework.Media.MediaPlayer.currentSong)
        Microsoft.Xna.Framework.Media.MediaPlayer.currentSong.pause();

      Microsoft.Xna.Framework.Media.MediaPlayer.currentSong = null;
    }
  );

  $.Method({Static:true , Public:true }, "set_IsRepeating",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_IsRepeating (value) {
      Microsoft.Xna.Framework.Media.MediaPlayer.repeat = value;
    }
  );
});

JSIL.ImplementExternals("Microsoft.Xna.Framework.Audio.SoundEffectInstance", function ($) {

  $.Method({Static:false, Public:false}, ".ctor",
    (new JSIL.MethodSignature(null, [$xnaasms[0].TypeRef("Microsoft.Xna.Framework.Audio.SoundEffect"), $.Boolean], [])),
    function _ctor (parentEffect, fireAndForget) {
      this.soundEffect = parentEffect;
      this.isFireAndForget = fireAndForget;
      this.isDisposed = false;
      this.looped = false;
      this.instance = null;
      this.volume = 1;
      this.pan = 0;
      this.pitch = 0;
    }
  );

  $.RawMethod(false, "$CreateInstanceIfNeeded", function () {
    if (this.instance === null)
      this.instance = this.soundEffect.$createInstance(this.looped);
    else
      this.instance.loop = this.looped;

    this.instance.set_volume(this.volume);
    this.instance.set_pan(this.pan);
    this.instance.set_pitch(this.pitch);
  });

  $.Method({Static:false, Public:true }, "Dispose",
    (JSIL.MethodSignature.Void),
    function Dispose () {
      this.Dispose(true);
    }
  );

  $.Method({Static:false, Public:false}, "Dispose",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function Dispose (disposing) {
      this.isDisposed = true;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsDisposed",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsDisposed () {
      return this.isDisposed;
    }
  );

  $.Method({Static:false, Public:false}, "get_IsFireAndForget",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsFireAndForget () {
      return this.isFireAndForget;
    }
  );

  $.Method({Static:false, Public:true }, "get_IsLooped",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsLooped () {
      return this.looped;
    }
  );

  $.Method({Static:false, Public:false}, "get_SoundEffect",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Audio.SoundEffect"), [], [])),
    function get_SoundEffect () {
      return this.soundEffect;
    }
  );

  $.Method({Static:false, Public:true }, "get_State",
    (new JSIL.MethodSignature($xnaasms[0].TypeRef("Microsoft.Xna.Framework.Audio.SoundState"), [], [])),
    function get_State () {
      if (!this.instance)
        return Microsoft.Xna.Framework.Audio.SoundState.Stopped;
      else if (this.instance.get_isPaused())
        return Microsoft.Xna.Framework.Audio.SoundState.Paused;
      else if (this.instance.get_isPlaying())
        return Microsoft.Xna.Framework.Audio.SoundState.Playing;
      else
        return Microsoft.Xna.Framework.Audio.SoundState.Stopped;
    }
  );

  $.Method({Static:false, Public:true }, "get_Volume",
    (new JSIL.MethodSignature($.Single, [], [])),
    function get_Volume () {
      return this.volume;
    }
  );

  $.Method({Static:false, Public:true }, "get_Pan",
    new JSIL.MethodSignature($.Single, [], []),
    function get_Pan () {
      return this.pan;
    }
  );

  $.Method({Static:false, Public:true }, "get_Pitch",
    new JSIL.MethodSignature($.Single, [], []),
    function get_Pitch () {
      return this.pitch;
    }
  );

  $.Method({Static:false, Public:true }, "Pause",
    (JSIL.MethodSignature.Void),
    function Pause () {
      if (this.instance !== null)
        this.instance.pause();
    }
  );

  $.Method({Static:false, Public:true }, "Play",
    (JSIL.MethodSignature.Void),
    function Play () {
      this.$CreateInstanceIfNeeded();
      this.instance.play();
    }
  );

  $.Method({Static:false, Public:true }, "Resume",
    (JSIL.MethodSignature.Void),
    function Resume () {
      if (this.instance !== null)
        this.instance.resume();
    }
  );

  $.Method({Static:false, Public:true }, "set_IsLooped",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function set_IsLooped (value) {
      if (this.looped === value)
        return;

      this.looped = value;

      if (this.instance !== null)
        this.instance.loop = this.looped;
    }
  );

  $.Method({Static:false, Public:true }, "set_Volume",
    (new JSIL.MethodSignature(null, [$.Single], [])),
    function set_Volume (value) {
      this.volume = value;

      if (this.instance !== null)
        this.instance.set_volume(value);
    }
  );

  $.Method({Static:false, Public:true }, "set_Pan",
    new JSIL.MethodSignature(null, [$.Single], []),
    function set_Pan (value) {
      this.pan = value;

      if (this.instance !== null)
        this.instance.set_pan(value);
    }
  );

  $.Method({Static:false, Public:true }, "set_Pitch",
    new JSIL.MethodSignature(null, [$.Single], []),
    function set_Pitch (value) {
      this.pitch = value;

      if (this.instance !== null)
        this.instance.set_pitch(value);
    }
  );

  $.Method({Static:false, Public:true }, "Stop",
    (JSIL.MethodSignature.Void),
    function Stop () {
      return this.Stop(true);
    }
  );

  $.Method({Static:false, Public:true }, "Stop",
    (new JSIL.MethodSignature(null, [$.Boolean], [])),
    function Stop (immediate) {
      if (this.instance !== null)
        this.instance.stop();

      this.instance = null;
    }
  );
});