using NewTek;

public enum PixelFormat { Invalid = -1, UYVY, UYVA }

public static class PixelFormatExtension
{
    public static FourCC ToFourCC(this PixelFormat format)
      => format == PixelFormat.UYVY ? FourCC.UYVY : FourCC.UYVA;
}

public static class FourCCExtension
{
    public static PixelFormat ToPixelFormat(this FourCC fourCC)
      => fourCC == FourCC.UYVY ? PixelFormat.UYVY :
         fourCC == FourCC.UYVA ? PixelFormat.UYVA :
         PixelFormat.Invalid;
}
