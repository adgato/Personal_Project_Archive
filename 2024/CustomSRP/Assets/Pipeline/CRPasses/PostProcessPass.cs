using System;

namespace UnityEngine.Rendering.CustomRenderPipeline
{
    [CreateAssetMenu(fileName = "PostProcessPass", menuName = "Rendering/CRPass/PostProcess")]
    internal class PostProcessPass : CRPassPost
    {
        [SerializeField] private Material[] postProcessMats;

        internal override bool Configured()
        {
            if (postProcessMats == null)
                return false;
            for (int i = 0; i < postProcessMats.Length; i++)
                if (postProcessMats[i] == null)
                    return false;

            return true;
        }

        internal override void Execute(ref ScriptableRenderContext context, DoubleBuffer DBuffer)
        {
            context.ExecuteCommandBufferSeq(cmd =>
            {
                for (int i = 0; i < postProcessMats.Length; i++)
                    LBlitter.Blit(cmd, DBuffer.Current, DBuffer.MoveNext(), postProcessMats[i]);
            });
        }
    }
}