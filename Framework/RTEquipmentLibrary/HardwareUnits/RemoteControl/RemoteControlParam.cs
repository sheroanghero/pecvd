using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RemoteControl
{
    /// <summary>
    ///  message header for any message
    /// </summary>
    public class head_t
    {
        public UInt32 message_id;
        public UInt32 command_id;
        public string sender_id;
        public string receiver_id;
        public UInt32 body_item_qty;
        public head_t()
        {
        }

    };

    /// <summary>
    /// common header receiving fields
    /// </summary>
    public class mxrh_t
    {
        // extracted fields from reply text message
        public string[] items;

        // parsed items
        public head_t h;

        public mxrh_t()
        {
            this.items = null;
            this.h = new head_t();
        }
    }



    // message 1 get status query
    public class m1_t
    {
        public head_t h;
        public m1_t()
        {
            this.h = new head_t();
        }
    }


    // message 101 get status reply
    public class m101r_t
    {
        public mxrh_t mxrh; // rx_header
        public Int32 error_code;
        public string error_message;

        public m101r_t()
        {
            this.mxrh = new mxrh_t();
        }
    }

    // message 2 start measurement query
    public class m2_t
    {
        public head_t h;
        public string recipe;
        public UInt32 slot;
        public string lot;
        public string cassette;
        public string techno;
        public m2_t()
        {
            this.h = new head_t();
        }
    }


    // message 102 start measurement reply
    public class m102r_t
    {
        public mxrh_t mxrh; // rx_header
        public Int32 error_code;
        public string error_message;

        public m102r_t()
        {
            this.mxrh = new mxrh_t();
        }
    }


    // message 3 get recipe list query
    public class m3_t
    {
        public head_t h;
        public m3_t()
        {
            this.h = new head_t();
        }
    }

    // message 103 get recipe list reply
    public class m103r_t
    {
        public mxrh_t mxrh; // rx_header
        public Int32 error_code;
        public string error_message;
        public UInt32 recipe_qty;
        public string[] recipes;

        public m103r_t()
        {
            this.mxrh = new mxrh_t();
            this.recipe_qty = 0;
            this.recipes = null;
        }
    }

    // message 4 stop measurement query
    public class m4_t
    {
        public head_t h;
        public string action;
        public m4_t()
        {
            this.h = new head_t();
        }
    }


    // message 104 stop measurement reply
    public class m104r_t
    {
        public mxrh_t mxrh; // rx_header
        public Int32 error_code;
        public string error_message;

        public m104r_t()
        {
            this.mxrh = new mxrh_t();
        }
    }
}
