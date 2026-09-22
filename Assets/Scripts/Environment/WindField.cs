using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WindField : MonoBehaviour
{
    [SerializeField] Vector2 force = new Vector2(25f, 0f);

    public void SetForce(Vector2 windForce)
    {
        force = windForce;
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<PlayerController>();
        if (player != null)
            player.AddExternalForce(force);
    }
}
