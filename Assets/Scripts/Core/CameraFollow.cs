using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] float horizontalSmoothTime = 0.08f;
    [SerializeField] float verticalSmoothTime = 0.04f;
    [SerializeField] float fallLookAhead = 0.12f;
    [SerializeField] float maxLookAhead = 4f;
    [SerializeField] Vector2 viewPadding = new Vector2(0.28f, 0.22f);

    Camera _camera;
    Rigidbody2D _targetBody;
    Vector3 _velocity;
    Vector3 _smoothedPosition;
    bool _initialized;
    bool _searchAttempted;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        _targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
        if (target != null)
        {
            _smoothedPosition = target.position + offset;
            _initialized = true;
        }
    }

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (target != null)
            _targetBody = target.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (!_searchAttempted)
            {
                _searchAttempted = true;
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                    SetTarget(player.transform);
            }
            if (target == null)
                return;
        }

        Vector3 desired = target.position + offset;
        if (_targetBody != null)
        {
            float down = Mathf.Min(0f, _targetBody.linearVelocity.y);
            desired.y += Mathf.Clamp(down * fallLookAhead, -maxLookAhead, 0f);
        }

        if (!_initialized)
        {
            _smoothedPosition = desired;
            _initialized = true;
        }

        float ySmooth = verticalSmoothTime;
        if (_targetBody != null && _targetBody.linearVelocity.y < -8f)
            ySmooth = 0.02f;

        _smoothedPosition.x = Mathf.SmoothDamp(_smoothedPosition.x, desired.x, ref _velocity.x, horizontalSmoothTime);
        _smoothedPosition.y = Mathf.SmoothDamp(_smoothedPosition.y, desired.y, ref _velocity.y, ySmooth);
        _smoothedPosition.z = desired.z;

        KeepTargetOnScreen(ref _smoothedPosition);

        Vector3 shake = ScreenShake.Instance != null ? ScreenShake.Instance.CurrentOffset : Vector3.zero;
        transform.position = _smoothedPosition + shake;
    }

    void KeepTargetOnScreen(ref Vector3 cameraPos)
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
        if (_camera == null || !_camera.orthographic)
            return;

        float halfH = _camera.orthographicSize;
        float halfW = halfH * _camera.aspect;
        Vector3 player = target.position;

        float maxOffsetX = halfW * Mathf.Max(0.05f, 1f - viewPadding.x * 2f);
        float maxOffsetY = halfH * Mathf.Max(0.05f, 1f - viewPadding.y * 2f);
        cameraPos.x = Mathf.Clamp(cameraPos.x, player.x - maxOffsetX, player.x + maxOffsetX);
        cameraPos.y = Mathf.Clamp(cameraPos.y, player.y - maxOffsetY, player.y + maxOffsetY);
    }
}
