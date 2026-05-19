using UnityEngine;

public class SpringChain : MonoBehaviour
{
    [SerializeField] private Transform[] _bones;
    [SerializeField] private WindSO _wind;
    [SerializeField] private float _stiffness = 30f;
    [SerializeField] private float _damping = 5f;
    [SerializeField] private float _gravity = 9.81f;
    [SerializeField] private float _windInfluence = 0.5f;
    [SerializeField] private float _maxStep = 0.02f;

    private Transform _anchorParent;
    private Vector3[] _restLocal;
    private Vector3[] _positions;
    private Vector3[] _velocities;
    private float[] _lengths;
    private Vector3[] _boneAxis;

    private void Awake()
    {
        int count = _bones.Length;

        _anchorParent = _bones[0].parent;
        _restLocal = new Vector3[count];
        _positions = new Vector3[count];
        _velocities = new Vector3[count];
        _lengths = new float[count];
        _boneAxis = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            _restLocal[i] = _anchorParent.InverseTransformPoint(_bones[i].position);
            _positions[i] = _bones[i].position;

            if (i > 0)
                _lengths[i] = Vector3.Distance(_bones[i].position, _bones[i - 1].position);

            if (i < count - 1)
                _boneAxis[i] = _bones[i].InverseTransformDirection(
                    (_bones[i + 1].position - _bones[i].position).normalized);
        }
    }

    private void LateUpdate()
    {
        _positions[0] = _anchorParent.TransformPoint(_restLocal[0]);

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
            Vector3 rest = _anchorParent.TransformPoint(_restLocal[i]);
            Vector3 spring = _stiffness * (rest - _positions[i]) - _damping * _velocities[i];

            _velocities[i] += (spring + external) * h;
            _positions[i] += _velocities[i] * h;

            Vector3 direction = _positions[i] - _positions[i - 1];
            _positions[i] = _positions[i - 1] + direction.normalized * _lengths[i];
        }
    }

    private void Apply()
    {
        for (int i = 0; i < _bones.Length - 1; i++)
        {
            Vector3 direction = _positions[i + 1] - _positions[i];

            if (direction.sqrMagnitude < 1e-8f)
                continue;

            Vector3 current = _bones[i].TransformDirection(_boneAxis[i]);
            _bones[i].rotation = Quaternion.FromToRotation(current, direction) * _bones[i].rotation;
        }
    }

    private void OnDrawGizmos()
    {
        if (_bones == null || _bones.Length < 2)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < _bones.Length; i++)
        {
            if (_bones[i] == null)
                return;

            Gizmos.DrawSphere(_bones[i].position, 0.03f);

            if (i > 0)
                Gizmos.DrawLine(_bones[i - 1].position, _bones[i].position);
        }
    }
}
