using System.Collections.Generic;
using System.Xml;
using Aitex.Core.RT.IOCore;

namespace MECF.Framework.RT.Core.IoProviders
{
    public enum IoProviderStateEnum
    {
        Uninitialized,

        Opening,

        Opened,

        Error,

        Closing,

        Closed,
    }

    public interface IIoProvider
    {
        string Name { get; set; }
        string Module { get; set; }

        bool IsOpened { get; }

        void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, Dictionary<int, string> ioMappingPathFile);
        void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, string ioMappingPathFile, string ioModule);

        void Start();
        void Stop();
        void Reset();

        bool SetValue(AOAccessor aoItem, short value);
        bool SetValueFloat(AOAccessor aoItem, float value);

        bool SetValue(DOAccessor doItem, bool value);
 
    }
 

}
