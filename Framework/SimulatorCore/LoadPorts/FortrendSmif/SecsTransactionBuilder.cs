using Brooks.WinSECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.LoadPorts.SecsSmif
{
    public static class SECSTransactionBuilder
    {
        public static SECSTransaction BuildS1F5(int formCode)
        {
            SECSTransaction secsTransaction = new SECSTransaction(1, 15);
            secsTransaction.Primary.Root.Name = "FSR";
            secsTransaction.Primary.Root.AddNew("SFCD", "Status form code");
            secsTransaction.Primary.Root.Item("SFCD").Format = SECS_FORMAT.BINARY;
            secsTransaction.Primary.Root.Item("SFCD").Value = formCode;
            return secsTransaction;
        }
        public static SECSTransaction BuildS6F3(int formCode)
        {
            SECSTransaction secsTransaction = new SECSTransaction(6, 3);
            secsTransaction.Primary.Root.Name = "FSR";
            secsTransaction.Primary.Root.AddNew("L2", "");
            secsTransaction.Primary.Root.Item("L2").Format = SECS_FORMAT.LIST;
            secsTransaction.Primary.Root.Item("L2").AddNew("SFCD1", "Status form code");
            secsTransaction.Primary.Root.Item("L2").Item("SFCD1").Format = SECS_FORMAT.U1;
            secsTransaction.Primary.Root.Item("L2").Item("SFCD1").Value = 0;
            secsTransaction.Primary.Root.Item("L2").AddNew("SFCD2", "Status form code");
            secsTransaction.Primary.Root.Item("L2").Item("SFCD2").Format = SECS_FORMAT.U1;
            secsTransaction.Primary.Root.Item("L2").Item("SFCD2").Value = formCode;
            return secsTransaction;
        }

        public static SECSTransaction BuildS5F1(int formCode,string value)
        {
            SECSTransaction secsTransaction = new SECSTransaction(5, 1);
            secsTransaction.Primary.Root.Name = "FSR";
            secsTransaction.Primary.Root.AddNew("L3", "");
            secsTransaction.Primary.Root.Item("L3").Format = SECS_FORMAT.LIST;
            secsTransaction.Primary.Root.Item("L3").AddNew("SFCD1", "Status form code");
            secsTransaction.Primary.Root.Item("L3").Item("SFCD1").Format = SECS_FORMAT.BINARY;
            secsTransaction.Primary.Root.Item("L3").Item("SFCD1").Value =Convert.ToByte(formCode);
            secsTransaction.Primary.Root.Item("L3").AddNew("SFCD2", "Status form code");
            secsTransaction.Primary.Root.Item("L3").Item("SFCD2").Format = SECS_FORMAT.U2;
            secsTransaction.Primary.Root.Item("L3").Item("SFCD2").Value = formCode;
            secsTransaction.Primary.Root.Item("L3").AddNew("SFCD3", "Status form code");
            secsTransaction.Primary.Root.Item("L3").Item("SFCD3").Format = SECS_FORMAT.ASCII;
            secsTransaction.Primary.Root.Item("L3").Item("SFCD3").Value = value;
            return secsTransaction;
        }

        public static SECSTransaction BuildS100F121(int formCode)
        {
            SECSTransaction secsTransaction = new SECSTransaction(100, 121);
            secsTransaction.Primary.Root.Name = "FSR";
            secsTransaction.Primary.Root.AddNew("SFCD", "Status form code");
            secsTransaction.Primary.Root.Item("SFCD").Format = SECS_FORMAT.BINARY;
            secsTransaction.Primary.Root.Item("SFCD").Value = formCode;
            return secsTransaction;
        }

        public static SECSTransaction BuildS2F41(int rcmd, string cpname = "", string cpval = "")
        {
            SECSTransaction secsTransaction = new SECSTransaction(2, 41);
            secsTransaction.Primary.Root.Name = "HCS";
            secsTransaction.Primary.Root.AddNew("L2a", "");
            secsTransaction.Primary.Root.Item("L2a").Format = SECS_FORMAT.LIST;
            secsTransaction.Primary.Root.Item("L2a").AddNew("RCMD", "Remote command code");
            secsTransaction.Primary.Root.Item("L2a").Item("RCMD").Format = SECS_FORMAT.U4;
            secsTransaction.Primary.Root.Item("L2a").Item("RCMD").Value = rcmd;

            secsTransaction.Primary.Root.Item("L2a").AddNew("ln", "");
            secsTransaction.Primary.Root.Item("L2a").Item("ln").Format = SECS_FORMAT.LIST;
            secsTransaction.Primary.Root.Item("L2a").Item("ln").AddNew("L2b", "");
            secsTransaction.Primary.Root.Item("L2a").Item("ln").Item("L2b").Format = SECS_FORMAT.LIST;
            secsTransaction.Primary.Root.Item("L2a").Item("ln").Item("L2b").AddNew("CPNAME", "Command parameter name");
            secsTransaction.Primary.Root.Item("L2a").Item("ln").Item("L2b").Item("CPNAME").Format = SECS_FORMAT.ASCII;
            secsTransaction.Primary.Root.Item("L2a").Item("ln").Item("L2b").Item("CPNAME").Value = cpname;

            secsTransaction.Primary.Root.Item("L2a").Item("ln").Item("L2b").AddNew("CPVAL", "Command parameter value");
            secsTransaction.Primary.Root.Item("L2a").Item("ln").Item("L2b").Item("CPVAL").Format = SECS_FORMAT.ASCII;
            secsTransaction.Primary.Root.Item("L2a").Item("ln").Item("L2b").Item("CPVAL").Value = cpval;
            return secsTransaction;
        }



    }
}
