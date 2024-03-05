using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flat : MonoBehaviour
{
    private static Dictionary<int, int> script2shader = new Dictionary<int, int>(20)
    {
        { 14, 0 },
        { 5, 1 },
        { 3, 2 },
        { 4, 3 },
        { 13, 4 },
        { 2, 5 },
        { 0, 6 },
        { 7, 7 },
        { 11, 8 },
        { 1, 9 },
        { 19, 10 },
        { 17, 11 },
        { 15, 12 },
        { 16, 13 },
        { 18, 14 },
        { 12, 15 },
        { 6, 16 },
        { 8, 17 },
        { 9, 18 },
        { 10, 19 }
    };

    public Transform nextGapPos;

    [SerializeField] private GameObject building;
    [SerializeField] private GameObject shelfPrefab;
    [SerializeField] private GameObject ceilLightPrefab;
    [SerializeField] private GameObject[] otherObjects;

    [SerializeField] private float generateRadius = 20;

    private bool debugMake;

    private Dictionary<Vector3, Vector3Int> objectWalls;

    private float sqrRadius;
    private bool generated = false;
    private bool init = false;
    private Vector3Int localGapPos;
    private float rot;
    private float seed;

    public void Init(int _seed, float _rot, Transform transformGapPos)
    {
        init = true;

        sqrRadius = generateRadius * generateRadius;

        rot = _rot;

        objectWalls = new Dictionary<Vector3, Vector3Int>();

        transformGapPos.parent = transform;
        localGapPos = RoundV3(transformGapPos.transform.localPosition);

        //Placeholder for flat when player is far away
        Material placeholder = transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
        placeholder = new Material(placeholder);
        seed = _seed;
        placeholder.SetFloat("_seed", seed);
        transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = placeholder;
        
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = gameObject.layer;
    }

    public void DebugMake()
    {
        debugMake = true;
        Update();
        Update();
        debugMake = false;
    }

    private void Update()
    {
        if (!init)
        {
            Debug.LogWarning("Warning: flat not initialised");
            return;
        }
            
        if (debugMake || (Camera.main.transform.position - transform.position).sqrMagnitude < sqrRadius + 100)
        {
            if (!generated)
            {
                generated = true;
                Instantiate(building, transform.position, transform.rotation, transform).transform.SetAsFirstSibling();
                GenerateBuilding();
                GenerateProps();

                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = gameObject.layer;
            }
            //If placeholder active and camera is close, swap placeholder for the actual flat
            else if (transform.GetChild(1).gameObject.activeSelf)
            {
                transform.GetChild(0).gameObject.SetActive(true);
                transform.GetChild(1).gameObject.SetActive(false);
            }
        }
        //If placeholder active and camera is far, swap actual flat for the placeholder
        else if (generated && transform.GetChild(0).gameObject.activeSelf)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(true);
        }
    }


    void GenerateBuilding()
    {
        Transform floorHolder = transform.GetChild(0).GetChild(0);
        Transform wallHolder = transform.GetChild(0).GetChild(1);

        for (int i = 0; i < floorHolder.childCount; i++)
        {
            //Remove floor tile in flat blocking enterance from stairs below
            if (RoundV3(floorHolder.GetChild(i).localPosition) == localGapPos)
                floorHolder.GetChild(i).GetChild(0).gameObject.SetActive(false);
        }
        int c = 0;
        for (int i = 0; i < wallHolder.childCount; i++)
        {
            Vector3 wallPos = RoundV3(wallHolder.GetChild(i).localPosition);
            Vector3Int wallType = GetWallType(wallHolder.GetChild(i), rot);

            //Replace wall with window if wall on outside
            if (wallHolder.GetChild(i).GetChild(0).GetChild(0).gameObject.activeSelf)
            {
                if (Mathf.Sin(seed + 10 * script2shader[c]) > 0.5f)
                {
                    wallHolder.GetChild(i).GetChild(0).gameObject.SetActive(false);
                    wallHolder.GetChild(i).GetChild(1).gameObject.SetActive(true);
                }
                wallHolder.GetChild(i).GetChild(1).name = i.ToString();
                c++;
            }

            //Decide which wall a prop will generate from onto a floor tile
            if (objectWalls.ContainsKey(wallPos))
            {
                if (Random.value < 0.5f)
                    objectWalls[wallPos] = wallType;
            }
            else
                objectWalls.Add(wallPos, wallType);
        }
    }

    void GenerateProps()
    {
        Transform floorHolder = transform.GetChild(0).GetChild(0);

        for (int i = 0; i < floorHolder.childCount; i++)
        {
            if (Random.value < 0.2f)
                Instantiate(ceilLightPrefab, floorHolder.GetChild(i).position, floorHolder.GetChild(i).rotation, transform.GetChild(0).GetChild(2));
        }

        
        foreach (Vector3 pos in objectWalls.Keys)
        {
            //80% of positions on the flat shouln't have a prop, nor the position where a gap will be made in the floor
            if (pos == localGapPos || Random.value < 0.8f)
                continue;

            //Temporary wall to spawn prop
            Wall wall = new Wall(objectWalls[pos], null, shelfPrefab, otherObjects);

            Transform holder = new GameObject("Object Holder").transform;
            
            holder.parent = transform.GetChild(0).GetChild(2);
            holder.localPosition = pos;
            wall.SpawnObject(holder);
            holder.rotation = transform.rotation * Quaternion.Euler(0, -rot, 0);
        }
    }

    Vector3Int RoundV3(Vector3 vector3)
    {
        return new Vector3Int(Mathf.RoundToInt(vector3.x / 5) * 5, Mathf.RoundToInt(vector3.y / 5) * 5, Mathf.RoundToInt(vector3.z / 5) * 5);
    }

    Vector3Int GetWallType(Transform wall, float yRot)
    {
        Vector3Int wallType = Vector3Int.zero;

        if (wall.name.Contains("forward"))
            wallType = Vector3Int.forward;
        else if (wall.name.Contains("back"))
            wallType = Vector3Int.back;
        else if (wall.name.Contains("left"))
            wallType = Vector3Int.left;
        else if (wall.name.Contains("right"))
            wallType = Vector3Int.right;
        else
            Debug.LogError("Error wall has invalid name: " + wall.name);

        if (yRot % 180 == 90)
        {
            int dir = yRot == 90 ? 1 : -1;

            if (wallType == Vector3Int.forward)
                wallType = Vector3Int.right * dir;
            else if (wallType == Vector3Int.right)
                wallType = Vector3Int.back * dir;
            else if (wallType == Vector3Int.back)
                wallType = Vector3Int.left * dir;
            else if (wallType == Vector3Int.left)
                wallType = Vector3Int.forward * dir;
        }
        else if (yRot == 180)
            wallType = -wallType;

        return wallType;
    }
}
