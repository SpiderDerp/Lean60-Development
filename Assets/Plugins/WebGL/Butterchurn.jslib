mergeInto(LibraryManager.library, {
  Butterchurn_Start: function () {
    if (window.Lean60Viz && window.Lean60Viz.start) {
      window.Lean60Viz.start();
    }
  },

  Butterchurn_Stop: function () {
    if (window.Lean60Viz && window.Lean60Viz.stop) {
      window.Lean60Viz.stop();
    }
  },

  Butterchurn_SetWaveform: function (ptr, length) {
    if (!window.Lean60Viz || !window.Lean60Viz.setWaveform) {
      return;
    }
    var samples = new Float32Array(HEAPF32.buffer, ptr, length);
    window.Lean60Viz.setWaveform(samples);
  },

  Butterchurn_StepPreset: function (delta) {
    if (window.Lean60Viz && window.Lean60Viz.stepPreset) {
      window.Lean60Viz.stepPreset(delta);
    }
  },

  Butterchurn_GetPresetName: function (buffer, maxBytes) {
    var name = (window.Lean60Viz && window.Lean60Viz.getPresetName)
      ? window.Lean60Viz.getPresetName()
      : "";
    stringToUTF8(name, buffer, maxBytes);
  }
});
