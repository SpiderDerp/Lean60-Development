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

        var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        proj.gameObject.SetActive(true);
        proj.Launch(direction.normalized, true, projectileSpeed);
    }
}
