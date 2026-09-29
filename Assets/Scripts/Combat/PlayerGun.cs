using UnityEngine;

public class PlayerGun : MonoBehaviour
{
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] float fireCooldown = 0.25f;
    [SerializeField] float bulletSpeed = 16f;
    [SerializeField] float maxRange = 8f;
    [SerializeField] Vector2 muzzleOffset = new Vector2(0.6f, 0.1f);

    PlayerController _player;
    float _cooldownRemaining;

    public void SetPrefab(Projectile prefab)
    {
        projectilePrefab = prefab;
    }

    void Awake()
    {
    }

    PlayerController Player
    {
        get
        {
            if (_player == null)
                _player = GetComponent<PlayerController>();
            return _player;
        }
    }

    void Update()
    {
        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= Time.deltaTime;
    }

    public void TryFire()
    {
        if (projectilePrefab == null || _cooldownRemaining > 0f || Player == null)
            return;

        _cooldownRemaining = fireCooldown;
        Vector2 dir = new Vector2(Player.FacingSign, 0f);
        Vector3 spawnPos = transform.position + new Vector3(muzzleOffset.x * Player.FacingSign, muzzleOffset.y, 0f);
        var proj = Projectile.Spawn(projectilePrefab, spawnPos);
        if (proj == null)
            return;
        float lifetime = bulletSpeed > 0.01f ? maxRange / bulletSpeed : 0.4f;
        proj.Launch(dir, false, bulletSpeed, lifetime);
    }
}
