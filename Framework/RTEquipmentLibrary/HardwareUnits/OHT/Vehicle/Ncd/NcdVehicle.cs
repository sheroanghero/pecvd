using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Routine;
using Aitex.Core.Util;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    public class NcdVehicle: VehicleBaseDevice
    {
        public NcdVehicle(string module, XmlElement node, string ioModule = "") : base(node.GetAttribute("module"), node.GetAttribute("id"))
        {
            base.Module = node.GetAttribute("module");//string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");

            ioModule = node.GetAttribute("ioModule");

            _aiVehiclePLCState = ParseAiNode($"AI_VehiclePLCState", node, ioModule);
            _aiTargetPosition = ParseAiNode($"AI_TargetPosition", node, ioModule);
            _aiCurrentCoordinate_L = ParseAiNode($"AI_CurrentCoordinate_L", node, ioModule);
            _aiCurrentCoordinate_H = ParseAiNode($"AI_CurrentCoordinate_H", node, ioModule);
            _aiVehiclePLCIP = ParseAiNode($"AI_VehiclePLCIP", node, ioModule);
            _aiVehicleID0 = ParseAiNode($"AI_VehicleID0", node, ioModule);
            _aiVehicleID1 = ParseAiNode($"AI_VehicleID1", node, ioModule);
            _aiVehicleID2 = ParseAiNode($"AI_VehicleID2", node, ioModule);
            _aiVehicleID3 = ParseAiNode($"AI_VehicleID3", node, ioModule);
            _aiCurrentCommand = ParseAiNode($"AI_CurrentCommand", node, ioModule);
            _aiCurrentCommandStatus = ParseAiNode($"AI_CurrentCommandStatus", node, ioModule);
            _aiErrorCode = ParseAiNode($"AI_ErrorCode", node, ioModule);
            _aiDistanceToFrontVehicle = ParseAiNode($"AI_DistanceToFrontVehicle", node, ioModule);
            _aiRFIDChar0 = ParseAiNode($"AI_RFIDChar0", node, ioModule);
            _aiRFIDChar1 = ParseAiNode($"AI_RFIDChar1", node, ioModule);
            _aiRFIDChar2 = ParseAiNode($"AI_RFIDChar2", node, ioModule);
            _aiRFIDChar3 = ParseAiNode($"AI_RFIDChar3", node, ioModule);
            _aiRFIDChar4 = ParseAiNode($"AI_RFIDChar4", node, ioModule);
            _aiRFIDChar5 = ParseAiNode($"AI_RFIDChar5", node, ioModule);
            _aiRFIDChar6 = ParseAiNode($"AI_RFIDChar6", node, ioModule);
            _aiRFIDChar7 = ParseAiNode($"AI_RFIDChar7", node, ioModule);
            _aiCurrentPosition = ParseAiNode($"AI_CurrentPosition", node, ioModule);



            _aoVehicleCommand = ParseAoNode($"AO_VehicleCommand", node, ioModule);
            _aoTargetPosition = ParseAoNode($"AO_TargetPosition", node, ioModule);
            _aoViaCoordinate1_L = ParseAoNode($"AO_ViaCoordinate1_L", node, ioModule);
            _aoViaCoordinate1_H = ParseAoNode($"AO_ViaCoordinate1_H", node, ioModule);
            _aoViaCoordinate2_L = ParseAoNode($"AO_ViaCoordinate2_L", node, ioModule);
            _aoViaCoordinate2_H = ParseAoNode($"AO_ViaCoordinate2_H", node, ioModule);
            _aoViaCoordinate3_L = ParseAoNode($"AO_ViaCoordinate3_L", node, ioModule);
            _aoViaCoordinate3_H = ParseAoNode($"AO_ViaCoordinate3_H", node, ioModule);
            _aoViaCoordinate4_L = ParseAoNode($"AO_ViaCoordinate4_L", node, ioModule);
            _aoViaCoordinate4_H = ParseAoNode($"AO_ViaCoordinate4_H", node, ioModule);
            _aoViaCoordinate5_L = ParseAoNode($"AO_ViaCoordinate5_L", node, ioModule);
            _aoViaCoordinate5_H = ParseAoNode($"AO_ViaCoordinate5_H", node, ioModule);
            _aoViaCoordinate6_L = ParseAoNode($"AO_ViaCoordinate6_L", node, ioModule);
            _aoViaCoordinate6_H = ParseAoNode($"AO_ViaCoordinate6_H", node, ioModule);
            _aoViaCoordinate7_L = ParseAoNode($"AO_ViaCoordinate7_L", node, ioModule);
            _aoViaCoordinate7_H = ParseAoNode($"AO_ViaCoordinate7_H", node, ioModule);
            _aoViaCoordinate8_L = ParseAoNode($"AO_ViaCoordinate8_L", node, ioModule);
            _aoViaCoordinate8_H = ParseAoNode($"AO_ViaCoordinate8_H", node, ioModule);
            _aoViaCoordinate9_L = ParseAoNode($"AO_ViaCoordinate9_L", node, ioModule);
            _aoViaCoordinate9_H = ParseAoNode($"AO_ViaCoordinate9_H", node, ioModule);
            _aoViaCoordinate10_L = ParseAoNode($"AO_ViaCoordinate10_L", node, ioModule);
            _aoViaCoordinate10_H = ParseAoNode($"AO_ViaCoordinate10_H", node, ioModule);
            _aoViaCoordinate11_L = ParseAoNode($"AO_ViaCoordinate11_L", node, ioModule);
            _aoViaCoordinate11_H = ParseAoNode($"AO_ViaCoordinate11_H", node, ioModule);
            _aoViaCoordinate12_L = ParseAoNode($"AO_ViaCoordinate12_L", node, ioModule);
            _aoViaCoordinate12_H = ParseAoNode($"AO_ViaCoordinate12_H", node, ioModule);
            _aoViaCoordinate13_L = ParseAoNode($"AO_ViaCoordinate13_L", node, ioModule);
            _aoViaCoordinate13_H = ParseAoNode($"AO_ViaCoordinate13_H", node, ioModule);
            _aoViaCoordinate14_L = ParseAoNode($"AO_ViaCoordinate14_L", node, ioModule);
            _aoViaCoordinate14_H = ParseAoNode($"AO_ViaCoordinate14_H", node, ioModule);
            _aoViaCoordinate15_L = ParseAoNode($"AO_ViaCoordinate15_L", node, ioModule);
            _aoViaCoordinate15_H = ParseAoNode($"AO_ViaCoordinate15_H", node, ioModule);
            _aoViaCoordinate16_L = ParseAoNode($"AO_ViaCoordinate16_L", node, ioModule);
            _aoViaCoordinate16_H = ParseAoNode($"AO_ViaCoordinate16_H", node, ioModule);
            _aoViaCoordinate17_L = ParseAoNode($"AO_ViaCoordinate17_L", node, ioModule);
            _aoViaCoordinate17_H = ParseAoNode($"AO_ViaCoordinate17_H", node, ioModule);
            _aoViaCoordinate18_L = ParseAoNode($"AO_ViaCoordinate18_L", node, ioModule);
            _aoViaCoordinate18_H = ParseAoNode($"AO_ViaCoordinate18_H", node, ioModule);
            _aoViaCoordinate19_L = ParseAoNode($"AO_ViaCoordinate19_L", node, ioModule);
            _aoViaCoordinate19_H = ParseAoNode($"AO_ViaCoordinate19_H", node, ioModule);
            _aoViaCoordinate20_L = ParseAoNode($"AO_ViaCoordinate20_L", node, ioModule);
            _aoViaCoordinate20_H = ParseAoNode($"AO_ViaCoordinate20_H", node, ioModule);
            _aoViaCoordinate21_L = ParseAoNode($"AO_ViaCoordinate21_L", node, ioModule);
            _aoViaCoordinate21_H = ParseAoNode($"AO_ViaCoordinate21_H", node, ioModule);
            _aoViaCoordinate22_L = ParseAoNode($"AO_ViaCoordinate22_L", node, ioModule);
            _aoViaCoordinate22_H = ParseAoNode($"AO_ViaCoordinate22_H", node, ioModule);
            _aoViaCoordinate23_L = ParseAoNode($"AO_ViaCoordinate23_L", node, ioModule);
            _aoViaCoordinate23_H = ParseAoNode($"AO_ViaCoordinate23_H", node, ioModule);
            _aoViaCoordinate24_L = ParseAoNode($"AO_ViaCoordinate24_L", node, ioModule);
            _aoViaCoordinate24_H = ParseAoNode($"AO_ViaCoordinate24_H", node, ioModule);
            _aoViaCoordinate25_L = ParseAoNode($"AO_ViaCoordinate25_L", node, ioModule);
            _aoViaCoordinate25_H = ParseAoNode($"AO_ViaCoordinate25_H", node, ioModule);
            _aoViaCoordinate26_L = ParseAoNode($"AO_ViaCoordinate26_L", node, ioModule);
            _aoViaCoordinate26_H = ParseAoNode($"AO_ViaCoordinate26_H", node, ioModule);
            _aoViaCoordinate27_L = ParseAoNode($"AO_ViaCoordinate27_L", node, ioModule);
            _aoViaCoordinate27_H = ParseAoNode($"AO_ViaCoordinate27_H", node, ioModule);
            _aoViaCoordinate28_L = ParseAoNode($"AO_ViaCoordinate28_L", node, ioModule);
            _aoViaCoordinate28_H = ParseAoNode($"AO_ViaCoordinate28_H", node, ioModule);
            _aoViaCoordinate29_L = ParseAoNode($"AO_ViaCoordinate29_L", node, ioModule);
            _aoViaCoordinate29_H = ParseAoNode($"AO_ViaCoordinate29_H", node, ioModule);
            _aoViaCoordinate30_L = ParseAoNode($"AO_ViaCoordinate30_L", node, ioModule);
            _aoViaCoordinate30_H = ParseAoNode($"AO_ViaCoordinate30_H", node, ioModule);
            _aoViaCoordinate31_L = ParseAoNode($"AO_ViaCoordinate31_L", node, ioModule);
            _aoViaCoordinate31_H = ParseAoNode($"AO_ViaCoordinate31_H", node, ioModule);
            _aoViaCoordinate32_L = ParseAoNode($"AO_ViaCoordinate32_L", node, ioModule);
            _aoViaCoordinate32_H = ParseAoNode($"AO_ViaCoordinate32_H", node, ioModule);
            _aoViaCoordinate33_L = ParseAoNode($"AO_ViaCoordinate33_L", node, ioModule);
            _aoViaCoordinate33_H = ParseAoNode($"AO_ViaCoordinate33_H", node, ioModule);
            _aoViaCoordinate34_L = ParseAoNode($"AO_ViaCoordinate34_L", node, ioModule);
            _aoViaCoordinate34_H = ParseAoNode($"AO_ViaCoordinate34_H", node, ioModule);
            _aoViaCoordinate35_L = ParseAoNode($"AO_ViaCoordinate35_L", node, ioModule);
            _aoViaCoordinate35_H = ParseAoNode($"AO_ViaCoordinate35_H", node, ioModule);
            _aoViaCoordinate36_L = ParseAoNode($"AO_ViaCoordinate36_L", node, ioModule);
            _aoViaCoordinate36_H = ParseAoNode($"AO_ViaCoordinate36_H", node, ioModule);
            _aoViaCoordinate37_L = ParseAoNode($"AO_ViaCoordinate37_L", node, ioModule);
            _aoViaCoordinate37_H = ParseAoNode($"AO_ViaCoordinate37_H", node, ioModule);
            _aoViaCoordinate38_L = ParseAoNode($"AO_ViaCoordinate38_L", node, ioModule);
            _aoViaCoordinate38_H = ParseAoNode($"AO_ViaCoordinate38_H", node, ioModule);
            _aoViaCoordinate39_L = ParseAoNode($"AO_ViaCoordinate39_L", node, ioModule);
            _aoViaCoordinate39_H = ParseAoNode($"AO_ViaCoordinate39_H", node, ioModule);
            _aoViaCoordinate40_L = ParseAoNode($"AO_ViaCoordinate40_L", node, ioModule);
            _aoViaCoordinate40_H = ParseAoNode($"AO_ViaCoordinate40_H", node, ioModule);
            _aoViaCoordinate41_L = ParseAoNode($"AO_ViaCoordinate41_L", node, ioModule);
            _aoViaCoordinate41_H = ParseAoNode($"AO_ViaCoordinate41_H", node, ioModule);
            _aoViaCoordinate42_L = ParseAoNode($"AO_ViaCoordinate42_L", node, ioModule);
            _aoViaCoordinate42_H = ParseAoNode($"AO_ViaCoordinate42_H", node, ioModule);
            _aoViaCoordinate43_L = ParseAoNode($"AO_ViaCoordinate43_L", node, ioModule);
            _aoViaCoordinate43_H = ParseAoNode($"AO_ViaCoordinate43_H", node, ioModule);
            _aoViaCoordinate44_L = ParseAoNode($"AO_ViaCoordinate44_L", node, ioModule);
            _aoViaCoordinate44_H = ParseAoNode($"AO_ViaCoordinate44_H", node, ioModule);
            _aoViaCoordinate45_L = ParseAoNode($"AO_ViaCoordinate45_L", node, ioModule);
            _aoViaCoordinate45_H = ParseAoNode($"AO_ViaCoordinate45_H", node, ioModule);
            _aoViaCoordinate46_L = ParseAoNode($"AO_ViaCoordinate46_L", node, ioModule);
            _aoViaCoordinate46_H = ParseAoNode($"AO_ViaCoordinate46_H", node, ioModule);
            _aoViaCoordinate47_L = ParseAoNode($"AO_ViaCoordinate47_L", node, ioModule);
            _aoViaCoordinate47_H = ParseAoNode($"AO_ViaCoordinate47_H", node, ioModule);
            _aoViaCoordinate48_L = ParseAoNode($"AO_ViaCoordinate48_L", node, ioModule);
            _aoViaCoordinate48_H = ParseAoNode($"AO_ViaCoordinate48_H", node, ioModule);
            _aoViaCoordinate49_L = ParseAoNode($"AO_ViaCoordinate49_L", node, ioModule);
            _aoViaCoordinate49_H = ParseAoNode($"AO_ViaCoordinate49_H", node, ioModule);
            _aoViaCoordinate50_L = ParseAoNode($"AO_ViaCoordinate50_L", node, ioModule);
            _aoViaCoordinate50_H = ParseAoNode($"AO_ViaCoordinate50_H", node, ioModule);




            _aoViaCoordinates = new AOAccessor[]
            {
                _aoViaCoordinate1_L,
                _aoViaCoordinate1_H,
                _aoViaCoordinate2_L,
                _aoViaCoordinate2_H,
                _aoViaCoordinate3_L,
                _aoViaCoordinate3_H,
                _aoViaCoordinate4_L,
                _aoViaCoordinate4_H,
                _aoViaCoordinate5_L,
                _aoViaCoordinate5_H,
                _aoViaCoordinate6_L,
                _aoViaCoordinate6_H,
                _aoViaCoordinate7_L,
                _aoViaCoordinate7_H,
                _aoViaCoordinate8_L,
                _aoViaCoordinate8_H,
                _aoViaCoordinate9_L,
                _aoViaCoordinate9_H,
                _aoViaCoordinate10_L,
                _aoViaCoordinate10_H,
                _aoViaCoordinate11_L,
                _aoViaCoordinate11_H,
                _aoViaCoordinate12_L,
                _aoViaCoordinate12_H,
                _aoViaCoordinate13_L,
                _aoViaCoordinate13_H,
                _aoViaCoordinate14_L,
                _aoViaCoordinate14_H,
                _aoViaCoordinate15_L,
                _aoViaCoordinate15_H,
                _aoViaCoordinate16_L,
                _aoViaCoordinate16_H,
                _aoViaCoordinate17_L,
                _aoViaCoordinate17_H,
                _aoViaCoordinate18_L,
                _aoViaCoordinate18_H,
                _aoViaCoordinate19_L,
                _aoViaCoordinate19_H,
                _aoViaCoordinate20_L,
                _aoViaCoordinate20_H,
                _aoViaCoordinate21_L,
                _aoViaCoordinate21_H,
                _aoViaCoordinate22_L,
                _aoViaCoordinate22_H,
                _aoViaCoordinate23_L,
                _aoViaCoordinate23_H,
                _aoViaCoordinate24_L,
                _aoViaCoordinate24_H,
                _aoViaCoordinate25_L,
                _aoViaCoordinate25_H,
                _aoViaCoordinate26_L,
                _aoViaCoordinate26_H,
                _aoViaCoordinate27_L,
                _aoViaCoordinate27_H,
                _aoViaCoordinate28_L,
                _aoViaCoordinate28_H,
                _aoViaCoordinate29_L,
                _aoViaCoordinate29_H,
                _aoViaCoordinate30_L,
                _aoViaCoordinate30_H,
                _aoViaCoordinate31_L,
                _aoViaCoordinate31_H,
                _aoViaCoordinate32_L,
                _aoViaCoordinate32_H,
                _aoViaCoordinate33_L,
                _aoViaCoordinate33_H,
                _aoViaCoordinate34_L,
                _aoViaCoordinate34_H,
                _aoViaCoordinate35_L,
                _aoViaCoordinate35_H,
                _aoViaCoordinate36_L,
                _aoViaCoordinate36_H,
                _aoViaCoordinate37_L,
                _aoViaCoordinate37_H,
                _aoViaCoordinate38_L,
                _aoViaCoordinate38_H,
                _aoViaCoordinate39_L,
                _aoViaCoordinate39_H,
                _aoViaCoordinate40_L,
                _aoViaCoordinate40_H,
                _aoViaCoordinate41_L,
                _aoViaCoordinate41_H,
                _aoViaCoordinate42_L,
                _aoViaCoordinate42_H,
                _aoViaCoordinate43_L,
                _aoViaCoordinate43_H,
                _aoViaCoordinate44_L,
                _aoViaCoordinate44_H,
                _aoViaCoordinate45_L,
                _aoViaCoordinate45_H,
                _aoViaCoordinate46_L,
                _aoViaCoordinate46_H,
                _aoViaCoordinate47_L,
                _aoViaCoordinate47_H,
                _aoViaCoordinate48_L,
                _aoViaCoordinate48_H,
                _aoViaCoordinate49_L,
                _aoViaCoordinate49_H,
                _aoViaCoordinate50_L,
                _aoViaCoordinate50_H,

            };
            SubscribeDataVariable();

        }

        private AIAccessor _aiVehiclePLCState;
        private AIAccessor _aiTargetPosition;
        private AIAccessor _aiCurrentCoordinate_L;
        private AIAccessor _aiCurrentCoordinate_H;
        private AIAccessor _aiVehiclePLCIP;
        private AIAccessor _aiVehicleID0;
        private AIAccessor _aiVehicleID1;
        private AIAccessor _aiVehicleID2;
        private AIAccessor _aiVehicleID3;

        private AIAccessor _aiCurrentCommand;
        private AIAccessor _aiCurrentCommandStatus;
        private AIAccessor _aiErrorCode;
        private AIAccessor _aiDistanceToFrontVehicle;

        

        private AIAccessor _aiTeachCommandStatus;
        private AIAccessor _aiCurrentXPosition_L;
        private AIAccessor _aiCurrentXPosition_H;
        private AIAccessor _aiCurrentYPosition_L;
        private AIAccessor _aiCurrentYPosition_H;
        private AIAccessor _aiCurrentZPosition_L;
        private AIAccessor _aiCurrentZPosition_H;
        private AIAccessor _aiRFIDChar0;
        private AIAccessor _aiRFIDChar1;
        private AIAccessor _aiRFIDChar2;
        private AIAccessor _aiRFIDChar3;
        private AIAccessor _aiRFIDChar4;
        private AIAccessor _aiRFIDChar5;
        private AIAccessor _aiRFIDChar6;
        private AIAccessor _aiRFIDChar7;
        private AIAccessor _aiRFIDChar8;
        private AIAccessor _aiRFIDChar9;
        private AIAccessor _aiRFIDChar10;
        private AIAccessor _aiCurrentPosition;





        private AOAccessor _aoVehicleCommand;
        private AOAccessor _aoTargetPosition;
        private AOAccessor _aoViaCoordinate1_L;
        private AOAccessor _aoViaCoordinate1_H;
        private AOAccessor _aoViaCoordinate2_L;
        private AOAccessor _aoViaCoordinate2_H;
        private AOAccessor _aoViaCoordinate3_L;
        private AOAccessor _aoViaCoordinate3_H;
        private AOAccessor _aoViaCoordinate4_L;
        private AOAccessor _aoViaCoordinate4_H;
        private AOAccessor _aoViaCoordinate5_L;
        private AOAccessor _aoViaCoordinate5_H;
        private AOAccessor _aoViaCoordinate6_L;
        private AOAccessor _aoViaCoordinate6_H;
        private AOAccessor _aoViaCoordinate7_L;
        private AOAccessor _aoViaCoordinate7_H;
        private AOAccessor _aoViaCoordinate8_L;
        private AOAccessor _aoViaCoordinate8_H;
        private AOAccessor _aoViaCoordinate9_L;
        private AOAccessor _aoViaCoordinate9_H;
        private AOAccessor _aoViaCoordinate10_L;
        private AOAccessor _aoViaCoordinate10_H;
        private AOAccessor _aoViaCoordinate11_L;
        private AOAccessor _aoViaCoordinate11_H;
        private AOAccessor _aoViaCoordinate12_L;
        private AOAccessor _aoViaCoordinate12_H;
        private AOAccessor _aoViaCoordinate13_L;
        private AOAccessor _aoViaCoordinate13_H;
        private AOAccessor _aoViaCoordinate14_L;
        private AOAccessor _aoViaCoordinate14_H;
        private AOAccessor _aoViaCoordinate15_L;
        private AOAccessor _aoViaCoordinate15_H;
        private AOAccessor _aoViaCoordinate16_L;
        private AOAccessor _aoViaCoordinate16_H;
        private AOAccessor _aoViaCoordinate17_L;
        private AOAccessor _aoViaCoordinate17_H;
        private AOAccessor _aoViaCoordinate18_L;
        private AOAccessor _aoViaCoordinate18_H;
        private AOAccessor _aoViaCoordinate19_L;
        private AOAccessor _aoViaCoordinate19_H;
        private AOAccessor _aoViaCoordinate20_L;
        private AOAccessor _aoViaCoordinate20_H;
        private AOAccessor _aoViaCoordinate21_L;
        private AOAccessor _aoViaCoordinate21_H;
        private AOAccessor _aoViaCoordinate22_L;
        private AOAccessor _aoViaCoordinate22_H;
        private AOAccessor _aoViaCoordinate23_L;
        private AOAccessor _aoViaCoordinate23_H;
        private AOAccessor _aoViaCoordinate24_L;
        private AOAccessor _aoViaCoordinate24_H;
        private AOAccessor _aoViaCoordinate25_L;
        private AOAccessor _aoViaCoordinate25_H;
        private AOAccessor _aoViaCoordinate26_L;
        private AOAccessor _aoViaCoordinate26_H;
        private AOAccessor _aoViaCoordinate27_L;
        private AOAccessor _aoViaCoordinate27_H;
        private AOAccessor _aoViaCoordinate28_L;
        private AOAccessor _aoViaCoordinate28_H;
        private AOAccessor _aoViaCoordinate29_L;
        private AOAccessor _aoViaCoordinate29_H;
        private AOAccessor _aoViaCoordinate30_L;
        private AOAccessor _aoViaCoordinate30_H;
        private AOAccessor _aoViaCoordinate31_L;
        private AOAccessor _aoViaCoordinate31_H;
        private AOAccessor _aoViaCoordinate32_L;
        private AOAccessor _aoViaCoordinate32_H;
        private AOAccessor _aoViaCoordinate33_L;
        private AOAccessor _aoViaCoordinate33_H;
        private AOAccessor _aoViaCoordinate34_L;
        private AOAccessor _aoViaCoordinate34_H;
        private AOAccessor _aoViaCoordinate35_L;
        private AOAccessor _aoViaCoordinate35_H;
        private AOAccessor _aoViaCoordinate36_L;
        private AOAccessor _aoViaCoordinate36_H;
        private AOAccessor _aoViaCoordinate37_L;
        private AOAccessor _aoViaCoordinate37_H;
        private AOAccessor _aoViaCoordinate38_L;
        private AOAccessor _aoViaCoordinate38_H;
        private AOAccessor _aoViaCoordinate39_L;
        private AOAccessor _aoViaCoordinate39_H;
        private AOAccessor _aoViaCoordinate40_L;
        private AOAccessor _aoViaCoordinate40_H;
        private AOAccessor _aoViaCoordinate41_L;
        private AOAccessor _aoViaCoordinate41_H;
        private AOAccessor _aoViaCoordinate42_L;
        private AOAccessor _aoViaCoordinate42_H;
        private AOAccessor _aoViaCoordinate43_L;
        private AOAccessor _aoViaCoordinate43_H;
        private AOAccessor _aoViaCoordinate44_L;
        private AOAccessor _aoViaCoordinate44_H;
        private AOAccessor _aoViaCoordinate45_L;
        private AOAccessor _aoViaCoordinate45_H;
        private AOAccessor _aoViaCoordinate46_L;
        private AOAccessor _aoViaCoordinate46_H;
        private AOAccessor _aoViaCoordinate47_L;
        private AOAccessor _aoViaCoordinate47_H;
        private AOAccessor _aoViaCoordinate48_L;
        private AOAccessor _aoViaCoordinate48_H;
        private AOAccessor _aoViaCoordinate49_L;
        private AOAccessor _aoViaCoordinate49_H;
        private AOAccessor _aoViaCoordinate50_L;
        private AOAccessor _aoViaCoordinate50_H;

        private AOAccessor[] _aoViaCoordinates;



        private bool _stateIsInitilizing => (_aiVehiclePLCState.Value & 0x1) == 0x1;
        private bool _stateIsIdle => (_aiVehiclePLCState.Value & 0x2) == 0x2;
        private bool _stateIsMoving => (_aiVehiclePLCState.Value & 0x4) == 0x4;
        private bool _stateIsPicking => (_aiVehiclePLCState.Value & 0x8) == 0x8;
        private bool _stateIsPlacing => (_aiVehiclePLCState.Value & 0x10) == 0x10;
        private bool _stateIsPaused => (_aiVehiclePLCState.Value & 0x20) == 0x20;
        private bool _stateIsWait => (_aiVehiclePLCState.Value & 0x40) == 0x40;
        private bool _stateIsOnlineMode => (_aiVehiclePLCState.Value & 0x80) == 0x80;
        private bool _stateIsTeachMode => (_aiVehiclePLCState.Value & 0x100) == 0x100;      
        private bool _stateIsError => (_aiVehiclePLCState.Value & 0x200) == 0x200;

        private uint _vehicleTargePostion => BitConverter.ToUInt16(BitConverter.GetBytes(_aiTargetPosition.Value),0);

        private float _vehicleCurrentCoordinate
        {
            get
            {
                List<byte> value = new List<byte>();
                value.AddRange(BitConverter.GetBytes(_aiCurrentCoordinate_L.Value));
                value.AddRange(BitConverter.GetBytes(_aiCurrentCoordinate_H.Value));
                return BitConverter.ToSingle(value.ToArray(), 0);
            }
        }


        private uint _vehicleCurrentPosition => BitConverter.ToUInt16(BitConverter.GetBytes(_aiCurrentPosition.Value), 0);
        private string _vehicleIdOnPlc
        {
            get
            {
                List<byte> lbName = new List<byte>();
                lbName.AddRange(BitConverter.GetBytes(_aiVehicleID0.Value));
                lbName.AddRange(BitConverter.GetBytes(_aiVehicleID1.Value));
                lbName.AddRange(BitConverter.GetBytes(_aiVehicleID2.Value));
                lbName.AddRange(BitConverter.GetBytes(_aiVehicleID3.Value));
                return Encoding.ASCII.GetString(lbName.ToArray());
            }
        }
        private uint _vehiclePLCIP => BitConverter.ToUInt16(BitConverter.GetBytes(_aiVehiclePLCIP.Value), 0);

        //private bool _currentCmdIsReplyInit => (_aiCurrentCommand.Value & 0x1) == 0x1;
        private bool _currentCmdIsReplyPause => (_aiCurrentCommand.Value & 0x1) == 0x1;
        private bool _currentCmdIsReplyClearCommand => (_aiCurrentCommand.Value & 0x2) == 0x2;
        private bool _currentCmdIsReplySetOnline => (_aiCurrentCommand.Value & 0x4) == 0x4;
        private bool _currentCmdIsReplyMoveToPosition => (_aiCurrentCommand.Value & 0x8) == 0x8;
        private bool _currentCmdIsReplyPick => (_aiCurrentCommand.Value & 0x10) == 0x10;
        private bool _currentCmdIsReplyPlace => (_aiCurrentCommand.Value & 0x20) == 0x20;

        private bool _currentCmdIsReplyChangeRoute => (_aiCurrentCommand.Value & 0x40) == 0x40;
        private bool _currentCmdIsReplyReadRFID => (_aiCurrentCommand.Value & 0x80) == 0x80;
        private bool _currentCmdIsReplyClearError => (_aiCurrentCommand.Value & 0x100) == 0x100;
        private bool _currentCmdIsReplyInit => (_aiCurrentCommand.Value & 0x200) == 0x200;

        private bool _cmdResultIsACK => (_aiCurrentCommandStatus.Value & 0x1) == 0x1;
        private bool _cmdResultIsComplete => (_aiCurrentCommandStatus.Value & 0x2) == 0x2;
        private bool _cmdResultIsNAK => (_aiCurrentCommandStatus.Value & 0x4) == 0x4;
        private bool _cmdResultIsAbs => (_aiCurrentCommandStatus.Value & 0x8) == 0x8;
        private bool _cmdResultIsRfidOK => (_aiCurrentCommandStatus.Value & 0x10) == 0x10;
        private bool _cmdResultIsRfidNG => (_aiCurrentCommandStatus.Value & 0x20) == 0x20;


        private bool _currentCmdIsSendPause => (_aoVehicleCommand.Value & 0x1) == 0x1;
        private bool _currentCmdIsSendClearCommand => (_aoVehicleCommand.Value & 0x2) == 0x2;
        private bool _currentCmdIsSendSetOnline => (_aoVehicleCommand.Value & 0x4) == 0x4;
        private bool _currentCmdIsSendMoveToPosition => (_aoVehicleCommand.Value & 0x8) == 0x8;
        private bool _currentCmdIsSendPick => (_aoVehicleCommand.Value & 0x10) == 0x10;
        private bool _currentCmdIsSendPlace => (_aoVehicleCommand.Value & 0x20) == 0x20;
        private bool _currentCmdIsSendChangeRoute => (_aoVehicleCommand.Value & 0x40) == 0x40;
        private bool _currentCmdIsSendReadRFID => (_aoVehicleCommand.Value & 0x80) == 0x80;
        private bool _currentCmdIsSendClearError => (_aoVehicleCommand.Value & 0x100) == 0x100;
        private bool _currentCmdIsSendInit => (_aoVehicleCommand.Value & 0x200) == 0x200;

        private uint _currentCmdTargetPosition => BitConverter.ToUInt16(BitConverter.GetBytes(_aoTargetPosition.Value),0);

        private float[] _currentCmdViaCoordinates
        {
            get
            {
                List<float> values = new List<float>();
                for (int i = 0; i < _aoViaCoordinates.Length/2;i++)
                {
                    List<byte> value = new List<byte>();
                    value.AddRange(BitConverter.GetBytes(_aoViaCoordinates[i * 2].Value));
                    value.AddRange(BitConverter.GetBytes(_aoViaCoordinates[i * 2 +1].Value));
                    float coordinatevalue = BitConverter.ToSingle(value.ToArray(), 0);
                    if (coordinatevalue == 0) break;
                    values.Add(coordinatevalue);
                }
                return values.ToArray();
            }
        }

        private string _currentRFID
        {
            get
            {
                List<byte> bValues = new List<byte>();
                AIAccessor[] aiRfid = {_aiRFIDChar0,_aiRFIDChar1,_aiRFIDChar2,_aiRFIDChar3,_aiRFIDChar4, _aiRFIDChar5, _aiRFIDChar6, _aiRFIDChar7, };

                foreach(var ai in aiRfid)
                {
                    bValues.AddRange(BitConverter.GetBytes(ai.Value));
                }

                string strValue = Encoding.ASCII.GetString(bValues.ToArray());

                int index = strValue.IndexOf(null);
                if (index != -1)
                {
                    strValue = strValue.Substring(0, index);
                }
                return strValue;
            }
        }


        private void SubscribeDataVariable()
        {
            DATA.Subscribe($"{Module}.{Name}.Status", () => ((VehicleStateEnum)fsm.State).ToString());
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStateInitializing", () => _stateIsInitilizing);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStateIdle", () => _stateIsIdle);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStateMoving", () => _stateIsMoving);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStatePlacing", () => _stateIsPlacing);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStatePicking", () => _stateIsPicking);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStatePaused", () => _stateIsPaused);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStateWait", () => _stateIsWait);

            DATA.Subscribe($"{Module}.{Name}.IsCurrentStateOnline", () => _stateIsOnlineMode);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStateTeachMode", () => _stateIsTeachMode);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStatePicking", () => _stateIsPicking);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentStateError", () => _stateIsError);
            DATA.Subscribe($"{Module}.{Name}.CurrentTargetPosition", () => _vehicleTargePostion);
            DATA.Subscribe($"{Module}.{Name}.CurrentPosition", () => _vehicleCurrentPosition);
            
            DATA.Subscribe($"{Module}.{Name}.CurrentCoordinate", () => _vehicleCurrentCoordinate);
            DATA.Subscribe($"{Module}.{Name}.VehicleIpAddress", () => _vehiclePLCIP);
            DATA.Subscribe($"{Module}.{Name}.VehicleID", () => _vehicleIdOnPlc);

            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyPause", () => _currentCmdIsReplyPause);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyClearCommand", () => _currentCmdIsReplyClearCommand);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplySetOnline", () => _currentCmdIsReplySetOnline);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyMoveToPos", () => _currentCmdIsReplyMoveToPosition);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyPickFromPos", () => _currentCmdIsReplyPick);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyPlaceToPos", () => _currentCmdIsReplyPlace);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyChangeRoute", () => _currentCmdIsReplyChangeRoute);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyReadRFID", () => _currentCmdIsReplyReadRFID);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandReplyClearError", () => _currentCmdIsReplyClearError);


            DATA.Subscribe($"{Module}.{Name}.CmdResultIsACK", () => _cmdResultIsACK);
            DATA.Subscribe($"{Module}.{Name}.CmdResultIsNAK", () => _cmdResultIsNAK);
            DATA.Subscribe($"{Module}.{Name}.CmdResultIsComplete", () => _cmdResultIsComplete);
            DATA.Subscribe($"{Module}.{Name}.CmdResultIsABS", () => _cmdResultIsAbs);

            DATA.Subscribe($"{Module}.{Name}.CmdResultIsRFIDOK", () => _cmdResultIsRfidOK);

            DATA.Subscribe($"{Module}.{Name}.CmdResultIsRFIDNG", () => _cmdResultIsRfidNG);
            DATA.Subscribe($"{Module}.{Name}.CurrentRFID", () => _currentRFID);

            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendPause", () => _currentCmdIsSendPause);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendClearCommand", () => _currentCmdIsSendClearCommand);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendSetOnline", () => _currentCmdIsSendSetOnline);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendMoveToPos", () => _currentCmdIsSendMoveToPosition);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendPickFromPos", () => _currentCmdIsSendPick);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendPlaceToPos", () => _currentCmdIsSendPlace);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendChangeRoute", () => _currentCmdIsSendChangeRoute);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendReadRFID", () => _currentCmdIsSendReadRFID);
            DATA.Subscribe($"{Module}.{Name}.IsCurrentCommandSendClearError", () => _currentCmdIsSendClearError);
           



        }

        protected override bool fStartReset(object[] param)
        {
            CleanCommand();
            ResetRoutine();
            return base.fStartReset(param);
        }



        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            try 
            {
                SetAoValue((int)VehicleStepEnum.ActionStep1, _aoVehicleCommand, (short)0x100, Notify);
                WaitCommandComplete((int)VehicleStepEnum.ActionStep2, 90, Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x000, Notify);
                WaitCommandStatusClear((int)VehicleStepEnum.ActionStep4, 10, Notify);
                WaitSigStateValue((int)VehicleStepEnum.ActionStep5, 10, _stateIsError, false, Notify);
              
            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                CleanCommand();
                OnError("ResetTimeout");
                return true;
            }            
            return true;
        }
        protected override bool fStartInit(object[] param)
        {
            CleanCommand();
            ResetRoutine();
            return base.fStartInit(param);
        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            try
            {
                SetAoValue((int)VehicleStepEnum.ActionStep1, _aoVehicleCommand, (short)0x200, Notify);
                WaitCommandComplete((int)VehicleStepEnum.ActionStep2, 90,Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x000, Notify);
                WaitCommandStatusClear((int)VehicleStepEnum.ActionStep4, 10, Notify);
            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                CleanCommand();
                OnError("ResetTimeout");
                return true;
            }
            return true;
        }

        protected override bool fStartPause(object[] param)
        {
            CleanCommand();
            ResetRoutine();
            return base.fStartInit(param);
        }


        protected override bool fMonitorPausing(object[] param)
        {
            IsBusy = false;
            try
            {
                SetAoValue((int)VehicleStepEnum.ActionStep1, _aoVehicleCommand, (short)0x1, Notify);
                WaitCommandComplete((int)VehicleStepEnum.ActionStep2, 90, Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x000, Notify);
                WaitCommandStatusClear((int)VehicleStepEnum.ActionStep4, 10, Notify);
                WaitSigStateValue((int)VehicleStepEnum.ActionStep4, 60, _stateIsPaused, true, Notify);
            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                CleanCommand();
                OnError("ResetTimeout");
                return true;
            }
            return true;
        }

        protected override bool fStartMove(object[] param)
        {
            CleanCommand();
            ResetRoutine();
            return base.fStartMove(param);
        }

        protected override bool fMonitorMoving(object[] param)
        {
            IsBusy = false;
            try
            {
                SetAoValue((int)VehicleStepEnum.ActionStep1, _aoTargetPosition, (short)CurrentParameters[0], Notify);
                SetViaPositionValue((int)VehicleStepEnum.ActionStep2, (ushort[])CurrentParameters[1], Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x8, Notify);
                WaitCommandComplete((int)VehicleStepEnum.ActionStep4, 12000, Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x000, Notify);
                WaitCommandStatusClear((int)VehicleStepEnum.ActionStep4, 10, Notify);
                WaitSigStateValue((int)VehicleStepEnum.ActionStep4, 60, _stateIsPaused, true, Notify);
            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                CleanCommand();
                OnError("ResetTimeout");
                return true;
            }
            return true;

        }

        protected override bool fStartPick(object[] param)
        {
            CleanCommand();
            ResetRoutine();
            return base.fStartPick(param);
        }

        protected override bool fMonitorPicking(object[] param)
        {
            IsBusy = false;
            try
            {
                SetAoValue((int)VehicleStepEnum.ActionStep1, _aoTargetPosition, (short)CurrentParameters[0], Notify);
                SetViaPositionValue((int)VehicleStepEnum.ActionStep2, (ushort[])CurrentParameters[1], Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x10, Notify);
                WaitCommandComplete((int)VehicleStepEnum.ActionStep4, 12000, Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x000, Notify);
                WaitCommandStatusClear((int)VehicleStepEnum.ActionStep4, 10, Notify);
                WaitSigStateValue((int)VehicleStepEnum.ActionStep4, 60, _stateIsPaused, true, Notify);
            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                CleanCommand();
                OnError("ResetTimeout");
                return true;
            }
            return true;
        }


        protected override bool fStartPlace(object[] param)
        {
            CleanCommand();
            ResetRoutine();
            return base.fStartPlace(param);
        }

        protected override bool fMonitorPlacing(object[] param)
        {
            IsBusy = false;
            try
            {
                SetAoValue((int)VehicleStepEnum.ActionStep1, _aoTargetPosition, (short)CurrentParameters[0], Notify);
                SetViaPositionValue((int)VehicleStepEnum.ActionStep2, (ushort[])CurrentParameters[1], Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x20, Notify);
                WaitCommandComplete((int)VehicleStepEnum.ActionStep4, 12000, Notify);
                SetAoValue((int)VehicleStepEnum.ActionStep3, _aoVehicleCommand, (short)0x000, Notify);
                WaitCommandStatusClear((int)VehicleStepEnum.ActionStep4, 10, Notify);
                WaitSigStateValue((int)VehicleStepEnum.ActionStep4, 60, _stateIsPaused, true, Notify);
            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                CleanCommand();
                OnError("ResetTimeout");
                return true;
            }
            return true;
        }




        private void CleanCommand()
        {
            _aoVehicleCommand.Value = 0;
        }


        public AOAccessor ParseAoNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() + $"_{Name}" : $"{ioModule}.{node.GetAttribute(name).Trim()}_{Name}"];
            return null;
        }

        public AIAccessor ParseAiNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim()+$"_{Name}" : $"{ioModule}.{node.GetAttribute(name).Trim()}_{Name}"];
            return null;
        }




        private void SetViaPositionValue(int id, ushort[] positions, Action<string> notify)
        {
            var ret = Execute(id, () =>
            {
                ClearViaData();

                int count = positions.Length >= 50 ? 50 : positions.Length;
                for (int i=0;i< count; i++)
                {
                    SetViaCoordinateValue(positions[i], _aoViaCoordinates[2 * i], _aoViaCoordinates[2 * i + 1]);
                }
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
            }
        }

        
        private void SetViaCoordinateValue(ushort postion,AOAccessor lowbits,AOAccessor highbits)
        {
            float coordinate = GetCoordinateValue(postion);
            byte[] bits = BitConverter.GetBytes(coordinate);

            lowbits.Value = BitConverter.ToInt16(bits, 0);
            highbits.Value = BitConverter.ToInt16(bits, 2);


            //lowbits.Value = bits.


        }

        private float GetCoordinateValue(ushort position)
        {
            return 6553.55F;
        }




        private void ClearViaData()
        {
            for(int i=1;i<=50; i++)
            {

            }
        }

        private void SetAoValue(int id, AOAccessor ao, short value, Action<string> notify)
        {
            var ret = Execute(id, () =>
            {
                notify($"{Name} set {ao.Name} to {value}.");
                ao.Value = value;
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
            }
        }

        private void WaitCommandComplete(int id, int time, Action<string> notify)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait Vehicle command ACK State to be true.");
                return true;
            }, () =>
            {
                if (_cmdResultIsACK && _cmdResultIsComplete && !_cmdResultIsAbs && !_cmdResultIsNAK)
                    return true;
                if(_cmdResultIsNAK || _cmdResultIsAbs)
                    throw new RoutineFaildException();
                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    OnError($"Wait Vehicle command execution complete timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }

        private void WaitCommandStatusClear(int id, int time, Action<string> notify)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait Vehicle command State to be clear.");
                return true;
            }, () =>
            {
                if (!_cmdResultIsACK && !_cmdResultIsComplete && !_cmdResultIsAbs && !_cmdResultIsNAK)
                    return true;
               
                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    OnError($"Wait Vehicle command status clear timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }

        private void WaitSigStateValue(int id, int time, bool signalState, bool value, Action<string> notify)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait Vehicle {nameof(signalState)} State to be {value}");
                return true;
            }, () =>
            {
                if (signalState == value)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    OnError($"Wait Vehicle {Name} to be {value} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }

        private void WaitAiValue(int id, int time, AIAccessor ai, short value, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait Vehicle {Name} {ai.Name} to be {value}");
                return true;
            }, () =>
            {
                if (ai.Value == value)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {                    
                    OnError($"Wait Vehicle {Name} {ai.Name} to be {value} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }

        protected void Notify(string message)
        {
            EV.PostMessage(Name, EventEnum.GeneralInfo, string.Format("{0}:{1}", Name, message));
        }
        protected void Stop(string failReason)
        {
            OnError(string.Format("Failed {0}, {1} ", Name, failReason));
        }






        private enum VehicleStepEnum
        { 
            ActionStep1,
            ActionStep2,
            ActionStep3,
            ActionStep4,
            ActionStep5,
            ActionStep6,
            ActionStep7,
            ActionStep8,

            ActionStep9,
            ActionStep10,
            ActionStep11,
            ActionStep12,

            ActionStep13,
            ActionStep14,
            ActionStep15,
            ActionStep16,
        }

        //timer, 计算routine时间
        protected DeviceTimer counter = new DeviceTimer();
        protected DeviceTimer delayTimer = new DeviceTimer();

        private enum STATE
        {
            IDLE,
            WAIT,
        }

        public int TokenId
        {
            get { return _id; }
        }
        private int _id;         //step index

        /// <summary>
        /// already done steps
        /// </summary>
        private Stack<int> _steps = new Stack<int>();

        private STATE state;    //step state //idel,wait,

        //loop control
        private int loop = 0;
        private int loopCount = 0;
        private int loopID = 0;

        private DeviceTimer timer = new DeviceTimer();

        public int LoopCounter { get { return loop; } }
        public int LoopTotalTime { get { return loopCount; } }

        // public int Timeout { get { return (int)(timer.GetTotalTime() / 1000); } }

        //状态持续时间，单位为秒
        public int Elapsed { get { return (int)(timer.GetElapseTime() / 1000); } }

        protected RoutineResult RoutineToken = new RoutineResult() { Result = RoutineState.Running };

        public void ResetRoutine()
        {
            _id = 0;
            _steps.Clear();

            loop = 0;
            loopCount = 0;

            state = STATE.IDLE;
            counter.Start(60 * 60 * 100);   //默认1小时

            RoutineToken.Result = RoutineState.Running;
        }
        protected void PerformRoutineStep(int id, Func<RoutineState> execution, RoutineResult result)
        {
            if (!Acitve(id))
                return;

            result.Result = execution();
        }



        #region interface

        public void StopLoop()
        {
            loop = loopCount;
        }

        public Tuple<bool, Result> Loop<T>(T id, Func<bool> func, int count)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (!func())
                {
                    return Tuple.Create(bActive, Result.FAIL);   //执行错误
                }

                loopID = idx;
                loopCount = count;

                next();
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> EndLoop<T>(T id, Func<bool> func)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (++loop >= loopCount)   //Loop 结束
                {
                    if (!func())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }

                    loop = 0;
                    loopCount = 0;  // Loop 结束时，当前loop和loop总数都清零

                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                //继续下一LOOP

                next(loopID);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, IRoutine routine)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    Result startRet = routine.Start();
                    if (startRet == Result.FAIL)
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    else if (startRet == Result.DONE)
                    {
                        next();
                        return Tuple.Create(true, Result.DONE);
                    }
                    state = STATE.WAIT;
                }

                Result ret = routine.Monitor();

                if (ret == Result.DONE)
                {
                    next();
                    return Tuple.Create(true, Result.DONE);
                }
                else if (ret == Result.FAIL || ret == Result.TIMEOUT)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    return Tuple.Create(true, Result.RUN);
                }

            }

            return Tuple.Create(false, Result.RUN);
        }


        public Tuple<bool, Result> ExecuteAndWait<T>(T id, List<IRoutine> routines)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    foreach (var item in routines)
                    {
                        if (item.Start() == Result.FAIL)
                            return Tuple.Create(true, Result.FAIL);
                    }

                    state = STATE.WAIT;
                }
                //wait all sub failed or completedboo

                bool bFail = false;
                bool bDone = true;

                foreach (var item in routines)
                {
                    Result ret = item.Monitor();

                    bDone &= (ret == Result.FAIL || ret == Result.DONE);
                    bFail |= ret == Result.FAIL;
                }

                if (bDone)
                {
                    next();

                    if (bFail)
                        return Tuple.Create(true, Result.FAIL);

                    return Tuple.Create(true, Result.DONE);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }



        public Tuple<bool, Result> Check<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(Check(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Execute<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(execute(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, double timeout = int.MaxValue)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if (!execute())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(bActive, Result.FAIL);    //Termianate
                }
                else
                {
                    if (bExecute.Value)       //检查Success, next
                    {
                        next();
                        return Tuple.Create(true, Result.RUN);
                    }
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, Func<double> time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;
            double timeout = 0;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timeout = time();
                    if (!execute())
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }
                if (bExecute.Value)       //检查Success, next
                {
                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> Wait<T>(T id, IRoutine rt)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    rt.Start();
                    state = STATE.WAIT;
                }

                Result ret = rt.Monitor();

                return Tuple.Create(true, ret);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Monitor
        public Tuple<bool, Result> Monitor<T>(T id, Func<bool> func, Func<bool> check, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool bCheck = false;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                bCheck = check();

                if (!bCheck)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Delay
        public Tuple<bool, Result> Delay<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //先delay 再运行
        public Tuple<bool, Result> DelayCheck<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    if (func != null && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }
        #endregion


        private Tuple<bool, bool> execute(int id, Func<bool> func)   //顺序执行
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                if (bExecute)
                {
                    next();
                }
            }

            return Tuple.Create(bActive, bExecute);
        }


        private Tuple<bool, bool> Check(int id, Func<bool> func)   //check
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                next();
            }

            return Tuple.Create(bActive, bExecute);
        }


        /// <summary>

        /// </summary>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeout"></param>
        /// <returns>
        ///  item1 Active
        ///  item2 execute
        ///  item3 Timeout
        ///</returns>

        private Tuple<bool, bool, bool> wait(int id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        private Tuple<bool, bool?, bool> wait(int id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition && Check error
        {
            bool bActive = Acitve(id);
            bool? bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute.HasValue && bExecute.Value)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        /// <summary>      
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// item1 true, return item2
        /// </returns>
        private Tuple<bool, Result> Check(Tuple<bool, bool> value)
        {
            if (value.Item1)
            {
                if (!value.Item2)
                {
                    return Tuple.Create(true, Result.FAIL);
                }

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (CheckTimeout(value))  //timeout
                {
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool?, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (value.Item2 == null)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    if (value.Item2 == false && value.Item3 == true)  //timeout
                    {
                        return Tuple.Create(true, Result.TIMEOUT);
                    }
                    return Tuple.Create(true, Result.RUN);
                }
            }

            return Tuple.Create(false, Result.RUN);
        }

        private bool CheckTimeout(Tuple<bool, bool, bool> value)
        {
            return value.Item1 == true && value.Item2 == false && value.Item3 == true;
        }

        private bool Acitve(int id) //
        {
            if (_steps.Contains(id))
                return false;

            this._id = id;
            return true;
        }

        private void next()
        {
            _steps.Push(this._id);
            state = STATE.IDLE;
        }

        private void next(int step)   //loop
        {
            while (_steps.Pop() != step) ;

            state = STATE.IDLE;
        }


        public void Delay(int id, double delaySeconds)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                return true;
            }, delaySeconds * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }



        public bool IsActived(int id)
        {
            return _steps.Contains(id);
        }


    }
}
