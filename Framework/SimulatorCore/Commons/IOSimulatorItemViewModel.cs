using Aitex.Core.UI.MVVM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class IOSimulatorItemViewModel: ViewModelBase
    {
        public string SourceCommandName { get; set; }//key
        public string SourceCommand { get; set; }
        public string SourceCommandType { get; set; }

        private string _commandContent;
        public string CommandContent
        {
            get { return _commandContent; }
            set
            {
                _commandContent = value;
                InvokePropertyChanged("CommandContent");
            }
        }


        private DateTime _commandRecievedTime;
        public DateTime CommandRecievedTime
        {
            get { return _commandRecievedTime; }
            set
            {
                _commandRecievedTime = value;
                InvokePropertyChanged("CommandRecievedTime");
            }
        }


        private string _response;
        public string Response
        {
            get { return _response; }
            set
            {
                _response = value;
                InvokePropertyChanged("Response");
            }
        }


        public string SuccessResponseStr
        { 
            get
            { 
                if(SuccessResponse != null)
                {
                    var sResponse = SuccessResponse.ToString();
                    if (sResponse.Contains('{'))
                    {
                        return JsonConvert.SerializeObject(SuccessResponse);
                    }
                    else
                    {
                        return sResponse;
                    }
                }
                return null;
            }
        }
        public string FailedResponseStr
        { 
            get
            { 
                if(FailedResponse != null)
                {
                    var sResponse = FailedResponse.ToString();
                    if (sResponse.Contains('{'))
                    {
                        return JsonConvert.SerializeObject(FailedResponse);
                    }
                    else
                    {
                        return sResponse;
                    }
                }
                return null;
            }
        }

        public object SuccessResponse { get; set; }//value, as default response
        public object FailedResponse { get; set; }//value, as default response

        //public int AutoReplyTimeout { get; set; }

        private bool _isManualReplyEnable;

        public bool IsManualReplyEnable
        {
            get { return _isManualReplyEnable; }
            set { 
                _isManualReplyEnable = value;
                InvokePropertyChanged("IsManualReplyEnable");
            }
        }
    }

    public class IOSimulatorItemViewModelConfig
    {
        public List<IOSimulatorItemViewModel> IOSimulatorItemList { get; set; }
    }
}
