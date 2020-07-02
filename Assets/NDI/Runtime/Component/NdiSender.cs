using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDI {

[ExecuteInEditMode]
public sealed partial class NdiSender : MonoBehaviour
{
    #region Editor interface

    internal void RequestReset() => ReleaseInternalObjects();

    #endregion

    #region Internal objects

    int _width, _height;
    Interop.Send _send;
    FormatConverter _converter;
    System.Action<AsyncGPUReadbackRequest> _onReadback;

    void PrepareInternalObjects()
    {
        if (_send == null) _send = Interop.Send.Create(_ndiName);
        if (_converter == null) _converter = new FormatConverter(_resources);
        if (_onReadback == null) _onReadback = OnReadback;
    }

    void ReleaseInternalObjects()
    {
        _send?.Dispose();
        _send = null;

        _converter?.Dispose();
        _converter = null;
    }

    #endregion

    #region Immediate capture methods

    ComputeBuffer CaptureImmediate()
    {
        PrepareInternalObjects();

        // Texture capture method
        // Simply convert the source texture and return it.
        if (_captureMethod == CaptureMethod.Texture)
        {
            _width = _sourceTexture.width;
            _height = _sourceTexture.height;
            return _converter.Encode(_sourceTexture, _enableAlpha);
        }

        // Game View capture method
        // Capture the screen into a temporary RT, then convert it.
        if (_captureMethod == CaptureMethod.GameView)
        {
            _width = Screen.width;
            _height = Screen.height;

            var tempRT = RenderTexture.GetTemporary(_width, _height, 0);

            ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);
            var converted = _converter.Encode(tempRT, _enableAlpha);

            RenderTexture.ReleaseTemporary(tempRT);
            return converted;
        }

        Debug.LogError("Wrong capture method.");
        return null;
    }

    // Capture coroutine: At the end of every frames, it captures the source
    // frame, convert it to the NDI frame format, then request GPU readback.
    System.Collections.IEnumerator ImmediateCaptureCoroutine()
    {
        for (var eof = new WaitForEndOfFrame(); true;)
        {
            yield return eof;
            if (!enabled) yield break;
            if (_captureMethod != CaptureMethod.Camera)
                AsyncGPUReadback.Request(CaptureImmediate(), _onReadback);
        }
    }

    #endregion

    #region SRP camera capture callback

    void OnCameraCapture(RenderTargetIdentifier source, CommandBuffer cb)
    {
        PrepareInternalObjects();

        _width = _sourceCamera.pixelWidth;
        _height = _sourceCamera.pixelHeight;

        // Temporary RT allocation
        var tempRT = Shader.PropertyToID("_TemporaryRT");
        cb.GetTemporaryRT(tempRT, _width, _height, 0);

        // Blit to the temporary RT
        cb.Blit(source, tempRT, new Vector2(1, -1), new Vector2(0, 1));

        // Pixel format conversion
        var converted = _converter.Encode
          (cb, tempRT, _width, _height, _enableAlpha);

        cb.ReleaseTemporaryRT(tempRT);

        // GPU readback request
        cb.RequestAsyncReadback(converted, _onReadback);
    }

    #endregion

    #region GPU readback completion callback

    unsafe void OnReadback(AsyncGPUReadbackRequest request)
    {
        // Ignore it if the NDI object has been already disposed.
        if (_send == null || _send.IsInvalid || _send.IsClosed) return;

        // Readback data retrieval
        var data = request.GetData<byte>();
        var pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);

        // Data size verification
        if (data.Length / sizeof(uint) !=
            Util.FrameDataCount(_width, _height, _enableAlpha))
            return;

        // Frame data setup
        var frame = new Interop.VideoFrame
          { Width = _width,
            Height = _height,
            LineStride = _width * 2,
            FourCC = _enableAlpha ? Interop.FourCC.UYVA : Interop.FourCC.UYVY,
            FrameFormat = Interop.FrameFormat.Progressive,
            Data = (System.IntPtr)pdata };

        // Send via NDI
        _send.SendVideoAsync(frame);
    }

    #endregion

    #region MonoBehaviour implementation

    void OnEnable()
    {
        StartCoroutine(ImmediateCaptureCoroutine());

    #if NDI_HAS_SRP
        if (_captureMethod == CaptureMethod.Camera)
            CameraCaptureBridge.AddCaptureAction
              (_sourceCamera, OnCameraCapture);
    #endif
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ReleaseInternalObjects();

    #if NDI_HAS_SRP
        if (_captureMethod == CaptureMethod.Camera)
            CameraCaptureBridge.RemoveCaptureAction
              (_sourceCamera, OnCameraCapture);
    #endif
    }

    void OnDestroy() => ReleaseInternalObjects();

    #endregion
}

}
