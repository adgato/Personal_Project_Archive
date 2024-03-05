using System.Collections;
using System.Collections.Generic;
namespace UnityEngine.Rendering.CustomRenderPipeline
{
    /// <summary>
    /// The goal of this class is to be able to seamlessly set the same double buffer as the target and destination of a blit.
    /// </summary>
    public class DoubleBuffer : IEnumerator<RTHandle>, IRTHandleHolder
    {
        private RTHandle evenBuffer;
        private RTHandle oddBuffer;
        private bool parity = true;

        public RTHandle Current => parity ? evenBuffer : oddBuffer;
        object IEnumerator.Current => Current;

        bool IEnumerator.MoveNext()
        {
            parity ^= true;
            return true;
        }
        public RTHandle MoveNext()
        {
            parity ^= true;
            return Current;
        }

        public DoubleBuffer(RTHandle evenBuffer, RTHandle oddBuffer)
        {
            parity = true;
            this.evenBuffer = evenBuffer;
            this.oddBuffer = oddBuffer;
        }
        public bool Allocated() => evenBuffer != null && oddBuffer != null;
        public void Reset() => parity = true;

        /// <summary>
        /// Should not use unless accessing the <see cref="RTHandleSystem"/> is impossible. Method exists as part of an <see cref="IEnumerator"/> implementation.
        /// </summary>
        public void Dispose()
        {
            evenBuffer.Release();
            oddBuffer.Release();
            CoreUtils.Destroy(evenBuffer);
            CoreUtils.Destroy(oddBuffer);
            evenBuffer = null;
            oddBuffer = null;
        }
        public void Release(RTHandleSystem RTSystem)
        {
            RTSystem.Release(evenBuffer);
            RTSystem.Release(oddBuffer);
            evenBuffer = null;
            oddBuffer = null;
        }

        public IEnumerator<(RTHandle Source, RenderTargetIdentifier Dest)> CreateBlitChain(RTHandle source, RenderTargetIdentifier dest, int numBlits)
        {
            switch (numBlits) 
            {
                case 0: 
                    yield break;

                case 1: 
                    yield return (source, dest);
                    break;

                default:
                    yield return (source, MoveNext());
                    for (int i = 0; i < numBlits - 2; i++)
                        yield return (Current, MoveNext());
                    yield return (Current, dest);
                    break;
            }
        }

        /// <summary>
        /// Safely perform an <see cref="LBlitter"/> Blit for each of the materials listed, from <paramref name="source"/> to <paramref name="dest"/>.
        /// </summary>
        public void BlitThrough(CommandBuffer cmd, RTHandle source, RenderTargetIdentifier dest, Material[] materials)
        {
            switch (materials.Length)
            {
                case 0:
                    LBlitter.Blit(cmd, source, dest, LBlitter.BlitCopyMaterial);
                    return;

                case 1:
                    LBlitter.Blit(cmd, source, dest, materials[0]);
                    return;

                default:
                    LBlitter.Blit(cmd, source, MoveNext(), materials[0]);
                    for (int i = 1; i < materials.Length - 1; i++)
                        LBlitter.Blit(cmd, Current, MoveNext(), materials[i]);
                    LBlitter.Blit(cmd, Current, dest, materials[^1]);
                    break;
            }
        }
        /// <summary>
        /// For debugging blits.
        /// </summary>
        public void CopyThrough(CommandBuffer cmd, RTHandle source, RenderTargetIdentifier dest, int numBlits) =>
            BlitThrough(cmd, source, dest, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Repeat(LBlitter.BlitCopyMaterial, numBlits)));
    }                                  
}                                      

