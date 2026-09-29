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
    if (!window.Lean60Viz || !window.Lean60Viz.setAudio) {
      return;
    }
    var timeOff = timePtr >> 2;
    var specOff = specPtr >> 2;
    window.Lean60Viz.setAudio(
      HEAPF32.slice(timeOff, timeOff + timeLength),
      HEAPF32.slice(specOff, specOff + specLength)
    );
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
