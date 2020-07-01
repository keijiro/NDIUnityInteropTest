using UnityEngine;

namespace NDI
{
    public sealed class NdiResources : ScriptableObject
    {
        public ComputeShader encoderCompute;
        public ComputeShader decoderCompute;
    }
}
