using UnityEngine;

public class BreakableBlock : MonoBehaviour
{
    [SerializeField] bool destroyOnBreak = true;

    public void Break()
    {
        if (destroyOnBreak)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
