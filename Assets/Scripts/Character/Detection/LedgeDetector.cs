using UnityEngine;

public class LedgeDetector : MonoBehaviour, ILedgeDetector
{
    [SerializeField] private float _chestHeight = 1.5f;
    [SerializeField] private float _headHeight = 2.2f;
    [SerializeField] private float _forwardDistance = 1f;
    [SerializeField] private float _ledgeForwardOffset = 0.2f;
    [SerializeField] private float _cornerProbeForward = 0.4f;
    [SerializeField] private LayerMask _wallLayer;

    public bool LedgeFound { get; private set; }
    public Vector3 LedgePoint { get; private set; }
    public Vector3 LedgeNormal { get; private set; }

    private float _pauseEndTime;

    private void FixedUpdate()
    {
        if (Time.time < _pauseEndTime)
        {
            ResetDetection();
            return;
        }

        Vector3 chestOrigin = transform.position + Vector3.up * _chestHeight;
        
        bool wallHit = Physics.Raycast(chestOrigin, transform.forward, out RaycastHit forwardHit, _forwardDistance, _wallLayer);

        if (wallHit)
        {
            Vector3 downOrigin = forwardHit.point + (transform.forward * _ledgeForwardOffset) + (Vector3.up * (_headHeight - _chestHeight));
            
            bool topHit = Physics.Raycast(downOrigin, Vector3.down, out RaycastHit downHit, _headHeight - _chestHeight + 0.2f, _wallLayer);

            if (topHit)
            {
                LedgeFound = true;
                LedgePoint = downHit.point;
                LedgeNormal = forwardHit.normal;
                return;
            }
        }

        ResetDetection();
    }

    public void PauseDetection(float duration)
    {
        _pauseEndTime = Time.time + duration;
        ResetDetection();
    }

    public bool TryGetLedgeTop(Vector3 approxTop, out Vector3 ledgeTop)
    {
        Vector3 origin = approxTop + Vector3.up * 0.3f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 0.5f, _wallLayer))
        {
            ledgeTop = hit.point;
            return true;
        }

        ledgeTop = approxTop;
        return false;
    }

    public bool TryGetOuterCorner(Vector3 cornerApprox, Vector3 worldTravel, out Vector3 ledgePoint, out Vector3 ledgeNormal)
    {
        ledgePoint = cornerApprox;
        ledgeNormal = Vector3.zero;

        Vector3 probeOrigin = cornerApprox + worldTravel * _cornerProbeForward - Vector3.up * (_headHeight - _chestHeight);

        if (!Physics.Raycast(probeOrigin, -worldTravel, out RaycastHit wallHit, _cornerProbeForward + _forwardDistance, _wallLayer))
            return false;

        Vector3 approxTop = new Vector3(wallHit.point.x, cornerApprox.y, wallHit.point.z);

        if (!TryGetLedgeTop(approxTop, out Vector3 top))
            return false;

        ledgePoint = top;
        ledgeNormal = wallHit.normal;
        return true;
    }

    private void ResetDetection()
    {
        LedgeFound = false;
        LedgePoint = Vector3.zero;
        LedgeNormal = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 chestOrigin = transform.position + Vector3.up * _chestHeight;
        
        Gizmos.color = LedgeFound ? Color.green : Color.red;
        Gizmos.DrawLine(chestOrigin, chestOrigin + transform.forward * _forwardDistance);

        if (Physics.Raycast(chestOrigin, transform.forward, out RaycastHit hit, _forwardDistance, _wallLayer))
        {
            Vector3 downOrigin = hit.point + (transform.forward * _ledgeForwardOffset) + (Vector3.up * (_headHeight - _chestHeight));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(downOrigin, downOrigin + Vector3.down * (_headHeight - _chestHeight + 0.2f));
            
            if (LedgeFound)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(LedgePoint, 0.1f);
            }
        }
    }
}