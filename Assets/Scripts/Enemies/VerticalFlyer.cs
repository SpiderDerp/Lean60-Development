using UnityEngine;

public class VerticalFlyer : MonoBehaviour
{
    [SerializeField] float amplitude = 2f;
    [SerializeField] float speed = 2.5f;
    [SerializeField] bool startGoingUp = true;

    float _centerY;
    float _direction = 1f;
    float _topY;
    float _bottomY;

    void Awake()
    {
        _centerY = transform.position.y;
        _topY = _centerY + amplitude;
        _bottomY = _centerY - amplitude;
        _direction = startGoingUp ? 1f : -1f;
    }

    public void Configure(float centerY, float amp, float moveSpeed)
    {
        _centerY = centerY;
        amplitude = amp;
        speed = moveSpeed;
        _topY = _centerY + amplitude;
        _bottomY = _centerY - amplitude;
        transform.position = new Vector3(transform.position.x, _centerY, transform.position.z);
    }

    void Update()
    {
        Vector3 pos = transform.position;
        pos.y += _direction * speed * Time.deltaTime;

        if (pos.y >= _topY)
        {
            pos.y = _topY;
            _direction = -1f;
        }
        else if (pos.y <= _bottomY)
        {
            pos.y = _bottomY;
            _direction = 1f;
        }

        transform.position = pos;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        KillPlayer(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        KillPlayer(collision.collider);
    }

    static void KillPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
            health.Die();
    }
}
