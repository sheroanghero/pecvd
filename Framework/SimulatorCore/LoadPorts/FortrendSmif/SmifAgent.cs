using Brooks.WinSECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.LoadPorts.SecsSmif
{
    public class SmifAgent : INotifyAgent
    {
        public WinSECS WinsecsHost;
        private SmifPortSimulator _sim;
        public SmifAgent(SmifPortSimulator simulator)
        {
            _sim = simulator;
        }
        public void OnPrimaryIn(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            switch (t.Name)
            {
                case "S1F1":
                    t.Secondary.Root.AddNew("MDLN");
                    t.Secondary.Root.AddNew("SOFTREV");

                    t.Secondary.Root.Item("MDLN").Value = "NTB";
                    t.Secondary.Root.Item("SOFTREV").Value = "1.0";
                    t.Reply(WinsecsHost);
                    break;
                case "S1F13":
                    t.Secondary.Root.AddNew("LIST1");
                    t.Secondary.Root.Item("LIST1").Format = SECS_FORMAT.LIST;



                    t.Secondary.Root.Item("LIST1").AddNew("COMMACK");
                    t.Secondary.Root.Item("LIST1").AddNew("LIST2");

                    t.Secondary.Root.Item("LIST1").Item("LIST2").AddNew("MDLN");
                    t.Secondary.Root.Item("LIST1").Item("LIST2").AddNew("SOFTREV");

                    t.Secondary.Root.Item("COMMACK").Format = SECS_FORMAT.BINARY;
                    t.Secondary.Root.Item("COMMACK").Value = 0;

                    t.Secondary.Root.Item("MDLN").Value = "NTB";
                    t.Secondary.Root.Item("SOFTREV").Value = "1.0";
                    t.Reply(WinsecsHost);
                    break;
                //SECSTransactionBuilder.BuildS1F14(0).Send(m_winsecs);

                case "S1F5":
                    t.Secondary.Root.AddNew("LIST1");
                    t.Secondary.Root.Item("LIST1").Format = SECS_FORMAT.LIST;


                    t.Secondary.Root.Item("LIST1").AddNew($"DATA1");
                    t.Secondary.Root.Item("LIST1").Item($"DATA1").Format = SECS_FORMAT.U1;
                    t.Secondary.Root.Item("LIST1").Item($"DATA1").Value = new byte[] { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 20, 1, 1 };

                    t.Secondary.Root.Item("LIST1").AddNew("STATUS");
                    t.Secondary.Root.Item("LIST1").Item("STATUS").Format = SECS_FORMAT.U2;
                    t.Secondary.Root.Item("LIST1").Item("STATUS").Value = new byte[] { 0, 0, 0, 0 };

                    t.Secondary.Root.Item("LIST1").AddNew("LOTID1");
                    t.Secondary.Root.Item("LIST1").Item("LOTID1").Format = SECS_FORMAT.U1;
                    t.Secondary.Root.Item("LIST1").Item("LOTID1").Value = 2;

                    t.Secondary.Root.Item("LIST1").AddNew("LOTID2");
                    t.Secondary.Root.Item("LIST1").Item("LOTID2").Format = SECS_FORMAT.ASCII;
                    t.Secondary.Root.Item("LIST1").Item("LOTID2").Value = $"PLM200W_20201123";
                    t.Secondary.Root.Item("LIST1").AddNew("LOTID3");
                    t.Secondary.Root.Item("LIST1").Item("LOTID3").Format = SECS_FORMAT.U1;
                    t.Secondary.Root.Item("LIST1").Item("LOTID3").Value = new byte[] { 0, 0, 0, 0 };


                    t.Reply(WinsecsHost);
                    break;
                case "S6F11":


                    t.Secondary.Root.AddNew("ACK");

                    t.Secondary.Root.Item("ACK").Format = SECS_FORMAT.BINARY;
                    t.Secondary.Root.Item("ACK").Value = 0;

                    t.Reply(WinsecsHost);
                    break;

                case "S2F41":



                    t.Secondary.Root.AddNew("ACK");

                    t.Secondary.Root.Item("ACK").Format = SECS_FORMAT.BINARY;
                    t.Secondary.Root.Item("ACK").Value = 0;

                    t.Reply(WinsecsHost);
                    break;
                case "S2F21":

                    //t.Secondary.Root.Name = "";
                    //t.Secondary.Root.Item("RCMD").Format = SECS_FORMAT.U1; ;

                    //t.Secondary.Root.Item("RCMD").Value =0;

                    //t.Reply(WinsecsHost);
                    if (t.Primary.Root.Item(0).Value.ToString() == "11")
                        SECSTransactionBuilder.BuildS6F3(18).Send(WinsecsHost);
                    else if (t.Primary.Root.Item(0).Value.ToString() == "10")
                        SECSTransactionBuilder.BuildS6F3(15).Send(WinsecsHost);
                    else if (t.Primary.Root.Item(0).Value.ToString() == "9")
                        SECSTransactionBuilder.BuildS6F3(12).Send(WinsecsHost);
                    else if (t.Primary.Root.Item(0).Value.ToString() == "12")
                        SECSTransactionBuilder.BuildS6F3(31).Send(WinsecsHost);
                    else if (t.Primary.Root.Item(0).Value.ToString() == "13")
                        SECSTransactionBuilder.BuildS6F3(32).Send(WinsecsHost);
                    //SECSTransactionBuilder.BuildS5F1(43,"Produte").Send(WinsecsHost);
                    break;
                case "S1F3":
                    t.Secondary.Root.AddNew("LIST1");
                    t.Secondary.Root.Item("LIST1").Format = SECS_FORMAT.LIST;
                    t.Secondary.Root.Item("LIST1").AddNew("SV");
                    t.Secondary.Root.Item("LIST1").Item("SV").Format = SECS_FORMAT.LIST;

                    for (int i = 0; i < 6; i++)
                    {
                        t.Secondary.Root.Item("LIST1").Item("SV").AddNew($"Slot{i}");
                        t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot{i}").Format = SECS_FORMAT.U1;
                        t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot{i}").Value = 1;
                    }
                    for (int i = 0; i < 19; i++)
                    {
                        t.Secondary.Root.Item("LIST1").Item("SV").AddNew($"Slot{i + 6}");
                        t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot{i + 6}").Format = SECS_FORMAT.U1;
                        t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot{i + 6}").Value = 2;
                    }
                    //t.Secondary.Root.Item("LIST1").Item("SV").AddNew($"Slot23");
                    //t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot23").Format = SECS_FORMAT.U1;
                    //t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot23").Value = 4;
                    //t.Secondary.Root.Item("LIST1").Item("SV").AddNew($"Slot24");
                    //t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot24").Format = SECS_FORMAT.U1;
                    //t.Secondary.Root.Item("LIST1").Item("SV").Item($"Slot24").Value = 1;

                    t.Secondary.Root.Item("LIST1").AddNew("SV1");
                    t.Secondary.Root.Item("LIST1").Item("SV1").Format = SECS_FORMAT.LIST;
                    t.Secondary.Root.Item("LIST1").Item("SV1").AddNew($"Slot");
                    t.Secondary.Root.Item("LIST1").Item("SV1").Item($"Slot").Format = SECS_FORMAT.U1;
                    t.Secondary.Root.Item("LIST1").Item("SV1").Item($"Slot").Value = 4;
                    t.Reply(WinsecsHost);
                    break;
                case "S100F121":

                    //t.Secondary.Root.Name = "";
                    //t.Secondary.Root.Item("RCMD").Format = SECS_FORMAT.U1; ;

                    //t.Secondary.Root.Item("RCMD").Value =0;

                    //t.Reply(WinsecsHost);
                    t.Secondary.Root.AddNew("LIST1");
                    t.Secondary.Root.Item("LIST1").Format = SECS_FORMAT.LIST;

                    t.Secondary.Root.Item("LIST1").AddNew($"SV");
                    t.Secondary.Root.Item("LIST1").Item("SV").Format = SECS_FORMAT.U1;
                    t.Secondary.Root.Item("LIST1").Item("SV").Value = 0;

                    t.Secondary.Root.Item("LIST1").AddNew($"SV1");
                    t.Secondary.Root.Item("LIST1").Item("SV1").Format = SECS_FORMAT.ASCII;
                    t.Secondary.Root.Item("LIST1").Item("SV1").Value = "LOT :A135380.1QTY :25STG :PA_PHMODE:BCLN :2009-10-19QTY :25MODE:BQTY :25STG :PA_PHMODE:BSTG :PA_PHSTG :PA_PHCLN :2009-10-19QTY :25QTY :25STG :PA_PHQTY :25STG :PA_PHSTG :PA_PHMODE:BCLN :2009-10-19APCOT01:090729 02:26 ";
                    t.Reply(WinsecsHost);
                    break;
            }
        }

        public void OnPrimaryOut(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {

        }

        public void OnSecondaryOut(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {

        }

        public void OnSecondaryIn(SECSTransaction t, ERRORS ErrorCode, string ErrorText)
        {
            if (t.Name == "S100F121")
            {

            }
        }

        public void OnError(ERRORS ErrorCode, string ErrorText)
        {

        }

        public void OnWarning(ERRORS ErrorCode, string ErrorText)
        {

        }

        public void OnDisconnect(ERRORS ErrorCode, string ErrorText)
        {

        }

        public void OnConnect()
        {

        }

        public void OnMonitor(DateTime Time, bool bSent, string evt, string sHex)
        {

        }

        public SECSItem BuildSecsItemForS1F14()
        {
            SECSItem retitem = new SECSItem("S1F14", "Secondary");
            retitem.Format = SECS_FORMAT.LIST;


            retitem.AddNew("COMMACK");
            retitem.Format = SECS_FORMAT.BINARY;
            retitem.Item("COMMACK").Value = 0;


            retitem.AddNew("LIST");
            retitem.Item("LIST").Format = SECS_FORMAT.LIST;
            retitem.Item("LIST").AddNew("MDLN");
            retitem.Item("LIST").AddNew("SOFTREV");

            retitem.Item("LIST").Item("MDLN").Value = "NTB";
            retitem.Item("LIST").Item("SOFTREV").Value = "1.0";




            return retitem;





        }


        public void SendEventS6F3()
        {

        }


    }
}
