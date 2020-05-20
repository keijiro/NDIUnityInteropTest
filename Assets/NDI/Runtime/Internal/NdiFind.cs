using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

[StructLayoutAttribute(LayoutKind.Sequential)]
public struct NdiSource
{
    public IntPtr _NdiName;
    public IntPtr _UrlAddress;

    public string NdiName => Marshal.PtrToStringAnsi(_NdiName);
    public string UrlAddress => Marshal.PtrToStringAnsi(_UrlAddress);
}

public class NdiFind : SafeHandleZeroOrMinusOneIsInvalid
{
    #region SafeHandle implementation

    NdiFind() : base(true) {}

    protected override bool ReleaseHandle()
    {
        _Destroy(handle);
        return true;
    }

    #endregion

    #region Public methods

    public static NdiFind Create()
      => _Create(new Settings { ShowLocalSources = true });

    unsafe public Span<NdiSource> CurrentSources { get {
        uint count;
        var array = _GetCurrentSources(this, out count);
        return new Span<NdiSource>((void*)array, (int)count);
    } }

    #endregion

    #region Unmanaged interface

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct Settings 
    {
        [MarshalAsAttribute(UnmanagedType.U1)] public bool ShowLocalSources;
        public IntPtr Groups;
        public IntPtr ExtraIPs;
    }

    [DllImport(Config.DllName, EntryPoint = "NDIlib_find_create_v2")]
    static extern NdiFind _Create(in Settings settings);

    [DllImport(Config.DllName, EntryPoint = "NDIlib_find_destroy")]
    static extern void _Destroy(IntPtr find);

    [DllImport(Config.DllName, EntryPoint = "NDIlib_find_get_current_sources")]
    static extern IntPtr _GetCurrentSources(NdiFind find, out uint count);

    #endregion
}
