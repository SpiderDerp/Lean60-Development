using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float lifetime = 6f;
    [SerializeField] bool damagesPlayer = true;
    [SerializeField] int bossDamage = 1;

    Vector2 _direction = Vector2.right;
    float _age;
    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void Launch(Vector2 direction, bool hurtPlayer, float overrideSpeed = -1f)
    {
        _direction = direction.normalized;
        damagesPlayer = hurtPlayer;
        if (overrideSpeed > 0f)
            speed = overrideSpeed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(_direction * speed * Time.deltaTime);
        _age += Time.deltaTime;
        if (_age >= lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Projectile>() != null)
            return;

        if (damagesPlayer && other.CompareTag("Player"))
        {
            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
                health.Die();
            Destroy(gameObject);
            return;
        }

        if (!damagesPlayer)
        {
            var boss = other.GetComponentInParent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(bossDamage);
                Destroy(gameObject);
                return;
            }
        }

        if (other.isTrigger || other.CompareTag("Player"))
            return;

        Destroy(gameObject);
    }
}
