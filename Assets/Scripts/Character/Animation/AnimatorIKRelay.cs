using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorIKRelay : MonoBehaviour
{
    private CharacterAnimator _target;

    private void Awake()
    {
        _target = GetComponentInParent<CharacterAnimator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_target != null)
            _target.RunIK(layerIndex);
    }
}
