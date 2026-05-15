using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(IGroundDetector))]
public class JumpSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader _inputReader;

    [Header("Jump Physics")]
    [SerializeField] private float _jumpForce = 6f;
    [SerializeField] private float _jumpCutoffMultiplier = 0.4f;
    [SerializeField] private float _fallGravityMultiplier = 2.5f;

    [Header("Game Feel (Forgiveness)")]
    [SerializeField] private float _coyoteTime = 0.15f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [SerializeField] private float _jumpCooldown = 0.2f;

    private Rigidbody _rb;
    private IGroundDetector _groundDetector;
    private IInputProvider _input;

    
    private float _lastGroundedTime = -100f;
    private float _lastJumpPressedTime = -100f;
    private float _lastJumpTime = -100f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _groundDetector = GetComponent<IGroundDetector>();
        _input = _inputReader;
    }

    private void OnEnable()
    {
        _input.OnJumpStarted += HandleJumpStarted;
        _input.OnJumpCancelled += HandleJumpCancelled;
    }

    private void OnDisable()
    {
        _input.OnJumpStarted -= HandleJumpStarted;
        _input.OnJumpCancelled -= HandleJumpCancelled;
    }

    private void Update()
    {
        
        if (_groundDetector.IsGrounded)
        {
            _lastGroundedTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        ApplyFallGravity();

        if (CanJump())
        {
            PerformJump();
        }
    }

    private void HandleJumpStarted()
    {
        _lastJumpPressedTime = Time.time; 
    }

    private void HandleJumpCancelled()
    {
        
        if (!_groundDetector.IsGrounded && _rb.linearVelocity.y > 0f)
        {
            Vector3 velocity = _rb.linearVelocity;
            velocity.y *= _jumpCutoffMultiplier;
            _rb.linearVelocity = velocity;
        }
    }

    private void ApplyFallGravity()
    {
        if (!_groundDetector.IsGrounded && _rb.linearVelocity.y < 0f)
        {
            _rb.AddForce(Physics.gravity * (_fallGravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    private bool CanJump()
    {
        bool isJumpBuffered = Time.time - _lastJumpPressedTime <= _jumpBufferTime;
        bool isCoyoteTimeValid = Time.time - _lastGroundedTime <= _coyoteTime;
        bool isCooldownFinished = Time.time - _lastJumpTime >= _jumpCooldown;

        return isJumpBuffered && isCoyoteTimeValid && isCooldownFinished;
    }

    private void PerformJump()
    {
        _lastJumpTime = Time.time;
        
        _lastJumpPressedTime = -100f;
        _lastGroundedTime = -100f;

        Vector3 velocity = _rb.linearVelocity;
        velocity.y = _jumpForce;
        _rb.linearVelocity = velocity;
    }
}