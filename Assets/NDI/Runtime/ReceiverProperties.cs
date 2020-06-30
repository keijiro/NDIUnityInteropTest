using UnityEngine;

namespace NDI {

partial class Receiver
{
    #region NDI source settings

    [SerializeField] string _ndiName = null;

    public string ndiName
      { get => _ndiName;
        set => SetNdiName(value); }

    void SetNdiName(string name)
    {
        _ndiName = name;
        RequestReconnect();
    }

    #endregion

    #region Target settings

    [SerializeField] RenderTexture _targetTexture = null;

    public RenderTexture targetTexture
      { get => _targetTexture;
        set => _targetTexture = value; }

    [SerializeField] Renderer _targetRenderer = null;

    public Renderer targetRenderer
      { get => _targetRenderer;
        set => _targetRenderer = value; }

    [SerializeField] string _targetMaterialProperty = null;

    public string targetMaterialProperty
      { get => _targetMaterialProperty;
        set => _targetMaterialProperty = value; }

    #endregion
}

}
