using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerPowerupInventory))]
public class PlayerController : MonoBehaviour
{
    public enum ControlMode
    {
        Normal,
        Ship
    }

    [Header("Obese movement")]
    [SerializeField] float moveAccel = 35f;
    [SerializeField] float maxSpeed = 6.5f;
    [SerializeField] float jumpForce = 11f;
    [SerializeField] float gravityScale = 3.2f;
    [SerializeField] float mass = 2.5f;

    [Header("Ship")]
    [SerializeField] float shipSpeed = 7f;
    [SerializeField] float shipFlapForce = 8f;

    [Header("Ground")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.12f;
    [SerializeField] LayerMask groundMask;

    Rigidbody2D _rb;
    PlayerHealth _health;
    PlayerPowerupInventory _inventory;
    GrappleController _grapple;
    PlayerGun _gun;
    SpriteRenderer _sprite;

    InputAction _moveAction;
    InputAction _jumpAction;
    InputAction _powerupAction;

    float _facingSign = 1f;
    bool _jumpBuffered;
    bool _powerupPressed;
    Vector2 _pendingForce;
    readonly Collider2D[] _groundHits = new Collider2D[8];

    public float FacingSign => _facingSign;
    public bool IsGrounded { get; private set; }
    public ControlMode Mode { get; private set; } = ControlMode.Normal;
    public Rigidbody2D Body => _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _health = GetComponent<PlayerHealth>();
        _inventory = GetComponent<PlayerPowerupInventory>();
        _grapple = GetComponent<GrappleController>();
        _gun = GetComponent<PlayerGun>();
        _sprite = GetComponent<SpriteRenderer>();

        _rb.mass = mass;
        _rb.gravityScale = gravityScale;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Ground");

        if (groundCheck == null)
        {
            var check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0f, -0.58f, 0f);
            groundCheck = check.transform;
        }

        SetupInput();
    }

    void SetupInput()
    {
        var asset = InputSystem.actions;
        if (asset != null)
        {
            _moveAction = asset.FindAction("Player/Move", false);
            _jumpAction = asset.FindAction("Player/Jump", false);
            _powerupAction = asset.FindAction("Player/Attack", false);
        }

        if (_moveAction == null)
        {
            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        if (_jumpAction == null)
        {
            _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/w");
        }

        if (_powerupAction == null)
        {
            _powerupAction = new InputAction("Powerup", InputActionType.Button, "<Keyboard>/space");
        }

        _moveAction.Enable();
        _jumpAction.Enable();
        _powerupAction.Enable();
    }

    void OnEnable()
    {
        _moveAction?.Enable();
        _jumpAction?.Enable();
        _powerupAction?.Enable();
    }

    void OnDisable()
    {
        _moveAction?.Disable();
        _jumpAction?.Disable();
        _powerupAction?.Disable();
    }

    void Update()
    {
        if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
            _jumpBuffered = true;
        if (TouchControls.Instance != null && TouchControls.Instance.ConsumeJump())
            _jumpBuffered = true;

        if (_powerupAction != null && _powerupAction.WasPressedThisFrame())
            _powerupPressed = true;
        if (TouchControls.Instance != null && TouchControls.Instance.ConsumeAction())
            _powerupPressed = true;

        SyncModeFromInventory();
    }

    void FixedUpdate()
    {
        IsGrounded = CheckGrounded();

        if (Mode == ControlMode.Ship)
            TickShip();
        else
            TickNormal();

        HandlePowerupPress();
        _jumpBuffered = false;
        _powerupPressed = false;
        _pendingForce = Vector2.zero;
    }

    void SyncModeFromInventory()
    {
        Mode = _inventory.InShipMode ? ControlMode.Ship : ControlMode.Normal;
        if (Mode != ControlMode.Ship)
            _rb.gravityScale = gravityScale;
    }

    void TickNormal()
    {
        Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        float x = move.x;
        if (TouchControls.Instance != null)
            x = Mathf.Clamp(x + TouchControls.Instance.MoveX, -1f, 1f);

        if (Mathf.Abs(x) > 0.01f)
            _facingSign = Mathf.Sign(x);

        Vector2 wind = WindBoost();

        if (_grapple != null && _grapple.IsAttached)
        {
            _rb.AddForce(new Vector2(x * moveAccel * 0.35f, 0f) + _pendingForce);
        }
        else
        {
            float targetVx = x * maxSpeed + wind.x;
            float newVx = Mathf.MoveTowards(_rb.linearVelocity.x, targetVx, moveAccel * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(newVx, _rb.linearVelocity.y + wind.y * Time.fixedDeltaTime);

            if (_jumpBuffered && IsGrounded)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                IsGrounded = false;
            }
        }

        FaceSprite();
    }

    void TickShip()
    {
        float dir = _inventory.ShipDirection;
        _facingSign = dir;
        Vector2 wind = WindBoost();
        _rb.gravityScale = gravityScale * 0.85f;
        _rb.linearVelocity = new Vector2(dir * shipSpeed + wind.x, _rb.linearVelocity.y + wind.y * Time.fixedDeltaTime);

        if (_powerupPressed)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, shipFlapForce);

        FaceSprite();
    }

    void HandlePowerupPress()
    {
        if (!_powerupPressed)
            return;

        // Ship flap consumed in TickShip.
        if (Mode == ControlMode.Ship)
            return;

        if (_inventory.HasGrapple && _grapple != null)
        {
            _grapple.ToggleGrapple();
            return;
        }

        if (_inventory.HasGun && _gun != null)
            _gun.TryFire();
    }

    bool CheckGrounded()
    {
        if (groundCheck == null)
            return false;

        var filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true
        };
        filter.SetLayerMask(groundMask);

        int count = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, filter, _groundHits);
        for (int i = 0; i < count; i++)
        {
            var hit = _groundHits[i];
            if (hit == null)
                continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;
            return true;
        }

        return false;
    }

    void FaceSprite()
    {
        if (_facingSign == 0f)
            _facingSign = 1f;

        // Keep scale positive. Negative X scale makes URP 2D sprites vanish.
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        if (scale.x < 0.01f)
            scale.x = 0.9f;
        transform.localScale = scale;

        if (_sprite != null)
            _sprite.flipX = _facingSign < 0f;
    }

    public void TeleportTo(Vector3 position)
    {
        _rb.position = position;
        _rb.linearVelocity = Vector2.zero;
        transform.position = position;
    }

    public void RespawnAt(Vector3 position)
    {
        if (_grapple != null)
            _grapple.ForceRelease();

        if (_inventory != null)
            _inventory.ClearAll();

        _rb.gravityScale = gravityScale;
        TeleportTo(position);
        _health.ClearDeathLock();
    }

    public void KillFromTimer()
    {
        _health.Die();
    }

    public void ApplyExternalVelocity(Vector2 velocity)
    {
        _rb.linearVelocity = velocity;
    }

    public void AddExternalForce(Vector2 force)
    {
        _pendingForce += force;
    }

    Vector2 WindBoost()
    {
        float m = _rb != null ? Mathf.Max(0.01f, _rb.mass) : 1f;
        return _pendingForce / m;
    }
}
