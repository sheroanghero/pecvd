using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Sorter.RT.Module.DBRecorder;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MECF.Framework.Common.SubstrateTrackings
{
    public class CarrierManager : Singleton<CarrierManager>
    {
        Dictionary<string, Dictionary<int, CarrierInfo>> _locationCarriers = new Dictionary<string, Dictionary<int, CarrierInfo>>();
        object _lockerCarrierList = new object();

        private const string EventCarrierMoveIn = "CarrierMoveIn";
        private const string EventCarrierMoveOut = "CarrierMoveOut";

        private const string DvidCarrierId = "CarrierId";
        private const string DvidStation = "Station";

        public void Initialize()
        {
            EV.Subscribe(new EventItem("Event", EventCarrierMoveIn, "carrier move in"));
            EV.Subscribe(new EventItem("Event", EventCarrierMoveOut, "carrier move out"));

            Deserialize();
        }

        public void Terminate()
        {
            lock (_lockerCarrierList)
            {
                Serialize();
            }
        }

        public void Deserialize()
        {
            lock (_lockerCarrierList)
            {
                var ccc = BinarySerializer<Dictionary<string, Dictionary<int, CarrierInfo>>>.FromStream("CarrierManager");
                if (ccc != null)
                    _locationCarriers = ccc;
            }
        }

        public void Serialize()
        {
            lock (_lockerCarrierList)
            {
                if (_locationCarriers != null)
                {
                    BinarySerializer<Dictionary<string, Dictionary<int, CarrierInfo>>>.ToStream(_locationCarriers, "CarrierManager");
                }
            }
        }

        /// <summary>
        /// 由各个模块自己定义注册可以放置几个载盒
        /// </summary>
        /// <param name="module">模块名称</param>
        /// <param name="capacity">有几个位置可以放</param>
        public void SubscribeLocation(string module, int capacity = 1)
        {
            lock (_lockerCarrierList)
            {
                if (!_locationCarriers.ContainsKey(module))
                {
                    _locationCarriers[module] = new Dictionary<int, CarrierInfo>();
                    for (int i = 0; i < capacity; i++)
                    {
                        _locationCarriers[module][i] = new CarrierInfo(25);
                    }
                }

                DATA.Subscribe(module, "Carrier", () => _locationCarriers[module][0]);

                DATA.Subscribe(module.ToString(), "ModuleCarrierList", () => _locationCarriers[module].Values.ToArray());
            }
        }


        public void CreateCarrier(string module)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].Status = CarrierStatus.Normal;
            _locationCarriers[module][0].InnerId = Guid.NewGuid();

            _locationCarriers[module][0].Name = $"Cassette{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            _locationCarriers[module][0].LoadTime = DateTime.Now;

            CarrierDataRecorder.Loaded(_locationCarriers[module][0].InnerId.ToString(), module);

            Serialize();
        }
        public void CreateCarrier(string module, ModuleName intCarmodulename, WaferSize size, int slotNumber)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].Status = CarrierStatus.Normal;
            _locationCarriers[module][0].InnerId = Guid.NewGuid();

            _locationCarriers[module][0].Name = $"Cassette{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            _locationCarriers[module][0].LoadTime = DateTime.Now;
            _locationCarriers[module][0].InternalModuleName = intCarmodulename;
            _locationCarriers[module][0].CarrierWaferSize = size;
            if (!WaferManager.Instance.AllLocationWafers.ContainsKey(intCarmodulename))
                WaferManager.Instance.SubscribeLocation(intCarmodulename, slotNumber);

            CarrierDataRecorder.Loaded(_locationCarriers[module][0].InnerId.ToString(), module);

            Serialize();
        }

        public void DeleteCarrier(string module)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            CarrierDataRecorder.Unloaded(_locationCarriers[module][0].InnerId.ToString());

            _locationCarriers[module][0].Clear();

            Serialize();
        }
        public void DeleteInternalCarrier(string module)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;


            CarrierDataRecorder.Unloaded(_locationCarriers[module][0].InnerId.ToString());

            _locationCarriers[module][0].Clear();

            Serialize();
        }


        public void RegisterCarrierWafer(string module, int index, WaferInfo wafer)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].Wafers[index] = wafer;
        }

        public void UnregisterCarrierWafer(string module, int index)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].Wafers[index] = null;
        }

        public CarrierInfo GetCarrier(string module)
        {
            return GetCarrier(module, 0);
        }

        public CarrierInfo GetCarrier(ModuleName module)
        {
            return GetCarrier(module.ToString(), 0);
        }

        public CarrierInfo GetCarrier(ModuleName module, int slot)
        {
            return GetCarrier(module.ToString(), slot);
        }

        public CarrierInfo GetCarrier(string module, int slot)
        {
            if (!_locationCarriers.ContainsKey(module))
                return null;

            return _locationCarriers[module][slot];
        }

        public CarrierInfo[] GetCarriers(string module)
        {
            if (!_locationCarriers.ContainsKey(module))
                return null;

            return _locationCarriers[module].Values.ToArray();
        }

        public string GetLocationByCarrierId(string carrierId)
        {
            foreach (var locationCarrier in _locationCarriers)
            {
                foreach (var carrierInfo in locationCarrier.Value)
                {
                    if (!carrierInfo.Value.IsEmpty && carrierInfo.Value.CarrierId == carrierId)
                        return locationCarrier.Key;
                }
            }

            return null;
        }

        public string GetLocationByInternalCarrierModuleName(ModuleName name)
        {
            foreach (var locationCarrier in _locationCarriers)
            {
                foreach (var carrierInfo in locationCarrier.Value)
                {
                    if (!carrierInfo.Value.IsEmpty && carrierInfo.Value.InternalModuleName == name)
                        return locationCarrier.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// 传入LPn, 返回该LP的LotId
        /// </summary>
        /// <param name="moduleName">LP1/2/n</param>
        /// <returns>LotId string</returns>
        public string GetLotIdByLoadPort(string moduleName)
        {
            return _locationCarriers.ContainsKey(moduleName) ? _locationCarriers[moduleName][0].LotId : "";
        }

        public bool HasCarrier(string module, int slot = 0)
        {
            if (!_locationCarriers.ContainsKey(module))
                return false;

            return !_locationCarriers[module][0].IsEmpty;
        }

        public bool CheckHasCarrier(string module, int slot)
        {
            if (!_locationCarriers.ContainsKey(module))
                return false;

            return !_locationCarriers[module][0].IsEmpty;
        }

        public bool CheckHasCarrier(ModuleName module, int slot)
        {
            return CheckHasCarrier(module.ToString(), 0);
        }

        public bool CheckNoCarrier(string module, int slot)
        {
            if (!_locationCarriers.ContainsKey(module))
                return false;

            return _locationCarriers[module][0].IsEmpty;
        }

        public bool CheckNoCarrier(ModuleName module, int slot)
        {
            return CheckNoCarrier(module.ToString(), slot);
        }

        public bool CheckNeedProcess(string module, int slot)
        {
            if (!_locationCarriers.ContainsKey(module))
                return false;

            if (_locationCarriers[module][0].IsEmpty)
                return false;


            if (!_locationCarriers[module][0].HasWaferIn)
                return false;

            return !_locationCarriers[module][0].IsProcessCompleted;
        }

        public bool CheckNeedProcess(ModuleName module, int slot)
        {
            return CheckNeedProcess(module.ToString(), slot);
        }

        public bool CheckHasWaferIn(string module, int slot)
        {
            if (!_locationCarriers.ContainsKey(module))
                return false;

            if (_locationCarriers[module][0].IsEmpty)
                return false;

            return _locationCarriers[module][0].HasWaferIn;
        }

        public bool CheckHasWaferIn(ModuleName module, int slot)
        {
            return CheckHasWaferIn(module.ToString(), slot);
        }

        public void CarrierMoved(string moduleFrom, string moduleTo)
        {
            if (_locationCarriers[moduleFrom][0].IsEmpty)
            {
                LOG.Write(string.Format("Invalid carrier move, no carrier at source, {0}{1}=>{2}{3}", moduleFrom, 0 + 1, moduleTo, 0 + 1));
                return;
            }

            if (!_locationCarriers[moduleTo][0].IsEmpty)
            {
                LOG.Write(string.Format("Invalid carrier move, destination has carrier, {0}{1}=>{2}{3}", moduleFrom, 0 + 1, moduleTo, 0 + 1));
                return;
            }

            _locationCarriers[moduleTo][0].CopyInfo(_locationCarriers[moduleFrom][0]);

            DeleteCarrier(moduleFrom);


            EV.Notify(EventCarrierMoveOut, new SerializableDictionary<string, string>()
                {
                    {DvidCarrierId, GetCarrier(moduleFrom).CarrierId},
                    { DvidStation, moduleFrom}
                });

            EV.Notify(EventCarrierMoveIn, new SerializableDictionary<string, string>()
                {
                    {DvidCarrierId, GetCarrier(moduleTo).CarrierId},
                    { DvidStation, moduleTo}
                });


            EV.PostInfoLog(ModuleName.System.ToString(), string.Format("Carrier moved from {0} to {1}", moduleFrom, moduleTo));

            //WaferMoveHistoryRecorder.WaferMoved(_locationCarriers[moduleTo][0].InnerId.ToString(), moduleTo.ToString(), 0, _locationCarriers[moduleTo][0].Status.ToString());

            Serialize();
        }


        public void UpdateCarrierId(string module, string carrierId)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            //if (!string.IsNullOrEmpty(_locationCarriers[module][0].CarrierId))
            //{
            //    DeleteCarrier(module);
            //    CreateCarrier(module);
            //}

            _locationCarriers[module][0].CarrierId = carrierId;

            CarrierDataRecorder.UpdateCarrierId(_locationCarriers[module][0].InnerId.ToString(), carrierId);
            Serialize();
        }

        public void UpdateRfId(string module, string rfid)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            if (!string.IsNullOrEmpty(_locationCarriers[module][0].CarrierId))
            {
                DeleteCarrier(module);
                CreateCarrier(module);
            }

            _locationCarriers[module][0].Rfid = rfid;

            Serialize();
        }

        public void UpdateCarrierLot(string module, string lotId)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].LotId = lotId;

            CarrierDataRecorder.UpdateLotId(_locationCarriers[module][0].InnerId.ToString(), lotId);
            Serialize();
        }

        public bool UpdateWaferSize(ModuleName module, int slot, WaferSize size)
        {


            // lock (_lockerWaferList)
            {
                _locationCarriers[module.ToString()][slot].CarrierWaferSize = size;
            }

            Serialize();
            return true;
        }

        public void UpdateProductCategory(string module, string productCategory)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].ProductCategory = productCategory;

            CarrierDataRecorder.UpdateProductCategory(_locationCarriers[module][0].InnerId.ToString(), productCategory);
            Serialize();
        }

        public void UpdateProcessJob(string module, object job)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].ProcessJob = job;
            Serialize();
        }

        public void UpdateProcessStatus(string module, bool processed)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].ProcessStatus[module] = processed;
            Serialize();
        }

        public void UpdateProcessCompleted(ModuleName module, int slot, bool processed)
        {
            UpdateProcessCompleted(module.ToString(), slot, processed);
        }

        public void UpdateProcessCompleted(string module, int slot, bool processed)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].IsProcessCompleted = processed;
            Serialize();
        }

        public void UpdateWaferIn(ModuleName module, int slot, bool waferIn)
        {
            UpdateWaferIn(module.ToString(), slot, waferIn);
        }

        public void UpdateWaferIn(string module, int slot, bool waferIn)
        {
            if (!_locationCarriers.ContainsKey(module))
                return;

            _locationCarriers[module][0].HasWaferIn = waferIn;
            Serialize();
        }
    }
}
