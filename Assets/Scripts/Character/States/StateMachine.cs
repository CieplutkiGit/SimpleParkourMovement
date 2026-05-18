using System;

public class StateMachine
{
    public IState CurrentState { get; private set; }

    public event Action<IState> OnStateChanged;

    public void ChangeState(IState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
        OnStateChanged?.Invoke(newState);
    }

    public void Tick()
    {
        CurrentState?.Tick();
    }

    public void FixedTick()
    {
        CurrentState?.FixedTick();
    }
}