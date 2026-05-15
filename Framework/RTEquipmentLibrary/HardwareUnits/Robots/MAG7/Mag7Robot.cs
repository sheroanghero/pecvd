using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using System.Text.RegularExpressions;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.MAG7
{
    public class Mag7Robot : Robot
    {
        public bool alignerError;

        public override bool Error
        {
            get
            {
                return _commErr || _exceuteErr;
            }
        }

        public Mag7Robot(string module, string address)
            : base(module, module, module, module, address, RobotType.MAG7)
        {
        }

        public Mag7Robot(string module, string port, int baudRate)
            : base(module, module, module, module, port, RobotType.MAG7, baudRate)
        {
        }

        public override void OnDataChanged(string package)
        {
            try
            {   
                package = package.ToUpper();

                #region Aligner Error
                if (package.StartsWith("ALGN _ERR"))
                {
                    if (package.EndsWith("_RDY\r"))  // "ALGN _ERR 0804\r_RDY\r"
                    {
                        package = package.Substring(0, package.Length - 5);
                        alignerError = false;
                    }
                    else                      // "ALGN _ERR 0804\r"
                    {
                        alignerError = true;
                    }

                }
                else if (package == "_RDY\r")  
                {
                    if (alignerError)  // 下一个_RDY忽略
                    {
                        alignerError = false;
                        return;
                    }
                    else
                    {
                        alignerError = false;
                    }                                      
                }
                #endregion

                string[] msgs = Regex.Split(package, delimiter);

                foreach (string msg in msgs)
                {
                    if (msg.Length > 0)
                    {
                        bool completed = false;
                        string resp = msg;

                        lock (_locker)
                        {
                            if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                if (completed)
                                {
                                    _foregroundHandler = null;
                                }
                            }
                            else if (_backgroundHandler != null && _backgroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                if (completed)
                                {
                                    string reason = string.Empty;
                                    QueryState(out reason);
                                    _backgroundHandler = null;
                                }
                            }
                            else
                            {
                                if (_eventHandler != null)
                                {
                                    if (_eventHandler.OnMessage(ref _socket, resp, out completed))
                                    {
                                        if (completed)
                                        {
                                            EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format(" has error. {0:X}", ErrorCode));
                                            _exceuteErr = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ExcuteFailedException e)
            {
                if (e.Message.StartsWith("Aligner"))
                {
                    EV.PostMessage("Aligner", EventEnum.DefaultAlarm, string.Format(" Aligner execute failed, {0}", e.Message.Substring(8)));
                }
                else
                {
                    EV.PostMessage("TM", EventEnum.DefaultAlarm, string.Format(" Robot execute failed, {0}", e.Message));
                }
                _exceuteErr = true;

                if (_foregroundHandler != null)
                {
                    _foregroundHandler = null;
                }
                else if (_backgroundHandler != null)
                {
                    _backgroundHandler = null;
                }
            }
            catch (InvalidPackageException e)
            {
                EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format(" receive invalid package. {0}", e.Message));
            }
            catch (System.Exception ex)
            {
                _commErr = true;
                LOG.Write("Robot failed：" + ex.ToString());
            }
        }

        public override void Reset()
        {
            _exceuteErr = false;
            if (_commErr)
            {
                Connect();
            }
            Swap = false;
        }
    }
}
