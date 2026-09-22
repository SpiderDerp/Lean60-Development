#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates placeholder prefabs under Assets/Prefabs for level design.
/// Menu: Lean60/Generate Placeholder Prefabs
/// </summary>
public static class Lean60PrefabGenerator
{
    const string PrefabFolder = "Assets/Prefabs";
    const string MarkerPath = "Assets/Prefabs/.generated";

    [InitializeOnLoadMethod]
    static void AutoGenerateOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying)
                return;
            if (System.IO.File.Exists(MarkerPath))
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Scripts"))
                return;
            Generate();
        };
    }

    [MenuItem("Lean60/Generate Placeholder Prefabs")]
    public static void Generate()
    {
        try
        {
            GenerateInternal();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    static void GenerateInternal()
    {
        EnsureFolder(PrefabFolder);
        var pixel = CreatePixelSpriteAsset();

        CreateSolidPrefab("Platform", pixel, new Color(0.35f, 0.35f, 0.4f), new Vector2(2f, 1f), false, null);
        CreateSolidPrefab("BreakableBlock", pixel, new Color(0.6f, 0.35f, 0.15f), Vector2.one, false, typeof(BreakableBlock));
        CreateSolidPrefab("SpikeHazard", pixel, new Color(0.85f, 0.1f, 0.1f), new Vector2(1f, 0.4f), true, typeof(SpikeHazard));
        CreateSolidPrefab("BouncePad", pixel, new Color(0.2f, 0.95f, 0.35f), new Vector2(1.5f, 0.4f), true, typeof(BouncePad));
        CreateSolidPrefab("WindField", pixel, new Color(0.5f, 0.8f, 1f, 0.35f), new Vector2(4f, 3f), true, typeof(WindField));
        CreateSolidPrefab("Portal", pixel, new Color(0.6f, 0.2f, 0.9f), new Vector2(1f, 1.5f), true, typeof(Portal));
        CreateSolidPrefab("VerticalFlyer", pixel, new Color(0.9f, 0.2f, 0.55f), Vector2.one, true, typeof(VerticalFlyer));
        CreateSolidPrefab("GrappleGunPickup", pixel, new Color(0.2f, 0.9f, 0.9f), Vector2.one * 0.7f, true, typeof(GrappleGunPickup));
        CreateSolidPrefab("ShipPickup", pixel, new Color(1f, 0.9f, 0.2f), new Vector2(0.9f, 0.6f), true, typeof(ShipPickup));
        CreateSolidPrefab("GunPickup", pixel, new Color(0.95f, 0.75f, 0.2f), Vector2.one * 0.7f, true, typeof(GunPickup));
        CreateSolidPrefab("WinItem", pixel, new Color(1f, 0.95f, 0.2f), Vector2.one * 0.9f, true, typeof(WinItem));
        CreateSolidPrefab("SpawnPoint", pixel, new Color(0.2f, 1f, 1f), Vector2.one * 0.4f, true, typeof(SpawnPoint));

        CreateRemoveZonePrefab(pixel, PowerupType.Grapple, "GrappleRemoveZone");
        CreateRemoveZonePrefab(pixel, PowerupType.Ship, "ShipRemoveZone");
        CreateRemoveZonePrefab(pixel, PowerupType.Gun, "GunRemoveZone");

        CreateProjectilePrefab(pixel);
        CreatePlayerPrefab(pixel);
        CreateBossPrefab(pixel);
        CreateSpawnerPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        System.IO.Directory.CreateDirectory(PrefabFolder);
        System.IO.File.WriteAllText(MarkerPath, "ok");
        AssetDatabase.Refresh();
        Debug.Log("Lean60 placeholder prefabs generated in " + PrefabFolder);
    }

    static void CreateRemoveZonePrefab(Sprite pixel, PowerupType type, string name)
    {
        var go = CreateBase(name, pixel, new Color(0.3f, 0.3f, 0.3f, 0.35f), new Vector2(2f, 3f), true);
        var zone = go.AddComponent<PowerupRemoveZone>();
        zone.SetType(type);
        Save(go, name);
    }

    static void CreateProjectilePrefab(Sprite pixel)
    {
        var go = CreateBase("Projectile", pixel, new Color(1f, 0.4f, 0.1f), new Vector2(0.35f, 0.2f), true);
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<Projectile>();
        Save(go, "Projectile");
    }

    static void CreateSpawnerPrefab()
    {
        var go = new GameObject("HazardProjectileSpawner");
        go.AddComponent<HazardProjectileSpawner>();
        Save(go, "HazardProjectileSpawner");
    }

    static void CreateBossPrefab(Sprite pixel)
    {
        var go = CreateBase("Boss", pixel, new Color(0.55f, 0.05f, 0.05f), new Vector2(3f, 4f), false);
        go.AddComponent<BossController>();
        var hit = CreateBase("Hitbox", pixel, new Color(1f, 1f, 1f, 0.01f), new Vector2(3.2f, 4.2f), true);
        hit.transform.SetParent(go.transform, false);
        hit.transform.localPosition = Vector3.zero;
        Save(go, "Boss");
    }

    static void CreatePlayerPrefab(Sprite pixel)
    {
        var go = CreateBase("Player", pixel, new Color(0.95f, 0.55f, 0.2f), new Vector2(0.9f, 1.1f), false);
        go.tag = "Player";
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<PlayerHealth>();
        go.AddComponent<PlayerPowerupInventory>();
        go.AddComponent<GrappleController>();
        go.AddComponent<PlayerGun>();
        go.AddComponent<FallImpactBreaker>();
        go.AddComponent<PlayerController>();
        Save(go, "Player");
    }

    static GameObject CreateSolidPrefab(string name, Sprite pixel, Color color, Vector2 size, bool trigger, System.Type component)
    {
        var go = CreateBase(name, pixel, color, size, trigger);
        if (component != null)
            go.AddComponent(component);
        Save(go, name);
        return go;
    }

    static GameObject CreateBase(string name, Sprite pixel, Color color, Vector2 size, bool trigger)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pixel;
        sr.color = color;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = trigger;
        return go;
    }

    static void Save(GameObject go, string name)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static Sprite CreatePixelSpriteAsset()
    {
        EnsureFolder("Assets/Art");
        string texPath = "Assets/Art/PixelWhite.png";
        if (!System.IO.File.Exists(texPath))
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 1;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
