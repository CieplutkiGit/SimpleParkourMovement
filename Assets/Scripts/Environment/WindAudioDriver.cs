using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WindAudioDriver : MonoBehaviour
{
    [SerializeField] private Rigidbody _player;
    [SerializeField] private float _minSpeed = 2f;
    [SerializeField] private float _maxSpeed = 12f;
    [SerializeField] private float _minVolume = 0.05f;
    [SerializeField] private float _maxVolume = 0.7f;
    [SerializeField] private float _minPitch = 0.85f;
    [SerializeField] private float _maxPitch = 1.4f;
    [SerializeField] private float _smoothing = 4f;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
    }

    private void Update()
    {
        Vector3 flat = _player.linearVelocity;
        flat.y = 0f;

        float t = Mathf.InverseLerp(_minSpeed, _maxSpeed, flat.magnitude);
        float k = 1f - Mathf.Exp(-_smoothing * Time.deltaTime);

        _source.volume = Mathf.Lerp(_source.volume, Mathf.Lerp(_minVolume, _maxVolume, t), k);
        _source.pitch = Mathf.Lerp(_source.pitch, Mathf.Lerp(_minPitch, _maxPitch, t), k);
    }
}
