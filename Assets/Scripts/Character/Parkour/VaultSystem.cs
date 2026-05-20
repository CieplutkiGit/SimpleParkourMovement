using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VaultSystem : MonoBehaviour
{
    [SerializeField] private float _vaultMatchStart = 0.1f;
    [SerializeField] private float _vaultMatchEnd = 0.85f;

    public float VaultMatchStart => _vaultMatchStart;
    public float VaultMatchEnd => _vaultMatchEnd;

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
}
