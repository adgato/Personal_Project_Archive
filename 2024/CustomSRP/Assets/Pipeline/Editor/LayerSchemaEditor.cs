using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.CustomRenderPipeline;
using System.Linq;
using System;

[CustomEditor(typeof(LayerSchemaAsset))]
public class LayerSchemaEditor : Editor
{
    private bool toggle;
    private bool valid = true;
    private const int MaxFields = 30;
    private static ShaderTagId LayersTag;
    private void OnEnable()
    {
        LayersTag = new ShaderTagId("Layers");
        valid = true;
    }

    public override void OnInspectorGUI()
    {
        LayerSchemaAsset schema = (LayerSchemaAsset)target;

        if (valid && GUILayout.Button("Initialise Deferred Layer Mask Macros"))
        {
            InitialiseMacros(schema);
            CRPAsset asset = null;
            CurrentCRP.GetAsset().TryGet(ref asset);
            if (asset != null)
                asset.Initialise();
        }
        else if (valid && GUILayout.Button("Optimise Deferred Layer Mask Macros"))
        {
            OptimiseMacros(schema);
            CRPAsset asset = null;
            CurrentCRP.GetAsset().TryGet(ref asset);
            if (asset != null)
                asset.Initialise();
        }
        else if (!valid)
            EditorGUILayout.HelpBox("Error: Deferred Pass Material is not correctly set up. It requires a _MainTex and _LayerTex property.", MessageType.Error);

        toggle = EditorGUILayout.BeginFoldoutHeaderGroup(toggle, "Schema format:");
        if (toggle)
        {
            EditorGUILayout.LabelField("Every shader that writes to the layer buffer should have a \"Layer\" Tag.");
            EditorGUILayout.LabelField("Every pass deferred by writing to the layer buffer is a material listed below.");
            EditorGUILayout.LabelField("If a shader wants to defer to a material below, it should include it's name in the \"Layer\" Tag.");
            EditorGUILayout.LabelField("Deferring to multiple materials can be achieved by space seperating their names in the Tag.");
            EditorGUILayout.LabelField("The corresponding value to set the layer buffer to is defined in the Layers Data file.");
            EditorGUILayout.LabelField("If the Tags have been set up correctly, and you press the Optimise button above...");
            EditorGUILayout.LabelField("You will be able to OR together your selected layers to write to the layer buffer.");
            EditorGUILayout.LabelField("Please refer to working examples to get a better idea for how this works.");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
            Validate(schema);
    }

    private void Validate(LayerSchemaAsset schema)
    {
        valid = true;
        for (int i = 0; i < schema.DeferredPassMats.Length; i++)
            if (!schema.DeferredPassMats[i].shader.HasProperty("_MainTex", ShaderPropertyType.Texture) ||
                !schema.DeferredPassMats[i].shader.HasProperty("_LayerTex", ShaderPropertyType.Texture))
            {
                schema.DeferredPassMats[i] = null;
                valid = false;
            }
    }

    private static void SaveLayersData(TextAsset file, string text)
    {
        if (file == null)
        {
            Debug.LogError("Layers Data file has not been assigned");
            return;
        }
        string path = AssetDatabase.GetAssetPath(file);
        System.IO.File.WriteAllText(path, text);
        AssetDatabase.Refresh();
        Debug.Log("Layer Data saved: " + path);
    }

    private static void InitialiseMacros(LayerSchemaAsset schema)
    {
        if (schema.DeferredPassMats.Length > MaxFields)
            Debug.LogWarning("Warning: Too many Deferred Pass Materials, cannot set more than " + MaxFields + " unique fields. Please press Optimise button.");
        string text = "";
        schema.EditorDeferredNames = new string[schema.DeferredPassMats.Length + 1];
        schema.EditorDeferredLayers = new uint[schema.DeferredPassMats.Length + 1];
        schema.EditorDeferredMasks = new uint[schema.DeferredPassMats.Length + 1];
        schema.EditorDeferredNames[0] = "Background";
        schema.EditorDeferredLayers[0] = 0u;
        schema.EditorDeferredMasks[0] = uint.MaxValue;
        for (int i = 0; i < schema.DeferredPassMats.Length; i++)
        {
            string name = schema.DeferredPassMats[i].name.Replace(" ", "_");
            uint layermask = 1u << i;
            schema.EditorDeferredNames[i + 1] = name;
            schema.EditorDeferredLayers[i + 1] = layermask;
            schema.EditorDeferredMasks[i + 1] = layermask;
            text += $"#define Layer_{name} {layermask}u\n";
            text += $"#define Mask_{name} {layermask}u\n\n";
        }
        SaveLayersData(schema.EditorGetLayersData, text);
    }

    private static void OptimiseMacros(LayerSchemaAsset schema)
    {
        Dictionary<string, int> materialIndexLookup = new Dictionary<string, int>();
        for (int i = 0; i < schema.DeferredPassMats.Length; i++)
            materialIndexLookup[schema.DeferredPassMats[i].name.Replace(" ", "_")] = i;

        List<List<int>> groups = new List<List<int>>();

        //Add a group for each shader's required deferred pass materials.
        ShaderInfo[] allShaders = ShaderUtil.GetAllShaderInfo();
        foreach (ShaderInfo shaderInfo in allShaders)
        {
            Shader shader = Shader.Find(shaderInfo.name);
            if (shader == null)
                continue;
            
            for (int i = 0; i < shader.passCount; i++)
            {
                string layers = (string)shader.FindPassTagValue(i, LayersTag);
                if (layers == null || layers == string.Empty)
                    continue;
                groups.Add(layers.Split(' ').Select(x => materialIndexLookup[x]).ToList());
            }
        }
        //Create a new list of groups, where elements that were in the same group are no longer.
        //Each group contains deferred passes that do not need to be applied in sequence. Hence they can be stored in the same bit field.
        List<List<int>> fields = IsolateGroups(groups);
        int shift = 0;
        int bitSize = 0;
        string text = "";
        //Create these bit fields
        List<string> deferredNames = new List<string>() { "Background" };
        List<uint> deferredLayers = new List<uint>() { 0u };
        List<uint> deferredMasks = new List<uint>() { uint.MaxValue };
        foreach (List<int> group in fields)
        {
            bitSize = Mathf.CeilToInt(Mathf.Log(group.Count + 1, 2));
            uint mask = 0u;
            for (int i = 0; i < bitSize; i++)
                mask |= 1u << (shift + i);
            for (int i = 0; i < group.Count; i++)
            {
                string name = schema.DeferredPassMats[group[i]].name.Replace(" ", "_");
                uint layer = (uint)(i + 1) << shift;
                deferredNames.Add(name);
                deferredLayers.Add(layer);
                deferredMasks.Add(mask);
                text += $"#define Layer_{name} {layer}u //0b{Convert.ToString(layer, 2)}\n";
                text += $"#define Mask_{name} {mask}u //0b{Convert.ToString(mask, 2)}\n\n";
            }
            shift += bitSize;
        }
        schema.EditorDeferredNames = deferredNames.ToArray();
        schema.EditorDeferredLayers = deferredLayers.ToArray();
        schema.EditorDeferredMasks = deferredMasks.ToArray();

        if (shift - bitSize > MaxFields)
            Debug.LogWarning("Warning: Too many Deferred Pass Materials in sequence, cannot set more than " + MaxFields + " unique fields. Consider using fewer deferred pass chains.");

        SaveLayersData(schema.EditorGetLayersData, text);
    }

    /// <summary>
    /// Return a set of sets, where if any elements are in the same input set, they cannot be in the same output set. This output set of sets has a small cardinaity (probably very close to smallest).
    /// </summary>
    private static List<List<T>> IsolateGroups<T>(List<List<T>> list)
    {
        List<HashSet<T>> X = list.Select(xs => xs.ToHashSet()).ToList();
        List<HashSet<T>> Y = new List<HashSet<T>>();
        HashSet<T> unionX = new HashSet<T>();
        foreach (HashSet<T> xs in X)
            unionX.UnionWith(xs);
        foreach (T x in unionX)
        {
            bool added = false;
            foreach (HashSet<T> ys in Y)
                if (!X.Any(xs => xs.Contains(x) && xs.Any(x => ys.Contains(x))))
                {
                    ys.Add(x);
                    added = true;
                    break;
                }
            if (!added)
                Y.Add(new HashSet<T> { x });
        }
        return Y.Select(ys => ys.ToList()).ToList();
    }
}
