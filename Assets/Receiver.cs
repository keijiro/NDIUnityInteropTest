using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using NewTek;

sealed class Receiver : MonoBehaviour
{
    [SerializeField, HideInInspector] ComputeShader _decoder;

    IntPtr _findInstance;
    IntPtr _recvInstance;

    ComputeBuffer _buffer;
    RenderTexture _texture;

    void Start()
    {
        var findOptions = new NDIlib.find_create_t{ show_local_sources = true };
        _findInstance = NDIlib.find_create_v2(ref findOptions);
    }

    void OnDisable()
    {
        if (_buffer != null)
        {
            _buffer.Dispose();
            _buffer = null;
        }
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
                color_format = NDIlib.recv_color_format_e.recv_color_format_fastest,
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

    unsafe void UpdateTexture(int width, int height, IntPtr data)
    {
        var dataLength = width / 2 * height;

        if (_buffer != null && _buffer.count != dataLength)
        {
            _buffer.Dispose();
            _buffer = null;
        }

        if (_buffer == null)
            _buffer = new ComputeBuffer(dataLength, 4);

        if (_texture != null && (_texture.width != width || _texture.height != height))
        {
            Destroy(_texture);
            _texture = null;
        }

        if (_texture == null)
        {
            _texture = new RenderTexture(
                width, height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB
            );
            _texture.enableRandomWrite = true;
            _texture.Create();
        }

        var temp = new NativeArray<uint>(dataLength, Allocator.Temp);
        UnsafeUtility.MemCpy(temp.GetUnsafePtr(), (void*)data, dataLength * 4);
        _buffer.SetData(temp);
        temp.Dispose();

        _decoder.SetBuffer(0, "Source", _buffer);
        _decoder.SetTexture(0, "Destination", _texture);
        _decoder.Dispatch(0, width / 2 / 8, height / 8, 1);

        GetComponent<Renderer>().material.mainTexture = _texture;
    }
}
