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

            // Instantiate the real strap component in a disposable editor hierarchy.
            // Its generated mesh is checked by the same attribute/triangle validation.
            Transform strapRoot = new GameObject("strap simulation smoke test").transform;
            strapRoot.SetParent(cap, false);
            strapRoot.localPosition = new Vector3(0.06f, -0.018f, -0.187f);
            HeadwearStrapDynamics strap = strapRoot.gameObject.AddComponent<HeadwearStrapDynamics>();
            strap.Configure(strapRoot, null, null, fabric);
            Mesh strapMesh = strapRoot.GetComponent<MeshFilter>()?.sharedMesh;
            if (strapMesh == null)
            {
                Debug.LogError("Headwear validation: dynamic strap did not generate a mesh.");
                valid = false;
            }
            else
            {
                meshes.Add(strapMesh);
                strap.ResetSimulation();
                if (strapMesh.bounds.size.sqrMagnitude < 1e-8f)
                {
                    Debug.LogError("Headwear validation: dynamic strap has no usable bounds.");
                    valid = false;
                }
            }

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

            // Baked assets are optional until Build and Assign is executed.
            CheckBakedPrefab("Assets/Art/Headwear/Baked/TechnicalCap.prefab", ref valid);
            CheckBakedPrefab("Assets/Art/Headwear/Baked/RaisedHood.prefab", ref valid);

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

                if (m.bounds.size.sqrMagnitude < 1e-8f)
                {
                    Debug.LogError("Headwear validation: empty or collapsed bounds on " + m.name);
                    valid = false;
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

    private static void CheckBakedPrefab(string path, ref bool valid)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning("Headwear validation: prefab not baked at " + path +
                             ". Run Tools/Parkour/Build and Assign Headwear to Open Scene.");
            return;
        }

        LODGroup lod = asset.GetComponent<LODGroup>();
        if (lod == null || lod.GetLODs().Length < 2)
        {
            Debug.LogError("Headwear validation: no valid LODs in " + path);
            valid = false;
        }

        MeshRenderer[] renderers = asset.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length < 3)
        {
            Debug.LogError("Headwear validation: missing mesh parts in " + path);
            valid = false;
        }

        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null ||
                !AssetDatabase.Contains(filter.sharedMesh))
            {
                Debug.LogError("Headwear validation: unsaved mesh on " + renderer.name);
                valid = false;
            }

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null ||
                    material.shader.name != "Universal Render Pipeline/Lit")
                {
                    Debug.LogError("Headwear validation: missing URP/Lit material on " + renderer.name);
                    valid = false;
                    continue;
                }

                if (material.GetTexture("_BaseMap") == null ||
                    material.GetTexture("_BumpMap") == null ||
                    !material.IsKeywordEnabled("_NORMALMAP"))
                {
                    Debug.LogError("Headwear validation: missing normal/albedo maps on " + renderer.name);
                    valid = false;
                }
            }
        }
    }
}
#endif
