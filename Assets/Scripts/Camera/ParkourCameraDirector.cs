using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ParkourCameraDirector : MonoBehaviour
{
    private const int ActivePriority = 20;
    private const int InactivePriority = 10;

    [SerializeField] private CinemachineVirtualCamera _defaultCamera;
    [SerializeField] private CinemachineVirtualCamera _wallRunCamera;
    [SerializeField] private CinemachineVirtualCamera _climbLedgeCamera;

    private PlayerController _player;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _player.StateMachine.OnStateChanged += HandleStateChanged;

        if (_player.StateMachine.CurrentState != null)
            HandleStateChanged(_player.StateMachine.CurrentState);
    }

    private void OnDestroy()
    {
        if (_player != null && _player.StateMachine != null)
            _player.StateMachine.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(IState state)
    {
        Activate(SelectCamera(state));
    }

    private CinemachineVirtualCamera SelectCamera(IState state)
    {
        switch (state)
        {
            case WallRunState:
            case WallRunUpState:
                return _wallRunCamera;
            case LedgeGrabState:
            case ShimmyState:
            case ClimbUpState:
            case VaultState:
                return _climbLedgeCamera;
            default:
                return _defaultCamera;
        }
    }

    private void Activate(CinemachineVirtualCamera target)
    {
        _defaultCamera.Priority = target == _defaultCamera ? ActivePriority : InactivePriority;
        _wallRunCamera.Priority = target == _wallRunCamera ? ActivePriority : InactivePriority;
        _climbLedgeCamera.Priority = target == _climbLedgeCamera ? ActivePriority : InactivePriority;
    }
}
