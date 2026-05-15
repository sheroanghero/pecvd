using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.FAServices.E40s;
using MECF.Framework.Common.FAServices.E94s;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.ClientBase;

//using Aitex.Sorter.UI.ViewModel;

namespace MECF.Framework.UI.Client.CenterViews.Operations.MonitorJob
{
    public class MonitorJobViewModel : UiViewModelBase
    {
        public bool IsPermission { get => this.Permission == 3; }

        public class ProcessJobData : NotifiableItem
        {
            [DataMember]
            public string ObjtID { get; set; }
            [DataMember]
            public string State { get; set; }
            [DataMember]
            public bool Auto { get; set; }
            [DataMember]
            public string RecID { get; set; }
            [DataMember]
            public DateTime CreateTime { get; set; }
            [DataMember]
            public DateTime CompleteTime { get; set; }
        }

        public class ControlJobData : NotifiableItem
        {
            [DataMember]
            public int Num { get; set; }
            [DataMember]
            public string ObjtID { get; set; }
            [DataMember]
            public string State { get; set; }
            [DataMember]
            public bool Auto { get; set; }
            [DataMember]
            public DateTime CreateTime { get; set; }
            [DataMember]
            public DateTime CompleteTime { get; set; }
        }

        public ObservableCollection<ProcessJobData> ProcessJobsData { get; set; }
        public ObservableCollection<ControlJobData> ControlJobsData { get; set; }


        public ICommand ControlJobsCommand { get; set; }

        public ICommand ProcessJobsCommand { get; set; }

        public MonitorJobViewModel()/* : base("MonitorJobViewModel")*/
        {
            ControlJobsData = new ObservableCollection<ControlJobData>();
            ProcessJobsData = new ObservableCollection<ProcessJobData>();

            ControlJobsCommand = new DelegateCommand<object>(DoControlJobsCommand);
            ProcessJobsCommand = new DelegateCommand<object>(DoProcessJobsCommand);
        }

        [Subscription("ProcessJobs")]
        public List<WCFProcessJobInterface> ProcessJobs
        {
            get;
            set;
        }

        [Subscription("ControlJobs")]
        public List<WCFControlJobInterface> ControlJobs
        {
            get;
            set;
        }

        public string SelectedControlJobsObjtID
        {
            get
            {
                return SelectedControlJob != null ? SelectedControlJob.ObjtID : "";
            }
        }
        public ControlJobData SelectedControlJob { get; set; }

        public string SelectedProcessJobsObjtID
        {
            get
            {
                return SelectedProcessJob!=null?SelectedProcessJob.ObjtID:"";
            }
        }

        public ProcessJobData SelectedProcessJob { get; set; }
 
        private void DoProcessJobsCommand(object obj)
        {
            if (string.IsNullOrEmpty(SelectedProcessJobsObjtID))
            {
                MessageBox.Show("No process job selected.");
                return;
            }
            var objList = new object[] { obj, SelectedProcessJobsObjtID };

            Common.OperationCenter.InvokeClient.Instance.Service.DoOperation("FAProcessJobsOperation", objList);

        }

        private void DoControlJobsCommand(object obj)
        {
            if (string.IsNullOrEmpty(SelectedControlJobsObjtID))
            {
                MessageBox.Show("No control job selected.");
                return;
            }
            var objList = new object[] { obj, SelectedControlJobsObjtID };

            Common.OperationCenter.InvokeClient.Instance.Service.DoOperation("FAControlJobsOperation", objList);
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            base.InvokeAfterUpdateProperty(data);

            if (ProcessJobs == null || ProcessJobs.Count == 0)
            {
                ProcessJobsData.Clear();
            }
            else
            {
                foreach (var item in ProcessJobs)
                {
                    var match = ProcessJobsData.ToList().Find(x => x.ObjtID == item.ObjtID);
                    if (match == null)
                    {
                        ProcessJobData pj = new ProcessJobData();
                        pj.ObjtID = item.ObjtID;
                        pj.State = item.PJstate.ToString();
                        pj.Auto = item.PRProcessStart;
                        pj.RecID = item.RecID;
                        pj.CreateTime = item.CreateTime;
                        pj.CompleteTime = item.CompleteTime;
                        ProcessJobsData.Add(pj);
                    }
                    else
                    {
                        match.ObjtID = item.ObjtID;
                        match.State = item.PJstate.ToString();
                        match.Auto = item.PRProcessStart;
                        match.RecID = item.RecID;
                        match.CreateTime = item.CreateTime;
                        match.CompleteTime = item.CompleteTime;

                        match.InvokePropertyChanged();
                    }
                }

                List<ProcessJobData> tobeRemove = new List<ProcessJobData>();
                foreach (var processJobData in ProcessJobsData)
                {
                    if (ProcessJobs.FirstOrDefault(x=>x.ObjtID==processJobData.ObjtID) == null)
                        tobeRemove.Add(processJobData);
                }

                foreach (var processJobData in tobeRemove)
                {
                    ProcessJobsData.Remove(processJobData);
                }
            }




            if (ControlJobs == null || ControlJobs.Count == 0)
            {
                ControlJobsData.Clear();
            }
            else
            {
                foreach (var item in ControlJobs)
                {
                    var match = ControlJobsData.ToList().Find(x => x.ObjtID == item.ObjtID);
                    if (match == null)
                    {
                        ControlJobData cj = new ControlJobData();
                        cj.Num = ControlJobsData.Count + 1;
                        cj.ObjtID = item.ObjtID;
                        cj.State = item.state.ToString();
                        cj.Auto = item.StartMethod;
                        cj.CreateTime = item.CreateTime;
                        cj.CompleteTime = item.CompleteTime;
                        ControlJobsData.Add(cj);
                    }
                    else
                    {
                        match.State = item.state.ToString();
                        match.Auto = item.StartMethod;
                        match.CreateTime = match.CreateTime;
                        match.CompleteTime = match.CompleteTime;

                        match.InvokePropertyChanged();
                    }
                }

                List<ControlJobData> tobeRemove = new List<ControlJobData>();
                foreach (var cj in ControlJobsData)
                {
                    if (ControlJobs.FirstOrDefault(x => x.ObjtID == cj.ObjtID) == null)
                        tobeRemove.Add(cj);
                }

                foreach (var cj in tobeRemove)
                {
                    ControlJobsData.Remove(cj);
                }
            }
        }
 
    }

 
}
