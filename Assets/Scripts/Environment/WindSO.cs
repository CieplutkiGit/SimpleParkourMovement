using UnityEngine;

[CreateAssetMenu(menuName = "Game/WindSO")]
public class WindSO : ScriptableObject
{
    public Vector3 Current { get; private set; }

    public void Set(Vector3 value)
    {
        Current = value;
    }
}
