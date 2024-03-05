using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
#endif
public class TowerGen : MonoBehaviour
{
    public Vector3 normalUp = Vector3.up;
    [SerializeField] private GameObject basePrefab;
    [SerializeField] private GameObject flatPrefab;
    [SerializeField] private GameObject[] topPrefabs;
    private GameObject topPrefab;
    [SerializeField] private GameObject shelfPrefab;
    [SerializeField] private GameObject ceilLightPrefab;
    [SerializeField] private GameObject[] otherObjects;
    [Space()]
    [SerializeField] private int stories = 5;
    [SerializeField] private int startRot = int.MaxValue;

    private List<Flat> flats;

    public bool makeOnStart = false;

    [HideInInspector] public bool debugMake = false;

    private void Start()
    {
        debugMake = false;
        if (makeOnStart)
            Init(Random.Range(0, 2000));
    }

    public void Init(int seed)
    {
        normalUp = normalUp.normalized;
        GenerateTower(seed);
        makeOnStart = false;
    }

    public void GenerateTower(int seed)
    {
        Random.InitState(seed);
        stories = Random.Range(5, 15);
        topPrefab = stories < 10 ? topPrefabs[0] : topPrefabs[Random.Range(0, topPrefabs.Length)];
        if (transform.childCount == 1)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            if (Application.isEditor)
                DestroyImmediate(transform.GetChild(0).gameObject);
            else
                Destroy(transform.GetChild(0).gameObject);
        }
        GameObject holder = new GameObject("Holder");
        holder.transform.parent = transform;
        holder.transform.localPosition = Vector3.zero;

        //Check if tower is on top of another tower (like the 3x3 on the 5x5 top prefab)
        foreach (TowerGen towerGen in FindObjectsOfType<TowerGen>())
        {
            if (transform.IsChildOf(towerGen.transform))
                normalUp = towerGen.normalUp;
        }

        //transform.rotation = Quaternion.LookRotation(Vector3.Cross(normalUp, Vector3.forward), normalUp);

        int prevRot = startRot;

        flats = new List<Flat>();

        for (int i = 0; i < stories; i++)
        {
            int rot;
            do { rot = Random.Range(0, 4) * 90; }
            while (rot == prevRot);

            GameObject prefab = i == 0 ? basePrefab : i == stories - 1 ? topPrefab : flatPrefab;

            Flat flat = Instantiate(prefab, transform.position + i * 5 * normalUp, Quaternion.LookRotation(Vector3.Cross(normalUp, Vector3.forward), normalUp) * Quaternion.Euler(0, rot, 0), holder.transform).GetComponent<Flat>();
            flat.gameObject.layer = gameObject.layer;
            flat.Init(Random.Range(0, 2000), rot, i == 0 ? transform : flats[i - 1].nextGapPos);

            flats.Add(flat);

            prevRot = rot;
        }
        if (!debugMake)
            return;
        foreach (Flat flat in flats)
            flat.DebugMake();
    }

    public void DebugRealign()
    {
        transform.rotation = Quaternion.identity;
    }
}
