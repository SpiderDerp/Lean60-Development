using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [SerializeField] Portal linkedPortal;
    [SerializeField] Vector2 exitOffset = new Vector2(1.2f, 0f);
    [SerializeField] float cooldown = 0.4f;

    bool _coolingDown;

    public Portal LinkedPortal
    {
        get => linkedPortal;
        set => linkedPortal = value;
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_coolingDown || linkedPortal == null)
            return;
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

        Vector3 destination = linkedPortal.transform.position + (Vector3)linkedPortal.exitOffset;
        player.TeleportTo(destination);
        StartCoroutine(CooldownBoth());
    }

    IEnumerator CooldownBoth()
    {
        _coolingDown = true;
        linkedPortal._coolingDown = true;
        yield return new WaitForSeconds(cooldown);
        _coolingDown = false;
        if (linkedPortal != null)
            linkedPortal._coolingDown = false;
    }
}
