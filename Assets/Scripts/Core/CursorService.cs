using UnityEngine;

public class CursorService : MonoBehaviour
{
    [SerializeField] private GameStateSO _gameState;

    private void OnEnable()
    {
        _gameState.OnStateChanged += HandleStateChanged;
        ApplyCursorState(_gameState.CurrentState);
    }

    private void OnDisable()
    {
        _gameState.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        ApplyCursorState(newState);
    }

    private void ApplyCursorState(GameState state)
    {
        switch (state)
        {
            case GameState.Gameplay:
            case GameState.Cutscene:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case GameState.Menu:
            case GameState.Paused:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }
}