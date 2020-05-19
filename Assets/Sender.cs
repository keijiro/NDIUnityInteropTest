using NewTek;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

using IntPtr = System.IntPtr;
using Marshal = System.Runtime.InteropServices.Marshal;

sealed class Sender : MonoBehaviour
{
    #region Serialized properties

    [SerializeField] PixelFormat _pixelFormat = PixelFormat.UYVY;

    [SerializeField, HideInInspector] ComputeShader _converter = null;

    #endregion

    #region Unmanaged resource operations

    IntPtr _sendInstance;

    void InitSendInstance()
    {
        var name = Marshal.StringToHGlobalAnsi("Test Server");
        var sendOptions = new NDIlib.send_create_t{ p_ndi_name = name };
        _sendInstance = NDIlib.send_create(ref sendOptions);
        Marshal.FreeHGlobal(name);
    }

    void ReleaseSendInstance()
    {
        if (_sendInstance == IntPtr.Zero) return;
        NDIlib.send_destroy(_sendInstance);
        _sendInstance = IntPtr.Zero;
    }

    #endregion

    #region Converter operations

    int _width, _height;
    RenderTexture _capture;
    ComputeBuffer _converted;

    void InitConverter()
    {
        _width = Screen.width;
        _height = Screen.height;

        // Screen capture render texture
        _capture = new RenderTexture
          (_width, _height, 0,
           RenderTextureFormat.ARGB32,
           RenderTextureReadWrite.Linear);

        // Conversion buffer
        var count = Util.FrameDataCount(_width, _height, _pixelFormat);
        _converted = new ComputeBuffer(count, 4);
    }

    void ReleaseConverterOnDisable()
    {
        if (_converted == null) return;
        _converted.Dispose();
        _converted = null;
    }

    void ReleaseConverterOnDestroy()
    {
        if (_capture == null) return;
        Destroy(_capture);
        _capture = null;
    }

    void RunConversion()
    {
        var pass = (int)_pixelFormat;
        _converter.SetTexture(pass, "Source", _capture);
        _converter.SetBuffer(pass, "Destination", _converted);
        _converter.Dispatch(pass, _width / 16, _height / 8, 1);
    }

    #endregion

    #region Readback completion

    unsafe void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (_sendInstance == IntPtr.Zero) return;

        var ptr = (IntPtr)NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(request.GetData<byte>());

        var format = NDIlib.frame_format_type_e.frame_format_type_progressive;

        var frame = new NDIlib.video_frame_v2_t
          { xres = _width, yres = _height,
            FourCC = _pixelFormat.ToFourCC(), frame_format_type = format,
            p_data = ptr, line_stride_in_bytes = _width * 2 }; 

        // Send via NDI
        NDIlib.send_send_video_async_v2(_sendInstance, ref frame);
    }

    #endregion

    #region MonoBehaviour implementation

    System.Collections.IEnumerator Start()
    {
        InitSendInstance();
        InitConverter();

        for (var eof = new WaitForEndOfFrame(); true;)
        {
            // Wait for the end of the frame.
            yield return eof;

            // Request chain: Capture -> Convert -> Readback
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_capture);
            RunConversion();
            AsyncGPUReadback.Request(_converted, OnCompleteReadback);
        }
    }

    void OnDisable()
      => ReleaseConverterOnDisable();

    void OnDestroy()
    {
        ReleaseSendInstance();
        ReleaseConverterOnDestroy();
    }

    #endregion
}
