using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MECF.Framework.UI.Core.Applications
{
    public interface IUiInstance
    {
        string LayoutFile { get; }
        string SystemName { get; }
        bool EnableAccountModule { get; }
        ImageSource MainIcon { get; }

        bool MaxSizeShow { get; }
    }

    public class UiInstanceDefault : IUiInstance
    {
        public virtual string LayoutFile { get; }
        public virtual string SystemName
        {
            get { return "UI"; }
        }
        public virtual bool EnableAccountModule
        {
            get { return false; }
        }
        public virtual ImageSource MainIcon { get; }
        public virtual bool MaxSizeShow
        {
            get { return true; }
        }
    }
}
