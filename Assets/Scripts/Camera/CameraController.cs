using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _cameraRoot;

    [Header("Sensitivity")]
    [SerializeField] private float _horizontalSensitivity = 0.15f;
    [SerializeField] private float _verticalSensitivity = 0.15f;
    [SerializeField] private bool _invertY = false;

    [Header("Vertical Clamp")]
    [SerializeField] private float _minVerticalAngle = -40f;
    [SerializeField] private float _maxVerticalAngle = 70f;

    private IInputProvider _input;

    private float _xRotation;
    private float _yRotation;

    private void Awake()
    {
        _input = _inputReader;

        if (_input == null)
            Debug.LogError("Input Reader is not assigned or does not implement IInputProvider!");
    }

    private void LateUpdate()
    {
        float verticalMultiplier = _invertY ? 1f : -1f;

        _yRotation += _input.LookInput.x * _horizontalSensitivity;
        _xRotation += _input.LookInput.y * _verticalSensitivity * verticalMultiplier;
        _xRotation = Mathf.Clamp(_xRotation, _minVerticalAngle, _maxVerticalAngle);

        _cameraRoot.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
    }
}