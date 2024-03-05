using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeetingHandler : MonoBehaviour
{
    [SerializeField]
    public struct PreviousMeetings 
    {
        public int count;
        public int[] prevMeetSystemSeeds;

        public bool DoneAlready(int seed)
        {
            if (prevMeetSystemSeeds == null)
                WipeSave();

            foreach (int prevSeed in prevMeetSystemSeeds)
            {
                if (seed == prevSeed)
                    return true;
            }
            return false;
        }

        public void AddDone(int seed)
        {
            if (prevMeetSystemSeeds == null)
                WipeSave();

            foreach (int prevSeed in prevMeetSystemSeeds)
            {
                if (seed == prevSeed)
                    return;
            }
            prevMeetSystemSeeds = prevMeetSystemSeeds.Concat(new int[1] { seed }).ToArray();
            count++;
        }

        public void WipeSave()
        {
            prevMeetSystemSeeds = new int[0];
            count = 0;
        }
    }
    public static void Wipe()
    {
        previousMeetings.WipeSave();
    }

    public static void Save()
    {
        JsonSaver.SaveData("Previous_Meetings", previousMeetings);
    }

    private static PreviousMeetings previousMeetings = new PreviousMeetings();

    [SerializeField] private Transform[] robots;
    [SerializeField] private bool inHideout;
    private Transform robot;
    private TextRender textRender;

    private int seed;
    private bool doneAlready;

    // Start is called before the first frame update
    void Start()
    {
        previousMeetings = JsonSaver.LoadData<PreviousMeetings>("Previous_Meetings", out bool success);
        if (!success)
        {
            Wipe();
            Save();
            previousMeetings = JsonSaver.LoadData<PreviousMeetings>("Previous_Meetings", out _);
        }

        if (robot != null)
            return;

        seed = FindObjectOfType<SunGenSystem>().lordSeed;
        doneAlready = previousMeetings.DoneAlready(seed);

        Random.InitState(transform.root.GetComponent<Planet>().planetValues.environmentSeed);
        if (previousMeetings.count < 4 || !inHideout)
        {
            int index = Random.Range(0, robots.Length);
            robot = robots[index];
            robot.gameObject.SetActive(true);
            textRender = robot.GetChild(0).GetComponent<TextRender>();

            //A meeting can only take place once per hideout
            if (!doneAlready && inHideout)
                textRender.LoadConversation("meeting/" + (1 + previousMeetings.count).ToString());
            else
                textRender.LoadConversation("hints/" + index.ToString());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (doneAlready)
            return;

        if (CameraState.CamIsInteractingW(robot.position, -robot.forward, 10, 60) && !textRender.NextSentence() && !inHideout)
        {
            previousMeetings.AddDone(seed);
        }
    }
}
