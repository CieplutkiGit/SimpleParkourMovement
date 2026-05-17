using UnityEngine;

public class WallRunState : IState
{
    private readonly PlayerController _controller;

    private float _timer;

    public WallRunState(PlayerController controller)
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
        if (_controller.Jump.CheckAndConsumeJumpInput())
        {
            _controller.WallRun.ConsumeWallRun();
            _controller.Jump.PerformWallJump(_controller.WallDetector.WallNormal);
            _controller.StateMachine.ChangeState(_controller.AirborneState);
            return;
        }

        if (_controller.GroundDetector.IsGrounded)
        {
            _controller.StateMachine.ChangeState(_controller.GroundedState);
            return;
        }

        if (!_controller.WallDetector.IsWallLeft && !_controller.WallDetector.IsWallRight)
        {
            _controller.StateMachine.ChangeState(_controller.FallingState);
            return;
        }

        if (_timer >= _controller.WallRun.WallRunDuration)
        {
            _controller.WallRun.ConsumeWallRun();
            _controller.StateMachine.ChangeState(_controller.FallingState);
        }
    }

    public void FixedTick()
    {
        _timer += Time.fixedDeltaTime;
        float t = _timer / _controller.WallRun.WallRunDuration;
        _controller.WallRun.ApplyWallRun(_controller.WallDetector.WallNormal, t);
    }
}
