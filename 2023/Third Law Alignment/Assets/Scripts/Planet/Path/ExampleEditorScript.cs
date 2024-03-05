using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class ExampleEditorScript : MonoBehaviour
{
    public ComputeShader simplexNoise;
    void Start()
    {
        //texture size in memory = [size^3 * colour_channels * colour_resolution] bits in memory
        //so for GraphicsFormat.R32_SFloat (colour_channels = 1, colour_resolution = 32)
        //size=32: 0.125MB, size=64: 1MB, size=128: 8MB, size=256: 64MB, size=512: 0.5GB

        int size = 64;
        GraphicsFormat graphicsFormat = GraphicsFormat.R32_SFloat;

        RenderTexture resultTexture = new RenderTexture(size, size, 0, graphicsFormat);
        resultTexture.dimension = TextureDimension.Tex3D;
        resultTexture.volumeDepth = size;
        resultTexture.enableRandomWrite = true;
        resultTexture.Create();

        simplexNoise.SetInt("Dims", size);
        simplexNoise.SetFloat("Offset", 2.89f);
        simplexNoise.SetFloat("Scale", 5);
        simplexNoise.SetTexture(0, "Result", resultTexture);
        simplexNoise.DispatchThreads(0, size, size, size);

        SaveRT3DToTexture3DAsset(resultTexture, "snoiseTexture3D");
    }

    void SaveRT3DToTexture3DAsset(RenderTexture rt3D, string pathWithoutAssetsAndExtension)
    {
        int width = rt3D.width, height = rt3D.height, depth = rt3D.volumeDepth;

        var a = new NativeArray<float>(width * height * depth, Allocator.Persistent, NativeArrayOptions.UninitializedMemory); //change if format is not 8 bits (i was using R8_UNorm) (create a struct with 4 bytes etc)
        AsyncGPUReadback.RequestIntoNativeArray(ref a, rt3D, 0, (_) =>
        {
            Texture3D output = new Texture3D(width, height, depth, rt3D.graphicsFormat, TextureCreationFlags.None);
            output.SetPixelData(a, 0);
            output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(output, $"Assets/{pathWithoutAssetsAndExtension}.asset");
            AssetDatabase.SaveAssetIfDirty(output);
#endif
            a.Dispose();
            rt3D.Release();
        });
    }
}
