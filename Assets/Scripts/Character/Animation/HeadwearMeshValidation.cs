#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only geometry smoke test. No test assembly or third-party packages required.</summary>
public static class HeadwearMeshValidation
{
    [MenuItem("Tools/Parkour/Validate Headwear Geometry")]
    private static void ValidateMeshes()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("Headwear mesh validation: cannot locate a compatible shader.");
            return;
        }

        GameObject root = new GameObject("TEMP - Headwear Geometry Validation");
        Material fabric = new Material(shader);
        List<Mesh> meshes = new List<Mesh>();
        bool valid = true;
        int faces = 0;

        try
        {
            Transform cap = new GameObject("cap").transform;
            cap.SetParent(root.transform, false);
            ProceduralHeadwearMeshes.BuildTechnicalCap(cap, fabric, fabric, fabric, meshes);

            Transform hood = new GameObject("hood").transform;
            hood.SetParent(root.transform, false);
            ProceduralHeadwearMeshes.BuildRaisedHood(hood, fabric, fabric, fabric, meshes);

            // When Main.unity is open, also verify that this demo's player is wired.
            HeadwearController controller = Object.FindFirstObjectByType<HeadwearController>();
            if (controller != null)
            {
                Animator avatar = controller.GetComponentInChildren<Animator>();
                if (avatar == null)
                {
                    Debug.LogError("Headwear validation: Player has no child Animator.");
                    valid = false;
                }
                else
                {
                    Transform head = null;
                    if (avatar.isHuman && avatar.avatar != null && avatar.avatar.isValid)
                        head = avatar.GetBoneTransform(HumanBodyBones.Head);

                    if (head == null)
                    {
                        foreach (Transform bone in avatar.GetComponentsInChildren<Transform>(true))
                        {
                            if (bone.name == "Head" || bone.name.EndsWith(":Head", System.StringComparison.Ordinal))
                            {
                                head = bone;
                                break;
                            }
                        }
                    }

                    if (head == null)
                    {
                        Debug.LogError("Headwear validation: Cannot resolve the head attachment bone.");
                        valid = false;
                    }

                    if (controller.GetComponentInChildren<SkinnedStripMesh>(true) == null)
                        Debug.LogWarning("Headwear validation: Original scarf not found. Style cycle will skip showing it.");
                }
            }
            else
            {
                Debug.LogWarning("Headwear validation: No HeadwearController in the open scene; only mesh checks were performed.");
            }

            if (meshes.Count < 15)
            {
                Debug.LogError("Headwear validation: expected all cap/hood parts to be generated.");
                valid = false;
            }

            foreach (Mesh m in meshes)
            {
                if (m == null || m.vertexCount == 0 || m.triangles.Length == 0)
                {
                    Debug.LogError("Headwear validation: missing mesh geometry.");
                    valid = false;
                    continue;
                }

                Vector3[] vertices = m.vertices;
                Vector3[] normals = m.normals;
                Vector2[] uvs = m.uv;
                int[] triangles = m.triangles;

                if (normals.Length != vertices.Length || uvs.Length != vertices.Length || triangles.Length % 3 != 0)
                {
                    Debug.LogError("Headwear validation: invalid vertex attributes on " + m.name);
                    valid = false;
                }

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    if (a < 0 || b < 0 || c < 0 || a >= vertices.Length || b >= vertices.Length || c >= vertices.Length)
                    {
                        Debug.LogError("Headwear validation: triangle index out of range in " + m.name);
                        valid = false;
                        break;
                    }

                    float area2 = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).sqrMagnitude;
                    if (area2 < 1e-16f)
                    {
                        Debug.LogWarning("Headwear validation: degenerate triangle in " + m.name);
                        valid = false;
                        break;
                    }

                    faces++;
                }
            }
            if (valid)
                Debug.Log("Headwear geometry validation passed: " + meshes.Count + " submeshes, " + faces + " triangles.");
            else
                Debug.LogError("Headwear geometry validation FAILED. Check errors above.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            foreach (Mesh mesh in meshes)
                if (mesh != null) Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(fabric);
        }
    }
}
