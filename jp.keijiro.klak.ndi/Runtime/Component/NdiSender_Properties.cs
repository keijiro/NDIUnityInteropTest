using UnityEngine;

namespace Klak.Ndi {

public enum CaptureMethod { GameView, Camera, Texture }

public sealed partial class NdiSender : MonoBehaviour
{
    #region NDI source settings

    [SerializeField] string _ndiName = "NDI Sender";

    public string ndiName
      { get => _ndiName;
        set => ChangeNdiName(value); }

    void ChangeNdiName(string name)
    {
        if (_ndiName == name) return;
        _ndiName = name;
        Restart();
    }

    [SerializeField] bool _keepAlpha = false;

    public bool keepAlpha
      { get => _keepAlpha;
        set => _keepAlpha = value; }

    #endregion

    #region Capture target settings

    [SerializeField] CaptureMethod _captureMethod = CaptureMethod.GameView;

    public CaptureMethod captureMethod
      { get => _captureMethod;
        set => ChangeCaptureMethod(value); }

    void ChangeCaptureMethod(CaptureMethod method)
    {
        if (_captureMethod == method) return;
        _captureMethod = method;
        Restart();
    }

    [SerializeField] Camera _sourceCamera = null;

    public Camera sourceCamera
      { get => _sourceCamera;
        set => ChangeSourceCamera(value); }

    void ChangeSourceCamera(Camera camera)
    {
        if (_sourceCamera == camera) return;
        _sourceCamera = camera;
        ResetState();
    }

    [SerializeField] Texture _sourceTexture = null;

    public Texture sourceTexture
      { get => _sourceTexture;
        set => _sourceTexture = value; }

    #endregion

    #region Runtime property

    public string metadata { get; set; }

    public Interop.Send internalSendObject => _send;

    #endregion

    #region Resources asset reference

    [SerializeField, HideInInspector] NdiResources _resources = null;

    public void SetResources(NdiResources resources)
      => _resources = resources;

    #endregion
}

} // namespace Klak.Ndi
