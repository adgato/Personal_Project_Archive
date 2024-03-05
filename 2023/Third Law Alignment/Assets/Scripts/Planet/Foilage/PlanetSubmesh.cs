using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetSubmesh : MonoBehaviour
{
    private const int enabledDist = 1;
    public MeshCollider meshCollider;
    public Mesh Surface => meshCollider.sharedMesh;
    private int submeshesPerEdge;
    private Vector3Int gridPos;
    public int PlayerDist { get; private set; } = int.MaxValue;


    /// <returns>True if there was the mesh was enabled or disabled, false if it stayed the same.</returns>
    public bool UpdatePlayerDist(Vector3Int[] collidingSubmesh, Vector3Int playerSubmesh)
    {
        int MeshDist = int.MaxValue;
        PlayerDist = MaxDistBetween(gridPos, playerSubmesh);// + Mathf.Abs(DistToGridEdge(gridPos) - DistToGridEdge(playerSubmesh));

        for (int j = 0; j < collidingSubmesh.Length; j++)
        {
            MeshDist = Mathf.Min(MeshDist, MaxDistBetween(gridPos, collidingSubmesh[j]));
        }
        bool collisionMeshWasEnabled = meshCollider.enabled;
        meshCollider.enabled = MeshDist <= enabledDist; //cube of (2 * enabledDist + 1) ^ 3 submeshes
        return collisionMeshWasEnabled ^ MeshDist <= enabledDist;
    }

    public void Init(Mesh collisionMesh, Vector3Int gridPos, int submeshesPerEdge)
    {
        this.gridPos = gridPos;
        this.submeshesPerEdge = submeshesPerEdge;
        meshCollider.sharedMesh = collisionMesh;
        meshCollider.enabled = true;

    }

    public void DisableCollider() => meshCollider.enabled = false;

    private int MaxDistBetween(Vector3Int a, Vector3Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
    }
    private int DistToGridEdge(Vector3Int a)
    {
        return Mathf.Min(a.x, a.y, a.z, submeshesPerEdge - a.x, submeshesPerEdge - a.y, submeshesPerEdge - a.z);
    }
}
