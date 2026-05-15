using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementSystem : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _groundSnapOffset = 0.5f;

    private Rigidbody _rb;
    private IGroundDetector _groundDetector;
    private IWallDetector _wallDetector;
    private IInputProvider _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _groundDetector = GetComponent<IGroundDetector>();
        _wallDetector = GetComponent<IWallDetector>();
        _input = _inputReader;
    }

    public void HandleGroundedMovement()
    {
        _rb.useGravity = false;
        
        Vector3 movementDirection = GetMovementDirection();
        Vector3 projected = Vector3.ProjectOnPlane(movementDirection, _groundDetector.GroundNormal);
        
        _rb.linearVelocity = projected * _moveSpeed;
        
        SnapToGround();
        RotateCharacter(movementDirection);
    }

    public void HandleAirborneMovement()
    {
        _rb.useGravity = true;

        Vector3 movementDirection = GetMovementDirection();

        if (_wallDetector.IsWallFront && Vector3.Dot(movementDirection, _wallDetector.FrontWallNormal) < 0f)
        {
            movementDirection = Vector3.ProjectOnPlane(movementDirection, _wallDetector.FrontWallNormal);
        }

        Vector3 velocity = movementDirection * _moveSpeed;
        velocity.y = _rb.linearVelocity.y;

        _rb.linearVelocity = velocity;

        RotateCharacter(movementDirection);
    }

    private Vector3 GetMovementDirection()
    {
        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return forward * _input.MoveInput.y + right * _input.MoveInput.x;
    }

    private void SnapToGround()
    {
        if (_rb.linearVelocity.y > 0.1f) return;
        if (_groundDetector.SlopeAngle > 5f) return;

        Vector3 targetPosition = _rb.position;
        targetPosition.y = _groundDetector.GroundPoint.y + _groundSnapOffset;

        if (Mathf.Abs(_rb.position.y - targetPosition.y) > 0.01f)
        {
            _rb.MovePosition(targetPosition);
        }
    }

    private void RotateCharacter(Vector3 movementDirection)
    {
        if (movementDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
    }
}