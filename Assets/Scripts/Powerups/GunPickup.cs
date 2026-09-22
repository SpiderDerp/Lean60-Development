using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GunPickup : MonoBehaviour
{
    [SerializeField] bool destroyOnPickup = true;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        var inventory = other.GetComponent<PlayerPowerupInventory>();
        if (inventory == null)
            return;

        inventory.GrantGun();
        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
