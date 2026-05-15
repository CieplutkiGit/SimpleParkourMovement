using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private float _horizontalSensitivity = 0.1f;
    [SerializeField] private float _verticalSensitivity = 0.1f;
    [SerializeField] private float _minVerticalAngle = -40f;
    [SerializeField] private float _maxVerticalAngle = 70f;

    private float _xRotation;
    private float _yRotation;

    private void LateUpdate()
    {
        _yRotation += _inputReader.LookInput.x * _horizontalSensitivity;
        _xRotation -= _inputReader.LookInput.y * _verticalSensitivity;
        _xRotation = Mathf.Clamp(_xRotation, _minVerticalAngle, _maxVerticalAngle);
        
        _cameraRoot.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
    }
}
