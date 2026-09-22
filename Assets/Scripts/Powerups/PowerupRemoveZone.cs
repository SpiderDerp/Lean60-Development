using UnityEngine;

public enum PowerupType
{
    Grapple,
    Ship,
    Gun
}

[RequireComponent(typeof(Collider2D))]
public class PowerupRemoveZone : MonoBehaviour
{
    [SerializeField] PowerupType powerupType = PowerupType.Grapple;

    public void SetType(PowerupType type)
    {
        powerupType = type;
    }

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

        switch (powerupType)
        {
            case PowerupType.Grapple:
                inventory.RemoveGrapple();
                break;
            case PowerupType.Ship:
                inventory.ExitShipMode();
                break;
            case PowerupType.Gun:
                inventory.RemoveGun();
                break;
        }
    }
}
