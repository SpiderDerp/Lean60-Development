using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [SerializeField] float defaultDuration = 0.2f;
    [SerializeField] float defaultMagnitude = 0.25f;

    float _durationRemaining;
    float _magnitude;
    Vector3 _offset;

    public Vector3 CurrentOffset => _offset;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (_durationRemaining <= 0f)
        {
            _offset = Vector3.zero;
            return;
        }

        _durationRemaining -= Time.unscaledDeltaTime;
        _offset = Random.insideUnitCircle * _magnitude;
        if (_durationRemaining <= 0f)
            _offset = Vector3.zero;
    }

    public void Shake(float magnitude = -1f, float duration = -1f)
    {
        _magnitude = magnitude > 0f ? magnitude : defaultMagnitude;
        _durationRemaining = duration > 0f ? duration : defaultDuration;
    }
}
