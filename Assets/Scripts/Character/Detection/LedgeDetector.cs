using UnityEngine;

public class LedgeDetector : MonoBehaviour, ILedgeDetector
{
    [SerializeField] private float _chestHeight = 1.5f;
    [SerializeField] private float _headHeight = 2.2f;
    [SerializeField] private float _forwardDistance = 1f;
    [SerializeField] private float _ledgeForwardOffset = 0.2f;
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