using System.Collections.Generic;
using System.Windows;

namespace OpenSEMI.ClientBase.Command
{
    public sealed class CommandTriggerGroup : FreezableCollection<CommandTrigger>, ICommandTrigger
    {
        private readonly HashSet<ICommandTrigger> _initList = new HashSet<ICommandTrigger>();

        void ICommandTrigger.Initialize(FrameworkElement source)
        {
            foreach (ICommandTrigger child in this)
            {
                if (!_initList.Contains(child))
                {
                    InitializeCommandSource(source, child);
                }
            }
        }

        private void InitializeCommandSource(FrameworkElement source, ICommandTrigger child)
        {
            child.Initialize(source);
            _initList.Add(child);
        }
    }
}
