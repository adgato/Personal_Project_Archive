using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class NodeLoader
{
    private static Dictionary<string, System.Type> nodeTypes;

    private static readonly List<System.Type> excludeNodeTypes = new List<System.Type>() { typeof(CustomNode), typeof(Forwarder) };

    public static void GetNodeTypes()
    {
        List<System.Type> nodeSystemTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(Node)) && !excludeNodeTypes.Contains(type) && !type.IsAbstract).ToList();

        List<string> nodeTypeNames = nodeSystemTypes.Select(type => type.Name)
            .Concat(
            Directory.GetDirectories(Application.persistentDataPath)
            .Select(directory => directory.Remove(0, Application.persistentDataPath.Length + 1))
            ).ToList();

        while (nodeSystemTypes.Count < nodeTypeNames.Count)
            nodeSystemTypes.Add(null);

        nodeTypes = nodeTypeNames.Zip(nodeSystemTypes, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
    }

    public static string[] QueryNodeTypes(string query)
    {
        if (nodeTypes == null)
            GetNodeTypes();

        List<string> matchingTypeNames = new List<string>(nodeTypes.Keys.Count);

        foreach (string typeName in nodeTypes.Keys)
        {
            if (typeName.ToLower().Contains(query.ToLower()))
                matchingTypeNames.Add(typeName);
        }

        return matchingTypeNames.ToArray();
    }

    public static Node LoadNode(string directory, ref List<Arc> arcs)
    {
        string typeName = JsonSaver.LoadData<Node.NodeHeader>(directory + "/header").nodeType;

        Node node = MakeNode(typeName);
        node.Load(directory, ref arcs);

        return node;
    }

    public static Node NewNode(string typeName)
    {
        Node node = MakeNode(typeName);
        node.New();

        return node;
    }

    private static Node MakeNode(string typeName)
    {
        if (nodeTypes == null)
            GetNodeTypes();

        if (!nodeTypes.Keys.Contains(typeName))
            return null;

        return nodeTypes[typeName] == null ? new CustomNode(typeName) : (Node)System.Activator.CreateInstance(nodeTypes[typeName]);
    }
}