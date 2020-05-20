using System.Reflection;
using UnityEngine;
using IntPtr = System.IntPtr;

namespace NDI
{
    static class Util
    {
        public static int FrameDataCount
          (int width, int height, PixelFormat format)
          => width * height * (format == PixelFormat.UYVA ? 3 : 2) / 4;
    }

    //
    // Directly load an unmanaged data array to a compute buffer via an
    // Intptr. This is not a public interface so will be broken one day.
    // DO NOT TRY AT HOME.
    //
    static class ComputeBufferUnmanagedExtension
    {
        static MethodInfo _method;

        static MethodInfo Method
          => _method ?? (_method = GetMethod());

        static MethodInfo GetMethod()
          => typeof(ComputeBuffer).GetMethod("InternalSetNativeData",
                                             BindingFlags.InvokeMethod |
                                             BindingFlags.NonPublic |
                                             BindingFlags.Instance);

        static object [] _args5 = new object[5];

        public static void SetData
          (this ComputeBuffer buffer, IntPtr pointer, int count, int stride)
        {
            _args5[0] = pointer;
            _args5[1] = 0;      // source offset
            _args5[2] = 0;      // buffer offset
            _args5[3] = count;
            _args5[4] = stride;

            Method.Invoke(buffer, _args5);
        }
    }
}
