using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class JsonSaver
{
    public static void SaveData(string filename, object data, bool verbose = true)
    {
        string path = AbsolutePath(filename);
        File.WriteAllText(path, JsonUtility.ToJson(data));
        if (verbose)
            Debug.Log("Saved: " + path);
    }

    public static Type LoadData<Type>(string filename) 
    {
        string path = AbsolutePath(filename);
        if (!File.Exists(path))
            throw new System.Exception("Error: File does not exist: " + path);
        return JsonUtility.FromJson<Type>(File.ReadAllText(path));
    }

    public static Type LoadResource<Type>(string filename)
    {
        return JsonUtility.FromJson<Type>(Resources.Load<TextAsset>(filename).text);
    }

    private static string AbsolutePath(string filename) //Acceptable formats: "filename", "filename.json", "directory/filename.json"
    {
        string root = filename.Contains(Application.persistentDataPath) ? "" : Application.persistentDataPath + "/";
        string extension = filename.Contains(".json") ? "" : ".json";
        return root + filename + extension;
    }
}