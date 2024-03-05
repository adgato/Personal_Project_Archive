using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class CLightWatcher<T> : SceneWatcher<T> where T : struct, IEquatable<T>
{
    [SerializeField] private string globalDataBufferName;
    [SerializeField] [ReadOnly] private string globalDataCountName;
    private string prevName;
    private ComputeBuffer dataBuffer;

    private static readonly int sizeofT = Marshal.SizeOf(typeof(T));
    protected override void PushChanges(T[] data)
    { 
        if (data.Length == 0)
            return;
        if (dataBuffer == null || dataBuffer.stride != sizeofT || dataBuffer.count != data.Length)
        {
            ReleaseComputeBuffer();
            dataBuffer = new ComputeBuffer(data.Length, sizeofT);
            Shader.SetGlobalBuffer(globalDataBufferName, dataBuffer);
            prevName = globalDataBufferName;
        }
        else if (prevName != globalDataBufferName)
        {
            Shader.SetGlobalBuffer(globalDataBufferName, dataBuffer);
            prevName = globalDataBufferName;
        }
        dataBuffer.SetData(data);

        Shader.SetGlobalInteger(globalDataCountName, data.Length);
    }

    protected void OnValidate()
    {
        globalDataCountName = globalDataBufferName + "Count";
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        AssemblyReloadEvents.beforeAssemblyReload += ReleaseComputeBuffer;
        ReleaseComputeBuffer();
    }

    void OnDisable()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= ReleaseComputeBuffer;
        ReleaseComputeBuffer();
    }
#endif

    private void OnDestroy()
    {
        ReleaseComputeBuffer();
    }

    void ReleaseComputeBuffer()
    {
        if (dataBuffer != null)   
        {
            dataBuffer.Release();
            dataBuffer = null;
        }
    }
}
