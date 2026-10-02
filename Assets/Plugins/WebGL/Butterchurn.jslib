mergeInto(LibraryManager.library, {
  Butterchurn_EnsureLoaded: function () {
    if (window.Lean60Viz && window.Lean60Viz.load) {
      window.Lean60Viz.load();
    }
  },

  Butterchurn_IsReady: function () {
    return window.Lean60Viz && window.Lean60Viz.start ? 1 : 0;
  },

  Butterchurn_Start: function () {
    if (window.Lean60Viz && window.Lean60Viz.load) {
      window.Lean60Viz.load(function () {
        if (window.Lean60Viz && window.Lean60Viz.start) {
          window.Lean60Viz.start();
        }
      });
      return;
    }
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
    var offset = ptr >> 2;
    window.Lean60Viz.setWaveform(HEAPF32.slice(offset, offset + length));
  },

  Butterchurn_SetAudio: function (timePtr, timeLength, specPtr, specLength) {
    var viz = window.Lean60Viz;
    if (!viz || !viz.setAudio) {
      return;
    }
    if (!viz._timeIn || viz._timeIn.length !== timeLength) {
      viz._timeIn = new Float32Array(timeLength);
    }
    if (!viz._specIn || viz._specIn.length !== specLength) {
      viz._specIn = new Float32Array(specLength);
    }
    viz._timeIn.set(HEAPF32.subarray(timePtr >> 2, (timePtr >> 2) + timeLength));
    viz._specIn.set(HEAPF32.subarray(specPtr >> 2, (specPtr >> 2) + specLength));
    viz.setAudio(viz._timeIn, viz._specIn);
  },

  Butterchurn_SetQuality: function (width, height, frameMs) {
    if (window.Lean60Viz && window.Lean60Viz.setQuality) {
      window.Lean60Viz.setQuality(width, height, frameMs);
    }
  },

  Butterchurn_RandomAllowedPreset: function () {
    if (window.Lean60Viz && window.Lean60Viz.randomAllowedPreset) {
      window.Lean60Viz.randomAllowedPreset();
    }
  },

  Butterchurn_GetCanvasWidth: function () {
    return window.Lean60Viz && window.Lean60Viz.getCanvasWidth
      ? window.Lean60Viz.getCanvasWidth()
      : 0;
  },

  Butterchurn_GetCanvasHeight: function () {
    return window.Lean60Viz && window.Lean60Viz.getCanvasHeight
      ? window.Lean60Viz.getCanvasHeight()
      : 0;
  },

  Butterchurn_BlitToTexture: function (texId) {
    var src = document.getElementById("viz-canvas");
    if (!src || !src.width || !src.height || !GL || !GL.textures) {
      return;
    }
    var tex = GL.textures[texId];
    if (!tex) {
      return;
    }
    GLctx.bindTexture(GLctx.TEXTURE_2D, tex);
    GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, true);
    GLctx.texSubImage2D(GLctx.TEXTURE_2D, 0, 0, 0, GLctx.RGBA, GLctx.UNSIGNED_BYTE, src);
    GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, false);
  }
});
