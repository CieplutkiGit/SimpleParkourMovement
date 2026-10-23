#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Import-and-rig smoke test. Use -executeMethod HeadwearBatchSmokeTest.RunBatch
/// in a licensed Unity 6 editor in batchmode. Failing checks return exit code 1.
/// Complements Python CI static OBJ checks; does not replace visual art review.
/// </summary>
public static class HeadwearBatchSmokeTest
{
    private const string Baked = "Assets/Art/Headwear/Baked/";
    private const string DemoScene = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Parkour/Run Unity Headwear Smoke Test")]
    public static void RunFromEditor()
    {
        try
        {
            RunChecks();
            Debug.Log("Headwear Unity import/skeleton smoke check PASSED.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Headwear Unity import/skeleton smoke check FAILED.");
        }
    }

    public static void RunBatch()
    {
        int exitCode = 1;
        try
        {
            RunChecks();
            Debug.Log("Headwear Unity import/skeleton smoke check PASSED.");
            exitCode = 0;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Headwear Unity import/skeleton smoke check FAILED.");
        }
        finally
        {
            EditorApplication.Exit(exitCode);
        }
    }

    private static void RunChecks()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // This catches broken OBJ importer metadata, shader API errors and
        // failed asset saving before trying to load the character scene.
        HeadwearAssetBaker.Bake();

        GameObject cap = LoadPrefab("TechnicalCap");
        GameObject hood = LoadPrefab("RaisedHood");
        ValidatePrefab(cap, "TechnicalCap", 3);
        ValidatePrefab(hood, "RaisedHood", 2);

        EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
        HeadwearController controller = UnityEngine.Object.FindFirstObjectByType<HeadwearController>();
        if (controller == null)
            throw new InvalidOperationException("Main.unity has no HeadwearController.");

        Animator animator = controller.GetComponentInChildren<Animator>();
        if (animator == null)
            throw new InvalidOperationException("Player has no Animator.");

        Transform head = null;
        if (animator.isHuman && animator.avatar != null && animator.avatar.isValid)
            head = animator.GetBoneTransform(HumanBodyBones.Head);

        if (head == null)
        {
            foreach (Transform candidate in animator.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == "Head" ||
                    candidate.name.EndsWith(":Head", StringComparison.Ordinal))
                {
                    head = candidate;
                    break;
                }
            }
        }
        if (head == null)
            throw new InvalidOperationException("No head attachment bone in Main.unity.");

        // Simulate the static attachment / bone orientation in edit mode.
        GameObject temp = UnityEngine.Object.Instantiate(cap, head, false);
        try
        {
            temp.transform.position = head.position +
                                      animator.transform.TransformDirection(new Vector3(0, 0.16f, 0.015f));
            temp.transform.rotation = animator.transform.rotation;
            if (Vector3.Dot(temp.transform.forward, animator.transform.forward) < 0.99f)
                throw new InvalidOperationException("Cap brim does not follow character forward.");

            foreach (Renderer renderer in temp.GetComponentsInChildren<Renderer>(true))
            {
                Bounds bounds = renderer.bounds;
                if (!IsFinite(bounds.center) || !IsFinite(bounds.extents))
                    throw new InvalidOperationException("Headwear renderer has invalid bounds: " + renderer.name);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temp);
        }
    }

    private static bool IsFinite(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
               !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    private static GameObject LoadPrefab(string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Baked + name + ".prefab");
        if (prefab == null)
            throw new InvalidOperationException("Cannot find baked headwear prefab: " + name);
        return prefab;
    }

    private static void ValidatePrefab(GameObject prefab, string name, int minimumMeshParts)
    {
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length < minimumMeshParts)
            throw new InvalidOperationException(name + " has too few mesh parts.");

        int triangles = 0;
        foreach (MeshFilter filter in filters)
        {
            Mesh m = filter.sharedMesh;
            if (m == null || m.vertexCount == 0 || !AssetDatabase.Contains(m))
                throw new InvalidOperationException("Headwear has no persistent mesh asset: " + filter.name);
            for (int sub = 0; sub < m.subMeshCount; sub++)
                triangles += m.GetTriangles(sub).Length / 3;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterials.Length == 0)
                throw new InvalidOperationException("Headwear has no material: " + filter.name);
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null ||
                    material.shader.name != "Universal Render Pipeline/Lit")
                    throw new InvalidOperationException("Headwear missing URP/Lit: " + filter.name);
                if (material.GetTexture("_BumpMap") == null ||
                    material.GetTexture("_BaseMap") == null)
                    throw new InvalidOperationException("Headwear missing fabric maps: " + filter.name);
            }
        }

        LODGroup lod = prefab.GetComponent<LODGroup>();
        if (lod == null || lod.GetLODs().Length != 2)
            throw new InvalidOperationException(name + " has no two-stage distance LOD.");

        LOD[] levels = lod.GetLODs();
        int[] lodTriangles = new int[levels.Length];
        for (int index = 0; index < levels.Length; index++)
        {
            if (levels[index].renderers.Length == 0 || levels[index].renderers.Length > 3)
                throw new InvalidOperationException(name + " LOD" + index +
                    " exceeds 3 material-batched renderers.");
            foreach (Renderer renderer in levels[index].renderers)
            {
                MeshFilter filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException(name + " LOD" + index + " is missing MeshFilter.");
                Mesh mesh = filter.sharedMesh;
                for (int part = 0; part < mesh.subMeshCount; part++)
                    lodTriangles[index] += mesh.GetTriangles(part).Length / 3;
            }
        }
        if (lodTriangles[0] < 100 || lodTriangles[1] > lodTriangles[0])
            throw new InvalidOperationException(name + " invalid triangle counts across LODs.");
        Debug.Log(name + ": LOD0=" + lodTriangles[0] + " triangles/" +
                  levels[0].renderers.Length + " renderers, LOD1=" + lodTriangles[1] +
                  " triangles/" + levels[1].renderers.Length + " renderers.");
    }
}
#endif
