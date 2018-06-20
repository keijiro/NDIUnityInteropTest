using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using NewTek;

sealed class FindTest : MonoBehaviour
{
    IntPtr _findInstance;
    int _lastCount;

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
    }

    unsafe void Update()
    {
        UInt32 count = 0;
        var sources = NDIlib.find_get_current_sources(_findInstance, ref count);

        for (; _lastCount < count; _lastCount++)
        {
            var source = UnsafeUtility.ReadArrayElement<NDIlib.source_t>((void*)sources, _lastCount);
            Debug.Log(Marshal.PtrToStringAnsi(source.p_ndi_name));
        }
    }
}
