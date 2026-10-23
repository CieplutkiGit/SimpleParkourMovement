using UnityEngine;

[DefaultExecutionOrder(-150)]
public class WindWake : MonoBehaviour
{
    private static readonly int GlobalWindId = Shader.PropertyToID("_GlobalWind");

    [SerializeField] private WindSO _wind;
    [SerializeField] private Rigidbody _player;
    [SerializeField] private Vector3 _baseDirection = Vector3.right;
    [SerializeField] private float _baseStrength = 1f;
    [SerializeField] private float _gustStrength = 0.5f;
    [SerializeField] private float _gustFrequency = 0.5f;
    [SerializeField] private float _wakeStrength = 1f;

    private void LateUpdate()
    {
        float gust = Mathf.PerlinNoise(Time.time * _gustFrequency, 0f) * 2f - 1f;
        Vector3 wind = _baseDirection.normalized * (_baseStrength + gust * _gustStrength);

        Vector3 flat = _player.linearVelocity;
        flat.y = 0f;
        wind -= flat * _wakeStrength;

        _wind.Set(wind);
        Shader.SetGlobalVector(GlobalWindId, wind);
    }
}
