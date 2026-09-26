using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class FallImpactBreaker : MonoBehaviour
{
    [SerializeField] float impactVelocityThreshold = 14f;
    [SerializeField] float breakRadius = 0.7f;
    [SerializeField] Vector2 breakBoxSize = new Vector2(1.2f, 0.6f);
    [SerializeField] float shakeMagnitude = 0.35f;
    [SerializeField] float shakeDuration = 0.25f;

    PlayerController _player;
    bool _wasGrounded;
    float _peakDownSpeed;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    void FixedUpdate()
    {
        bool grounded = _player.IsGrounded;
        float vy = _player.Body.linearVelocity.y;

        if (!grounded)
        {
            if (vy < 0f)
                _peakDownSpeed = Mathf.Max(_peakDownSpeed, -vy);
        }
        else if (!_wasGrounded)
        {
            if (_peakDownSpeed >= impactVelocityThreshold)
                OnHeavyLanding(_peakDownSpeed);
            _peakDownSpeed = 0f;
        }
        else
        {
            _peakDownSpeed = 0f;
        }

        _wasGrounded = grounded;
    }

    void OnHeavyLanding(float impactSpeed)
    {
        float intensity = Mathf.Clamp01(impactSpeed / (impactVelocityThreshold * 1.6f));
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(shakeMagnitude * (0.7f + intensity), shakeDuration);

        Vector2 origin = (Vector2)transform.position + Vector2.down * breakRadius;
        var hits = Physics2D.OverlapBoxAll(origin, breakBoxSize, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            var block = hits[i].GetComponentInParent<BreakableBlock>();
            if (block != null)
                block.Break();
        }
    }
}
