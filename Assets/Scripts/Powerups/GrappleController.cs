using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GrappleController : MonoBehaviour
{
    [SerializeField] float maxDistance = 12f;
    [SerializeField] LayerMask grappleMask = ~0;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] float ropeWidth = 0.05f;

    DistanceJoint2D _joint;
    PlayerController _player;
    Transform _anchor;

    public bool IsAttached => _joint != null && _joint.enabled;

    void Awake()
    {
        _joint = gameObject.GetComponent<DistanceJoint2D>();
        if (_joint == null)
            _joint = gameObject.AddComponent<DistanceJoint2D>();

        _joint.enabled = false;
        _joint.autoConfigureDistance = false;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.maxDistanceOnly = false;

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
        }

        lineRenderer.enabled = false;
    }

    PlayerController Player
    {
        get
        {
            if (_player == null)
                _player = GetComponent<PlayerController>();
            return _player;
        }
    }

    void LateUpdate()
    {
        if (!IsAttached)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, _joint.connectedAnchor);
    }

    public void ToggleGrapple()
    {
        if (IsAttached)
            ForceRelease();
        else
            TryAttach();
    }

    void TryAttach()
    {
        Vector2 dir = new Vector2(Player.FacingSign, 0.2f).normalized;
        Vector2 origin = (Vector2)transform.position + dir * 0.55f;

        if (!TryRay(origin, dir) && !TryRay(origin, new Vector2(Player.FacingSign, 0.65f).normalized))
            return;
    }

    bool TryRay(Vector2 origin, Vector2 dir)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, maxDistance, grappleMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;
            if (hit.collider.transform == transform || hit.collider.CompareTag("Player"))
                continue;

            _joint.connectedBody = null;
            _joint.connectedAnchor = hit.point;
            _joint.distance = Vector2.Distance((Vector2)transform.position, hit.point);
            _joint.enableCollision = true;
            _joint.enabled = true;
            _anchor = hit.transform;
            return true;
        }

        return false;
    }

    public void ForceRelease()
    {
        if (_joint != null)
            _joint.enabled = false;
        _anchor = null;
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
}
