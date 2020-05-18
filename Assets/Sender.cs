using NewTek;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

using IntPtr = System.IntPtr;
using Marshal = System.Runtime.InteropServices.Marshal;

sealed class Sender : MonoBehaviour
{
    IntPtr _sendInstance;
    RenderTexture _readbackRT;

    System.Collections.IEnumerator Start()
    {
        var name = Marshal.StringToHGlobalAnsi("Test Server");
        var sendOptions = new NDIlib.send_create_t{ p_ndi_name = name };
        _sendInstance = NDIlib.send_create(ref sendOptions);
        Marshal.FreeHGlobal(name);

        _readbackRT = new RenderTexture(Screen.width, Screen.height, 0);

        while (true)
        {
            yield return new WaitForEndOfFrame();

            ScreenCapture.CaptureScreenshotIntoRenderTexture(_readbackRT);
            AsyncGPUReadback.Request
              (_readbackRT, 0, TextureFormat.RGBA32, OnCompleteReadback);
        }
    }

    void OnDestroy()
    {
        if (_sendInstance != IntPtr.Zero)
        {
            NDIlib.send_destroy(_sendInstance);
            _sendInstance = IntPtr.Zero;
        }

        if (_readbackRT != null)
        {
            Destroy(_readbackRT);
            _readbackRT = null;
        }
    }

    unsafe void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (_sendInstance == IntPtr.Zero) return;

        var ptr = (IntPtr)NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(request.GetData<byte>());

        var frame = new NDIlib.video_frame_v2_t
        {
            xres = _readbackRT.width,
            yres = _readbackRT.height,
            FourCC = NDIlib.FourCC_type_e.FourCC_type_RGBX,
            frame_format_type =
              NDIlib.frame_format_type_e.frame_format_type_progressive,
            p_data = ptr,
            line_stride_in_bytes = _readbackRT.width * 4
        };

        NDIlib.send_send_video_async_v2(_sendInstance, ref frame);
    }
}
