using UnityEngine;
using NDI;

class ReceiverBenchmark : MonoBehaviour
{
    [SerializeField] string _ndiNamePrefix = "Computer Name";
    [SerializeField] Mesh _mesh = null;
    [SerializeField] Material _material = null;
    [SerializeField] NdiResources _ndiResources = null;

    void Start()
    {
        var components = new []
          { typeof(MeshFilter), typeof(MeshRenderer), typeof(NdiReceiver) };

        for (var i = 0; i < 16; i++)
        {
            var x = i % 4;
            var y = i / 4;

            var go = new GameObject($"Receiver {i}", components);

            go.transform.parent = transform;
            go.transform.localPosition =
              new Vector3((x + 0.5f) / 4 - 0.5f, (y + 0.5f) / 4 - 0.5f, 0);
            go.transform.localScale = Vector3.one / 4;

            var mf = go.GetComponent<MeshFilter>();
            mf.sharedMesh = _mesh;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _material;

            var receiver = go.GetComponent<NdiReceiver>();
            receiver.SetResources(_ndiResources);
            receiver.ndiName = $"{_ndiNamePrefix} (Sender {i})";
            receiver.targetRenderer = mr;
            receiver.targetMaterialProperty = "_BaseMap";
        }
    }
}
