using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.CustomRenderPipeline
{
    // Should match CRPHelper.cginc
    public static class CRPTarget
    {
        public const int COLOUR = 0;
        public const int NORMAL = 1;
        public const int LAYER = 2;
    }

    internal class CRP : RenderPipeline
    {
        // Keeping all variables in the asset allows them to be reassigned on reload.
        private readonly CRPAsset A;

        public CRP(CRPAsset asset)
        {
            A = asset;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            ShaderBindings.SetPerFrameShaderVariables(context);

            foreach (Camera camera in cameras)
            {
                context.SetupCameraProperties(camera);


                CullingResults cullingResults = Cull(context, camera);

#if UNITY_EDITOR
                if (camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview)
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                    DrawSceneView(context, cullingResults, camera);
                    continue;
                }
#endif
                ShaderBindings.SetPerCameraShaderVariables(context, camera);
                if (A.FullyConfigured())
                    Draw(context, cullingResults, camera);
            }
        }

        CullingResults Cull(ScriptableRenderContext context, Camera camera)
        {
            // Culling. Adjust culling parameters for your needs. One could enable/disable
            // per-object lighting or control shadow caster distance.
            camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
            return context.Cull(ref cullingParameters);
        }

        void Draw(ScriptableRenderContext context, CullingResults cullingResults, Camera camera)
        {
            context.ExecuteCommandBufferSeq(cmd =>
            {
                cmd.SetRenderTarget(A.GBuffer, A.GBuffer.DepthBuffer);
                cmd.ClearRenderTarget(true, true, Vector4.zero);
                cmd.SetRenderTarget(A.GBuffer[CRPTarget.COLOUR]);
                cmd.ClearRenderTarget(false, true, camera.backgroundColor);

                cmd.SetRenderTarget(A.GBuffer, A.GBuffer.DepthBuffer);
            });

            SortingSettings sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            // ShaderTagId must match the "LightMode" tag inside the shader pass.
            // If not "LightMode" tag is found the object won't render.
            DrawingSettings drawingSettings = new DrawingSettings(ShaderPassTag.example, sortingSettings)
            {
                enableDynamicBatching = false,
                enableInstancing = true,
                perObjectData = PerObjectData.None
            };

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            RendererListParams rendererListParams = new RendererListParams(cullingResults, drawingSettings, filteringSettings);

            // Renders skybox if required
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
                context.DrawSkybox(camera);

            context.ExecuteCommandBufferSeq(cmd => cmd.DrawRendererList(context.CreateRendererList(ref rendererListParams)));

            for (int i = 0; i < A.DrawPasses.Length; i++)
                A.DrawPasses[i].Execute(ref context, cullingResults, camera);


            context.ExecuteCommandBufferSeq(cmd => LBlitter.Blit(cmd, A.GBuffer[CRPTarget.COLOUR], A.DBuffer.Current, LBlitter.BlitCopyMaterial));

            A.LayerSchema.Execute(ref context, A.DBuffer);
            for (int i = 0; i < A.BlitPasses.Length; i++)
                A.BlitPasses[i].Execute(ref context, A.DBuffer);

            
            context.ExecuteCommandBufferSeq(cmd => LBlitter.Blit(cmd, A.DBuffer.Current, BuiltinRenderTextureType.CameraTarget, LBlitter.BlitCopyMaterial));

#if UNITY_EDITOR
            if (A.EditorShowGameViewGizmos)
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
#endif

            context.Submit();
            //SaveTextureToFileUtility.SaveRenderTextureToFile(A.DBuffer.Current, Application.persistentDataPath + "/Dbuffer");
            //SaveTextureToFileUtility.SaveRenderTextureToFile(A.GBuffer[CRPTarget.COLOUR], Application.persistentDataPath + "/Colour");
        }

#if UNITY_EDITOR
        void DrawSceneView(ScriptableRenderContext context, CullingResults cullingResults, Camera camera)
        {
            // Sets active render target and clear based on camera background color.
            context.ExecuteCommandBufferSeq(cmd =>
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                cmd.ClearRenderTarget(true, true, camera.backgroundColor);
            });

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            SortingSettings sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            // ShaderTagId must match the "LightMode" tag inside the shader pass.
            // If not "LightMode" tag is found the object won't render.
            DrawingSettings drawingSettings = new DrawingSettings(ShaderPassTag.example, sortingSettings)
            {
                enableDynamicBatching = false,
                enableInstancing = true,
                perObjectData = PerObjectData.None
            };

            // Render Opaque objects given the filtering and settings computed above.
            // This functions will sort and batch objects.
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            // Renders skybox if required
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
                context.DrawSkybox(camera);

            if (UnityEditor.SceneView.lastActiveSceneView != null && UnityEditor.SceneView.lastActiveSceneView.cameraMode.drawMode == UnityEditor.DrawCameraMode.TexturedWire)
                context.DrawWireOverlay(camera);

            if (A.EditorShowSceneViewGizmos)
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);

            // Submit commands to GPU. Up to this point all commands have been enqueued in the context.
            // Several submits can be done in a frame to better controls CPU/GPU workload.
            context.Submit();
        }
#endif
    }

}

