using Unity.Collections;
using System.Collections.Generic;

namespace Klak.Ndi {

static class BufferQueue
{
    static Queue<NativeArray<byte>> _queue = new Queue<NativeArray<byte>>();
    static int _delay = 2;

    public static NativeArray<byte> Allocate(int width, int height)
    {
        var size = width * height * 4;
        var array = new NativeArray<byte>
          (size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _queue.Enqueue(array);
        return array;
    }

    public static void Dequeue()
    {
        if (_delay > 0)
        {
            _delay --;
            return;
        }

        _queue.Dequeue().Dispose();
    }
}

} // namespace Klak.Ndi
