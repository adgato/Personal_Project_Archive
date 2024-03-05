using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CustomNodeSaver
{
    private static Dictionary<string, System.Type> baseNodeTypes;
    private static HashSet<string> customNodeTypes;
    private static HashSet<string> allNodeTypes;

    private static readonly List<System.Type> excludeNodeTypes = new List<System.Type>() { typeof(CustomNode), typeof(PortFwdNode) };

    public static void GetNodeTypes()
    {
        List<System.Type> baseNodeTypeTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(Node)) && !excludeNodeTypes.Contains(type) && !type.IsAbstract).ToList();

        baseNodeTypes = baseNodeTypeTypes.Select(type => type.Name).Zip(baseNodeTypeTypes, (k, v) => (k, v)).ToDictionary(x => x.k, x => x.v);

        customNodeTypes = Directory.GetFiles(Application.persistentDataPath, "*.json").Select(filename => Path.GetFileNameWithoutExtension(filename)).ToHashSet();

        allNodeTypes = new HashSet<string>(baseNodeTypes.Keys);
        allNodeTypes.UnionWith(customNodeTypes);
    }

    public static string[] QueryNodeTypes(string query)
    {
        if (allNodeTypes == null)
            GetNodeTypes();

        List<string> matchingTypeNames = new List<string>(allNodeTypes.Count);

        foreach (string typeName in allNodeTypes)
        {
            if (typeName.ToLower().Contains(query.ToLower()))
                matchingTypeNames.Add(typeName);
        }

        return matchingTypeNames.ToArray();
    }
    public static bool IsCustomNode(string typeName)
    {
        if (customNodeTypes == null)
            GetNodeTypes();
        return customNodeTypes.Contains(typeName);
    }

    public static void SaveCustomNode(CustomNode customNode)
    {
        customNode.GenerateSaveData();
        JsonSaver.SaveData(customNode.externalData.Name, customNode);
        GetNodeTypes();
    }

    public static CustomNode LoadCustomNode(string name)
    {
        if (customNodeTypes == null)
            GetNodeTypes();

        CustomNode customNode;
        try
        {
            if (!customNodeTypes.Contains(name))
                throw new System.Exception("Custom Node not found");
            customNode = JsonSaver.LoadData<CustomNode>(name);
        }
        catch
        {
            try
            {
                JsonSaver.SaveData(name, JsonSaver.LoadResource<CustomNode>(name));
                customNode = JsonSaver.LoadData<CustomNode>(name);
            }
            catch
            {
                Debug.LogError($"Custom Node {name} not found");
                return null;
            }
        }

        for (int i = 0; i < customNode.saveSubNodes.Length; i++)
        {
            Node.ExternalData externalData = customNode.saveSubNodes[i];
            Node subnode = NewNode(externalData.Name);
            subnode.externalData = externalData;
            customNode.TryAddSubnode(subnode);
        }

        return customNode;
    }

    public static Node NewNode(string name)
    {
        if (allNodeTypes == null)
            GetNodeTypes();

        if (baseNodeTypes.ContainsKey(name))
            return (Node)System.Activator.CreateInstance(baseNodeTypes[name]);
        else
            return LoadCustomNode(name);
    }
}