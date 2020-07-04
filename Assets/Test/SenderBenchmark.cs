using UnityEngine;
using NDI;

class SenderBenchmark : MonoBehaviour
{
    [SerializeField] NdiResources _ndiResources = null;

    void Start()
    {
        var components = new [] { typeof(Camera), typeof(NdiSender) };
        var rt = new RenderTexture(256, 256, 32);

        for (var i = 0; i < 16; i++)
        {
            var x = i % 4;
            var y = i / 4;

            var go = new GameObject($"Sender {i}", components);

            go.transform.parent = transform;
            go.transform.localPosition =
              new Vector3((x + 0.5f) / 4 - 0.5f, (y + 0.5f) / 4 - 0.5f, -10);

            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 0.5f / 4;
            camera.targetTexture = rt;

            var sender = go.GetComponent<NdiSender>();
            sender.SetResources(_ndiResources);
            sender.ndiName = go.name;
            sender.enableAlpha = true;
            sender.captureMethod = CaptureMethod.Camera;
            sender.sourceCamera = camera;
        }
    }
}
