using UnityEngine;

public class PlayerPowerupInventory : MonoBehaviour
{
    public bool HasGrapple { get; private set; }
    public bool HasGun { get; private set; }
    public bool InShipMode { get; private set; }
    public float ShipDirection { get; private set; } = 1f;

    public void GrantGrapple()
    {
        HasGrapple = true;
    }

    public void RemoveGrapple()
    {
        HasGrapple = false;
        var grapple = GetComponent<GrappleController>();
        if (grapple != null)
            grapple.ForceRelease();
    }

    public void GrantGun()
    {
        HasGun = true;
    }

    public void RemoveGun()
    {
        HasGun = false;
    }

    public void EnterShipMode(float directionSign)
    {
        InShipMode = true;
        ShipDirection = Mathf.Sign(directionSign) >= 0f ? 1f : -1f;
        var grapple = GetComponent<GrappleController>();
        if (grapple != null)
            grapple.ForceRelease();
    }

    public void ExitShipMode()
    {
        InShipMode = false;
    }
}
