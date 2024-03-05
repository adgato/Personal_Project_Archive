using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This tile is prefabricated. There are two custom tiles, the stairs and the hall.
public class CustomTile : MonoBehaviour
{
    public Vector3Int parentKey { get; private set; }


    [SerializeField] private Transform Exit;
    [SerializeField] private Transform Start;
    [SerializeField] private Transform End;
    //List of points describing the path an android can take through the custom tile during pathfinding
    [SerializeField] private Transform[] ExitToStartPath;

    [HideInInspector] public Vector3Int localExitPos;

    private Vector3Int startTilePos;
    private Vector3Int endTilePos;



    public void Init(Vector3Int pos, Vector3Int _parentKey, Vector2 tileSize, Vector3Int dir)
    {
        parentKey = _parentKey;

        localExitPos = new Vector3Int(Mathf.RoundToInt(Exit.localPosition.x), Mathf.RoundToInt(Exit.localPosition.y), Mathf.RoundToInt(Exit.localPosition.z));
        startTilePos = new Vector3Int(Mathf.RoundToInt(Start.localPosition.x), Mathf.RoundToInt(Start.localPosition.y), Mathf.RoundToInt(Start.localPosition.z));
        endTilePos = new Vector3Int(Mathf.RoundToInt(End.localPosition.x), Mathf.RoundToInt(End.localPosition.y), Mathf.RoundToInt(End.localPosition.z));

        localExitPos /= 5;
        startTilePos /= 5;
        endTilePos /= 5;

        ChangeDir(dir);

        startTilePos += pos;
        endTilePos += pos;

        transform.localPosition = new Vector3(pos.x * tileSize.x, pos.y * tileSize.y, pos.z * tileSize.x);
        transform.localScale = new Vector3(tileSize.x, tileSize.y, tileSize.x) / 5;
    }

    void ChangeDir(Vector3Int new_dir)
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);

        if (new_dir == Vector3Int.back)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else if (new_dir == Vector3Int.left)
            transform.rotation = Quaternion.Euler(0, -90, 0);
        else if (new_dir == Vector3Int.right)
            transform.rotation = Quaternion.Euler(0, 90, 0);

        RotateAroundOrigin(ref localExitPos, new_dir);
        RotateAroundOrigin(ref startTilePos, new_dir);
        RotateAroundOrigin(ref endTilePos, new_dir);
    }

    void RotateAroundOrigin(ref Vector3Int point, Vector3Int dir)
    {
        if (dir == Vector3Int.back)
            point = new Vector3Int(-point.x, point.y, -point.z);
        else if (dir == Vector3Int.left)
            point = new Vector3Int(-point.z, point.y, point.x);
        else if (dir == Vector3Int.right)
            point = new Vector3Int(point.z, point.y, -point.x);
    }

    public void AddPlaceholders(ref Dictionary<Vector3Int, Tile> TileMap)
    {
        //Every coordinate tile within the bounding box of the custom tile should be overwritten with null as a placeholder
        for (int x = Mathf.Min(startTilePos.x, endTilePos.x); x <= Mathf.Max(startTilePos.x, endTilePos.x); x++)
        {
            for (int y = Mathf.Min(startTilePos.y, endTilePos.y); y <= Mathf.Max(startTilePos.y, endTilePos.y); y++)
            {
                for (int z = Mathf.Min(startTilePos.z, endTilePos.z); z <= Mathf.Max(startTilePos.z, endTilePos.z); z++)
                {
                    if (TileMap.ContainsKey(new Vector3Int(x, y, z)))
                    {
                        if (TileMap[new Vector3Int(x, y, z)] != null)
                        {
                            //Remove all walls from any tiles in the way so they don't render
                            foreach (Wall wall in TileMap[new Vector3Int(x, y, z)].Walls.Values)
                                wall.wallState.SetState(Wall.State.removed);

                            TileMap[new Vector3Int(x, y, z)] = null;
                        }
                    }
                    else
                        TileMap.Add(new Vector3Int(x, y, z), null);
                }
            }
        }
    }

    public Vector3[] GetPath()
    {
        List<Vector3> path = new List<Vector3>();
        foreach (Transform point in ExitToStartPath)
            path.Add(point.position);
        return path.ToArray();
    }
}
