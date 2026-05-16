using UnityEngine;

public interface ILedgeDetector
{
    bool LedgeFound { get; }
    Vector3 LedgePoint { get; }
    Vector3 LedgeNormal { get; }
    void PauseDetection(float duration);
    bool TryGetLedgeTop(Vector3 approxTop, out Vector3 ledgeTop);
}