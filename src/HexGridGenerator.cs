using System.Collections.Generic;
using UnityEngine;


public class HexGridGenerator : MonoBehaviour
{
    public float unit = 1f;
    public int relaxIterations = 20;

    Triangle[][] triangles;
    List<Face> faces = new List<Face>();
    public List<Face> facesFinal = new List<Face>();

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        triangles = new Triangle[4][];
        for (int r = 0; r < 4; r++) triangles[r] = new Triangle[6];
        faces.Clear();
        facesFinal.Clear();

        GenerateTriangles();
        BuildFaces();
        BuildFacesFinal();
        Relax();
        SpawnTileColliderGen();
    }

    // Step 1: generate the triangle rings
    void GenerateTriangles()
    {
        // Ring 0
        Vector2 a = Vector2.zero;
        Vector2 b = new Vector2(0, unit);
        triangles[0][0] = new Triangle(a, b, ThirdPoint(a, b, -60f));

        for (int i = 1; i < 6; i++)
        {
            Triangle prev = triangles[0][i - 1];
            triangles[0][i] = new Triangle(prev.a, prev.c, ThirdPoint(prev.a, prev.c, -60f));
        }

        // Rings 1 and 2 (ring 1 grows to the left of a-b, ring 2 to the right)
        // Very important otherwise the normals of meshes we build upon this grid will be messed up
        for (int r = 1; r <= 2; r++)
        {
            float angle = r == 1 ? 60f : -60f;
            for (int i = 0; i < 6; i++)
            {
                Triangle prev = triangles[r - 1][i];
                triangles[r][i] = new Triangle(prev.b, prev.c, ThirdPoint(prev.b, prev.c, angle));
            }
        }

        // Ring 3 closes the second hexagon
        for (int i = 0; i < 6; i++)
        {
            triangles[3][i] = new Triangle(
                triangles[2][i].a,
                triangles[2][i].c,
                triangles[1][(i + 1) % 6].c);
        }
    }

    // Equilateral triangle: rotate a->b by 60 degrees (negative = right, positive = left)
    Vector2 ThirdPoint(Vector2 a, Vector2 b, float angle)
    {
        Vector2 dir = (b - a).normalized * unit;
        return a + (Vector2)(Quaternion.Euler(0, 0, angle) * dir);
    }

    // Step 2: merge triangles into faces
    void BuildFaces()
    {
        for (int i = 0; i < 6; i += 2)
        {
            faces.Add(new Face(triangles[0][i].b, triangles[0][i].c, triangles[0][i + 1].c, triangles[0][i].a));
        }

        for (int i = 0; i < 6; i++)
        {
            faces.Add(new Face(triangles[1][i].a, triangles[1][i].c, triangles[2][i].c, triangles[1][i].b));
        }
    }

    // Step 3: subdivide faces and quadify the ring 3 triangles
    void BuildFacesFinal()
    {
        foreach (Face face in faces)
            facesFinal.AddRange(Subdivision(face));

        foreach (Triangle triangle in triangles[3])
            facesFinal.AddRange(Quadification(triangle));
    }

    public Face[] Quadification(Triangle t)
    {
        Vector2 ab = (t.a + t.b) / 2;
        Vector2 bc = (t.b + t.c) / 2;
        Vector2 ca = (t.c + t.a) / 2;
        Vector2 center = (t.a + t.b + t.c) / 3;

        return new Face[]
        {
            new Face(t.a, ab, center, ca),
            new Face(t.b, bc, center, ab),
            new Face(t.c, ca, center, bc)
        };
    }

    public Face[] Subdivision(Face f)
    {
        Vector2 ab = (f.a + f.b) / 2;
        Vector2 bc = (f.b + f.c) / 2;
        Vector2 cd = (f.c + f.d) / 2;
        Vector2 da = (f.d + f.a) / 2;
        Vector2 center = (f.a + f.b + f.c + f.d) / 4;

        return new Face[]
        {
            new Face(f.a, ab, center, da),
            new Face(ab, f.b, bc, center),
            new Face(center, bc, f.c, cd),
            new Face(da, center, cd, f.d)
        };
    }

    // Step 4: relax interior vertices, keep the boundary fixed
    void Relax()
    {
        var pos = new Dictionary<Vector2Int, Vector2>();
        var neighbors = new Dictionary<Vector2Int, HashSet<Vector2Int>>();
        var edgeCount = new Dictionary<(Vector2Int, Vector2Int), int>();

        foreach (Face f in facesFinal)
        {
            Vector2[] p = { f.a, f.b, f.c, f.d };
            for (int i = 0; i < 4; i++)
            {
                Vector2Int k = Key(p[i]);
                Vector2Int n = Key(p[(i + 1) % 4]);
                pos[k] = p[i];
                pos[n] = p[(i + 1) % 4];
                AddNeighbor(neighbors, k, n);
                AddNeighbor(neighbors, n, k);

                var edge = IsLess(k, n) ? (k, n) : (n, k);
                edgeCount.TryGetValue(edge, out int count);
                edgeCount[edge] = count + 1;
            }
        }

        // Boundary vertices belong to edges used by only one face
        var boundary = new HashSet<Vector2Int>();
        foreach (var kv in edgeCount)
        {
            if (kv.Value != 1) continue;
            boundary.Add(kv.Key.Item1);
            boundary.Add(kv.Key.Item2);
        }

        for (int it = 0; it < relaxIterations; it++)
        {
            var next = new Dictionary<Vector2Int, Vector2>(pos);
            foreach (var kv in neighbors)
            {
                if (boundary.Contains(kv.Key)) continue;

                Vector2 sum = Vector2.zero;
                foreach (Vector2Int n in kv.Value) sum += pos[n];
                next[kv.Key] = sum / kv.Value.Count;
            }
            pos = next;
        }

        foreach (Face f in facesFinal)
        {
            f.a = pos[Key(f.a)];
            f.b = pos[Key(f.b)];
            f.c = pos[Key(f.c)];
            f.d = pos[Key(f.d)];
        }
    }

    Vector2Int Key(Vector2 v)
    {
        return new Vector2Int(Mathf.RoundToInt(v.x / unit * 1000f), Mathf.RoundToInt(v.y / unit * 1000f));
    }

    bool IsLess(Vector2Int a, Vector2Int b)
    {
        return a.x < b.x || (a.x == b.x && a.y < b.y);
    }

    void AddNeighbor(Dictionary<Vector2Int, HashSet<Vector2Int>> dict, Vector2Int key, Vector2Int n)
    {
        if (!dict.TryGetValue(key, out var set))
        {
            set = new HashSet<Vector2Int>();
            dict[key] = set;
        }
        set.Add(n);
    }

    // Draw the grid in the Scene view
    void OnDrawGizmos()
    {
        if (facesFinal == null) return;

        Gizmos.color = Color.white;
        foreach (Face f in facesFinal)
        {
            Vector3 a = transform.TransformPoint(f.a);
            Vector3 b = transform.TransformPoint(f.b);
            Vector3 c = transform.TransformPoint(f.c);
            Vector3 d = transform.TransformPoint(f.d);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
    }
    public GameObject tileColliderGenerator;
    public void SpawnTileColliderGen()
    {
        GameObject cg = Instantiate(tileColliderGenerator);
        cg.GetComponent<TileColliderGenerator>().HexQuadGrid = this;

    }
}
