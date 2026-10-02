using System.Collections.Generic;
using UnityEngine;

public class TileColliderGenerator : MonoBehaviour
{
    public HexGridGenerator HexQuadGrid;

    private MeshCollider meshCollider;
    private List<Face> faces = new List<Face>();
    public bool[] built;

    private void Start()
    {
        Physics.queriesHitBackfaces = true;
        GenerateCollider();
    }

    private void GenerateCollider()
    {
        if (HexQuadGrid == null)
        {
            Debug.LogError("HexQuadGrid nu este asignat!");
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        faces.Clear();

        foreach (Face face in HexQuadGrid.facesFinal)
        {
            int index = vertices.Count;

            // Vector2 -> Vector3
            // X ramane X
            // Y devine Z
            // Inaltimea este Y = 0

            vertices.Add(new Vector3(face.a.x, face.a.y, 0f));
            vertices.Add(new Vector3(face.b.x, face.b.y, 0f));
            vertices.Add(new Vector3(face.c.x, face.c.y, 0f));
            vertices.Add(new Vector3(face.d.x, face.d.y, 0f));


            triangles.Add(index + 0);
            triangles.Add(index + 1);
            triangles.Add(index + 2);

            triangles.Add(index + 0);
            triangles.Add(index + 2);
            triangles.Add(index + 3);

            // Un Face = 2 triunghiuri
            faces.Add(face);
        }

        Mesh mesh = new Mesh();
        mesh.name = "Tilemap Collider";

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshCollider = GetComponent<MeshCollider>();

        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        // Fortam Unity sa recalculeze colliderul
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        built = new bool[faces.Count];
    }

    public Face GetFaceFromRaycast(RaycastHit hit)
    {
        int triangleIndex = hit.triangleIndex;

        if (triangleIndex < 0)
        {
            Debug.Log("err indextriunghi:" + triangleIndex);
            return null;
        }

        // Fiecare Face are 2 triunghiuri
        int faceIndex = triangleIndex / 2;

        if (faceIndex < 0 || faceIndex >= faces.Count)
        {
            Debug.Log("err indexface:" + faceIndex);
            return null;
        }
        Debug.Log("index: " + faceIndex);
        return faces[faceIndex];
    }
}