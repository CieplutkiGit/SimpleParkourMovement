using System;
using UnityEngine;

public interface IInputProvider
{
    event Action OnJumpStarted;
    event Action OnJumpCancelled;
    event Action OnSprintStarted;
    event Action OnSprintCancelled;

    Vector2 MoveInput { get; }
    Vector2 LookInput { get; }
    bool IsSprintHeld { get; }
}