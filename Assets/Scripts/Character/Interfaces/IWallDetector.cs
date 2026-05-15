using UnityEngine;

public interface IWallDetector
{
    bool IsWallLeft { get; }
    bool IsWallRight { get; }
    bool IsWallFront { get; }
    Vector3 WallNormal { get; }
    Vector3 WallPoint { get; }
    Vector3 FrontWallNormal { get; }
    Vector3 FrontWallPoint { get; }
}