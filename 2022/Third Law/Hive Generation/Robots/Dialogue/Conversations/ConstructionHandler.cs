using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionHandler : MonoBehaviour
{

    [SerializeField] private Transform[] robots;
    private Transform robot;
    private TextRender textRender;
    private int conversation;
    private int upgradeIndex;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(transform.root.GetComponent<Planet>().planetValues.environmentSeed);
        robot = robots[Random.Range(0, robots.Length)];
        robot.gameObject.SetActive(true);
        textRender = robot.GetChild(0).GetComponent<TextRender>();
        textRender.LoadConversation("construction/greeting/" + Random.Range(0, 4).ToString());
        conversation = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //If the player requests the upgrade conversation and they have sufficient metal to buy an upgrade and the greeting conversation has no more sentences to render
        if (CameraState.CamIsInteractingW(robot.position, 7) && !textRender.NextSentence() && InventoryUI.robotMetalCount >= 100)
        {
            if (conversation == 0)
            {
                conversation = 1;
                List<int> upgradeIndexes = new List<int>();

                //Cannot upgrade beyond level 3
                if (InventoryUI.player.robotFuelCapacityLvl != 3)
                    upgradeIndexes.Add(0);
                if (InventoryUI.player.robotFuelBurnRateLvl != 3)
                    upgradeIndexes.Add(1);
                if (InventoryUI.player.robotFuelPerOreLvl != 3)
                    upgradeIndexes.Add(2);
                if (InventoryUI.player.robotOxygenCapacityLvl != 3)
                    upgradeIndexes.Add(3);
                if (InventoryUI.player.robotOxygenTemperatureScaleLvl != 3)
                    upgradeIndexes.Add(4);
                if (InventoryUI.player.robotOxygenSafeScaleLvl != 3)
                    upgradeIndexes.Add(5);
                if (InventoryUI.player.shipFuelCapacityLvl != 3)
                    upgradeIndexes.Add(6);
                if (InventoryUI.player.shipFuelBurnRateLvl != 3)
                    upgradeIndexes.Add(7);
                if (InventoryUI.player.shipSolarChargeRateLvl != 3)
                    upgradeIndexes.Add(8);

                upgradeIndex = upgradeIndexes.Count == 0 ? 9 : upgradeIndexes[Random.Range(0, upgradeIndexes.Count)];
                textRender.LoadConversation("construction/upgrade/" + upgradeIndex.ToString());
            }
            else if (conversation == 1 && upgradeIndex != 9)
            {
                conversation = 2;
                textRender.LoadConversation("construction/upgrade/done");
            }
        }
        //Added to take metal from player *after* robot announces confirmation.
        if (CameraState.CamIsInteractingW(robot.position, 7) && conversation == 2)
        {
            conversation = 3;
            InventoryUI.robotMetalCount -= 100;

            switch (upgradeIndex)
            {
                case 0: InventoryUI.player.robotFuelCapacityLvl++; break;
                case 1: InventoryUI.player.robotFuelBurnRateLvl++; break;
                case 2: InventoryUI.player.robotFuelPerOreLvl++; break;
                case 3: InventoryUI.player.robotOxygenCapacityLvl++; break;
                case 4: InventoryUI.player.robotOxygenTemperatureScaleLvl++; break;
                case 5: InventoryUI.player.robotOxygenSafeScaleLvl++; break;
                case 6: InventoryUI.player.shipFuelCapacityLvl++; break;
                case 7: InventoryUI.player.shipFuelBurnRateLvl++; break;
                case 8: InventoryUI.player.shipSolarChargeRateLvl++; break;
                default: break;
            };

            JsonSaver.SaveData("Player_Stats", InventoryUI.player);
        }
    }
}
