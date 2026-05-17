using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class CharacterAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private const float StateBlend = 0.15f;
    private const float FastStateBlend = 0.1f;
    private const float SpeedDamp = 0.1f;

    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _footOffset = 0.1f;
    [SerializeField] private float _handForwardOffset = 0.15f;
    [SerializeField] private float _handHeightOffset = -0.05f;
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private float _footHangDrop = 0.55f;
    [SerializeField] private float _footHangSpread = 0.12f;

    private Animator _animator;
    private PlayerController _player;

    private IState _previousState;
    private bool _isShimmying;
    private bool _climbMatched;
    private bool _vaultMatched;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _player = GetComponent<PlayerController>();
    }

    public bool TryGetStateProgress(string stateName, out float progress)
    {
        if (_animator.IsInTransition(0))
        {
            progress = 0f;
            return false;
        }

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

        if (!state.IsName(stateName))
        {
            progress = 0f;
            return false;
        }

        progress = Mathf.Clamp01(state.normalizedTime);
        return true;
    }

    private void Update()
    {
        IState current = _player.StateMachine.CurrentState;

        if (current != _previousState)
        {
            _isShimmying = false;
            OnStateChanged(current);
            _previousState = current;
        }

        UpdateContinuous(current);

        if (current is ClimbUpState)
            TryMatchClimb();
        else
            _climbMatched = false;

        if (current is VaultState)
            TryMatchVault();
        else
            _vaultMatched = false;
    }

    private void TryMatchClimb()
    {
        if (_climbMatched || _animator.IsInTransition(0) || _animator.isMatchingTarget)
            return;

        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbUp"))
            return;

        Vector3 target = _player.LedgeGrab.GetClimbUpTarget(
            _player.LedgeGrab.CachedLedgePoint,
            _player.LedgeGrab.CachedLedgeNormal);

        _animator.MatchTarget(
            target,
            _player.RB.rotation,
            AvatarTarget.Root,
            new MatchTargetWeightMask(Vector3.one, 0f),
            _player.LedgeGrab.ClimbMatchStart,
            _player.LedgeGrab.ClimbMatchEnd);

        _climbMatched = true;
    }

    private void TryMatchVault()
    {
        if (_vaultMatched || _animator.IsInTransition(0) || _animator.isMatchingTarget)
            return;

        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Vault"))
            return;

        Vector3 target = _player.VaultDetector.VaultTarget + Vector3.up * _player.Movement.GroundSnapOffset;

        _animator.MatchTarget(
            target,
            _player.RB.rotation,
            AvatarTarget.Root,
            new MatchTargetWeightMask(Vector3.one, 0f),
            _player.Vault.VaultMatchStart,
            _player.Vault.VaultMatchEnd);

        _vaultMatched = true;
    }

    private void OnStateChanged(IState state)
    {
        switch (state)
        {
            case GroundedState:
                _animator.CrossFade("Locomotion", StateBlend);
                break;
            case AirborneState:
                _animator.CrossFade("Jump", FastStateBlend);
                break;
            case FallingState:
                _animator.CrossFade("Fall", StateBlend);
                break;
            case WallRunState:
                _animator.CrossFade(_player.WallDetector.IsWallLeft ? "WallRun" : "WallRunMirror", FastStateBlend);
                break;
            case WallRunUpState:
                _animator.CrossFade("WallClimbUp", FastStateBlend);
                break;
            case LedgeGrabState:
            case ShimmyState:
                _animator.CrossFade("Hang", StateBlend);
                break;
            case ClimbUpState:
                _animator.CrossFade("ClimbUp", FastStateBlend);
                break;
            case VaultState:
                _animator.CrossFade("Vault", FastStateBlend);
                break;
        }
    }

    private void UpdateContinuous(IState state)
    {
        if (state is GroundedState)
        {
            _animator.SetFloat(SpeedHash, _player.RB.linearVelocity.magnitude, SpeedDamp, Time.deltaTime);
            return;
        }

        if (state is ShimmyState)
        {
            float input = _player.InputProvider != null ? Mathf.Abs(_player.InputProvider.MoveInput.x) : 0f;
            bool moving = input > 0.1f;

            if (moving == _isShimmying)
                return;

            _isShimmying = moving;
            _animator.CrossFade(moving ? "Shimmy" : "Hang", FastStateBlend);
        }
    }

    public void ApplyRootMotion()
    {
        IState current = _player.StateMachine.CurrentState;

        if (current is not ClimbUpState && current is not VaultState)
            return;

        _player.RB.MovePosition(_player.RB.position + _animator.deltaPosition);
        _player.RB.MoveRotation(_animator.deltaRotation * _player.RB.rotation);
    }

    public void RunIK(int layerIndex)
    {
        IState current = _player.StateMachine.CurrentState;

        if (current is GroundedState)
        {
            float speed = _player.RB.linearVelocity.magnitude;
            float ikWeight = Mathf.Clamp01(1f - speed / 2f);
            ApplyFootIK(AvatarIKGoal.LeftFoot, ikWeight);
            ApplyFootIK(AvatarIKGoal.RightFoot, ikWeight);
            return;
        }

        if (current is LedgeGrabState or ShimmyState)
        {
            ApplyLedgeHandIK();
            ApplyHangFootIK();
        }
    }

    private void ApplyHangFootIK()
    {
        Vector3 normal = _player.LedgeGrab.CachedLedgeNormal;
        Vector3 baseFoot = transform.position + Vector3.down * _footHangDrop;

        ApplyWallFoot(AvatarIKGoal.LeftFoot, baseFoot - transform.right * _footHangSpread, normal);
        ApplyWallFoot(AvatarIKGoal.RightFoot, baseFoot + transform.right * _footHangSpread, normal);
    }

    private void ApplyWallFoot(AvatarIKGoal foot, Vector3 height, Vector3 wallNormal)
    {
        Vector3 target = height;

        if (Physics.Raycast(height + wallNormal * 0.3f, -wallNormal, out RaycastHit hit, 0.6f, _wallLayer))
            target = hit.point + wallNormal * _footOffset;

        _animator.SetIKPositionWeight(foot, 1f);
        _animator.SetIKPosition(foot, target);
    }

    private void ApplyLedgeHandIK()
    {
        Vector3 ledgeNormal = _player.LedgeGrab.CachedLedgeNormal;
        Vector3 gripOffset = ledgeNormal * _handForwardOffset + Vector3.up * _handHeightOffset;

        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
        _animator.SetIKPosition(AvatarIKGoal.RightHand, _player.LedgeGrab.RightHandAnchor + gripOffset);

        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
        _animator.SetIKPosition(AvatarIKGoal.LeftHand, _player.LedgeGrab.LeftHandAnchor + gripOffset);
    }

    private void ApplyFootIK(AvatarIKGoal foot, float weight)
    {
        _animator.SetIKPositionWeight(foot, weight);
        _animator.SetIKRotationWeight(foot, weight);

        if (weight <= 0f)
            return;

        Ray ray = new Ray(_animator.GetIKPosition(foot) + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2f, _groundLayer))
        {
            _animator.SetIKPosition(foot, hit.point + Vector3.up * _footOffset);

            Quaternion tilt = Quaternion.FromToRotation(Vector3.up, hit.normal);
            _animator.SetIKRotation(foot, tilt * _animator.GetIKRotation(foot));
        }
    }
}
