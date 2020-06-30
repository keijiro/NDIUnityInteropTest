using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDI {

sealed partial class Sender : MonoBehaviour
{
    #region Internal method (for editor use)

    internal void RequestReset()
      => ReleaseNdiSend();

    #endregion

    #region Unmanaged NDI object

    NdiSend _ndiSend;

    void PrepareNdiSend()
    {
        if (_ndiSend != null) return;
        _ndiSend = NdiSend.Create(_ndiName);
    }

    void ReleaseNdiSend()
    {
        _ndiSend?.Dispose();
        _ndiSend = null;
    }

    #endregion

    #region Pixel format converter object

    [SerializeField] PixelFormatConverter _defaultConverter = null;

    PixelFormatConverter _converterInstance;

    PixelFormatConverter Converter
      => _converterInstance ??
         (_converterInstance = Instantiate(_defaultConverter));

    void ReleaseConverter()
    {
        if (_converterInstance == null) return;
        Destroy(_converterInstance);
        _converterInstance = null;
    }

    #endregion

    #region Render texture for screen capture

    RenderTexture _captureRT;

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

    #region Readback completion

    unsafe void OnCompleteReadback
      (AsyncGPUReadbackRequest request, int width, int height)
    {
        if (_ndiSend == null || _ndiSend.IsInvalid || _ndiSend.IsClosed)
            return;

        // Raw pointer retrieval
        var pdata = NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(request.GetData<byte>());

        // Frame data setup
        var frame = new VideoFrame
        {
            Width = width,
            Height = height,
            LineStride = width * 2,
            FourCC = _enableAlpha ? FourCC.UYVA : FourCC.UYVY,
            FrameFormat = FrameFormat.Progressive,
            Data = (System.IntPtr)pdata
        };

        // Send via NDI
        _ndiSend.SendVideoAsync(frame);
    }

    #endregion

    #region MonoBehaviour implementation

    System.Collections.IEnumerator Start()
    {
        // Readback completion callback
        var dims = (x:0, y:0);
        var complete = new System.Action<AsyncGPUReadbackRequest>
          ((AsyncGPUReadbackRequest req)
             => OnCompleteReadback(req, dims.x, dims.y));

        for (var eof = new WaitForEndOfFrame(); true;)
        {
            PrepareNdiSend();

            yield return eof;

            if (_captureMethod == CaptureMethod.GameView)
            {
                // Request chain: Capture -> Convert -> Readback
                ScreenCapture.CaptureScreenshotIntoRenderTexture(GetCaptureRT());
                var converted = Converter.Encode(_captureRT, _enableAlpha);
                dims = (_captureRT.width, _captureRT.height);
                AsyncGPUReadback.Request(converted, complete);
            }
            else if (_captureMethod == CaptureMethod.Texture)
            {
                // Request chain: Convert -> Readback
                var converted = Converter.Encode(_sourceTexture, _enableAlpha);
                dims = (_sourceTexture.width, _sourceTexture.height);
                AsyncGPUReadback.Request(converted, complete);
            }
        }
    }

    void OnDestroy()
    {
        ReleaseConverter();
        ReleaseNdiSend();
        ReleaseCaptureRT();
    }

    #endregion
}

}
