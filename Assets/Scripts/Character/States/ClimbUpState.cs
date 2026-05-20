using UnityEngine;

public class ClimbUpState : IState
{
    private readonly PlayerController _controller;
    private readonly Collider _collider;

    private float _timer;

    public ClimbUpState(PlayerController controller)
    {
        _controller = controller;
        _collider = controller.GetComponent<Collider>();
    }

    public void Enter()
    {
        _controller.RB.isKinematic = true;
        _collider.enabled = false;
        _timer = 0f;

        Vector3 point = _controller.LedgeGrab.CachedLedgePoint;
        Vector3 normal = _controller.LedgeGrab.CachedLedgeNormal;
        _controller.RB.position = _controller.LedgeGrab.GetHangTarget(point, normal);
        _controller.RB.rotation = _controller.LedgeGrab.GetHangRotation(normal);
    }

    public void Exit()
    {
        _collider.enabled = true;
        _controller.RB.isKinematic = false;
    }

    public void Tick()
    {
    }

    public void FixedTick()
    {
        _timer += Time.fixedDeltaTime;

        if (_timer >= _controller.LedgeGrab.ClimbUpDuration)
        {
            _controller.StateMachine.ChangeState(_controller.GroundedState);
        }
    }
}