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

    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; }
    public float SlopeAngle { get; private set; }

    public Vector3 GroundPoint { get; private set; }

    private void FixedUpdate()
    {
        DetectGround();
    }

    private void DetectGround()
    {
        Vector3 origin = transform.position + Vector3.up * _originOffset;
        Vector3 direction = Vector3.down;
        float distance = _originOffset + _checkDistance;

        // Sphere cast for grounded detection (forgiving)
        bool hitGround = Physics.SphereCast(
            origin, _sphereRadius, direction, out RaycastHit sphereHit, distance, _groundLayer);

        if (hitGround)
        {
            GroundNormal = sphereHit.normal;
            SlopeAngle = Vector3.Angle(Vector3.up, GroundNormal);
            IsGrounded = SlopeAngle <= _maxSlopeAngle;
        }
        else
        {
            IsGrounded = false;
            GroundNormal = Vector3.up;
            SlopeAngle = 0f;
        }

        // Raycast for precise ground point directly beneath the player
        if (Physics.Raycast(origin, direction, out RaycastHit rayHit, distance + 1f, _groundLayer))
        {
            GroundPoint = rayHit.point;
        }
        else
        {
            GroundPoint = transform.position;
        }
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