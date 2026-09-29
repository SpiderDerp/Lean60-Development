using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerController))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    enum AnimState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Land
    }

    [SerializeField] Sprite[] idle;
    [SerializeField] Sprite[] run;
    [SerializeField] Sprite[] jump;
    [SerializeField] Sprite[] fall;
    [SerializeField] Sprite[] land;
    [SerializeField] float idleFps = 6f;
    [SerializeField] float runFps = 10f;
    [SerializeField] float jumpFps = 12f;
    [SerializeField] float fallFps = 8f;
    [SerializeField] float landFps = 8f;
    [SerializeField] float runSpeedThreshold = 0.2f;
    [SerializeField] float jumpVelocityThreshold = 0.2f;

    SpriteRenderer _sprite;
    PlayerController _player;
    Rigidbody2D _body;
    AnimState _state;
    int _frame;
    float _elapsed;
    bool _wasGrounded = true;
    bool _landDone;
    bool _fromJump;

    void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _player = GetComponent<PlayerController>();
        _body = GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        AnimState next = PickState();
        if (next != _state)
        {
            _state = next;
            _frame = 0;
            _elapsed = 0f;
        }

        Sprite[] frames = FramesFor(_state);
        if (frames != null && frames.Length > 0)
        {
            float fps = FpsFor(_state);
            _elapsed += Time.deltaTime * Mathf.Max(0.01f, fps);

            if (_state == AnimState.Jump)
            {
                float vy = _body != null ? _body.linearVelocity.y : 0f;
                _frame = vy > jumpVelocityThreshold ? 0 : Mathf.Min(1, frames.Length - 1);
            }
            else if (_state == AnimState.Land)
            {
                _frame = Mathf.Min((int)_elapsed, frames.Length - 1);
                if (_elapsed >= frames.Length)
                    _landDone = true;
            }
            else
            {
                _frame = (int)_elapsed % frames.Length;
            }

            if (frames[_frame] != null)
                _sprite.sprite = frames[_frame];
        }

        if (_player != null)
            _player.ApplyFacingScale(_player.FacingSign, _sprite);
    }

    AnimState PickState()
    {
        bool grounded = _player != null && _player.IsGrounded;
        float vy = _body != null ? _body.linearVelocity.y : 0f;
        float vx = _body != null ? _body.linearVelocity.x : 0f;

        if (!grounded)
        {
            _wasGrounded = false;
            _landDone = false;
            if (vy > jumpVelocityThreshold)
            {
                _fromJump = true;
                return AnimState.Jump;
            }

            return _fromJump ? AnimState.Jump : AnimState.Fall;
        }

        _fromJump = false;
        bool justLanded = !_wasGrounded;
        _wasGrounded = true;

        if (justLanded || (_state == AnimState.Land && !_landDone))
            return AnimState.Land;

        return Mathf.Abs(vx) > runSpeedThreshold ? AnimState.Run : AnimState.Idle;
    }

    Sprite[] FramesFor(AnimState state)
    {
        switch (state)
        {
            case AnimState.Run: return run;
            case AnimState.Jump: return jump;
            case AnimState.Fall: return fall;
            case AnimState.Land: return land;
            default: return idle;
        }
    }

    float FpsFor(AnimState state)
    {
        switch (state)
        {
            case AnimState.Run: return runFps;
            case AnimState.Jump: return jumpFps;
            case AnimState.Fall: return fallFps;
            case AnimState.Land: return landFps;
            default: return idleFps;
        }
    }
}
