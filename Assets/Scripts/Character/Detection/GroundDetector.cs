using System;
using UnityEngine;

public class GroundDetector : MonoBehaviour, IGroundDetector
{
    [Header("Detection")]
    [SerializeField] private float _sphereRadius = 0.4f;
    [SerializeField] private float _checkDistance = 0.3f;
    [SerializeField] private float _originOffset = 0.5f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Slope")]
    [SerializeField] private float _maxSlopeAngle = 50f;

    public event Action OnLanded;
    public event Action OnLeftGround;

    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; }
    public float SlopeAngle { get; private set; }

    private bool _wasGroundedLastFrame;

    private void FixedUpdate()
    {
        DetectGround();
        FireEventsIfStateChanged();
    }

    private void DetectGround()
    {
        Vector3 origin = transform.position + Vector3.up * _originOffset;
        Vector3 direction = Vector3.down;
        float distance = _originOffset + _checkDistance;

        bool hitGround = Physics.SphereCast(
            origin,
            _sphereRadius,
            direction,
            out RaycastHit hit,
            distance,
            _groundLayer
        );

        if (hitGround)
        {
            GroundNormal = hit.normal;
            SlopeAngle = Vector3.Angle(Vector3.up, GroundNormal);
            IsGrounded = SlopeAngle <= _maxSlopeAngle;
        }
        else
        {
            IsGrounded = false;
            GroundNormal = Vector3.up;
            SlopeAngle = 0f;
        }
    }

    private void FireEventsIfStateChanged()
    {
        if (IsGrounded && !_wasGroundedLastFrame) OnLanded?.Invoke();
        else if (!IsGrounded && _wasGroundedLastFrame) OnLeftGround?.Invoke();

        _wasGroundedLastFrame = IsGrounded;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * _originOffset;
        Vector3 end = origin + Vector3.down * (_originOffset + _checkDistance);

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(end, _sphereRadius);
        Gizmos.DrawLine(origin, end);
    }
}