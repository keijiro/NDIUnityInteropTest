using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDI {

sealed class Sender : MonoBehaviour
{
    #region NDI source settings

    [SerializeField] string _sourceName = "NDI Sender";

    public string sourceName
      { get => _sourceName;
        set => SetSourceName(value); }

    void SetSourceName(string name)
    {
        _sourceName = name;
        RequestReset();
    }

    [SerializeField] bool _enableAlpha = false;

    public bool enableAlpha
      { get => _enableAlpha;
        set => _enableAlpha = value; }

    #endregion

    #region Internal method (for editor use)

    internal void RequestReset()
      => ReleaseNdiSend();

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

    #region Unmanaged NDI object

    NdiSend _ndiSend;

    void LazyInitNdiSend()
    {
        if (_ndiSend != null) return;
        _ndiSend = NdiSend.Create(_sourceName);
    }

    void ReleaseNdiSend()
    {
        _ndiSend?.Dispose();
        _ndiSend = null;
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

    #region MonoBehaviour implementation

    System.Collections.IEnumerator Start()
    {
        for (var eof = new WaitForEndOfFrame(); true;)
        {
            LazyInitNdiSend();

            yield return eof;

            // Request chain: Capture -> Convert -> Readback
            ScreenCapture.CaptureScreenshotIntoRenderTexture(GetCaptureRT());
            var converted = Converter.Encode(_captureRT, _enableAlpha);
            AsyncGPUReadback.Request(converted, OnCompleteReadback);
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
