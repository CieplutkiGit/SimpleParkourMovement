public class GroundedState : IState
{
    private readonly PlayerController _controller;

    public GroundedState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter() { }

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
        _controller.Movement.HandleGroundedMovement();
        
        bool didJump = _controller.Jump.HandleJump();
        if (didJump)
        {
            _controller.StateMachine.ChangeState(_controller.AirborneState);
        }
    }
}