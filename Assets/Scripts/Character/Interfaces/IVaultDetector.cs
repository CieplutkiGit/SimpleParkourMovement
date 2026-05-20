using UnityEngine;

public interface IVaultDetector
{
    bool VaultFound { get; }
    Vector3 ObstacleTop { get; }
    Vector3 VaultTarget { get; }
}
