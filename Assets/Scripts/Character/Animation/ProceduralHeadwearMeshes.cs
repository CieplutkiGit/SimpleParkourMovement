using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime placeholder geometry with intentional silhouette, piping, panel seams and a
/// separate brim. Production quality requires swapping these for authored meshes/UVs.
/// No Unity primitives, colliders, or per-frame mesh allocations are used.
/// </summary>
public static class ProceduralHeadwearMeshes
{
    private const int CrownSides = 48;
    private const int PipeSides = 6;
    private const int HoodSides = 32;
    private const float DegToRad = Mathf.Deg2Rad;

    private struct Ring
    {
        public float y, width, depth, z;
        public Ring(float y, float width, float depth, float z = 0f)
        {
            this.y = y;
            this.width = width;
            this.depth = depth;
            this.z = z;
        }
    }

    private sealed class Geometry
    {
        public readonly List<Vector3> positions = new List<Vector3>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> triangles = new List<int>();

        public int Vertex(Vector3 p, Vector2 uv)
        {
            int i = positions.Count;
            positions.Add(p);
            uvs.Add(uv);
            return i;
        }

        public void Quad(int a, int b, int c, int d, bool flipped = false)
        {
            if (flipped)
            {
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
            else
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }

        public Mesh ToMesh(string name)
        {
            Mesh m = new Mesh { name = name };
            m.SetVertices(positions);
            m.SetUVs(0, uvs);
            m.SetTriangles(triangles, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }
    }

    public static void BuildTechnicalCap(Transform root, Material fabric, Material trim, Material accent, List<Mesh> ownedMeshes)
    {
        Ring[] crown =
        {
            new Ring(-0.023f, 0.92f, 0.92f),
            new Ring(0.008f, 1f, 1f),
            new Ring(0.065f, 0.985f, 0.985f),
            new Ring(0.125f, 0.84f, 0.86f),
            new Ring(0.18f, 0.53f, 0.57f),
            new Ring(0.208f, 0.08f, 0.08f)
        };

        BuildCrown(root, crown, fabric, ownedMeshes);
        BuildBrim(root, trim, ownedMeshes);
        BuildBand(root, crown, trim, ownedMeshes);

        // Six panel seams, stitched into the curvature instead of drawn as flat decals.
        for (int panel = 0; panel < 6; panel++)
        {
            float angle = (panel / 6f) * Mathf.PI * 2f;
            Vector3[] seam = new Vector3[crown.Length];
            for (int i = 0; i < crown.Length; i++)
            {
                Ring ring = crown[i];
                seam[i] = new Vector3(
                    Mathf.Cos(angle) * ring.width * 0.182f * 1.009f,
                    ring.y + 0.002f,
                    Mathf.Sin(angle) * ring.depth * 0.194f * 1.009f);
            }
            BuildPipe(root, "Stitched crown panel " + panel, seam, 0.0022f, trim, ownedMeshes);
        }

        // A restrained technical chevron on the front panel; no brand/logo dependency.
        BuildPipe(root, "Embroidered chevron", new[]
        {
            new Vector3(-0.045f, 0.107f, 0.171f),
            new Vector3(0f, 0.087f, 0.189f),
            new Vector3(0.045f, 0.107f, 0.171f)
        }, 0.004f, accent, ownedMeshes);

        // Back adjustment tab, fixed to the crown so parkour poses cannot snag it.
        BuildPipe(root, "Rear closure tab", new[]
        {
            new Vector3(-0.055f, -0.014f, -0.188f),
            new Vector3(0f, -0.023f, -0.208f),
            new Vector3(0.055f, -0.014f, -0.188f)
        }, 0.007f, accent, ownedMeshes);

        // Button closes the crown apex and gives the seams an intentional endpoint.
        Vector3[] buttonLoop = new Vector3[CrownSides + 1];
        for (int i = 0; i <= CrownSides; i++)
        {
            float a = i * Mathf.PI * 2f / CrownSides;
            buttonLoop[i] = new Vector3(Mathf.Cos(a) * 0.012f, 0.211f, Mathf.Sin(a) * 0.012f);
        }
        BuildPipe(root, "Top crown button", buttonLoop, 0.004f, accent, ownedMeshes);
    }

    private static void BuildCrown(Transform root, Ring[] rings, Material material, List<Mesh> meshes)
    {
        Geometry g = new Geometry();
        for (int r = 0; r < rings.Length; r++)
        {
            Ring ring = rings[r];
            for (int s = 0; s <= CrownSides; s++)
            {
                float a = s * 2f * Mathf.PI / CrownSides;
                g.Vertex(new Vector3(Mathf.Cos(a) * ring.width * 0.182f,
                                     ring.y,
                                     Mathf.Sin(a) * ring.depth * 0.194f),
                         new Vector2((float)s / CrownSides, (float)r / (rings.Length - 1)));
            }
        }

        int stride = CrownSides + 1;
        for (int r = 0; r < rings.Length - 1; r++)
        for (int s = 0; s < CrownSides; s++)
        {
            int a = r * stride + s;
            g.Quad(a, a + stride, a + 1, a + stride + 1);
        }

        int apex = g.Vertex(new Vector3(0f, 0.210f, 0f), new Vector2(0.5f, 1f));
        int topRing = (rings.Length - 1) * stride;
        for (int s = 0; s < CrownSides; s++)
        {
            g.triangles.Add(topRing + s);
            g.triangles.Add(apex);
            g.triangles.Add(topRing + s + 1);
        }

        AddPart(root, "Six-panel crown", g, material, meshes);
    }

    private static void BuildBrim(Transform root, Material material, List<Mesh> meshes)
    {
        const int rows = 12;
        const int widthSections = 20;
        Geometry g = new Geometry();
        int stride = widthSections + 1;
        int layerStride = (rows + 1) * stride;

        for (int layer = 0; layer < 2; layer++)
        for (int r = 0; r <= rows; r++)
        {
            float t = (float)r / rows;
            float z = Mathf.Lerp(0.12f, 0.39f, t);
            float halfWidth = (0.145f + 0.047f * Mathf.Sin(t * Mathf.PI)) * (1f - 0.11f * t);
            for (int c = 0; c <= widthSections; c++)
            {
                float u = c / (float)widthSections * 2f - 1f;
                float y = 0.008f - 0.048f * t + 0.036f * u * u * t;
                if (layer == 1)
                    y -= 0.012f;
                g.Vertex(new Vector3(u * halfWidth, y, z), new Vector2((u + 1f) * 0.5f, t));
            }
        }

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < widthSections; c++)
        {
            int top = r * stride + c;
            g.Quad(top, top + stride, top + 1, top + stride + 1);
            int bottom = layerStride + top;
            g.Quad(bottom, bottom + stride, bottom + 1, bottom + stride + 1, true);
        }

        // Connect the upper and lower surfaces: the brim has real thickness.
        for (int r = 0; r < rows; r++)
        {
            int left = r * stride;
            int right = left + widthSections;
            g.Quad(left, layerStride + left, left + stride, layerStride + left + stride);
            g.Quad(right, right + stride, layerStride + right, layerStride + right + stride);
        }
        for (int c = 0; c < widthSections; c++)
        {
            g.Quad(c, c + 1, layerStride + c, layerStride + c + 1);
            int front = rows * stride + c;
            g.Quad(front, layerStride + front, front + 1, layerStride + front + 1);
        }

        AddPart(root, "Curved reinforced brim", g, material, meshes);
    }

