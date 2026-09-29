using UnityEngine;

public class HazardProjectileSpawner : MonoBehaviour
{
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Vector2 direction = Vector2.left;
    [SerializeField] float interval = 1.5f;
    [SerializeField] float projectileSpeed = 10f;
    [SerializeField] bool autoStart = true;

    float _timer;

    public void SetPrefab(Projectile prefab)
    {
        projectilePrefab = prefab;
    }

    void Update()
    {
        if (!autoStart || projectilePrefab == null)
            return;

        _timer += Time.deltaTime;
        if (_timer < interval)
            return;

        _timer = 0f;
        Fire();
    }

    public void Fire()
    {
        if (projectilePrefab == null)
            return;

        var proj = Projectile.Spawn(projectilePrefab, transform.position);
        if (proj == null)
            return;
        proj.Launch(direction.normalized, true, projectileSpeed);
    }
}
