using NewTek;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

using IntPtr = System.IntPtr;
using Marshal = System.Runtime.InteropServices.Marshal;

sealed class Sender : MonoBehaviour
{
    [SerializeField, HideInInspector] ComputeShader _encoder = null;

    IntPtr _sendInstance;

    RenderTexture _texture;
    ComputeBuffer _buffer;

    System.Collections.IEnumerator Start()
    {
        var name = Marshal.StringToHGlobalAnsi("Test Server");
        var sendOptions = new NDIlib.send_create_t{ p_ndi_name = name };
        _sendInstance = NDIlib.send_create(ref sendOptions);
        Marshal.FreeHGlobal(name);

        _texture = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        _buffer = new ComputeBuffer(Screen.width / 2 * Screen.height, 4);

        while (true)
        {
            yield return new WaitForEndOfFrame();

            ScreenCapture.CaptureScreenshotIntoRenderTexture(_texture);

            _encoder.SetTexture(0, "Source", _texture);
            _encoder.SetBuffer(0, "Destination", _buffer);
            _encoder.Dispatch(0, _texture.width / 2 / 8, _texture.height / 8, 1);

            AsyncGPUReadback.Request(_buffer, OnCompleteReadback);
        }
    }

    void OnDisable()
    {
        if (_buffer != null)
        {
            _buffer.Dispose();
            _buffer = null;
        }
    }

    void OnDestroy()
    {
        if (_sendInstance != IntPtr.Zero)
        {
            NDIlib.send_destroy(_sendInstance);
            _sendInstance = IntPtr.Zero;
        }

        if (_texture != null)
        {
            Destroy(_texture);
            _texture = null;
        }
    }

    unsafe void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (_sendInstance == IntPtr.Zero) return;

        var ptr = (IntPtr)NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(request.GetData<byte>());

        var frame = new NDIlib.video_frame_v2_t
        {
            xres = _texture.width,
            yres = _texture.height,
            FourCC = NDIlib.FourCC_type_e.FourCC_type_UYVY,
            frame_format_type =
              NDIlib.frame_format_type_e.frame_format_type_progressive,
            p_data = ptr,
            line_stride_in_bytes = _texture.width * 2
        };

        NDIlib.send_send_video_async_v2(_sendInstance, ref frame);
    }
}
