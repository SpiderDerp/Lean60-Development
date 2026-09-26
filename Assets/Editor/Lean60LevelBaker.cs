#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Instantiates the starter Lean60 course as prefab instances you can drag around.
/// Menu: Lean60/Bake Demo Level Into Scene
/// </summary>
public static class Lean60LevelBaker
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string PrefabFolder = "Assets/Prefabs";

    [InitializeOnLoadMethod]
    static void AutoBakeIfNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying)
                return;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;
            if (GameObject.Find("Level") != null)
                return;
            Bake();
        };
    }

    [MenuItem("Lean60/Bake Demo Level Into Scene")]
    public static void Bake()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var existing = GameObject.Find("Level");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var oldBuilder = GameObject.Find("DemoLevelBuilder");
        if (oldBuilder != null)
            Object.DestroyImmediate(oldBuilder);

        int ground = LayerMask.NameToLayer("Ground");
        var projectilePrefab = LoadPrefab("Projectile");
        var projectile = projectilePrefab != null ? projectilePrefab.GetComponent<Projectile>() : null;
        var level = new GameObject("Level");

        Place("SpawnPoint", new Vector3(0f, 1f, 0f));
        var playerGo = Place("Player", new Vector3(0f, 1.2f, 0f));

        PlaceScaled("Platform", "Platform_Start", new Vector3(0f, 0f, 0f), new Vector3(14f, 1f, 1f), ground);
        PlaceScaled("Platform", "Platform_High", new Vector3(10f, 8f, 0f), new Vector3(6f, 1f, 1f), ground);
        PlaceScaled("Platform", "Platform_Mid", new Vector3(16f, 3f, 0f), new Vector3(4f, 1f, 1f), ground);
        for (int i = 0; i < 4; i++)
            PlaceScaled("BreakableBlock", $"Breakable_{i}", new Vector3(14f + i * 1.05f, 0.75f, 0f), Vector3.one, ground);
        PlaceScaled("Platform", "Platform_Path", new Vector3(28f, 0f, 0f), new Vector3(20f, 1f, 1f), ground);

        for (int i = 0; i < 5; i++)
            Place("SpikeHazard", new Vector3(22f + i * 0.9f, 0.7f, 0f), $"Spike_{i}");
        PlaceScaled("Platform", "Platform_AfterSpikes", new Vector3(40f, 0f, 0f), new Vector3(12f, 1f, 1f), ground);

        PlaceScaled("Platform", "Platform_Projectile", new Vector3(52f, 0f, 0f), new Vector3(16f, 1f, 1f), ground);
        var spawner = Place("HazardProjectileSpawner", new Vector3(60f, 2f, 0f));
        var spawnerComp = spawner.GetComponent<HazardProjectileSpawner>();
        if (spawnerComp != null && projectile != null)
            spawnerComp.SetPrefab(projectile);

        PlaceScaled("Platform", "Platform_BeforeGrapple", new Vector3(64f, 0f, 0f), new Vector3(8f, 1f, 1f), ground);
        Place("GrappleGunPickup", new Vector3(64f, 1.2f, 0f));
        PlaceScaled("Platform", "GrappleCeiling", new Vector3(72f, 6f, 0f), new Vector3(14f, 0.6f, 1f), ground);
        PlaceScaled("Platform", "Platform_AfterGrapple", new Vector3(82f, 0f, 0f), new Vector3(10f, 1f, 1f), ground);
        Place("GrappleRemoveZone", new Vector3(84f, 1.5f, 0f));

        PlaceScaled("Platform", "Platform_PortalA", new Vector3(90f, 0f, 0f), new Vector3(8f, 1f, 1f), ground);
        PlaceScaled("Platform", "Platform_PortalB", new Vector3(110f, 4f, 0f), new Vector3(8f, 1f, 1f), ground);
        var portalA = Place("Portal", new Vector3(92f, 1.2f, 0f), "PortalA").GetComponent<Portal>();
        var portalB = Place("Portal", new Vector3(108f, 5.2f, 0f), "PortalB").GetComponent<Portal>();
        if (portalA != null)
            portalA.SetLinkId("A");
        if (portalB != null)
            portalB.SetLinkId("A");

        PlaceScaled("Platform", "Platform_Wind", new Vector3(122f, 0f, 0f), new Vector3(16f, 1f, 1f), ground);
        var wind = Place("WindField", new Vector3(122f, 2f, 0f));
        wind.transform.localScale = new Vector3(10f, 3f, 1f);

        PlaceScaled("Platform", "Platform_Flyer", new Vector3(138f, 0f, 0f), new Vector3(14f, 1f, 1f), ground);
        Place("VerticalFlyer", new Vector3(138f, 2.5f, 0f));
        Place("BouncePad", new Vector3(144f, 0.7f, 0f));
        PlaceScaled("Platform", "Platform_HighBounce", new Vector3(150f, 5f, 0f), new Vector3(6f, 1f, 1f), ground);

        PlaceScaled("Platform", "Platform_BeforeShip", new Vector3(158f, 0f, 0f), new Vector3(8f, 1f, 1f), ground);
        Place("ShipPickup", new Vector3(158f, 1.2f, 0f));
        PlaceScaled("Platform", "ShipCeiling", new Vector3(170f, 8f, 0f), new Vector3(20f, 0.5f, 1f), ground);
        PlaceScaled("SpikeHazard", "ShipFloorHazard", new Vector3(170f, -2f, 0f), new Vector3(20f, 1f, 1f), -1);
        PlaceScaled("Platform", "Platform_AfterShip", new Vector3(184f, 0f, 0f), new Vector3(10f, 1f, 1f), ground);
        Place("ShipRemoveZone", new Vector3(184f, 1.5f, 0f));

        PlaceScaled("Platform", "Platform_Boss", new Vector3(200f, 0f, 0f), new Vector3(24f, 1f, 1f), ground);
        Place("GunPickup", new Vector3(192f, 1.2f, 0f));
        var boss = Place("Boss", new Vector3(208f, 2.5f, 0f));
        var bossCtrl = boss.GetComponent<BossController>();
        if (bossCtrl != null && projectile != null)
            bossCtrl.SetPrefab(projectile);
        Place("GunRemoveZone", new Vector3(214f, 1.5f, 0f));

        PlaceScaled("Platform", "Platform_Win", new Vector3(222f, 0f, 0f), new Vector3(10f, 1f, 1f), ground);
        Place("WinItem", new Vector3(224f, 1.3f, 0f));

        EnsureCoreObjects(playerGo, projectile);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Lean60 starter level baked into SampleScene. Drag items under Level to redesign.");

        GameObject Place(string prefabName, Vector3 position, string instanceName = null)
        {
            var prefab = LoadPrefab(prefabName);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, level.transform);
            instance.transform.position = position;
            if (!string.IsNullOrEmpty(instanceName))
                instance.name = instanceName;
            return instance;
        }

        GameObject PlaceScaled(string prefabName, string instanceName, Vector3 position, Vector3 scale, int layer)
        {
            var instance = Place(prefabName, position, instanceName);
            instance.transform.localScale = scale;
            if (layer >= 0)
                SetLayerRecursive(instance, layer);
            return instance;
        }
    }

    static void EnsureCoreObjects(GameObject playerGo, Projectile projectile)
    {
        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<ScreenShake>() == null)
                cam.gameObject.AddComponent<ScreenShake>();
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<CameraFollow>();
            if (playerGo != null)
                follow.SetTarget(playerGo.transform);
            cam.orthographic = true;
            cam.orthographicSize = 6f;
        }

        if (GameObject.Find("HUD") == null)
            CreateHud();

        if (Object.FindFirstObjectByType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
        if (bootstrap == null)
            bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        if (projectile != null)
            bootstrap.SetProjectilePrefab(projectile);

        var gun = playerGo != null ? playerGo.GetComponent<PlayerGun>() : null;
        if (gun != null && projectile != null)
            gun.SetPrefab(projectile);

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    static void CreateHud()
    {
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        CreateText(canvasGo.transform, "TimerText", new Vector2(0.5f, 1f), new Vector2(0f, -30f), 28, TextAnchor.UpperCenter);
        CreateText(canvasGo.transform, "StatusText", new Vector2(0.5f, 0.5f), Vector2.zero, 48, TextAnchor.MiddleCenter);
        CreateText(canvasGo.transform, "BossHpText", new Vector2(1f, 1f), new Vector2(-20f, -30f), 22, TextAnchor.UpperRight);
    }

    static Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 pos, int size, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        var rt = text.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(480f, 80f);
        return text;
    }

    static GameObject LoadPrefab(string name)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            throw new System.InvalidOperationException("Missing prefab: " + path);
        return asset;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
#endif
