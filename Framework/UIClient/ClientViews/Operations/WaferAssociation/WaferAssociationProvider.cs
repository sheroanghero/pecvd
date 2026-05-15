using System.Collections.Generic;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.ClientBase.ServiceProvider;

namespace MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation
{
    public class WaferAssociationProvider : IProvider
    {
        private static WaferAssociationProvider _Instance = null;
        public static WaferAssociationProvider Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new WaferAssociationProvider();

                return _Instance;
            }
        }

        public void Create()
        {

        }

        public void CreateJob(string jobId, string module, List<string> slotSequence, bool autoStart, bool isCycleMode, int waferCount)
        {
            Dictionary<string, object> param = new Dictionary<string, object>()
            {
                {"JobId", jobId},
                {"Module", module},
                {"SlotSequence", slotSequence.ToArray()},
                {"AutoStart", autoStart},
                {"IsCycleMode", isCycleMode},
                {"WaferCount", waferCount},
            };
            InvokeClient.Instance.Service.DoOperation("System.CreateJob", param);
        }

        public void AbortJob(string jobID)
        {
            var param = new object[] { jobID };
            InvokeClient.Instance.Service.DoOperation("System.AbortJob", param);
        }

        public void Start(string jobID)
        {
            var param = new object[] { jobID };
            InvokeClient.Instance.Service.DoOperation("System.StartJob", param);
        }

        public void Pause(string jobID)
        {
            var param = new object[] { jobID };
            InvokeClient.Instance.Service.DoOperation("System.PauseJob", param);
        }

        public void Resume(string jobID)
        {
            var param = new object[] { jobID };
            InvokeClient.Instance.Service.DoOperation("System.ResumeJob", param);
        }

        public void PauseAllJob()
        {
            InvokeClient.Instance.Service.DoOperation("System.PauseAllJob");
        }

        public void ResumeAllJob()
        {
            InvokeClient.Instance.Service.DoOperation("System.ResumeAllJob");
        }

        public void Stop(string jobID)
        {
            var param = new object[] { jobID };
            InvokeClient.Instance.Service.DoOperation("System.StopJob", param);
        }
    }
}
