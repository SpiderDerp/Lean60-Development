using System;
using UnityEngine;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    public enum PatternType
    {
        AimedBurst,
        FixedFan,
        StraightVolley
    }

    [Serializable]
    public class AttackPattern
    {
        public PatternType type = PatternType.AimedBurst;
        public float delayBefore = 1f;
        public int shotCount = 5;
        public float shotInterval = 0.25f;
        public float projectileSpeed = 8f;
        public float fanSpread = 60f;
        public Vector2 fixedDirection = Vector2.left;
    }

    [SerializeField] int maxHp = 20;
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] float projectileRange = 32f;
    [SerializeField] AttackPattern[] patterns;
    [SerializeField] Image hpFill;
    [SerializeField] GameObject hpBarRoot;
    [SerializeField] Transform firePoint;

    int _hp;
    int _patternIndex;
    float _patternTimer;
    int _shotsFired;
    float _shotTimer;
    bool _inPattern;
    Transform _player;
    Collider2D _body;
    bool _hpVisible;

    public void SetPrefab(Projectile prefab)
    {
        projectilePrefab = prefab;
    }

    public void SetHpBar(Image fill, GameObject root)
    {
        hpFill = fill;
        hpBarRoot = root;
        UpdateHpUi();
    }

    void Awake()
    {
        _hp = maxHp;
        _body = GetComponent<Collider2D>();
        if (firePoint == null)
            firePoint = transform;
        if (patterns == null || patterns.Length == 0)
        {
            patterns = new[]
            {
                new AttackPattern { type = PatternType.AimedBurst, delayBefore = 1.2f, shotCount = 4, shotInterval = 0.3f },
                new AttackPattern { type = PatternType.FixedFan, delayBefore = 0.8f, shotCount = 5, shotInterval = 0.05f, fanSpread = 50f },
                new AttackPattern { type = PatternType.StraightVolley, delayBefore = 1f, shotCount = 6, shotInterval = 0.2f, fixedDirection = Vector2.left }
            };
        }
    }

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            _player = player.transform;
        UpdateHpUi();
    }

    void Update()
    {
        if (_hp <= 0 || projectilePrefab == null)
            return;

        if (!_inPattern)
        {
            _patternTimer += Time.deltaTime;
            var pattern = patterns[_patternIndex % patterns.Length];
            if (_patternTimer >= pattern.delayBefore)
            {
                _inPattern = true;
                _shotsFired = 0;
                _shotTimer = 0f;
                _patternTimer = 0f;
            }
            return;
        }

        var active = patterns[_patternIndex % patterns.Length];
        _shotTimer += Time.deltaTime;
        if (_shotTimer < active.shotInterval)
            return;

        _shotTimer = 0f;
        FirePatternShot(active);
        _shotsFired++;

        if (_shotsFired >= active.shotCount)
        {
            _inPattern = false;
            _patternIndex = (_patternIndex + 1) % patterns.Length;
        }
    }

    void FirePatternShot(AttackPattern pattern)
    {
        Vector2 aim = AimAtPlayer();
        switch (pattern.type)
        {
            case PatternType.FixedFan:
            {
                float t = pattern.shotCount <= 1 ? 0.5f : _shotsFired / (float)(pattern.shotCount - 1);
                float angle = Mathf.Lerp(-pattern.fanSpread * 0.5f, pattern.fanSpread * 0.5f, t);
                float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                float rad = (baseAngle + angle) * Mathf.Deg2Rad;
                SpawnBullet(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), pattern.projectileSpeed);
                break;
            }
            default:
                SpawnBullet(aim, pattern.projectileSpeed);
                break;
        }
    }

    void EnsurePlayer()
    {
        if (_player != null)
            return;
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            _player = player.transform;
    }

    Vector2 AimAtPlayer()
    {
        EnsurePlayer();
        Vector2 from = transform.position;
        if (_body != null)
            from = _body.bounds.center;
        if (_player == null)
            return Vector2.left;
        Vector2 dir = (Vector2)_player.position - from;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.left;
    }

    void SpawnBullet(Vector2 direction, float speed)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
        var proj = Projectile.Spawn(projectilePrefab, SpawnOutsideBody(dir));
        if (proj == null)
            return;
        float lifetime = speed > 0.01f ? projectileRange / speed : 0.5f;
        proj.Launch(dir, true, speed, lifetime);
    }

    Vector3 SpawnOutsideBody(Vector2 direction)
    {
        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        if (_body == null)
            return origin;

        Bounds b = _body.bounds;
        if (!b.Contains(origin))
            return origin;

        float tx = Mathf.Abs(direction.x) > 0.0001f ? b.extents.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float ty = Mathf.Abs(direction.y) > 0.0001f ? b.extents.y / Mathf.Abs(direction.y) : float.PositiveInfinity;
        float toSurface = Mathf.Min(tx, ty);
        if (float.IsInfinity(toSurface))
            return origin;

        return (Vector2)b.center + direction * (toSurface + 0.25f);
    }

    public void TakeDamage(int amount)
    {
        if (_hp <= 0)
            return;

        _hp -= amount;
        UpdateHpUi();
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(0.2f, 0.1f);

        if (_hp <= 0)
        {
            _hp = 0;
            gameObject.SetActive(false);
        }
    }

    public void ResetFight()
    {
        _hp = maxHp;
        _patternIndex = 0;
        _patternTimer = 0f;
        _shotsFired = 0;
        _shotTimer = 0f;
        _inPattern = false;
        _hpVisible = false;
        gameObject.SetActive(true);
        UpdateHpUi();
    }

    public void RevealHpUi()
    {
        _hpVisible = true;
        UpdateHpUi();
    }

    void UpdateHpUi()
    {
        if (hpFill != null)
        {
            float max = Mathf.Max(1, maxHp);
            hpFill.fillAmount = Mathf.Clamp01(_hp / max);
        }

        if (hpBarRoot != null)
            hpBarRoot.SetActive(_hpVisible);
    }
}
