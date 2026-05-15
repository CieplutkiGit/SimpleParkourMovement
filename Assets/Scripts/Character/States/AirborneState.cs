public class AirborneState : IState
{
    private readonly PlayerController _controller;

    public AirborneState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter() { }

    public void Exit() { }

    public void Tick()
    {
        _controller.Jump.UpdateTimers();

        if (_controller.LedgeDetector.LedgeFound && _controller.RB.linearVelocity.y < 0f)
        {
            _controller.StateMachine.ChangeState(_controller.LedgeGrabState);
            return;
        }

        if (_controller.GroundDetector.IsGrounded && _controller.RB.linearVelocity.y <= 0f)
        {
            _controller.StateMachine.ChangeState(_controller.GroundedState);
            return;
        }

        if (_controller.RB.linearVelocity.y < 0f && !_controller.Jump.CanCoyoteJump)
        {
            _controller.StateMachine.ChangeState(_controller.FallingState);
        }
    }

    public void FixedTick()
    {
        _controller.Movement.HandleAirborneMovement();
        _controller.Jump.HandleJump();
    }
}