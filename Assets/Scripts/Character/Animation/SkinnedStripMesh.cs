using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class SkinnedStripMesh : MonoBehaviour
{
    [SerializeField] private Transform[] _bones;
    [SerializeField] private Material _material;
    [SerializeField] private float _width = 0.6f;
    [SerializeField] private int _widthSegments = 4;
    [SerializeField] private int _segmentsPerBone = 3;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        int boneCount = _bones.Length;
        int spans = boneCount - 1;
        int rows = spans * _segmentsPerBone + 1;
        int columns = _widthSegments + 1;

        Vector3 widthAxis = _bones[0].right;
        Matrix4x4 toLocal = transform.worldToLocalMatrix;

        Vector3[] vertices = new Vector3[rows * columns];
        Vector2[] uv = new Vector2[rows * columns];
        BoneWeight[] weights = new BoneWeight[rows * columns];

        for (int r = 0; r < rows; r++)
        {
            float along = (float)r / (rows - 1) * spans;
            int b = Mathf.Min((int)along, spans - 1);
            float t = along - b;

            Vector3 center = Vector3.Lerp(_bones[b].position, _bones[b + 1].position, t);

            for (int c = 0; c < columns; c++)
            {
                float side = (float)c / (columns - 1);
                Vector3 world = center + widthAxis * ((side - 0.5f) * _width);

                int index = r * columns + c;
                vertices[index] = toLocal.MultiplyPoint3x4(world);
                uv[index] = new Vector2(side, (float)r / (rows - 1));

                weights[index] = new BoneWeight
                {
                    boneIndex0 = b,
                    weight0 = 1f - t,
                    boneIndex1 = b + 1,
                    weight1 = t
                };
            }
        }

        int[] triangles = new int[(rows - 1) * (columns - 1) * 6];
        int ti = 0;

        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < columns - 1; c++)
            {
                int i0 = r * columns + c;
                int i1 = i0 + 1;
                int i2 = i0 + columns;
                int i3 = i2 + 1;

                triangles[ti++] = i0;
                triangles[ti++] = i2;
                triangles[ti++] = i1;
                triangles[ti++] = i1;
                triangles[ti++] = i2;
                triangles[ti++] = i3;
            }
        }

        Matrix4x4[] bindposes = new Matrix4x4[boneCount];

        for (int i = 0; i < boneCount; i++)
            bindposes[i] = _bones[i].worldToLocalMatrix * transform.localToWorldMatrix;

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            uv = uv,
            triangles = triangles,
            boneWeights = weights,
            bindposes = bindposes
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        SkinnedMeshRenderer renderer = GetComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = mesh;
        renderer.bones = _bones;
        renderer.rootBone = _bones[0];
        renderer.material = _material;
        renderer.localBounds = mesh.bounds;
        renderer.updateWhenOffscreen = true;
    }
}
