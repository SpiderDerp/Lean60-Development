using UnityEngine;

public class BreakableBlock : MonoBehaviour
{
    public void Break()
    {
        gameObject.SetActive(false);
    }

    public void Restore()
    {
        gameObject.SetActive(true);
    }
}
