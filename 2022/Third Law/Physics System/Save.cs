using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class JsonSaver
{
    public static void SaveData(string filename, object data)
    {
        File.WriteAllText(PathOf(filename), JsonUtility.ToJson(data));
        Debug.Log("Saved: " + PathOf(filename));
    }

    public static Type LoadData<Type>(string filename, out bool success)
    {
        success = File.Exists(PathOf(filename));
        return success ? JsonUtility.FromJson<Type>(File.ReadAllText(PathOf(filename))) : default;
    }

    private static string PathOf(string filename)
    {
        return Application.persistentDataPath + "/" + filename + ".json";
    }
}
