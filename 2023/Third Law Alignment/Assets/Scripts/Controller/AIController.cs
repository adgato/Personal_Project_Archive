using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController
{
    private Dictionary<Controller.Inputs, bool> PressedButtons = new Dictionary<Controller.Inputs, bool>(12)
    {
        { Controller.Inputs.A, false },
        { Controller.Inputs.B, false },
        { Controller.Inputs.X, false },
        { Controller.Inputs.Y, false },
        { Controller.Inputs.LB, false },
        { Controller.Inputs.RB, false },
        { Controller.Inputs.L, false },
        { Controller.Inputs.R, false },
        { Controller.Inputs.LS, false },
        { Controller.Inputs.RS, false },
        { Controller.Inputs.Start, false },
        { Controller.Inputs.Pause, false }
    };

    private Dictionary<Controller.Inputs, bool> PrevUpdatePressedButtons = new Dictionary<Controller.Inputs, bool>(12)    
    {
        { Controller.Inputs.A, false },
        { Controller.Inputs.B, false },
        { Controller.Inputs.X, false },
        { Controller.Inputs.Y, false },
        { Controller.Inputs.LB, false },
        { Controller.Inputs.RB, false },
        { Controller.Inputs.L, false },
        { Controller.Inputs.R, false },
        { Controller.Inputs.LS, false },
        { Controller.Inputs.RS, false },
        { Controller.Inputs.Start, false },
        { Controller.Inputs.Pause, false }
    };

    private Dictionary<Controller.Inputs, float> HeldAxis = new Dictionary<Controller.Inputs, float>(6)
    {
        { Controller.Inputs.LV, default },
        { Controller.Inputs.LH, default },
        { Controller.Inputs.RV, default },
        { Controller.Inputs.RH, default },
        { Controller.Inputs.DV, default },
        { Controller.Inputs.DH, default }
    };

    public bool GetButtonDown(Controller.Inputs input)
    {
        if (PrevUpdatePressedButtons == null)
            return false;
        return PressedButtons[input] && !PrevUpdatePressedButtons[input];
    }
    public bool GetButton(Controller.Inputs input)
    {
        return PressedButtons[input];
    }
    public bool GetButtonUp(Controller.Inputs input)
    {
        if (PrevUpdatePressedButtons == null)
            return false;
        return !PressedButtons[input] && PrevUpdatePressedButtons[input];
    }

    public float GetAxis(Controller.Inputs input, float maxMagnitude01 = 1)
    {
        return Mathf.Clamp(HeldAxis[input] / maxMagnitude01, -1, 1);
    }

    protected void SetButton(Controller.Inputs input, bool state)
    {
        PressedButtons[input] = state;
    }
    protected void SetAxis(Controller.Inputs input, float magnitude)
    {
        HeldAxis[input] = magnitude;
    }

    public virtual void FixedUpdate()
    {
        //This is so that GetButtonDown() and GetButtonUp() work correctly
        foreach (Controller.Inputs key in PressedButtons.Keys)
            PrevUpdatePressedButtons[key] = PressedButtons[key];
    }
}
