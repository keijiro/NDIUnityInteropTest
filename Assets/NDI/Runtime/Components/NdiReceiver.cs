using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace NDI {

public sealed partial class NdiReceiver : MonoBehaviour
{
    #region Internal method (for editor use)

    internal void RequestReconnect()
      => ReleaseRecv();

    #endregion

    #region Unmanaged NDI object

    Interop.Recv _recv;

    Interop.Source? TryGetSource()
    {
        foreach (var source in SharedInstance.Find.CurrentSources)
            if (source.NdiName == _ndiName) return source;
        return null;
    }

    unsafe void TryCreateRecv()
    {
        // Source search
        var source = TryGetSource();
        if (source == null) return;

        // Recv instantiation
        var opt = new Interop.Recv.Settings
          { Source = (Interop.Source)source,
            ColorFormat = Interop.ColorFormat.Fastest,
            Bandwidth = Interop.Bandwidth.Highest };
        _recv = Interop.Recv.Create(opt);
    }

    RenderTexture TryReceiveFrame()
    {
        var frameOrNull = _recv.TryCaptureVideoFrame();
        if (frameOrNull == null) return null;

        var frame = (Interop.VideoFrame)frameOrNull;

        var rt = Converter.Decode
          (frame.Width, frame.Height,
           Util.CheckAlpha(frame.FourCC), frame.Data);

        _recv.FreeVideoFrame(frame);

        return rt;
    }

    void ReleaseRecv()
    {
        _recv?.Dispose();
        _recv = null;
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

    #region Output method implementations

    MaterialPropertyBlock _propertyBlock;

    void UpdateRendererOverride(RenderTexture rt)
    {
        if (_targetRenderer == null || rt == null) return;

        // Material property block lazy initialization
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        // Read-modify-write
        _targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetTexture(_targetMaterialProperty, rt);
        _targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    void BlitToTargetTexture(RenderTexture rt)
    {
        if (_targetTexture == null | rt == null) return;
        Graphics.Blit(rt, _targetTexture);
    }

    #endregion

    #region MonoBehaviour implementation

    void OnDestroy()
    {
        ReleaseConverter();
        ReleaseRecv();
    }

    void Update()
    {
        if (_recv == null)
        {
            TryCreateRecv();
            // We don't expect that we can get the first frame right now,
            // so return even if we successfully created the recv instance.
            return;
        }

        var rt = TryReceiveFrame();
        if (rt != null)
        {
            UpdateRendererOverride(rt);
            BlitToTargetTexture(rt);
        }
    }

    #endregion
}

}
