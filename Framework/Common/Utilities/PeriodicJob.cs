using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Util
{
    public class PeriodicJob
    {
        Thread _thread;
        int _interval;
        DeviceTimer _elapseTimer;
        Func<bool> _func;
        
        CancellationTokenSource _cancelFlag = new CancellationTokenSource();        //for quit thread 
        ManualResetEvent _waitFlag = new ManualResetEvent(true);                    //for pause purpose
        ManualResetEvent _sleepFlag = new ManualResetEvent(false);                //for sleep time

        object _locker = new object();

        public PeriodicJob(int interval, Func<bool> func, string name, bool isStartNow=false, bool isBackground=true)
        {
            _thread = new Thread(new ParameterizedThreadStart(ThreadFunction));
            _thread.Name = name;
            _thread.IsBackground = isBackground;

            _interval = interval;
            _func = func;

            _elapseTimer = new DeviceTimer();

            if (isStartNow)
                Start();
        }

        public void Start()
        {
            //lock (_locker)
            {
                if (_thread == null)
                    return;

                _waitFlag.Set();

                if (_thread.IsAlive)
                    return;

                _thread.Start(this);

                _elapseTimer.Start(0);
            }
        }

        public void Pause()
        {
            //lock (_locker)
            {
                _waitFlag.Reset();
            }
        }

        public void Stop()
        {
            try
            {
                //lock (_locker)
                {
                    _sleepFlag.Set(); //do not sleep

                    _waitFlag.Set();    //do not pause

                    _cancelFlag.Cancel();   //quit

                    if (_thread == null)
                        return;

                    if (_thread.ThreadState != ThreadState.Suspended)
                    {
                        try
                        {
                            _thread.Abort();
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("Thread aborted exception", ex);
                        }
                    }

                    _thread = null;
                }
            }
            catch (Exception ex)
            {
                LOG.Error("Thread stop exception", ex);
            }
        }

        void ThreadFunction(object param)
        {
            PeriodicJob t = (PeriodicJob)param;
            t.Run();
        }


        public void ChangeInterval(int msInterval)
        {
            _interval = msInterval;
        }

        void Run()
        {
            while (!_cancelFlag.IsCancellationRequested)
            {
                _waitFlag.WaitOne();

                _elapseTimer.Start(0);

                try
                {
                    if (!_func())
                        break;
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
                _sleepFlag.WaitOne(Math.Max(_interval - (int)_elapseTimer.GetElapseTime(), 30));
            }
        }
    }
}
