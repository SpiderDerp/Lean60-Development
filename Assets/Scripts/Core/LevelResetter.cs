using UnityEngine;

public static class LevelResetter
{
    public static void ResetLevel()
    {
        var blocks = Object.FindObjectsByType<BreakableBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < blocks.Length; i++)
            blocks[i].Restore();

        RestorePickups<GrappleGunPickup>();
        RestorePickups<ShipPickup>();
        RestorePickups<GunPickup>();

        var bosses = Object.FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bosses.Length; i++)
            bosses[i].ResetFight();

        var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] != null && projectiles[i].gameObject.scene.IsValid())
                Object.Destroy(projectiles[i].gameObject);
        }
    }

    static void RestorePickups<T>() where T : MonoBehaviour
    {
        var items = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].gameObject.SetActive(true);
        }
    }
}
