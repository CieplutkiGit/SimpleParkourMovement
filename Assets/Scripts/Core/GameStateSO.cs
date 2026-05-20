using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameStateSO")]
public class GameStateSO : ScriptableObject
{
    public event Action<GameState> OnStateChanged;

    public GameState CurrentState { get; private set; }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}