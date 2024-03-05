using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class JsonSaver
{
    public static void SaveData(string filename, object data)
    {
        string path = AbsolutePath(filename);
        File.WriteAllText(path, JsonUtility.ToJson(data));
        //Debug.Log("Saved: " + path);
    }

    public static Type LoadData<Type>(string filename)
    {
        string path = AbsolutePath(filename);
        if (!File.Exists(path))
            throw new System.Exception("Error: File does not exist: " + path);
        return JsonUtility.FromJson<Type>(File.ReadAllText(path));
    }

    private static string AbsolutePath(string filename)
    {
        string root = filename.Contains(Application.persistentDataPath) ? "" : Application.persistentDataPath + "/";
        string extension = filename.Contains(".json") ? "" : ".json";
        return root + filename + extension;
    }
}