using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Non-destructive headwear slot for the Mixamo Y Bot. Uses humanoid bones so
/// accessories follow every existing parkour animation without altering its state machine.
/// Prototype meshes can be replaced with artist-authored prefabs in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public sealed class HeadwearController : MonoBehaviour
{
    public enum HeadwearStyle
    {
        OriginalScarf,
        TechnicalCap,
        RaisedHood,
        Bare
    }

    [Header("Rig")]
    [SerializeField] private Animator _animator;
    [SerializeField] private HeadwearStyle _initialStyle = HeadwearStyle.TechnicalCap;

    [Header("Optional production prefabs")]
    [SerializeField] private GameObject _capPrefab;
    [SerializeField] private GameObject _hoodPrefab;

    [Header("Fit to character (local to head bone)")]
    [SerializeField] private Vector3 _capOffset = new Vector3(0f, 0.16f, 0.015f);
    [SerializeField] private Vector3 _capEuler;
    [SerializeField] private Vector3 _hoodOffset = new Vector3(0f, 0.105f, -0.015f);
    [SerializeField] private Vector3 _hoodEuler;

    [Header("Fabric materials (URP Lit recommended)")]
    [SerializeField] private Material _fabric;
    [SerializeField] private Material _trim;
    [SerializeField] private Material _accent;

    [Header("Development")]
    [SerializeField] private bool _allowHKeyCycle = true;

    private Transform _head;
    private Transform _neck;
    private GameObject _capView;
    private GameObject _hoodView;
    private SkinnedMeshRenderer _scarfRenderer;
    private SpringChain _scarfMotion;
    private bool _scarfWasVisible;
    private bool _scarfMotionWasEnabled;
    private readonly System.Collections.Generic.List<Mesh> _generatedMeshes = new System.Collections.Generic.List<Mesh>();
    private readonly System.Collections.Generic.List<Material> _generatedMaterials = new System.Collections.Generic.List<Material>();

    public HeadwearStyle CurrentStyle { get; private set; }

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError("Headwear: No Animator found on the player.", this);
            enabled = false;
            return;
        }

        _head = ResolveBone(HumanBodyBones.Head, "Head");
        _neck = ResolveBone(HumanBodyBones.Neck, "Neck");

        if (_head == null)
        {
            Debug.LogError("Headwear: Could not find the Y Bot head bone. Check the Humanoid rig/avatar.", this);
            enabled = false;
            return;
        }

        // Limit hiding to the scarf mesh, not the character's body renderer.
        SkinnedStripMesh original = GetComponentInChildren<SkinnedStripMesh>(true);
        if (original != null)
            _scarfRenderer = original.GetComponent<SkinnedMeshRenderer>();

        _scarfMotion = GetComponentInChildren<SpringChain>(true);
        if (_scarfRenderer != null)
            _scarfWasVisible = _scarfRenderer.enabled;
        if (_scarfMotion != null)
            _scarfMotionWasEnabled = _scarfMotion.enabled;

        Material fabric = _fabric != null ? _fabric : CreateMaterial("Headwear charcoal fabric", new Color(0.065f, 0.077f, 0.09f), 0.18f);
        Material trim = _trim != null ? _trim : CreateMaterial("Headwear graphite trim", new Color(0.018f, 0.025f, 0.032f), 0.28f);
        Material accent = _accent != null ? _accent : CreateMaterial("Headwear muted orange detail", new Color(0.93f, 0.28f, 0.075f), 0.23f);

        _capView = CreateView(_capPrefab, _head, "Technical Cap", _capOffset, _capEuler);
        if (_capPrefab == null)
            ProceduralHeadwearMeshes.BuildTechnicalCap(_capView.transform, fabric, trim, accent, _generatedMeshes);

        // The raised hood is anchored to the head. Its bottom is deliberately spacious
        // to allow turns; an authored full hoodie should instead be skinned to the torso.
        _hoodView = CreateView(_hoodPrefab, _head, "Raised Hood", _hoodOffset, _hoodEuler);
        if (_hoodPrefab == null)
            ProceduralHeadwearMeshes.BuildRaisedHood(_hoodView.transform, fabric, trim, accent, _generatedMeshes);

        SetStyle(_initialStyle);
    }

    private Transform ResolveBone(HumanBodyBones humanBone, string suffix)
    {
        if (_animator.isHuman && _animator.avatar != null && _animator.avatar.isValid)
        {
            Transform bone = _animator.GetBoneTransform(humanBone);
            if (bone != null)
                return bone;
        }

        // Keeps the feature usable while an imported model is still set to Generic.
        Transform[] transforms = _animator.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == suffix || candidate.name.EndsWith(":" + suffix, System.StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }

    private static GameObject CreateView(GameObject prefab, Transform parent, string objectName, Vector3 offset, Vector3 euler)
    {
        GameObject view = prefab != null ? Object.Instantiate(prefab, parent, false) : new GameObject(objectName);
        view.name = objectName;
        view.transform.SetParent(parent, false);
        view.transform.localPosition = offset;
        view.transform.localRotation = Quaternion.Euler(euler);
        return view;
    }

    private Material CreateMaterial(string name, Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("Headwear: Neither URP Lit nor Standard shader was found.", this);
            return null;
        }

        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f); // Hood interior is intentionally visible.
        _generatedMaterials.Add(material);
        return material;
    }

    /// <summary>Call from a cosmetics UI, or use H in a development build.</summary>
    public void SetStyle(HeadwearStyle style)
    {
        if (_head == null)
            return;

        CurrentStyle = style;
        if (_capView != null)
            _capView.SetActive(style == HeadwearStyle.TechnicalCap);
        if (_hoodView != null)
            _hoodView.SetActive(style == HeadwearStyle.RaisedHood);

        bool useScarf = style == HeadwearStyle.OriginalScarf;
        if (_scarfRenderer != null)
            _scarfRenderer.enabled = useScarf && _scarfWasVisible;
        if (_scarfMotion != null)
        {
            if (useScarf && _scarfMotionWasEnabled && !_scarfMotion.enabled)
                _scarfMotion.ResetSimulation();
            _scarfMotion.enabled = useScarf && _scarfMotionWasEnabled;
        }
    }

    private void Update()
    {
        if (!_allowHKeyCycle || Keyboard.current == null || !Keyboard.current.hKey.wasPressedThisFrame)
            return;

        HeadwearStyle next = (HeadwearStyle)(((int)CurrentStyle + 1) % 4);
        SetStyle(next);
    }

    private void OnDestroy()
    {
        // Restore the scene's original scarf if this component is removed at runtime.
        if (_scarfRenderer != null)
            _scarfRenderer.enabled = _scarfWasVisible;
        if (_scarfMotion != null)
            _scarfMotion.enabled = _scarfMotionWasEnabled;

        foreach (Mesh mesh in _generatedMeshes)
            if (mesh != null)
                Destroy(mesh);
        foreach (Material material in _generatedMaterials)
            if (material != null)
                Destroy(material);
    }
}
