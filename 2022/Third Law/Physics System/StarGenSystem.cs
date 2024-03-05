using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StarGenSystem : MonoBehaviour
{
    public enum GenPurpose { starting, respawning, wiping }

    public static Vector3 galaxyCentre;
    public int kingSeed;

    [SerializeField] private Material starMat;
    [SerializeField] private Material starMatSmall;
    private Texture2D starMap;
    [SerializeField] private ComputeShader starUpdateShader;
    private int handle;
    private Vector3[] starPosRelativeToGalaxy;
    private float[] starSphereRadii;
    private Vector3[] starRenderPos;
    private float[] starRenderLocalScaleX;

    [SerializeField] private GameObject starPrefab;
    public SunGenSystem sun;
    [SerializeField] private float galaxyRadius;
    public float starcoordLength = 250_000;
    [SerializeField] private int closeScale = 30_000;
    [SerializeField] private int approxStars;
    [SerializeField] private float updateStarsEachFrame;
    [SerializeField] private float probabilityScale;
    [SerializeField] float minPixelStarSize = 1;
    [SerializeField] float renderDistance = 100_000;
    private float minAt1Dist = -1;

    private Dictionary<Transform, StarCircle> stars;
    public Dictionary<string, int> starCodes { get; private set; }
    private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public int HME { get; private set; }
    public int EDN { get; private set; }
    public float GUL { get; private set; }

    private List<int> visitedSystems;
    public Transform closestStar;
    public bool generatedAtStar;

    public static float genTime;

    public struct StarCircle
    {
        public Vector3 starPosRelativeToGalaxy;
        public float starSphereRadius;

        public Color starColour;
        public Color sunColour;

        public StarCircle(Vector3 starPosRelativeToGalaxy, float starSphereRadius, float hue, float absoluteSaturation)
        {
            this.starPosRelativeToGalaxy = starPosRelativeToGalaxy;

            this.starSphereRadius = starSphereRadius;

            starColour = Color.HSVToRGB(hue, absoluteSaturation * 0.4f - 0.2f, 1);
            sunColour = Color.HSVToRGB(hue, absoluteSaturation, 1);
        }
    }

    [System.Serializable]
    private struct GalaxySaveData
    {
        public int kingSeed;
        public Vector3 galaxyCentre;
        public int[] visitedSystems;
    }

    private bool LoadTheGalaxy()
    {
        GalaxySaveData galaxySaveData = JsonSaver.LoadData<GalaxySaveData>("Galaxy_Save_Data", out bool success);

        kingSeed = galaxySaveData.kingSeed;
        galaxyCentre = galaxySaveData.galaxyCentre;
        if (success)
            visitedSystems = galaxySaveData.visitedSystems.ToList();

        if (generatedAtStar)
        {
            generatedAtStar = false;
            UnloadSun();
        }

        return success;
    }
    private void SaveTheGalaxy()
    {
        GalaxySaveData galaxySaveData;
        galaxySaveData.kingSeed = kingSeed;
        galaxySaveData.galaxyCentre = galaxyCentre;
        galaxySaveData.visitedSystems = visitedSystems.ToArray();
        JsonSaver.SaveData("Galaxy_Save_Data", galaxySaveData);
        MeetingHandler.Save();
    }

    private void DrawVisitedGalaxy()
    {
        Color32[] starMapColours = new Color32[starMap.width * starMap.height];

        foreach (int i in visitedSystems)
        {
            Vector3 position01 = starPosRelativeToGalaxy[i] / galaxyRadius * 0.5f + 0.5f * Vector3.one;
            starMapColours[Mathf.FloorToInt(position01.x * starMap.width) * starMap.height + Mathf.FloorToInt(position01.z * starMap.height)] = stars[transform.GetChild(0).GetChild(i)].starColour;
        }

        starMap.SetPixels32(starMapColours);

        starMap.Apply();
        starMat.SetTexture("_starTexture", starMap);
    }

    private void ApplyToSmallMap()
    {
        Vector3 playerPos = -galaxyCentre / galaxyRadius * 0.5f + 0.5f * Vector3.one;
        int scale = Mathf.RoundToInt(5 * starcoordLength / galaxyRadius * 0.5f * starMap.width);

        Vector2Int playerPix = new Vector2Int(Mathf.RoundToInt(playerPos.z * starMap.width), Mathf.RoundToInt(playerPos.x * starMap.height));

        Color[] starMapSmallColours = starMap.GetPixels(Mathf.Clamp(playerPix.x - scale, 0, starMap.width - 2 * scale), Mathf.Clamp(playerPix.y - scale, 0, starMap.height - 2 * scale), 2 * scale, 2 * scale);

        Texture2D starMapSmall = new Texture2D(2 * scale, 2 * scale);
        starMapSmall.SetPixels(starMapSmallColours);
        starMapSmall.Apply();

        starMatSmall.SetTexture("_starTexture", starMapSmall);
    }

    public void InitGalaxy(GenPurpose purpose)
    {
        CameraState.isDead = false;

        if (transform.childCount > 0)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(0).gameObject);
            else
                DestroyImmediate(transform.GetChild(0).gameObject);
        }

        starMap = new Texture2D(960, 960);
        visitedSystems = new List<int>();

        galaxyRadius = 2 * starcoordLength * Mathf.Sqrt(approxStars);

        Random.InitState(System.Environment.TickCount);

        GUL = Random.value;

        //If there is no save or we are making a new save
        if (!LoadTheGalaxy() || purpose == GenPurpose.wiping)
        {
            Vector3 randomRadius = Random.onUnitSphere * Random.Range(0.5f * galaxyRadius, 0.6f * galaxyRadius);
            galaxyCentre = new Vector3(randomRadius.x, 0.01f * randomRadius.y, randomRadius.z);

            kingSeed = Random.Range(0, 999999);

            visitedSystems = new List<int>();
            MeetingHandler.Wipe();
            InventoryUI.Wipe();
            FindObjectOfType<SadGirl>().Wipe();
            SaveTheGalaxy();
        }
        //If we are not starting the game, Init the galaxy with the player in a random position in it (between 50% & 60% out)
        if (purpose != GenPurpose.starting && Application.isPlaying)
        {
            Vector3 randomRadius = Random.onUnitSphere * Random.Range(0.5f * galaxyRadius, 0.6f * galaxyRadius);
            galaxyCentre = new Vector3(randomRadius.x, 0.01f * randomRadius.y, randomRadius.z);

            ShipWeight shipWeight = FindObjectOfType<ShipWeight>();
            shipWeight.Teleport(-shipWeight.position);
            shipWeight.transform.rotation = FindObjectOfType<RobotWeight>().transform.rotation;
            PhysicsUpdate.Reinit();
            CameraState.Reset();
            FindObjectOfType<InventoryUI>().Start();
        }
    }

    public void GenerateStars(GenPurpose purpose)
    {
        genTime = Time.realtimeSinceStartup;
        FindObjectOfType<RoboVision>().KillTransition();
        InitGalaxy(purpose);

        Transform almightyHolder = new GameObject("AlmightyHolder (0)").transform;
        almightyHolder.parent = transform;
        almightyHolder.SetAsFirstSibling();

        Random.InitState(kingSeed);

        int capacity = Mathf.CeilToInt(approxStars * 1.25f);

        stars = new Dictionary<Transform, StarCircle>(capacity);
        starCodes = new Dictionary<string, int>(capacity);

        //Need individual lists as well as the StarCircles for our compute shader
        List<Vector3> starPosRelativeToGalaxy = new List<Vector3>(capacity);
        List<float> starSphereRadii = new List<float>(capacity);
        List<Vector3> starRenderPos = new List<Vector3>(capacity);
        List<float> starRenderLocalScaleX = new List<float>(capacity);

        float spiralAngle = Random.Range(Mathf.PI, 2 * Mathf.PI);
        float spiralDirection = Random.Range(0, 2) * 2 - 1;

        Color32[] starMapColours = new Color32[starMap.width * starMap.height];

        float minCamSqrDist = float.MaxValue;
        float minGalSqrDist = float.MaxValue;
        float maxGalSqrDist = float.MinValue;

        float totalScale = 0;
        for (float x = -galaxyRadius; x < galaxyRadius; x += starcoordLength)
        {
            for (float y = -galaxyRadius; y < galaxyRadius; y += starcoordLength)
            {
                //More likely to generate stars closer to the centre of the galaxy and galaxy should have a double spiral shape
                if (1.35f * Mathf.Pow(Random.value * 0.5f, 2) > (x * x + y * y) / (galaxyRadius * galaxyRadius) &&
                    (Mathf.Abs((spiralDirection * Mathf.Atan2(y, x) + spiralAngle) % Mathf.PI - 2 * Mathf.PI * Mathf.Sqrt(x * x + y * y) / galaxyRadius) > Random.value))
                {
                    //Each star is assinged a unique cuboid in which it can generate
                    Vector3 _starPosRelativeToGalaxy = new Vector3(x, 0, y) + starcoordLength * Vector3.Scale(Random.insideUnitSphere, new Vector3(0.5f, 1, 0.5f));

                    StarCircle star = new StarCircle(_starPosRelativeToGalaxy, 5000, Random.value, Random.Range(0.5f, 1));
                    Vector3 starPosRelativeToCamera = star.starPosRelativeToGalaxy + galaxyCentre;
                    Transform starInstance = Instantiate(starPrefab, starPosRelativeToCamera.normalized * renderDistance, Quaternion.LookRotation(starPosRelativeToCamera), almightyHolder).transform;

                    starInstance.localScale = GetCircleStarScale(star.starSphereRadius, starPosRelativeToCamera.sqrMagnitude);
                    totalScale += starInstance.localScale.x;

                    starInstance.GetComponent<MeshRenderer>().sharedMaterial = new Material(starInstance.GetComponent<MeshRenderer>().sharedMaterial);

                    starInstance.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_starColour", Color.Lerp(star.starColour, star.sunColour, Mathf.Clamp01(starInstance.localScale.x / closeScale)));
                    starInstance.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_brightness", Mathf.Lerp(1, 5, Mathf.Clamp01(starInstance.localScale.x / 30_000)));

                    Vector3 i01 = _starPosRelativeToGalaxy / galaxyRadius * 0.5f + 0.5f * Vector3.one;
                    starMapColours[Mathf.FloorToInt(i01.x * starMap.width) * starMap.height + Mathf.FloorToInt(i01.z * starMap.height)] = star.starColour;

                    stars.Add(starInstance, star);

                    System.Random prng = new System.Random(stars.Count);
                    string starCode;
                    do
                    {
                        starCode = alphabet[prng.Next(0, 26)].ToString() + alphabet[prng.Next(0, 26)].ToString() + alphabet[prng.Next(0, 26)].ToString();
                    }
                    while (starCodes.ContainsKey(starCode) || starCode == "HME" || starCode == "END" || starCode == "GUL");

                    starCodes.Add(starCode, stars.Count - 1);

                    //visitedSystems.Add(stars.Count - 1);

                    starPosRelativeToGalaxy.Add(_starPosRelativeToGalaxy);
                    starSphereRadii.Add(5000);
                    starRenderPos.Add(starInstance.position);
                    starRenderLocalScaleX.Add(starInstance.localScale.x);

                    if (starPosRelativeToCamera.sqrMagnitude < minCamSqrDist)
                        (closestStar, minCamSqrDist) = (starInstance, starPosRelativeToCamera.sqrMagnitude);

                    if (_starPosRelativeToGalaxy.sqrMagnitude < minGalSqrDist)
                        (HME, minGalSqrDist) = (stars.Count - 1, _starPosRelativeToGalaxy.sqrMagnitude);
                    if (_starPosRelativeToGalaxy.sqrMagnitude > maxGalSqrDist)
                        (EDN, maxGalSqrDist) = (stars.Count - 1, _starPosRelativeToGalaxy.sqrMagnitude);
                }
            }
            if (stars.Count > approxStars * 1.25f)
            {
                Debug.LogError("Error: too many stars");
                break;
            }
        }

        List<string> binCodes = new List<string>(3);
        foreach (string starCode in starCodes.Keys)
        {
            if (starCodes[starCode] == HME)
                binCodes.Add(starCode);
            else if (starCodes[starCode] == EDN)
                binCodes.Add(starCode);
            else if (starCodes[starCode] == Mathf.FloorToInt(GUL * stars.Count))
                binCodes.Add(starCode);
        }
        foreach (string binCode in binCodes)
            starCodes.Remove(binCode);
        starCodes.Add("HME", HME);
        starCodes.Add("EDN", EDN);
        starCodes.Add("GUL", Mathf.FloorToInt(GUL * stars.Count));

        float meanScale = totalScale / stars.Count;
        probabilityScale = updateStarsEachFrame / (Mathf.Pow(meanScale / closeScale, 2) * stars.Count);

        this.starPosRelativeToGalaxy = starPosRelativeToGalaxy.ToArray();
        this.starSphereRadii = starSphereRadii.ToArray();
        this.starRenderPos = starRenderPos.ToArray();
        this.starRenderLocalScaleX = starRenderLocalScaleX.ToArray();

        for (int i = 0; i < stars.Count; i++)
        {
            float sqrDist = (starPosRelativeToGalaxy[i] + galaxyCentre).sqrMagnitude;

            if (sqrDist < 25 * starcoordLength * starcoordLength && !visitedSystems.Contains(i))
                visitedSystems.Add(i);
        }

        DrawVisitedGalaxy();
        ApplyToSmallMap();
    }

    public void PathBetween(int startStarIndex, int endStarIndex, Color color)
    {
        DrawVisitedGalaxy();

        Dijkstras pathFinder = new Dijkstras(starPosRelativeToGalaxy, 10 * starcoordLength);
        pathFinder.ShortestPath(startStarIndex, endStarIndex, out List<int> pathIndices, out _);
        Color32[] starMapColours = starMap.GetPixels32();

        Vector3 previ01 = starPosRelativeToGalaxy[pathIndices[0]] / galaxyRadius * 0.5f + 0.5f * Vector3.one;
        for (int i = 1; i < pathIndices.Count; i++)
        {
            Vector3 i01 = starPosRelativeToGalaxy[pathIndices[i]] / galaxyRadius * 0.5f + 0.5f * Vector3.one;
            //Draw line between previous node and current node on path
            for (float j = 0; j <= 1; j += 0.1f)
            {
                Vector3 lerpi01 = Vector3.Lerp(previ01, i01, j);

                starMapColours[Mathf.FloorToInt(lerpi01.x * starMap.width) * starMap.height + Mathf.FloorToInt(lerpi01.z * starMap.height)] = color;
            }
            previ01 = i01;
        }
        starMap.SetPixels32(starMapColours);
        starMap.Apply();
        starMat.SetTexture("_starTexture", starMap);


        ApplyToSmallMap();
    }

    private Vector3 GetCircleStarScale(float starRadius, float sqrStarDist)
    {
        //stars are at least rendered by minPixelStarSize pixels, with help from: https://stackoverflow.com/questions/21648630/radius-of-projected-sphere-in-screen-space
        if (minAt1Dist == -1)
            minAt1Dist = minPixelStarSize / Mathf.Sqrt(Mathf.Pow(Screen.height * 0.5f / Mathf.Tan(0.5f * Camera.main.fieldOfView * Mathf.Deg2Rad), 2) + minPixelStarSize * minPixelStarSize);

        float horizonRadius = Mathf.Max(minAt1Dist, Mathf.Sqrt(sqrStarDist - starRadius * starRadius) * starRadius / sqrStarDist);

        float scale = 2 * horizonRadius * renderDistance;

        return new Vector3(scale, scale, 1);
    }
    public bool UpdateStarColour(int index)
    {
        Transform starInstance = transform.GetChild(0).GetChild(index);
        float close01 = Mathf.Clamp01(starInstance.localScale.x / closeScale); //1 is close

        starInstance.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_starColour", Color.Lerp(stars[starInstance].starColour, stars[starInstance].sunColour, close01 * close01));
        starInstance.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_brightness", Mathf.Lerp(1, 5, close01 * close01));
        //starInstance.GetComponent<MeshRenderer>().enabled ^= true;

        return true;
    }
    // Update is called once per frame
    void Start()
    {
        handle = starUpdateShader.FindKernel("UpdateStars");
        GenerateStars(GenPurpose.starting);
        InvokeRepeating(nameof(ApplyToSmallMap), 3, 3);
    }
    private void Update()
    {
        if (PauseMenu.selectedPauseMenuOption == 0)
            GenerateStars(GenPurpose.wiping);
        else if (PauseMenu.selectedPauseMenuOption == 1 || CameraState.isDead && Time.realtimeSinceStartup > genTime + 5)
            GenerateStars(GenPurpose.respawning);

        //Mark the player's position
        starMat.SetVector("_playerPos", -galaxyCentre / galaxyRadius * 0.5f + 0.5f * Vector3.one);
        starMat.SetFloat("_engineOn", InventoryUI.shipEngineOn01);
        starMatSmall.SetFloat("_engineOn", InventoryUI.shipEngineOn01);

        int count = stars.Count;

        ComputeBuffer starPosRelativeToGalaxyBuffer = new ComputeBuffer(count, sizeof(float) * 3);
        ComputeBuffer starSphereRadiiBuffer = new ComputeBuffer(count, sizeof(float));
        ComputeBuffer starRenderPosBuffer = new ComputeBuffer(count, sizeof(float) * 3);
        ComputeBuffer starRenderLocalScaleXBuffer = new ComputeBuffer(count, sizeof(float));

        starPosRelativeToGalaxyBuffer.SetData(starPosRelativeToGalaxy);
        starSphereRadiiBuffer.SetData(starSphereRadii);
        starRenderPosBuffer.SetData(starRenderPos);
        starRenderLocalScaleXBuffer.SetData(starRenderLocalScaleX);

        starUpdateShader.SetBuffer(handle, "starPosRelativeToGalaxy", starPosRelativeToGalaxyBuffer);
        starUpdateShader.SetBuffer(handle, "starSphereRadii", starSphereRadiiBuffer);
        starUpdateShader.SetBuffer(handle, "starRenderPos", starRenderPosBuffer);
        starUpdateShader.SetBuffer(handle, "starRenderLocalScaleX", starRenderLocalScaleXBuffer);

        starUpdateShader.SetFloat("numStars", count);
        starUpdateShader.SetVector("galaxyCentre", galaxyCentre);
        starUpdateShader.SetFloat("minAt1Dist", minAt1Dist);
        starUpdateShader.SetFloat("renderDistance", renderDistance);

        starUpdateShader.Dispatch(handle, Mathf.CeilToInt(count / 128f), 1, 1);

        starRenderPosBuffer.GetData(starRenderPos);
        starRenderLocalScaleXBuffer.GetData(starRenderLocalScaleX);

        starPosRelativeToGalaxyBuffer.Dispose();
        starSphereRadiiBuffer.Dispose();
        starRenderPosBuffer.Dispose();
        starRenderLocalScaleXBuffer.Dispose();

        Transform newClosestStar = closestStar;
        float minSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Transform starInstance = transform.GetChild(0).GetChild(i);

            float sqrDist = (starPosRelativeToGalaxy[i] + galaxyCentre).sqrMagnitude;

            if (sqrDist < 25 * starcoordLength * starcoordLength && !visitedSystems.Contains(i))
                visitedSystems.Add(i);

            if (!(starInstance == closestStar && generatedAtStar) && (Random.value < probabilityScale * Mathf.Pow(starRenderLocalScaleX[i] / closeScale, 2)))
                UpdateStarColour(i);

            //Determine if the star is within the camera's view frustum
            Vector3 viewPoint = Camera.main.WorldToViewportPoint(starRenderPos[i]);
            if (starInstance != closestStar && (viewPoint.x < -0.1f || viewPoint.x > 1.1f || viewPoint.y < -0.1f || viewPoint.y > 1.1f || viewPoint.z < 0))
            {
                //If not, don't bother rendering it
                starInstance.gameObject.SetActive(false);
                continue;
            }

            //If it is, enable its game object and update its position, rotation, and scale
            starInstance.gameObject.SetActive(true);
            starInstance.SetPositionAndRotation(starRenderPos[i], Quaternion.LookRotation(starRenderPos[i]));
            starInstance.localScale = new Vector3(starRenderLocalScaleX[i], starRenderLocalScaleX[i], 1);

            if (sqrDist < minSqrDist)
            {
                newClosestStar = starInstance;
                minSqrDist = sqrDist;
            }
        }
        if (closestStar != newClosestStar)
            generatedAtStar = false;
        closestStar = newClosestStar;


        UpdateStarColour(closestStar.GetSiblingIndex());

        if ((stars[closestStar].starPosRelativeToGalaxy + galaxyCentre).sqrMagnitude < starcoordLength * starcoordLength)
        {
            if (!generatedAtStar)
            {
                SaveTheGalaxy();
                generatedAtStar = true;
                LoadSun();

                if (!visitedSystems.Contains(closestStar.GetSiblingIndex()))
                    visitedSystems.Add(closestStar.GetSiblingIndex());
                DrawVisitedGalaxy();
                ApplyToSmallMap();
            }
            else
                //Hide the 2D sprite that has been replaced with the 3D sun
                closestStar.GetComponent<MeshRenderer>().enabled = false;
        }
        else if (generatedAtStar)
        {
            SaveTheGalaxy();
            generatedAtStar = false;
            UnloadSun();
        }
    }

    private void LoadSun()
    {
        sun.gameObject.SetActive(true);

        foreach (Planet celestialBody in sun.celestialBodies)
            PhysicsUpdate.RemoveWeight(celestialBody.GetComponent<Weight>());
        PhysicsUpdate.RemoveWeight(sun.GetComponent<Weight>());

        float close01 = Mathf.Clamp01(closestStar.localScale.x / closeScale); //1 is close
        Color settledSunColour = Color.Lerp(stars[closestStar].starColour, stars[closestStar].sunColour, close01 * close01);
        sun.Generate(kingSeed + closestStar.GetSiblingIndex(), stars[closestStar].starPosRelativeToGalaxy + galaxyCentre, stars[closestStar].starSphereRadius, settledSunColour);
    }

    private void UnloadSun()
    {
        closestStar.GetComponent<MeshRenderer>().enabled = true;
        sun.gameObject.SetActive(false);

        foreach (Planet celestialBody in sun.celestialBodies)
        {
            celestialBody.gameObject.SetActive(false);
            PhysicsUpdate.RemoveWeight(celestialBody.GetComponent<Weight>());
        }
        PhysicsUpdate.RemoveWeight(sun.GetComponent<Weight>());
    }
}
