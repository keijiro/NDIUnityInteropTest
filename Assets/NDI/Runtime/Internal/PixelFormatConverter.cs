using UnityEngine;
using UnityEngine.Rendering;
using IntPtr = System.IntPtr;

namespace NDI {

sealed class PixelFormatConverter : ScriptableObject
{
    #region Common members

    [SerializeField] ComputeShader _encoderCompute = null;
    [SerializeField] ComputeShader _decoderCompute = null;

    ComputeBuffer _encoderOutput;

    ComputeBuffer _decoderInput;
    ComputeDataSetter _decoderSetter;
    RenderTexture _decoderOutput;

    void OnDisable()
    {
        _encoderOutput?.Dispose();
        _encoderOutput = null;

        _decoderInput?.Dispose();
        _decoderInput = null;
    }

    void OnDestroy()
    {
        if (_decoderOutput != null)
        {
            Destroy(_decoderOutput);
            _decoderOutput = null;
        }
    }

    #endregion

    #region Encoder implementation

    // Immediate mode version
    public ComputeBuffer Encode(Texture source, bool enableAlpha)
    {
        var width = source.width;
        var height = source.height;
        var dataCount = Util.FrameDataCount(width, height, enableAlpha);

        // Reallocate the output buffer when the output size was changed.
        if (_encoderOutput != null && _encoderOutput.count != dataCount)
        {
            _encoderOutput.Dispose();
            _encoderOutput = null;
        }

        // Output buffer allocation
        if (_encoderOutput == null)
            _encoderOutput = new ComputeBuffer(dataCount, 4);

        // Compute thread dispatching
        var pass = enableAlpha ? 1 : 0;
        _encoderCompute.SetTexture(pass, "Source", source);
        _encoderCompute.SetBuffer(pass, "Destination", _encoderOutput);
        _encoderCompute.Dispatch(pass, width / 16, height / 8, 1);

        return _encoderOutput;
    }

    // Command buffer version
    public ComputeBuffer Encode
      (CommandBuffer cb, RenderTargetIdentifier source,
       int width, int height, bool enableAlpha)
    {
        var dataCount = Util.FrameDataCount(width, height, enableAlpha);

        // Reallocate the output buffer when the output size was changed.
        if (_encoderOutput != null && _encoderOutput.count != dataCount)
        {
            _encoderOutput.Dispose();
            _encoderOutput = null;
        }

        // Output buffer allocation
        if (_encoderOutput == null)
            _encoderOutput = new ComputeBuffer(dataCount, 4);

        // Compute thread dispatching
        var pass = enableAlpha ? 1 : 0;
        cb.SetComputeTextureParam(_encoderCompute, pass, "Source", source);
        cb.SetComputeBufferParam
          (_encoderCompute, pass, "Destination", _encoderOutput);
        cb.DispatchCompute(_encoderCompute, pass, width / 16, height / 8, 1);

        return _encoderOutput;
    }

    #endregion

    #region Decoder implementation

    public RenderTexture
      Decode(int width, int height, bool enableAlpha, IntPtr data)
    {
        var dataCount = Util.FrameDataCount(width, height, enableAlpha);

        // Reallocate the input buffer when the input size was changed.
        if (_decoderInput != null && _decoderInput.count != dataCount)
        {
            _decoderInput.Dispose();
            _decoderInput = null;
        }

        // Reallocate the output buffer when the output size was changed.
        if (_decoderOutput != null &&
            (_decoderOutput.width != width ||
             _decoderOutput.height != height))
        {
            Destroy(_decoderOutput);
            _decoderOutput = null;
        }

        // Input buffer allocation
        if (_decoderInput == null)
        {
            _decoderInput = new ComputeBuffer(dataCount, 4);
            _decoderSetter = new ComputeDataSetter(_decoderInput);
        }

        // Output buffer allocation
        if (_decoderOutput == null)
        {
            _decoderOutput = new RenderTexture
              (width, height, 0,
               RenderTextureFormat.ARGB32,
               RenderTextureReadWrite.sRGB);
            _decoderOutput.enableRandomWrite = true;
            _decoderOutput.Create();
        }

        // Input buffer update
        _decoderSetter.SetData(data, dataCount, 4);

        // Decoder compute dispatching
        var pass = enableAlpha ? 1 : 0;
        _decoderCompute.SetBuffer(pass, "Source", _decoderInput);
        _decoderCompute.SetTexture(pass, "Destination", _decoderOutput);
        _decoderCompute.Dispatch(pass, width / 16, height / 8, 1);

        return _decoderOutput;
    }

    #endregion
}

}
