using System.Collections.Generic;
using UnityEngine;

public class ManualGridBuilding : MonoBehaviour
{
    [SerializeField]
    private Camera mainCamera;

    public int height = 0;
    public int materialIndx = 0;
    // 0 = iarba
    // 1 = pamant
    // 2 = apa

    public List<Material> materials = new List<Material>();

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastTile();
        }
    }

    private void RaycastTile()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Nu exista o camera asignata!");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Debug.DrawRay(
            ray.origin,
            ray.direction * 1000f,
            Color.red,
            2f
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            1000f,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore))
        {
            TileColliderGenerator generator =
                hit.collider.GetComponent<TileColliderGenerator>();

            if (generator == null)
                return;

            Face face = generator.GetFaceFromRaycast(hit);

            if (face == null)
                return;

            int faceIndex = hit.triangleIndex / 2;

            if (!generator.built[faceIndex])
            {
                generator.built[faceIndex] = true;

                BuildTile(face);
            }
        }
    }

    private void BuildTile(Face face)
    {
        if (materials == null || materials.Count == 0)
        {
            Debug.LogError("Lista materials este goala!");
            return;
        }

        if (materialIndx < 0 || materialIndx >= materials.Count)
        {
            Debug.LogError("materialIndx invalid: " + materialIndx);
            return;
        }

        GameObject tileObject = new GameObject("BuiltTile");

        tileObject.transform.SetParent(transform);

        MeshFilter meshFilter = tileObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = tileObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "BuiltTileMesh";

        Vector3[] vertices =
        {
        new Vector3(face.a.x, 0f, face.a.y),
        new Vector3(face.b.x, 0f, face.b.y),
        new Vector3(face.c.x, 0f, face.c.y),
        new Vector3(face.d.x, 0f, face.d.y)
    };

        int[] triangles =
        {
        0, 1, 2,
        0, 2, 3
    };

        // UV coordinates pentru textura
        Vector2[] uv =
        {
        new Vector2(0f, 0f),
        new Vector2(1f, 0f),
        new Vector2(1f, 1f),
        new Vector2(0f, 1f)
    };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        meshRenderer.sharedMaterial = materials[materialIndx];
    }
}