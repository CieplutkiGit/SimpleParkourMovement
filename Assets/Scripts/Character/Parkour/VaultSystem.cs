using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VaultSystem : MonoBehaviour
{
    [SerializeField] private float _vaultDuration = 0.45f;
    [SerializeField] private float _arcClearance = 0.3f;

    public float VaultDuration => _vaultDuration;
    public float ArcClearance => _arcClearance;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void BeginVault()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }

    public void ReleaseVault()
    {
        _rb.isKinematic = false;
    }

    public Vector3 GetVaultPoint(Vector3 start, Vector3 target, float apexY, float t)
    {
        Vector3 pos = Vector3.Lerp(start, target, t);
        pos.y += (apexY - Mathf.Max(start.y, target.y)) * Mathf.Sin(t * Mathf.PI);
        return pos;
    }
}
