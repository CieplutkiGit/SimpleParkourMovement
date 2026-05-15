using UnityEngine;

public interface IGroundDetector
{
    bool IsGrounded { get; }
    Vector3 GroundNormal { get; }
    Vector3 GroundPoint { get; }
    float SlopeAngle { get; }
}