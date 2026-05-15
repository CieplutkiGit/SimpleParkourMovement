using UnityEngine;

public class LedgeGrabState : IState
{
    private readonly PlayerController _controller;

    private Vector3 _startPos;
    private Quaternion _startRot;
    private Vector3 _hangPos;
    private Quaternion _hangRot;
    private float _timer;
    private bool _snapped;

    public LedgeGrabState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        Vector3 point = _controller.LedgeDetector.LedgePoint;
        Vector3 normal = _controller.LedgeDetector.LedgeNormal;

        _controller.LedgeGrab.BeginGrab(point, normal);

        _startPos = _controller.transform.position;
        _startRot = _controller.transform.rotation;
        _hangPos = _controller.LedgeGrab.GetHangTarget(point, normal);
        _hangRot = _controller.LedgeGrab.GetHangRotation(normal);
        _timer = 0f;
        _snapped = false;
    }

    public void Exit()
    {
        _controller.LedgeGrab.ReleaseLedge();
    }

    public void Tick()
    {
        if (!_snapped)
            return;

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
            _controller.StateMachine.ChangeState(_controller.AirborneState);
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
        if (_snapped)
            return;

        _timer += Time.fixedDeltaTime;
        float t = _timer / _controller.LedgeGrab.SnapDuration;
        float smooth = Mathf.SmoothStep(0f, 1f, t);

        _controller.RB.MovePosition(Vector3.Lerp(_startPos, _hangPos, smooth));
        _controller.RB.MoveRotation(Quaternion.Slerp(_startRot, _hangRot, smooth));

        if (t >= 1f)
            _snapped = true;
    }
}
