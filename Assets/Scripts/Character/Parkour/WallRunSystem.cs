using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WallRunSystem : MonoBehaviour
{
    [Header("Run Along")]
    [SerializeField] private float _wallRunSpeed = 6f;
    [SerializeField] private float _wallRunRiseSpeed = 3f;
    [SerializeField] private float _wallRunFallSpeed = 4f;
    [SerializeField] private float _wallStickForce = 8f;
    [SerializeField] private float _wallRunDuration = 1.5f;
    [SerializeField] private float _minEntrySpeed = 3f;

    [Header("Run Up")]
    [SerializeField] private float _wallRunUpSpeed = 6f;
    [SerializeField] private float _wallRunUpDuration = 0.6f;

    public float WallRunDuration => _wallRunDuration;
    public float WallRunUpDuration => _wallRunUpDuration;
    public float MinEntrySpeed => _minEntrySpeed;
    public bool WallRunAvailable { get; private set; } = true;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void ConsumeWallRun()
    {
        WallRunAvailable = false;
    }

    public void ResetWallRun()
    {
        WallRunAvailable = true;
    }

    public void ApplyWallRun(Vector3 wallNormal, float t)
    {
        Vector3 along = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(along, transform.forward) < 0f)
            along = -along;

        Vector3 velocity = along * _wallRunSpeed;
        velocity.y = Mathf.Lerp(_wallRunRiseSpeed, -_wallRunFallSpeed, t);
        _rb.linearVelocity = velocity;

        _rb.AddForce(-wallNormal * _wallStickForce, ForceMode.Acceleration);
    }

    public void ApplyWallRunUp(Vector3 wallNormal, float t)
    {
        Vector3 velocity = _rb.linearVelocity;
        velocity.y = _wallRunUpSpeed * (1f - t);
        _rb.linearVelocity = velocity;

        _rb.AddForce(-wallNormal * _wallStickForce, ForceMode.Acceleration);
    }
}
