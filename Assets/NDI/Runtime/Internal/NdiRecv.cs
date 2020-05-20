using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NDI {

public enum Bandwidth
{
    MetadataOnly = -10,
    AudioOnly = 10,
    Lowest = 0,
    Highest = 100
}

public enum ColorFormat
{
    BGRX_BGRA = 0,
    UYVY_BGRA = 1,
    RGBX_RGBA = 2,
    UYVY_RGBA = 3,
    BGRX_BGRA_Flipped = 200,
    Fastest = 100
}

public class NdiRecv : SafeHandleZeroOrMinusOneIsInvalid
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct Settings 
    {
        public NdiSource Source;
        public ColorFormat ColorFormat;
        public Bandwidth Bandwidth;
        [MarshalAsAttribute(UnmanagedType.U1)]
        public bool AllowVideoFields;
        public IntPtr Name;
    }

    #region SafeHandle implementation

    NdiRecv() : base(true) {}

    protected override bool ReleaseHandle()
    {
        _Destroy(handle);
        return true;
    }

    #endregion

    #region Public methods

    public static NdiRecv Create(in Settings settings)
      => _Create(settings);

    public VideoFrame? TryCaptureVideoFrame()
    {
        VideoFrame video;
        var type = _Capture(this, out video, IntPtr.Zero, IntPtr.Zero, 0);
        return type == FrameType.Video ? (VideoFrame?)video : null;
    }

    public void FreeVideoFrame(in VideoFrame frame)
      => _FreeVideo(this, frame);

    #endregion

    #region Unmanaged interface

    [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_create_v3")]
    static extern NdiRecv _Create(in Settings Settings);

    [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_destroy")]
    static extern void _Destroy(IntPtr recv);

    [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_capture_v2")]
    static extern FrameType _Capture(NdiRecv recv,
      out VideoFrame video, IntPtr audio, IntPtr metadata, uint timeout);

    [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_free_video_v2")]
    static extern void _FreeVideo(NdiRecv recv, in VideoFrame data);

    #endregion
}

}
