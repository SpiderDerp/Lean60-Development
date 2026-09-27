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
    [SerializeField] Text bossHpText;

    void Awake()
    {
        EnsureHud();
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
        bossHpText = bossHp;
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
            if (bossHpText == null)
                bossHpText = existing.transform.Find("BossHpText")?.GetComponent<Text>();
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
        bossHpText = CreateUiText(canvasGo.transform, "BossHpText", new Vector2(1f, 1f), new Vector2(-20f, -30f), 22, TextAnchor.UpperRight);
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
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);

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
        gm.BindReferences(player, spawn, timerText, statusText);
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
            if (bossHpText != null)
                bosses[i].SetHpText(bossHpText);
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
