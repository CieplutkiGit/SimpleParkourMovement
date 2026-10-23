#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Produces persistent URP materials, woven-fabric maps, Mesh assets and
/// two-level LOD prefabs from the runtime prototype geometry. Generated assets
/// are deterministic and editable; this is an art-pipeline *starting point*,
/// not a substitute for a sculpted, UV-baked studio character garment.
/// </summary>
public static class HeadwearAssetBaker
{
    private const string Folder = "Assets/Art/Headwear/Baked";

    [MenuItem("Tools/Parkour/Build Headwear Assets")]
    public static void Bake()
    {
        CreateFolderTree(Folder);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("Headwear baker needs Universal Render Pipeline/Lit.");
            return;
        }

        Texture2D albedo = CreateWeaveTexture(Folder + "/Fabric_Weave_Albedo.png", false);
        Texture2D normal = CreateWeaveTexture(Folder + "/Fabric_Weave_Normal.png", true);
        Material fabric = MakeMaterial(shader, "Mat_FabricCharcoal", new Color(0.31f, 0.34f, 0.38f),
                                       albedo, normal, 0.17f, 0f, 0.65f);
        Material trim = MakeMaterial(shader, "Mat_RubberizedTrim", new Color(0.045f, 0.055f, 0.07f),
                                     albedo, normal, 0.34f, 0f, 0.18f);
        Material accent = MakeMaterial(shader, "Mat_AccentOrange", new Color(0.91f, 0.25f, 0.075f),
                                       albedo, normal, 0.23f, 0.05f, 0.22f);

        BuildPrefab("TechnicalCap", fabric, trim, accent, true);
        BuildPrefab("RaisedHood", fabric, trim, accent, false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Headwear: baked URP textile materials, texture maps, Mesh assets and LOD prefabs in " + Folder);
    }

