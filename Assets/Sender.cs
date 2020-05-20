using NewTek;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

sealed class Sender : MonoBehaviour
{
    #region Serialized properties

    [SerializeField] PixelFormat _pixelFormat = PixelFormat.UYVY;
    [SerializeField, HideInInspector] ComputeShader _converter = null;

    #endregion

    #region Private members

    NdiSend _ndiSend;

    // Screen capture render texture
    RenderTexture _capture;

    // Conversion buffer
    ComputeBuffer _converted;

    int Width => _capture.width;
    int Height => _capture.height;

    #endregion

    #region Converter operations

    void InitConverter()
    {
        // Screen capture render texture allocation
        _capture = new RenderTexture
          (Screen.width, Screen.height, 0,
           RenderTextureFormat.ARGB32,
           RenderTextureReadWrite.Linear);

        // Conversion buffer allocation
        var count = Util.FrameDataCount(Width, Height, _pixelFormat);
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

    void RunConverter()
    {
        var pass = (int)_pixelFormat;
        _converter.SetTexture(pass, "Source", _capture);
        _converter.SetBuffer(pass, "Destination", _converted);
        _converter.Dispatch(pass, Width / 16, Height / 8, 1);
    }

    #endregion

    #region Readback completion

    unsafe void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (_ndiSend == null || _ndiSend.IsInvalid || _ndiSend.IsClosed) return;

        var frame = new VideoFrame
          { Width = Width, Height = Height,
            LineStride = Width * 2,
            FourCC = _pixelFormat.ToFourCC2(),
            FrameFormat = FrameFormat.Progressive };

        frame.Data = (System.IntPtr)NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(request.GetData<byte>());

        _ndiSend.SendVideoAsync(frame);
    }

    #endregion

    #region MonoBehaviour implementation

    System.Collections.IEnumerator Start()
    {
        _ndiSend = NdiSend.Create("Test");

        InitConverter();

        for (var eof = new WaitForEndOfFrame(); true;)
        {
            // Wait for the end of the frame.
            yield return eof;

            // Request chain: Capture -> Convert -> Readback
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_capture);
            RunConverter();
            AsyncGPUReadback.Request(_converted, OnCompleteReadback);
        }
    }

    void OnDisable()
      => ReleaseConverterOnDisable();

    void OnDestroy()
    {
        _ndiSend?.Dispose();
        ReleaseConverterOnDestroy();
    }

    #endregion
}
