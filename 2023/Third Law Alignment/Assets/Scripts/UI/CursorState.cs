using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorState : MonoBehaviour
{
    private enum State { Default, Selecting }

    [SerializeField] private Texture2D DefaultMouse;
    [SerializeField] private Texture2D SelectMouse;
    private static bool SelectingThisFrame;
    private static State CurrentState;


    // Update is called once per frame
    void Update()
    {
        Cursor.visible = ControlSaver.GamePaused;
        Cursor.lockState = ControlSaver.GamePaused ? CursorLockMode.None : CursorLockMode.Locked;

        if (SelectingThisFrame && CurrentState != State.Selecting)
        {
            Cursor.SetCursor(SelectMouse, new Vector2(6, 0), CursorMode.ForceSoftware);
            CurrentState = State.Selecting;
        }
        else if (!SelectingThisFrame && CurrentState == State.Selecting)
        {
            Cursor.SetCursor(DefaultMouse, Vector2.zero, CursorMode.ForceSoftware);
            CurrentState = State.Default;
        }

        SelectingThisFrame = false;
    }

    public static void Select()
    {
        SelectingThisFrame = true;
    }
}
