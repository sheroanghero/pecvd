using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Aitex.Core.Util
{
    public class DeviceTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private enum DeviceTimerState
        {
            TM_ST_IDLE = 0,
            TM_ST_BUSY = 1,
            TM_ST_TIMEOUT = 2,
        }

        DeviceTimerState _state;
        long _startTime;
        long _timeOut;
        double _freq;
        double _duration;

        public DeviceTimer()
        {
            long freq;
            if (QueryPerformanceFrequency(out freq) == false)
                throw new Exception("本计算机不支持高性能计数器");
            //得到每1ms的CPU计时TickCount数目
            _freq = (double)freq / 1000.0;
            SetState(DeviceTimerState.TM_ST_IDLE);
            QueryPerformanceCounter(out _startTime);
            _timeOut = _startTime;
            _duration = 0;
        }
        private void SetState(DeviceTimerState state)
        {
            _state = state;
        }

        private DeviceTimerState GetState()
        {
            return _state;
        }

        public double GetElapseTime()
        {
            long curCount;
            QueryPerformanceCounter(out curCount);
            return (double)(curCount - _startTime) / (double)_freq;
        }


        public double GetTotalTime()
        {
            return _duration;
        }

        public void Stop()
        {
            SetState(DeviceTimerState.TM_ST_IDLE);
            QueryPerformanceCounter(out _startTime);
            _timeOut = _startTime;
            _duration = 0;
        }

        public void Start(double delay_ms)
        {
            QueryPerformanceCounter(out _startTime);
            _timeOut = Convert.ToInt64(_startTime + delay_ms * _freq);
            SetState(DeviceTimerState.TM_ST_BUSY);
            _duration = delay_ms;
        }

        public void Restart(double delay_ms)
        {
            _timeOut = Convert.ToInt64(_startTime + delay_ms * _freq);
            SetState(DeviceTimerState.TM_ST_BUSY);
            _duration = delay_ms;
        }

        public bool IsTimeout()
        {
            if (_state == DeviceTimerState.TM_ST_IDLE)
            {
            }
            long curCount;
            QueryPerformanceCounter(out curCount);
            if (_state == DeviceTimerState.TM_ST_BUSY && (curCount >= _timeOut))
            {
                SetState(DeviceTimerState.TM_ST_TIMEOUT);
                return true;
            }
            else if (_state == DeviceTimerState.TM_ST_TIMEOUT)
            {
                return true;
            }
            return false;
        }

        public bool IsIdle()
        {
            return (_state == DeviceTimerState.TM_ST_IDLE);
        }
    }
}
