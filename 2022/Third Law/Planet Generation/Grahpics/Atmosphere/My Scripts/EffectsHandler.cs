using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "PostProcessing/EffectsHandler")]
public class EffectsHandler : ScriptableObject
{
    PlanetEffect[] allEffects;

    List<PlanetEffect> GetActiveEffectsByDist()
    {
        //Should contain all four planet effects
        if (allEffects == null || allEffects.Length < 4)
            allEffects = FindObjectsOfType<PlanetEffect>(true);

        List<PlanetEffect> effects = new List<PlanetEffect>();
        foreach (PlanetEffect effect in allEffects)
        {
            if (effect == null)
            {
                allEffects = FindObjectsOfType<PlanetEffect>(true);
                continue;
            }
            if (effect.active && effect.transform.parent.gameObject.activeSelf)
            {
                effect.UpdateInfo();
                effects.Add(effect);
            }
        }
        effects = effects.OrderBy(x => x.cameraSqrDist).ToList();
        effects.Reverse();
        return effects;
    }
    public virtual List<Material> GetMaterials()
    {
        List<PlanetEffect> effects = GetActiveEffectsByDist();

        List<Material> materials = new List<Material>();

        foreach (PlanetEffect _effect in effects)
            materials.Add(_effect.GetMaterial());

        return materials;
    }
}
