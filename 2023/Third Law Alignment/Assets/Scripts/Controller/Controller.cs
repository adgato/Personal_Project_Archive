using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Controller
{
    public static Controller Player { get; private set; } = new Controller();

    public enum InputType { None, Button, Axis }
    public enum Inputs { A, B, X, Y, LB, RB, L, R, LS, RS, Start, Pause, LV, LH, RV, RH, DV, DH }

    private SaveData Keyboard0 = new SaveData()
    {
        ButtonMap = new int[] { 101, 114, 32, 113, 120, 304, 0, 0, 0, 0, 0, 0 },
        AxisMap = new string[] { "W/S", "D/A", "Mouse Y", "Mouse X", "", "" },
        InvertAxisMap = new int[] { 1, 1, 1, 1, 1, 1 }
    };

    [System.Serializable]
    private struct SaveData
    {
        public float LastUsed;
        public int[] ButtonMap;
        public string[] AxisMap;
        public int[] InvertAxisMap;
    }

    private Dictionary<Inputs, KeyCode> ButtonMap = new Dictionary<Inputs, KeyCode>(12)
    {
        { Inputs.A, default },
        { Inputs.B, default },
        { Inputs.X, default },
        { Inputs.Y, default },
        { Inputs.LB, default },
        { Inputs.RB, default },
        { Inputs.L, default },
        { Inputs.R, default },
        { Inputs.LS, default },
        { Inputs.RS, default },
        { Inputs.Start, default },
        { Inputs.Pause, default }
    };

    private Dictionary<Inputs, string> AxisMap = new Dictionary<Inputs, string>(6)
    {
        { Inputs.LV, default },
        { Inputs.LH, default },
        { Inputs.RV, default },
        { Inputs.RH, default },
        { Inputs.DV, default },
        { Inputs.DH, default }
    };

    private Dictionary<Inputs, int> InvertAxisMap = new Dictionary<Inputs, int>(6)
    {
        { Inputs.LV, 1 },
        { Inputs.LH, 1 },
        { Inputs.RV, 1 },
        { Inputs.RH, 1 },
        { Inputs.DV, 1 },
        { Inputs.DH, 1 }
    };

    private bool IsAI = false;
    private AIController AI;

    public KeyCode Button(Inputs key)
    {
        return ButtonMap[key];
    }
    public string Axis(Inputs key)
    {
        return AxisMap[key];
    }
    public int GetAxisInvert(Inputs key)
    {
        return InvertAxisMap[key];
    }


    public bool GetButtonDown(Inputs input)
    {
        return IsAI ? AI.GetButtonDown(input) : Input.GetKeyDown(Button(input));
    }
    public bool GetButton(Inputs input)
    {
        return IsAI ? AI.GetButton(input) : Input.GetKey(Button(input));
    }
    public bool GetButtonUp(Inputs input)
    {
        return IsAI ? AI.GetButtonUp(input) : Input.GetKeyUp(Button(input));
    }
    public bool AnyButton()
    {
        foreach (Inputs button in ButtonMap.Keys)
            if (GetButton(button))
                return true;
        return false;
    }
    public bool AnyButtonDown()
    {
        foreach (Inputs button in ButtonMap.Keys)
            if (GetButtonDown(button))
                return true;
        return false;
    }

    public float GetAxis(Inputs input, float maxMagnitude01 = 1)
    {
        if (IsAI)
            return AI.GetAxis(input, maxMagnitude01);
        if (Axis(input) == null || Axis(input) == "")
            return 0;
        return Mathf.Clamp(Input.GetAxis(Axis(input)) * GetAxisInvert(input) / maxMagnitude01, -1, 1);
    }


    public void Bind(Inputs inputs, KeyCode value)
    {
        if (IsAI)
            Debug.LogError("Error: cannot bind button to an AI controller");

        if (ButtonMap.ContainsKey(inputs))
            ButtonMap[inputs] = value;
    }
    public void Bind(Inputs inputs, string value)
    {
        if (IsAI)
            Debug.LogError("Error: cannot bind axis to an AI controller");

        if (AxisMap.ContainsKey(inputs))
            AxisMap[inputs] = value;
    }
    public void SetAxisInvert(Inputs inputs, int value)
    {
        if (IsAI)
            Debug.LogError("Error: cannot invert an axis on an AI controller");

        if (value != -1 && value != 1)
        {
            Debug.LogError("Error: Can only invert to -1 or 1");
            return;
        }

        if (InvertAxisMap.ContainsKey(inputs))
            InvertAxisMap[inputs] = value;
    }

    public void SaveAs(string filename, bool verbose = false)
    {
        if (IsAI)
        {
            Debug.LogError("Error: cannot save an AI controller");
            return;
        }

        SaveData savedControl;

        //time.time() in C#
        savedControl.LastUsed = (float)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;

        savedControl.ButtonMap = ButtonMap.Values.Select(x => (int)x).ToArray();
        savedControl.AxisMap = AxisMap.Values.ToArray();
        savedControl.InvertAxisMap = InvertAxisMap.Values.ToArray();

        System.IO.Directory.CreateDirectory(Application.persistentDataPath + "/ControlMap");
        JsonSaver.SaveData("ControlMap/" + filename, savedControl, verbose);
    }
    public void LoadFrom(string filename)
    {
        if (IsAI)
        {
            Debug.LogError("Error: cannot load an AI controller");
            return;
        }

        LoadFrom(JsonSaver.LoadData<SaveData>("ControlMap/" + filename));

        SaveAs(filename); //Update LastUsed time
    }
    private void LoadFrom(SaveData savedControl)
    {
        if (IsAI)
        {
            Debug.LogError("Error: cannot load an AI controller");
            return;
        }

        int i = 0;
        Dictionary<Inputs, KeyCode> newButtonMap = new Dictionary<Inputs, KeyCode>(12);

        foreach (Inputs input in ButtonMap.Keys)
            newButtonMap[input] = (KeyCode)savedControl.ButtonMap[i++];
        ButtonMap = newButtonMap;

        i = 0;
        Dictionary<Inputs, string> newAxisMap = new Dictionary<Inputs, string>(6);

        foreach (Inputs input in AxisMap.Keys)
            newAxisMap[input] = savedControl.AxisMap[i++];
        AxisMap = newAxisMap;

        i = 0;
        Dictionary<Inputs, int> newInvertAxisMap = new Dictionary<Inputs, int>(6);

        foreach (Inputs input in InvertAxisMap.Keys)
            newInvertAxisMap[input] = savedControl.InvertAxisMap[i++];
        InvertAxisMap = newInvertAxisMap;
    }
    public void LoadFromMostRecent()
    {
        string[] order = GetSavedControls();
        if (order.Length > 0)
            LoadFrom(order[0]);
        else
        {
            LoadFrom(Keyboard0);
            SaveAs("Keyboard0");
        }
    }

    public void LoadAIController(AIController controller)
    {
        IsAI = true;
        AI = controller;
    }


    public static InputType GetInputType(Inputs input)
    {
        if ((int)input <= 11)
            return InputType.Button;
        return InputType.Axis;
    }

    public static string[] GetSavedControls()
    {
        System.IO.Directory.CreateDirectory(Application.persistentDataPath + "/ControlMap");
        return System.IO.Directory.GetFiles(Application.persistentDataPath + "/ControlMap", "*.json").OrderByDescending(x => JsonSaver.LoadData<SaveData>(x).LastUsed)
            .Select(x => x.Replace(Application.persistentDataPath + "/ControlMap\\", "").Replace(".json", "")).ToArray();
    }
}