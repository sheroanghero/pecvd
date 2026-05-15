using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Routine;

namespace MECF.Framework.RT.ModuleLibrary.SystemModules.Routines
{
    public interface IAutoTransfer : IRoutine
    {
        bool CheckAllJobDone();
        bool IsCJExisted(string cjid);
        bool HasJobRunning { get; }

        bool CreateJob(Dictionary<string, object> namedParameter);

        void AbortJob(string cjName);
        void StopJob(string cjName);
        void ResumeJob(string cjName);
        void PauseJob(string cjName);
        void PauseAllJob();
        void ResumeAllJob();
        bool StartJob(string cjName);
        void Clear();
        void ModuleError(string moduleName);
        void Map(string lpName);
        bool CheckCJIDInListControlJobs(string jobId);
        bool CreateJobWithoutSlot(Dictionary<string, object> namedParameter);
        bool CreateControlJob(string jobId, string module, List<string> pjIDs, bool isAutoStart);

        bool CreateProcessJob(string jobId, string sequenceName, List<int> slotNumbers/*0 based*/, bool isAutoStart, string LPModule);

    }
}
