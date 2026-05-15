using UnityEngine;

public class ClimbUpState : IState
{
    private readonly PlayerController _controller;
    
    private float _timer;
    private Vector3 _startPos;
    private Vector3 _endPos;

    public ClimbUpState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.RB.isKinematic = true;
        _timer = 0f;
        _startPos = _controller.transform.position;

        Vector3 point = _controller.LedgeGrab.CachedLedgePoint;
        Vector3 normal = _controller.LedgeGrab.CachedLedgeNormal;
        _endPos = _controller.LedgeGrab.GetClimbUpTarget(point, normal);
    }

    public void Exit()
    {
        _controller.RB.isKinematic = false;
    }

    public void Tick()
    {
        _timer += Time.deltaTime;
        float percent = _timer / _controller.LedgeGrab.ClimbUpDuration;

        Vector3 currentPos = Vector3.Lerp(_startPos, _endPos, percent);
        
        currentPos.y += Mathf.Sin(percent * Mathf.PI) * 0.5f;

        _controller.transform.position = currentPos;

        if (percent >= 1f)
        {
            _controller.StateMachine.ChangeState(_controller.GroundedState);
        }
    }

    public void FixedTick()
    {
    }
}