using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using IntPtr = System.IntPtr;

namespace Klak.Ndi {

// Small utility functions
static class Util
{
    public static int FrameDataSize(int width, int height, bool alpha)
      => width * height * (alpha ? 3 : 2);

    public static bool HasAlpha(Interop.FourCC fourCC)
      => fourCC == Interop.FourCC.UYVA;
}

// Extension method to add IntPtr support to ComputeBuffer.SetData
static class ComputeBufferExtension
{
    public unsafe static void SetData
      (this ComputeBuffer buffer, IntPtr pointer, int count, int stride)
    {
        // NativeArray view for the unmanaged memory block
        var view =
          NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>
            ((void*)pointer, count * stride, Allocator.None);

        #if ENABLE_UNITY_COLLECTIONS_CHECKS
        var safety = AtomicSafetyHandle.Create();
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref view, safety);
        #endif

        buffer.SetData(view);

        #if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.Release(safety);
        #endif
    }
}

} // namespace Klak.Ndi
