(function () {
  var canvas = null;
  var visualizer = null;
  var audioContext = null;
  var raf = 0;
  var names = [];
  var presets = {};
  var index = 0;
  var timeBytes = null;
  var timeBytesL = null;
  var timeBytesR = null;
  var running = false;

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
      console.log("Butterchurn preset:", names[index]);
    }
  }

  function resize() {
    if (!canvas || !visualizer) {
      return;
    }
    var w = canvas.clientWidth || window.innerWidth;
    var h = canvas.clientHeight || window.innerHeight;
    canvas.width = w;
    canvas.height = h;
    if (visualizer.setRendererSize) {
      visualizer.setRendererSize(w, h);
    }
  }

  function tick() {
    if (!running || !visualizer) {
      return;
    }
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
        visualizer.render({
          audioLevels: {
            timeByteArray: timeBytes,
            timeByteArrayL: timeBytesL,
            timeByteArrayR: timeBytesR
          }
        });
      } else {
        visualizer.render();
      }
    } catch (err) {
      visualizer.render();
    }
    raf = requestAnimationFrame(tick);
  }

  window.Lean60Viz = {
    start: function () {
      canvas = document.getElementById("viz-canvas");
      var create = getCreateVisualizer();
      if (!canvas || !create) {
        console.warn("Butterchurn is not available");
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
        presets = getPresets() || {};
        names = Object.keys(presets);
        index = 0;
        visualizer = create(audioContext, canvas, {
          width: canvas.clientWidth || window.innerWidth,
          height: canvas.clientHeight || window.innerHeight,
          pixelRatio: window.devicePixelRatio || 1
        });
        loadCurrent(0);
        window.addEventListener("resize", resize);
      }

      canvas.style.display = "block";
      resize();
      if (!running) {
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
      var n = samples.length;
      if (!timeBytes || timeBytes.length !== n) {
        timeBytes = new Uint8Array(n);
        timeBytesL = new Uint8Array(n);
        timeBytesR = new Uint8Array(n);
      }
      for (var i = 0; i < n; i++) {
        var v = (samples[i] * 0.5 + 0.5) * 255;
        if (v < 0) v = 0;
        if (v > 255) v = 255;
        timeBytes[i] = v;
        timeBytesL[i] = v;
        timeBytesR[i] = v;
      }
    },

    stepPreset: function (delta) {
      if (names.length === 0) {
        return;
      }
      index = (index + delta) % names.length;
      if (index < 0) {
        index += names.length;
      }
      loadCurrent(1);
    },

    getPresetName: function () {
      return currentName();
    }
  };
})();
