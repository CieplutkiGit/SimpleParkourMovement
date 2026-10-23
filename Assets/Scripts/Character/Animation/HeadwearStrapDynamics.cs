using UnityEngine;

/// <summary>
/// Lightweight, animation-following PBD ribbon for a cap adjustment tail or small
/// garment strap. Do not use on the rigid crown or brim. Reuses fixed buffers,
/// substeps motion, constrains segment lengths and resolves a head sphere.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(150)]
public sealed class HeadwearStrapDynamics : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField, Range(2, 16)] private int _segments = 7;
    [SerializeField, Min(0.03f)] private float _length = 0.16f;
    [SerializeField, Min(0.003f)] private float _width = 0.023f;
    [SerializeField] private Vector3 _restDirection = new Vector3(0.12f, -1f, -0.4f);

    [Header("Movement")]
    [SerializeField, Range(0f, 1f)] private float _damping = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _restPosePull = 0.13f;
    [SerializeField, Range(2, 12)] private int _constraintIterations = 5;
    [SerializeField, Min(0f)] private float _gravityScale = 0.35f;
    [SerializeField, Min(0f)] private float _windInfluence = 0.55f;
    [SerializeField, Range(0.005f, 0.03f)] private float _maxStep = 0.016f;
    [SerializeField, Min(0.1f)] private float _teleportDistance = 0.55f;
    [SerializeField, Range(10f, 180f)] private float _teleportAngle = 85f;

    [Header("Collision")]
    [SerializeField, Min(0.01f)] private float _headCollisionRadius = 0.14f;

    private Transform _anchor;
    private Transform _head;
    private WindSO _wind;
    private Rigidbody _body;
    private Mesh _mesh;
    private MeshFilter _filter;
    private MeshRenderer _renderer;
    private Vector3[] _positions;
    private Vector3[] _previous;
    private Vector3[] _vertices;
    private Vector2[] _uv;
    private int[] _triangles;
    private float _segmentLength;
    private Vector3 _oldAnchorPosition;
    private Quaternion _oldAnchorRotation;
    private bool _ready;

    /// <summary>Configure only once, after adding the component to an active object.</summary>
    public void Configure(Transform anchor, Transform collisionHead, Rigidbody body, WindSO wind, Material material)
    {
        _anchor = anchor;
        _head = collisionHead;
        _body = body;
        _wind = wind;

        if (_anchor == null)
        {
            Debug.LogError("Headwear strap requires an attachment transform.", this);
            enabled = false;
            return;
        }

        _segments = Mathf.Clamp(_segments, 2, 16);
        _segmentLength = _length / _segments;
        _positions = new Vector3[_segments + 1];
        _previous = new Vector3[_segments + 1];
        _vertices = new Vector3[(_segments + 1) * 4];
        _uv = new Vector2[_vertices.Length];
        _triangles = new int[_segments * 24];

        for (int i = 0; i <= _segments; i++)
        {
            float t = i / (float)_segments;
            int v = i * 4;
            _uv[v] = new Vector2(0f, t);
            _uv[v + 1] = new Vector2(1f, t);
            _uv[v + 2] = new Vector2(0f, t);
            _uv[v + 3] = new Vector2(1f, t);
        }

        int tri = 0;
        for (int i = 0; i < _segments; i++)
        {
            int a = i * 4;
            int b = (i + 1) * 4;
            // Front
            AddTriangle(ref tri, a, b, a + 1);
            AddTriangle(ref tri, a + 1, b, b + 1);
            // Back, with independent normals
            AddTriangle(ref tri, a + 2, a + 3, b + 2);
            AddTriangle(ref tri, a + 3, b + 3, b + 2);
            // Side edges, double sided material supplies visible thickness.
            AddTriangle(ref tri, a, a + 2, b);
            AddTriangle(ref tri, a + 2, b + 2, b);
            AddTriangle(ref tri, a + 1, b + 1, a + 3);
            AddTriangle(ref tri, a + 3, b + 1, b + 3);
        }

        _mesh = new Mesh { name = "Dynamic headwear adjustment tail" };
        _mesh.MarkDynamic();
        _mesh.vertices = _vertices;
        _mesh.uv = _uv;
        _mesh.triangles = _triangles;

        _filter = GetComponent<MeshFilter>();
        if (_filter == null)
            _filter = gameObject.AddComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null)
            _renderer = gameObject.AddComponent<MeshRenderer>();

        _filter.sharedMesh = _mesh;
        _renderer.sharedMaterial = material;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        _renderer.receiveShadows = true;

        _ready = true;
        ResetSimulation();
    }

    private void AddTriangle(ref int index, int a, int b, int c)
    {
        _triangles[index++] = a;
        _triangles[index++] = b;
        _triangles[index++] = c;
    }

    private void OnEnable()
    {
        if (_ready)
            ResetSimulation();
    }

    public void ResetSimulation()
    {
        if (!_ready)
            return;

        Vector3 origin = _anchor.position;
        Vector3 dir = _anchor.TransformDirection(_restDirection.sqrMagnitude > 0.001f
            ? _restDirection.normalized : Vector3.down);
        for (int i = 0; i <= _segments; i++)
        {
            Vector3 rest = origin + dir * (i * _segmentLength);
            _positions[i] = rest;
            _previous[i] = rest;
        }

        _oldAnchorPosition = origin;
        _oldAnchorRotation = _anchor.rotation;
        RefreshMesh();
    }

    private void LateUpdate()
    {
        if (!_ready || _anchor == null)
            return;

        Vector3 origin = _anchor.position;
        if ((origin - _oldAnchorPosition).sqrMagnitude > _teleportDistance * _teleportDistance ||
            Quaternion.Angle(_oldAnchorRotation, _anchor.rotation) > _teleportAngle)
        {
            ResetSimulation();
            return;
        }

        // Fixed maximum step; a stalled frame cannot explode the constraint solver.
        float remaining = Mathf.Min(Time.deltaTime, 0.05f);
        while (remaining > 0.00001f)
        {
            float h = Mathf.Min(_maxStep, remaining);
            SimulateStep(h, origin);
            remaining -= h;
        }

        _oldAnchorPosition = origin;
        _oldAnchorRotation = _anchor.rotation;
        RefreshMesh();
    }

    private void SimulateStep(float h, Vector3 origin)
    {
        Vector3 acceleration = Physics.gravity * _gravityScale;
        if (_wind != null)
            acceleration += _wind.Current * _windInfluence;

        // The player's full Rigidbody acceleration is already represented by
        // moving the pinned head anchor. Do not add velocity a second time.
        Vector3 restDir = _anchor.TransformDirection(_restDirection.sqrMagnitude > 0.001f
            ? _restDirection.normalized : Vector3.down);

        _positions[0] = origin;
        _previous[0] = origin;

        for (int i = 1; i <= _segments; i++)
        {
            Vector3 p = _positions[i];
            Vector3 velocity = (p - _previous[i]) * Mathf.Clamp01(1f - _damping * h * 30f);
            _previous[i] = p;
            _positions[i] = p + velocity + acceleration * (h * h);
            // Mild shape retention avoids chaotic straps during extreme vault poses.
            Vector3 rest = origin + restDir * (_segmentLength * i);
            _positions[i] = Vector3.Lerp(_positions[i], rest, _restPosePull * h * 30f);
        }

        for (int iteration = 0; iteration < _constraintIterations; iteration++)
        {
            _positions[0] = origin;
            for (int i = 1; i <= _segments; i++)
            {
                Vector3 delta = _positions[i] - _positions[i - 1];
                if (delta.sqrMagnitude < 1e-8f)
                    delta = restDir * _segmentLength;
                _positions[i] = _positions[i - 1] + delta.normalized * _segmentLength;

                if (_head != null)
                {
                    Vector3 toPoint = _positions[i] - _head.position;
                    float radiusSquared = _headCollisionRadius * _headCollisionRadius;
                    if (toPoint.sqrMagnitude < radiusSquared)
                    {
                        Vector3 normal = toPoint.sqrMagnitude > 1e-8f
                            ? toPoint.normalized : -_head.forward;
                        _positions[i] = _head.position + normal * _headCollisionRadius;
                    }
                }
            }
        }
    }

    private void RefreshMesh()
    {
        if (_mesh == null)
            return;

        for (int i = 0; i <= _segments; i++)
        {
            Vector3 tangent;
            if (i == 0)
                tangent = (_positions[1] - _positions[0]).normalized;
            else if (i == _segments)
                tangent = (_positions[i] - _positions[i - 1]).normalized;
            else
                tangent = (_positions[i + 1] - _positions[i - 1]).normalized;

            Vector3 width = Vector3.Cross(tangent, _anchor.forward);
            if (width.sqrMagnitude < 0.0001f)
                width = _anchor.right;
            width.Normalize();
            Vector3 normal = Vector3.Cross(width, tangent).normalized;

            float halfWidth = _width * 0.5f * Mathf.Lerp(1f, 0.76f, i / (float)_segments);
            Vector3 center = _positions[i];
            int v = i * 4;
            _vertices[v] = transform.InverseTransformPoint(center - width * halfWidth + normal * 0.001f);
            _vertices[v + 1] = transform.InverseTransformPoint(center + width * halfWidth + normal * 0.001f);
            _vertices[v + 2] = transform.InverseTransformPoint(center - width * halfWidth - normal * 0.001f);
            _vertices[v + 3] = transform.InverseTransformPoint(center + width * halfWidth - normal * 0.001f);
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
    }

    private void OnValidate()
    {
        _segments = Mathf.Clamp(_segments, 2, 16);
        _constraintIterations = Mathf.Clamp(_constraintIterations, 2, 12);
        _length = Mathf.Max(0.03f, _length);
        _width = Mathf.Max(0.003f, _width);
        _maxStep = Mathf.Clamp(_maxStep, 0.005f, 0.03f);
    }
}
