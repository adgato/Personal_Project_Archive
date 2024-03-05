using UnityEngine;

static class ComputeShaderExtensions
{
    public static void SetInts
      (this ComputeShader compute, string name, (int x, int y, int z) t)
      => compute.SetInts(name, t.x, t.y, t.z);

    public static void SetInts
      (this ComputeShader compute, string name, Vector3Int v)
      => compute.SetInts(name, v.x, v.y, v.z);

    public static void DispatchThreads
      (this ComputeShader compute, int kernel, int x, int y, int z)
    {
        compute.GetKernelThreadGroupSizes(kernel, out uint xc, out uint yc, out uint zc);

        x = (x + (int)xc - 1) / (int)xc;
        y = (y + (int)yc - 1) / (int)yc;
        z = (z + (int)zc - 1) / (int)zc;

        compute.Dispatch(kernel, x, y, z);
    }

    public static void DispatchThreads
      (this ComputeShader compute, int kernel, (int x, int y, int z) t)
      => DispatchThreads(compute, kernel, t.x, t.y, t.z);

    public static void DispatchThreads
      (this ComputeShader compute, int kernel, Vector3Int v)
      => DispatchThreads(compute, kernel, v.x, v.y, v.z);

    public static int GetCounterValue(this ComputeBuffer counterBuffer)
    {
        //https://web.archive.org/web/20160408182735/https://scrawkblog.com/2014/08/14/directcompute-tutorial-for-unity-append-buffers/
        using ComputeBuffer argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

        int[] args = new int[] { 0, 1, 0, 0 };
        argBuffer.SetData(args);

        ComputeBuffer.CopyCount(counterBuffer, argBuffer, 0);
        argBuffer.GetData(args);

        return args[0];
    }
}