    private static void BuildBand(Transform root, Ring[] crown, Material material, List<Mesh> meshes)
    {
        Geometry g = new Geometry();
        for (int ring = 0; ring < 2; ring++)
        for (int s = 0; s <= CrownSides; s++)
        {
            float a = s * 2f * Mathf.PI / CrownSides;
            float y = ring == 0 ? -0.018f : 0.012f;
            g.Vertex(new Vector3(Mathf.Cos(a) * 0.184f, y, Mathf.Sin(a) * 0.196f),
                     new Vector2((float)s / CrownSides, ring));
        }
        for (int s = 0; s < CrownSides; s++)
            g.Quad(s, s + CrownSides + 1, s + 1, s + CrownSides + 2);
        AddPart(root, "Reinforced lower band", g, material, meshes);
    }

    public static void BuildRaisedHood(Transform root, Material fabric, Material trim, Material accent, List<Mesh> ownedMeshes)
    {
        // Arc intentionally leaves the face open; the head bone moves the entire hood.
        Ring[] rings =
        {
            new Ring(-0.16f, 0.205f, 0.180f, -0.060f),
            new Ring(-0.07f, 0.240f, 0.220f, -0.035f),
            new Ring(0.08f, 0.252f, 0.230f, -0.025f),
            new Ring(0.205f, 0.224f, 0.203f, -0.045f),
            new Ring(0.296f, 0.140f, 0.145f, -0.060f),
            new Ring(0.326f, 0.018f, 0.024f, -0.075f)
        };

        BuildHoodLayer(root, "Outer hood shell", rings, 1f, fabric, ownedMeshes);
        BuildHoodLayer(root, "Dark inner hood lining", rings, 0.955f, trim, ownedMeshes);

        Vector3[] leftEdge = new Vector3[rings.Length];
        Vector3[] rightEdge = new Vector3[rings.Length];
        Vector3[] rearSeam = new Vector3[rings.Length];

        for (int i = 0; i < rings.Length; i++)
        {
            leftEdge[i] = HoodPoint(rings[i], 305f, 1.007f);
            rightEdge[i] = HoodPoint(rings[i], 55f, 1.007f);
            rearSeam[i] = HoodPoint(rings[i], 180f, 1.012f);
        }

        BuildPipe(root, "Hood opening - left rolled seam", leftEdge, 0.011f, trim, ownedMeshes);
        BuildPipe(root, "Hood opening - right rolled seam", rightEdge, 0.011f, trim, ownedMeshes);
        BuildPipe(root, "Back hood structural seam", rearSeam, 0.003f, accent, ownedMeshes);

        Vector3[] lowerRim = new Vector3[HoodSides + 1];
        for (int i = 0; i <= HoodSides; i++)
            lowerRim[i] = HoodPoint(rings[0], Mathf.Lerp(55f, 305f, (float)i / HoodSides), 1.008f);
        BuildPipe(root, "Reinforced hood lower rim", lowerRim, 0.010f, trim, ownedMeshes);
    }

