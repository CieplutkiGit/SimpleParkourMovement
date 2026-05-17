using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LedgeGrabSystem : MonoBehaviour
{
    [SerializeField] private float _heightOffset = 1.2f;
    [SerializeField] private float _forwardOffset = 0.3f;
    [SerializeField] private float _snapDuration = 0.18f;
    [SerializeField] private float _jumpCooldownDuration = 0.3f;
    [SerializeField] private float _strideLength = 0.45f;
    [SerializeField] private float _strideSpeed = 2.2f;

    [SerializeField] private float _climbUpDuration = 0.4f;
    [SerializeField] private float _climbUpHeightOffset = 1.1f;
    [SerializeField] private float _climbUpForwardOffset = 0.5f;

    public float JumpCooldownDuration => _jumpCooldownDuration;
    public float ClimbUpDuration => _climbUpDuration;
    public float SnapDuration => _snapDuration;
    public float StrideLength => _strideLength;
    public float StrideSpeed => _strideSpeed;

    public Vector3 CachedLedgePoint { get; private set; }
    public Vector3 CachedLedgeNormal { get; private set; }

    public Vector3 RightHandAnchor { get; private set; }
    public Vector3 LeftHandAnchor { get; private set; }

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void BeginGrab(Vector3 ledgePoint, Vector3 ledgeNormal)
    {
        CachedLedgePoint = ledgePoint;
        CachedLedgeNormal = ledgeNormal;

        Vector3 d = GetShimmyDirection(ledgeNormal);
        float half = _strideLength * 0.5f;
        RightHandAnchor = ledgePoint + d * half;
        LeftHandAnchor = ledgePoint - d * half;

        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    public void ReleaseLedge()
    {
        _rb.isKinematic = false;
    }

    public Vector3 GetHangTarget(Vector3 ledgePoint, Vector3 ledgeNormal)
    {
        return ledgePoint - (Vector3.up * _heightOffset) + (ledgeNormal * _forwardOffset);
    }

    public Quaternion GetHangRotation(Vector3 ledgeNormal)
    {
        return Quaternion.LookRotation(-ledgeNormal);
    }

    public Vector3 GetClimbUpTarget(Vector3 ledgePoint, Vector3 ledgeNormal)
    {
        return ledgePoint + (Vector3.up * _climbUpHeightOffset) + (-ledgeNormal * _climbUpForwardOffset);
    }

    public Vector3 GetShimmyDirection(Vector3 ledgeNormal)
    {
        return Vector3.Cross(ledgeNormal, Vector3.up).normalized;
    }

    public void SetCachedLedge(Vector3 ledgePoint, Vector3 ledgeNormal)
    {
        CachedLedgePoint = ledgePoint;
        CachedLedgeNormal = ledgeNormal;
    }

    public void SetHandAnchors(Vector3 rightAnchor, Vector3 leftAnchor)
    {
        RightHandAnchor = rightAnchor;
        LeftHandAnchor = leftAnchor;
    }
}