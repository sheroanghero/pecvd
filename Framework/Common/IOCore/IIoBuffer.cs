using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.IOCore;

namespace MECF.Framework.RT.Core.IoProviders
{
    public enum IoType
    {
        DI,
        DO,

        AI,
        AO,
    }

    public class IoBlockItem
    {
        public int Offset { get; set; }
        public ushort Size { get; set; }
        public IoType Type { get; set; }
        public Type AIOType { get; set; }
    }

    public interface IIoBuffer
    {
        
        void SetBufferBlock(string provider, List<IoBlockItem> lstBlocks);

        void SetIoMap(string provider, int blockOffset, List<DIAccessor> ioList);
        void SetIoMap(string provider, int blockOffset, List<DOAccessor> ioList);
        void SetIoMap(string provider, int blockOffset, List<AIAccessor> ioList);
        void SetIoMap(string provider, int blockOffset, List<AOAccessor> ioList);

        void SetIoMap(string provider, Dictionary<int, string> ioMappingPathFile);
        void SetIoMapByModule(string provider, int offset, string ioMappingPathFile, string module);

        Dictionary<int, bool[]> GetDoBuffer(string source);
        Dictionary<int, bool[]> GetDiBuffer(string source);

        Dictionary<int, short[]> GetAoBuffer(string source);
        Dictionary<int, short[]> GetAiBuffer(string source);

        Dictionary<int, float[]> GetAoBufferFloat(string source);
        Dictionary<int, float[]> GetAiBufferFloat(string source);

        void SetDiBuffer(string source, int offset, bool[] buffer);
        void SetDoBuffer(string source, int offset, bool[] buffer);

        void SetAiBuffer(string source, int offset, short[] buffer, int bufferStartIndex = 0);
        void SetAoBuffer(string source, int offset, short[] buffer, int bufferStartIndex = 0);

        void SetAiBufferFloat(string source, int offset, float[] buffer, int bufferStartIndex = 0);
        void SetAoBufferFloat(string source, int offset, float[] buffer, int bufferStartIndex = 0);
    }
}
