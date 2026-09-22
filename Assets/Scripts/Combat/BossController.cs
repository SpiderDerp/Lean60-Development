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
    [SerializeField] AttackPattern[] patterns;
    [SerializeField] Text hpText;
    [SerializeField] Transform firePoint;

    int _hp;
    int _patternIndex;
    float _patternTimer;
    int _shotsFired;
    float _shotTimer;
    bool _inPattern;
    Transform _player;

    public void SetPrefab(Projectile prefab)
    {
        projectilePrefab = prefab;
    }

    public void SetHpText(Text text)
    {
        hpText = text;
    }

    void Awake()
    {
        _hp = maxHp;
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
        Vector2 dir = pattern.fixedDirection.normalized;
        switch (pattern.type)
        {
            case PatternType.AimedBurst:
                if (_player != null)
                    dir = ((Vector2)_player.position - (Vector2)firePoint.position).normalized;
                SpawnBullet(dir, pattern.projectileSpeed);
                break;
            case PatternType.FixedFan:
            {
                float t = pattern.shotCount <= 1 ? 0.5f : _shotsFired / (float)(pattern.shotCount - 1);
                float angle = Mathf.Lerp(-pattern.fanSpread * 0.5f, pattern.fanSpread * 0.5f, t);
                Vector2 baseDir = pattern.fixedDirection.normalized;
                float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
                float rad = (baseAngle + angle) * Mathf.Deg2Rad;
                SpawnBullet(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), pattern.projectileSpeed);
                break;
            }
            default:
                SpawnBullet(dir, pattern.projectileSpeed);
                break;
        }
    }

    void SpawnBullet(Vector2 direction, float speed)
    {
        var proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.gameObject.SetActive(true);
        proj.Launch(direction, true, speed);
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

    void UpdateHpUi()
    {
        if (hpText != null)
            hpText.text = $"Boss HP: {Mathf.Max(0, _hp)}";
    }
}
