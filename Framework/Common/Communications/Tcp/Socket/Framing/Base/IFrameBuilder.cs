namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base
{
    public interface IFrameBuilder
    {
        IFrameEncoder Encoder { get; }
        IFrameDecoder Decoder { get; }
    }
}
