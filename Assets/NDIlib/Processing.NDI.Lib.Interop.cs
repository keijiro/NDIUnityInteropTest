// NOTE : The following MIT license applies to this file ONLY and not to the SDK as a whole. Please review the SDK documentation
// for the description of the full license terms, which are also provided in the file "NDI License Agreement.pdf" within the SDK or
// online at http://new.tk/ndisdk_license/. Your use of any part of this SDK is acknowledgment that you agree to the SDK license
// terms. The full NDI SDK may be downloaded at http://ndi.tv/
//
//*************************************************************************************************************************************
//
// Copyright(c) 2014-2020, NewTek, inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify,
// merge, publish, distribute, sublicense, and / or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace NewTek
{
    [SuppressUnmanagedCodeSecurity]
    public static partial class NDIlib
    {
    #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        const string _dllName = "libndi.4";
    #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string _dllName = "Processing.NDI.Lib.x64";
    #else
        const string _dllName = "__Internal";
    #endif

        public static UInt32 NDILIB_CPP_DEFAULT_CONSTRUCTORS = 0;

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

