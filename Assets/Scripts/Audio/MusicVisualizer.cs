using System.Runtime.InteropServices;
using UnityEngine;

public class MusicVisualizer : MonoBehaviour
{
    [SerializeField] int waveformSamples = 512;
    [SerializeField] int blitInterval = 1;

    static readonly QualityStep[] Steps =
    {
        new QualityStep(1920, 1080, 0f, 1),
        new QualityStep(1280, 720, 0f, 1),
        new QualityStep(640, 360, 0f, 2),
        new QualityStep(640, 360, 33f, 3)
    };

    struct QualityStep
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float FrameMs;
        public readonly int BlitInterval;

        public QualityStep(int width, int height, float frameMs, int blitInterval)
        {
            Width = width;
            Height = height;
            FrameMs = frameMs;
            BlitInterval = blitInterval;
        }
    }

    float[] _waveform;
    float[] _spectrum;
    Texture2D _backdropTex;
    Texture2D _pendingTex;
    SpriteRenderer _backdrop;
    Transform _backdropTf;
    Camera _cam;
    int _blitTick;
    int _qualityStep;
    int _slowFrames;
    int _fastFrames;
    float _warmup = 1f;
    bool _vizStarted;

    public static MusicVisualizer Instance { get; private set; }

    public static MusicVisualizer Ensure()
    {
        if (Instance != null)
            return Instance;
        var existing = FindFirstObjectByType<MusicVisualizer>();
        if (existing != null)
            return existing;
        var go = new GameObject("MusicVisualizer");
        return go.AddComponent<MusicVisualizer>();
    }

    public static void RandomizePreset()
    {
        if (Instance != null)
            ButterchurnBridge.RandomAllowedPreset();
    }

    void Awake()
    {
        Instance = this;
        int n = Mathf.ClosestPowerOfTwo(Mathf.Max(64, waveformSamples));
        _waveform = new float[n];
        _spectrum = new float[n];
    }

    void Start()
    {
        ButterchurnBridge.EnsureLoaded();
        TryStartViz();
    }

    void LateUpdate()
    {
        TryStartViz();
        EnsureBackdrop();
        FitBackdrop();
        _blitTick++;
        if (_blitTick < blitInterval)
            return;
        _blitTick = 0;
        BlitBackdrop();
    }

    void Update()
    {
        var source = MusicDirector.Instance != null ? MusicDirector.Instance.Source : null;
        if (source != null && source.isPlaying)
        {
            source.GetOutputData(_waveform, 0);
            source.GetSpectrumData(_spectrum, 0, FFTWindow.Rectangular);
            ButterchurnBridge.SetAudio(_waveform, _waveform.Length, _spectrum, _spectrum.Length);
        }

        TuneQuality();
    }

    void TryStartViz()
    {
        if (_vizStarted)
            return;
        if (!ButterchurnBridge.IsReady())
            return;
        _vizStarted = true;
        ButterchurnBridge.Start();
        ApplyQuality();
        EnsureBackdrop();
    }

    void TuneQuality()
    {
        if (_warmup > 0f)
        {
            _warmup -= Time.unscaledDeltaTime;
            return;
        }

        float dt = Time.unscaledDeltaTime;
        if (dt > 0.022f)
        {
            _slowFrames++;
            _fastFrames = 0;
            if (_slowFrames >= 8)
                StepQuality(1);
        }
        else if (dt < 0.018f)
        {
            _fastFrames++;
            _slowFrames = 0;
            if (_fastFrames >= 30)
                StepQuality(-1);
        }
        else
        {
            _slowFrames = 0;
            _fastFrames = 0;
        }
    }

    void StepQuality(int delta)
    {
        int next = Mathf.Clamp(_qualityStep + delta, 0, Steps.Length - 1);
        _slowFrames = 0;
        _fastFrames = 0;
        if (next == _qualityStep)
            return;
        _qualityStep = next;
        ApplyQuality();
    }

    void ApplyQuality()
    {
        var step = Steps[_qualityStep];
        blitInterval = step.BlitInterval;
        _blitTick = 0;
        ButterchurnBridge.SetQuality(step.Width, step.Height, step.FrameMs);
    }

    void OnDestroy()
    {
        ButterchurnBridge.Stop();
        DiscardPending();
        if (_backdropTex != null)
            Destroy(_backdropTex);
        if (Instance == this)
            Instance = null;
    }

    Camera Cam
    {
        get
        {
            if (_cam == null)
                _cam = Camera.main;
            return _cam;
        }
    }

    void EnsureBackdrop()
    {
        var cam = Cam;
        if (cam == null)
            return;

        if (_backdrop == null)
        {
            var go = new GameObject("VizBackdrop");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 15f);
            _backdropTf = go.transform;
            _backdrop = go.AddComponent<SpriteRenderer>();
            _backdrop.sortingOrder = -32000;
            _backdrop.color = Color.white;
        }
        else if (_backdropTf.parent != cam.transform)
        {
            _backdropTf.SetParent(cam.transform, false);
            _backdropTf.localPosition = new Vector3(0f, 0f, 15f);
        }

        int w = ButterchurnBridge.GetCanvasWidth();
        int h = ButterchurnBridge.GetCanvasHeight();
        if (w < 2 || h < 2)
            return;

        if (_backdropTex != null && _backdropTex.width == w && _backdropTex.height == h)
        {
            DiscardPending();
            return;
        }

        if (_pendingTex != null && (_pendingTex.width != w || _pendingTex.height != h))
            DiscardPending();

        if (_pendingTex == null)
        {
            _pendingTex = new Texture2D(w, h, TextureFormat.RGBA32, false, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _pendingTex.Apply(false, false);
        }

        if (!CanBlitTo(_pendingTex))
            return;

        ButterchurnBridge.BlitToTexture(_pendingTex);
        SwapPendingToBackdrop();
    }

    void FitBackdrop()
    {
        if (_backdropTf == null)
            return;
        var cam = Cam;
        if (cam == null || !cam.orthographic || _backdropTex == null)
            return;

        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;
        _backdropTf.localRotation = Quaternion.identity;
        _backdropTf.localScale = new Vector3(worldW / _backdropTex.width, worldH / _backdropTex.height, 1f);
    }

    void BlitBackdrop()
    {
        if (!CanBlitTo(_backdropTex))
            return;
        ButterchurnBridge.BlitToTexture(_backdropTex);
    }

    static bool CanBlitTo(Texture2D tex)
    {
        if (tex == null)
            return false;
        int w = ButterchurnBridge.GetCanvasWidth();
        int h = ButterchurnBridge.GetCanvasHeight();
        return w == tex.width && h == tex.height && w >= 2 && h >= 2;
    }

    void SwapPendingToBackdrop()
    {
        if (_pendingTex == null || _backdrop == null)
            return;

        var oldTex = _backdropTex;
        var oldSprite = _backdrop.sprite;
        _backdropTex = _pendingTex;
        _pendingTex = null;
        _backdrop.sprite = Sprite.Create(_backdropTex, new Rect(0f, 0f, _backdropTex.width, _backdropTex.height), new Vector2(0.5f, 0.5f), 1f);
        if (oldSprite != null)
            Destroy(oldSprite);
        if (oldTex != null)
            Destroy(oldTex);
        FitBackdrop();
    }

    void DiscardPending()
    {
        if (_pendingTex == null)
            return;
        Destroy(_pendingTex);
        _pendingTex = null;
    }
}

