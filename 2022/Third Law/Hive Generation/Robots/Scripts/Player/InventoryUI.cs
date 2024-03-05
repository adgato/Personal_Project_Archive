using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct PlayerLevel 
{
    //All levels are 0,1,2,3
    public int robotFuelCapacityLvl;
    public int robotFuelBurnRateLvl;
    public int robotFuelPerOreLvl;

    public int robotOxygenCapacityLvl;
    public int robotOxygenTemperatureScaleLvl;
    public int robotOxygenSafeScaleLvl;

    public int shipFuelCapacityLvl;
    public int shipFuelBurnRateLvl;
    public int shipSolarChargeRateLvl;

    private Dictionary<string, float> Stats;

    private float Val(int lvl, float zero, float one, float two, float three)
    {
        return lvl == 0 ? zero : lvl == 1 ? one : lvl == 2 ? two : lvl == 3 ? three : float.NaN;
    }

    public float Stat(string stat)
    {
        if (Stats == null)
        {
            Stats = new Dictionary<string, float>(9)
            {
                { "robotFuelCapacity", Val(robotFuelCapacityLvl, 100, 110, 120, 130) },
                { "robotFuelBurnRate", Val(robotFuelBurnRateLvl, 0.25f, 0.22f, 0.2f, 0.18f) },
                { "robotFuelPerOre", Val(robotFuelPerOreLvl, 5, 10, 15, 20) },

                { "robotOxygenCapacity", Val(robotOxygenCapacityLvl, 600, 720, 840, 960) },
                { "robotOxygenTemperatureScale", Val(robotOxygenTemperatureScaleLvl, 10, 8, 6, 4) },
                { "robotOxygenSafeScale", Val(robotOxygenSafeScaleLvl, -2, -4, -5, -7) },

                { "shipFuelCapacity", Val(shipFuelCapacityLvl, 1000, 1100, 1200, 1300) },
                { "shipFuelBurnRate", Val(shipFuelBurnRateLvl, 1.67f, 1.33f, 1.17f, 1) },
                { "shipSolarChargeRate", Val(shipSolarChargeRateLvl, 4, 4.68f, 5.32f, 6.68f) }
            };
        }
        return Stats[stat];
    }
}


