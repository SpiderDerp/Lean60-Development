(function () {
  var canvas = null;
  var visualizer = null;
  var audioContext = null;
  var raf = 0;
  var allowedNames = [
    "Aderrasi - Songflower (Moss Posy)",
    "Eo.S. + Zylot - skylight (Stained Glass Majesty mix)",
    "Eo.S. - glowsticks v2 05 and proton lights (+Krash\u2032s beat code) _Phat_remix02b",
    "Flexi + amandio c - piercing 05 - Kopie (2) - Kopie",
    "Flexi + stahlregen - jelly showoff parade",
    "Flexi - mindblob mix",
    "flexi - mom, why the sky looks different today",
    "flexi - patternton, district of media, capitol of the united abstractions of fractopia",
    "flexi - what is the matrix"
  ];
  var names = [];
  var presets = {};
  var index = 0;
  var shuffleOrder = [];
  var shufflePos = 0;
  var timeBytes = null;
  var timeBytesL = null;
  var timeBytesR = null;
  var freqBytes = null;
  var freqFloats = null;
  var running = false;
  var lastRender = 0;
  var MAX_W = 1920;
  var MAX_H = 1080;
  var FRAME_MS = 0;

  function getCreateVisualizer() {
    var lib = window.butterchurn || window.Butterchurn;
    if (!lib) {
      return null;
    }
    if (typeof lib.createVisualizer === "function") {
      return lib.createVisualizer.bind(lib);
    }
    if (lib.default && typeof lib.default.createVisualizer === "function") {
      return lib.default.createVisualizer.bind(lib.default);
    }
    return null;
  }

  function getPresets() {
    var pack = window.butterchurnPresets || window.ButterchurnPresets;
    if (!pack) {
      return {};
    }
    if (typeof pack.getPresets === "function") {
      return pack.getPresets();
    }
    if (pack.default && typeof pack.default.getPresets === "function") {
      return pack.default.getPresets();
    }
    return pack;
  }

  function currentName() {
    return names[index] || "";
  }

  function loadCurrent(blend) {
    if (!visualizer || names.length === 0) {
      return;
    }
    var preset = presets[names[index]];
    if (preset) {
      visualizer.loadPreset(preset, blend);
    }
  }

  function toTimeByte(sample) {
    var v = (sample * 0.5 + 0.5) * 255;
    if (v < 0) return 0;
    if (v > 255) return 255;
    return v;
  }

  function toFreqByte(sample) {
    var v = sample * 255;
    if (v < 0) return 0;
    if (v > 255) return 255;
    return v;
  }

  function stretchToBytes(dest, src, convert) {
    var n = dest.length;
    var m = src.length;
    if (n === 0 || m === 0) {
      return;
    }
    if (m === n) {
      for (var i = 0; i < n; i++) {
        dest[i] = convert(src[i]);
      }
      return;
    }
    for (var j = 0; j < n; j++) {
      var t = m === 1 ? 0 : (j * (m - 1)) / (n - 1);
      var i0 = t | 0;
      var i1 = i0 + 1 < m ? i0 + 1 : i0;
      var f = t - i0;
      dest[j] = convert(src[i0] * (1 - f) + src[i1] * f);
    }
  }

  function stretchToFloat(dest, src) {
    var n = dest.length;
    var m = src.length;
    if (n === 0 || m === 0) {
      return;
    }
    for (var i = 0; i < n; i++) {
      var t = m === 1 ? 0 : (i * (m - 1)) / (n - 1);
      var i0 = t | 0;
      var i1 = i0 + 1 < m ? i0 + 1 : i0;
      var f = t - i0;
      var v = src[i0] * (1 - f) + src[i1] * f;
      dest[i] = v < 0 ? 0 : v;
    }
  }

  function hookAudio() {
    if (!visualizer || !visualizer.audio || visualizer.audio._lean60Hooked) {
      return;
    }
    var audio = visualizer.audio;
    if (typeof audio.updateAudio !== "function") {
      return;
    }
    var original = audio.updateAudio.bind(audio);
    audio.updateAudio = function (time, left, right) {
      original(time, left, right);
      if (freqFloats && audio.freqArray && audio.freqArray.length) {
        stretchToFloat(audio.freqArray, freqFloats);
      }
    };
    audio._lean60Hooked = true;
  }

  function rebuildShuffle(exclude) {
    shuffleOrder = [];
    for (var i = 0; i < names.length; i++) {
      if (exclude < 0 || i !== exclude) {
        shuffleOrder.push(i);
      }
    }
    for (var j = shuffleOrder.length - 1; j > 0; j--) {
      var k = Math.floor(Math.random() * (j + 1));
      var tmp = shuffleOrder[j];
      shuffleOrder[j] = shuffleOrder[k];
      shuffleOrder[k] = tmp;
    }
    // After a full cycle, allow the previous preset again on the next bag.
    if (exclude >= 0 && names.length > 1 && shuffleOrder.length === 0) {
      shuffleOrder.push(exclude);
    }
    shufflePos = 0;
  }

  function pickRandomIndex(exclude) {
    if (names.length <= 1) {
      return 0;
    }
    if (shufflePos >= shuffleOrder.length) {
      rebuildShuffle(exclude);
    }
    if (shuffleOrder.length === 0) {
      rebuildShuffle(-1);
    }
    var next = shuffleOrder[shufflePos++];
    return next;
  }

  function sizeCanvas() {
    var cssW = (canvas && canvas.clientWidth) || window.innerWidth;
    var cssH = (canvas && canvas.clientHeight) || window.innerHeight;
    if (cssW < 2) cssW = window.innerWidth;
    if (cssH < 2) cssH = window.innerHeight;
    var scale = Math.min(1, MAX_W / cssW, MAX_H / cssH);
    var w = Math.max(2, Math.round(cssW * scale));
    var h = Math.max(2, Math.round(cssH * scale));
    if (canvas) {
      canvas.width = w;
      canvas.height = h;
    }
    return { w: w, h: h };
  }

  function resize() {
    var size = sizeCanvas();
    if (visualizer && visualizer.setRendererSize) {
      visualizer.setRendererSize(size.w, size.h);
    }
  }

  function tick(now) {
    if (!running || !visualizer) {
      return;
    }
    raf = requestAnimationFrame(tick);
    if (now - lastRender < FRAME_MS) {
      return;
    }
    lastRender = now;
    try {
      if (timeBytes) {
        if (visualizer.audio) {
          if (visualizer.audio.timeByteArray && visualizer.audio.timeByteArray.set) {
            visualizer.audio.timeByteArray.set(timeBytes.subarray(0, visualizer.audio.timeByteArray.length));
          }
          if (visualizer.audio.timeByteArrayL && visualizer.audio.timeByteArrayL.set) {
            visualizer.audio.timeByteArrayL.set(timeBytesL.subarray(0, visualizer.audio.timeByteArrayL.length));
          }
          if (visualizer.audio.timeByteArrayR && visualizer.audio.timeByteArrayR.set) {
            visualizer.audio.timeByteArrayR.set(timeBytesR.subarray(0, visualizer.audio.timeByteArrayR.length));
          }
        }
        if (freqBytes && visualizer.audio && visualizer.audio.freqByteArray && visualizer.audio.freqByteArray.set) {
          visualizer.audio.freqByteArray.set(freqBytes.subarray(0, visualizer.audio.freqByteArray.length));
        }
        visualizer.render({
          audioLevels: {
            timeByteArray: timeBytes,
            timeByteArrayL: timeBytesL,
            timeByteArrayR: timeBytesR,
            freqByteArray: freqBytes
          }
        });
      } else {
        visualizer.render();
      }
    } catch (err) {
      console.error("Butterchurn render failed", err);
    }
  }

  window.Lean60Viz = {
    start: function () {
      canvas = document.getElementById("viz-canvas");
      if (!canvas) {
        console.error("Butterchurn: #viz-canvas is missing");
        return;
      }

      canvas.style.display = "block";
      canvas.style.visibility = "hidden";
      canvas.style.opacity = "0";
      canvas.style.pointerEvents = "none";
      canvas.style.background = "#05030a";
      var size = sizeCanvas();

      var create = getCreateVisualizer();
      if (!create) {
        console.error("Butterchurn is not available (createVisualizer missing)");
        return;
      }

      if (!audioContext) {
        var Ctx = window.AudioContext || window.webkitAudioContext;
        audioContext = new Ctx();
      }
      if (audioContext.resume) {
        audioContext.resume();
      }

      if (!visualizer) {
        var all = getPresets() || {};
        presets = {};
        names = [];
        for (var i = 0; i < allowedNames.length; i++) {
          var key = allowedNames[i];
          if (all[key]) {
            presets[key] = all[key];
            names.push(key);
          }
        }
        index = names.length > 0 ? Math.floor(Math.random() * names.length) : 0;
        rebuildShuffle(index);
        try {
          visualizer = create(audioContext, canvas, {
            width: size.w,
            height: size.h,
            pixelRatio: 1
          });
          hookAudio();
          loadCurrent(0);
          window.addEventListener("resize", resize);
        } catch (err) {
          console.error("Butterchurn createVisualizer failed", err);
          visualizer = null;
          return;
        }
      }

      hookAudio();
      resize();
      if (!running && visualizer) {
        running = true;
        raf = requestAnimationFrame(tick);
      }
    },

    stop: function () {
      running = false;
      if (raf) {
        cancelAnimationFrame(raf);
        raf = 0;
      }
      if (canvas) {
        canvas.style.display = "none";
      }
    },

    setWaveform: function (samples) {
      window.Lean60Viz.setAudio(samples, null);
    },

    setAudio: function (timeSamples, specSamples) {
      var timeLen = 1024;
      var freqLen = 512;
      if (visualizer && visualizer.audio) {
        if (visualizer.audio.timeByteArray && visualizer.audio.timeByteArray.length) {
          timeLen = visualizer.audio.timeByteArray.length;
        }
        if (visualizer.audio.freqArray && visualizer.audio.freqArray.length) {
          freqLen = visualizer.audio.freqArray.length;
        } else if (visualizer.audio.numSamps) {
          freqLen = visualizer.audio.numSamps;
        }
      }

      if (!timeBytes || timeBytes.length !== timeLen) {
        timeBytes = new Uint8Array(timeLen);
        timeBytesL = new Uint8Array(timeLen);
        timeBytesR = new Uint8Array(timeLen);
      }
      if (!freqBytes || freqBytes.length !== freqLen) {
        freqBytes = new Uint8Array(freqLen);
      }

      if (timeSamples && timeSamples.length) {
        stretchToBytes(timeBytes, timeSamples, toTimeByte);
        timeBytesL.set(timeBytes);
        timeBytesR.set(timeBytes);
      }

      if (specSamples && specSamples.length) {
        freqFloats = specSamples;
        stretchToBytes(freqBytes, specSamples, toFreqByte);
      }
    },

    randomAllowedPreset: function () {
      if (names.length === 0) {
        return;
      }
      index = pickRandomIndex(index);
      loadCurrent(1);
    },

    setQuality: function (width, height, frameMs) {
      var w = Math.max(2, width | 0);
      var h = Math.max(2, height | 0);
      if (MAX_W === w && MAX_H === h && FRAME_MS === frameMs) {
        return;
      }
      MAX_W = w;
      MAX_H = h;
      FRAME_MS = frameMs > 0 ? frameMs : 0;
      lastRender = 0;
      resize();
    },

    getCanvasWidth: function () {
      return canvas && canvas.width ? canvas.width : 0;
    },

    getCanvasHeight: function () {
      return canvas && canvas.height ? canvas.height : 0;
    }
  };
})();
