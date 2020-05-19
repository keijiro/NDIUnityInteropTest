using NewTek;

public enum PixelFormat { Invalid = -1, UYVY, UYVA }

public static class PixelFormatExtension
{
    public static NDIlib.FourCC_type_e ToFourCC(this PixelFormat format)
      => format == PixelFormat.UYVY ?
        NDIlib.FourCC_type_e.FourCC_type_UYVY :
        NDIlib.FourCC_type_e.FourCC_type_UYVA;
}

public static class FourCCExtension
{
    public static PixelFormat ToPixelFormat(this NDIlib.FourCC_type_e fourCC)
      => fourCC == NDIlib.FourCC_type_e.FourCC_type_UYVY ? PixelFormat.UYVY :
         fourCC == NDIlib.FourCC_type_e.FourCC_type_UYVA ? PixelFormat.UYVA :
         PixelFormat.Invalid;
}
