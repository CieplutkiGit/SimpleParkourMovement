using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ParkourImpactFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem _landDust;
    [SerializeField] private ParticleSystem _wallScuff;
    [SerializeField] private ParticleSystem _vaultDust;
    [SerializeField] private float _minLandSpeed = 4f;
    [SerializeField] private float _maxLandSpeed = 14f;
    [SerializeField] private int _minLandBurst = 6;
    [SerializeField] private int _maxLandBurst = 24;
    [SerializeField] private int _wallScuffBurst = 12;
    [SerializeField] private int _vaultBurst = 10;

    private PlayerController _player;
    private IState _previous;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _player.StateMachine.OnStateChanged += HandleStateChanged;
        _previous = _player.StateMachine.CurrentState;
    }

    private void OnDestroy()
    {
        if (_player != null && _player.StateMachine != null)
            _player.StateMachine.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(IState state)
    {
        switch (state)
        {
            case GroundedState when _previous is FallingState or AirborneState:
                EmitLanding();
                break;
            case WallRunState:
                Burst(_wallScuff, _player.WallDetector.WallPoint,
                    Quaternion.LookRotation(_player.WallDetector.WallNormal), _wallScuffBurst);
                break;
            case VaultState:
                Burst(_vaultDust, _player.VaultDetector.ObstacleTop, Quaternion.identity, _vaultBurst);
                break;
        }

        _previous = state;
    }

    private void EmitLanding()
    {
        float speed = -_player.RB.linearVelocity.y;

        if (speed < _minLandSpeed)
            return;

        float t = Mathf.InverseLerp(_minLandSpeed, _maxLandSpeed, speed);
        int count = Mathf.RoundToInt(Mathf.Lerp(_minLandBurst, _maxLandBurst, t));

        Burst(_landDust, _player.GroundDetector.GroundPoint, Quaternion.identity, count);
    }

    private void Burst(ParticleSystem system, Vector3 position, Quaternion rotation, int count)
    {
        system.transform.SetPositionAndRotation(position, rotation);
        system.Emit(count);
    }
}
