using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace NDI.Editor {

#if DONT_INCLUDE_THIS

static class PixelFormatConverterEditor
{
    [MenuItem("Assets/Create/NDI/Pixel Format Converter")]
    public static void CreatePixelFormatConverterAsset()
    {
        var asset = ScriptableObject.CreateInstance<PixelFormatConverter>();
        ProjectWindowUtil.CreateAsset(asset, "New Pixel Format Converter.asset");
    }
}

#endif

}
