using Brooks.WinSECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.Fortrend
{
    public static class SECSTransactionBuilder
    {

        public static SECSTransaction BuildS2F15(int id, int value)
        {
            SECSTransaction secsTransaction = new SECSTransaction(2, 15);
            secsTransaction.Primary.Root.Name = "HCS";
            secsTransaction.Primary.Root.AddNew("L2", "");
            secsTransaction.Primary.Root.Item("L2").Format = SECS_FORMAT.LIST;
            secsTransaction.Primary.Root.Item("L2").AddNew("NAME", "ECID NAME");
            secsTransaction.Primary.Root.Item("L2").Item("NAME").Format = SECS_FORMAT.U1;
            secsTransaction.Primary.Root.Item("L2").Item("NAME").Value = id;

            secsTransaction.Primary.Root.Item("L2").AddNew("VALUE", "ECID VALUE");
            secsTransaction.Primary.Root.Item("L2").Item("VALUE").Format = SECS_FORMAT.U1;
            secsTransaction.Primary.Root.Item("L2").Item("VALUE").Value = value;
            return secsTransaction;
        }
        public static SECSTransaction BuildS1F5(int formCode)
        {
            SECSTransaction secsTransaction = new SECSTransaction(1, 5);
            secsTransaction.Primary.Root.Name = "FSR";
            secsTransaction.Primary.Root.AddNew("SFCD", "Status form code");
            secsTransaction.Primary.Root.Item("SFCD").Format = SECS_FORMAT.BINARY;
            secsTransaction.Primary.Root.Item("SFCD").Value = formCode;
            secsTransaction.ReplyExpected = true;
            return secsTransaction;
        }

        public static SECSTransaction BuildS2F41(int rcmd, string cpname = "", string cpval = "")
        {
            SECSTransaction secsTransaction = new SECSTransaction(2, 41);

            secsTransaction.Primary.Root.Name = "HCS";
            secsTransaction.Primary.Root.AddNew("L2", "");
            secsTransaction.Primary.Root.Item("L2").Format = SECS_FORMAT.LIST;
            secsTransaction.Primary.Root.Item("L2").AddNew("RCMD", "Remote command code");
            secsTransaction.Primary.Root.Item("L2").Item("RCMD").Format = SECS_FORMAT.U1;
            secsTransaction.Primary.Root.Item("L2").Item("RCMD").Value = rcmd;

            secsTransaction.Primary.Root.Item("L2").AddNew("Ln", "");
            secsTransaction.Primary.Root.Item("L2").Item("Ln").Format = SECS_FORMAT.LIST;

            if (cpval != "")
            {
                secsTransaction.Primary.Root.Item("L2").Item("Ln").AddNew("CPAL", "Command parameter value");
                secsTransaction.Primary.Root.Item("L2").Item("Ln").Item("CPAL").Format = SECS_FORMAT.U1;
                secsTransaction.Primary.Root.Item("L2").Item("Ln").Item("CPAL").Value = cpval;
            }
            secsTransaction.ReplyExpected = true;
            return secsTransaction;

        }

        public static SECSTransaction BuildS2F21(int rcmd, string cpname = "", string cpval = "")
        {
            SECSTransaction secsTransaction = new SECSTransaction(2, 21);

            secsTransaction.Primary.Root.Name = "HCS";
            secsTransaction.Primary.Root.AddNew("RCMD", "Remote command code");
            secsTransaction.Primary.Root.Item("RCMD").Format = SECS_FORMAT.U1;
            //secsTransaction.Primary.Root.Item("RCMD").NLB = 1;
            secsTransaction.Primary.Root.Item("RCMD").Value = rcmd;
            //secsTransaction.Primary.Root.AddNew("L2", "");
            //secsTransaction.Primary.Root.Item("L2").Format = SECS_FORMAT.LIST;
            //secsTransaction.Primary.Root.Item("L2").AddNew("RCMD", "Remote command code");
            //secsTransaction.Primary.Root.Item("L2").Item("RCMD").Format = SECS_FORMAT.U1;
            //secsTransaction.Primary.Root.Item("L2").Item("RCMD").Value = rcmd;

            //secsTransaction.Primary.Root.Item("L2").AddNew("Ln", "");
            //secsTransaction.Primary.Root.Item("L2").Item("Ln").Format = SECS_FORMAT.LIST;

            if (cpval != "")
            {
                secsTransaction.Primary.Root.Item("L2").Item("Ln").AddNew("CPAL", "Command parameter value");
                secsTransaction.Primary.Root.Item("L2").Item("Ln").Item("CPAL").Format = SECS_FORMAT.U1;
                secsTransaction.Primary.Root.Item("L2").Item("Ln").Item("CPAL").Value = cpval;
            }
            secsTransaction.ReplyExpected = true;
            return secsTransaction;

        }

        public static SECSTransaction BuildS1F3(int[] vids)
        {
            SECSTransaction secsTransaction = new SECSTransaction(1, 3);
            secsTransaction.Primary.Root.Name = "SSR";
            secsTransaction.Primary.Root.AddNew("L2", "");
            for (int i = 0; i < vids.Length; i++)
            {
                secsTransaction.Primary.Root.Item("L2").AddNew($"SVID{i + 1}");
                secsTransaction.Primary.Root.Item("L2").Item($"SVID{i + 1}").Format = SECS_FORMAT.U1;
                secsTransaction.Primary.Root.Item("L2").Item($"SVID{i + 1}").NLB = 1;
                secsTransaction.Primary.Root.Item("L2").Item($"SVID{i + 1}").Value = vids[i];
            }

            secsTransaction.ReplyExpected = true;
            return secsTransaction;
        }

    }
}
