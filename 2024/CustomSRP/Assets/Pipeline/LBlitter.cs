namespace UnityEngine.Rendering
{
    /// <summary>
    /// "Lightweight" Blitter. A barebones version of the <see cref="Blitter"/> API that I dissected because the documentation on the rest was frustratingly sparse.
    /// </summary>
    public static class LBlitter
    {
        private static Mesh triangleMesh;
        private static MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();

        public static Material BlitCopyMaterial { get; private set; }
        public const int DefaultNearestPass = 0;
        public const int DefaultBilinearPass = 1;

        public static class ShaderID
        {
            public static int _MainTex = Shader.PropertyToID("_MainTex");
            public static int _NormalTex = Shader.PropertyToID("_NormalTex");
            public static int _DepthTex = Shader.PropertyToID("_DepthTex");
            public static int _LayerTex = Shader.PropertyToID("_LayerTex");
            public static int _MirrorTex = Shader.PropertyToID("_MirrorTex");
            public static int _BlitTexture = Shader.PropertyToID("_BlitTexture");
            public static int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static int _BlitMipLevel = Shader.PropertyToID("_BlitMipLevel");
        }

        /// <summary>
        /// Initialise Blitter resources. Must be called once before any use.
        /// </summary>
        public static void Initialise()
        {
            BlitCopyMaterial = new Material(Shader.Find("Hidden/Blit"));

            if (SystemInfo.graphicsUVStartsAtTop)
                Shader.EnableKeyword("UNITY_UV_STARTS_AT_TOP");
            else
                Shader.DisableKeyword("UNITY_UV_STARTS_AT_TOP");

            if (SystemInfo.graphicsShaderLevel >= 30)
                return;

            /*UNITY_NEAR_CLIP_VALUE*/
            float nearClipZ = -1;
            if (SystemInfo.usesReversedZBuffer)
                nearClipZ = 1;

            if (!triangleMesh)
            {
                triangleMesh = new Mesh();
                triangleMesh.vertices = GetFullScreenTriangleVertexPosition(nearClipZ);
                triangleMesh.uv = GetFullScreenTriangleTexCoord();
                triangleMesh.triangles = new int[3] { 0, 1, 2 };
            }

            // Should match Common.hlsl
            static Vector3[] GetFullScreenTriangleVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
            {
                var r = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    Vector2 uv = new Vector2((i << 1) & 2, i & 2);
                    r[i] = new Vector3(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, z);
                }
                return r;
            }

            // Should match Common.hlsl
            static Vector2[] GetFullScreenTriangleTexCoord()
            {
                var r = new Vector2[3];
                for (int i = 0; i < 3; i++)
                {
                    if (SystemInfo.graphicsUVStartsAtTop)
                        r[i] = new Vector2((i << 1) & 2, 1.0f - (i & 2));
                    else
                        r[i] = new Vector2((i << 1) & 2, i & 2);
                }
                return r;
            }
        }

        /// <summary>
        /// Release Blitter resources.
        /// </summary>
        public static void Cleanup()
        {
            CoreUtils.Destroy(triangleMesh);
            triangleMesh = null;
            CoreUtils.Destroy(BlitCopyMaterial);
            BlitCopyMaterial = null;
        }

        /// <summary>
        /// Blits to the current render target. Assumes everything has been configured in advance.
        /// </summary>
        /// <param name="material">Material to blit through.</param>
        /// <param name="pass">The pass of the material to use.</param>
        public static void Blit(CommandBuffer cmd, Material material, int pass = 0)
        {
            if (SystemInfo.graphicsShaderLevel < 30)
                cmd.DrawMesh(triangleMesh, Matrix4x4.identity, material, 0, pass, materialProperties);
            else
                cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, materialProperties);
        }

        /// <summary>
        /// Blit <paramref name="source"/> to current render target.
        /// </summary>
        /// <param name="material">Material to blit through.</param>
        /// <param name="sourcePropertyID">The ID of the source texture property. May find this method helpful: <code><see cref="Shader.PropertyToID(string)"/></code></param>
        /// <param name="viewportScalePropertyID">The ID of the viewport scale vector property. May find this method helpful: <code><see cref="Shader.PropertyToID(string)"/></code></param>
        /// <param name="sourceSubElement">Which subelement of the source to blit.</param>
        /// <param name="pass">The pass of the material to use.</param>
        public static void Blit(CommandBuffer cmd, RTHandle source, Material material, int sourcePropertyID, int viewportScalePropertyID, RenderTextureSubElement sourceSubElement = RenderTextureSubElement.Default, int pass = 0)
        {
            Vector4 viewportScale = source.useScaling ? new Vector4(source.scaleFactor.x, source.scaleFactor.y, 0, 0) : new Vector4(1, 1, 0, 0);
            //viewportScale.w += viewportScale.y;
            //viewportScale.y *= -1;
            materialProperties.SetTexture(sourcePropertyID, source, sourceSubElement);
            materialProperties.SetVector(viewportScalePropertyID, viewportScale);
            Blit(cmd, material, pass);
        }

        /// <summary>
        /// Blit <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="material">Material to blit through.</param>
        /// <param name="sourcePropertyID">The ID of the source texture property. May find this method helpful: <code><see cref="Shader.PropertyToID(string)"/></code></param>
        /// <param name="viewportScalePropertyID">The ID of the viewport scale vector property. May find this method helpful: <code><see cref="Shader.PropertyToID(string)"/></code></param>
        /// <param name="sourceSubElement">Which subelement of the source to blit.</param>
        /// <param name="pass">The pass of the material to use.</param>
        public static void Blit(CommandBuffer cmd, RTHandle source, RenderTargetIdentifier destination, Material material, int sourcePropertyID, int viewportScalePropertyID, RenderTextureSubElement sourceSubElement = RenderTextureSubElement.Default, int pass = 0)
        {
            cmd.SetRenderTarget(destination);
            Blit(cmd, source, material, sourcePropertyID, viewportScalePropertyID, sourceSubElement, pass);
        }

        /// <summary>
        /// Blits to the current render target. Automatically sets the material properties. <see cref="ShaderID._MainTex"/> is used for the source texture. <see cref="ShaderID._BlitScaleBias"/> is used for the viewport scale vector.
        /// </summary>
        /// <param name="material">Material to blit through.</param>
        /// <param name="sourceSubElement">Which subelement of the source to blit.</param>
        /// <param name="pass">The pass of the material to use.</param>
        public static void Blit(CommandBuffer cmd, RTHandle source, RenderTargetIdentifier destination, Material material, RenderTextureSubElement sourceSubElement = RenderTextureSubElement.Default, int pass = 0) =>
            Blit(cmd, source, destination, material, ShaderID._MainTex, ShaderID._BlitScaleBias, sourceSubElement, pass);

        /// <summary>
        /// Blits to the current render target. Automatically sets the material properties. <see cref="ShaderID._MainTex"/> is used for the source texture. <see cref="ShaderID._BlitScaleBias"/> is used for the viewport scale vector.
        /// </summary>
        /// <param name="material">Material to blit through.</param>
        /// <param name="sourceSubElement">Which subelement of the source to blit.</param>
        /// <param name="pass">The pass of the material to use.</param>
        public static void Blit(CommandBuffer cmd, RTHandle source, Material material, RenderTextureSubElement sourceSubElement = RenderTextureSubElement.Default, int pass = 0) =>
            Blit(cmd, source, material, ShaderID._MainTex, ShaderID._BlitScaleBias, sourceSubElement, pass);
    }
}
