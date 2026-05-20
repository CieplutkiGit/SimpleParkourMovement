using UnityEngine;

public class WallRunState : IState
{
    private readonly PlayerController _controller;

    private float _timer;
    private Vector3 _wallNormal;

    public WallRunState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.RB.useGravity = false;
        _timer = 0f;
        _wallNormal = _controller.WallDetector.WallNormal;
    }

    public void Exit()
    {
        _controller.RB.useGravity = true;
        _controller.WallRun.RememberWall(_wallNormal);
    }

    public void Tick()
    {
        if (_controller.Jump.CheckAndConsumeJumpInput())
        {
            _controller.Jump.PerformWallJump(_wallNormal);
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
            _controller.StateMachine.ChangeState(_controller.FallingState);
        }
    }

    public void FixedTick()
    {
        _timer += Time.fixedDeltaTime;
        float t = _timer / _controller.WallRun.WallRunDuration;

        Vector3 detected = _controller.WallDetector.WallNormal;
        if (detected != Vector3.zero)
            _wallNormal = detected;

        _controller.WallRun.ApplyWallRun(_wallNormal, t);
    }
}
