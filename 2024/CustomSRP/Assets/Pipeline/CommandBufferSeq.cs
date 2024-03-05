using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.CustomRenderPipeline
{
    /// <summary>
    /// Intended to make command buffer pipelines a bit neater.
    /// </summary>
    internal static class CommandBufferSeq
    {
        public static void ExecuteCommandBufferSeq(this ScriptableRenderContext context, Action<CommandBuffer> CommandSequence)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            CommandSequence(cmd);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
    public abstract class CRPassPost : ScriptableObject
    {
        internal virtual void Initialise(CRPAsset A) { }
        internal abstract bool Configured();
        internal abstract void Execute(ref ScriptableRenderContext context, DoubleBuffer DBuffer);
    }
    public abstract class CRPassDraw : ScriptableObject
    {
        internal virtual void Initialise(CRPAsset A) { }
        internal abstract bool Configured();
        internal abstract void Execute(ref ScriptableRenderContext context, CullingResults cullingResults, Camera camera);
    }
}

