using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDI {

sealed class Sender : MonoBehaviour
{
    #region Serialized properties

    [SerializeField] string _name = "NDI Sender";
    [SerializeField] bool _enableAlpha = false;
    [SerializeField, HideInInspector] ComputeShader _converter = null;

    #endregion

    #region MonoBehaviour implementation

    NdiSend _ndiSend;

    System.Collections.IEnumerator Start()
    {
        _ndiSend = NdiSend.Create(_name);

        for (var eof = new WaitForEndOfFrame(); true;)
        {
            yield return eof;

            // Request chain: Capture -> Convert -> Readback
            ScreenCapture.CaptureScreenshotIntoRenderTexture(GetCaptureRT());
            RunConverter();
            AsyncGPUReadback.Request(_converted, OnCompleteReadback);
        }
    }

    void OnDisable()
      => ReleaseConverter();

    void OnDestroy()
    {
        _ndiSend?.Dispose();
        ReleaseCaptureRT();
    }

    #endregion

    #region Render texture for screen capture

    RenderTexture _captureRT;

    int Width => _captureRT.width;
    int Height => _captureRT.height;

    RenderTexture GetCaptureRT()
    {
        if (_captureRT != null) return _captureRT;

        _captureRT = new RenderTexture
          (Screen.width, Screen.height, 0,
           RenderTextureFormat.ARGB32,
           RenderTextureReadWrite.Linear);

        return _captureRT;
    }

    void ReleaseCaptureRT()
    {
        if (_captureRT == null) return;
        Destroy(_captureRT);
        _captureRT = null;
    }

    #endregion

    #region Pixel format converter

    ComputeBuffer _converted;

    void RunConverter()
    {
        var count = Util.FrameDataCount(Width, Height, _enableAlpha);

        // Buffer lazy allocation
        if (_converted == null)
            _converted = new ComputeBuffer(count, 4);

        // Compute thread dispatching
        var pass = _enableAlpha ? 1 : 0;
        _converter.SetTexture(pass, "Source", _captureRT);
        _converter.SetBuffer(pass, "Destination", _converted);
        _converter.Dispatch(pass, Width / 16, Height / 8, 1);
    }

    void ReleaseConverter()
    {
        if (_converted == null) return;
        _converted.Dispose();
        _converted = null;
    }

    #endregion

    #region Readback completion

    unsafe void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (_ndiSend == null) return;
        if (_ndiSend.IsInvalid || _ndiSend.IsClosed) return;

        // Raw pointer retrieval
        var pdata = NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(request.GetData<byte>());

        // Frame data setup
        var frame = new VideoFrame
        {
            Width = Width,
            Height = Height,
            LineStride = Width * 2,
            FourCC = _enableAlpha ? FourCC.UYVA : FourCC.UYVY,
            FrameFormat = FrameFormat.Progressive,
            Data = (System.IntPtr)pdata
        };

        // Send via NDI
        _ndiSend.SendVideoAsync(frame);
    }

    #endregion
}

}
