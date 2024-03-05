using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BlitMaterial : System.IDisposable
{
    private static List<BlitMaterial> blitMaterials = new List<BlitMaterial>();
    private static bool ordered;

    public Material Material { get; private set; }
    public int Layer { get; private set; }

    private bool added;
    private readonly bool locked;

    public BlitMaterial(Material material, int layer)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Error: blit materials only allowed in game mode");
            while (blitMaterials.Count > 0)
                blitMaterials[0].Dispose();
            return;
        }

        locked = blitMaterials.Select(x => x.Material).Contains(material);
        if (locked)
            Debug.LogWarning("Warning: this material is already a blitMaterial so will be ignored");

        added = false;

        Material = material;
        ChangeLayer(layer);
    }

    public void Enable()
    {
        if (added || locked)
            return;
        blitMaterials.Add(this);
        ordered = false;
        added = true;
    }

    public void ChangeLayer(int layer)
    {
        Layer = layer;
        ordered = !added || locked;
    }

    public void Dispose()
    {
        if (added)
            blitMaterials.Remove(this);
        added = true;
        Object.Destroy(Material);
        Material = null;
    }

    public static List<BlitMaterial> GetMaterials()
    {
        if (!ordered)
            blitMaterials = blitMaterials.OrderByDescending(x => x.Layer).ToList();
        ordered = true;
        return blitMaterials;
    }
    public static int MaterialsCount()
    {
        return blitMaterials.Count;
    }

}