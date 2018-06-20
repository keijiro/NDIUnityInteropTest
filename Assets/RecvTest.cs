using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using NewTek;

sealed class RecvTest : MonoBehaviour
{
    IntPtr _findInstance;
    IntPtr _recvInstance;

    Texture2D _texture;

    void Start()
    {
        var findOptions = new NDIlib.find_create_t{ show_local_sources = true };
        _findInstance = NDIlib.find_create_v2(ref findOptions);
    }

    void OnDestroy()
    {
        if (_findInstance != IntPtr.Zero)
        {
            NDIlib.find_destroy(_findInstance);
            _findInstance = IntPtr.Zero;
        }

        if (_recvInstance != IntPtr.Zero)
        {
            NDIlib.recv_destroy(_recvInstance);
            _recvInstance = IntPtr.Zero;
        }

        if (_texture != null) Destroy(_texture);
    }

    unsafe void Update()
    {
        if (_findInstance != IntPtr.Zero)
        {
            UInt32 count = 0;
            var sources = NDIlib.find_get_current_sources(_findInstance, ref count);
            if (count == 0) return;

            var source = UnsafeUtility.ReadArrayElement<NDIlib.source_t>((void*)sources, 0);
            Debug.Log("Sender found: " + Marshal.PtrToStringAnsi(source.p_ndi_name));

            var recvOptions = new NDIlib.recv_create_v3_t {
                source_to_connect_to = source,
                bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest
            };
            _recvInstance = NDIlib.recv_create_v3(ref recvOptions);

            NDIlib.find_destroy(_findInstance);
            _findInstance = IntPtr.Zero;
        }

        if (_recvInstance != IntPtr.Zero)
        {
            var frame = new NDIlib.video_frame_v2_t();
            var type = NDIlib.recv_capture_v2(_recvInstance, ref frame, IntPtr.Zero, IntPtr.Zero, 0);
            if (type != NDIlib.frame_type_e.frame_type_video) return;
            UpdateTexture(frame.xres, frame.yres, frame.p_data);
            NDIlib.recv_free_video_v2(_recvInstance, ref frame);
        }
    }

    void UpdateTexture(int width, int height, IntPtr data)
    {
        if (_texture != null && (_texture.width != width || _texture.height != height))
        {
            Destroy(_texture);
            _texture = null;
        }

        if (_texture == null) _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        _texture.LoadRawTextureData(data, width * height * 4);
        _texture.Apply();

        GetComponent<Renderer>().material.mainTexture = _texture;
    }
}
