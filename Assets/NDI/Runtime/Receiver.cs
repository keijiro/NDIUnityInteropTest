using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace NDI {

public sealed class Receiver : MonoBehaviour
{
    #region Source settings

    [SerializeField] string _sourceName = null;

    #endregion

    #region Serialized properties

    [SerializeField, HideInInspector] ComputeShader _converter = null;

    #endregion

    #region Public method

    public void RequestReconnect()
      => ReleaseNdiRecv();

    #endregion

    #region Unmanaged resource operations

    NdiFind _ndiFind;
    NdiRecv _ndiRecv;

    void CreateNdiFind()
      => _ndiFind = NdiFind.Create();

    NdiSource? TryGetSource()
    {
        foreach (var source in _ndiFind.CurrentSources)
            if (source.NdiName == _sourceName) return source;
        return null;
    }

    unsafe void TryCreateNdiRecv()
    {
        if (_ndiFind == null || _ndiFind.IsInvalid || _ndiFind.IsClosed) return;

        // Source search
        var source = TryGetSource();
        if (source == null) return;

        // Recv instantiation
        var opt = new NdiRecv.Settings {
            Source = (NdiSource)source,
            ColorFormat = ColorFormat.Fastest,
            Bandwidth = Bandwidth.Highest
        };
        _ndiRecv = NdiRecv.Create(opt);
    }

    void ReleaseNdiFind()
    {
        _ndiFind?.Dispose();
        _ndiFind = null;
    }

    void ReleaseNdiRecv()
    {
        _ndiRecv?.Dispose();
        _ndiRecv = null;
    }

    #endregion

    #region Converter operations

    int _width, _height;
    PixelFormat _pixelFormat;
    ComputeBuffer _received;
    RenderTexture _converted;

    void ReleaseConverterOnDisable()
    {
        if (_received == null) return;
        _received.Dispose();
        _received = null;
    }

    void ReleaseConverterOnDestroy()
    {
        if (_converted == null) return;
        Destroy(_converted);
        _converted = null;
    }

    bool TryCaptureFrame()
    {
        // Try capturing a frame.
        var frameOrNull = _ndiRecv.TryCaptureVideoFrame();
        if (frameOrNull == null) return false;
        var frame = (VideoFrame)frameOrNull;

        // Video frame information
        _width = frame.Width;
        _height = frame.Height;
        _pixelFormat = frame.FourCC.ToPixelFormat();

        // Receive buffer preparation
        var count = Util.FrameDataCount(_width, _height, _pixelFormat);

        if (_received != null && _received.count != count)
        {
            _received.Dispose();
            _received = null;
        }

        if (_received == null)
            _received = new ComputeBuffer(count, 4);

        // Receive buffer update
        _received.SetData(frame.Data, count, 4);

        _ndiRecv.FreeVideoFrame(frame);
        return true;
    }

    void UpdateTexture()
    {
        // Conversion buffer preparation
        if (_converted != null &&
            (_converted.width != _width ||
             _converted.height != _height))
        {
            Destroy(_converted);
            _converted = null;
        }

        if (_converted == null)
        {
            _converted = new RenderTexture
              (_width, _height, 0,
               RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _converted.enableRandomWrite = true;
            _converted.Create();
        }

        // Conversion
        var pass = (int)_pixelFormat;
        _converter.SetBuffer(pass, "Source", _received);
        _converter.SetTexture(pass, "Destination", _converted);
        _converter.Dispatch(pass, _width / 16, _height / 8, 1);

        GetComponent<Renderer>().material.mainTexture = _converted;
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
      => CreateNdiFind();

    void OnDisable()
      => ReleaseConverterOnDisable();

    void OnDestroy()
    {
        ReleaseNdiFind();
        ReleaseNdiRecv();
        ReleaseConverterOnDestroy();
    }

    void Update()
    {
        if (_ndiRecv == null)
            TryCreateNdiRecv();
        else if (TryCaptureFrame())
            UpdateTexture();
    }

    #endregion
}

}
