using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public class NdiSend : SafeHandleZeroOrMinusOneIsInvalid
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct Settings 
    {
        public IntPtr NdiName;
        public IntPtr Groups;

        [MarshalAsAttribute(UnmanagedType.U1)]
        public bool ClockVideo;

        [MarshalAsAttribute(UnmanagedType.U1)]
        public bool ClockAudio;
    }

    NdiSend() : base(true) {}

    protected override bool ReleaseHandle()
    {
        _Destroy(handle);
        return true;
    }

    public static NdiSend Create(string name)
    {
        var cname = Marshal.StringToHGlobalAnsi("Test Server");
        var settings = new Settings { NdiName = cname };
        var ptr = _Create(ref settings);
        Marshal.FreeHGlobal(cname);
        return ptr;
    }

    public void SendVideoAsync(in VideoFrame data)
      => _SendVideoAsync(this, data);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    const string _dllName = "libndi.4";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    const string _dllName = "Processing.NDI.Lib.x64";
#else
    const string _dllName = "__Internal";
#endif

    [DllImport(_dllName, EntryPoint = "NDIlib_send_create")]
    static extern NdiSend _Create(ref Settings settings);

    [DllImport(_dllName, EntryPoint = "NDIlib_send_destroy")]
    static extern void _Destroy(IntPtr instance);

    [DllImport(_dllName, EntryPoint = "NDIlib_send_send_video_async_v2")]
    static extern void _SendVideoAsync(NdiSend instance, in VideoFrame data);
}
