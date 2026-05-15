using UnityEngine;

public interface IGroundDetector
{
    bool IsGrounded { get; }
    Vector3 GroundNormal { get; }
    float SlopeAngle { get; }
}