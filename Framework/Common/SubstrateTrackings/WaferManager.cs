using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using FabConnect.SecsGemInterface.Common;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.Common.SubstrateTrackings
{
    public class WaferManager : Singleton<WaferManager>
    {
        Dictionary<string, WaferInfo> _dict = new Dictionary<string, WaferInfo>();

        object _lockerWaferList = new object();

        Dictionary<ModuleName, Dictionary<int, WaferInfo>> _locationWafers = new Dictionary<ModuleName, Dictionary<int, WaferInfo>>();
        public Dictionary<ModuleName, Dictionary<int, WaferInfo>> AllLocationWafers => _locationWafers;
        Dictionary<ModuleName, Dictionary<int, WaferInfo>> _backupLocationWafers = new Dictionary<ModuleName, Dictionary<int, WaferInfo>>();

        private const string EventWaferLeft = "WAFER_LEFT_POSITION";
        private const string EventWaferArrive = "WAFER_ARRIVE_POSITION";
        private const string Event_STS_AtSourcs = "STS_ATSOURCE";
        private const string Event_STS_AtWork = "STS_ATWORK";
        private const string Event_STS_AtDestination = "STS_ATDESTINATION";
        private const string Event_STS_Deleted = "STS_DELETED";

        private const string Event_STS_NeedProcessing = "STS_NEEDPROCESSING";
        private const string Event_STS_InProcessing = "STS_INPROCESSING";
        private const string Event_STS_Processed = "STS_PROCESSED";
        private const string Event_STS_Aborted = "STS_ABORTED";
        private const string Event_STS_Stopped = "STS_STOPPED";
        private const string Event_STS_Rejected = "STS_REJECTED";
        private const string Event_STS_Lost = "STS_LOST";
        private const string Event_STS_Skipped = "STS_SKIPPED";

        //private const string Event_STS_Unoccupied = "STS_UNOCCUPIED";
        //private const string Event_STS_Occupied = "STS_OCCUPIED";
        private const string Event_STS_Nostate = "STS_NOSTATE";



        public const string LotID = "LotID";
        public const string SubstDestination = "SubstDestination";
        public const string SubstHistory = "SubstHistory";
        public const string SubstID = "SubstID";
        public const string SubstLocID = "SubstLocID";
        public const string SubstLocState = "SubstLocState";
        public const string SubstProcState = "SubstProcState";
        public const string PrvSubstProcState = "PrvSubstProcState";
        public const string SubstSource = "SubstSource";
        public const string SubstState = "SubstState";
        public const string PrvSubstState = "PrvSubstState";
        public const string SubstType = "SubstType";
        public const string SubstUsage = "SubstUsage";
        public const string SubstMtrlStatus = "SubstMtrlStatus";
        public const string SubstSourceSlot = "SourceSlot";

        public const string SubstSourceCarrierID = "SourceCarrier";
        public const string SubstCurrentSlot = "Slot";


        private PeriodicJob _thread;
        private bool _needSerialize;

        public WaferManager()
        {
        }

        public bool Serialize()
        {
            if (!_needSerialize) return true;
            _needSerialize = false;
            try
            {
                if (_locationWafers != null)
                {
                    BinarySerializer<Dictionary<ModuleName, Dictionary<int, WaferInfo>>>.ToStream(_locationWafers, "WaferManager");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return true;
        }

        public void Deserialize()
        {
            try
            {
                var ccc = BinarySerializer<Dictionary<ModuleName, Dictionary<int, WaferInfo>>>.FromStream("WaferManager");
                if (ccc != null)
                {
                    //foreach (var moduleWafers in ccc)
                    //{
                    //    if (ModuleHelper.IsLoadPort(moduleWafers.Key))
                    //    {
                    //        foreach (var waferInfo in moduleWafers.Value)
                    //        {
                    //            waferInfo.Value.SetEmpty();
                    //        }
                    //    }
                    //}
                    _locationWafers = ccc;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        public void Initialize()
        {
            EV.Subscribe(new EventItem("Event", EventWaferLeft, "Wafer Left"));
            EV.Subscribe(new EventItem("Event", EventWaferArrive, "Wafer Arrived"));

            EV.Subscribe(new EventItem("Event", Event_STS_AtSourcs, "Substrate transport state is AT_SOURCE."));
            EV.Subscribe(new EventItem("Event", Event_STS_AtWork, "Substrate transport state is AT_WORK."));
            EV.Subscribe(new EventItem("Event", Event_STS_AtDestination, "Substrate transport state is AT_DESTINATION."));
            EV.Subscribe(new EventItem("Event", Event_STS_Deleted, "Substrate transport state is DELETED."));

            EV.Subscribe(new EventItem("Event", Event_STS_NeedProcessing, "Substrate process state is NEEDPROCESSING."));
            EV.Subscribe(new EventItem("Event", Event_STS_InProcessing, "Substrate process state is INPROCESSING."));
            EV.Subscribe(new EventItem("Event", Event_STS_Processed, "Substrate process state is PROCESSED."));
            EV.Subscribe(new EventItem("Event", Event_STS_Aborted, "Substrate process state is ABORTED."));
            EV.Subscribe(new EventItem("Event", Event_STS_Stopped, "Substrate process state is STOPPED."));
            EV.Subscribe(new EventItem("Event", Event_STS_Rejected, "Substrate process state is REJECTED."));
            EV.Subscribe(new EventItem("Event", Event_STS_Lost, "Substrate process state is LOST."));
            EV.Subscribe(new EventItem("Event", Event_STS_Skipped, "Substrate process state is SKIPPED."));
            EV.Subscribe(new EventItem("Event", Event_STS_Nostate, "Substrate process state is NOSTATE."));

            //EV.Subscribe(new EventItem("Event", Event_STS_Unoccupied, "Substrate location state is Unoccupied."));
            //EV.Subscribe(new EventItem("Event", Event_STS_Occupied, "Substrate location state is occupied."));


            OP.Subscribe("WaferManagerMoveWaferInfo", (cmd, param) =>
            {
                ModuleName sourceModule = (ModuleName)param[0];
                int sourceSlot = (int)param[1];
                ModuleName destModule = (ModuleName)param[2];
                int destSlot = (int)param[3];
                WaferMoved(sourceModule, sourceSlot, destModule, destSlot);
                return true;
            });

            Deserialize();
            _thread = new PeriodicJob(1000, Serialize, $"SerializeMonitorHandler", true);
        }

        public void SubscribeLocation(string module, int slotNumber)
        {
            ModuleName mod;
            if (Enum.TryParse(module, out mod))
            {
                SubscribeLocation(mod, slotNumber);
            }
            else
            {
                LOG.Write(string.Format("Failed SubscribeLocation, module name invalid, {0} ", module));
            }
        }

        public void SubscribeLocation(ModuleName module, int slotNumber)
        {
            if (!_locationWafers.ContainsKey(module))
            {
                _locationWafers[module] = new Dictionary<int, WaferInfo>();
                for (int i = 0; i < slotNumber; i++)
                {
                    _locationWafers[module][i] = new WaferInfo();
                }
            }

            DATA.Subscribe(module.ToString(), "ModuleWaferList", () => _locationWafers[module].Values.ToArray());
        }

        public void WaferMoved(ModuleName moduleFrom, int slotFrom, ModuleName moduleTo, int slotTo, bool needLog = true)
        {
            if (_locationWafers[moduleFrom][slotFrom].IsEmpty)
            {
                LOG.Write(string.Format("Invalid wafer move, no wafer at source, {0}{1}=>{2}{3}", moduleFrom, slotFrom + 1, moduleTo, slotTo + 1));
                return;
            }

            if (!_locationWafers[moduleTo][slotTo].IsEmpty)
            {
                LOG.Write(string.Format("Invalid wafer move, destination has wafer, {0}{1}=>{2}{3}", moduleFrom, slotFrom + 1, moduleTo, slotTo + 1));
                return;
            }

            UpdateWaferHistory(moduleFrom, slotFrom, SubstAccessType.Left);

            string waferOrigin = _locationWafers[moduleFrom][slotFrom].WaferOrigin;

            WaferInfo wafer = CopyWaferInfo(moduleTo, slotTo, _locationWafers[moduleFrom][slotFrom]);

            UpdateWaferHistory(moduleTo, slotTo, SubstAccessType.Arrive);
            DeleteWaferForMove(moduleFrom, slotFrom);


            WaferMoveHistoryRecorder.WaferMoved(wafer.InnerId.ToString(), moduleTo.ToString(), slotTo, wafer.Status.ToString());

            //EV.Notify(EventWaferLeft, new SerializableDictionary<string, string>()
            //    {
            //        {"SLOT_NO", (slotTo+1).ToString("D2")},
            //        {"Slot", (slotTo+1).ToString("D2") },
            //        {"WAFER_ID", wafer.WaferID},
            //        {"SubstID", wafer.WaferID},
            //        {"SubstLocState", "0"},
            //        {"LotID", wafer.LotId},
            //        {"LOT_ID", wafer.LotId},
            //        { "CAR_ID", GetCarrierID(moduleTo)},
            //        { "CarrierID", GetCarrierID(moduleTo)},
            //        { "LEFT_POS_NAME", $"{moduleFrom}.{(slotFrom+1).ToString("D2")}" },
            //        { "SubstLocID", $"{moduleFrom}.{(slotTo+1).ToString("D2")}" }
            //    });
            if (ModuleHelper.IsLoadPort(moduleFrom) || ModuleHelper.IsVCE(moduleFrom))
            {

                UpdateWaferTransportState(wafer.WaferID, SubstrateTransportStatus.AtWork);

                UpdateWaferE90State(wafer.WaferID, EnumE90Status.InProcess);

            }
            //EV.Notify(EventWaferArrive, new SerializableDictionary<string, string>()
            //    {
            //        {"SLOT_NO", (slotTo+1).ToString("D2")},
            //        {"Slot", (slotTo+1).ToString("D2") },
            //        {"WAFER_ID", wafer.WaferID},
            //        {"SubstID", wafer.WaferID},
            //        {"SubstLocState", "1"},
            //        {"LotID", wafer.LotId},
            //        {"LOT_ID", wafer.LotId},
            //        { "CAR_ID", GetCarrierID(moduleTo)},
            //        { "CarrierID", GetCarrierID(moduleTo)},
            //        { "ARRIVE_POS_NAME", $"{moduleTo}.{(slotTo+1).ToString("D2")}" },
            //        { "SubstLocID", $"{moduleTo}.{(slotTo+1).ToString("D2")}" }
            //    });

            if (ModuleHelper.IsLoadPort(moduleTo) || ModuleHelper.IsVCE(moduleTo))
            {


                if (wafer.SubstE90Status == EnumE90Status.InProcess)
                    UpdateWaferE90State(wafer.WaferID, EnumE90Status.Processed);


                if (moduleTo == (ModuleName)wafer.OriginStation && wafer.SubstE90Status != EnumE90Status.Processed)
                    UpdateWaferTransportState(wafer.WaferID, SubstrateTransportStatus.AtSource);
                else
                    UpdateWaferTransportState(wafer.WaferID, SubstrateTransportStatus.AtDestination);


            }

            if (needLog)
                EV.PostInfoLog("System", $"Wafer:{wafer.WaferID} (original:{wafer.WaferOrigin}) moved from {moduleFrom} slot:{slotFrom + 1} to {moduleTo} slot:{slotTo + 1}.");

            //Serialize();
            _needSerialize = true;
        }

        //传送过程中出现错误，Wafer未知
        public void WaferDuplicated(ModuleName moduleFrom, int slotFrom, ModuleName moduleTo, int slotTo)
        {
            UpdateWaferHistory(moduleFrom, slotFrom, SubstAccessType.Left);

            if (_locationWafers[moduleFrom][slotFrom].IsEmpty)
            {
                LOG.Write(string.Format("Invalid wafer move, no wafer at source, {0}{1}=>{2}{3}", moduleFrom, slotFrom + 1, moduleTo, slotTo + 1));
                return;
            }

            if (!_locationWafers[moduleTo][slotTo].IsEmpty)
            {
                LOG.Write(string.Format("Invalid wafer move, destination has wafer, {0}{1}=>{2}{3}", moduleFrom, slotFrom + 1, moduleTo, slotTo + 1));
                return;
            }

            string waferOrigin = _locationWafers[moduleFrom][slotFrom].WaferOrigin;

            WaferInfo wafer = CopyWaferInfo(moduleTo, slotTo, _locationWafers[moduleFrom][slotFrom]);

            UpdateWaferHistory(moduleTo, slotTo, SubstAccessType.Arrive);
            //DeleteWaferForMove(moduleFrom, slotFrom);

            //EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferMoved, waferOrigin, moduleFrom.ToString(), slotFrom + 1, moduleTo.ToString(), slotTo + 1);

            EV.PostInfoLog("System", $"Wafer:{wafer.WaferID} (original:{wafer.WaferOrigin}) moved from {moduleFrom} slot:{slotFrom + 1} to {moduleTo} slot:{slotTo + 1}.");

            WaferMoveHistoryRecorder.WaferMoved(wafer.InnerId.ToString(), moduleTo.ToString(), slotTo, wafer.Status.ToString());

            //EV.Notify(EventWaferLeft, new SerializableDictionary<string, string>()
            //    {
            //        {"SLOT_NO", (slotFrom+1).ToString("D2")},
            //        {"Slot", (slotFrom+1).ToString("D2")},
            //        {"WAFER_ID", wafer.WaferID},
            //        {"SubstID", wafer.WaferID},
            //        {"SubstLocState", "0"},
            //        {"LOT_ID", wafer.LotId},
            //        {"LotID", wafer.LotId},
            //        { "CAR_ID", GetCarrierID(moduleFrom)},
            //        { "CarrierID", GetCarrierID(moduleFrom)},
            //        { "LEFT_POS_NAME", $"{moduleFrom}.{(slotFrom+1).ToString("D2")}" },
            //        { "SubstLocID", $"{moduleFrom}.{(slotTo+1).ToString("D2")}" }
            //    });
            if (ModuleHelper.IsLoadPort(moduleFrom) || ModuleHelper.IsVCE(moduleFrom))
            {
                UpdateWaferTransportState(wafer.WaferID, SubstrateTransportStatus.AtWork);

                UpdateWaferE90State(wafer.WaferID, EnumE90Status.InProcess);
            }
            //EV.Notify(EventWaferArrive, new SerializableDictionary<string, string>()
            //    {
            //        {"SLOT_NO", (slotTo+1).ToString("D2")},
            //        {"WAFER_ID", wafer.WaferID},
            //        {"SubstID", wafer.WaferID},
            //        {"SubstLocState", "1"},
            //        {"LOT_ID", wafer.LotId},
            //        {"LotID", wafer.LotId},
            //        { "CAR_ID", GetCarrierID(moduleTo)},
            //        { "CarrierID", GetCarrierID(moduleTo)},
            //        { "ARRIVE_POS_NAME", $"{moduleTo}.{(slotTo+1).ToString("D2")}" },
            //        { "SubstLocID", $"{moduleTo}.{(slotTo+1).ToString("D2")}" }
            //    });

            if (ModuleHelper.IsLoadPort(moduleTo) || ModuleHelper.IsVCE(moduleTo))
            {


                if (wafer.SubstE90Status == EnumE90Status.InProcess)
                    UpdateWaferE90State(wafer.WaferID, EnumE90Status.Processed);


                if (moduleTo == (ModuleName)wafer.OriginStation && wafer.SubstE90Status != EnumE90Status.Processed)
                    UpdateWaferTransportState(wafer.WaferID, SubstrateTransportStatus.AtSource);
                else
                    UpdateWaferTransportState(wafer.WaferID, SubstrateTransportStatus.AtDestination);


            }

            UpdateWaferProcessStatus(moduleFrom, slotFrom, EnumWaferProcessStatus.Failed);
            UpdateWaferProcessStatus(moduleTo, slotTo, EnumWaferProcessStatus.Failed);

            //Serialize();
            _needSerialize = true;
        }


        private string GetCarrierID(ModuleName module)
        {
            if (!ModuleHelper.IsLoadPort(module))
                return "";
            var id = DATA.Poll($"{module.ToString()}.CarrierId");
            if (id == null)
                return "";

            return id.ToString();
        }

        public bool IsWaferSlotLocationValid(ModuleName module, int slot)
        {
            return _locationWafers.ContainsKey(module) && _locationWafers[module].ContainsKey(slot);
        }

        public WaferInfo[] GetWafers(ModuleName module)
        {
            if (!_locationWafers.ContainsKey(module))
                return null;
            return _locationWafers[module].Values.ToArray();

        }

        public WaferInfo[] GetWaferByProcessJob(string jobName)
        {
            List<WaferInfo> wafers = new List<WaferInfo>();
            foreach (var moduleWafer in _locationWafers)
            {
                foreach (var waferInfo in moduleWafer.Value)
                {
                    if (waferInfo.Value != null && !waferInfo.Value.IsEmpty
                                              && (waferInfo.Value.ProcessJob != null)
                        && waferInfo.Value.ProcessJob.Name == jobName)
                        wafers.Add(waferInfo.Value);

                }
            }
            return wafers.ToArray();

        }
        public WaferInfo[] GetWafersByOriginalPosition(ModuleName module, int slot)
        {
            List<WaferInfo> ret = new List<WaferInfo>();
            foreach (var locationWafer in _locationWafers)
            {
                foreach (var wafer in locationWafer.Value)
                {
                    if (wafer.Value.IsEmpty) continue;
                    if (wafer.Value.OriginStation == (int)module && wafer.Value.OriginSlot == slot)
                        ret.Add(wafer.Value);
                }
            }
            return ret.ToArray();
        }

        public WaferInfo[] GetWafersByOriginalPosition(int originStation, int slot)
        {
            List<WaferInfo> ret = new List<WaferInfo>();
            foreach (var locationWafer in _locationWafers)
            {
                foreach (var wafer in locationWafer.Value)
                {
                    if (wafer.Value.IsEmpty) continue;
                    if (wafer.Value.OriginStation == originStation && wafer.Value.OriginSlot == slot)
                        ret.Add(wafer.Value);
                }
            }
            return ret.ToArray();
        }

        public WaferInfo[] GetWafer(string waferID)
        {
            List<WaferInfo> result = new List<WaferInfo>();
            foreach (var locationWafer in _locationWafers)
            {
                foreach (var wafer in locationWafer.Value)
                {
                    if (wafer.Value.WaferID == waferID)
                        result.Add(wafer.Value);
                }
            }

            return result.ToArray();
        }

        public WaferInfo GetWafer(ModuleName module, int slot)
        {
            if (!_locationWafers.ContainsKey(module))
                return null;
            if (!_locationWafers[module].ContainsKey(slot))
                return null;
            return _locationWafers[module][slot];
        }
        public WaferInfo[] GetWafers(string Originalcarrier, int Originalslot)
        {
            List<WaferInfo> ret = new List<WaferInfo>();
            foreach (var locationWafer in _locationWafers)
            {
                foreach (var wafer in locationWafer.Value)
                {
                    if (wafer.Value.OriginCarrierID == Originalcarrier && wafer.Value.OriginSlot == Originalslot)
                        ret.Add(wafer.Value);
                }
            }
            return ret.ToArray();
        }

        public string GetWaferID(ModuleName module, int slot)
        {
            return IsWaferSlotLocationValid(module, slot) ? _locationWafers[module][slot].WaferID : "";
        }
        public WaferSize GetWaferSize(ModuleName module, int slot)
        {
            return IsWaferSlotLocationValid(module, slot) ? _locationWafers[module][slot].Size : WaferSize.WS0;
        }
        public bool CheckNoWafer(ModuleName module, int slot)
        {
            return IsWaferSlotLocationValid(module, slot) && _locationWafers[module][slot].IsEmpty;
        }

        public bool CheckNoWafer(string module, int slot)
        {
            return CheckNoWafer((ModuleName)Enum.Parse(typeof(ModuleName), module), slot);
        }

        public bool CheckHasWafer(ModuleName module, int slot)
        {
            return IsWaferSlotLocationValid(module, slot) && !_locationWafers[module][slot].IsEmpty;
        }
        public bool CheckWaferIsDummy(ModuleName module, int slot)
        {
            return IsWaferSlotLocationValid(module, slot) && !_locationWafers[module][slot].IsEmpty
                                                          && _locationWafers[module][slot].Status == WaferStatus.Dummy;
        }

        /// <summary>
        /// Verify the slot map in string[] format, if there's any mis-match it will return false.
        /// </summary>
        /// <param name="moduleNo">LP No</param>
        /// <param name="flagStrings">Flag input</param>
        /// <returns></returns>
        public bool CheckWaferExistFlag(string moduleNo, string[] flagStrings, out string reason)
        {
            var i = 0;
            reason = string.Empty;
            if (!Enum.TryParse($"LP{moduleNo}", out ModuleName module))
            {
                reason = "Port Number Error";
                return false;
            }
            foreach (var slot in flagStrings)
            {
                if (slot == "1")
                {
                    if (IsWaferSlotLocationValid(module, i) && _locationWafers[module][i].IsEmpty)
                    {
                        reason = "Flag Mis-Match";
                        return false;
                    }
                }
                else
                {
                    if (IsWaferSlotLocationValid(module, i) && !_locationWafers[module][i].IsEmpty)
                    {
                        reason = "Flag Mis-Match";
                        return false;
                    }
                }
                i++;
            }
            return true;
        }
        public bool CheckHasWafer(string module, int slot)
        {
            return CheckHasWafer((ModuleName)Enum.Parse(typeof(ModuleName), module), slot);
        }
        public bool CheckWaferFull(ModuleName module)
        {
            var wafers = _locationWafers[module];
            foreach (var waferInfo in wafers)
            {
                if (waferInfo.Value.IsEmpty)
                    return false;
            }

            return true;
        }

        public bool CheckWaferEmpty(ModuleName module)
        {
            var wafers = _locationWafers[module];
            foreach (var waferInfo in wafers)
            {
                if (!waferInfo.Value.IsEmpty)
                    return false;
            }

            return true;
        }
        public bool CheckWafer(ModuleName module, int slot, WaferStatus state)
        {
            return IsWaferSlotLocationValid(module, slot) && (_locationWafers[module][slot].Status == state);
        }

        public WaferInfo CreateWafer(ModuleName module, int slot, WaferStatus state, WaferType waferType = WaferType.None, string lotId = "")
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Invalid wafer create, invalid parameter, {0},{1}", module, slot + 1));
                return null;
            }

            string carrierInnerId = "";
            string carrierID = string.Empty;
            if (ModuleHelper.IsLoadPort(module) || ModuleHelper.IsCassette(module) || ModuleHelper.IsVCE(module))
            {
                CarrierInfo carrier = CarrierManager.Instance.GetCarrier(module.ToString());
                if (carrier == null)
                {
                    EV.PostWarningLog(ModuleName.System.ToString(), string.Format("No carrier at {0}, can not create wafer.", module));
                    return null;
                }

                carrierInnerId = carrier.InnerId.ToString();
                carrierID = carrier.CarrierId;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].Status = state;
                _locationWafers[module][slot].ProcessState = EnumWaferProcessStatus.Idle;
                _locationWafers[module][slot].SubstE90Status = EnumE90Status.NeedProcessing;


                _locationWafers[module][slot].WaferID = GenerateWaferId(module, slot, carrierID);
                _locationWafers[module][slot].WaferOrigin = GenerateOrigin(module, slot);

                _locationWafers[module][slot].Station = (int)module;
                _locationWafers[module][slot].Slot = slot;

                _locationWafers[module][slot].InnerId = Guid.NewGuid();

                _locationWafers[module][slot].OriginStation = (int)module;
                _locationWafers[module][slot].OriginSlot = slot;
                _locationWafers[module][slot].OriginCarrierID = carrierID;

                _locationWafers[module][slot].LotId = lotId;
                _locationWafers[module][slot].HostLaserMark1 = "";
                _locationWafers[module][slot].HostLaserMark2 = "";
                _locationWafers[module][slot].LaserMarker = "";
                _locationWafers[module][slot].T7Code = "";

                _locationWafers[module][slot].PPID = "";
                _locationWafers[module][slot].WaferType = waferType;

                SubstHistory hist = new SubstHistory(module.ToString(), slot, DateTime.Now, SubstAccessType.Create);
                _locationWafers[module][slot].SubstHists = new SubstHistory[] { hist };

                _dict[_locationWafers[module][slot].WaferID] = _locationWafers[module][slot];



                //EV.Notify(Event_STS_Occupied, new SerializableDictionary<string, object>()
                //{
                //    {SubstLocID,module.ToString()},
                //    {SubstID,_locationWafers[module][slot].WaferID},
                //    {SubstLocState,1}
                //});


            }

            UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.NeedProcessing);
            UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.AtSource);


            WaferDataRecorder.CreateWafer(_locationWafers[module][slot].InnerId.ToString(), carrierInnerId, module.ToString(), slot, _locationWafers[module][slot].WaferID, _locationWafers[module][slot].ProcessState.ToString());

            //Serialize();
            _needSerialize = true;
            EV.PostInfoLog("System", $"Create wafer successfully on {module} slot:{slot + 1}.");
            return _locationWafers[module][slot];
        }

        public WaferInfo CreateWafer(ModuleName module, int slot, ModuleName originModule, int originSlot, WaferStatus state, WaferType waferType = WaferType.None)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Invalid wafer create, invalid parameter, {0},{1}", module, slot + 1));
                return null;
            }

            string carrierInnerId = "";
            string carrierID = string.Empty;
            if (ModuleHelper.IsLoadPort(module) || ModuleHelper.IsCassette(module))
            {
                CarrierInfo carrier = CarrierManager.Instance.GetCarrier(module.ToString());
                if (carrier == null)
                {
                    EV.PostMessage(ModuleName.System.ToString(), EventEnum.DefaultWarning,
                        string.Format("No carrier at {0}, can not create wafer", module));
                    return null;
                }

                carrierInnerId = carrier.InnerId.ToString();
                carrierID = carrier.CarrierId;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].Status = state;
                _locationWafers[module][slot].ProcessState = EnumWaferProcessStatus.Idle;
                _locationWafers[module][slot].SubstE90Status = EnumE90Status.NeedProcessing;


                _locationWafers[module][slot].WaferID = GenerateWaferId(originModule, originSlot, carrierID);
                _locationWafers[module][slot].WaferOrigin = GenerateOrigin(originModule, originSlot);

                _locationWafers[module][slot].Station = (int)module;
                _locationWafers[module][slot].Slot = slot;

                _locationWafers[module][slot].InnerId = Guid.NewGuid();

                _locationWafers[module][slot].OriginStation = (int)originModule;
                _locationWafers[module][slot].OriginSlot = originSlot;
                _locationWafers[module][slot].OriginCarrierID = carrierID;

                _locationWafers[module][slot].LotId = "";
                _locationWafers[module][slot].HostLaserMark1 = "";
                _locationWafers[module][slot].HostLaserMark2 = "";
                _locationWafers[module][slot].LaserMarker = "";
                _locationWafers[module][slot].T7Code = "";

                _locationWafers[module][slot].PPID = "";
                _locationWafers[module][slot].WaferType = waferType;

                SubstHistory hist = new SubstHistory(module.ToString(), slot, DateTime.Now, SubstAccessType.Create);
                _locationWafers[module][slot].SubstHists = new SubstHistory[] { hist };

                _dict[_locationWafers[module][slot].WaferID] = _locationWafers[module][slot];



                //EV.Notify(Event_STS_Occupied, new SerializableDictionary<string, object>()
                //{
                //    {SubstLocID,module.ToString()},
                //    {SubstID,_locationWafers[module][slot].WaferID},
                //    {SubstLocState,1}
                //});


            }

            UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.NeedProcessing);
            UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.AtSource);


            WaferDataRecorder.CreateWafer(_locationWafers[module][slot].InnerId.ToString(), carrierInnerId, module.ToString(), slot, _locationWafers[module][slot].WaferID, _locationWafers[module][slot].ProcessState.ToString());

            //Serialize();
            _needSerialize = true;
            EV.PostInfoLog("System", $"Create wafer successfully on {module} slot:{slot + 1}.");
            return _locationWafers[module][slot];
        }

        public WaferInfo CreateWafer(ModuleName module, int slot, WaferStatus state, WaferSize wz)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Invalid wafer create, invalid parameter, {0},{1}", module, slot + 1));
                return null;
            }

            string carrierInnerId = "";
            string carrierID = string.Empty;
            if (ModuleHelper.IsLoadPort(module) || ModuleHelper.IsCassette(module))
            {
                CarrierInfo carrier = CarrierManager.Instance.GetCarrier(module.ToString());
                if (carrier == null)
                {
                    EV.PostWarningLog(ModuleName.System.ToString(), string.Format("No carrier at {0}, can not create wafer.", module));
                    return null;
                }

                carrierInnerId = carrier.InnerId.ToString();
                carrierID = carrier.CarrierId;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].Status = state;
                _locationWafers[module][slot].ProcessState = EnumWaferProcessStatus.Idle;
                _locationWafers[module][slot].SubstE90Status = EnumE90Status.NeedProcessing;


                _locationWafers[module][slot].WaferID = GenerateWaferId(module, slot, carrierID);
                _locationWafers[module][slot].WaferOrigin = GenerateOrigin(module, slot);

                _locationWafers[module][slot].Station = (int)module;
                _locationWafers[module][slot].Slot = slot;

                _locationWafers[module][slot].InnerId = Guid.NewGuid();

                _locationWafers[module][slot].OriginStation = (int)module;
                _locationWafers[module][slot].OriginSlot = slot;
                _locationWafers[module][slot].OriginCarrierID = carrierID;

                _locationWafers[module][slot].LotId = "";
                _locationWafers[module][slot].HostLaserMark1 = "";
                _locationWafers[module][slot].HostLaserMark2 = "";
                _locationWafers[module][slot].LaserMarker = "";
                _locationWafers[module][slot].T7Code = "";
                _locationWafers[module][slot].PPID = "";
                _locationWafers[module][slot].Size = wz;
                SubstHistory hist = new SubstHistory(module.ToString(), slot, DateTime.Now, SubstAccessType.Create);
                _locationWafers[module][slot].SubstHists = new SubstHistory[] { hist };

                _dict[_locationWafers[module][slot].WaferID] = _locationWafers[module][slot];


            }
            UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.NeedProcessing);
            UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.AtSource);

            EV.PostInfoLog("System", $"Create wafer successfully on {module} slot:{slot + 1} wafersize:{wz}.");
            WaferDataRecorder.CreateWafer(_locationWafers[module][slot].InnerId.ToString(), carrierInnerId, module.ToString(), slot, _locationWafers[module][slot].WaferID, _locationWafers[module][slot].ProcessState.ToString());

            //Serialize();
            _needSerialize = true;

            return _locationWafers[module][slot];
        }

        public void DeleteWafer(ModuleName module, int slotFrom, int count = 1)
        {
            lock (_lockerWaferList)
            {
                EV.PostInfoLog("System", $"Delete wafer successfully on {module} from slot:{slotFrom + 1} count:{count}.");

                for (int i = 0; i < count; i++)
                {
                    int slot = slotFrom + i;
                    if (!IsWaferSlotLocationValid(module, slot))
                    {
                        LOG.Write(string.Format("Invalid wafer delete, invalid parameter, {0},{1}", module, slot + 1));
                        continue;
                    }
                    //EV.Notify(Event_STS_Unoccupied, new SerializableDictionary<string, object>()
                    //{
                    //    {SubstLocID,module.ToString()},
                    //    {SubstID,_locationWafers[module][slot].WaferID},
                    //    {SubstLocState,0}
                    //});
                    UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.None);
                    UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.None);
                    WaferDataRecorder.DeleteWafer(_locationWafers[module][slot].InnerId.ToString());

                    _locationWafers[module][slot].SetEmpty();
                    _locationWafers[module][slot].SubstHists = null;


                    _dict.Remove(_locationWafers[module][slot].WaferID);
                }
            }

            //Serialize();
            _needSerialize = true;
        }
        public void DeleteWaferForMove(ModuleName module, int slotFrom, int count = 1)
        {
            lock (_lockerWaferList)
            {
                for (int i = 0; i < count; i++)
                {
                    int slot = slotFrom + i;
                    if (!IsWaferSlotLocationValid(module, slot))
                    {
                        LOG.Write(string.Format("Invalid wafer delete, invalid parameter, {0},{1}", module, slot + 1));
                        continue;
                    }
                    //EV.Notify(Event_STS_Unoccupied, new SerializableDictionary<string, object>()
                    //{
                    //    {SubstLocID,module.ToString()},
                    //    {SubstID,_locationWafers[module][slot].WaferID},
                    //    {SubstLocState,0}
                    //});
                    //UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.None);

                    //???
                    //WaferDataRecorder.DeleteWafer(_locationWafers[module][slot].InnerId.ToString());

                    _locationWafers[module][slot].SetEmpty();
                    _locationWafers[module][slot].SubstHists = null;


                    _dict.Remove(_locationWafers[module][slot].WaferID);
                }
            }

            //Serialize();
            _needSerialize = true;
        }

        public void ManualDeleteWafer(ModuleName module, int slotFrom, int count = 1)
        {

            for (int i = 0; i < count; i++)
            {
                int slot = slotFrom + i;
                if (!IsWaferSlotLocationValid(module, slot))
                {
                    LOG.Write(string.Format("Invalid wafer delete, invalid parameter, {0},{1}", module, slot + 1));
                    continue;
                }
                //    EV.Notify(Event_STS_Unoccupied, new SerializableDictionary<string, object>()
                //{
                //    {SubstLocID,module.ToString()},
                //    {SubstID,_locationWafers[module][slot].WaferID},
                //    {SubstLocState,0}
                //});

                UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.Lost);
                UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.None);
                UpdateWaferHistory(module, slotFrom, SubstAccessType.Delete);

                lock (_lockerWaferList)
                {
                    WaferDataRecorder.DeleteWafer(_locationWafers[module][slot].InnerId.ToString());

                    _locationWafers[module][slot].SetEmpty();

                    _dict.Remove(_locationWafers[module][slot].WaferID);
                }
            }

            //Serialize();
            _needSerialize = true;
        }


        public void UpdateWaferLaser(ModuleName module, int slot, string laserMarker)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferLaser, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].LaserMarker = laserMarker;

                WaferDataRecorder.SetWaferMarker(_locationWafers[module][slot].InnerId.ToString(), laserMarker);
            }

            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferDestination(ModuleName module, int slot, string destCarrierID, int destslot)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferLaser, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].DestinationCarrierID = destCarrierID;
                _locationWafers[module][slot].DestinationSlot = destslot;


                //WaferDataRecorder.SetWaferMarker(_locationWafers[module][slot].InnerId.ToString(), laserMarker);
            }

            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferDestination(ModuleName module, int slot, ModuleName destmodule, int destslot)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferLaser, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].DestinationStation = (int)destmodule;
                _locationWafers[module][slot].DestinationSlot = destslot;


                //WaferDataRecorder.SetWaferMarker(_locationWafers[module][slot].InnerId.ToString(), laserMarker);
            }

            //Serialize();
            _needSerialize = true;
        }


        public void UpdateWaferLaserWithScoreAndFileName(ModuleName module, int slot, string laserMarker, string laserMarkerScore, string fileName, string filePath)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferLaser, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].LaserMarker = laserMarker;
                _locationWafers[module][slot].LaserMarkerScore = laserMarkerScore;
                _locationWafers[module][slot].ImageFileName = fileName;
                _locationWafers[module][slot].ImageFilePath = filePath;

                WaferDataRecorder.SetWaferMarkerWithScoreAndFileName(_locationWafers[module][slot].InnerId.ToString(), laserMarker, laserMarkerScore, fileName, filePath);
            }

            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferT7Code(ModuleName module, int slot, string T7Code)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferT7Code, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].T7Code = T7Code;

                WaferDataRecorder.SetWaferT7Code(_locationWafers[module][slot].InnerId.ToString(), T7Code);
            }
            //Serialize();
            _needSerialize = true;
        }

        public void UpdataWaferPPID(ModuleName module, int slot, string PPID)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferPPID, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].PPID = PPID;

                WaferDataRecorder.SetWaferSequence(_locationWafers[module][slot].InnerId.ToString(), PPID);
            }
            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferT7CodeWithScoreAndFileName(ModuleName module, int slot, string t7Code, string t7CodeScore, string fileName, string filePath)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferT7Code, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].T7Code = t7Code;
                _locationWafers[module][slot].T7CodeScore = t7CodeScore;
                _locationWafers[module][slot].ImageFileName = fileName;
                _locationWafers[module][slot].ImageFilePath = filePath;

                WaferDataRecorder.SetWaferT7CodeWithScoreAndFileName(_locationWafers[module][slot].InnerId.ToString(), t7Code, t7CodeScore, fileName, filePath);
            }
            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferTransFlag(ModuleName module, int slot, string flag)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferTransFlag, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].TransFlag = flag;

            }
            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferNotch(ModuleName module, int slot, int angle)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferNotch, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].Notch = angle;
            }
            //Serialize();
            _needSerialize = true;
        }
        public void UpdateWaferStatistics(ModuleName module, int slot, float useCount, float useTime, float useThick)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferStatistics, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].UseCount = useCount;
                _locationWafers[module][slot].UseTime = useTime;
                _locationWafers[module][slot].UseThick = useThick;
                WaferDataRecorder.SetStatistics(_locationWafers[module][slot].InnerId.ToString(), useCount, useTime, useThick);
            }
            _needSerialize = true;
        }

        public void UpdateWaferProcessStatus(ModuleName module, int slot, EnumWaferProcessStatus status)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferProcessStatus, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].ProcessState = status;

                WaferDataRecorder.SetWaferStatus(_locationWafers[module][slot].InnerId.ToString(), status.ToString());
            }
            //Serialize();
            _needSerialize = true;
        }
