using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Event
{
    [DataContract]
    [Serializable]
    public enum EventLevel
    {
        [EnumMember]
        Information = 0,
        [EnumMember]
        Warning,
        [EnumMember]
        Alarm
    }

    [DataContract]
    [Serializable]
    public enum EventType
    {
        /// <summary>
        /// 在TopView显示一条记录
        /// </summary>
        [EnumMember]
        EventUI_Notify,

        /// <summary>
        /// 弹出对话框
        /// </summary>
        [EnumMember]
        Dialog_Nofity,

        /// <summary>
        /// 
        /// </summary>
        [EnumMember]
        KickOut_Notify,

        /// <summary>
        /// 
        /// </summary>
        [EnumMember]
        Sound_Notify,

        /// <summary>
        /// 在屏幕左下角弹出一个提醒的悬浮窗口，自动消失
        /// </summary>
        [EnumMember]
        UIMessage_Notify,

        [EnumMember]
        OperationLog,

        [EnumMember]
        HostNotification,
    }
    [DataContract]
    [Serializable]
    public enum EventAction
    {
        [EnumMember]
        Clear = 0,
        [EnumMember]
        Retry,
        [EnumMember]
        ClearAndRetry
    }
    [DataContract]
    [Serializable]
    public enum EventGroup
    {
        [EnumMember]
        Group0 = 0,
        [EnumMember]
        Group1,
        [EnumMember]
        Group2,
        [EnumMember]
        Group3,
        [EnumMember]
        Group4,
        [EnumMember]
        Group5,
        [EnumMember]
        Group6,
        [EnumMember]
        Group7,
        [EnumMember]
        Group8,
        [EnumMember]
        Group9,
        [EnumMember]
        Group10,
    }


    [DataContract]
    [Serializable]
    public class EventItem
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public int RoleId { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string DisplayName { get; set; } = "";
        [DataMember]
        public string EventEnum { get; set; }                    //
        [DataMember]
        public EventType Type { get; set; } = EventType.EventUI_Notify;
        [DataMember]
        public EventLevel Level { get; set; } = EventLevel.Information;
        [DataMember]
        public string Source { get; set; }                  //Which module send out this event
        [DataMember]
        public string Description { get; set; } = "";            //detail text
        [DataMember]
        public string AdditionalDescription { get; set; } = "";  //补充描述
        [DataMember]
        public DateTime OccuringTime { get; set; }          //
        [DataMember]
        public bool IsAcknowledged { get; set; }
        [DataMember]
        public string Explaination { get; set; } = "No information available";
        [DataMember]
        public string Solution { get; set; } = "No information available";
        [DataMember]
        public int RetryMessage { get; set; }
        [DataMember]
        public object[] RetryMessageParas { get; set; }
        [DataMember]
        public int Count { get; set; }//发生的次数
        [DataMember]
        public EventAction Action { get; set; } = EventAction.Clear;//处理方式
        [DataMember]
        public bool Bypass { get; set; } = false;//是否忽略
        [DataMember]
        public bool AutoRecovery { get; set; } = false;//是否自动清除
        [DataMember]
        public EventGroup Group { get; set; } = EventGroup.Group0;//分组
        [DataMember]
        public string Category { get; set; } = "";
        [DataMember]
        public bool Editable { get; set; } = true;//是否可修改

        [DataMember]
        public string GlobalDescription_en
        {
            get;
            set;
        }

        [DataMember]
        public string GlobalDescription_zh
        {
            get;
            set;
        }

        [DataMember]
        public SerializableDictionary<string, string> DVID
        {
            get;
            set;
        }

        public SerializableDictionary<string, object> ObjDVID
        {
            get;
            set;
        }

        public bool IsTriggered
        {
            get
            {
                return !IsAcknowledged;
            }
        }

        public EventItem Clone()
        {
            EventItem result = new EventItem();
            result.Description = Description;
            result.EventEnum = EventEnum;
            result.Explaination = Explaination;
            result.Id = Id;
            result.RoleId = RoleId;
            result.UserName = UserName;
            result.DisplayName = DisplayName;
            result.IsAcknowledged = IsAcknowledged;
            result.Level = Level;
            result.OccuringTime = OccuringTime;
            result.Solution = Solution;
            result.Source = Source;
            result.Type = Type;
            result.RetryMessage = RetryMessage;
            result.Count = Count;
            result.Action = Action;
            result.Bypass = Bypass;
            result.AutoRecovery = AutoRecovery;
            result.Group = Group;
            result.Editable = Editable;
            result.Category = Category;

            result.GlobalDescription_en = GlobalDescription_en;
            result.GlobalDescription_zh = GlobalDescription_zh;

            result.DVID = DVID;
            result.ObjDVID = ObjDVID;

            return result;
        }
        public void Clone(EventItem item)
        {
            this.Description = item.Description;
            this.EventEnum = item.EventEnum;
            this.Explaination = item.Explaination;
            this.Id = item.Id;
            this.RoleId = item.RoleId;
            this.UserName = item.UserName;
            this.DisplayName = item.DisplayName;
            this.IsAcknowledged = item.IsAcknowledged;
            this.Level = item.Level;
            this.OccuringTime = item.OccuringTime;
            this.Solution = item.Solution;
            this.Source = item.Source;
            this.Type = item.Type;
            this.RetryMessage = item.RetryMessage;
            this.Count = item.Count;
            this.Action = item.Action;
            this.Bypass = item.Bypass;
            this.AutoRecovery = item.AutoRecovery;
            this.Group = item.Group;
            this.Editable = item.Editable;
            this.Category = item.Category;

            this.GlobalDescription_en = item.GlobalDescription_en;
            this.GlobalDescription_zh = item.GlobalDescription_zh;

            this.DVID = item.DVID;
            this.ObjDVID = item.ObjDVID;
        }

        public EventItem Clone(SerializableDictionary<string, object> objDvid)
        {
            EventItem result = new EventItem();
            result.Description = Description;
            result.EventEnum = EventEnum;
            result.Explaination = Explaination;
            result.Id = Id;
            result.RoleId = RoleId;
            result.UserName = UserName;
            result.DisplayName = DisplayName;
            result.IsAcknowledged = IsAcknowledged;
            result.Level = Level;
            result.OccuringTime = OccuringTime;
            result.Solution = Solution;
            result.Source = Source;
            result.Type = Type;
            result.RetryMessage = RetryMessage;
            result.Count = Count;
            result.Action = Action;
            result.Bypass = Bypass;
            result.AutoRecovery = AutoRecovery;
            result.Group = Group;
            result.Editable = Editable;
            result.Category = Category;

            result.GlobalDescription_en = GlobalDescription_en;
            result.GlobalDescription_zh = GlobalDescription_zh;

            result.DVID = DVID;
            result.ObjDVID = objDvid;

            return result;
        }

        public EventItem()
        {

        }
        public EventItem(string name)
            : this(0, "System", name, "", EventLevel.Information, EventType.EventUI_Notify)
        {
        }

        public EventItem(string name, string description)
            : this(0, "System", name, description, EventLevel.Information, EventType.EventUI_Notify)
        {
        }

        public EventItem(string source, string name, string description)
        : this(0, source, name, description, EventLevel.Information, EventType.EventUI_Notify)
        {
        }

        public EventItem(string name, EventType type, EventLevel level)
            : this(0, "System", name, "", level, type)
        {
        }

        public EventItem(string source, string name, string description, EventLevel level, EventType type)
            : this(0, source, name, description, level, type)
        {
        }

        public EventItem(int id, string source, string name, string description, EventLevel level, EventType type)
        {
            EventEnum = name;
            Type = type;
            Level = level;
            Source = source;
            Description = description;
            Id = id;
        }
    }
}
