using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpikeHazard : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryKill(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryKill(collision.collider);
    }

    static void TryKill(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
            health.Die();
    }
}
