using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Brooks.WinSECS;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Text.RegularExpressions;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.Fortrend
{
    public delegate void OnEventArrivedEventHandler(string message);
    public delegate void OnAlarmArrivedEventHandler(string message);
    public delegate void OnErrorEventHandler(string alarmcode, string alarmdecri, string alarmtext);
    public delegate void OnStatusdEventHandler(bool isplace, bool home, bool alarm, bool isbusy);
    public class FortrendSMIFHostAgent : INotifyAgent
    {
        private WinSECS wshost;
        private FortrendSmifPort SMIF;

        private string _currentExecuteCommand;

        public FortrendSMIFHostAgent(WinSECS winSecs, FortrendSmifPort smif)
        {
            wshost = winSecs;
            SMIF = smif;
        }
        public void OnConnect()
        {
            SMIF.IsConnected = true;
            SECSTransactionBuilder.BuildS2F15(0, 1).Send(wshost);
        }

        public void OnDisconnect(ERRORS ErrorCode, string ErrorText)
        {
            SMIF.IsConnected = false;
            EV.PostAlarmLog("BrooksSMIF", ErrorText);
            EV.PostAlarmLog("BrooksSMIF", "connectError:" + ErrorText);
        }

        public void OnError(ERRORS ErrorCode, string ErrorText)
        {
            SMIF.IsAlarm = true;
            EV.PostAlarmLog("BrooksSMIF", ErrorText);
            EV.PostAlarmLog("BrooksSMIF", "Error:" + ErrorText);
        }

        public void OnMonitor(DateTime Time, bool bSent, string evt, string sHex)
        {

        }

        public void OnPrimaryIn(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            LOG.Write("Received S" + Convert.ToString(t.Primary.Stream) + "F" + Convert.ToString(t.Primary.Function));
            LOG.Write(t.Primary.ToString());
            string SF = "S" + t.Primary.Stream.ToString() + "F" + t.Primary.Function.ToString();
            SECSTransaction reply = null;
            SECSTransaction recive = null;
            switch (SF)
            {
                case "S5F1":
                    {
                        var alarmcode = t.Primary.Root.Item(0).Item(0).Value.ToString();
                        var alarmdescri = t.Primary.Root.Item(0).Item(1).Value.ToString();
                        var alarmdtext = t.Primary.Root.Item(0).Item(2).Value.ToString();
                        LOG.Write($"alarmcode:{alarmcode}--alarmdescri:{alarmdescri}--alarmdtext{alarmdtext}");
                        SMIF.OnErrorArrived(alarmcode, alarmdescri, alarmdtext);
                    }
                    break;
                case "S6F11":
                    SMIF.OnEventArrived(t.Primary.Root.Item(0).Item(1).Value.ToString());
                    reply = new SECSTransaction(6, 12);
                    reply.Secondary.Root.Name = "ERA";
                    reply.Secondary.Root.AddNew("ACKC6", "Acknowledge code");
                    reply.Secondary.Root.Item("ACKC6").Format = SECS_FORMAT.BINARY;
                    reply.Secondary.Root.Item("ACKC6").Value = 0;

                    break;
                case "S6F13":
                    SMIF.OnEventArrived(t.Primary.Root.Item(0).Item(1).Value.ToString());

                    reply = new SECSTransaction(6, 14);
                    reply.Secondary.Root.Name = "AERA";
                    reply.Secondary.Root.AddNew("ACKC6", "Acknowledge code");
                    reply.Secondary.Root.Item("ACKC6").Format = SECS_FORMAT.BINARY;
                    reply.Secondary.Root.Item("ACKC6").Value = 0;
                    break;
                case "S6F3":
                    SMIF.OnEventArrived(t.Primary.Root.Item(0).Item(1).Value.ToString());

                    reply = new SECSTransaction(6, 4);
                    reply.Secondary.Root.Name = "AERA";
                    reply.Secondary.Root.AddNew("ACKC6", "Acknowledge code");
                    reply.Secondary.Root.Item("ACKC6").Format = SECS_FORMAT.BINARY;
                    reply.Secondary.Root.Item("ACKC6").Value = 0;
                    break;
                case "S2F41":

                    if (recive.Primary.Root.Value != null)
                    {
                        EV.PostAlarmLog("BrooksSMIF", $"Value:{recive.Primary.Root.Value}");
                    }
                    break;
            }

            reply?.Reply(wshost);
        }

        public void OnPrimaryOut(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            LOG.Write("Send S" + Convert.ToString(t.Primary.Stream) + "F" + Convert.ToString(t.Primary.Function));
            LOG.Write(t.ToString());
            string SF = "S" + t.Primary.Stream.ToString() + "F" + t.Primary.Function.ToString();
            switch (SF)
            {
                case "S2F41":
                    _currentExecuteCommand = t.Primary.Root.Item(0).Item(0).Value.ToString();
                    EV.PostInfoLog("BrooksSMIF", $"Value:{ _currentExecuteCommand}");
                    break;
            }
        }

        public void OnSecondaryIn(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            LOG.Write("Received S" + Convert.ToString(t.Secondary.Stream) + "F" + Convert.ToString(t.Secondary.Function));
            LOG.Write(t.ToString());
            string SF = "S" + t.Secondary.Stream.ToString() + "F" + t.Secondary.Function.ToString();
            switch (SF)
            {
                case "S2F42":
                    {
                        if (t.Secondary.Root.Item(0).Value.ToString() == "0")
                        {
                            EV.PostInfoLog("BrooksSMIF", $"Ack:{t.Secondary.Root.Item(0).Value.ToString()}");
                            if (_currentExecuteCommand != "13" || _currentExecuteCommand != "9" || _currentExecuteCommand != "11")
                                SMIF.IsIdle = true;
                        }

                        if (ErrorCode != ERRORS.OK)
                        {
                            EV.PostInfoLog("BrooksSMIF", $"ErrorCode:{ErrorCode}");
                            //EV.PostAlarmLog("BrooksSMIF", $"Execute {_currentExecuteCommand} Failed , Reason:{ErrorText}");
                        }
                    }
                    break;
                case "S1F6":
                    {
                        //EV.PostInfoLog("BrooksSMIF", $"S1F6{t.Secondary.Root.Item(0).Item(0).Value.ToString()}");


                        byte[] statusValue = (byte[])t.Secondary.Root.Item(0).Item(0).Value;
                        SMIF.ParseStatus1(statusValue);
                        string strValue = "";
                        foreach (var bvalue in statusValue)
                        {
                            strValue += bvalue.ToString() + ",";
                        }
                        LOG.Write("GetStatus1:" + strValue);
                        statusValue = new byte[] { (byte)t.Secondary.Root.Item(0).Item(2).Value };
                        SMIF.ParseStatus2(statusValue);
                        strValue = "";
                        foreach (var bvalue in statusValue)
                        {
                            strValue += bvalue.ToString() + ",";
                        }
                        LOG.Write("GetStatus2:" + strValue);
                        statusValue = (byte[])t.Secondary.Root.Item(0).Item(4).Value;
                        SMIF.ParseStatus3(statusValue);
                        strValue = "";
                        foreach (var bvalue in statusValue)
                        {
                            strValue += bvalue.ToString() + ",";
                        }
                        LOG.Write("GetStatus3:" + strValue);
                        SMIF.IsQueryComplete = true;


                    }
                    break;
                case "S100F110":
                    {
                        string str = t.Secondary.Root.Item(0).Item(1).Value.ToString();
                        LOG.Write($"Smart tag reader received message:{str}.");
                        string startstr = SC.GetStringValue($"LoadPort.{SMIF.LPModuleName}.CarrierIDStartString");
                        string endstr = SC.GetStringValue($"LoadPort.{SMIF.LPModuleName}.CarrierIDEndString");
                        Regex rg = new Regex("(?<=(" + startstr + "))[.\\s\\S]*?(?=(" + endstr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
                        string carrierID = rg.Match(str).Value.Replace(":", "").Trim();
                        SMIF.OnCarrierIdRead(carrierID);
                    }
                    break;
                case "S100F122":
                    {
                        string str = t.Secondary.Root.Item(0).Item(1).Value.ToString();
                        LOG.Write($"Smart tag reader received message:{str}.");
                        string startstr = SC.GetStringValue($"LoadPort.{SMIF.LPModuleName}.CarrierIDStartString");
                        string endstr = SC.GetStringValue($"LoadPort.{SMIF.LPModuleName}.CarrierIDEndString");
                        Regex rg = new Regex("(?<=(" + startstr + "))[.\\s\\S]*?(?=(" + endstr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
                        string carrierID = rg.Match(str).Value.Replace(":", "").Trim();
                        LOG.Write($"CarrierID:{carrierID }");
                        SMIF.SetLotID(new object[] { str }, out _);
                        SMIF.OnCarrierIdRead(carrierID);
                        SMIF.IsBusy = false;
                    }
                    break;
                case "S100F120":
                    {
                        string str = t.Secondary.Root.Item(0).Value.ToString();
                        LOG.Write($"Smart tag writer received message:{str}");
                        if(str=="0")
                          EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} carrierID Write successfully.");
                        SMIF.IsBusy = false;
                    }
                    break;

                case "S1F4":
                    string slotmap = null;
                    for (int i = 0; i < 25; i++)
                    {
                        slotmap += t.Secondary.Root.Item(0).Item(0).Item(i).Value.ToString();
                    }
                    //string slotmap = t.Secondary.Root.Item(0).Item(0).Value.ToString();
                    LOG.Write($"SlotMap:{slotmap}");
                    //string count = t.Secondary.Root.Item(0).Item(0).Value.ToString();
                    SMIF.OnSlotMapRead(slotmap);
                    break;


            }


        }

        public void OnSecondaryOut(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            LOG.Write("Send S" + Convert.ToString(t.Secondary.Stream) + "F" + Convert.ToString(t.Secondary.Function));
            LOG.Write(t.ToString());
            ;
        }

        public void OnWarning(ERRORS ErrorCode, string ErrorText)
        {
            ;
        }

        public string ConvertSecsMessage(SECSMessage smsg)
        {
            string retValue = "S" + smsg.Stream.ToString() + "F" + smsg.Function.ToString() + "\r\n";
            if (smsg.Root.ItemCount != 0)
                retValue += "<L" + "\r\n";

            for (int i = 0; i < smsg.Root.ItemCount; i++)
            {

            }


            return retValue;
        }



    }
}