using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class CharacterAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _footOffset = 0.1f;
    [SerializeField] private float _handSpread = 0.25f;

    private Animator _animator;
    private PlayerController _player;

    private IState _previousState;
    private bool _isShimmying;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _player = GetComponent<PlayerController>();
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
    }

    private void OnStateChanged(IState state)
    {
        switch (state)
        {
            case GroundedState:
                _animator.CrossFade("Locomotion", 0.15f);
                break;
            case AirborneState:
                _animator.CrossFade("Jump", 0.1f);
                break;
            case FallingState:
                _animator.CrossFade("Fall", 0.15f);
                break;
            case WallRunState:
                _animator.CrossFade("WallRun", 0.1f);
                break;
            case WallRunUpState:
                _animator.CrossFade("WallClimbUp", 0.1f);
                break;
            case LedgeGrabState:
            case ShimmyState:
                _animator.CrossFade("Hang", 0.15f);
                break;
            case ClimbUpState:
                _animator.CrossFade("ClimbUp", 0.1f);
                break;
            case VaultState:
                _animator.CrossFade("Vault", 0.1f);
                break;
        }
    }

    private void UpdateContinuous(IState state)
    {
        if (state is GroundedState)
        {
            _animator.SetFloat(SpeedHash, _player.RB.linearVelocity.magnitude, 0.1f, Time.deltaTime);
            return;
        }

        if (state is ShimmyState)
        {
            float input = _player.InputProvider != null ? Mathf.Abs(_player.InputProvider.MoveInput.x) : 0f;
            bool moving = input > 0.1f;

            if (moving == _isShimmying)
                return;

            _isShimmying = moving;
            _animator.CrossFade(moving ? "Shimmy" : "Hang", 0.1f);
        }
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
            ApplyLedgeHandIK(1f);
    }

    private void ApplyLedgeHandIK(float weight)
    {
        Vector3 ledgePoint  = _player.LedgeGrab.CachedLedgePoint;
        Vector3 ledgeNormal = _player.LedgeGrab.CachedLedgeNormal;
        Vector3 shimmyDir   = _player.LedgeGrab.GetShimmyDirection(ledgeNormal);

        Debug.Log($"[HandIK] applied. point={ledgePoint} normal={ledgeNormal} dir={shimmyDir} spread={_handSpread}");

        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
        _animator.SetIKPosition(AvatarIKGoal.RightHand, ledgePoint + shimmyDir * _handSpread);

        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
        _animator.SetIKPosition(AvatarIKGoal.LeftHand, ledgePoint - shimmyDir * _handSpread);
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
