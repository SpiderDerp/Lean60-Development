using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TouchControls : MonoBehaviour
{
    public static TouchControls Instance { get; private set; }

    public float MoveX => (_rightHeld ? 1f : 0f) + (_leftHeld ? -1f : 0f);

    bool _leftHeld;
    bool _rightHeld;
    bool _jumpPressed;
    bool _actionPressed;
    GameObject _padRoot;

    public static TouchControls Ensure()
    {
        if (Instance != null)
            return Instance;
        var go = new GameObject("TouchControls");
        return go.AddComponent<TouchControls>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildOverlay();
        SetPadVisible(Application.isMobilePlatform);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (WasGameplayKeyPressed())
            SetPadVisible(false);

        if (WasScreenTouched())
            SetPadVisible(true);
    }

    public bool ConsumeJump()
    {
        if (!_jumpPressed)
            return false;
        _jumpPressed = false;
        return true;
    }

    public bool ConsumeAction()
    {
        if (!_actionPressed)
            return false;
        _actionPressed = false;
        return true;
    }

    void SetPadVisible(bool visible)
    {
        if (_padRoot != null)
            _padRoot.SetActive(visible);
    }

    void BuildOverlay()
    {
        EnsureEventSystem();

        var canvasGo = new GameObject("TouchCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _padRoot = new GameObject("Pad");
        _padRoot.transform.SetParent(canvasGo.transform, false);
        var padRt = _padRoot.AddComponent<RectTransform>();
        padRt.anchorMin = Vector2.zero;
        padRt.anchorMax = Vector2.one;
        padRt.offsetMin = Vector2.zero;
        padRt.offsetMax = Vector2.zero;

        CreateHoldButton(_padRoot.transform, "A", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 36f), new Vector2(160f, 160f), () => _leftHeld = true, () => _leftHeld = false);
        CreateHoldButton(_padRoot.transform, "D", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 36f), new Vector2(160f, 160f), () => _rightHeld = true, () => _rightHeld = false);
        CreatePressButton(_padRoot.transform, "Jump", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-220f, 36f), new Vector2(160f, 160f), () => _jumpPressed = true);
        CreatePressButton(_padRoot.transform, "Action", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 36f), new Vector2(160f, 160f), () => _actionPressed = true);
    }

    void CreateHoldButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, System.Action down, System.Action up)
    {
        var go = CreateButtonVisual(parent, label, anchorMin, anchorMax, anchoredPos, size);
        var hold = go.AddComponent<TouchHoldButton>();
        hold.OnDown = down;
        hold.OnUp = up;
    }

    void CreatePressButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, System.Action down)
    {
        var go = CreateButtonVisual(parent, label, anchorMin, anchorMax, anchoredPos, size);
        var press = go.AddComponent<TouchHoldButton>();
        press.OnDown = down;
    }

    static GameObject CreateButtonVisual(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.28f);
        image.raycastTarget = true;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        text.fontSize = 42;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = label;
        return go;
    }

    static bool WasGameplayKeyPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;
        return keyboard.wKey.wasPressedThisFrame
            || keyboard.aKey.wasPressedThisFrame
            || keyboard.sKey.wasPressedThisFrame
            || keyboard.dKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame;
    }

    static bool WasScreenTouched()
    {
        var touch = Touchscreen.current;
        if (touch == null)
            return false;
        if (touch.primaryTouch.press.wasPressedThisFrame)
            return true;
        foreach (var finger in touch.touches)
        {
            if (finger.press.wasPressedThisFrame)
                return true;
        }
        return false;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}

public class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public System.Action OnDown;
    public System.Action OnUp;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnUp?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnUp?.Invoke();
    }
}
