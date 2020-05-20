namespace NDI
{
    public enum PixelFormat { UYVY, UYVA }

    public static class PixelFormatExtension
    {
        public static FourCC ToFourCC(this PixelFormat format)
          => format == PixelFormat.UYVA ? FourCC.UYVA : FourCC.UYVY;
    }

    public static class FourCCExtension
    {
        public static PixelFormat ToPixelFormat(this FourCC fourCC)
          => fourCC == FourCC.UYVA ? PixelFormat.UYVA : PixelFormat.UYVY;
    }
}
