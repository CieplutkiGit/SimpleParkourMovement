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
    private const string TextureSources = "Assets/Art/Headwear/Source/Textures";

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

        // Source textures are committed as PNGs and are available immediately
        // on checkout. The original deterministic generator remains a fallback.
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureSources + "/Fabric_Weave_Albedo.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureSources + "/Fabric_Weave_Normal.png");
        Texture2D occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureSources + "/Fabric_Weave_Occlusion.png");
        if (albedo == null)
            albedo = CreateWeaveTexture(Folder + "/Fabric_Weave_Albedo.png", false);
        if (normal == null)
            normal = CreateWeaveTexture(Folder + "/Fabric_Weave_Normal.png", true);

        Material fabric = MakeMaterial(shader, "Mat_FabricCharcoal", new Color(0.31f, 0.34f, 0.38f),
                                       albedo, normal, occlusion, 0.17f, 0f, 0.65f);
        Material trim = MakeMaterial(shader, "Mat_RubberizedTrim", new Color(0.045f, 0.055f, 0.07f),
                                     albedo, normal, occlusion, 0.34f, 0f, 0.18f);
        Material accent = MakeMaterial(shader, "Mat_AccentOrange", new Color(0.91f, 0.25f, 0.075f),
                                       albedo, normal, occlusion, 0.23f, 0.05f, 0.22f);

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

    // Mesh.CombineMeshes is run in the Editor only. Instead of shipping one
    // renderer per tiny seam or eyelet, bake into at most 3 material batches
    // per LOD. Source OBJ remains untouched and fully editable in Blender.
    private static void BuildPrefab(string name, Material fabric, Material trim, Material accent, bool isCap)
    {
        GameObject root = new GameObject(name);
        List<Mesh> temporary = new List<Mesh>();
        try
        {
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
                        string key = ((materials[i] != null ? materials[i].name : "") +
                            "_" + renderer.gameObject.name).ToUpperInvariant();
                        if (key.Contains("ACCENT") || key.Contains("STITCH") ||
                            key.Contains("MARK") || key.Contains("DART"))
                            materials[i] = accent;
                        else if (key.Contains("TRIM") || key.Contains("BRIM") ||
                                 key.Contains("BAND") || key.Contains("EYELET") ||
                                 key.Contains("SEAM") || key.Contains("INNER") ||
                                 key.Contains("BUTTON") || key.Contains("BACK_ADJUSTER"))
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

            // Snapshot before creating output LODs; excludes the generated renderers.
            MeshRenderer[] authoredRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (authoredRenderers.Length == 0)
                throw new InvalidOperationException("Headwear source has no renderers: " + sourcePath);

            Material[] palette = { fabric, trim, accent };
            List<CombineInstance>[] lod0 = NewBuckets(palette.Length);
            List<CombineInstance>[] lod1 = NewBuckets(palette.Length);
            bool hasSelectedFarParts = false;

            foreach (MeshRenderer renderer in authoredRenderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException("Headwear source missing MeshFilter: " + renderer.name);

                Mesh mesh = filter.sharedMesh;
                Matrix4x4 toRoot = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Material[] materialSlots = renderer.sharedMaterials;
                bool keepFar = IsSilhouettePiece(renderer.name, isCap);

                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    // A missing material slot is a source asset bug, not a reason
                    // to silently discard thousands of triangles.
                    if (submesh >= materialSlots.Length)
                        throw new InvalidOperationException("Not enough material slots: " + renderer.name);
                    Material material = materialSlots[submesh];
                    int bucket = Array.IndexOf(palette, material);
                    if (bucket < 0)
                        throw new InvalidOperationException("Unsupported material in " + renderer.name);
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = mesh, subMeshIndex = submesh, transform = toRoot
                    };
                    lod0[bucket].Add(ci);
                    if (keepFar)
                    {
                        lod1[bucket].Add(ci);
                        hasSelectedFarParts = true;
                    }
                }
            }

            // If the OBJ importer merges every group into one renderer, preserve
            // the geometry rather than making the distant cap invisible.
            if (!hasSelectedFarParts)
            {
                for (int i = 0; i < palette.Length; i++)
                    lod1[i].AddRange(lod0[i]);
                Debug.LogWarning(name + ": OBJ parts merged by importer; LOD1 currently uses full mesh. " +
                                 "Split the source into objects for silhouette-only LOD.");
            }

            GameObject near = new GameObject("LOD0");
            near.transform.SetParent(root.transform, false);
            GameObject far = new GameObject("LOD1");
            far.transform.SetParent(root.transform, false);
            Renderer[] nearRenderers = BakeBatches(name, "LOD0", near.transform, lod0, palette, true);
            Renderer[] farRenderers = BakeBatches(name, "LOD1", far.transform, lod1, palette, false);
            if (nearRenderers.Length == 0 || farRenderers.Length == 0)
                throw new InvalidOperationException("Baked headwear LODs have no visible renderers.");

            // Source meshes are not part of the final prefab: otherwise they
            // would render *in addition* to the combined geometry.
            for (int i = authoredRenderers.Length - 1; i >= 0; i--)
                authoredRenderers[i].enabled = false;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child != near.transform && child != far.transform)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            LODGroup lod = root.AddComponent<LODGroup>();
            lod.fadeMode = LODFadeMode.None; // No double-draw during transitions.
            lod.SetLODs(new[]
            {
                new LOD(0.35f, nearRenderers),
                new LOD(0.025f, farRenderers)
            });
            lod.RecalculateBounds();

            string prefabPath = Folder + "/" + name + ".prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (saved == null)
                throw new InvalidOperationException("Could not write headwear prefab: " + prefabPath);

            Debug.Log(name + " draw calls: LOD0=" + nearRenderers.Length +
                      " LOD1=" + farRenderers.Length + " (one renderer per material).");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (Mesh mesh in temporary)
                if (mesh != null)
                    UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    private static List<CombineInstance>[] NewBuckets(int length)
    {
        List<CombineInstance>[] buckets = new List<CombineInstance>[length];
        for (int i = 0; i < buckets.Length; i++)
            buckets[i] = new List<CombineInstance>();
        return buckets;
    }

    private static bool IsSilhouettePiece(string name, bool isCap)
    {
        string key = name.ToLowerInvariant();
        return isCap
            ? key.Contains("crown") || key.Contains("brim") || key.Contains("lower_band") ||
              key.Contains("lower band") || key.Contains("visor") && !key.Contains("stitch")
            : key.Contains("outer") || key.Contains("inner") || key.Contains("shell") ||
              key.Contains("lining");
    }

    private static Renderer[] BakeBatches(string name, string lodName, Transform parent,
                                          List<CombineInstance>[] batches, Material[] materials,
                                          bool castShadows)
    {
        List<Renderer> renderers = new List<Renderer>();
        for (int i = 0; i < materials.Length; i++)
        {
            if (batches[i].Count == 0)
                continue;

            Mesh mesh = new Mesh { name = name + "_" + lodName + "_Batch" + i };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(batches[i].ToArray(), true, true, false);

            // Stable asset paths/GUIDs: re-baking cannot break prefab references.
            string path = Folder + "/" + mesh.name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                mesh = existing;
            }
            else
                AssetDatabase.CreateAsset(mesh, path);

            GameObject part = new GameObject("Batch" + i);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials[i];
            renderer.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;
            renderers.Add(renderer);
        }
        return renderers.ToArray();
    }

    private static Material MakeMaterial(Shader shader, string name, Color tint, Texture2D baseMap,
                                          Texture2D normal, Texture2D occlusion,
                                          float smoothness, float metallic, float bumpStrength)
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
        if (occlusion != null)
        {
            material.SetTexture("_OcclusionMap", occlusion);
            material.SetFloat("_OcclusionStrength", 0.42f);
            material.EnableKeyword("_OCCLUSIONMAP");
        }
        else
        {
            material.SetTexture("_OcclusionMap", null);
            material.DisableKeyword("_OCCLUSIONMAP");
        }
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_BumpScale", bumpStrength);
        material.EnableKeyword("_NORMALMAP");
        // For close camera views, slightly higher-resolution texture repeats
        // help break up smooth, plastic-looking synthetic surfaces.
        material.SetTextureScale("_BaseMap", new Vector2(2.5f, 2.5f));
        material.SetTextureScale("_BumpMap", new Vector2(2.5f, 2.5f));
        material.SetTextureScale("_OcclusionMap", new Vector2(2.5f, 2.5f));
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
