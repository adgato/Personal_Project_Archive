using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PlanetData))]
public class PlanetWeight : Weight
{
    [HideInInspector] public PlanetData PlanetData;
    bool collidersUpToDate;

    public override float Radius => PlanetData.PlanetTerrain.OceanRadius;

    protected override void Start()
    {
        base.Start();
        PlanetData = GetComponent<PlanetData>();
        PlanetData.Initialise();
    }

    public override void PreUpdate()
    {
        collidersUpToDate = false;
        base.PreUpdate();
    }

    public override void UpdateColliders()
    {
        if (collidersUpToDate)
            return;
        PlanetData.UpdateColliders(gravity.GetZeroWeights().Where(x => x.EqualsClosest(this)));
        collidersUpToDate = true;
        base.UpdateColliders();
    }

    public void LateUpdate()
    {
        if (ControlSaver.GamePaused)
            return;
        PlanetData.PlanetEffects.SetPlanetPosition(baseTransform.position);
    }
}
