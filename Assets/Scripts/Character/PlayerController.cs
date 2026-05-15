using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _cameraTransform;

    [SerializeField] private float _moveSpeed = 5f;

    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
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
    private void FixedUpdate()
    {
        Vector3 movementDirection = GetMovementDirection();
        Vector3 velocity = movementDirection * _moveSpeed;
        velocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = velocity;
    }   
}
