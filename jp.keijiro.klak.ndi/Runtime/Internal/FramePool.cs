using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Debug = UnityEngine.Debug;
using IDisposable = System.IDisposable;
using IntPtr = System.IntPtr;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Klak.Ndi {

sealed class FrameEntry
{
    #region Private members

    NativeArray<byte> _image;
    IntPtr _metadata;
    int _width, _height;
    bool _alpha;

    ~FrameEntry()
    {
        if (IsValid) Debug.LogWarning("FrameEntry leak was detected.");
    }

    #endregion

    #region Public accessors

    public ref NativeArray<byte> ImageBuffer => ref _image;
    public IntPtr MetadataPointer => _metadata;
    public int Width => _width;
    public int Height => _height;
    public bool AlphaFlag => _alpha;

    public unsafe IntPtr ImagePointer
      => (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(_image);

    public int Stride => Width * 2;

    public Interop.FourCC FourCC
      => _alpha ? Interop.FourCC.UYVA : Interop.FourCC.UYVY;

    public bool IsValid => _image.IsCreated;

    #endregion

    #region Resource allocation/deallocation

    public void Open(int width, int height, bool alpha, string metadata)
    {
        // Image buffer
        var size = Util.FrameDataCount(width, height, alpha) * sizeof(uint);
        _image = new NativeArray<byte>(size, Allocator.Persistent,
                                       NativeArrayOptions.UninitializedMemory);

        // Metadata string on heap
        if (string.IsNullOrEmpty(metadata))
            _metadata = IntPtr.Zero;
        else
            _metadata = (IntPtr)Marshal.StringToHGlobalAnsi(metadata);

        // Values
        (_width, _height, _alpha) = (width, height, alpha);
    }

    public void Close()
    {
        if (_image.IsCreated)
        {
            _image.Dispose();
            _image = default(NativeArray<byte>);
        }

        if (_metadata != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_metadata);
            _metadata = IntPtr.Zero;
        }

        (_width, _height, _alpha) = (0, 0, false);
    }

    #endregion
}

sealed class FramePool : IDisposable
{
    #region Private members

    List<FrameEntry> _hot = new List<FrameEntry>();
    Stack<FrameEntry> _cold = new Stack<FrameEntry>();
    FrameEntry _marked;

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
        foreach (var e in _hot ) e.Close();
        foreach (var e in _cold) e.Close();
        _hot .Clear();
        _cold.Clear();
    }

    #endregion

    #region Pool operations

    public FrameEntry Allocate(int width, int height, bool alpha, string metadata)
    {
        var entry = _cold.Count > 0 ? _cold.Pop() : new FrameEntry();
        entry.Open(width, height, alpha, metadata);
        _hot.Add(entry);
        return entry;
    }

    public void Free(FrameEntry entry)
    {
        entry.Close();
        _hot.Remove(entry);
        _cold.Push(entry);
    }

    public unsafe FrameEntry FindByImageBuffer(NativeArray<byte> buffer)
    {
        var pimage =
          (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(buffer);

        foreach (var entry in _hot)
            if (entry.ImagePointer == pimage) return entry;

        return null;
    }

    public void Mark(FrameEntry entry)
    {
        Debug.Assert(_marked == null, "Marked twice.");
        _marked = entry;
    }

    public void FreeMarked()
    {
        if (_marked == null) return;
        Free(_marked);
        _marked = null;
    }

    #endregion
}

} // namespace Klak.Ndi
