using System;
using System.Collections.Generic;
using System.ServiceModel;
using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.OperationCenter
{
    [ServiceContract]
    [ServiceKnownType(typeof(ModuleName))]
    [ServiceKnownType(typeof(MoveType))]
    [ServiceKnownType(typeof(MoveOption))]
    [ServiceKnownType(typeof(Hand))]
    [ServiceKnownType(typeof(WaferStatus))]
    [ServiceKnownType(typeof(TransferInfo[]))]
    [ServiceKnownType(typeof(TransferInfo))]
	[ServiceKnownType(typeof(TowerLightStatus))]
	[ServiceKnownType(typeof(short[]))]
	[ServiceKnownType(typeof(bool[]))]
    [ServiceKnownType(typeof(string[]))]
    [ServiceKnownType(typeof(Dictionary<string, object>))]
    [ServiceKnownType(typeof(Tuple<string, int>))]
    [ServiceKnownType(typeof(List<Tuple<string, int>>))]
    [ServiceKnownType(typeof(ManualTransferTask))]
    [ServiceKnownType(typeof(ManualTransferTask[]))]
    [ServiceKnownType(typeof(Dictionary<string, Dictionary<string, EventItem>>))]
    public interface IInvokeService
    {
        [OperationContract]
        void DoOperation(string operationName, params object[] args);
    }
}
