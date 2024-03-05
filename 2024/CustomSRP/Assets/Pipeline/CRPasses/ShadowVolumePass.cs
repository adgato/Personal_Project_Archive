namespace UnityEngine.Rendering.CustomRenderPipeline
{
    [CreateAssetMenu(fileName = "ShadowVolumePass", menuName = "Rendering/CRPass/ShadowVolume")]
    internal class ShadowVolumePass : CRPassDraw
    {
        DrawingSettings frontSettings;
        DrawingSettings backSettings;
        DrawingSettings shadowWriteSettings;
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        bool configured = false;

        internal override void Initialise(CRPAsset A)
        {
            frontSettings = new DrawingSettings(ShaderPassTag.shadowFront, default)
            {
                enableDynamicBatching = false,
                enableInstancing = true,
                perObjectData = PerObjectData.None
            };
            backSettings = new DrawingSettings(ShaderPassTag.shadowBack, default)
            {
                enableDynamicBatching = false,
                enableInstancing = true,
                perObjectData = PerObjectData.None
            };
            shadowWriteSettings = new DrawingSettings(ShaderPassTag.shadowWrite, default)
            {
                enableDynamicBatching = false,
                enableInstancing = true,
                perObjectData = PerObjectData.None
            };
            configured = true;
        }

        internal override bool Configured() => configured;

        internal override void Execute(ref ScriptableRenderContext context, CullingResults cullingResults, Camera camera)
        {
            if (SpiderMovement.inReflectedWorld == -1)
                return;

            SortingSettings sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            frontSettings.sortingSettings = sortingSettings;
            backSettings.sortingSettings = sortingSettings;
            shadowWriteSettings.sortingSettings = sortingSettings;

            

            RendererListParams rendererListParams = new RendererListParams(cullingResults, frontSettings, filteringSettings);

            ScriptableRenderContext localcontext = context;

            context.ExecuteCommandBufferSeq(cmd =>
            {
                cmd.DrawRendererList(localcontext.CreateRendererList(ref rendererListParams));
                rendererListParams.drawSettings = backSettings;
                cmd.DrawRendererList(localcontext.CreateRendererList(ref rendererListParams));
                rendererListParams.drawSettings = shadowWriteSettings;
                cmd.DrawRendererList(localcontext.CreateRendererList(ref rendererListParams));
            });
        }
    }
}