    [MenuItem("Tools/Parkour/Build and Assign Headwear to Open Scene")]
    public static void BakeAndAssign()
    {
        Bake();

        HeadwearController controller = UnityEngine.Object.FindFirstObjectByType<HeadwearController>();
        if (controller == null)
        {
            Debug.LogWarning("Headwear: open Main.unity before Build and Assign. The prefabs were baked but not assigned.");
            return;
        }

        GameObject cap = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/TechnicalCap.prefab");
        GameObject hood = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/RaisedHood.prefab");
        if (cap == null || hood == null)
        {
            Debug.LogError("Headwear: generated prefabs are missing. Nothing assigned.");
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("_capPrefab").objectReferenceValue = cap;
        serialized.FindProperty("_hoodPrefab").objectReferenceValue = hood;
        serialized.FindProperty("_fabric").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Mat_FabricCharcoal.mat");
        serialized.FindProperty("_trim").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Mat_RubberizedTrim.mat");
        serialized.FindProperty("_accent").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Mat_AccentOrange.mat");
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("Headwear: assigned baked cap and hood prefabs to " + controller.name +
                  ". Save the scene to persist these references.");
    }

    private static void BuildPrefab(string name, Material fabric, Material trim, Material accent, bool isCap)
    {
        GameObject root = new GameObject(name);
        List<Mesh> temporary = new List<Mesh>();

        try
        {
            // Prefer real, editable OBJ source committed to the repository.
            // Fallback generator remains available if the source is not imported.
            string sourcePath = "Assets/Art/Headwear/Source/" + name + ".obj";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source != null)
            {
                GameObject model = UnityEngine.Object.Instantiate(source, root.transform, false);
                model.name = name + "_Source";
                if (PrefabUtility.IsPartOfPrefabInstance(model))
                    PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely,
                                                       InteractionMode.AutomatedAction);
                foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        string key = materials[i] != null ? materials[i].name.ToUpperInvariant() :
                            renderer.gameObject.name.ToUpperInvariant();
                        if (key.Contains("ACCENT") || key.Contains("STITCH") || key.Contains("MARK"))
                            materials[i] = accent;
                        else if (key.Contains("TRIM") || key.Contains("BRIM") ||
                                 key.Contains("BAND") || key.Contains("EYELET") ||
                                 key.Contains("SEAM") || key.Contains("BUTTON") ||
                                 key.Contains("BACK_ADJUSTER"))
                            materials[i] = trim;
                        else
                            materials[i] = fabric;
                    }
                    renderer.sharedMaterials = materials;
                }
            }
            else if (isCap)
                ProceduralHeadwearMeshes.BuildTechnicalCap(root.transform, fabric, trim, accent, temporary);
            else
                ProceduralHeadwearMeshes.BuildRaisedHood(root.transform, fabric, trim, accent, temporary);

            MeshRenderer[] fullRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (fullRenderers.Length == 0)
                throw new InvalidOperationException("Imported headwear source contained no renderers: " + sourcePath);

            HashSet<string> usedMeshPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (MeshRenderer renderer in fullRenderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException("Headwear mesh part is missing its MeshFilter: " + renderer.name);

                string basePath = Folder + "/" + name + "_" + SafeName(renderer.name);
                string path = basePath + ".asset";
                int duplicate = 2;
                while (!usedMeshPaths.Add(path))
                    path = basePath + "_" + duplicate++ + ".asset";

                Mesh oldAsset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                Mesh copy = UnityEngine.Object.Instantiate(filter.sharedMesh);
                copy.name = name + "_" + renderer.name;
                if (oldAsset == null)
                {
                    AssetDatabase.CreateAsset(copy, path);
                    filter.sharedMesh = copy;
                }
                else
                {
                    // Preserve the original GUID so prefab links survive rebaking.
                    oldAsset.Clear();
                    oldAsset.indexFormat = copy.indexFormat;
                    oldAsset.vertices = copy.vertices;
                    oldAsset.uv = copy.uv;
                    oldAsset.normals = copy.normals;
                    oldAsset.tangents = copy.tangents;
                    oldAsset.subMeshCount = copy.subMeshCount;
                    for (int submesh = 0; submesh < copy.subMeshCount; submesh++)
                        oldAsset.SetTriangles(copy.GetTriangles(submesh), submesh);
                    oldAsset.RecalculateBounds();
                    EditorUtility.SetDirty(oldAsset);
                    filter.sharedMesh = oldAsset;
                    UnityEngine.Object.DestroyImmediate(copy);
                }
            }

            // Simplified visual LOD: reuse crown/brim on the cap, shell/lining
            // on the hood and omit small seams and embroidery at distance.
            GameObject lodRoot = new GameObject("LOD1");
            lodRoot.transform.SetParent(root.transform, false);
            List<Renderer> farRenderers = new List<Renderer>();

            foreach (MeshRenderer original in fullRenderers)
            {
                string partName = original.name;
                string key = partName.ToLowerInvariant();
                bool keep = isCap
                    ? key.Contains("crown") || key.Contains("brim") ||
                      key.Contains("lower_band") || key.Contains("lower band")
                    : key.Contains("outer") || key.Contains("inner");
                if (!keep)
                    continue;

                GameObject part = new GameObject(partName + "_LOD1");
                part.transform.SetParent(lodRoot.transform, false);
                MeshFilter filter = part.AddComponent<MeshFilter>();
                filter.sharedMesh = original.GetComponent<MeshFilter>().sharedMesh;
                MeshRenderer renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = original.sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                farRenderers.Add(renderer);
            }

            LODGroup lod = root.AddComponent<LODGroup>();
            lod.SetLODs(new[]
            {
                new LOD(0.35f, Array.ConvertAll<MeshRenderer, Renderer>(fullRenderers, item => item)),
                new LOD(0.08f, farRenderers.ToArray())
            });
            lod.RecalculateBounds();

            string prefabPath = Folder + "/" + name + ".prefab";
            // Saving to the same prefab path preserves its GUID on rebake.
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (saved == null)
                throw new InvalidOperationException("Failed to save " + prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (Mesh mesh in temporary)
                if (mesh != null)
                    UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    private static Material MakeMaterial(Shader shader, string name, Color tint, Texture2D baseMap,
                                          Texture2D normal, float smoothness, float metallic, float bumpStrength)
    {
        string path = Folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool newlyCreated = material == null;
        if (newlyCreated)
            material = new Material(shader) { name = name };
        else
            material.shader = shader;
        material.SetColor("_BaseColor", tint);
        material.SetTexture("_BaseMap", baseMap);
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_BumpScale", bumpStrength);
        material.EnableKeyword("_NORMALMAP");
        // For close camera views, slightly higher-resolution texture repeats
        // help break up smooth, plastic-looking synthetic surfaces.
        material.SetTextureScale("_BaseMap", new Vector2(2.5f, 2.5f));
        material.SetTextureScale("_BumpMap", new Vector2(2.5f, 2.5f));
        if (newlyCreated)
            AssetDatabase.CreateAsset(material, path);
        else
            EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D CreateWeaveTexture(string path, bool normalMap)
    {
        const int size = 512;
        Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false, normalMap);
        generated.name = Path.GetFileNameWithoutExtension(path);
        generated.wrapMode = TextureWrapMode.Repeat;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float h = WeaveHeight(x, y);
            if (normalMap)
            {
                float dhdx = WeaveHeight(x + 1, y) - WeaveHeight(x - 1, y);
                float dhdy = WeaveHeight(x, y + 1) - WeaveHeight(x, y - 1);
                Vector3 n = new Vector3(-dhdx * 1.8f, -dhdy * 1.8f, 1f).normalized;
                pixels[y * size + x] = new Color(
                    n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            else
            {
                float value = Mathf.Clamp01(0.65f + h * 0.25f + 0.045f * (Hash(x, y) - 0.5f));
                pixels[y * size + x] = new Color(value, value * 0.96f, value * 0.92f, 1f);
            }
        }
        generated.SetPixels32(pixels);
        generated.Apply(false, false);
        byte[] png = generated.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(generated);

        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Cannot import generated texture " + path);

        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normalMap;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static float WeaveHeight(int x, int y)
    {
        // Two orthogonal yarn directions plus irregular fibres, all deterministic.
        float horizontal = Mathf.Cos((y % 16) * Mathf.PI * 2f / 16f);
        float vertical = Mathf.Cos((x % 16) * Mathf.PI * 2f / 16f);
        float warp = Mathf.Pow(0.5f + 0.5f * vertical, 3f);
        float weft = Mathf.Pow(0.5f + 0.5f * horizontal, 3f);
        float overUnder = ((x / 16 + y / 16) & 1) == 0 ? 1f : -1f;
        return (warp + weft) * 0.32f + (warp - weft) * overUnder * 0.1f;
    }

    private static float Hash(int x, int y)
    {
        unchecked
        {
            uint value = (uint)(x * 374761393 + y * 668265263);
            value = (value ^ (value >> 13)) * 1274126177u;
            return (value ^ (value >> 16)) / (float)uint.MaxValue;
        }
    }

    private static string SafeName(string value)
    {
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        return new string(chars);
    }

    private static void CreateFolderTree(string path)
    {
        string[] segments = path.Split('/');
        string cursor = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = cursor + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cursor, segments[i]);
            cursor = next;
        }
    }
}
#endif
