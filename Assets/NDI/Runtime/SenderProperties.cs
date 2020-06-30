using UnityEngine;

namespace NDI {

partial class Sender
{
    #region NDI source settings

    [SerializeField] string _ndiName = "NDI Sender";

    public string ndiName
      { get => _ndiName;
        set => SetNdiName(value); }

    void SetNdiName(string name)
    {
        _ndiName = name;
        RequestReset();
    }

    [SerializeField] bool _enableAlpha = false;

    public bool enableAlpha
      { get => _enableAlpha;
        set => _enableAlpha = value; }

    #endregion

    #region Capture target settings

    [SerializeField] CaptureMethod _captureMethod = CaptureMethod.GameView;

    public CaptureMethod captureMethod
      { get => _captureMethod;
        set => _captureMethod = value; }

    [SerializeField] Camera _sourceCamera = null;

    public Camera sourceCamera
      { get => _sourceCamera;
        set => _sourceCamera = value; }

    [SerializeField] Texture _sourceTexture = null;

    public Texture sourceTexture
      { get => _sourceTexture;
        set => _sourceTexture = value; }

    #endregion
}

}
