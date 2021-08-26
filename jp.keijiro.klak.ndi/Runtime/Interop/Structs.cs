using System;
using System.Runtime.InteropServices;

namespace Klak.Ndi.Interop {

public enum FrameType
{
    None = 0,
    Video = 1,
    Audio = 2,
    Metadata = 3,
    Error = 4,
    StatusChange = 100
}

public enum FourCC
{
    UYVY = 0x59565955,
    YV12 = 0x32315659,
    NV12 = 0x3231564E,
    I420 = 0x30323449,
    BGRA = 0x41524742,
    BGRX = 0x58524742,
    RGBA = 0x41424752,
    RGBX = 0x58424752,
    UYVA = 0x41565955
}

public enum FrameFormat
{
    Interleaved,
    Progressive,
    Field0,
    Field1
}

[StructLayout(LayoutKind.Sequential)]
public struct Source
{
    IntPtr _NdiName;
    IntPtr _UrlAddress;

#if KLAK_NDI_NET_STANDARD_2_1
    public string NdiName => Marshal.PtrToStringUTF8(_NdiName);
    public string UrlAddress => Marshal.PtrToStringUTF8(_UrlAddress);
#else
    public string NdiName => Marshal.PtrToStringAnsi(_NdiName);
    public string UrlAddress => Marshal.PtrToStringAnsi(_UrlAddress);
#endif
}

[StructLayout(LayoutKind.Sequential)]
public struct VideoFrame
{
    public int Width, Height;
    public FourCC FourCC;
    public int FrameRateN, FrameRateD;
    public float AspectRatio;
    public FrameFormat FrameFormat;
    public long Timecode;
    public IntPtr Data;
    public int LineStride;
    public IntPtr Metadata;
    public long Timestamp;
}

[StructLayout(LayoutKind.Sequential)]
public struct Tally
{
    [MarshalAs(UnmanagedType.U1)] public bool OnProgram;
    [MarshalAs(UnmanagedType.U1)] public bool OnPreview;
}

} // namespace Klak.Ndi.Interop
