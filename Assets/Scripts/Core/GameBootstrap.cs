using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameStateSO _gameState;
    [SerializeField] private GameState _initialState = GameState.Gameplay;

    private void Start()
    {
        _gameState.ChangeState(_initialState);
    }
}