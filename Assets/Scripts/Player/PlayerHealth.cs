using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    bool _dead;

    public void Die()
    {
        if (_dead)
            return;

        _dead = true;
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlayerDied();
        else
            _dead = false;
    }

    public void ClearDeathLock()
    {
        _dead = false;
    }
}
