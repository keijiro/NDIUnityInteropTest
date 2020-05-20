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

    NdiSend _ndiSend;

    void InitSendInstance()
      => _ndiSend = NdiSend.Create("Test");

    void ReleaseSendInstance()
    {
        _ndiSend?.Dispose();
        _ndiSend = null;
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
        if (_ndiSend == null) return;

        var ptr = (IntPtr)NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(request.GetData<byte>());

        var frame = new VideoFrame
          { Width = _width, Height = _height,
            FourCC = _pixelFormat.ToFourCC2(),
            FrameFormat = FrameFormat.Progressive,
            Data = ptr, LineStride = _width * 2 };

        // Send via NDI
        _ndiSend.SendVideoAsync(frame);
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
