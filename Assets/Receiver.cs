using NewTek;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using IntPtr = System.IntPtr;
using Marshal = System.Runtime.InteropServices.Marshal;

sealed class Receiver : MonoBehaviour
{
    #region Serialized properties

    [SerializeField, HideInInspector] ComputeShader _converter = null;

    #endregion

    #region Unmanaged resource operations

    NdiFind _ndiFind;
    IntPtr _ndiRecv;

    void CreateNdiFind()
      => _ndiFind = NdiFind.Create();

    unsafe void TryCreateNdiRecv()
    {
        if (_ndiFind == null || _ndiFind.IsInvalid || _ndiFind.IsClosed) return;

        // NDI source enumeration
        var sources = _ndiFind.CurrentSources;
        if (sources.IsEmpty) return;
        Debug.Log($"Sender found: {sources[0].NdiName}");

        // Recv instantiation
        var opt = new NDIlib.recv_create_v3_t {
          bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
          color_format = NDIlib.recv_color_format_e.recv_color_format_fastest,
          source_to_connect_to = new NDIlib.source_t {
                p_ndi_name = sources[0]._NdiName,
                p_url_address = sources[0]._UrlAddress
          }
        };
        _ndiRecv = NDIlib.recv_create_v3(ref opt);
    }

    void ReleaseNdiFind()
      => _ndiFind?.Dispose();

    void ReleaseNdiRecv()
    {
        if (_ndiRecv == IntPtr.Zero) return;
        NDIlib.recv_destroy(_ndiRecv);
        _ndiRecv = IntPtr.Zero;
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
        // Frame struct (unmanaged)
        var frame = new NDIlib.video_frame_v2_t();

        // Try capturing a frame.
        var type = NDIlib.recv_capture_v2
          (_ndiRecv, ref frame, IntPtr.Zero, IntPtr.Zero, 0);

        // Return if it isn't a video frame.
        if (type != NDIlib.frame_type_e.frame_type_video)
        {
            NDIlib.recv_free_video_v2(_ndiRecv, ref frame);
            return false;
        }

        // Video frame information
        _width = frame.xres;
        _height = frame.yres;
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
        _received.SetData(frame.p_data, count, 4);

        NDIlib.recv_free_video_v2(_ndiRecv, ref frame);
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
        if (_ndiRecv == IntPtr.Zero)
            TryCreateNdiRecv();
        else if (TryCaptureFrame())
            UpdateTexture();
    }

    #endregion
}
