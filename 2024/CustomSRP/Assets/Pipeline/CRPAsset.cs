using UnityEngine.Experimental.Rendering;
namespace UnityEngine.Rendering.CustomRenderPipeline
{
    [CreateAssetMenu(fileName = "New CRPAsset", menuName = "Rendering/CRPAsset")]
    public class CRPAsset : RenderPipelineAsset
    {
#if UNITY_EDITOR
        public LayerSchemaAsset EditorLayerSchema => LayerSchema;
        [HideInInspector] public bool EditorShowSceneViewGizmos;
        [HideInInspector] public bool EditorShowGameViewGizmos;
#endif
        public GraphicsBuffer GBuffer { get; private set; }
        public DoubleBuffer DBuffer { get; private set; }

        [SerializeField] internal CRPassDraw[] DrawPasses;
        [SerializeField] internal LayerSchemaAsset LayerSchema;
        [SerializeField] internal CRPassPost[] BlitPasses;
        [SerializeField] internal CDirectionalLightWatcher dLightWatcher;
        [SerializeField] internal CPointLightWatcher pLightWatcher;

        private RTHandleSystem RTSystem;

        protected override RenderPipeline CreatePipeline()
        {
            if (!FullyConfigured())
                Initialise();

            return new CRP(this);
        }

        public void Initialise()
        {
            LBlitter.Initialise();
            AllocateRTHandles();
            LayerSchema.Initialise(this);
            if (BlitPasses == null)
                return;
            for (int i = 0; i < BlitPasses.Length; i++)
                BlitPasses[i].Initialise(this);
            if (DrawPasses == null)
                return;
            for (int i = 0; i < DrawPasses.Length; i++)
                DrawPasses[i].Initialise(this);
            if (dLightWatcher != null)
                dLightWatcher.OnChange();
            if (pLightWatcher != null)
                pLightWatcher.OnChange();
        }

        private void AllocateRTHandles()
        {
            if (Camera.main == null)
                return;
            RTSystem = new RTHandleSystem();
            RTSystem.Initialize(Camera.main.pixelWidth, Camera.main.pixelHeight);

            GBuffer = GetGraphicsBuffer();

            DBuffer = new DoubleBuffer(FullScreenColourHandle(), FullScreenColourHandle());
        }

        private GraphicsBuffer GetGraphicsBuffer()
        {
            GraphicsBuffer GBuffer = new GraphicsBuffer(3);
            GBuffer[CRPTarget.COLOUR] = FullScreenColourHandle();
            GBuffer[CRPTarget.NORMAL] = RTSystem.Alloc(Vector2.one, colorFormat: GraphicsFormat.R16G16_SNorm);
            GBuffer[CRPTarget.LAYER] = RTSystem.Alloc(Vector2.one, colorFormat: GraphicsFormat.R32_SFloat);
            GBuffer.DepthBuffer = RTSystem.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth32);
            GBuffer.MakeReadOnly();
            return GBuffer;
        }

        private RTHandle FullScreenColourHandle() => RTSystem.Alloc(Vector2.one, colorFormat: GraphicsFormat.R8G8B8A8_UNorm);



        public bool FullyConfigured()
        {
            if (LBlitter.BlitCopyMaterial == null || DrawPasses == null || BlitPasses == null || LayerSchema == null || !LayerSchema.Configured() || DBuffer == null || !DBuffer.Allocated() || GBuffer == null || !GBuffer.Allocated())
                return false;
             
            for (int i = 0; i < BlitPasses.Length; i++)
                if (BlitPasses[i] == null || !BlitPasses[i].Configured())
                    return false;

            for (int i = 0; i < DrawPasses.Length; i++)
                if (DrawPasses[i] == null || !DrawPasses[i].Configured())
                    return false;

            return true;
        }
    }
}
