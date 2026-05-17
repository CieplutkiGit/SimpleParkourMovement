using UnityEngine;

public class ShimmyState : IState
{
    private readonly PlayerController _controller;

    private Vector3 _point;
    private Vector3 _normal;

    public ShimmyState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.RB.isKinematic = true;
        _point = _controller.LedgeGrab.CachedLedgePoint;
        _normal = _controller.LedgeGrab.CachedLedgeNormal;
    }

    public void Exit()
    {
        _controller.LedgeGrab.ReleaseLedge();
    }

    public void Tick()
    {
        float forwardInput = _controller.InputProvider != null ? _controller.InputProvider.MoveInput.y : 0f;

        if (forwardInput > 0.5f)
        {
            _controller.LedgeDetector.PauseDetection(_controller.LedgeGrab.JumpCooldownDuration);
            _controller.StateMachine.ChangeState(_controller.ClimbUpState);
            return;
        }

        if (forwardInput < -0.5f)
        {
            _controller.LedgeDetector.PauseDetection(_controller.LedgeGrab.JumpCooldownDuration);
            _controller.StateMachine.ChangeState(_controller.FallingState);
            return;
        }

        if (_controller.Jump.CheckAndConsumeJumpInput())
        {
            _controller.LedgeDetector.PauseDetection(_controller.LedgeGrab.JumpCooldownDuration);
            _controller.Jump.PerformLedgeJump(_controller.LedgeGrab.CachedLedgeNormal);
            _controller.StateMachine.ChangeState(_controller.AirborneState);
        }
    }

    public void FixedTick()
    {
        float input = _controller.InputProvider != null ? _controller.InputProvider.MoveInput.x : 0f;

        if (Mathf.Abs(input) > 0.1f)
        {
            Vector3 direction = _controller.LedgeGrab.GetShimmyDirection(_normal);
            Vector3 candidate = _point + direction * Mathf.Sign(input) * _controller.LedgeGrab.ShimmySpeed * Time.fixedDeltaTime;

            if (_controller.LedgeDetector.TryGetLedgeTop(candidate, out Vector3 top))
                _point = new Vector3(top.x, _point.y, top.z);
        }

        _controller.LedgeGrab.SetCachedLedge(_point, _normal);

        _controller.RB.MovePosition(_controller.LedgeGrab.GetHangTarget(_point, _normal));
        _controller.RB.MoveRotation(_controller.LedgeGrab.GetHangRotation(_normal));
    }
}
