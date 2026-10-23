using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rebind a skinned garment prefab exported against the SAME Y Bot rest pose
/// and skeleton to the animated character's bones. Unlike a rigid head socket,
/// a properly authored hood/jacket can blend weights across head, neck, chest
/// and shoulders. The asset and bindposes must already match this rig.
/// </summary>
[DisallowMultipleComponent]
public sealed class HeadwearSkinnedGarmentBinding : MonoBehaviour
{
    [SerializeField] private bool _logMissingBones = true;
    [Tooltip("Enable only for garments with genuinely animated offscreen cloth.")]
    [SerializeField] private bool _updateWhenOffscreen = false;

    /// <returns>True only if every renderer can be rebound without missing bones.</returns>
    public bool BindTo(Animator characterAnimator)
    {
        if (characterAnimator == null)
        {
            Debug.LogError("Headwear garment binding requires a character Animator.", this);
            return false;
        }

        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0)
            return true;

        // Exclude bones bundled into the garment prefab. They are NOT the live
        // animated character skeleton even when their names happen to match.
        Dictionary<string, Transform> byName = new Dictionary<string, Transform>(StringComparer.Ordinal);
        Dictionary<string, Transform> byShortName = new Dictionary<string, Transform>(StringComparer.Ordinal);
        HashSet<string> ambiguous = new HashSet<string>(StringComparer.Ordinal);

        foreach (Transform bone in characterAnimator.GetComponentsInChildren<Transform>(true))
        {
            if (bone.IsChildOf(transform))
                continue;

            if (!byName.ContainsKey(bone.name))
                byName.Add(bone.name, bone);

            string key = ShortName(bone.name);
            if (byShortName.ContainsKey(key))
                ambiguous.Add(key);
            else
                byShortName.Add(key, bone);
        }

        // Stage all mappings first. A partially remapped garment would deform
        // unpredictably and potentially disappear during parkour.
        Transform[][] newBones = new Transform[renderers.Length][];
        Transform[] newRoots = new Transform[renderers.Length];
        bool valid = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            Transform[] originalBones = renderer.bones;
            if (renderer.sharedMesh == null || originalBones == null || originalBones.Length == 0 ||
                renderer.sharedMesh.bindposes.Length != originalBones.Length)
            {
                if (_logMissingBones)
                    Debug.LogError("Headwear garment has no valid skinned mesh/bindposes: " + renderer.name, renderer);
                valid = false;
                continue;
            }

            newBones[i] = new Transform[originalBones.Length];
            for (int b = 0; b < originalBones.Length; b++)
            {
                Transform source = originalBones[b];
                if (source == null ||
                    !TryMatch(source.name, byName, byShortName, ambiguous, out newBones[i][b]))
                {
                    if (_logMissingBones)
                        Debug.LogError("Cannot map garment bone '" + (source != null ? source.name : "<null>") +
                                       "' on " + renderer.name + ". Export it from the project's Y Bot rest skeleton.", renderer);
                    valid = false;
                }
            }

            if (renderer.rootBone != null)
            {
                if (!TryMatch(renderer.rootBone.name, byName, byShortName, ambiguous, out newRoots[i]))
                {
                    if (_logMissingBones)
                        Debug.LogError("Cannot map garment rootBone on " + renderer.name, renderer);
                    valid = false;
                }
            }
            else
                newRoots[i] = characterAnimator.transform;
        }

        if (!valid)
            return false;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].bones = newBones[i];
            renderers[i].rootBone = newRoots[i];
            renderers[i].updateWhenOffscreen = _updateWhenOffscreen;
        }

        return true;
    }

    private static bool TryMatch(string fullName, Dictionary<string, Transform> exact,
                                 Dictionary<string, Transform> shortNames, HashSet<string> ambiguous,
                                 out Transform match)
    {
        if (exact.TryGetValue(fullName, out match))
            return true;

        string key = ShortName(fullName);
        if (!ambiguous.Contains(key) && shortNames.TryGetValue(key, out match))
            return true;

        match = null;
        return false;
    }

    private static string ShortName(string fullName)
    {
        int colon = fullName.LastIndexOf(':');
        int pipe = fullName.LastIndexOf('|');
        int index = Mathf.Max(colon, pipe);
        return index < 0 ? fullName : fullName.Substring(index + 1);
    }
}
