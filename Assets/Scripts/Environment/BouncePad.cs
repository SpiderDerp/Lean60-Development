using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    [SerializeField] float bounceForce = 18f;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

        Vector2 v = player.Body.linearVelocity;
        player.ApplyExternalVelocity(new Vector2(v.x, bounceForce));
    }
}
