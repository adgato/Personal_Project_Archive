using UnityEngine.Rendering;
using UnityEngine.Rendering.CustomRenderPipeline;
public static class CurrentCRP
{
    public static Optional<CRPAsset> GetAsset()
    {
        if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.GetType() == typeof(CRPAsset))
            return new Optional<CRPAsset>((CRPAsset)GraphicsSettings.currentRenderPipeline);
        else
            return new Optional<CRPAsset>();
    }
}
