using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "PostProcessing/ScreenEffectsHandler")]
public class ScreenEffectsHandler : EffectsHandler
{
    public Material screenEffects;
    public bool enabled;
    public bool debug;

    public override List<Material> GetMaterials()
    {
        if (Application.isPlaying && enabled || debug)
            return new List<Material>(1) { screenEffects };

        return new List<Material>();
    }
}
