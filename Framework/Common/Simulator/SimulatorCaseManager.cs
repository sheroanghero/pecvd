using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Simulator
{
    public class SimulatorCaseManager : Singleton<SimulatorCaseManager>
    {
        private const string FileName = "ExceptionCase.Obj";
        private PeriodicJob _thread;
        private Dictionary<string, bool> _dicValue = new Dictionary<string, bool>(); 

        public SimulatorCaseManager()
        {

        }

        public void Initialize()
        {
            if (File.Exists(FileName))
            {
                SerializeStatic.Load(typeof (ExceptionCase), FileName);
            }

            foreach (var propertyInfo in typeof(ExceptionCase).GetProperties())
            {
                _dicValue[propertyInfo.Name] = (bool)propertyInfo.GetValue(null, null);
            }
            _thread = new PeriodicJob(1000, OnMonitor, "simualtor case serializer", true);
        }

        bool OnMonitor()
        {
            try
            {
                foreach (var propertyInfo in typeof(ExceptionCase).GetProperties())
                {
                    if (_dicValue[propertyInfo.Name] != (bool)propertyInfo.GetValue(null, null))
                    {
                        SerializeStatic.Save(typeof (ExceptionCase), FileName);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                
                LOG.Write(ex);
            }

            return true;
        }
        

    }
}
