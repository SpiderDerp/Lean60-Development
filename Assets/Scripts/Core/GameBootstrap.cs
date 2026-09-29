using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires HUD, camera, and projectile refs for a scene-authored level.
/// Does not spawn platforms or hazards — drag those from Assets/Prefabs.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Text timerText;
    [SerializeField] Text statusText;
    [SerializeField] Image timerRing;
    [SerializeField] Image bossHpFill;
    [SerializeField] GameObject bossHpBarRoot;

    static Sprite _ringSprite;
    static Sprite _whiteSprite;

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        EnsureHud();
        EnsureTimerRing();
        EnsureBossHpBar();
        MusicDirector.Ensure();
        EnsureNowPlaying();
        MusicVisualizer.Ensure();
        TouchControls.Ensure();
        EnsureCamera();
        BindGameManager();
        AssignProjectileRefs();
    }

    public void BindHud(Text timer, Text status, Text bossHp)
    {
        timerText = timer;
        statusText = status;
    }

    public void SetProjectilePrefab(Projectile prefab)
    {
        projectilePrefab = prefab;
    }

    void EnsureHud()
    {
        if (timerText != null && statusText != null)
            return;

        var existing = GameObject.Find("HUD");
        if (existing != null)
        {
            if (timerText == null)
                timerText = existing.transform.Find("TimerText")?.GetComponent<Text>();
            if (statusText == null)
                statusText = existing.transform.Find("StatusText")?.GetComponent<Text>();

            var oldHp = existing.transform.Find("BossHpText");
            if (oldHp != null)
                oldHp.gameObject.SetActive(false);
        }

        if (timerText != null)
            return;

        EnsureEventSystem();
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        timerText = CreateUiText(canvasGo.transform, "TimerText", new Vector2(0.5f, 1f), new Vector2(0f, -30f), 28, TextAnchor.UpperCenter);
        statusText = CreateUiText(canvasGo.transform, "StatusText", new Vector2(0.5f, 0.5f), Vector2.zero, 48, TextAnchor.MiddleCenter);
    }

    void EnsureBossHpBar()
    {
        var hud = GameObject.Find("HUD");
        if (hud == null && timerText != null)
            hud = timerText.canvas != null ? timerText.canvas.gameObject : null;
        if (hud == null)
            return;

        var oldHp = hud.transform.Find("BossHpText");
        if (oldHp != null)
            oldHp.gameObject.SetActive(false);

        var existing = hud.transform.Find("BossHpBar");
        if (existing != null)
        {
            bossHpBarRoot = existing.gameObject;
            var fillT = existing.Find("Fill");
            if (fillT != null)
                bossHpFill = fillT.GetComponent<Image>();
            bossHpBarRoot.SetActive(false);
            return;
        }

        bossHpBarRoot = new GameObject("BossHpBar");
        bossHpBarRoot.transform.SetParent(hud.transform, false);
        var rootRt = bossHpBarRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0f);
        rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0f, 28f);
        rootRt.sizeDelta = new Vector2(420f, 18f);

        var trackGo = new GameObject("Track");
        trackGo.transform.SetParent(bossHpBarRoot.transform, false);
        var track = trackGo.AddComponent<Image>();
        track.sprite = GetOrCreateWhiteSprite();
        track.color = new Color(0f, 0f, 0f, 0.5f);
        track.raycastTarget = false;
        var trackRt = track.rectTransform;
        trackRt.anchorMin = Vector2.zero;
        trackRt.anchorMax = Vector2.one;
        trackRt.offsetMin = Vector2.zero;
        trackRt.offsetMax = Vector2.zero;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bossHpBarRoot.transform, false);
        bossHpFill = fillGo.AddComponent<Image>();
        bossHpFill.sprite = GetOrCreateWhiteSprite();
        bossHpFill.type = Image.Type.Filled;
        bossHpFill.fillMethod = Image.FillMethod.Horizontal;
        bossHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        bossHpFill.fillAmount = 1f;
        bossHpFill.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        bossHpFill.raycastTarget = false;
        var fillRt = bossHpFill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        bossHpBarRoot.SetActive(false);
    }

    void EnsureTimerRing()
    {
        if (timerText == null)
            return;

        var parent = timerText.transform.parent;
        var existing = parent != null ? parent.Find("TimerRing") : null;
        if (existing != null)
            timerRing = existing.GetComponent<Image>();

        if (timerRing == null)
        {
            var go = new GameObject("TimerRing");
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(timerText.transform.GetSiblingIndex());
            timerRing = go.AddComponent<Image>();
            timerRing.sprite = GetOrCreateRingSprite();
            timerRing.type = Image.Type.Filled;
            timerRing.fillMethod = Image.FillMethod.Radial360;
            timerRing.fillOrigin = (int)Image.Origin360.Top;
            timerRing.fillClockwise = false;
            timerRing.fillAmount = 1f;
            timerRing.color = Color.white;
            timerRing.raycastTarget = false;

            var rt = timerRing.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f);
            rt.sizeDelta = new Vector2(72f, 72f);
        }

        var textRt = timerText.rectTransform;
        textRt.anchorMin = new Vector2(0.5f, 1f);
        textRt.anchorMax = new Vector2(0.5f, 1f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = new Vector2(0f, -52f);
        textRt.sizeDelta = new Vector2(72f, 40f);
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.fontSize = 22;
        timerText.raycastTarget = false;
    }

    static Sprite GetOrCreateRingSprite()
    {
        if (_ringSprite != null)
            return _ringSprite;

        const int size = 64;
        const float outer = 31f;
        const float inner = 24f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var clear = new Color(0f, 0f, 0f, 0f);
        var white = Color.white;
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d <= outer && d >= inner ? white : clear);
            }
        }

        tex.Apply(false, true);
        _ringSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _ringSprite;
    }

    static Sprite GetOrCreateWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return _whiteSprite;
    }

    void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographic = true;
        if (cam.orthographicSize < 6f)
            cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);

        if (cam.GetComponent<ScreenShake>() == null)
            cam.gameObject.AddComponent<ScreenShake>();

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow>();

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            follow.SetTarget(player.transform);
    }

    void EnsureNowPlaying()
    {
        if (NowPlayingHud.Instance != null)
            return;

        var hud = GameObject.Find("HUD");
        if (hud == null && timerText != null)
            hud = timerText.canvas != null ? timerText.canvas.gameObject : null;
        if (hud == null)
            return;

        NowPlayingHud.Ensure(hud.transform);
    }

    void BindGameManager()
    {
        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
            gm = new GameObject("GameManager").AddComponent<GameManager>();

        var player = FindFirstObjectByType<PlayerController>();
        var spawn = FindFirstObjectByType<SpawnPoint>();
        gm.BindReferences(player, spawn, timerText, statusText, timerRing);
    }

    void AssignProjectileRefs()
    {
        if (projectilePrefab == null)
            projectilePrefab = FindFirstObjectByType<Projectile>(FindObjectsInactive.Include);

        if (projectilePrefab == null)
            return;

        var gun = FindFirstObjectByType<PlayerGun>();
        if (gun != null)
            gun.SetPrefab(projectilePrefab);

        var spawners = FindObjectsByType<HazardProjectileSpawner>(FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
            spawners[i].SetPrefab(projectilePrefab);

        var bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        for (int i = 0; i < bosses.Length; i++)
        {
            bosses[i].SetPrefab(projectilePrefab);
            if (bossHpFill != null && bossHpBarRoot != null)
                bosses[i].SetHpBar(bossHpFill, bossHpBarRoot);
        }
    }

    static Text CreateUiText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = string.Empty;
        var rt = text.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(480f, 80f);
        return text;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
