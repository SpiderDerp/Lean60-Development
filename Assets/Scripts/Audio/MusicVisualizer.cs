using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MusicVisualizer : MonoBehaviour
{
    [SerializeField] bool showPresetPicker = true;
    [SerializeField] int waveformSamples = 1024;

    float[] _waveform;
    Text _presetLabel;
    readonly byte[] _nameBuffer = new byte[256];

    public static MusicVisualizer Ensure()
    {
        var existing = FindFirstObjectByType<MusicVisualizer>();
        if (existing != null)
            return existing;
        var go = new GameObject("MusicVisualizer");
        return go.AddComponent<MusicVisualizer>();
    }

    void Awake()
    {
        _waveform = new float[Mathf.ClosestPowerOfTwo(Mathf.Max(64, waveformSamples))];
        if (showPresetPicker)
            BuildPicker();
    }

    void Start()
    {
        ButterchurnBridge.Start();
        RefreshPresetLabel();
    }

    void Update()
    {
        var source = MusicDirector.Instance != null ? MusicDirector.Instance.Source : null;
        if (source != null && source.isPlaying)
        {
            source.GetOutputData(_waveform, 0);
            ButterchurnBridge.SetWaveform(_waveform, _waveform.Length);
        }

        if (!showPresetPicker)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.leftBracketKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            StepPreset(-1);
        else if (keyboard.rightBracketKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            StepPreset(1);
    }

    void OnDestroy()
    {
        ButterchurnBridge.Stop();
    }

    void StepPreset(int delta)
    {
        ButterchurnBridge.StepPreset(delta);
        RefreshPresetLabel();
    }

    void RefreshPresetLabel()
    {
        string name = ButterchurnBridge.GetPresetName(_nameBuffer);
        Debug.Log("Butterchurn preset: " + name);
        if (_presetLabel != null)
            _presetLabel.text = name;
    }

    void BuildPicker()
    {
        var canvasGo = new GameObject("PresetPicker");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        CreateButton(canvasGo.transform, "Prev", new Vector2(0.38f, 0.06f), () => StepPreset(-1));
        CreateButton(canvasGo.transform, "Next", new Vector2(0.62f, 0.06f), () => StepPreset(1));

        var labelGo = new GameObject("PresetName");
        labelGo.transform.SetParent(canvasGo.transform, false);
        _presetLabel = labelGo.AddComponent<Text>();
        _presetLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_presetLabel.font == null)
            _presetLabel.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        _presetLabel.fontSize = 18;
        _presetLabel.alignment = TextAnchor.MiddleCenter;
        _presetLabel.color = Color.white;
        _presetLabel.text = "preset";
        var rt = _presetLabel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.06f);
        rt.anchorMax = new Vector2(0.5f, 0.06f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(520f, 36f);
    }

    static void CreateButton(Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction click)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.22f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(click);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(100f, 40f);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }
}

static class ButterchurnBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void Butterchurn_Start();

    [DllImport("__Internal")]
    static extern void Butterchurn_Stop();

    [DllImport("__Internal")]
    static extern void Butterchurn_SetWaveform(float[] samples, int length);

    [DllImport("__Internal")]
    static extern void Butterchurn_StepPreset(int delta);

    [DllImport("__Internal")]
    static extern void Butterchurn_GetPresetName(byte[] buffer, int maxBytes);

    public static void Start() => Butterchurn_Start();
    public static void Stop() => Butterchurn_Stop();
    public static void SetWaveform(float[] samples, int length) => Butterchurn_SetWaveform(samples, length);
    public static void StepPreset(int delta) => Butterchurn_StepPreset(delta);

    public static string GetPresetName(byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0)
            return string.Empty;
        Butterchurn_GetPresetName(buffer, buffer.Length);
        int count = 0;
        while (count < buffer.Length && buffer[count] != 0)
            count++;
        return Encoding.UTF8.GetString(buffer, 0, count);
    }
#else
    public static void Start() { }
    public static void Stop() { }
    public static void SetWaveform(float[] samples, int length) { }
    public static void StepPreset(int delta) { }
    public static string GetPresetName(byte[] buffer) => "(Butterchurn WebGL only)";
#endif
}
