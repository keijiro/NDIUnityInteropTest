using System;
using System.Runtime.InteropServices;
using System.Security;

namespace NewTek
{
    [SuppressUnmanagedCodeSecurity]
    public static partial class NDIlib
    {

    #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        const string _dllName = "libndi.3";
    #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string _dllName = "Processing.NDI.Lib.x64";
    #else
        const string _dllName = "__Internal";
    #endif

        // This is not actually required, but will start and end the libraries which might get
        // you slightly better performance in some cases. In general it is more "correct" to
        // call these although it is not required. There is no way to call these that would have
        // an adverse impact on anything (even calling destroy before you've deleted all your
        // objects). This will return false if the CPU is not sufficiently capable to run NDILib
        // currently NDILib requires SSE4.2 instructions (see documentation). You can verify
        // a specific CPU against the library with a call to NDIlib_is_supported_CPU()
        [DllImport(_dllName, EntryPoint = "NDIlib_initialize", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAsAttribute(UnmanagedType.U1)]
        public static extern bool initialize();

        [DllImport(_dllName, EntryPoint = "NDIlib_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void destroy();

        [DllImport(_dllName, EntryPoint = "NDIlib_version", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr version();

        // Recover whether the current CPU in the system is capable of running NDILib.
        [DllImport(_dllName, EntryPoint = "NDIlib_is_supported_CPU", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAsAttribute(UnmanagedType.U1)]
        public static extern bool is_supported_CPU();

    } // class NDIlib

} // namespace NewTek

