using Unity.Collections;
using System.Collections.Generic;

namespace Klak.Ndi {

sealed class BufferPool : System.IDisposable
{
    Queue<NativeArray<byte>> _queue = new Queue<NativeArray<byte>>();

    public BufferPool()
      => _queue.Enqueue(new NativeArray<byte>(8, Allocator.Persistent));

    public NativeArray<byte> Allocate(int width, int height)
    {
        var size = width * height * 4;
        var array = new NativeArray<byte>
          (size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _queue.Enqueue(array);
        return array;
    }

    public void DisposeOldest()
      => _queue.Dequeue().Dispose();

    public void Dispose()
    {
        if (_queue != null)
        {
            foreach (var buffer in _queue) buffer.Dispose();
            _queue = null;
        }
    }
}

} // namespace Klak.Ndi
