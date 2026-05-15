using System;
using System.Text.RegularExpressions;
using System.Threading;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots.HAtm
{

    public class HAtmRobotSimulator : SocketDeviceSimulator
    {
        protected Random _rd = new Random();
        public bool IsError { get; set; }
        public bool IsBusy { get; set; }
        public bool IsWaferPresent { get; set; }
        public string ResultValue { get; set; }

        private const string EOT = "\n";

        private int _targetThetaAxisAddressPosition;
        private int _targetZAxisAddressPosition;
        private DeviceTimer _devicetimer = new DeviceTimer();
        private DeviceTimer _waferTimer = new DeviceTimer();
        private DeviceTimer _homeTimer = new DeviceTimer();
        private int _waferPresentTime = 3700;
        private int _moveTime = 5000;
        private bool _isAllCommandsHaveResponse;
        private bool _isUp;

        public HAtmRobotSimulator(int port = 9002)
            :base(port, 1, "\r", ',')
        {
            //Action Command
            AddCommandHandler("HM", HandleMHOM);

            AddCommandHandler("D", HandleAbort);
            AddCommandHandler("ER", HandleError);
            AddCommandHandler("PK", HandlePick);
            AddCommandHandler("PL", HandlePlace);
            AddCommandHandler("GO", HandleGo);
            AddCommandHandler("GZ", HandleGz);
            AddCommandHandler("AA", HandleQueryAllAxisStatus);
            AddCommandHandler("TA", HandleQueryThetaAxisStatus);
            AddCommandHandler("ZA", HandleQueryZAxisStatus);
            AddCommandHandler("RA", HandleQueryRadialAxisStatus);
            AddCommandHandler("OS", HandleQueryOperationalStatus);
            AddCommandHandler("GP", HandleQueryPosition);
            AddCommandHandler("RR", HandleSetCommandResponse);
            AddCommandHandler("VN", HandleVacuumOff);
            AddCommandHandler("VY", HandleVacuumOn);
            AddCommandHandler("MH", HandleSetSpeed);
            AddCommandHandler("MP", HandleWaferMap);

            AddCommandHandler("Unknown", HandleUnknown);
        }

        public void HandleAbort(string obj)
        {

        }

        public void HandleError(string obj)
        {
            string[] datas = Regex.Split(obj.Replace("\r", "").Replace("\n", ""), ",");
            string ack = string.Format($"X,ER,OK{EOT}");
            if (datas.Length >= 2 && datas[0] == "R")
            {
                
                if (IsError)
                {
                    ack = string.Format("X,ER,{0},{1},{2},{3}{4}",
                "A0",
                "A1",
                "A2",
                "A3",
                EOT);
                }
            }

            OnWriteMessage(ack);
        }

        public void HandlePick(string obj)
        {
            string[] datas = Regex.Split(obj.Replace("\r", "").Replace("\n", ""), ",");
            if(datas.Length >=4)
            {
                int.TryParse(datas[2], out _targetThetaAxisAddressPosition);
                int.TryParse(datas[3], out _targetZAxisAddressPosition);
            }

            IsWaferPresent = false;
            IsBusy = true;
            if (!_devicetimer.IsIdle())
                _devicetimer.Stop();
            _devicetimer.Start(0);
            if (!_waferTimer.IsIdle())
                _waferTimer.Stop();
            _waferTimer.Start(0);
            if(_isAllCommandsHaveResponse)
                OnWriteMessage($"X,PK{EOT}");
        }

        public void HandlePlace(string obj)
        {
            string[] datas = Regex.Split(obj.Replace("\r", "").Replace("\n", ""), ",");
            if (datas.Length >= 4)
            {
                int.TryParse(datas[2], out _targetThetaAxisAddressPosition);
                int.TryParse(datas[3], out _targetZAxisAddressPosition);
            }

            IsWaferPresent = true;
            IsBusy = true;
            if (!_devicetimer.IsIdle())
                _devicetimer.Stop();
            _devicetimer.Start(0);
            if (!_waferTimer.IsIdle())
                _waferTimer.Stop();
            _waferTimer.Start(0);
            if (_isAllCommandsHaveResponse)
                OnWriteMessage($"X,PL{EOT}");
        }

        public void HandleGz(string obj)
        {
            if (obj.Contains("U"))
                _isUp = true;
            else
                _isUp = false;

            IsBusy = true;
            _devicetimer.Start(0);
            if (_isAllCommandsHaveResponse)
                OnWriteMessage($"X,GZ{EOT}");
        }

        public void HandleGo(string obj)
        {
            string[] datas = Regex.Split(obj.Replace("\r", "").Replace("\n", ""), ",");
            if (datas.Length >= 5)
            {
                var data4 = datas[4].Replace("U", "").Replace("D", "");
                int.TryParse(datas[3], out _targetThetaAxisAddressPosition);
                int.TryParse(data4, out _targetZAxisAddressPosition);
            }

            IsBusy = true;
            _devicetimer.Start(0);
            if (_isAllCommandsHaveResponse)
                OnWriteMessage($"X,GO{EOT}");
        }

        public void HandleQueryAllAxisStatus(string obj)
        {
            Thread.Sleep(200);
            string ack = string.Format("X,AA,{0}{1:D2}{2:D2}{3}{4}",
                "RE",
                _targetThetaAxisAddressPosition,
                _targetZAxisAddressPosition,
                _isUp ? "U" : "D",
                EOT);
            OnWriteMessage(ack);
        }

        public void HandleQueryRadialAxisStatus(string obj)
        {
            Thread.Sleep(200);
            string ack = string.Format("X,RA,{0}{1}{2}{3}{4}{5}",
                IsBusy ? "B" : "R",
                "R",
                "RE",
                "RE",
                " ",
                EOT);
            OnWriteMessage(ack);
        }

        public void HandleQueryThetaAxisStatus(string obj)
        {
            Thread.Sleep(200);
            string ack = string.Format("X,TA,{0}{1}{2:D2}{3}{4}{5}",
                IsBusy ? "B" : "R",
                "R",
                _targetThetaAxisAddressPosition,
                "  ",
                " ",
                EOT);
            OnWriteMessage(ack);
        }

        public void HandleQueryZAxisStatus(string obj)
        {
            Thread.Sleep(200);
            string ack = string.Format("X,ZA,{0}{1}{2:D2}{3}{4}{5}{6}{7}",
                IsBusy ? "B" : "R",
                "R",
                _targetZAxisAddressPosition,
                _isUp ? "U" : "D",
                "  ",
                "U",
                " ",
                EOT);
            OnWriteMessage(ack);
        }

        public void HandleQueryOperationalStatus(string obj)
        {
            if (!_waferTimer.IsIdle())
            {
                if (_waferTimer.GetElapseTime() > _waferPresentTime)
                {
                    IsWaferPresent = !IsWaferPresent;
                    _waferTimer.Stop();

                    LOG.Info("simulator receive os change Wafer present");
                }
            }

            if (!_devicetimer.IsIdle())
            {
                if (_devicetimer.GetElapseTime() > _moveTime)
                {
                    _devicetimer.Stop();
                    IsBusy = false;
                    LOG.Info("simulator receive os change busy");
                }
            }
            

            if(!_homeTimer.IsIdle() && _homeTimer.GetElapseTime() > _moveTime)
            {
                IsBusy = false;
                _homeTimer.Stop();
            }

            Thread.Sleep(200);
            string ack = string.Format("X,OS,{0}{1}{2}{3}{4}{5:D2}{6:D2}{7}{8}{9}{10}{11}{12}",
                IsError ? "E" : " ",
                IsBusy ? "B" : " ",
                "R",
                "V",
                "AA",
                _targetThetaAxisAddressPosition,
                _targetZAxisAddressPosition,
                "S",
                IsWaferPresent ? "Y" : "N",
                "J",
                " ",
                "L",
                EOT);
            OnWriteMessage(ack);
        }

        public void HandleQueryPosition(string obj)
        {
            Thread.Sleep(20);
            if (!_homeTimer.IsIdle() && _homeTimer.GetElapseTime() > 2000)
            {
                string ack = string.Format("X,GP,{0},{1},{2},{3},{4},{5}{6}",
                0.ToString(),
                11118.ToString(),
                0.ToString(),
                0.ToString(),
                11118.ToString(),
                1149.ToString(),
                EOT);
                OnWriteMessage(ack);
            }
            else
            {
                string ack = string.Format("X,GP,{0},{1},{2},{3},{4},{5}{6}",
                0.ToString(),
                11118.ToString(),
                1149.ToString(),
                0.ToString(),
                11118.ToString(),
                1149.ToString(),
                EOT);
                OnWriteMessage(ack);
            }
            
        }

        public virtual void HandleUnknown(string obj)
        {

        }

        
        public virtual void HandleMHOM(string msg)
        {
            _targetThetaAxisAddressPosition = 0;
            IsBusy = true;
            _homeTimer.Start(0);
            if (_isAllCommandsHaveResponse)
                OnWriteMessage($"X,HM{EOT}");
        }

        public void HandleSetCommandResponse(string msg)
        {
            OnWriteMessage($"X,RR{EOT}");
            _isAllCommandsHaveResponse = true;
        }

        public void HandleSetSpeed(string msg)
        {
            if (_isAllCommandsHaveResponse)
                OnWriteMessage($"X,MH{EOT}");
        }

        public void HandleVacuumOn(string msg)
        {
            if (_isAllCommandsHaveResponse)
                OnWriteMessage($"X,VY{EOT}");
        }

        public void HandleVacuumOff(string msg)
        {
            if (_isAllCommandsHaveResponse)
                OnWriteMessage($"X,VN{EOT}");
        }

        public void HandleWaferMap(string msg)
        {
            string[] datas = Regex.Split(msg.Replace("\r", "").Replace("\n", ""), ",");
            int _targetThetaAxisAddressPosition = 0;
            if (datas.Length >= 3)
            {
                int.TryParse(datas[2], out _targetThetaAxisAddressPosition);
            }
            if (msg.Contains("A"))
            {
                if (_isAllCommandsHaveResponse)
                    OnWriteMessage($"X,MP,{_targetThetaAxisAddressPosition.ToString("D2")}{EOT}");
            }
            else
            {
                OnWriteMessage($"X,MP,{_targetThetaAxisAddressPosition.ToString("D2")},1111111111111111111111111{EOT}");
            }
        }
    }
}

