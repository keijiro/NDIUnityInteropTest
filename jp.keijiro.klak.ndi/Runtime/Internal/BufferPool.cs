using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

using IDisposable = System.IDisposable;
using IntPtr = System.IntPtr;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Klak.Ndi {

public readonly struct FrameEntry : IDisposable
{
    readonly NativeArray<byte> _image;
    readonly IntPtr _metadata;
    readonly int _width, _height;
    readonly bool _alpha;

    public NativeArray<byte> ImageBuffer
      => _image;

    public unsafe IntPtr ImagePointer
      => (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(_image);

    public IntPtr Metadata => _metadata;

    public int Width => _width;
    public int Height => _height;
    public bool AlphaFlag => _alpha;

    public bool IsValid => _image.IsCreated;

    public FrameEntry(int width, int height, bool alpha, string metadata)
    {
        var size =
          Util.FrameDataCount(width, height, alpha) * sizeof(uint);

        _image = new NativeArray<byte>
          (size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        if (string.IsNullOrEmpty(metadata))
            _metadata = IntPtr.Zero;
        else
            _metadata = (IntPtr)Marshal.StringToHGlobalAnsi(metadata);

        (_width, _height, _alpha) = (width, height, alpha);
    }

    public void Dispose()
    {
        if (_image.IsCreated) _image.Dispose();
        if (_metadata != IntPtr.Zero) Marshal.FreeHGlobal(_metadata);
    }
}

public class FrameQueue : IDisposable
{
    List<FrameEntry> _entries;

    public FrameQueue()
    {
        _entries = new List<FrameEntry>();
    }

    public void Dispose()
    {
        if (_entries != null)
            foreach (var e in _entries) e.Dispose();
        _entries = null;
    }

    public FrameEntry Allocate(int width, int height, bool alpha, string metadata)
    {
        var entry = new FrameEntry(width, height, alpha, metadata);
        _entries.Add(entry);
        return entry;
    }

    public unsafe FrameEntry Retrieve(NativeArray<byte> image)
    {
        var pimage = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(image);
        for (var i = 0; i < _entries.Count; i++)
        {
            if (pimage == _entries[i].ImagePointer)
            {
                var e = _entries[i];
                _entries.RemoveAt(i);
                return e;
            }
        }
        return default(FrameEntry);
    }
}

} // namespace Klak.Ndi