static class ButterchurnBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void Butterchurn_EnsureLoaded();

    [DllImport("__Internal")]
    static extern int Butterchurn_IsReady();

    [DllImport("__Internal")]
    static extern void Butterchurn_Start();

    [DllImport("__Internal")]
    static extern void Butterchurn_Stop();

    [DllImport("__Internal")]
    static extern void Butterchurn_SetWaveform(float[] samples, int length);

    [DllImport("__Internal")]
    static extern void Butterchurn_SetAudio(float[] time, int timeLength, float[] spectrum, int spectrumLength);

    [DllImport("__Internal")]
    static extern void Butterchurn_SetQuality(int width, int height, float frameMs);

    [DllImport("__Internal")]
    static extern void Butterchurn_RandomAllowedPreset();

    [DllImport("__Internal")]
    static extern int Butterchurn_GetCanvasWidth();

    [DllImport("__Internal")]
    static extern int Butterchurn_GetCanvasHeight();

    [DllImport("__Internal")]
    static extern void Butterchurn_BlitToTexture(int texId);

    public static void EnsureLoaded() => Butterchurn_EnsureLoaded();
    public static bool IsReady() => Butterchurn_IsReady() != 0;
    public static void Start() => Butterchurn_Start();
    public static void Stop() => Butterchurn_Stop();
    public static void SetWaveform(float[] samples, int length) => Butterchurn_SetWaveform(samples, length);
    public static void SetAudio(float[] time, int timeLength, float[] spectrum, int spectrumLength) =>
        Butterchurn_SetAudio(time, timeLength, spectrum, spectrumLength);
    public static void SetQuality(int width, int height, float frameMs) =>
        Butterchurn_SetQuality(width, height, frameMs);
    public static void RandomAllowedPreset() => Butterchurn_RandomAllowedPreset();
    public static int GetCanvasWidth() => Butterchurn_GetCanvasWidth();
    public static int GetCanvasHeight() => Butterchurn_GetCanvasHeight();
    public static void BlitToTexture(Texture tex)
    {
        if (tex == null)
            return;
        Butterchurn_BlitToTexture((int)tex.GetNativeTexturePtr());
    }
#else
    public static void EnsureLoaded() { }
    public static bool IsReady() => true;
    public static void Start() { }
    public static void Stop() { }
    public static void SetWaveform(float[] samples, int length) { }
    public static void SetAudio(float[] time, int timeLength, float[] spectrum, int spectrumLength) { }
    public static void SetQuality(int width, int height, float frameMs) { }
    public static void RandomAllowedPreset() { }
    public static int GetCanvasWidth() => 0;
    public static int GetCanvasHeight() => 0;
    public static void BlitToTexture(Texture tex) { }
#endif
}
