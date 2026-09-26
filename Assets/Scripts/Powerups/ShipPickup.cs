using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShipPickup : MonoBehaviour
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
        var player = other.GetComponent<PlayerController>();
        if (inventory == null || player == null)
            return;

        inventory.EnterShipMode(player.FacingSign);
        if (destroyOnPickup)
            gameObject.SetActive(false);
    }
}
