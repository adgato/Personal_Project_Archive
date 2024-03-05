using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine.Rendering.CustomRenderPipeline
{
    public interface IRTHandleHolder
    {
        public void Release(RTHandleSystem RTSystem);
        public bool Allocated();
    }
}

