using UnityEngine;

public class WallDetector : MonoBehaviour, IWallDetector
{
    [SerializeField] private float _rayLength = 1f;
    [SerializeField] private float _originOffset = 1f;
    [SerializeField] private LayerMask _wallLayer;

    public bool IsWallLeft { get; private set; }
    public bool IsWallRight { get; private set; }
    public Vector3 WallNormal { get; private set; }
    public Vector3 WallPoint { get; private set; }

    private void FixedUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * _originOffset;
        
        bool hitLeft = Physics.Raycast(origin, -transform.right, out RaycastHit leftHit, _rayLength, _wallLayer);
        bool hitRight = Physics.Raycast(origin, transform.right, out RaycastHit rightHit, _rayLength, _wallLayer);

        IsWallLeft = hitLeft;
        IsWallRight = hitRight;

        if (IsWallLeft)
        {
            WallNormal = leftHit.normal;
            WallPoint = leftHit.point;
        }
        else if (IsWallRight)
        {
            WallNormal = rightHit.normal;
            WallPoint = rightHit.point;
        }
        else
        {
            WallNormal = Vector3.zero;
            WallPoint = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * _originOffset;
        
        Gizmos.color = IsWallLeft ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin - transform.right * _rayLength);
        
        Gizmos.color = IsWallRight ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin + transform.right * _rayLength);
    }
}