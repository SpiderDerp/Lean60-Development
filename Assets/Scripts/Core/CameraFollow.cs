using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] float smoothTime = 0.15f;

    Vector3 _velocity;
    Vector3 _smoothedPosition;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        if (target != null)
            _smoothedPosition = target.position + offset;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + offset;
        _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, desired, ref _velocity, smoothTime);
        Vector3 shake = ScreenShake.Instance != null ? ScreenShake.Instance.CurrentOffset : Vector3.zero;
        transform.position = _smoothedPosition + shake;
    }
}
