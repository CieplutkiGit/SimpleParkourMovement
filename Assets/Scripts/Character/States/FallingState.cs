public class FallingState : IState
{
    private readonly PlayerController _controller;

    public FallingState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter() { }

    public void Exit() { }

    public void Tick()
    {
        if (_controller.LedgeDetector.LedgeFound)
        {
            _controller.StateMachine.ChangeState(_controller.LedgeGrabState);
            return;
        }

        if (_controller.GroundDetector.IsGrounded)
        {
            _controller.StateMachine.ChangeState(_controller.GroundedState);
        }
    }

    public void FixedTick()
    {
        _controller.Movement.HandleAirborneMovement();
        _controller.Jump.ApplyFallGravity();
    }
}
