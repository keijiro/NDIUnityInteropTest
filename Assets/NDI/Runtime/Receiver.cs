using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace NDI {

public sealed class Receiver : MonoBehaviour
{
    #region NDI source settings

    [SerializeField] string _sourceName = null;

    public string sourceName
      { get => _sourceName;
        set => SetSourceName(value); }

    void SetSourceName(string name)
    {
        _sourceName = name;
        RequestReconnect();
    }

    #endregion

    #region Target settings

    [SerializeField] RenderTexture _targetTexture = null;

    public RenderTexture targetTexture
      { get => _targetTexture;
        set => _targetTexture = value; }

    [SerializeField] Renderer _targetRenderer = null;

    public Renderer targetRenderer
      { get => _targetRenderer;
        set => _targetRenderer = value; }

    [SerializeField] string _targetMaterialProperty = null;

    public string targetMaterialProperty
      { get => _targetMaterialProperty;
        set => _targetMaterialProperty = value; }

    #endregion

    #region Internal method (for editor use)

    internal void RequestReconnect()
      => ReleaseNdiRecv();

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

    NdiRecv _ndiRecv;

    NdiSource? TryGetSource()
    {
        foreach (var source in SharedInstance.Find.CurrentSources)
            if (source.NdiName == _sourceName) return source;
        return null;
    }

    unsafe void TryCreateNdiRecv()
    {
        // Source search
        var source = TryGetSource();
        if (source == null) return;

        // Recv instantiation
        var opt = new NdiRecv.Settings
          { Source = (NdiSource)source,
            ColorFormat = ColorFormat.Fastest,
            Bandwidth = Bandwidth.Highest };
        _ndiRecv = NdiRecv.Create(opt);
    }

    RenderTexture TryReceiveFrame()
    {
        var frameOrNull = _ndiRecv.TryCaptureVideoFrame();
        if (frameOrNull == null) return null;

        var frame = (VideoFrame)frameOrNull;

        var rt = Converter.Decode
          (frame.Width, frame.Height,
           Util.CheckAlpha(frame.FourCC), frame.Data);

        _ndiRecv.FreeVideoFrame(frame);

        return rt;
    }

    void ReleaseNdiRecv()
    {
        _ndiRecv?.Dispose();
        _ndiRecv = null;
    }

    #endregion

    #region Output methods

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
        ReleaseNdiRecv();
    }

    void Update()
    {
        if (_ndiRecv == null)
        {
            TryCreateNdiRecv();
            // We don't expect that we can get the first frame right now,
            // so return even if we successfully created the NdiRecv instance.
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
