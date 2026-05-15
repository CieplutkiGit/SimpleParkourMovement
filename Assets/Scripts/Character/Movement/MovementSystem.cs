using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 movementDirection = GetMovementDirection();
        ApplyMovement(movementDirection);
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

        return forward * _inputReader.MoveInput.y + right * _inputReader.MoveInput.x;
    }

    private void ApplyMovement(Vector3 movementDirection)
    {
        Vector3 velocity = movementDirection * _moveSpeed;
        velocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = velocity;
    }

    private void RotateCharacter(Vector3 movementDirection)
    {
        if (movementDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
    }
}