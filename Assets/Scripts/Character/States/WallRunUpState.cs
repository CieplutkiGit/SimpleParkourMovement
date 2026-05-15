using UnityEngine;

public class WallRunUpState : IState
{
    private readonly PlayerController _controller;

    private float _timer;

    public WallRunUpState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.RB.useGravity = false;
        _timer = 0f;
    }

    public void Exit()
    {
        _controller.RB.useGravity = true;
    }

    public void Tick()
    {
        if (_controller.LedgeDetector.LedgeFound)
        {
            _controller.StateMachine.ChangeState(_controller.LedgeGrabState);
            return;
        }

        if (_controller.Jump.CheckAndConsumeJumpInput())
        {
            _controller.Jump.PerformWallJump(_controller.WallDetector.FrontWallNormal);
            _controller.StateMachine.ChangeState(_controller.AirborneState);
            return;
        }

        if (_controller.GroundDetector.IsGrounded)
        {
            _controller.StateMachine.ChangeState(_controller.GroundedState);
            return;
        }

        if (!_controller.WallDetector.IsWallFront)
        {
            _controller.StateMachine.ChangeState(_controller.FallingState);
            return;
        }

        if (_timer >= _controller.WallRun.WallRunUpDuration)
        {
            _controller.WallRun.ConsumeWallRun();
            _controller.StateMachine.ChangeState(_controller.FallingState);
        }
    }

    public void FixedTick()
    {
        _timer += Time.fixedDeltaTime;
        float t = _timer / _controller.WallRun.WallRunUpDuration;
        _controller.WallRun.ApplyWallRunUp(_controller.WallDetector.FrontWallNormal, t);
    }
}
