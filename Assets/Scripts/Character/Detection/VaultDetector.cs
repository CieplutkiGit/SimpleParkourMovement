using UnityEngine;

public class VaultDetector : MonoBehaviour, IVaultDetector
{
    [SerializeField] private float _lowCheckHeight = 0.4f;
    [SerializeField] private float _highCheckHeight = 1.3f;
    [SerializeField] private float _checkDistance = 0.8f;
    [SerializeField] private float _topProbeOffset = 0.1f;
    [SerializeField] private float _maxVaultHeight = 1.2f;
    [SerializeField] private float _landingDistance = 1.4f;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private LayerMask _groundLayer;

    public bool VaultFound { get; private set; }
    public Vector3 ObstacleTop { get; private set; }
    public Vector3 VaultTarget { get; private set; }

    private void FixedUpdate()
    {
        Vector3 lowOrigin = transform.position + Vector3.up * _lowCheckHeight;
        Vector3 highOrigin = transform.position + Vector3.up * _highCheckHeight;

        bool lowHit = Physics.Raycast(lowOrigin, transform.forward, out RaycastHit frontHit, _checkDistance, _obstacleLayer);
        bool highHit = Physics.Raycast(highOrigin, transform.forward, _checkDistance, _obstacleLayer);

        if (lowHit && !highHit)
        {
            Vector3 topOrigin = frontHit.point + (transform.forward * _topProbeOffset) + (Vector3.up * _highCheckHeight);

            if (Physics.Raycast(topOrigin, Vector3.down, out RaycastHit topHit, _highCheckHeight, _obstacleLayer))
            {
                float height = topHit.point.y - transform.position.y;

                if (height <= _maxVaultHeight)
                {
                    Vector3 landOrigin = frontHit.point + (transform.forward * _landingDistance) + (Vector3.up * _highCheckHeight);

                    if (Physics.Raycast(landOrigin, Vector3.down, out RaycastHit landHit, _highCheckHeight + 1f, _groundLayer))
                    {
                        VaultFound = true;
                        ObstacleTop = topHit.point;
                        VaultTarget = landHit.point;
                        return;
                    }
                }
            }
        }

        VaultFound = false;
        ObstacleTop = Vector3.zero;
        VaultTarget = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 lowOrigin = transform.position + Vector3.up * _lowCheckHeight;
        Vector3 highOrigin = transform.position + Vector3.up * _highCheckHeight;

        Gizmos.color = VaultFound ? Color.green : Color.red;
        Gizmos.DrawLine(lowOrigin, lowOrigin + transform.forward * _checkDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(highOrigin, highOrigin + transform.forward * _checkDistance);

        if (VaultFound)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(ObstacleTop, 0.1f);
            Gizmos.DrawSphere(VaultTarget, 0.1f);
        }
    }
}
