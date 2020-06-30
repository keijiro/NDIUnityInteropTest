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

    #region Capture method implementations

    // Capture method: Game View
    (ComputeBuffer converted, int width, int height) CaptureGameView()
    {
        // Temporary RT allocation for the capture.
        var rt = RenderTexture.GetTemporary
          (Screen.width, Screen.height, 0,
           RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        // Capture and pixel format conversion
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
        var converted = Converter.Encode(rt, _enableAlpha);

        // Temporary RT deallocation
        RenderTexture.ReleaseTemporary(rt);

        return (converted, Screen.width, Screen.height);
    }

    // Capture method: Texture
    (ComputeBuffer converted, int width, int height) CaptureSourceTexture()
    {
        // Simply convert the given texture.
        var converted = Converter.Encode(_sourceTexture, _enableAlpha);

        return (converted, _sourceTexture.width, _sourceTexture.height);
    }

    (ComputeBuffer converted, int width, int height) InvokeCaptureMethod()
    {
        if (_captureMethod == CaptureMethod.GameView)
            return CaptureGameView();

        if (_captureMethod == CaptureMethod.Texture)
            return CaptureSourceTexture();

        // CaptureMethod.Camera
        return (null, 0, 0);
    }

    #endregion

    #region GPU readback completion callback

    unsafe void OnCompleteReadback
      (AsyncGPUReadbackRequest request, int width, int height)
    {
        // Ignore it if the NDI object has been already disposed.
        if (_ndiSend == null || _ndiSend.IsInvalid || _ndiSend.IsClosed)
            return;

        // Readback data retrieval
        var data = request.GetData<byte>();
        var pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);

        // Data size verification
        if (data.Length / sizeof(uint) !=
            Util.FrameDataCount(width, height, _enableAlpha)) return;

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
        // A temporary Action object for the GPU readback completion callback.
        // It's needed to avoid GC allocations in AsyncGPUReadback.Request.
        // The variables captured from the lambda function (width/height)
        // are updated in the following for-loop.
        var width = 0;
        var height = 0;
        var complete = new System.Action<AsyncGPUReadbackRequest>
          ((AsyncGPUReadbackRequest req)
             => OnCompleteReadback(req, width, height));

        for (var eof = new WaitForEndOfFrame(); true;)
        {
            // Unmanaged NDI object preparation
            // We have to do this every frame because the NDI object could be
            // disposed by renaming.
            PrepareNdiSend();

            // Wait for the end of the frame.
            yield return eof;

            // Capture and conversion
            var converted = (ComputeBuffer)null;
            (converted, width, height) = InvokeCaptureMethod();

            // GPU readback request
            if (converted != null)
                AsyncGPUReadback.Request(converted, complete);
        }
    }

    void OnDestroy()
    {
        ReleaseConverter();
        ReleaseNdiSend();
    }

    #endregion
}

}
