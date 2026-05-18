using UnityEngine;

public class WallCameraPivot : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private Transform _lookRoot;
    [SerializeField] private float _heightOffset = 1.4f;
    [SerializeField] private float _outwardOffset = 1.6f;
    [SerializeField] private float _upOffset = 0.15f;
    [SerializeField] private float _normalSmoothTime = 0.08f;
    [SerializeField] private float _exitSmoothTime = 0.18f;

    private Vector3 _lastNormal = Vector3.forward;
    private Vector3 _offset;
    private Vector3 _offsetVelocity;
    private bool _wasContact;

    private void LateUpdate()
    {
        transform.rotation = _lookRoot.rotation;

        bool contact = TryGetWallNormal(out Vector3 normal);

        if (contact && normal != Vector3.zero)
            _lastNormal = normal;

        Vector3 targetOffset = contact
            ? _lastNormal * _outwardOffset + Vector3.up * _upOffset
            : Vector3.zero;

        if (contact && !_wasContact)
        {
            _offset = targetOffset;
            _offsetVelocity = Vector3.zero;
        }
        else
        {
            float smooth = contact ? _normalSmoothTime : _exitSmoothTime;
            _offset = Vector3.SmoothDamp(
                _offset, targetOffset, ref _offsetVelocity, smooth);
        }

        _wasContact = contact;

        transform.position =
            _player.transform.position + Vector3.up * _heightOffset + _offset;
    }

    private bool TryGetWallNormal(out Vector3 normal)
    {
        switch (_player.StateMachine.CurrentState)
        {
            case WallRunUpState:
                normal = _player.WallDetector.FrontWallNormal;
                return true;
            case WallRunState:
                normal = _player.WallDetector.WallNormal;
                return true;
            case LedgeGrabState:
            case ShimmyState:
            case ClimbUpState:
                normal = _player.LedgeGrab.CachedLedgeNormal;
                return true;
            default:
                normal = Vector3.zero;
                return false;
        }
    }
}
