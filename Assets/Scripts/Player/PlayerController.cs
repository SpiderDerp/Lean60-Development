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
    [SerializeField] float groundCheckRadius = 0.15f;
    [SerializeField] LayerMask groundMask = ~0;

    Rigidbody2D _rb;
    PlayerHealth _health;
    PlayerPowerupInventory _inventory;
    GrappleController _grapple;
    PlayerGun _gun;

    InputAction _moveAction;
    InputAction _jumpAction;
    InputAction _powerupAction;

    float _facingSign = 1f;
    bool _jumpBuffered;
    bool _powerupPressed;

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

        _rb.mass = mass;
        _rb.gravityScale = gravityScale;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (groundCheck == null)
        {
            var check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0f, -0.55f, 0f);
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

        if (_powerupAction != null && _powerupAction.WasPressedThisFrame())
            _powerupPressed = true;

        SyncModeFromInventory();
    }

    void FixedUpdate()
    {
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        if (Mode == ControlMode.Ship)
            TickShip();
        else
            TickNormal();

        HandlePowerupPress();
        _jumpBuffered = false;
        _powerupPressed = false;
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

        if (Mathf.Abs(x) > 0.01f)
            _facingSign = Mathf.Sign(x);

        if (_grapple != null && _grapple.IsAttached)
        {
            // Allow slight air influence while swinging.
            _rb.AddForce(new Vector2(x * moveAccel * 0.35f, 0f));
        }
        else
        {
            float targetVx = x * maxSpeed;
            float newVx = Mathf.MoveTowards(_rb.linearVelocity.x, targetVx, moveAccel * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(newVx, _rb.linearVelocity.y);

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
        _rb.gravityScale = gravityScale * 0.85f;
        _rb.linearVelocity = new Vector2(dir * shipSpeed, _rb.linearVelocity.y);

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

    void FaceSprite()
    {
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * _facingSign;
        transform.localScale = scale;
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

        // Leave ship mid-flight on death, but keep inventory flags for pickups.
        if (_inventory.InShipMode)
            _inventory.ExitShipMode();

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
        _rb.AddForce(force);
    }
}
