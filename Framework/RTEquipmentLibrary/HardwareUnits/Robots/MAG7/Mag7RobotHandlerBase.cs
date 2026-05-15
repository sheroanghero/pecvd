using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.MAG7
{
    public class Mag7RobotHandlerBase<T> : IHandler where T : ITransferMsg, new()
    {
        public int ID { get; set; }
        public int Unit { get; set; }

        public bool IsBackground
        {
            get
            {
                return !_seqMode && _imp.background;
            }
        }


        private static int retry_time = 3;
        private int retry_count = retry_time;

        private object[] _objs = null;



        private TokenGenerator _generator;

        private T _imp = new T();
        private Robot _device;
        private bool _seqMode = true;

        public Mag7RobotHandlerBase(IDevice device)
        {
            _device = (Robot)device;
            _imp.Robot = device;
        }

        public Mag7RobotHandlerBase(IDevice device, ref TokenGenerator gen, params object[] objs)
        {
            _device = (Robot)device;
            _imp.Robot = device;
            this._generator = gen;
            this._objs = objs;
        }

        public bool Execute<TPort>(ref TPort port) where TPort : ICommunication
        {
            retry_count = retry_time;
            ID = _generator.create();
            return port.Write(string.Format("{0}{1}", package(), ProtocolTag.tag_end));
        }
        /// <summary>
        /// return value: bhandle
        /// </summary>
        /// <typeparam name="TPort"></typeparam>
        /// <param name="port"></param>
        /// <param name="msg"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        /// 

        public bool OnMessage<TPort>(ref TPort port, string message, out bool completed) where TPort : ICommunication
        {
            try
            {
                completed = false;

                string package = message;
                string[] words = Regex.Split(package, ProtocolTag.cmd_token);
                string type = words[0];

                if (type == ProtocolTag.resp_tag_error)
                {
                    int error = int.Parse(words[1]);
                    _device.LastErrorCode = error;
                    if (error != 0)  //can't retry
                    {
                        string warning = string.Format("Error code {0:D4}", error);
                        LOG.Warning(warning);

                        completed = _imp.unpackage(type, words);

                        throw (new ExcuteFailedException(warning));
                    }
                    if (retry_count-- <= 0)
                    {
                        string warning = string.Format("retry over {0} times", retry_time);
                        LOG.Warning(warning);
                        throw (new ExcuteFailedException(warning));
                    }

                    return true;
                }
                else if (type == ProtocolTag.resp_tag_event)
                {
                    string evtType = words[3];
                    string evtInfo = words[5];

                    if (_imp.evt)
                        completed = _imp.unpackage(type, words);
                    return _imp.evt;
                }
                else if (type == ProtocolTag.resp_tag_excute)
                {
                    _device.AlignFinished = true;

                    completed = _imp.unpackage(type, words);
                    if (completed)
                    {
                        _generator.release(ID);
                        return true;
                    }

                    return true;
                }
                else if (type == ProtocolTag.resp_tage_alignerError)
                {
                    string err = words[1];
                    int error = int.Parse(words[2]);
                    _device.LastErrorCode = error;

                    if (err == ProtocolTag.resp_tag_error)
                    {
                        _device.AlignFinished = false;

                        if (error != 0)  //can't retry
                        {
                            string warning = string.Format("Aligner Error code {0:D4}", error);
                            LOG.Warning(warning);
                            
                            completed = _imp.unpackage(words[1], words);

                            if (_device.RetryCounter < _device.MaxRetryCounter)
                            {
                                LOG.Write($"Current retry Align {_device.RetryCounter + 1} / {_device.MaxRetryCounter} time.");

                                if (completed)
                                {
                                    _generator.release(ID);
                                    return true;
                                }
                            }
                            else
                            {
                                throw (new ExcuteFailedException(warning));
                            }
                        }
                        if (retry_count-- <= 0)
                        {
                            string warning = string.Format("retry over {0} times", retry_time);
                            LOG.Warning(warning);
                            throw (new ExcuteFailedException(warning));
                        }
                    }

                    return true;
                }
                else
                {
                    completed = _imp.unpackage(type, words);
                    if (completed)
                    {
                        _generator.release(ID);
                        return true;
                    }

                    return true;
                }

            }
            catch (ExcuteFailedException e)
            {
                throw (e);
            }
            catch (InvalidPackageException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                throw (new InvalidPackageException(message));
            }
        }


        private string package()
        {
            //Commands<CR>
            string data = string.Empty;
            data = _imp.package(this._objs);
            return data;
        }
    }

}
