using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a playable Lean60 test bed at runtime with placeholder colored blocks.
/// Attach to any object in SampleScene (or let it create itself).
/// </summary>
public class DemoLevelBuilder : MonoBehaviour
{
    [SerializeField] bool buildOnAwake = false;

    Sprite _pixel;
    Projectile _projectilePrefab;

    void Awake()
    {
        // Levels are now authored in the scene with prefabs. Keep this as a legacy helper only.
        if (buildOnAwake)
            Build();
    }

    [ContextMenu("Build Demo Level")]
    public void Build()
    {
        _pixel = CreatePixelSprite();

        EnsureEventSystem();
        var ui = BuildUi(out Text timerText, out Text statusText, out Text bossHpText);

        var spawnGo = CreateEmpty("SpawnPoint", new Vector3(0f, 1f, 0f));
        var spawn = spawnGo.AddComponent<SpawnPoint>();

        var player = BuildPlayer(spawnGo.transform.position);
        var projectileTemplate = BuildProjectileTemplate();
        _projectilePrefab = projectileTemplate.GetComponent<Projectile>();

        var gun = player.GetComponent<PlayerGun>();
        gun.SetPrefab(_projectilePrefab);

        BuildGroundAndBreakables();
        BuildSpikes();
        BuildProjectileCorridor();
        BuildGrappleSection();
        BuildPortals();
        BuildWindSection();
        BuildFlyerAndBounce();
        BuildShipSection();
        BuildBossArena(bossHpText);
        BuildWinItem();

        var gmGo = CreateEmpty("GameManager", Vector3.zero);
        var gm = gmGo.AddComponent<GameManager>();
        gm.BindReferences(player, spawn, timerText, statusText);

        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<ScreenShake>() == null)
                cam.gameObject.AddComponent<ScreenShake>();
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.SetTarget(player.transform);
            cam.orthographic = true;
            cam.orthographicSize = 6f;
        }

        // Prevent rebuilding if somehow Awake runs twice after domain reload on same instance contents.
        buildOnAwake = false;
    }

    PlayerController BuildPlayer(Vector3 position)
    {
        var go = CreateBody("Player", position, new Vector2(0.9f, 1.1f), new Color(0.95f, 0.55f, 0.2f), false);
        go.tag = "Player";

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<PlayerHealth>();
        go.AddComponent<PlayerPowerupInventory>();
        var player = go.AddComponent<PlayerController>();
        go.AddComponent<GrappleController>();
        go.AddComponent<PlayerGun>();
        go.AddComponent<FallImpactBreaker>();
        return player;
    }

    void BuildGroundAndBreakables()
    {
        // Starting platform
        CreateBody("Platform_Start", new Vector3(0f, 0f, 0f), new Vector2(14f, 1f), new Color(0.35f, 0.35f, 0.4f), false);

        // High ledge for fall-break test
        CreateBody("Platform_High", new Vector3(10f, 8f, 0f), new Vector2(6f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
        CreateBody("Platform_Mid", new Vector3(16f, 3f, 0f), new Vector2(4f, 1f), new Color(0.35f, 0.35f, 0.4f), false);

        for (int i = 0; i < 4; i++)
        {
            var block = CreateBody($"Breakable_{i}", new Vector3(14f + i * 1.05f, 0.75f, 0f), new Vector2(1f, 1f), new Color(0.6f, 0.35f, 0.15f), false);
            block.AddComponent<BreakableBlock>();
        }

        // Path continuation
        CreateBody("Platform_Path", new Vector3(28f, 0f, 0f), new Vector2(20f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
    }

    void BuildSpikes()
    {
        for (int i = 0; i < 5; i++)
        {
            var spike = CreateBody($"Spike_{i}", new Vector3(22f + i * 0.9f, 0.7f, 0f), new Vector2(0.8f, 0.4f), new Color(0.85f, 0.1f, 0.1f), true);
            spike.AddComponent<SpikeHazard>();
        }

        CreateBody("Platform_AfterSpikes", new Vector3(40f, 0f, 0f), new Vector2(12f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
    }

    void BuildProjectileCorridor()
    {
        CreateBody("Platform_Projectile", new Vector3(52f, 0f, 0f), new Vector2(16f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
        var spawnerGo = CreateEmpty("HazardSpawner", new Vector3(60f, 2f, 0f));
        var spawner = spawnerGo.AddComponent<HazardProjectileSpawner>();
        spawner.SetPrefab(_projectilePrefab);
    }

    void BuildGrappleSection()
    {
        CreateBody("Platform_BeforeGrapple", new Vector3(64f, 0f, 0f), new Vector2(8f, 1f), new Color(0.35f, 0.35f, 0.4f), false);

        var pickup = CreateBody("GrapplePickup", new Vector3(64f, 1.2f, 0f), new Vector2(0.7f, 0.7f), new Color(0.2f, 0.9f, 0.9f), true);
        pickup.AddComponent<GrappleGunPickup>();

        // Ceiling to grapple onto across a gap
        CreateBody("GrappleCeiling", new Vector3(72f, 6f, 0f), new Vector2(14f, 0.6f), new Color(0.45f, 0.45f, 0.5f), false);

        CreateBody("Platform_AfterGrapple", new Vector3(82f, 0f, 0f), new Vector2(10f, 1f), new Color(0.35f, 0.35f, 0.4f), false);

        var remove = CreateBody("GrappleRemoveZone", new Vector3(84f, 1.5f, 0f), new Vector2(2f, 3f), new Color(0.2f, 0.4f, 0.5f, 0.35f), true);
        var zone = remove.AddComponent<PowerupRemoveZone>();
        zone.SetType(PowerupType.Grapple);
    }

    void BuildPortals()
    {
        CreateBody("Platform_PortalA", new Vector3(90f, 0f, 0f), new Vector2(8f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
        CreateBody("Platform_PortalB", new Vector3(110f, 4f, 0f), new Vector2(8f, 1f), new Color(0.35f, 0.35f, 0.4f), false);

        var a = CreateBody("PortalA", new Vector3(92f, 1.2f, 0f), new Vector2(1f, 1.5f), new Color(0.6f, 0.2f, 0.9f), true);
        var b = CreateBody("PortalB", new Vector3(108f, 5.2f, 0f), new Vector2(1f, 1.5f), new Color(0.6f, 0.2f, 0.9f), true);
        var portalA = a.AddComponent<Portal>();
        var portalB = b.AddComponent<Portal>();
        portalA.SetLinkId("A");
        portalB.SetLinkId("A");
    }

    void BuildWindSection()
    {
        CreateBody("Platform_Wind", new Vector3(122f, 0f, 0f), new Vector2(16f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
        var wind = CreateBody("WindField", new Vector3(122f, 2f, 0f), new Vector2(10f, 3f), new Color(0.5f, 0.8f, 1f, 0.25f), true);
        wind.AddComponent<WindField>();
    }

    void BuildFlyerAndBounce()
    {
        CreateBody("Platform_Flyer", new Vector3(138f, 0f, 0f), new Vector2(14f, 1f), new Color(0.35f, 0.35f, 0.4f), false);

        var flyer = CreateBody("VerticalFlyer", new Vector3(138f, 2.5f, 0f), new Vector2(1.2f, 1.2f), new Color(0.9f, 0.2f, 0.55f), true);
        var vf = flyer.AddComponent<VerticalFlyer>();
        vf.Configure(2.5f, 2f, 2.5f);

        var pad = CreateBody("BouncePad", new Vector3(144f, 0.7f, 0f), new Vector2(1.5f, 0.4f), new Color(0.2f, 0.95f, 0.35f), true);
        pad.AddComponent<BouncePad>();
        CreateBody("Platform_HighBounce", new Vector3(150f, 5f, 0f), new Vector2(6f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
    }

    void BuildShipSection()
    {
        CreateBody("Platform_BeforeShip", new Vector3(158f, 0f, 0f), new Vector2(8f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
        var ship = CreateBody("ShipPickup", new Vector3(158f, 1.2f, 0f), new Vector2(0.9f, 0.6f), new Color(1f, 0.9f, 0.2f), true);
        ship.AddComponent<ShipPickup>();

        // Open air corridor — no floor for a stretch
        CreateBody("ShipCeiling", new Vector3(170f, 8f, 0f), new Vector2(20f, 0.5f), new Color(0.3f, 0.3f, 0.35f), false);
        CreateBody("ShipFloorHazard", new Vector3(170f, -2f, 0f), new Vector2(20f, 1f), new Color(0.85f, 0.1f, 0.1f), true)
            .AddComponent<SpikeHazard>();

        CreateBody("Platform_AfterShip", new Vector3(184f, 0f, 0f), new Vector2(10f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
        var remove = CreateBody("ShipRemoveZone", new Vector3(184f, 1.5f, 0f), new Vector2(2f, 3f), new Color(0.5f, 0.45f, 0.1f, 0.35f), true);
        remove.AddComponent<PowerupRemoveZone>().SetType(PowerupType.Ship);
    }

    void BuildBossArena(Text bossHpText)
    {
        CreateBody("Platform_Boss", new Vector3(200f, 0f, 0f), new Vector2(24f, 1f), new Color(0.35f, 0.35f, 0.4f), false);

        var gunPickup = CreateBody("GunPickup", new Vector3(192f, 1.2f, 0f), new Vector2(0.7f, 0.7f), new Color(0.95f, 0.75f, 0.2f), true);
        gunPickup.AddComponent<GunPickup>();

        var boss = CreateBody("Boss", new Vector3(208f, 2.5f, 0f), new Vector2(3f, 4f), new Color(0.55f, 0.05f, 0.05f), false);
        // Boss needs a trigger child for bullet hits OR make collider non-trigger and use trigger child
        var hitbox = CreateBody("BossHitbox", boss.transform.position, new Vector2(3.2f, 4.2f), new Color(0.55f, 0.05f, 0.05f, 0.01f), true);
        hitbox.transform.SetParent(boss.transform, true);
        var bossCtrl = boss.AddComponent<BossController>();
        bossCtrl.SetPrefab(_projectilePrefab);
        bossCtrl.SetHpText(bossHpText);
        // Move hitbox component... BossController is on parent; GetComponentInParent works from hitbox collisions on child - projectile checks GetComponentInParent. Child needs to be the one entered - projectile hits child trigger, GetComponentInParent finds BossController. Good.
        // But hitbox is separate object with its own sprite - parent also has collider. Make parent collider non-trigger solid for standing? Boss floating - disable parent physics collision for player walk-through? Keep solid so player can bump.
        // Add BossController reference on child via same parent.

        var gunRemove = CreateBody("GunRemoveZone", new Vector3(214f, 1.5f, 0f), new Vector2(2f, 3f), new Color(0.5f, 0.4f, 0.1f, 0.35f), true);
        gunRemove.AddComponent<PowerupRemoveZone>().SetType(PowerupType.Gun);
    }

    void BuildWinItem()
    {
        CreateBody("Platform_Win", new Vector3(222f, 0f, 0f), new Vector2(10f, 1f), new Color(0.35f, 0.35f, 0.4f), false);
        var win = CreateBody("WinItem", new Vector3(224f, 1.3f, 0f), new Vector2(0.9f, 0.9f), new Color(1f, 0.95f, 0.2f), true);
        win.AddComponent<WinItem>();
    }

    GameObject BuildProjectileTemplate()
    {
        var go = CreateBody("ProjectileTemplate", new Vector3(0f, -50f, 0f), new Vector2(0.35f, 0.2f), new Color(1f, 0.4f, 0.1f), true);
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<Projectile>();
        go.SetActive(false);
        return go;
    }

    Canvas BuildUi(out Text timerText, out Text statusText, out Text bossHpText)
    {
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        timerText = CreateUiText(canvasGo.transform, "TimerText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), 28, TextAnchor.UpperCenter);
        statusText = CreateUiText(canvasGo.transform, "StatusText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 48, TextAnchor.MiddleCenter);
        bossHpText = CreateUiText(canvasGo.transform, "BossHpText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -30f), 22, TextAnchor.UpperRight);
        return canvas;
    }

    static Text CreateUiText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, int fontSize, TextAnchor alignment)
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
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
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

    GameObject CreateBody(string name, Vector3 position, Vector2 size, Color color, bool isTrigger)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _pixel;
        sr.color = color;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = isTrigger;
        return go;
    }

    static GameObject CreateEmpty(string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        return go;
    }

    static Sprite CreatePixelSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
