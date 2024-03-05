using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector3Int parentKey { get; private set; }
    
    private Vector2 tileSize;
    [SerializeField] private GameObject[] WallPrefabs;

    [SerializeField] private GameObject Shelf;
    [SerializeField] private GameObject Light;
    [SerializeField] private GameObject Door;
    [SerializeField] private GameObject[] OtherObjects;

    public Dictionary<Vector3Int, Wall> Walls;

    public void Init(Vector3Int pos, Vector3Int _parentKey, Vector2 _tileSize)
    {
        parentKey = _parentKey;
        tileSize = _tileSize;

        foreach (GameObject prefab in WallPrefabs)
            prefab.transform.localScale = new Vector3(tileSize.x, tileSize.y * 2, tileSize.x);


        Walls = new Dictionary<Vector3Int, Wall>(5)
        {
            { Vector3Int.right, new Wall(Vector3Int.right, WallPrefabs[0], Shelf, OtherObjects) },
            { Vector3Int.left, new Wall(Vector3Int.left, WallPrefabs[1], Shelf, OtherObjects) },
            { Vector3Int.forward, new Wall(Vector3Int.forward, WallPrefabs[2], Shelf, OtherObjects) },
            { Vector3Int.back, new Wall(Vector3Int.back, WallPrefabs[3], Shelf, OtherObjects) },
            { Vector3Int.down, new Wall(Vector3Int.down, WallPrefabs[4], Shelf, OtherObjects) }
        };

        transform.localPosition = new Vector3(pos.x * tileSize.x, pos.y * tileSize.y, pos.z * tileSize.x);
    }

    public void Render()
    {
        Transform root = new GameObject("Tile root").transform;
        root.parent = transform;
        root.localPosition = Vector3.zero;

        if (tileSize == Vector2.zero)
        {
            Debug.LogError("Error: Tile not initialised");
            return;
        }

        bool corridor = false;
        Vector3Int opposite = Vector3Int.zero;
        foreach (Vector3Int key in Walls.Keys)
        {
            if (!Walls[key].wallState.removed)
            {
                if (key == -opposite)
                {
                    corridor = true;
                    break;
                }
                else
                    opposite = key;
            }
        }
            
        //Add a door if the tile has two opposing walls
        if (Random.value < 0.3f && corridor)
        {
            Vector3Int[] dirs = new Vector3Int[4] { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right };
            Vector3Int dir = dirs[Random.Range(0, dirs.Length)];

            //Locked if removing wall would make a hole in the hive
            bool locked = !Walls[dir].wallState.removed;

            //Replace wall with door

            Walls[dir].wallState.SetState(Wall.State.removed);

            Quaternion rot = Quaternion.Euler(0, 180, 0);

            if (dir == Vector3Int.back)
                rot = Quaternion.Euler(0, 0, 0);
            else if (dir == Vector3Int.left)
                rot = Quaternion.Euler(0, 90, 0);
            else if (dir == Vector3Int.right)
                rot = Quaternion.Euler(0, -90, 0);

            GameObject door = Instantiate(Door, transform.position, rot, root);
            door.GetComponent<OpenDoor>().locked = locked;
        }


        foreach (Wall wall in Walls.Values)
            wall.Render(root);

        if (Random.value < 0.1f)
            Instantiate(Light, transform.position, transform.rotation, root);
        
    }

    private IEnumerator showIfClose;

    private IEnumerator ShowIfClose(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);
            if (transform.childCount > 0)
                transform.GetChild(0).gameObject.SetActive((Camera.main.transform.position - transform.position).sqrMagnitude < 2500);
        }
    }
    public void Start()
    {
        showIfClose = ShowIfClose(0.5f);
        StartCoroutine(showIfClose);
    }
    public void OnDestroy()
    {
        if (showIfClose != null)
            StopCoroutine(showIfClose);
    }
}
