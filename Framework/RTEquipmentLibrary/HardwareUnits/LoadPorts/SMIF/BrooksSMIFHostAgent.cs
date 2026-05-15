using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Brooks.WinSECS;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.SMIF
{
    public delegate void OnEventArrivedEventHandler(string message);
    public delegate void OnAlarmArrivedEventHandler(string message);
    public delegate void OnErrorEventHandler(string alarmcode, string alarmdecri, string alarmtext);
    public delegate void OnStatusdEventHandler(bool isplace, bool home, bool alarm, bool isbusy);
    public class BrooksSMIFHostAgent : INotifyAgent
    {
        private WinSECS wshost;
        private BrooksSmifPort SMIF;

        //public event OnEventArrivedEventHandler OnEventArrived;
        //public event OnAlarmArrivedEventHandler OnAlarmArrived;
        public event OnStatusdEventHandler OnStausArrived;
        public event OnErrorEventHandler OnErrorArrived;
        private string _currentExecuteCommand;

        public BrooksSMIFHostAgent(WinSECS winSecs, BrooksSmifPort smif)
        {
            wshost = winSecs;
            SMIF = smif;
        }
        public void OnConnect()
        {
            SMIF.IsConnected = true;
            SECSTransactionBuilder.BuildS1F5(0).Send(wshost);
        }

        public void OnDisconnect(ERRORS ErrorCode, string ErrorText)
        {
            SMIF.IsConnected = false;
            EV.PostAlarmLog("LoadPort",$"{SMIF.LPModuleName} occurred connectError:{ErrorText}" );
            
        }

        public void OnError(ERRORS ErrorCode, string ErrorText)
        {
            SMIF.IsAlarm = true;
            EV.PostAlarmLog("LoadPort", $"{SMIF.LPModuleName} occurred error:{ErrorText}");

        }

        public void OnMonitor(DateTime Time, bool bSent, string evt, string sHex)
        {

        }

        public void OnPrimaryIn(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            string SF = "S" + t.Primary.Stream.ToString() + "F" + t.Primary.Function.ToString();
            EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} recived primary message:{SF}.");
            SECSTransaction reply = null;
            SECSTransaction recive = null;
            switch (SF)
            {
                case "S5F1":
                    {
                        var alarmcode = t.Primary.Root.Item(0).Item(0).Value.ToString();
                        var alarmdescri = t.Primary.Root.Item(0).Item(1).Value.ToString();
                        var alarmdtext = t.Primary.Root.Item(0).Item(2).Value.ToString();
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
                case "S2F41":
                    //recive = new SECSTransaction(6, 14);
                    //recive.Receive();
                    //EV.PostAlarmLog("BrooksSMIF", $"Value:{recive.Primary.Root.Value}");
                    if (recive.Primary.Root.Value != null)
                    {
                        EV.PostAlarmLog("LoadPort", $"{SMIF.LPModuleName} Value:{recive.Primary.Root.Value}");
                    }
                    break;
            }

            reply?.Reply(wshost);
        }

        public void OnPrimaryOut(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            
            string SF = "S" + t.Primary.Stream.ToString() + "F" + t.Primary.Function.ToString();
            EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} Send primary message:{SF}.");
            switch (SF)
            {
                case "S2F41":
                    SMIF.IsActionComplete = false;
                    _currentExecuteCommand = t.Primary.Root.Item(0).Item(0).Value.ToString();
                    EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} send S2F41 command:{ _currentExecuteCommand}");
                    break;
            }
        }

        public void OnSecondaryIn(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            string SF = "S" + t.Secondary.Stream.ToString() + "F" + t.Secondary.Function.ToString();
            
            switch (SF)
            {
                case "S2F42":
                    {
                        EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} received {SF} return code:{t.Secondary.Root.Item(0).Value.ToString()} for S2F41 command {t.Primary.Root.Item(0).Item(0).Value.ToString()}");

                        if (t.Secondary.Root.Item(0).Value.ToString() == "0")
                        {
                            if (_currentExecuteCommand != "13" || _currentExecuteCommand != "9" || _currentExecuteCommand != "11")
                                SMIF.IsActionComplete = true;
                        }

                        if (ErrorCode != ERRORS.OK)
                        {
                            EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName}  ErrorCode:{ErrorCode}");
                            //EV.PostAlarmLog("LoadPort", $"Execute {_currentExecuteCommand} Failed , Reason:{ErrorText}");
                        }
                    }
                    break;
                case "S1F6":
                    {
                        string fcode = t.Secondary.Root.Item(0).Item(0).Value.ToString();
                        EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} Received secondary message:{SF},form code:{fcode}.");
                        if (fcode == "0")
                        {
                            List<object> ldata = new List<object>();
                            for(int i=0;i< t.Secondary.Root.Item(0).Item(1).ItemCount;i++)
                            {
                                ldata.Add(t.Secondary.Root.Item(0).Item(1).Item(i).Value);
                            }
                            SMIF.OnStausArrived(ldata.ToArray());

                        }
                        if (t.Secondary.Root.Item(0).Item(0).Value.ToString() == "1")
                        {

                        }
                        if (t.Secondary.Root.Item(0).Item(0).Value.ToString() == "2")
                        {                            
                            string slotMap = null;
                            int iCount = t.Secondary.Root.Item(0).Item(1).ItemCount;
                            for (int i = 0; i < iCount; i++)
                            {
                                slotMap += t.Secondary.Root.Item(0).Item(1).Item(i).Value.ToString();
                                
                            }
                            if(SMIF.IsMapWaferByLoadPort)
                                SMIF.OnSlotMapRead(slotMap);
                            EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} Received secondary message:{SF},form code:{fcode},slotmap:{slotMap}.");
                        }
                        if (t.Secondary.Root.Item(0).Item(0).Value.ToString() == "3")
                        {

                        }
                    }
                    break;
                case "S100F122":
                    
                    string str = t.Secondary.Root.Item(0).Item(1).Value.ToString();
                    EV.PostInfoLog("LoadPort", $"{SMIF.LPModuleName} received {SF} return code:{str}");

                    LOG.Write($"Smart tag reader received message:{str}.");
                    //string startstr = SC.GetStringValue($"LoadPort.{SMIF.LPModuleName}.CarrierIDStartString");
                    //string endstr = SC.GetStringValue($"LoadPort.{SMIF.LPModuleName}.CarrierIDEndString");
                    //Regex rg = new Regex("(?<=(" + startstr + "))[.\\s\\S]*?(?=(" + endstr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
                    //string carrierID = rg.Match(str).Value.Replace(":", "").Trim();
                    SMIF.OnCarrierIdRead(str);
                    break;
            }
        }
        
        public void OnSecondaryOut(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            ;
        }

        public void OnWarning(ERRORS ErrorCode, string ErrorText)
        {
            ;
        }



    }
}