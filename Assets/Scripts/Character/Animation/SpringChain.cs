using UnityEngine;

public class SpringChain : MonoBehaviour
{
    [SerializeField] private Transform[] _bones;
    [SerializeField] private WindSO _wind;
    [SerializeField] private float _stiffness = 12f;
    [SerializeField] private float _damping = 4f;
    [SerializeField] private float _gravity = 15f;
    [SerializeField] private float _windInfluence = 0.3f;
    [SerializeField] private float _maxStep = 0.02f;
    [SerializeField] private Transform _colliderTop;
    [SerializeField] private Transform _colliderBottom;
    [SerializeField] private float _colliderRadius = 0.15f;

    private Transform _anchorParent;
    private Vector3 _capA;
    private Vector3 _capB;
    private Vector3[] _segmentLocal;
    private Vector3[] _positions;
    private Vector3[] _velocities;
    private float[] _lengths;
    private Vector3[] _boneAxis;
    private Quaternion[] _bindLocalRot;

    private void Awake()
    {
        int count = _bones.Length;

        _anchorParent = _bones[0].parent;
        _segmentLocal = new Vector3[count];
        _positions = new Vector3[count];
        _velocities = new Vector3[count];
        _lengths = new float[count];
        _boneAxis = new Vector3[count];
        _bindLocalRot = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            _positions[i] = _bones[i].position;
            _bindLocalRot[i] = _bones[i].localRotation;

            if (i > 0)
            {
                _lengths[i] = Vector3.Distance(_bones[i].position, _bones[i - 1].position);
                Vector3 worldDir = (_bones[i].position - _bones[i - 1].position).normalized;
                _segmentLocal[i] = _anchorParent.InverseTransformDirection(worldDir) * _lengths[i];
            }

            if (i < count - 1)
                _boneAxis[i] = _bones[i].InverseTransformDirection(
                    (_bones[i + 1].position - _bones[i].position).normalized);
        }
    }

    /// <summary>
    /// Re-seed simulation after an accessory has been hidden, or after a teleport.
    /// Avoids a large velocity spike when switching back from cap/hood to scarf.
    /// </summary>
    public void ResetSimulation()
    {
        if (_positions == null || _velocities == null || _bones == null)
            return;

        for (int i = 0; i < _bones.Length; i++)
        {
            if (_bones[i] == null)
                continue;

            _positions[i] = _bones[i].position;
            _velocities[i] = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        _positions[0] = _bones[0].position;
        _capA = _colliderTop.position;
        _capB = _colliderBottom.position;

        float remaining = Time.deltaTime;

        while (remaining > 0f)
        {
            float h = Mathf.Min(remaining, _maxStep);
            Step(h);
            remaining -= h;
        }

        Apply();
    }

    private void Step(float h)
    {
        Vector3 external = Vector3.down * _gravity + _wind.Current * _windInfluence;

        for (int i = 1; i < _positions.Length; i++)
        {
            Vector3 restTarget = _positions[i - 1] + _anchorParent.TransformDirection(_segmentLocal[i]);
            Vector3 spring = _stiffness * (restTarget - _positions[i]) - _damping * _velocities[i];

            _velocities[i] += (spring + external) * h;
            _positions[i] += _velocities[i] * h;

            Vector3 direction = _positions[i] - _positions[i - 1];
            _positions[i] = _positions[i - 1] + direction.normalized * _lengths[i];
            _positions[i] = Collide(_positions[i]);
        }
    }

    private Vector3 Collide(Vector3 p)
    {
        Vector3 ab = _capB - _capA;
        float t = Mathf.Clamp01(Vector3.Dot(p - _capA, ab) / Vector3.Dot(ab, ab));
        Vector3 c = _capA + ab * t;
        Vector3 delta = p - c;
        float sqr = delta.sqrMagnitude;

        if (sqr > 0f && sqr < _colliderRadius * _colliderRadius)
            return c + delta * (_colliderRadius / Mathf.Sqrt(sqr));

        return p;
    }

    private void Apply()
    {
        for (int i = 0; i < _bones.Length - 1; i++)
        {
            _bones[i].localRotation = _bindLocalRot[i];

            Vector3 aimVector = _bones[i].TransformDirection(_boneAxis[i]);
            Vector3 desired = _positions[i + 1] - _bones[i].position;

            if (desired.sqrMagnitude < 1e-8f)
                continue;

            Quaternion aimRotation = Quaternion.FromToRotation(aimVector, desired);
            _bones[i].rotation = aimRotation * _bones[i].rotation;
        }
    }

    private void OnDrawGizmos()
    {
        if (_bones == null || _bones.Length < 2)
            return;

        bool playing = Application.isPlaying;
        Gizmos.color = Color.cyan;

        for (int i = 0; i < _bones.Length; i++)
        {
            if (_bones[i] == null)
                return;

            Vector3 pos = (playing && _positions != null) ? _positions[i] : _bones[i].position;
            Gizmos.DrawSphere(pos, 0.03f);

            if (i > 0)
            {
                Vector3 prev = (playing && _positions != null) ? _positions[i - 1] : _bones[i - 1].position;
                Gizmos.DrawLine(prev, pos);
            }
        }

        if (_colliderTop == null || _colliderBottom == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_colliderTop.position, _colliderRadius);
        Gizmos.DrawWireSphere(_colliderBottom.position, _colliderRadius);
        Gizmos.DrawLine(_colliderTop.position, _colliderBottom.position);
    }
}