#pragma warning disable CS0618  
        public void UpdateWaferProcessStatus(ModuleName module, int slot, ProcessStatus status)
        {
            switch (status)
            {
                case ProcessStatus.Busy:
                    UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.InProcess);
                    break;
                case ProcessStatus.Completed:
                    UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.Completed);
                    break;
                case ProcessStatus.Failed:
                case ProcessStatus.Abort:
                    UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.Failed);
                    break;
                //case ProcessStatus.Abort:
                //    UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.Abort);
                //    break;
                case ProcessStatus.Idle:
                    UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.Idle);
                    break;
                case ProcessStatus.Wait:
                    UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.InProcess);
                    break;
            }
        }

        public void UpdateWaferProcessStatus(string waferID, ProcessStatus status)
        {
            switch (status)
            {
                case ProcessStatus.Busy:
                    UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.InProcess);
                    break;
                case ProcessStatus.Completed:
                    UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.Completed);
                    break;
                case ProcessStatus.Failed:
                    UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.Failed);
                    break;
                case ProcessStatus.Idle:
                    UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.Idle);
                    break;
                case ProcessStatus.Wait:
                    UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.InProcess);
                    break;
            }
        }

#pragma warning restore CS0618  

        public void UpdateWaferId(ModuleName module, int slot, string waferId)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferId, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].WaferID = waferId;
            }
            WaferDataRecorder.UpdateWaferId(_locationWafers[module][slot].InnerId.ToString(), waferId);
            UpdateWaferHistory(module, slot, SubstAccessType.UpdateWaferID);
            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferJodID(ModuleName module, int slot, string pjID, string cjID)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferId, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }
            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].ProcessJobID = pjID;
                _locationWafers[module][slot].ControlJobID = cjID;

            }
            //Serialize();
            _needSerialize = true;
        }

        public void SlotMapVerifyOK(ModuleName module)
        {
            WaferInfo[] wafers = GetWafers(module);
            foreach (WaferInfo wafer in wafers)
            {
                lock (_lockerWaferList)
                {
                    if (wafer.IsEmpty) continue;


                }              //Serialize();


            }
            _needSerialize = true;
        }

        public int portidswicth(int modulename)
        {
            if (modulename == 40)
            {
                return 1;
            }
            else if (modulename == 41)
            {
                return 2;
            }
            else return 0;
        }

        public void UpdateWaferTransportState(string waferid, SubstrateTransportStatus ststate)
        {
            if (string.IsNullOrEmpty(waferid)) return;
            WaferInfo[] wafers = GetWafer(waferid);

            lock (_lockerWaferList)
            {
                foreach (var wafer in wafers)
                {
                    //_locationWafers[(ModuleName)wafer.Station][wafer.Slot].SubstTransStatus  = ststate;
                    wafer.SubstTransStatus = ststate;
                    var dvid = new SerializableDictionary<string, object>()
                    {
                    {SubstID, wafer.WaferID},
                    {LotID, wafer.LotId},
                    {SubstMtrlStatus,(int)wafer.ProcessState },
                    {SubstDestination,((ModuleName)wafer.DestinationStation).ToString() },
                    {SubstHistory,wafer.SubstHists},
                    {SubstLocID,((ModuleName)wafer.Station).ToString()},
                    {SubstProcState,(int)wafer.SubstE90Status},
                    {SubstSource,((ModuleName)wafer.OriginStation).ToString() },
                    {SubstState,(int)wafer.SubstTransStatus},
                     };

                    if (ststate == SubstrateTransportStatus.AtWork) EV.Notify(Event_STS_AtWork, dvid);

                    if (ststate == SubstrateTransportStatus.AtDestination) EV.Notify(Event_STS_AtDestination, dvid);

                    if (ststate == SubstrateTransportStatus.AtSource) EV.Notify(Event_STS_AtSourcs, dvid);

                    if (ststate == SubstrateTransportStatus.None) EV.Notify(Event_STS_Deleted, dvid);



                }
            }
            //Serialize();
            _needSerialize = true;
            //Serialize();
            _needSerialize = true;
        }


        public void UpdateWaferE90State(string waferid, EnumE90Status E90state)
        {
            if (string.IsNullOrEmpty(waferid)) return;
            WaferInfo[] wafers = GetWafer(waferid);

            lock (_lockerWaferList)
            {
                foreach (var wafer in wafers)
                {
                    if (wafer.SubstE90Status == E90state)
                        continue;
                    //_locationWafers[(ModuleName)wafer.Station][wafer.Slot].SubstE90Status = E90state;
                    wafer.SubstE90Status = E90state;
                    string currentcid = "";
                    if (ModuleHelper.IsLoadPort((ModuleName)wafer.Station))
                        currentcid = CarrierManager.Instance.GetCarrier(((ModuleName)wafer.Station).ToString()).CarrierId;

                    SECsDataItem data = new SECsDataItem(SECsFormat.List);
                    data.Add("SourceCarrier", wafer.OriginCarrierID ?? "");
                    data.Add("SourceSlot", (wafer.OriginSlot + 1).ToString());
                    data.Add("CurrentCarrier", currentcid ?? "");
                    data.Add("CurrentSlot", (wafer.Slot + 1).ToString());
                    data.Add("LaserMark1", wafer.LaserMarker ?? "");
                    data.Add("LaserMark1Score", wafer.LaserMarkerScore ?? "");
                    data.Add("LaserMark2", wafer.T7Code ?? "");
                    data.Add("LaserMark2Score", wafer.T7CodeScore ?? "");

                    var dvid = new SerializableDictionary<string, object>()
                    {
                        {SubstID, wafer.WaferID},
                        {LotID, wafer.LotId},
                        {"PortID", portidswicth((int)wafer.OriginStation)},
                        {SubstMtrlStatus,(int)wafer.ProcessState },
                        {SubstDestination,((ModuleName)wafer.DestinationStation).ToString() },
                        {SubstHistory,wafer.SubstHists},
                        {SubstLocID,((ModuleName)wafer.Station).ToString()},
                        {SubstProcState,(int)wafer.SubstE90Status},
                        {SubstState,(int)wafer.SubstTransStatus},
                        {SubstSource,((ModuleName)(wafer.OriginStation)).ToString() },
                        {SubstSourceCarrierID,wafer.OriginCarrierID  },
                        {SubstSourceSlot,wafer.OriginSlot +1 },
                        {SubstCurrentSlot,wafer.Slot+1 },
                        {"LASERMARK",wafer.LaserMarker??"" },
                        {"OCRScore",wafer.LaserMarkerScore??"" },
                        {"CarrierID",currentcid??"" },
                        {"LASERMARK1",wafer.LaserMarker??"" },
                        {"LASERMARK2",wafer.T7Code??"" },
                        {"LASERMARK1SCORE",wafer.LaserMarkerScore??"" },
                        {"LASERMARK2SCORE",wafer.T7CodeScore??"" },

                        {"PORT_ID",wafer.Station},
                        {"SubstInfoList",data }
                     };
                    if (E90state == EnumE90Status.InProcess) EV.Notify(Event_STS_InProcessing, dvid);
                    if (E90state == EnumE90Status.NeedProcessing) EV.Notify(Event_STS_NeedProcessing, dvid);
                    if (E90state == EnumE90Status.Processed) EV.Notify(Event_STS_Processed, dvid);
                    if (E90state == EnumE90Status.Aborted) EV.Notify(Event_STS_Aborted, dvid);
                    if (E90state == EnumE90Status.Lost) EV.Notify(Event_STS_Lost, dvid);
                    if (E90state == EnumE90Status.Rejected) EV.Notify(Event_STS_Rejected, dvid);
                    if (E90state == EnumE90Status.Skipped) EV.Notify(Event_STS_Skipped, dvid);
                    if (E90state == EnumE90Status.Stopped) EV.Notify(Event_STS_Stopped, dvid);
                }
            }
            //Serialize();
            _needSerialize = true;

        }

        public void UpdateWaferE90State(ModuleName module, int slot, EnumE90Status E90state)
        {

            WaferInfo wafer = GetWafer(module, slot);
            if (wafer.IsEmpty) return;

            lock (_lockerWaferList)
            {

                //_locationWafers[(ModuleName)wafer.Station][wafer.Slot].SubstE90Status = E90state;
                wafer.SubstE90Status = E90state;
                string currentcid = "";
                if (ModuleHelper.IsLoadPort((ModuleName)wafer.Station))
                    currentcid = CarrierManager.Instance.GetCarrier(((ModuleName)wafer.Station).ToString()).CarrierId;

                var dvid = new SerializableDictionary<string, object>()
                    {
                    {SubstID, wafer.WaferID},
                    {LotID, wafer.LotId},
                    {SubstMtrlStatus,(int)wafer.ProcessState },
                    {SubstDestination,((ModuleName)wafer.DestinationStation).ToString() },
                    {SubstHistory,wafer.SubstHists},
                    {SubstLocID,((ModuleName)wafer.Station).ToString()},
                    {SubstProcState,(int)wafer.SubstE90Status},
                    {SubstState,(int)wafer.SubstTransStatus},
                    {SubstSource,((ModuleName)(wafer.OriginStation)).ToString() },
                    {SubstSourceCarrierID,wafer.OriginCarrierID  },
                    {SubstSourceSlot,wafer.OriginSlot +1 },
                    {SubstCurrentSlot,wafer.Slot+1 },
                        {"LASERMARK",wafer.LaserMarker },
                        {"OCRScore",wafer.LaserMarkerScore },
                        {"CarrierID",currentcid }

                     };
                if (E90state == EnumE90Status.InProcess) EV.Notify(Event_STS_InProcessing, dvid);
                if (E90state == EnumE90Status.NeedProcessing) EV.Notify(Event_STS_NeedProcessing, dvid);
                if (E90state == EnumE90Status.Processed) EV.Notify(Event_STS_Processed, dvid);
                if (E90state == EnumE90Status.Aborted) EV.Notify(Event_STS_Aborted, dvid);
                if (E90state == EnumE90Status.Lost) EV.Notify(Event_STS_Lost, dvid);
                if (E90state == EnumE90Status.Rejected) EV.Notify(Event_STS_Rejected, dvid);
                if (E90state == EnumE90Status.Skipped) EV.Notify(Event_STS_Skipped, dvid);
                if (E90state == EnumE90Status.Stopped) EV.Notify(Event_STS_Stopped, dvid);

            }
            //Serialize();
            _needSerialize = true;

        }

        public void UpdateWaferHistory(ModuleName module, int slot, SubstAccessType accesstype)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferHistory, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }

            lock (_lockerWaferList)
            {
                if (accesstype == SubstAccessType.Left || accesstype == SubstAccessType.Delete)
                {
                    if (_locationWafers[module][slot].SubstHists == null)
                        return;
                    foreach (var histItem in _locationWafers[module][slot].SubstHists)
                    {
                        if (histItem.locationID == module.ToString() && histItem.SlotNO == slot && histItem.AccessTimeOut == DateTime.MinValue)
                        {
                            histItem.AccessTimeOut = DateTime.Now;
                            break;
                        }
                    }
                }
                if (accesstype == SubstAccessType.Arrive || accesstype == SubstAccessType.Create)
                {
                    SubstHistory hist = new SubstHistory(module.ToString(), slot, DateTime.Now, accesstype);

                    if (_locationWafers[module][slot].SubstHists == null)
                        _locationWafers[module][slot].SubstHists = new SubstHistory[] { hist };

                    else
                    {
                        if (_locationWafers[module][slot].SubstHists.Length < 50)
                            _locationWafers[module][slot].SubstHists = _locationWafers[module][slot].SubstHists.Concat(new SubstHistory[] { hist }).ToArray();
                    }
                }



            }
            //Serialize();
            _needSerialize = true;
        }


        public void UpdateWaferLotId(ModuleName module, int slot, string lotId)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferLotId, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].LotId = lotId;
                WaferDataRecorder.SetWaferLotId(_locationWafers[module][slot].InnerId.ToString(), lotId);
                CarrierManager.Instance.UpdateCarrierLot(module.ToString(), lotId);
            }
            //Serialize();
            _needSerialize = true;
        }
        public void UpdateWaferHostLM1(ModuleName module, int slot, string lasermark1)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferHostLM1, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].HostLaserMark1 = lasermark1;
            }
            //Serialize();
            _needSerialize = true;
        }
        public void UpdateWaferHostLM2(ModuleName module, int slot, string lasermark2)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferHostLM2, invalid parameter, {0},{1}", module, slot + 1));
                return;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].HostLaserMark2 = lasermark2;
            }
            //Serialize();
            _needSerialize = true;
        }
        public void UpdateWaferProcessStatus(string waferID, EnumWaferProcessStatus status)
        {
            WaferInfo[] wafers = GetWafer(waferID);

            lock (_lockerWaferList)
            {
                foreach (var waferInfo in wafers)
                {
                    waferInfo.ProcessState = status;
                }
            }
            //Serialize();
            _needSerialize = true;
        }
        public void UpdateWaferProcess(string waferID, string processId)
        {
            WaferInfo[] wafers = GetWafer(waferID);

            lock (_lockerWaferList)
            {
                foreach (var waferInfo in wafers)
                {
                    WaferDataRecorder.SetProcessInfo(waferInfo.InnerId.ToString(), processId);
                }
            }
            //Serialize();
            _needSerialize = true;
        }

        public void UpdateWaferProcessID(string wafer_guid, string process_guid)
        {

            WaferDataRecorder.SetProcessInfo(wafer_guid, process_guid);


        }
        public WaferInfo CopyWaferInfo(ModuleName module, int slot, WaferInfo wafer)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed CopyWaferInfo, invalid parameter, {0},{1}", module, slot + 1));
                return null;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].Update(wafer);

                _locationWafers[module][slot].Station = (int)module;
                _locationWafers[module][slot].Slot = slot;
            }

            return _locationWafers[module][slot];
        }
        public bool CheckWaferSize(ModuleName module, int slot, WaferSize size)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferProcessStatus, invalid parameter, {0},{1}", module, slot + 1));
                return false;
            }

            WaferSize oldSize;
            lock (_lockerWaferList)
            {
                oldSize = _locationWafers[module][slot].Size;

                if (oldSize == WaferSize.WS0)
                {
                    _locationWafers[module][slot].Size = size;
                }
            }

            if (oldSize == WaferSize.WS0)
            {
                //Serialize();
                _needSerialize = true;
                return true;
            }

            return oldSize == size;
        }

        public bool UpdateWaferSize(ModuleName module, int slot, WaferSize size)
        {
            if (!IsWaferSlotLocationValid(module, slot))
            {
                LOG.Write(string.Format("Failed UpdateWaferSize, invalid parameter, {0},{1}", module, slot + 1));
                return false;
            }

            lock (_lockerWaferList)
            {
                _locationWafers[module][slot].Size = size;
            }

            _needSerialize = true;

            Serialize();
            return true;
        }

        private string GenerateWaferId(ModuleName module, int slot, string carrierID)
        {
            string carrierinfor = "";
            //5 + 2(unit) + 2(slot) + time(18) + index{5}     
            if (string.IsNullOrEmpty(carrierID))
                carrierinfor = module.ToString() + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            else carrierinfor = carrierID;

            return string.Format($"{carrierinfor}.{(slot + 1).ToString("00")}");
        }
        private string GenerateOrigin(ModuleName module, int slot)
        {
            return string.Format("{0}.{1:D2}", ModuleHelper.GetAbbr(module), slot + 1);
        }

        public void ClearBackupWafers()
        {
            _backupLocationWafers.Clear();
        }

        public void ClearBackupWafersByVCE(ModuleName vce)
        {
            foreach (var backupLocationWafer in _backupLocationWafers)
            {
                foreach (var backWafer in backupLocationWafer.Value)
                {
                    if (!backWafer.Value.IsEmpty)
                    {
                        if ((ModuleName)backWafer.Value.OriginStation == vce)
                        {
                            _backupLocationWafers[backupLocationWafer.Key][backWafer.Key].SetEmpty();
                        }
                    }
                }
            }
        }

        public void BackupLocationWafers()
        {
            foreach (var locationWafer in _locationWafers)
            {
                Dictionary<int, WaferInfo> slotWafer = new Dictionary<int, WaferInfo>();

                foreach (var item in locationWafer.Value)
                {
                    WaferInfo wafer = new WaferInfo();
                    wafer.Status = item.Value.Status;
                    wafer.WaferOrigin = item.Value.WaferOrigin;
                    wafer.OriginStation = item.Value.OriginStation;
                    wafer.OriginSlot = item.Value.OriginSlot;

                    slotWafer[item.Key] = wafer;
                }

                _backupLocationWafers[locationWafer.Key] = slotWafer;
            }
        }

        public bool CheckLocationWafers(out string result)
        {
            bool positionCorrect = true;
            result = string.Empty;

            foreach (var backupLocationWafer in _backupLocationWafers)
            {
                foreach (var backWafer in backupLocationWafer.Value)
                {
                    if (!backWafer.Value.IsEmpty && backWafer.Value.ProcessJob != null)
                    {
                        WaferInfo wafer = GetWafer(backupLocationWafer.Key, backWafer.Key);

                        if (!wafer.IsEmpty && wafer.OriginStation == backWafer.Value.OriginStation && wafer.OriginSlot == backWafer.Value.OriginSlot) // Wafer位置不变                       
                        {
                            continue;
                        }
                        else if (GetWafersByOriginalPosition(backWafer.Value.OriginStation, backWafer.Value.OriginSlot).Length > 0) // Wafer位置变化
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += "\n";

                            result += $"{backWafer.Value.WaferOrigin} should be at {backupLocationWafer.Key}, Slot {backWafer.Key + 1}";

                            positionCorrect = false;
                        }
                        else // Wafer被删除
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += "\n";

                            result += $"{backWafer.Value.WaferOrigin} has been deleted";
                        }
                    }
                }
            }

            return positionCorrect;
        }
    }
}
