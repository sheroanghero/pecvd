using OpenSEMI.Ctrlib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenSEMI.Ctrlib.Controls
{
    public enum ValveLogicType
    {
        Positive,
        Nagative
    }

    public class Valve : Button
    {
        #region Properties and Fields
        #region ValveState (DependencyProperty)
        public ValveState ValveState
        {
            get { return (ValveState)GetValue(ValveStateProperty); }
            set { SetValue(ValveStateProperty, value); }
        }
        public static readonly DependencyProperty ValveStateProperty =
            DependencyProperty.Register("ValveState", typeof(ValveState), typeof(Valve),
                new UIPropertyMetadata(ValveState.UNKNOWN));
        #endregion      

        #region Value (DependencyProperty)
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(Valve),
                new UIPropertyMetadata(-1, new PropertyChangedCallback(ValueChangedCallBack)));
        #endregion

        #region ValveType (DependencyProperty)
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Valve),
                new UIPropertyMetadata(Orientation.Vertical));
        #endregion

        private ValveLogicType m_ValveLogic = ValveLogicType.Positive;
        public ValveLogicType ValveLogic
        {
            get { return m_ValveLogic; }
            set { m_ValveLogic = value; }
        }
        #endregion

        static Valve()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Valve), new FrameworkPropertyMetadata(typeof(Valve)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetValveState(this);
        }

        public static void ValueChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            Valve v = d as Valve;
            if (v != null)
            {
                SetValveState(v);
            }
        }

        private static void SetValveState(Valve v)
        {
            if (v.Value == 0)
            {
                if (v.ValveLogic == ValveLogicType.Positive)
                    v.ValveState = ValveState.OFF;
                else
                    v.ValveState = ValveState.ON;
            }
            else if (v.Value == 1)
            {
                if (v.ValveLogic == ValveLogicType.Positive)
                    v.ValveState = ValveState.ON;
                else
                    v.ValveState = ValveState.OFF;
            }
            else
            {
                v.ValveState = ValveState.UNKNOWN;
            }

            //ControlValveDisplay(v);
        }

    }
}
