using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SadGirl : MonoBehaviour
{
    private enum State { readyToSpeak, waitingToVanish, vanished  }

    [SerializeField] private State girlState;
    private bool interacting = false;
    [SerializeField] private Material shipLightMap;
    [SerializeField] private ShipWeight shipWeight;

    private TextRender textRender;
    Vector3 ogLocalAngles;
    private float spokenTime;
    private float spokenGapMinutes = 5;

    private float prevGenTime;

    public static float faceLight01;

    private int conversationCount;

    [System.Serializable]
    private struct ConversationWrap 
    {
        public int conversationCount;

        public ConversationWrap(int conversationCount)
        {
            this.conversationCount = conversationCount;
        }
    }

    public void Wipe()
    {
        conversationCount = 0;
        JsonSaver.SaveData("Eva_Conversation", new ConversationWrap(0));
    }


    // Start is called before the first frame update
    void Start()
    {
        conversationCount = JsonSaver.LoadData<ConversationWrap>("Eva_Conversation", out bool success).conversationCount;
        if (!success)
        {
            conversationCount = 0;
            JsonSaver.SaveData("Eva_Conversation", new ConversationWrap(conversationCount));
        }

        ogLocalAngles = transform.GetChild(1).localEulerAngles;
        textRender = transform.GetChild(2).GetComponent<TextRender>();
    }

    // Update is called once per frame
    void Update()
    {
        if (girlState == State.waitingToVanish && (!CameraState.inShip || CameraState.flyingShip))
            girlState = State.vanished;
        else if (girlState == State.vanished && Time.realtimeSinceStartup > spokenTime + spokenGapMinutes * 60 && prevGenTime < StarGenSystem.genTime)
            girlState = State.readyToSpeak;

        //Set the opaque or transparent skin based on whether Eva has vanished
        transform.GetChild(1).gameObject.SetActive(girlState != State.vanished);
        transform.GetChild(3).gameObject.SetActive(girlState == State.vanished);

        if (CameraState.InLockState(CameraState.LockState.unlocked))
            interacting = false;

        if (!interacting && CameraState.CamIsInteractingW(transform.position, transform.GetChild(0).forward, 3, 60))
        {
            interacting = true;
            CameraState.LockCamera(transform.GetChild(0));
            LoadConversation();
        }
        else if (interacting && CameraState.InLockState(CameraState.LockState.locked) && Input.GetKeyDown(KeyCode.E) && !textRender.NextSentence())
        {
            interacting = false;
            CameraState.UnlockCamera();

            JsonSaver.SaveData("Eva_Conversation", new ConversationWrap(conversationCount));
        }
        Material skin = transform.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial;
        skin.SetFloat("_light", Mathf.Lerp(skin.GetFloat("_light"), shipLightMap.GetFloat("_brightness") * Mathf.Lerp(0.75f, 1, faceLight01), Time.deltaTime));

        //Teeter forward and back for realism (nobody sits completely still)
        transform.GetChild(1).localEulerAngles = ogLocalAngles + 1 * Mathf.Sin(Time.realtimeSinceStartup) * Vector3.right;
        transform.GetChild(3).localEulerAngles = transform.GetChild(1).localEulerAngles;
    }
    void LoadConversation()
    {
        if (girlState != State.readyToSpeak)
        {
            textRender.LoadConversation("eva/blank");
            return;
        }
        textRender.LoadConversation("eva/" + (conversationCount + 1).ToString());

        spokenTime = Time.realtimeSinceStartup;
        conversationCount++;

        girlState = State.waitingToVanish;

        prevGenTime = StarGenSystem.genTime;
    }
}
