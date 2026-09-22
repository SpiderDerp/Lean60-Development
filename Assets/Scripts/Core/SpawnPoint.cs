using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public Vector3 Position => transform.position;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}
