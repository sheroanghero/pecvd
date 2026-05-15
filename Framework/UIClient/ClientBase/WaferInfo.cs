using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Caliburn.Micro.Core;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferInfo : PropertyChangedBase
    {
        private int _WaferStatus = 0;   // WaferStatus.Empty;
        public int WaferStatus
        {
            get { return _WaferStatus; }
            set { _WaferStatus = value; NotifyOfPropertyChange("WaferStatus"); }
        }

        /// <summary>
        /// SlotID start from 0
        /// </summary>
        private int _slotID;
        public int SlotID
        {
            get { return _slotID; }
            set { _slotID = value; NotifyOfPropertyChange("SlotID"); }
        }

        /// <summary>
        /// SlotIndex start from 1
        /// </summary>
        private int _slotIndex;
        public int SlotIndex
        {
            get { return _slotIndex; }
            set { _slotIndex = value; NotifyOfPropertyChange("SlotIndex"); }    
        }

        private string _moduleID;
        public string ModuleID
        {
            get { return _moduleID; }
            set { _moduleID = value; NotifyOfPropertyChange("ModuleID"); }
        }

        private string _waferid;
        public string WaferID
        {
            get { return _waferid; }
            set { _waferid = value; NotifyOfPropertyChange("WaferID"); }
        }

        private string _sourceName;
        public string SourceName
        {
            get { return _sourceName; }
            set { _sourceName = value; NotifyOfPropertyChange("SourceName"); }
        }

        private string _sequenceName = string.Empty;
        public string SequenceName
        {
            get { return _sequenceName; }
            set { _sequenceName = value; NotifyOfPropertyChange("SequenceName"); }
        }

        private string _originName = string.Empty;
        public string OriginName
        {
            get { return _originName; }
            set { _originName = value; NotifyOfPropertyChange("OriginName"); }
        }

        private bool isChecked;
        public bool IsChecked
        {
            get
            { return isChecked; }
            set
            {
                isChecked = value;
                NotifyOfPropertyChange("IsChecked");
            }
        }
        private WaferType waferType;
        public WaferType WaferType
        {
            get
            { return waferType; }
            set
            {
                waferType = value;
                NotifyOfPropertyChange("WaferType");
            }
        }
        private float useCount;
        public float UseCount
        {
            get
            { return useCount; }
            set
            {
                useCount = value;
                NotifyOfPropertyChange("UseCount");
            }
        }
        private float useTime;
        public float UseTime
        {
            get
            { return useTime; }
            set
            {
                useTime = value;
                NotifyOfPropertyChange("UseTime");
            }
        }
        private float useThick;
        public float UseThick
        {
            get
            { return useThick; }
            set
            {
                useThick = value;
                NotifyOfPropertyChange("UseThick");
            }
        }
    }
}