    private static Vector3 HoodPoint(Ring r, float angleDegrees, float scale)
    {
        float a = angleDegrees * DegToRad;
        return new Vector3(Mathf.Sin(a) * r.width * scale,
                           r.y,
                           Mathf.Cos(a) * r.depth * scale + r.z);
    }

    private static void BuildHoodLayer(Transform root, string name, Ring[] rings, float scale, Material material, List<Mesh> meshes)
    {
        Geometry g = new Geometry();
        int stride = HoodSides + 1;
        for (int r = 0; r < rings.Length; r++)
        for (int s = 0; s <= HoodSides; s++)
        {
            float angle = Mathf.Lerp(55f, 305f, s / (float)HoodSides);
            g.Vertex(HoodPoint(rings[r], angle, scale), new Vector2((float)s / HoodSides, (float)r / (rings.Length - 1)));
        }
        for (int r = 0; r < rings.Length - 1; r++)
        for (int s = 0; s < HoodSides; s++)
        {
            int a = r * stride + s;
            g.Quad(a, a + 1, a + stride, a + stride + 1, scale < 1f);
        }
        AddPart(root, name, g, material, meshes);
    }

    private static void BuildPipe(Transform root, string name, Vector3[] points, float radius, Material material, List<Mesh> meshes)
    {
        if (points.Length < 2)
            return;
        Geometry g = new Geometry();
        int stride = PipeSides + 1;

        for (int p = 0; p < points.Length; p++)
        {
            Vector3 tangent = (points[Mathf.Min(p + 1, points.Length - 1)] - points[Mathf.Max(0, p - 1)]).normalized;
            Vector3 right = Vector3.Cross(tangent, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(tangent, Vector3.forward);
            right.Normalize();
            Vector3 up = Vector3.Cross(tangent, right).normalized;

            for (int side = 0; side <= PipeSides; side++)
            {
                float a = side * Mathf.PI * 2f / PipeSides;
                Vector3 pos = points[p] + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
                g.Vertex(pos, new Vector2((float)side / PipeSides, (float)p / (points.Length - 1)));
            }
        }

        for (int p = 0; p < points.Length - 1; p++)
        for (int s = 0; s < PipeSides; s++)
        {
            int a = p * stride + s;
            g.Quad(a, a + stride, a + 1, a + stride + 1);
        }

        AddPart(root, name, g, material, meshes);
    }

    private static void AddPart(Transform root, string partName, Geometry geometry, Material material, List<Mesh> ownedMeshes)
    {
        GameObject part = new GameObject(partName);
        part.transform.SetParent(root, false);
        Mesh mesh = geometry.ToMesh(partName);
        ownedMeshes.Add(mesh);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }
}
