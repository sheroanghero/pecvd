using Aitex.Core.RT.Device;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData.SorterDefines;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Event;

namespace MECF.Framework.Common.SubstrateTrackings
{
    public class CarrierConfigurationManager : Singleton<CarrierConfigurationManager>
    {
        public CarrierConfigurationManager()
        {
            
        }

        private CarrierConfigurationItem[] _lstCarriers
        {
            get
            {
                List<CarrierConfigurationItem> lstitems = new List<CarrierConfigurationItem>();
                for (int i = 0; i < _carrierCount; i++)
                {
                    var item = GetCarrierConfigItem(i);
                    lstitems.Add(item);
                }
                return lstitems.ToArray();
            }
        }
        private int _carrierCount;

        public void Initialize(int count=16)
        {
            _carrierCount = count;

            DATA.Subscribe("CarrierConfigurationList", () => _lstCarriers);
            OP.Subscribe($"SetCarrierConfigurationItem", (string cmd, object[] param) =>
            {

                CarrierConfigurationItem item = new CarrierConfigurationItem()
                {
                    Index = (int)param[0],
                    CarrierName = (string)param[1],
                    CarrierWaferSize = (int)param[2],
                    CarrierSlotsNumber = (int)param[3],
                    IsInfoPadAOn = (bool)param[4],
                    IsInfoPadBOn = (bool)param[5],
                    IsInfoPadCOn = (bool)param[6],
                    IsInfoPadDOn = (bool)param[7],
                    LP1StationName = (string)param[8],
                    LP2StationName = (string)param[9],
                    LP3StationName = (string)param[10],
                    LP4StationName = (string)param[11],
                    LP5StationName = (string)param[12],
                    LP6StationName = (string)param[13],
                    LP7StationName = (string)param[14],
                    LP8StationName = (string)param[15],
                    GetOffset = (string)param[16],
                    PutOffset = (string)param[17],
                    CIDReaderIndex = (int)param[18],
                    CarrierFosbMode = (bool)param[19],
                    NeedCheckIronDoorCarrier = (bool)param[20],
                    KeepClampedAfterUnloadCarrier = (bool)param[21],
                    DisableEvenSlot = (bool)param[22],
                    DisableOddSlot = (bool)param[23],

                    ThicknessLowLimit = (int)param[24],
                    ThicknessHighLimit = (int)param[25],
                    SlotPositionBaseLine = (int)param[26],
                    SlotPitch = (int)param[27],
                    WaferCenterDeviationLimit = (int)param[28],
                    EnableDualTransfer = (bool)param[29],
                    ForbidAccessAboveWafer = param.Length > 30 ? (bool)param[30] : true,
                    MappedByRobot = param.Length > 31 ? (bool)param[31] : false,
                    EnableCarrier = param.Length > 32 ? (bool)param[32] : true,
                    LPRecipeNumber = param.Length > 33 ? (int)param[33] : 0,
                 
                    AccessPermitToCarrierIndex = param.Length > 34 ? param[34].ToString():"",


            }
                ;                
                SetCarrierConfigItem(item);
                EV.PostInfoLog("System", $"Complete set carrier configuration item");
                return true;
            });

        }
        public CarrierConfigurationItem GetCarrierConfigItem(int index)
        {
            CarrierConfigurationItem item = new CarrierConfigurationItem()
            {
                Index = index,
                IsInfoPadAOn = (index & 8) == 8,
                IsInfoPadBOn = (index & 4) == 4,
                IsInfoPadCOn = (index & 2) == 2,
                IsInfoPadDOn = (index & 1) == 1,
            };




            if(SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierName"))
                item.CarrierName = SC.GetStringValue($"CarrierInfo.Carrier{index}.CarrierName");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.GetOffset"))
                item.GetOffset = SC.GetStringValue($"CarrierInfo.Carrier{index}.GetOffset");

            if(SC.ContainsItem($"CarrierInfo.Carrier{index}.PutOffset"))
                item.PutOffset = SC.GetStringValue($"CarrierInfo.Carrier{index}.PutOffset");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CIDReaderIndex"))
                item.CIDReaderIndex = SC.GetValue<int>($"CarrierInfo.Carrier{index}.CIDReaderIndex");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierFosbMode"))
                item.CarrierFosbMode = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.CarrierFosbMode");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierWaferSize"))
                item.CarrierWaferSize = SC.GetValue<int>($"CarrierInfo.Carrier{index}.CarrierWaferSize");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.NeedCheckIronDoorCarrier"))
                item.NeedCheckIronDoorCarrier = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.NeedCheckIronDoorCarrier");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.KeepClampedAfterUnloadCarrier"))
                item.KeepClampedAfterUnloadCarrier = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.KeepClampedAfterUnloadCarrier");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.DisableEvenSlot"))
                item.DisableEvenSlot = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.DisableEvenSlot");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.DisableOddSlot"))
                item.DisableOddSlot = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.DisableOddSlot");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierSlotsNumber"))
                item.CarrierSlotsNumber = SC.GetValue<int>($"CarrierInfo.Carrier{index}.CarrierSlotsNumber");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.ThicknessLowLimit"))
                item.ThicknessLowLimit = SC.GetValue<int>($"CarrierInfo.Carrier{index}.ThicknessLowLimit");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.ThicknessHighLimit"))
                item.ThicknessHighLimit = SC.GetValue<int>($"CarrierInfo.Carrier{index}.ThicknessHighLimit");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.SlotPositionBaseLine"))
                item.SlotPositionBaseLine = SC.GetValue<int>($"CarrierInfo.Carrier{index}.SlotPositionBaseLine");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.SlotPitch"))
                item.SlotPitch = SC.GetValue<int>($"CarrierInfo.Carrier{index}.SlotPitch");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.WaferCenterDeviationLimit"))
                item.WaferCenterDeviationLimit = SC.GetValue<int>($"CarrierInfo.Carrier{index}.WaferCenterDeviationLimit");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.EnableDualTransfer"))
                item.EnableDualTransfer = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.EnableDualTransfer");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.ForbidAccessAboveWafer"))
                item.ForbidAccessAboveWafer = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.ForbidAccessAboveWafer");



            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP1Station"))
                item.LP1StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP1Station");
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP2Station"))
                item.LP2StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP2Station");
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP3Station"))
                item.LP3StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP3Station");
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP4Station"))
                item.LP4StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP4Station");
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP5Station"))
                item.LP5StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP5Station");
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP6Station"))
                item.LP6StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP6Station");
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP7Station"))
                item.LP7StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP7Station");
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP8Station"))
                item.LP8StationName = SC.GetStringValue($"CarrierInfo.Carrier{index}.LP8Station");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.MappedByRobot"))
                item.MappedByRobot = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.MappedByRobot");

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.EnableCarrier"))
                item.EnableCarrier = SC.GetValue<bool>($"CarrierInfo.Carrier{index}.EnableCarrier");
            else
                item.EnableCarrier = true;

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LPRecipeNumber"))
                item.LPRecipeNumber = SC.GetValue<int>($"CarrierInfo.Carrier{index}.LPRecipeNumber");
            
            List<int> accessP = new List<int>();
            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.AccessPermitToCarrierIndex"))
            {
                item.AccessPermitToCarrierIndex = SC.GetStringValue($"CarrierInfo.Carrier{index}.AccessPermitToCarrierIndex");
                

                foreach(var strIndex in item.AccessPermitToCarrierIndex.Split(','))
                {
                    if(int.TryParse(strIndex,out int nIndex))
                    {
                        if(!accessP.Contains(nIndex))
                            accessP.Add(nIndex);
                    }
                }
                item.Int_AccessPermitToCarrierIndex = accessP.ToArray();
            }
            else
            {
                item.AccessPermitToCarrierIndex = null;   //Enable All
                for(int i=0;i<100;i++)
                {
                    accessP.Add(i);
                }
                item.Int_AccessPermitToCarrierIndex = accessP.ToArray();
            }
                




            return item;

        }
        public bool SetCarrierConfigItem(CarrierConfigurationItem item)
        {
            int index = item.Index;

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierName"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.CarrierName",item.CarrierName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.GetOffset"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.GetOffset",item.GetOffset);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.PutOffset"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.PutOffset",item.PutOffset);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CIDReaderIndex"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.CIDReaderIndex",item.CIDReaderIndex);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierFosbMode"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.CarrierFosbMode",item.CarrierFosbMode);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierWaferSize"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.CarrierWaferSize",(int)item.CarrierWaferSize);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.NeedCheckIronDoorCarrier"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.NeedCheckIronDoorCarrier",item.NeedCheckIronDoorCarrier);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.KeepClampedAfterUnloadCarrier"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.KeepClampedAfterUnloadCarrier",item.KeepClampedAfterUnloadCarrier);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.DisableEvenSlot"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.DisableEvenSlot",item.DisableEvenSlot);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.DisableOddSlot"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.DisableOddSlot",item.DisableOddSlot);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.CarrierSlotsNumber"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.CarrierSlotsNumber",item.CarrierSlotsNumber);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.ThicknessLowLimit"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.ThicknessLowLimit",item.ThicknessLowLimit);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.ThicknessHighLimit"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.ThicknessHighLimit",item.ThicknessHighLimit);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.SlotPositionBaseLine"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.SlotPositionBaseLine", item.SlotPositionBaseLine) ;

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.SlotPitch"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.SlotPitch",item.SlotPitch);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.WaferCenterDeviationLimit"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.WaferCenterDeviationLimit",item.WaferCenterDeviationLimit);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP1Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP1Station",item.LP1StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP2Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP2Station", item.LP2StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP3Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP3Station", item.LP3StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP4Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP4Station", item.LP4StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP5Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP5Station", item.LP5StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP6Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP6Station", item.LP6StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP7Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP7Station", item.LP7StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LP8Station"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LP8Station", item.LP8StationName);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.EnableDualTransfer"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.EnableDualTransfer", item.EnableDualTransfer);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.ForbidAccessAboveWafer"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.ForbidAccessAboveWafer", item.ForbidAccessAboveWafer);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.MappedByRobot"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.MappedByRobot", item.MappedByRobot);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.EnableCarrier"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.EnableCarrier", item.EnableCarrier);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.LPRecipeNumber"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.LPRecipeNumber", item.LPRecipeNumber);

            if (SC.ContainsItem($"CarrierInfo.Carrier{index}.AccessPermitToCarrierIndex"))
                SC.SetItemValue($"CarrierInfo.Carrier{index}.AccessPermitToCarrierIndex", item.AccessPermitToCarrierIndex);



            return true;

        }






    }





}
