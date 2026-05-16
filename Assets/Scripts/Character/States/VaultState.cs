using UnityEngine;

public class VaultState : IState
{
    private readonly PlayerController _controller;

    private float _timer;
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _apexY;

    public VaultState(PlayerController controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        _controller.Vault.BeginVault();
        _timer = 0f;
        _startPos = _controller.transform.position;
        _targetPos = _controller.VaultDetector.VaultTarget;
        _apexY = _controller.VaultDetector.ObstacleTop.y + _controller.Vault.ArcClearance;
    }

    public void Exit()
    {
        _controller.Vault.ReleaseVault();
    }

    public void Tick() { }

    public void FixedTick()
    {
        _timer += Time.fixedDeltaTime;
        float t = _timer / _controller.Vault.VaultDuration;

        _controller.RB.MovePosition(_controller.Vault.GetVaultPoint(_startPos, _targetPos, _apexY, t));

        if (t >= 1f)
            _controller.StateMachine.ChangeState(_controller.GroundedState);
    }
}
