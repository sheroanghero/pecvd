namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base
{
    public interface IFrameEncoder
    {
        void EncodeFrame(byte[] payload, int offset, int count, out byte[] frameBuffer, out int frameBufferOffset, out int frameBufferLength);
    }
}
