using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [SerializeField] string linkId = "A";
    [SerializeField] Vector2 exitOffset = new Vector2(1.2f, 0f);
    [SerializeField] float cooldown = 0.4f;

    bool _coolingDown;

    public string LinkId
    {
        get => linkId;
        set => linkId = value;
    }

    public void SetLinkId(string id)
    {
        linkId = id;
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_coolingDown)
            return;
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

        var destination = FindLinkedPortal();
        if (destination == null)
            return;

        player.TeleportTo(destination.transform.position + (Vector3)destination.exitOffset);
        if (string.Equals(linkId.Trim(), "C", System.StringComparison.OrdinalIgnoreCase))
            RevealBossHp();
        StartCoroutine(CooldownGroup());
    }

    static void RevealBossHp()
    {
        var bosses = FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bosses.Length; i++)
        {
            if (bosses[i] != null)
                bosses[i].RevealHpUi();
        }
    }

    Portal FindLinkedPortal()
    {
        if (string.IsNullOrWhiteSpace(linkId))
            return null;

        var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        for (int i = 0; i < portals.Length; i++)
        {
            var other = portals[i];
            if (other == this || !other.isActiveAndEnabled)
                continue;
            if (string.IsNullOrWhiteSpace(other.linkId))
                continue;
            if (string.Equals(other.linkId.Trim(), linkId.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return other;
        }

        return null;
    }

    IEnumerator CooldownGroup()
    {
        var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        for (int i = 0; i < portals.Length; i++)
        {
            var other = portals[i];
            if (other == null)
                continue;
            if (string.IsNullOrWhiteSpace(other.linkId))
                continue;
            if (string.Equals(other.linkId.Trim(), linkId.Trim(), System.StringComparison.OrdinalIgnoreCase))
                other._coolingDown = true;
        }

        yield return new WaitForSeconds(cooldown);

        for (int i = 0; i < portals.Length; i++)
        {
            if (portals[i] != null)
                portals[i]._coolingDown = false;
        }
    }
}