public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public struct ShipMetalCountWrap 
    {
        public int shipMetalCount;
        public ShipMetalCountWrap(int shipMetalCount)
        {
            this.shipMetalCount = shipMetalCount;
        }
    }


    public static float fuelRemaining;
    public static float oxygenRemaining;
    public static float shipFuelRemaining;
    public static float shipMetalCount;
    public static float robotMetalCount;
    public static float robotTemperature;
    public static PlayerLevel player;
    public static Weight shipTargetX;
    public static float shipEngineOn01;

    [SerializeField] private Slider fuel;
    [SerializeField] private Slider oxygen;
    [SerializeField] private Material fuelMat;
    [SerializeField] private Material oxygenMat;
    [SerializeField] private Material shipFuelMat;
    [SerializeField] private Material shipTempMat;

    [SerializeField] private TMPro.TextMeshProUGUI metalText;
    [SerializeField] private TextMesh shipMetalText;
    [SerializeField] private TMPro.TextMeshProUGUI warningText;
    [SerializeField] private TMPro.TextMeshProUGUI metalCounter;

    [SerializeField] private RobotWeight playerRobot;
    [SerializeField] private ShipWeight playerShip;
    [SerializeField] private StarGenSystem starGenSystem;

    private float barTurnOnLerp;
    private float chargingLerp;

    private float prevRobotMetalCount;
    private readonly string aquiredMetalMessage = "AQUIRED $ METAL";
    private float metalMessageTime;

    private readonly string oxygenDepletedMessage = "WARNING: OXYGEN DEPLETED. BURNING FUEL FASTER";
    private float oxygenMessageTime;
    private readonly string fuelDepletedMessage =   "WARNING: FUEL DEPLETED. BURNING METAL INSTEAD";
    private float fuelMessageTime;
    private readonly string lockOnMessage = "HOLD SPACE-BAR TO MATCH VELOCITY";
    private float lockOnMessageTime;
    private readonly string liftoffMessage = "HOLD SHIFT TO LIFT-OFF";
    private float liftoffMessageTime;

    public static void Wipe()
    {
        JsonSaver.SaveData("Player_Stats", new PlayerLevel());
        JsonSaver.SaveData("Ship_Metal_Count", new ShipMetalCountWrap(0));
    }

    // Start is called before the first frame update
    public void Start()
    {
        player = JsonSaver.LoadData<PlayerLevel>("Player_Stats", out bool success);
        if (!success)
            JsonSaver.SaveData("Player_Stats", player);

        shipEngineOn01 = 1;

        robotMetalCount = 0;

        shipMetalCount = JsonSaver.LoadData<ShipMetalCountWrap>("Ship_Metal_Count", out success).shipMetalCount;
        if (!success)
            JsonSaver.SaveData("Ship_Metal_Count", new ShipMetalCountWrap(0));

        fuel.maxValue = player.Stat("robotFuelCapacity");
        oxygen.maxValue = player.Stat("robotOxygenCapacity");

        shipFuelMat.SetFloat("_maxShipFuel", player.Stat("shipFuelCapacity"));

        fuelMat.SetFloat("_maxSliderValue", fuel.maxValue);
        oxygenMat.SetFloat("_maxSliderValue", oxygen.maxValue);

        fuelRemaining = fuel.maxValue;
        oxygenRemaining = oxygen.maxValue;
        shipFuelRemaining = shipFuelMat.GetFloat("_maxShipFuel");
        UpdateSliders();
    }

    // Update is called once per frame
    void Update()
    {
        if (CameraState.isPaused)
            return;

        CameraState.isDead |= fuelRemaining <= 0 && robotMetalCount <= 0;

        float prevShipFuelRemaining = shipFuelRemaining;

        float sunDist10 = Mathf.InverseLerp(starGenSystem.starcoordLength, starGenSystem.sun.minMaxDist.x, starGenSystem.sun.transform.position.magnitude);
        float solarDot01 = Mathf.Clamp01(Vector3.Dot(playerShip.transform.up * (ShipWeight.xRotatesYaw ? -1 : 1), (starGenSystem.sun.transform.position - playerShip.position).normalized));

        robotTemperature = 0;

        if (playerRobot.sigWeight != null)
        {
            float altitude = (playerRobot.position - playerRobot.sigWeight.position).magnitude;

            float altitude01 = Mathf.InverseLerp(Mathf.Max(playerRobot.sigWeight.planet.planetValues.radius, playerRobot.sigWeight.planet.planetMesh.elevationData.Min), 
                playerRobot.sigWeight.planet.atmosphere.atmosRadius, altitude);

            float sunPermittivity01 = Mathf.InverseLerp(6, 0.5f, playerRobot.sigWeight.planet.atmosphere.density) *
                Mathf.Clamp01(Vector3.Dot((playerRobot.position - playerRobot.sigWeight.position) / altitude, (starGenSystem.sun.transform.position - playerRobot.sigWeight.position).normalized));

            if (starGenSystem.generatedAtStar)
                shipFuelRemaining += player.Stat("shipSolarChargeRate") * solarDot01 * sunDist10 * Mathf.Lerp(0.75f, 1, Mathf.Lerp(sunPermittivity01, 1, altitude01)) * Time.deltaTime;

            //temperature given by planet (one minus so that the thermometer has cold = 0)
            robotTemperature = 1 - playerRobot.sigWeight.planet.planetValues.temperature;
            //temperature given by sunlight (varies based on atmosphere density)
            robotTemperature += 0.5f * sunPermittivity01;
            robotTemperature = Mathf.Clamp01(robotTemperature);
            //lerp temperature by altitude
            robotTemperature *= 1 - altitude01;
        }
        else
            shipFuelRemaining += player.Stat("shipSolarChargeRate") * solarDot01 * sunDist10 * Time.deltaTime;

        float temperatureScale = Mathf.Abs(robotTemperature - 0.5f) * 2 * player.Stat("robotOxygenTemperatureScale");
        float safeScale = CameraState.inShip || CameraState.inHive ? player.Stat("robotOxygenSafeScale") : 1;

        RobotBreath(Time.deltaTime * temperatureScale * safeScale);

        if (!CameraState.inShip)
            RobotBurn(player.Stat("robotFuelBurnRate") * Time.deltaTime * (Input.anyKey ? 1 : 0.9f));

        shipFuelRemaining -= player.Stat("shipFuelBurnRate") * (!CameraState.flyingShip || !Input.anyKey ? 0.5f : ShipWeight.hyperOn ? 10 : 1) * shipEngineOn01 * Time.deltaTime;

        shipFuelRemaining = Mathf.Clamp(shipFuelRemaining, 0, player.Stat("shipFuelCapacity"));

        if (shipFuelRemaining == 0)
            shipEngineOn01 = 0;

        chargingLerp = Mathf.Clamp01(chargingLerp + Time.deltaTime * (shipFuelRemaining > prevShipFuelRemaining ? 2 : -5));
        shipFuelMat.SetFloat("_charging", Mathf.Pow(chargingLerp, 2));
        //Debug.Log(shipFuelRemaining - prevShipFuelRemaining);

        UpdateSliders();
        UpdateText();
    }

    private void OnValidate()
    {
        Start();
    }

    private void UpdateSliders()
    {
        fuel.value = fuelRemaining;
        oxygen.value = oxygenRemaining;

        shipFuelMat.SetFloat("_engineOn", shipEngineOn01);
        shipTempMat.SetFloat("_engineOn", shipEngineOn01);

        shipFuelMat.SetFloat("_shipFuel", shipFuelRemaining);
        fuelMat.SetFloat("_sliderValue", fuel.value);

        oxygenMat.SetFloat("_sliderValue", oxygen.value);

        //Turn on the oxygen and fuel bars when the player is not flying the ship
        barTurnOnLerp = Mathf.Clamp01(barTurnOnLerp + Time.deltaTime * (CameraState.flyingShip ? -1 : 1));
        fuelMat.SetFloat("_turnOnLerp", barTurnOnLerp);
        oxygenMat.SetFloat("_turnOnLerp", barTurnOnLerp);

        metalCounter.color = new Color(RoboVision.visionColour.r, RoboVision.visionColour.g, RoboVision.visionColour.b, 0.4f * barTurnOnLerp);
        warningText.color = new Color(RoboVision.visionColour.r, RoboVision.visionColour.g, RoboVision.visionColour.b, 0.5f);

        Color rgb = fuel.transform.GetChild(0).GetComponent<Image>().color;
        fuel.transform.GetChild(0).GetComponent<Image>().color = new Color(rgb.r, rgb.g, rgb.b, 0.5f * barTurnOnLerp);
        rgb = oxygen.transform.GetChild(0).GetComponent<Image>().color;
        oxygen.transform.GetChild(0).GetComponent<Image>().color = new Color(rgb.r, rgb.g, rgb.b, 0.5f * barTurnOnLerp);

        shipTempMat.SetFloat("_shipTemp", robotTemperature);
    }

    private void UpdateText()
    {
        UpdateAquiredMetalMessage();
        UpdateWarnings();
    }
    private void UpdateAquiredMetalMessage()
    {
        shipMetalText.text = "Metal:\n\n" + Mathf.FloorToInt(shipMetalCount).ToString();
        shipMetalText.color = Color.Lerp(Color.red, Color.cyan, Mathf.InverseLerp(0, 100, shipMetalCount));
        shipMetalText.color *= Mathf.Clamp(shipEngineOn01, 0.05f, 1);

        //If no metal count has not increased this frame, do not display message
        if (prevRobotMetalCount >= robotMetalCount)
        {
            //robotMetalCount = Mathf.Max(0, robotMetalCount);
            prevRobotMetalCount = robotMetalCount;
            metalText.text = Mathf.FloorToInt(robotMetalCount).ToString();
            return;
        }
        
        //robotMetalCount can be updated here and the message on screen will be updated accordingly
        metalText.text = aquiredMetalMessage.Substring(0, Mathf.Min(Mathf.FloorToInt(metalMessageTime * 20), aquiredMetalMessage.Length)).Replace("$", Mathf.FloorToInt(robotMetalCount - prevRobotMetalCount).ToString());

        metalMessageTime += Time.deltaTime;

        //If message has been displayed for more than four seconds (and message has been written in this time) hide the message
        if (Mathf.FloorToInt(metalMessageTime * 20) >= aquiredMetalMessage.Length && metalMessageTime > 4)
        {
            prevRobotMetalCount = robotMetalCount;
            metalMessageTime = 0;
        }
    }
    private void UpdateWarnings()
    {
        warningText.text = "";

        if (oxygenRemaining != 0)
            oxygenMessageTime = 0;
        else
        {
            oxygenMessageTime += Time.deltaTime;
            warningText.text += "[" + oxygenDepletedMessage.Substring(0, Mathf.Min(Mathf.FloorToInt(oxygenMessageTime * 20), oxygenDepletedMessage.Length)) + "]\n";
        }

        if (fuelRemaining != 0)
            fuelMessageTime = 0;
        else
        {
            fuelMessageTime += Time.deltaTime;
            warningText.text += "[" + fuelDepletedMessage.Substring(0, Mathf.Min(Mathf.FloorToInt(fuelMessageTime * 20), fuelDepletedMessage.Length)) + "]\n";
        }

        if (CameraState.flyingShip)
        {
            shipTargetX = playerRobot.sigWeight;

            float chosenSqrDist = 3_600_000_000; //60_000^2

            foreach (Planet planet in starGenSystem.sun.celestialBodies)
            {
                //Check if the planet is active, is closer than the current chosen planet, and is in front of the camera
                if (planet.gameObject.activeSelf && planet.transform.position.sqrMagnitude < chosenSqrDist && Vector3.Dot((planet.transform.position - Camera.main.transform.position).normalized, Camera.main.transform.forward) > 0.85f)
                {
                    shipTargetX = planet.GetComponent<Weight>();
                    chosenSqrDist = planet.transform.position.sqrMagnitude;
                }
            }

            //If a valid target was found, highlight it unless the player is on the planet and update the lock-on message
            if (shipTargetX != null)
            {
                if (shipTargetX == playerRobot.sigWeight)
                    RoboVision.highlightBounds = new RoboVision.TargetBounds(float.MaxValue);
                else
                    RoboVision.highlightBounds = new RoboVision.TargetBounds(shipTargetX.transform.GetChild(3));

                lockOnMessageTime += Time.deltaTime;
                warningText.text += "[" + lockOnMessage.Substring(0, Mathf.Min(Mathf.FloorToInt(lockOnMessageTime * 20), lockOnMessage.Length)) + "]\n";
            }
            else
                lockOnMessageTime = 0;

            if (playerShip.kinematicBody.isGrounded)
            {
                liftoffMessageTime += Time.deltaTime;
                warningText.text += "[" + liftoffMessage.Substring(0, Mathf.Min(Mathf.FloorToInt(liftoffMessageTime * 20), liftoffMessage.Length)) + "]\n";
            }
            else
                liftoffMessageTime = 0;
        }
        else
        {
            lockOnMessageTime = 0;
            liftoffMessageTime = 0;
        }

    }

    private void RobotBurn(float amount)
    {
        if (fuelRemaining == 0)
            robotMetalCount -= amount / 2;
        else
            fuelRemaining = Mathf.Clamp(fuelRemaining - amount, 0, fuel.maxValue);    // + playerRobot.forceMeter / 100; //[needs balancing first] like foxy
    }
    private void RobotBreath(float amount)
    {
        if (oxygenRemaining == 0 && amount > 0)
            RobotBurn(amount);
        else
            oxygenRemaining = Mathf.Clamp(oxygenRemaining - amount, 0, oxygen.maxValue);
    }
}
