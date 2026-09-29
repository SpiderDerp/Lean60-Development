using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GrappleController : MonoBehaviour
{
    [SerializeField] float maxDistance = 14f;
    [SerializeField] LayerMask grappleMask;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] float ropeWidth = 0.05f;

    DistanceJoint2D _joint;
    PlayerController _player;
    Rigidbody2D _anchorBody;
    Collider2D[] _ownColliders;

    public bool IsAttached => _joint != null && _joint.enabled && _anchorBody != null;

    void Awake()
    {
        if (grappleMask.value == 0)
            grappleMask = LayerMask.GetMask("Ground");

        _ownColliders = GetComponentsInChildren<Collider2D>();
        _joint = gameObject.GetComponent<DistanceJoint2D>();
        if (_joint == null)
            _joint = gameObject.AddComponent<DistanceJoint2D>();

        _joint.enabled = false;
        _joint.autoConfigureDistance = false;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.maxDistanceOnly = false;
        _joint.enableCollision = true;

        if (lineRenderer == null)
            lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        var shader = Shader.Find("Lean60/SpriteUnlitDoubleSided");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineRenderer.material = new Material(shader);
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
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

        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, _anchorBody.position);
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
        if (Player == null)
            return;

        float facing = Player.FacingSign;
        Vector2 origin = (Vector2)transform.position + new Vector2(facing * 0.6f, 0.1f);
        Vector2[] dirs =
        {
            new Vector2(facing, 0.15f).normalized,
            new Vector2(facing, 0.65f).normalized,
            new Vector2(facing, 1.2f).normalized,
            new Vector2(facing * 0.35f, 1f).normalized
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            if (TryRay(origin, dirs[i]))
                return;
        }
    }

    bool TryRay(Vector2 origin, Vector2 dir)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, maxDistance, grappleMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;
            if (IsOwnCollider(hit.collider))
                continue;
            if (hit.collider.CompareTag("Player"))
                continue;

            AttachAt(hit.point);
            return true;
        }

        return false;
    }

    bool IsOwnCollider(Collider2D collider)
    {
        if (collider.transform == transform || collider.transform.IsChildOf(transform))
            return true;
        if (_ownColliders == null)
            return false;
        for (int i = 0; i < _ownColliders.Length; i++)
        {
            if (_ownColliders[i] == collider)
                return true;
        }

        return false;
    }

    void AttachAt(Vector2 worldPoint)
    {
        if (_anchorBody == null)
        {
            var anchorGo = new GameObject("GrappleAnchor");
            _anchorBody = anchorGo.AddComponent<Rigidbody2D>();
        }

        _anchorBody.bodyType = RigidbodyType2D.Kinematic;
        _anchorBody.gravityScale = 0f;
        _anchorBody.simulated = true;
        _anchorBody.position = worldPoint;
        _anchorBody.transform.position = worldPoint;

        _joint.connectedBody = _anchorBody;
        _joint.connectedAnchor = Vector2.zero;
        _joint.distance = Vector2.Distance((Vector2)transform.position, worldPoint);
        _joint.enabled = true;
    }

    public void ForceRelease()
    {
        if (_joint != null)
        {
            _joint.enabled = false;
            _joint.connectedBody = null;
        }

        if (_anchorBody != null)
            Destroy(_anchorBody.gameObject);

        _anchorBody = null;
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    void OnDisable()
    {
        ForceRelease();
    }
}
