using UnityEngine;

public interface IWallDetector
{
    bool IsWallLeft { get; }
    bool IsWallRight { get; }
    Vector3 WallNormal { get; }
    Vector3 WallPoint { get; }
}