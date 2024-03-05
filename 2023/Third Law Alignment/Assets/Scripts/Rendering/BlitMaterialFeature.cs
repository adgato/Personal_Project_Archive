//The sections of this script that I wrote are commented to indicate this

//    Copyright (C) 2020 Ned Makes Games

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program. If not, see <https://www.gnu.org/licenses/>.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class BlitMaterialFeature : ScriptableRendererFeature
{
    class RenderPass : ScriptableRenderPass
    {

        private string profilingName;
        private int materialPassIndex;
        private ScriptableRenderer source;
#pragma warning disable CS0618 // Type or member is obsolete
        private RenderTargetHandle[] tempDests;
#pragma warning restore CS0618 // Type or member is obsolete

        public RenderPass(string profilingName, int passIndex) : base()
        {
            this.profilingName = profilingName;
            this.materialPassIndex = passIndex;
        }

        public void SetSource(ScriptableRenderer source)
        {
            this.source = source;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            //renderPassEvent = Camera.main.transform.position.sqrMagnitude < 700 * 700 ? RenderPassEvent.BeforeRenderingTransparents : RenderPassEvent.AfterRenderingTransparents;

            if (!Application.isPlaying)
                return;

            int L = BlitMaterial.MaterialsCount();
            List<BlitMaterial> blitMaterials = BlitMaterial.GetMaterials();

            if (L == 0)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(profilingName);

            RenderTextureDescriptor cameraTextureDesc = renderingData.cameraData.cameraTargetDescriptor;
            cameraTextureDesc.depthBufferBits = 0;

            RenderTargetIdentifier[] sources = new RenderTargetIdentifier[L];
#pragma warning disable CS0618 // Type or member is obsolete
            tempDests = new RenderTargetHandle[L];
#pragma warning restore CS0618 // Type or member is obsolete


            //Loop through each material and blit the source to the temporary render target using the current material (ordered by distance to player)
            //Each material is generated either from the Atmosphere.shader, Clouds.shader or ScreenEffects.shader scripts
            for (int i = 0; i < L; i++)
            {
                if (i == 0)
#pragma warning disable CS0618 // Type or member is obsolete
                    sources[i] = source.cameraColorTarget;
#pragma warning restore CS0618 // Type or member is obsolete
                else
                    sources[i] = tempDests[i - 1].Identifier();

                tempDests[i].Init(i.ToString());
                cmd.GetTemporaryRT(tempDests[i].id, cameraTextureDesc, FilterMode.Bilinear);
                cmd.Blit(sources[i], tempDests[i].Identifier(), blitMaterials[i].Material);
            }

            //Loop through each material in reverse order and blit the temporary render target to the source.
            for (int i = 0; i < L; i++)
                cmd.Blit(tempDests[L - 1 - i].Identifier(), sources[L - 1 - i]);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (tempDests == null)
                return;
#pragma warning disable CS0618 // Type or member is obsolete
            foreach (RenderTargetHandle _tempDest in tempDests)
#pragma warning restore CS0618 // Type or member is obsolete
                cmd.ReleaseTemporaryRT(_tempDest.id);
        }

    }

    [System.Serializable]
    public class Settings
    {
        public int materialPassIndex = -1; // -1 means render all passes
        public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    [SerializeField]
    private Settings settings = new Settings();

    private RenderPass renderPass;

    public override void Create()
    {
        renderPass = new RenderPass(name, settings.materialPassIndex);
        SetRenderPassEvent(settings.renderEvent);
    }

    public void SetRenderPassEvent(RenderPassEvent renderPassEvent)
    {
        renderPass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderPass.SetSource(renderer);
        renderer.EnqueuePass(renderPass);
    }
}