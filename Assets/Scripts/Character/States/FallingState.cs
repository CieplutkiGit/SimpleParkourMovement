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
            return;
        }

        float forwardInput = _controller.InputProvider != null ? _controller.InputProvider.MoveInput.y : 0f;

        if (forwardInput > 0.1f && _controller.WallDetector.IsWallFront)
        {
            _controller.StateMachine.ChangeState(_controller.WallRunUpState);
            return;
        }

        if (forwardInput > 0.1f && (_controller.WallDetector.IsWallLeft || _controller.WallDetector.IsWallRight))
        {
            _controller.StateMachine.ChangeState(_controller.WallRunState);
        }
    }

    public void FixedTick()
    {
        _controller.Movement.HandleAirborneMovement();
        _controller.Jump.ApplyFallGravity();
    }
}
