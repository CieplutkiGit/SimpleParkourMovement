public class GroundedState : IState
{
    private readonly PlayerController _controller;

    public GroundedState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.WallRun.ResetWallRun();
    }

    public void Exit() { }

    public void Tick()
    {
        _controller.Jump.UpdateTimers();

        if (!_controller.GroundDetector.IsGrounded)
        {
            _controller.StateMachine.ChangeState(_controller.AirborneState);
        }
    }

    public void FixedTick()
    {
        if (TryVault())
            return;

        _controller.Movement.HandleGroundedMovement();

        bool didJump = _controller.Jump.HandleJump();
        if (didJump)
        {
            _controller.StateMachine.ChangeState(_controller.AirborneState);
        }
    }

    private bool TryVault()
    {
        float forwardInput = _controller.InputProvider != null ? _controller.InputProvider.MoveInput.y : 0f;

        if (forwardInput <= 0.1f || !_controller.VaultDetector.VaultFound)
            return false;

        _controller.StateMachine.ChangeState(_controller.VaultState);
        return true;
    }
}