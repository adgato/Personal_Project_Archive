namespace UnityEngine.Rendering.CustomRenderPipeline
{
    public class GraphicsBuffer : IRTHandleHolder
    {
        private RTHandle[] colourBuffers;
        private RTHandle depthBuffer;
        private RenderTargetIdentifier[] colourBufferIDs;
        /// <summary>
        /// The number of colour buffers in the GBuffer.
        /// </summary>
        public readonly int Length;

        private bool isReadonly;

        public GraphicsBuffer(int colourBufferCount)
        {
            Length = colourBufferCount;
            colourBuffers = new RTHandle[Length];
            colourBufferIDs = new RenderTargetIdentifier[Length];
            isReadonly = false;
        }

        public RTHandle DepthBuffer
        {
            get => depthBuffer;
            set
            {
                if (isReadonly)
                    Debug.LogWarning("Warning: Cannot modify GBuffer as it has been made read only.");
                else
                    depthBuffer = value;
            }
        }

        public RTHandle this[int i]
        {
            get => colourBuffers[i];
            set
            {
                if (isReadonly)
                    Debug.LogWarning("Warning: Cannot modify GBuffer as it has been made read only.");
                else
                {
                    colourBuffers[i] = value;
                    colourBufferIDs[i] = value;
                }
            }
        }
        public void Release(RTHandleSystem RTSystem)
        {
            for (int i = 0; i < Length; i++)
                RTSystem.Release(colourBuffers[i]);
            RTSystem.Release(depthBuffer);
            colourBuffers = null;
            colourBufferIDs = null;
            depthBuffer = null;
        }

        public void MakeReadOnly() => isReadonly = true;
        public bool Allocated()
        {
            if (colourBuffers == null)
                return false;
            for (int i = 0; i < Length; i++)
                if (colourBuffers[i] == null)
                    return false;
            return depthBuffer != null;
        }

        public static implicit operator RTHandle[](GraphicsBuffer gBuffer) => gBuffer.colourBuffers;
        public static implicit operator RenderTargetIdentifier[](GraphicsBuffer gBuffer) => gBuffer.colourBufferIDs;
    }
}

