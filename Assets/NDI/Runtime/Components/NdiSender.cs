using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDI {

public sealed partial class NdiSender : MonoBehaviour
{
    #region Internal method (for editor use)

    internal void RequestReset()
      => ReleaseSend();

    #endregion

    #region Unmanaged NDI object

    Interop.Send _send;

    void PrepareSend()
    {
        if (_send != null) return;
        _send = Interop.Send.Create(_ndiName);
    }

    void ReleaseSend()
    {
        _send?.Dispose();
        _send = null;
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

    #region SRP camera capture callback

    void OnCameraCapture(RenderTargetIdentifier source, CommandBuffer cb)
    {
        var tempRT = Shader.PropertyToID("_TemporaryRT");
        var width = _sourceCamera.pixelWidth;
        var height = _sourceCamera.pixelHeight;

        // Temporary RT allocation
        cb.GetTemporaryRT
          (tempRT, width, height, 0,
           FilterMode.Bilinear, RenderTextureFormat.ARGB32,
           RenderTextureReadWrite.Linear, 1, false);

        // Blit to the temporary RT
        cb.Blit(source, tempRT);

        // Pixel format conversion
        var converted =
          Converter.Encode(cb, tempRT, width, height, _enableAlpha);

        cb.ReleaseTemporaryRT(tempRT);

        // GPU readback request
        cb.RequestAsyncReadback
          (converted, (AsyncGPUReadbackRequest req)
             => OnCompleteReadback(req, width, height));
    }

    #endregion

    #region GPU readback completion callback

    unsafe void OnCompleteReadback
      (AsyncGPUReadbackRequest request, int width, int height)
    {
        // Ignore it if the NDI object has been already disposed.
        if (_send == null || _send.IsInvalid || _send.IsClosed) return;

        // Readback data retrieval
        var data = request.GetData<byte>();
        var pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);

        // Data size verification
        if (data.Length / sizeof(uint) !=
            Util.FrameDataCount(width, height, _enableAlpha)) return;

        // Frame data setup
        var frame = new Interop.VideoFrame
          { Width = width,
            Height = height,
            LineStride = width * 2,
            FourCC = _enableAlpha ? Interop.FourCC.UYVA : Interop.FourCC.UYVY,
            FrameFormat = Interop.FrameFormat.Progressive,
            Data = (System.IntPtr)pdata };

        // Send via NDI
        _send.SendVideoAsync(frame);
    }

    #endregion

    #region MonoBehaviour implementation

    #if NDI_HAS_SRP

    void OnEnable()
    {
        if (_captureMethod == CaptureMethod.Camera)
            CameraCaptureBridge.AddCaptureAction
              (_sourceCamera, OnCameraCapture);
    }

    void OnDisable()
    {
        if (_captureMethod == CaptureMethod.Camera)
            CameraCaptureBridge.RemoveCaptureAction
              (_sourceCamera, OnCameraCapture);
    }

    #endif

    void OnDestroy()
    {
        ReleaseConverter();
        ReleaseSend();
    }

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
            PrepareSend();

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

    #endregion
}

}
