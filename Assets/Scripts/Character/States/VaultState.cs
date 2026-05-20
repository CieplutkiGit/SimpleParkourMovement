using UnityEngine;

public class VaultState : IState
{
    private readonly PlayerController _controller;
    private readonly Collider _collider;

    public VaultState(PlayerController controller)
    {
        _controller = controller;
        _collider = controller.GetComponent<Collider>();
    }

    public void Enter()
    {
        _controller.Vault.BeginVault();
        _collider.enabled = false;
    }

    public void Exit()
    {
        _collider.enabled = true;
        _controller.Vault.ReleaseVault();
    }

    public void Tick() { }

    public void FixedTick()
    {
        if (_controller.CharacterAnimator.TryGetStateProgress("Vault", out float t) && t >= 1f)
            _controller.StateMachine.ChangeState(_controller.GroundedState);
    }
}
