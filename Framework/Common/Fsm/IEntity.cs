using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Fsm
{
    public interface IEntity
    {
        bool Initialize();
        void Terminate();
        void PostMsg<T>(T msg, params object[] args) where T : struct;

        bool Check(int msg, out string reason, params object[] args);

    }

    public interface IModuleEntity : IEntity
    {
        bool IsInit { get; }

        bool IsBusy { get; }

        bool IsIdle { get; }

        bool IsError { get; }

        bool IsOnline { get; set; }

        int Invoke(string function, params object[] args);

        bool CheckAcked(int msg);
    }
}
