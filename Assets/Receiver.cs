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

    IntPtr _ndiFind;
    IntPtr _ndiRecv;

    void CreateNdiFind()
    {
        var opt = new NDIlib.find_create_t { show_local_sources = true };
        _ndiFind = NDIlib.find_create_v2(ref opt);
    }

    unsafe void TryCreateNdiRecv()
    {
        if (_ndiFind == IntPtr.Zero) return;

        // NDI source enumeration
        var count = (System.UInt32)0;
        var sources = NDIlib.find_get_current_sources(_ndiFind, ref count);
        if (count == 0) return;

        // First source entry
        var source =
          UnsafeUtility.ReadArrayElement<NDIlib.source_t>((void*)sources, 0);

        var name = Marshal.PtrToStringAnsi(source.p_ndi_name);
        Debug.Log($"Sender found: {name}");

        // Recv instantiation
        var opt = new NDIlib.recv_create_v3_t {
          bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
          color_format = NDIlib.recv_color_format_e.recv_color_format_fastest,
          source_to_connect_to = source
        };
        _ndiRecv = NDIlib.recv_create_v3(ref opt);
    }

    void ReleaseNdiFind()
    {
        if (_ndiFind == IntPtr.Zero) return;
        NDIlib.find_destroy(_ndiFind);
        _ndiFind = IntPtr.Zero;
    }

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
