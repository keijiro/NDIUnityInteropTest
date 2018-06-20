using System;
using System.Runtime.InteropServices;
using System.Security;

namespace NewTek
{
    [SuppressUnmanagedCodeSecurity]
    public static partial class NDIlib
    {
        // The creation structure that is used when you are creating a sender
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct send_create_t
        {
            // The name of the NDI source to create. This is a NULL terminated UTF8 string.
            public IntPtr   p_ndi_name;

            // What groups should this source be part of. NULL means default.
            public IntPtr   p_groups;

            // Do you want audio and video to "clock" themselves. When they are clocked then
            // by adding video frames, they will be rate limited to match the current frame-rate
            // that you are submitting at. The same is true for audio. In general if you are submitting
            // video and audio off a single thread then you should only clock one of them (video is
            // probably the better of the two to clock off). If you are submtiting audio and video
            // of separate threads then having both clocked can be useful.
            [MarshalAsAttribute(UnmanagedType.U1)]
            public bool clock_video,    clock_audio;
        }

        // Create a new sender instance. This will return NULL if it fails.
        [DllImport(_dllName, EntryPoint = "NDIlib_send_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr send_create(ref send_create_t p_create_settings);

        // This will destroy an existing finder instance.
        [DllImport(_dllName, EntryPoint = "NDIlib_send_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_destroy(IntPtr p_instance);

        // This will add a video frame
        [DllImport(_dllName, EntryPoint = "NDIlib_send_send_video_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_send_video_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data);

        // This will add a video frame and will return immediately, having scheduled the frame to be displayed.
        // All processing and sending of the video will occur asynchronously. The memory accessed by NDIlib_video_frame_t
        // cannot be freed or re-used by the caller until a synchronizing event has occurred. In general the API is better
        // able to take advantage of asynchronous processing than you might be able to by simple having a separate thread
        // to submit frames.
        //
        // This call is particularly beneficial when processing BGRA video since it allows any color conversion, compression
        // and network sending to all be done on separate threads from your main rendering thread.
        //
        // Synchronozing events are :
        //      - a call to NDIlib_send_send_video
        //      - a call to NDIlib_send_send_video_async with another frame to be sent
        //      - a call to NDIlib_send_send_video with p_video_data=NULL
        //      - a call to NDIlib_send_destroy
        [DllImport(_dllName, EntryPoint = "NDIlib_send_send_video_async_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_send_video_async_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data);

        [DllImport(_dllName, EntryPoint = "NDIlib_send_send_video_async_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_send_video_async_v2(IntPtr p_instance, IntPtr p_video_data);

        // This will add an audio frame
        [DllImport(_dllName, EntryPoint = "NDIlib_send_send_audio_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_send_audio_v2(IntPtr p_instance, ref audio_frame_v2_t p_audio_data);

        // This will add a metadata frame
        [DllImport(_dllName, EntryPoint = "NDIlib_send_send_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_send_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata);

        // This allows you to receive metadata from the other end of the connection
        [DllImport(_dllName, EntryPoint = "NDIlib_send_capture", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern frame_type_e send_capture(IntPtr p_instance, ref metadata_frame_t p_metadata, UInt32 timeout_in_ms);

        // Free the buffers returned by capture for metadata
        [DllImport(_dllName, EntryPoint = "NDIlib_send_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_free_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata);

        // Determine the current tally sate. If you specify a timeout then it will wait until it has changed, otherwise it will simply poll it
        // and return the current tally immediately. The return value is whether anything has actually change (true) or whether it timed out (false)
        [DllImport(_dllName, EntryPoint = "NDIlib_send_get_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAsAttribute(UnmanagedType.U1)]
        public static extern bool send_get_tally(IntPtr p_instance, ref tally_t p_tally, UInt32 timeout_in_ms);

        // Get the current number of receivers connected to this source. This can be used to avoid even rendering when nothing is connected to the video source.
        // which can significantly improve the efficiency if you want to make a lot of sources available on the network. If you specify a timeout that is not
        // 0 then it will wait until there are connections for this amount of time.
        [DllImport(_dllName, EntryPoint = "NDIlib_send_get_no_connections", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int send_get_no_connections(IntPtr p_instance, UInt32 timeout_in_ms);

        // Connection based metadata is data that is sent automatically each time a new connection is received. You queue all of these
        // up and they are sent on each connection. To reset them you need to clear them all and set them up again.
        [DllImport(_dllName, EntryPoint = "NDIlib_send_clear_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_clear_connection_metadata(IntPtr p_instance);

        // Add a connection metadata string to the list of what is sent on each new connection. If someone is already connected then
        // this string will be sent to them immediately.
        [DllImport(_dllName, EntryPoint = "NDIlib_send_add_connection_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_add_connection_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata);

        // This will assign a new fail-over source for this video source. What this means is that if this video source was to fail
        // any receivers would automatically switch over to use this source, unless this source then came back online. You can specify
        // NULL to clear the source.
        [DllImport(_dllName, EntryPoint = "NDIlib_send_set_failover", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_set_failover(IntPtr p_instance, ref source_t p_failover_source);

    } // class NDIlib

} // namespace NewTek
