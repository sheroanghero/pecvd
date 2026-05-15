namespace MECF.Framework.Common.Communications.Tcp.Socket.Framing.Base
{
    public interface IFrameDecoder
    {
        bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frameLength, out byte[] payload, out int payloadOffset, out int payloadCount);
    }
}
