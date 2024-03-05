using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiveGen : MonoBehaviour
{
    [Range(0,1)]
    public float corridorStraight = 0.8f;
    public int corridorLength = 25;
    public int corridors = 5;

    private int corridorCountForName;

    public Vector2 tileSize;
    public Tile tilePrefab;
    public CustomTile[] customTilePrefabs;
    public CustomTile enteranceTile;
    public float[] customSpawnProb;

    public Dictionary<Vector3Int, Tile> TileMap;
    public Dictionary<Vector3Int, CustomTile> CustomTileMap;

    [SerializeField] private ShipDoorOpen trigger;
    private bool comeFromTower = true;

    void Update()
    {
        if (!CameraState.inHive)
            return;

        if (!comeFromTower && trigger.lerp < 0)
            CameraState.inHive = false;
        else if (trigger.lerp > 0)
            comeFromTower = false;
    }

    // Start is called before the first frame update
    public void Start()
    {
        Random.InitState((transform.root.TryGetComponent(out Planet planet) ? planet.planetValues.environmentSeed : System.Environment.TickCount) + Mathf.RoundToInt(transform.eulerAngles.sqrMagnitude));

        Quaternion rotation = transform.rotation;
        transform.rotation = Quaternion.identity;

        Clear();

        if (customSpawnProb.Length != customTilePrefabs.Length)
            Debug.LogError("Error: Each custom prefab must have an associated spawn probability");

        TileMap = new Dictionary<Vector3Int, Tile>();
        CustomTileMap = new Dictionary<Vector3Int, CustomTile>();

        Tile first = NewTile(Vector3Int.left, Vector3Int.left);
        trigger = NewCustomTile(Vector3Int.zero, Vector3Int.zero, enteranceTile).transform.GetChild(1).GetChild(0).GetComponent<ShipDoorOpen>();
        first.Walls[-Vector3Int.left].wallState.SetState(Wall.State.removed);

        //Generate the corridors
        for (int i = 0; i < corridors; i++)
        {
            corridorCountForName = i;
            Vector3Int[] allPos = new Vector3Int[TileMap.Count];
            TileMap.Keys.CopyTo(allPos, 0);

            Vector3Int randPos = Vector3Int.zero;
            Tile randTile = null;
            Vector3Int randDir = Vector3Int.zero;

            //Choose a random tile and direction
            while (randDir == Vector3Int.zero)
            {
                randPos = allPos[Random.Range(0, TileMap.Count)];

                randTile = TileMap[randPos];

                if (randTile == null)
                    continue;

                List<Vector3Int> removed = new List<Vector3Int>(4);
                
                if (!randTile.Walls[Vector3Int.forward].wallState.removed)
                    removed.Add(Vector3Int.forward);
                else if (!randTile.Walls[Vector3Int.back].wallState.removed)
                    removed.Add(Vector3Int.back);
                else if (!randTile.Walls[Vector3Int.left].wallState.removed)
                    removed.Add(Vector3Int.left);
                else if (!randTile.Walls[Vector3Int.right].wallState.removed)
                    removed.Add(Vector3Int.right);

                if (removed.Count > 0)
                    randDir = removed[Random.Range(0, removed.Count)];
            }

            Tile starterTile = NewTileInDir(randDir, ref randPos, randTile);

            //Add tiles to the new corridor
            AddCorridor(randPos, randDir, starterTile, corridorLength);
        }

        Cleanup();
        Render();

        transform.rotation = rotation;

        Random.InitState(System.Environment.TickCount);
    }

    public void Clear()
    {
        if (Application.isEditor)
            DestroyImmediate(transform.GetChild(0).gameObject);
        else
            Destroy(transform.GetChild(0).gameObject);

        GameObject newHolder = new GameObject("Holder");
        newHolder.transform.parent = transform;
        newHolder.transform.SetAsFirstSibling();
        newHolder.transform.localRotation = Quaternion.identity;
        newHolder.transform.localPosition = Vector3.zero;
    }
    Tile NewTile(Vector3Int pos, Vector3Int dir)
    {
        if (TileMap.TryGetValue(pos + dir, out Tile tile))
            return tile;

        GameObject tileObject = Instantiate(tilePrefab.gameObject, transform.GetChild(0));
        tileObject.name = "Tile of corridor: " + corridorCountForName;

        tile = tileObject.GetComponent<Tile>();

        tile.Init(pos + dir, pos, tileSize);

        TileMap.Add(pos + dir, tile);

        return tile;
    }
    CustomTile NewCustomTile(Vector3Int pos, Vector3Int dir, CustomTile customTilePrefab)
    {
        GameObject customTileObject = Instantiate(customTilePrefab.gameObject, transform.GetChild(0));
        customTileObject.name = "Custom Tile of corridor: " + corridorCountForName;

        CustomTile customTile = customTileObject.GetComponent<CustomTile>();

        customTile.Init(pos + dir, pos, tileSize, dir);

        return customTile;
    }
    Tile NewTileInDir(Vector3Int dir, ref Vector3Int pos, Tile tile)
    {
        Tile new_tile = NewTile(pos, dir);

        pos += dir;

        //If the tile at that position is a "custom" tile, it cannot be overwritten
        if (new_tile == null) 
            return null;

        //If the previous tile is not a placeholder, remove the wall of it towards the new tile in the specified direction
        if (tile != null)
            tile.Walls[dir].wallState.SetState(Wall.State.removed);

        //Remove the wall of the new tile towards the previous tile in the opposite direction
        new_tile.Walls[-dir].wallState.SetState(Wall.State.removed);

        return new_tile;
    }

    void NewCustomTileInDir(Vector3Int dir, ref Vector3Int pos, Tile tile, CustomTile customTilePrefab)
    {
        CustomTile customTile = NewCustomTile(pos, dir, customTilePrefab);

        pos += dir;

        if (tile != null)
            tile.Walls[dir].wallState.SetState(Wall.State.removed);

        //Add placeholders over the custom tile to occupy the space on the TileMap that the customTile occupies in the game
        customTile.AddPlaceholders(ref TileMap);

        pos += customTile.localExitPos;

        if (!CustomTileMap.ContainsKey(pos))
            CustomTileMap.Add(pos, customTile);
    }

    void AddCorridor(Vector3Int pos, Vector3Int dir, Tile tile, int length)
    {
        //If the desired length is reached or there is no tile to connect to, return
        if (length == 0 || tile == null)
            return;

        Tile new_tile = null;

        bool custom = false;

        for (int i = 0; i < customTilePrefabs.Length; i++)
        {
            if (Random.value < customSpawnProb[i])
            {
                //If a custom tile is spawned, add it to the corridor and set the new tile to the next regular tile in the corridor
                NewCustomTileInDir(dir, ref pos, tile, customTilePrefabs[Random.Range(0, customTilePrefabs.Length)]);
                new_tile = NewTileInDir(dir, ref pos, null);
                custom = true;
                break;
            }
        }
        if (!custom)
        {
            dir = RandomBiasDir(dir, corridorStraight);
            new_tile = NewTileInDir(dir, ref pos, tile);
        }

        //The function is called recursively until the desired length is reached
        AddCorridor(pos, dir, new_tile, length - 1);
    }

    Vector3Int RandomBiasDir(Vector3Int biasDir, float strength)
    {
        if (biasDir != Vector3Int.forward && biasDir != Vector3Int.back && biasDir != Vector3Int.left && biasDir != Vector3Int.right)
        {
            Debug.LogError("Error: invalid direction");
            return Vector3Int.zero;
        }
        else if (Random.value < strength)
            return biasDir;
        //Return a random vector perpendicular to the input direction vector if the random value is greater than or equal to the strength
        else if (biasDir == Vector3Int.forward || biasDir == Vector3Int.back)
            return Random.value < 0.5f ? Vector3Int.right : Vector3Int.left;
        else
            return Random.value < 0.5f ? Vector3Int.forward : Vector3Int.back;
    }

    void Cleanup()
    {
        Vector3Int[] dirs = new Vector3Int[4] { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right };

        //For each tile in the map, set the wall state between the current tile all tiles adjacent to "removed"
        foreach (Vector3Int pos in TileMap.Keys)
        {
            foreach (Vector3Int dir in dirs)
            {
                if (TileMap.ContainsKey(pos + dir) && TileMap[pos] != null)
                    TileMap[pos].Walls[dir].wallState.SetState(Wall.State.removed);
            }
        }
    }

    void Render()
    {
        foreach (Tile tile in transform.GetChild(0).GetComponentsInChildren<Tile>())
            tile.Render();
    }

    private List<Vector3> PathToOrigin(Vector3Int child)
    {
        
        List<Vector3> path = new List<Vector3>();
        path.Add(TileMap[child].transform.position);

        while (child != Vector3Int.left + Vector3Int.left)
        {
            if (!TileMap.ContainsKey(child))
                break;

            if (TileMap[child] == null)
            {
                if (!CustomTileMap.ContainsKey(child))
                    break;

                foreach (Vector3 position in CustomTileMap[child].GetPath())
                    path.Add(position);

                
                path.Add(CustomTileMap[child].transform.position);
                child = CustomTileMap[child].parentKey;
            }
            else
            {
                path.Add(TileMap[child].transform.position);
                child = TileMap[child].parentKey;
            }
        }
        
        path.Add(TileMap[Vector3Int.left + Vector3Int.left].transform.position);

        return path;
    }

    public bool PathBetweenPoints(Vector3 start, Vector3 end, out List<Vector3> worldPath)
    {
        Vector3Int tileClosestToStart = Vector3Int.zero;
        Vector3Int tileClosestToEnd = Vector3Int.zero;
        float minStartSqrDist = float.MaxValue;
        float minEndSqrDist = float.MaxValue;
        foreach (Vector3Int key in TileMap.Keys)
        {
            if (TileMap[key] == null)
                continue;

            float sqrStartDist = (TileMap[key].transform.position - start).sqrMagnitude;
            if (sqrStartDist < minStartSqrDist)
            {
                minStartSqrDist = sqrStartDist;
                tileClosestToStart = key;
            }

            float sqrEndDist = (TileMap[key].transform.position - end).sqrMagnitude;
            if (sqrEndDist < minEndSqrDist)
            {
                minEndSqrDist = sqrEndDist;
                tileClosestToEnd = key;
            }
        }

        List<Vector3> startToOrigin = PathToOrigin(tileClosestToStart);
        List<Vector3> endToOrigin = PathToOrigin(tileClosestToEnd);
        worldPath = new List<Vector3>(startToOrigin.Count);

        bool success = true;
        int index = -1;

        //Add each point from the start point until the common point to the path
        foreach (Vector3 point in startToOrigin)
        {
            if (endToOrigin.Contains(point))
            {
                index = endToOrigin.FindIndex(x => x == point);
                break;
            }

            worldPath.Add(point);
        }

        //If there is no common point between the startToOrigin and endToOrigin lists then there is no path between the start and end
        if (index == -1)
        {
            success = false;
            index = 0;
        }

        //Add each point from the common point to the end point to the path 
        for (int i = index; i >= 0; i--)
            worldPath.Add(endToOrigin[i]);


        for (int i = 1; i < worldPath.Count; i++)
            Debug.DrawLine(worldPath[i - 1], worldPath[i], Color.red, Time.smoothDeltaTime);

        return success;
    }
}
