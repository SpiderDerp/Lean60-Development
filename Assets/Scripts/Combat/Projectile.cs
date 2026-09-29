using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float lifetime = 6f;
    [SerializeField] bool damagesPlayer = true;
    [SerializeField] int bossDamage = 1;

    static readonly List<Projectile> _inactive = new List<Projectile>(32);

    Vector2 _direction = Vector2.right;
    float _age;
    float _defaultSpeed;
    float _defaultLifetime;
    Rigidbody2D _rb;

    public static Projectile Spawn(Projectile prefab, Vector3 position)
    {
        if (prefab == null)
            return null;

        Projectile p = null;
        for (int i = _inactive.Count - 1; i >= 0; i--)
        {
            p = _inactive[i];
            _inactive.RemoveAt(i);
            if (p != null)
                break;
            p = null;
        }

        if (p == null)
            p = Instantiate(prefab, position, Quaternion.identity);
        else
            p.transform.SetPositionAndRotation(position, Quaternion.identity);

        p.gameObject.SetActive(true);
        return p;
    }

    public void Release()
    {
        if (!gameObject.activeSelf)
            return;

        _age = 0f;
        gameObject.SetActive(false);
        _inactive.Add(this);
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        _defaultSpeed = speed;
        _defaultLifetime = lifetime;
    }

    public void Launch(Vector2 direction, bool hurtPlayer, float overrideSpeed = -1f, float overrideLifetime = -1f)
    {
        _age = 0f;
        _direction = direction.normalized;
        damagesPlayer = hurtPlayer;
        speed = overrideSpeed > 0f ? overrideSpeed : _defaultSpeed;
        lifetime = overrideLifetime > 0f ? overrideLifetime : _defaultLifetime;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(_direction * speed * Time.deltaTime);
        _age += Time.deltaTime;
        if (_age >= lifetime)
            Release();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Projectile>() != null)
            return;

        if (damagesPlayer && other.GetComponentInParent<BossController>() != null)
            return;

        if (damagesPlayer && other.CompareTag("Player"))
        {
            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
                health.Die();
            Release();
            return;
        }

        if (!damagesPlayer)
        {
            var boss = other.GetComponentInParent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(bossDamage);
                Release();
                return;
            }
        }

        if (other.isTrigger || other.CompareTag("Player"))
            return;

        Release();
    }
}
