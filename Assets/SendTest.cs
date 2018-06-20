using System;
using System.Runtime.InteropServices;
using UnityEngine;
using NewTek;

sealed class SendTest : MonoBehaviour
{
    IntPtr _sendInstance;
    UInt32[] _buffer = new UInt32[64 * 64];

    void Start()
    {
        var name = Marshal.StringToHGlobalAnsi("Test Server");
        var sendOptions = new NDIlib.send_create_t{ p_ndi_name = name };
        _sendInstance = NDIlib.send_create(ref sendOptions);
        Marshal.FreeHGlobal(name);
    }

    void OnDestroy()
    {
        if (_sendInstance != IntPtr.Zero)
        {
            NDIlib.send_destroy(_sendInstance);
            _sendInstance = IntPtr.Zero;
        }
    }

    void Update()
    {
        var offs = Time.frameCount;
        for (var i = 0; i < _buffer.Length; i++)
            _buffer[i] = (UInt32)((offs + i) * 0x010203);

        var frame = new NDIlib.video_frame_v2_t {
            xres = 64, yres = 64,
            FourCC = NDIlib.FourCC_type_e.FourCC_type_RGBX,
            frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
            p_data = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0),
            line_stride_in_bytes = 64 * 4
        };

        NDIlib.send_send_video_async_v2(_sendInstance, ref frame);
    }
}
