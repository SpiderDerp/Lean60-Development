using UnityEngine;

/// <summary>
/// Optional standalone timer component; GameManager owns the run timer by default.
/// Kept for placeable countdown displays if needed.
/// </summary>
public class GameTimer : MonoBehaviour
{
    [SerializeField] float duration = 60f;

    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; } = true;

    public System.Action OnExpired;

    void Awake()
    {
        TimeRemaining = duration;
    }

    void Update()
    {
        if (!IsRunning)
            return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining > 0f)
            return;

        TimeRemaining = 0f;
        IsRunning = false;
        OnExpired?.Invoke();
    }

    public void ResetTimer()
    {
        TimeRemaining = duration;
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }
}
