namespace UnityEngine.Rendering.CustomRenderPipeline
{
    [CreateAssetMenu(fileName = "New Layer Schema", menuName = "Rendering/CRPass/LayerSchema")]
    public class LayerSchemaAsset : CRPassPost
    {
#if UNITY_EDITOR
        [SerializeField] private TextAsset layersData;
        public TextAsset EditorGetLayersData => layersData;
        [HideInInspector] public string[] EditorDeferredNames;
        [HideInInspector] public uint[] EditorDeferredLayers;
        [HideInInspector] public uint[] EditorDeferredMasks;
#endif
        public Material[] DeferredPassMats => deferredPassMats;
        [SerializeField] private Material[] deferredPassMats = new Material[0];

        internal override void Initialise(CRPAsset A) => SetLayeredPassProperties(A.GBuffer);

        private void SetLayeredPassProperties(GraphicsBuffer GBuffer)
        {
            for (int i = 0; i < deferredPassMats.Length; i++)
            {
                Material mat = deferredPassMats[i];
                mat.SetTexture(LBlitter.ShaderID._NormalTex, GBuffer[CRPTarget.NORMAL], RenderTextureSubElement.Color);
                mat.SetTexture(LBlitter.ShaderID._DepthTex, GBuffer.DepthBuffer, RenderTextureSubElement.Depth);
                mat.SetTexture(LBlitter.ShaderID._LayerTex, GBuffer[CRPTarget.LAYER], RenderTextureSubElement.Color);
            }
        }

        internal override bool Configured()
        {
            if (deferredPassMats == null)
                return false;
            for (int i = 0; i < deferredPassMats.Length; i++)
                if (deferredPassMats[i] == null)
                    return false;

            return true;
        }

        internal override void Execute(ref ScriptableRenderContext context, DoubleBuffer DBuffer)
        {
            context.ExecuteCommandBufferSeq(cmd =>
            {
                for (int i = 0; i < deferredPassMats.Length; i++)
                    LBlitter.Blit(cmd, DBuffer.Current, DBuffer.MoveNext(), deferredPassMats[i]);
            });
        }
    }
}

