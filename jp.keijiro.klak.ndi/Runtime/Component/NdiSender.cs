using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Klak.Ndi {

[ExecuteInEditMode]
public sealed partial class NdiSender : MonoBehaviour
{
    #region Internal objects

    int _width, _height;
    Interop.Send _send;
    FormatConverter _converter;
    FramePool _framePool;
    System.Action<AsyncGPUReadbackRequest> _onReadback;

    void PrepareInternalObjects()
    {
        if (_send == null)
            _send = _captureMethod == CaptureMethod.GameView ?
              SharedInstance.GameViewSend : Interop.Send.Create(_ndiName);
        if (_converter == null) _converter = new FormatConverter(_resources);
        if (_onReadback == null) _onReadback = OnReadback;
        if (_framePool == null) _framePool = new FramePool();
    }

    void ReleaseInternalObjects()
    {
        // Total synchronization: This may cause a frame hiccup, but it's
        // needed to dispose the readback buffer objects safely.
        AsyncGPUReadback.WaitAllRequests();

        if (_send != null && !SharedInstance.IsGameViewSend(_send))
            _send.Dispose();
        _send = null;

        _converter?.Dispose();
        _converter = null;

        _framePool?.Dispose();
        _framePool = null;
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
            if (_sourceTexture == null) return null;

            _width = _sourceTexture.width;
            _height = _sourceTexture.height;

            return _converter.Encode(_sourceTexture, _enableAlpha, true);
        }

        // Game View capture method
        // Capture the screen into a temporary RT, then convert it.
        if (_captureMethod == CaptureMethod.GameView)
        {
            _width = Screen.width;
            _height = Screen.height;

            var tempRT = RenderTexture.GetTemporary(_width, _height, 0);

            ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);
            var converted = _converter.Encode(tempRT, _enableAlpha, false);

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

            var converted = CaptureImmediate();
            if (converted == null) continue;

            // GPU readback request
            var entry = _framePool.Allocate
              (_width, _height, enableAlpha, metadata);
            AsyncGPUReadback.RequestIntoNativeArray
              (ref entry.ImageBuffer, converted, _onReadback);
        }
    }

    #endregion

    #region SRP camera capture callback

    void OnCameraCapture(RenderTargetIdentifier source, CommandBuffer cb)
    {
        // NOTE: In some corner cases, this callback is called after object
        // destruction. To avoid these cases, we check the _attachedCamera
        // value and return if it's null. See ResetState() for details.
        if (_attachedCamera == null) return;

        PrepareInternalObjects();

        _width = _sourceCamera.pixelWidth;
        _height = _sourceCamera.pixelHeight;

        // Pixel format conversion
        var converted = _converter.Encode
          (cb, source, _width, _height, _enableAlpha, true);

        // GPU readback request
        var entry = _framePool.Allocate
          (_width, _height, enableAlpha, metadata);
        cb.RequestAsyncReadbackIntoNativeArray
          (ref entry.ImageBuffer, converted, _onReadback);
    }

    #endregion

    #region GPU readback completion callback

    unsafe void OnReadback(AsyncGPUReadbackRequest req)
    {
        // Retrieve the frame entry using the readback buffer as a key.
        var entry = _framePool.FindByImageBuffer(req.GetData<byte>());
        if (!entry.IsValid) return;

        // Readback error or inactive sender: Free the frame entry and break.
        if (req.hasError || _send == null || _send.IsInvalid || _send.IsClosed)
        {
            _framePool.Free(entry);
            return;
        }

        // Frame data
        var frame = new Interop.VideoFrame
          { Width       = entry.Width,
            Height      = entry.Height,
            LineStride  = entry.Stride,
            FourCC      = entry.FourCC,
            FrameFormat = Interop.FrameFormat.Progressive,
            Data        = entry.ImagePointer,
            Metadata    = entry.MetadataPointer };

        // Start sending this frame.
        // This causes a synchronization for the last frame -- i.e., It locks
        // the thread if the last frame is still under processing.
        _send.SendVideoAsync(frame);

        // We don't need the last frame anymore; Let it freed.
        _framePool.FreeMarked();

        // Mark this frame to be freed up in the next frame.
        _framePool.Mark(entry);
    }

    #endregion

    #region Component state controller

    Camera _attachedCamera;

    // Reset the component state without disposing the NDI send object.
    internal void ResetState(bool willBeActive)
    {
        // Disable the subcomponents.
        StopAllCoroutines();

        //
        // Remove the capture callback from the camera.
        //
        // NOTE: We're not able to remove the capture callback correcly when
        // the camera has been destroyed because we end up with getting a null
        // reference from _attachedCamera. To avoid causing issues in the
        // callback, we make sure that _attachedCamera has a null reference.
        //
        if (_attachedCamera != null)
        {
        #if KLAK_NDI_HAS_SRP
            CameraCaptureBridge.RemoveCaptureAction
              (_attachedCamera, OnCameraCapture);
        #endif
        }

        _attachedCamera = null;

        // The following blocks are to activate the subcomponents.
        // We can return here if willBeActive is false.
        if (!willBeActive) return;

        if (_captureMethod == CaptureMethod.Camera)
        {
            // Enable the camera capture callback.
            if (_sourceCamera != null)
            {
                _attachedCamera = _sourceCamera;
            #if KLAK_NDI_HAS_SRP
                CameraCaptureBridge.AddCaptureAction
                  (_attachedCamera, OnCameraCapture);
            #endif
            }
        }
        else
        {
            // Enable the immediate capture coroutine.
            StartCoroutine(ImmediateCaptureCoroutine());
        }
    }

    // Reset the component state and dispose the NDI send object.
    internal void Restart(bool willBeActivate)
    {
        ResetState(willBeActivate);
        ReleaseInternalObjects();
    }

    internal void ResetState() => ResetState(isActiveAndEnabled);
    internal void Restart() => Restart(isActiveAndEnabled);

    #endregion

    #region MonoBehaviour implementation

    void OnEnable() => ResetState();

    void OnDisable() => Restart(false);

    void OnDestroy() => Restart(false);

    #endregion
}

} // namespace Klak.Ndi
