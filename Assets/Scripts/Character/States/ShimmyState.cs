using UnityEngine;

public class ShimmyState : IState
{
    private readonly PlayerController _controller;

    private Vector3 _point;
    private Vector3 _normal;

    private Vector3 _frontAnchor;
    private Vector3 _rearAnchor;
    private bool _frontIsRight;

    private bool _striding;
    private float _phase;
    private int _dir;
    private Vector3 _reachFrom;
    private Vector3 _reachTo;
    private Vector3 _bodyStart;
    private Vector3 _bodyEnd;

    public ShimmyState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.RB.isKinematic = true;
        _point = _controller.LedgeGrab.CachedLedgePoint;
        _normal = _controller.LedgeGrab.CachedLedgeNormal;

        Vector3 d = _controller.LedgeGrab.GetShimmyDirection(_normal);
        float half = _controller.LedgeGrab.StrideLength * 0.5f;

        _frontAnchor = ClampToLedge(_point + d * half);
        _rearAnchor = ClampToLedge(_point - d * half);
        _frontIsRight = true;
        _striding = false;
        _phase = 0f;

        PushAnchors();
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

        if (!_striding && Mathf.Abs(input) > 0.1f)
            TryBeginStride(input);

        if (_striding)
            AdvanceStride();

        _controller.LedgeGrab.SetCachedLedge(_point, _normal);
        PushAnchors();

        _controller.RB.MovePosition(_controller.LedgeGrab.GetHangTarget(_point, _normal));
        _controller.RB.MoveRotation(_controller.LedgeGrab.GetHangRotation(_normal));
    }

    private void TryBeginStride(float input)
    {
        _dir = input > 0f ? 1 : -1;

        Vector3 d = _controller.LedgeGrab.GetShimmyDirection(_normal);

        float frontProj = Vector3.Dot(_frontAnchor, d) * _dir;
        float rearProj = Vector3.Dot(_rearAnchor, d) * _dir;

        if (rearProj > frontProj)
        {
            (_frontAnchor, _rearAnchor) = (_rearAnchor, _frontAnchor);
            _frontIsRight = !_frontIsRight;
        }

        Vector3 target = _frontAnchor + d * _dir * _controller.LedgeGrab.StrideLength;

        if (!_controller.LedgeDetector.TryGetLedgeTop(target, out Vector3 top))
            return;

        _reachFrom = _rearAnchor;
        _reachTo = top;
        _bodyStart = _point;
        _bodyEnd = (top + _frontAnchor) * 0.5f;
        _phase = 0f;
        _striding = true;
    }

    private void AdvanceStride()
    {
        _phase += _controller.LedgeGrab.StrideSpeed * Time.fixedDeltaTime;
        float p = Mathf.Clamp01(_phase);

        if (p < 0.5f)
        {
            float a = Mathf.SmoothStep(0f, 1f, p / 0.5f);
            AssignRear(Vector3.Lerp(_reachFrom, _reachTo, a));
            _point = _bodyStart;
        }
        else
        {
            float b = Mathf.SmoothStep(0f, 1f, (p - 0.5f) / 0.5f);
            AssignRear(_reachTo);
            _point = Vector3.Lerp(_bodyStart, _bodyEnd, b);
        }

        if (_phase < 1f)
            return;

        Vector3 newFront = _reachTo;
        Vector3 newRear = _frontAnchor;
        _frontAnchor = newFront;
        _rearAnchor = newRear;
        _frontIsRight = !_frontIsRight;

        _point = _bodyEnd;
        _striding = false;
        _phase = 0f;
    }

    private void AssignRear(Vector3 pos)
    {
        _rearAnchor = pos;
    }

    private void PushAnchors()
    {
        Vector3 right = _frontIsRight ? _frontAnchor : _rearAnchor;
        Vector3 left = _frontIsRight ? _rearAnchor : _frontAnchor;
        _controller.LedgeGrab.SetHandAnchors(right, left);
    }

    private Vector3 ClampToLedge(Vector3 approxTop)
    {
        return _controller.LedgeDetector.TryGetLedgeTop(approxTop, out Vector3 top) ? top : _point;
    }
}
