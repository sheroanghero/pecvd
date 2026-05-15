using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MECF.Framework.Common.Equipment;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.ClientControls.RobotControls.DualArmRobot4
{
    /// <summary>
    /// DualArmRobot.xaml 的交互逻辑
    /// </summary>
	public partial class DualArmRobot : UserControl
	{
		private const int ms = 300;

		public int RotateAngle
		{
			get { return (int)GetValue(RotateAngleProperty); }
			set { SetValue(RotateAngleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RotateAngel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RotateAngleProperty =
			DependencyProperty.Register("RotateAngel", typeof(int), typeof(DualArmRobot), new PropertyMetadata(0));

		public int RobotWidth
		{
			get { return (int)GetValue(RobotWidthProperty); }
			set { SetValue(RobotWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RobotWidth.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RobotWidthProperty =
			DependencyProperty.Register("RobotWidth", typeof(int), typeof(DualArmRobot), new PropertyMetadata(0));

		public int RobotHeight
		{
			get { return (int)GetValue(RobotHeightProperty); }
			set { SetValue(RobotHeightProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RobotHeight.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RobotHeightProperty =
			DependencyProperty.Register("RobotHeight", typeof(int), typeof(DualArmRobot), new PropertyMetadata(60));

		public int DockerWidth
		{
			get { return (int)GetValue(DockerWidthProperty); }
			set { SetValue(DockerWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DockerWidth.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DockerWidthProperty =
			DependencyProperty.Register("DockerWidth", typeof(int), typeof(DualArmRobot), new PropertyMetadata(0));

		public WaferInfo Wafer1
		{
			get { return (WaferInfo)GetValue(Wafer1Property); }
			set { SetValue(Wafer1Property, value); }
		}

		// Using a DependencyProperty as the backing store for WaferItem.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Wafer1Property =
			DependencyProperty.Register("Wafer1", typeof(WaferInfo), typeof(DualArmRobot), new PropertyMetadata(null));

		public WaferInfo Wafer2
		{
			get { return (WaferInfo)GetValue(Wafer2Property); }
			set { SetValue(Wafer2Property, value); }
		}

		// Using a DependencyProperty as the backing store for WaferItem.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Wafer2Property =
			DependencyProperty.Register("Wafer2", typeof(WaferInfo), typeof(DualArmRobot), new PropertyMetadata(null));

		public ModuleName Station
		{
			get { return (ModuleName)GetValue(StationProperty); }
			set { SetValue(StationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StationProperty =
			DependencyProperty.Register("Station", typeof(ModuleName), typeof(DualArmRobot), new PropertyMetadata(ModuleName.Robot));

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register("Command", typeof(ICommand), typeof(DualArmRobot), new PropertyMetadata(null));

		public ModuleName RobotBladeTarget
		{
			get { return (ModuleName)GetValue(RobotBladeTargetProperty); }
			set { SetValue(RobotBladeTargetProperty, value); }
		}

		public static readonly DependencyProperty RobotBladeTargetProperty =
			DependencyProperty.Register("RobotBladeTarget", typeof(ModuleName), typeof(DualArmRobot), new FrameworkPropertyMetadata(ModuleName.System, FrameworkPropertyMetadataOptions.AffectsRender));

		public DualArmRobot()
		{
			InitializeComponent();
			root.DataContext = this;

			canvas1.Rotate(90);
			canvas2.Rotate(180);
			canvas3.Rotate(180);
			canvas23.Rotate(180);

			Blade1Status = BladeStatus.Retract;
			Blade2Status = BladeStatus.Retract;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			//var text = new FormattedText(RobotBladeTarget.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 10, Brushes.Black);
			//drawingContext.DrawText(text, new Point(10, 0));

			if (Blade1Status == BladeStatus.Extend && (RobotBladeTarget == ModuleName.System || RobotBladeTarget == ModuleName.Robot))
			{
				RetractBlade1();
				RetractBlade2();
			}

			if (Blade1Status == BladeStatus.Retract && RobotBladeTarget != ModuleName.System && RobotBladeTarget != ModuleName.Robot)
			{
				ExtendBlade1();
				ExtendBlade2();
			}

			if (Blade1Status == BladeStatus.Retract && Blade2Status == BladeStatus.Retract)
			{
				MoveToHome();
				return;
			}

			if (Blade1Status == BladeStatus.Extend || Blade2Status == BladeStatus.Extend)
			{
				MoveRobot(RobotBladeTarget);
			}
		}

		private bool MoveRobot(ModuleName chamberSet)
		{
			switch (chamberSet)
			{
				case ModuleName.System:
					break;
				case ModuleName.LP1:
					return MoveToLP1();
				case ModuleName.LP2:
					return MoveToLP2();
				case ModuleName.LP3:
					break;
				case ModuleName.LP4:
					break;
				case ModuleName.Robot:
					return MoveToHome();
				case ModuleName.Aligner:
					return MoveToAligner();
				default:
					break;
			}
			return false;
		}

		public RobotPostion CurrentPosition
		{
			get; set;
		}

		public BladeStatus Blade1Status
		{
			get; set;
		}

		public BladeStatus Blade2Status
		{
			get; set;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			var cmd = ((MenuItem)sender).Tag;

			Command.Execute(new[] { Station.ToString(), cmd });
		}

		private bool MoveToLP1()
		{
			switch (CurrentPosition)
			{
				case RobotPostion.Home:
					HomeToLP1();
					return true;
				case RobotPostion.LP1:
					break;
				case RobotPostion.LP2:
					LP2ToLP1();
					return true;
				case RobotPostion.Aligner:
					AlignerToLP1();
					return true;
				default:
					break;
			}
			return false;
		}

		private bool MoveToLP2()
		{
			switch (CurrentPosition)
			{
				case RobotPostion.Home:
					HomeToLP2();
					return true;
				case RobotPostion.LP1:
					LP1ToLP2();
					return true;
				case RobotPostion.LP2:
					break;
				case RobotPostion.Aligner:
					AlignerToLP2();
					return true;
				default:
					break;
			}
			return false;
		}

		private bool MoveToAligner()
		{
			switch (CurrentPosition)
			{
				case RobotPostion.Home:
					HomeToAligner();
					return true;
				case RobotPostion.LP1:
					LP1ToAligner();
					return true;
				case RobotPostion.LP2:
					LP2ToAligner();
					return true;
				case RobotPostion.Aligner:
					break;
				default:
					break;
			}
			return false;
		}

		private bool MoveToHome()
		{
			switch (CurrentPosition)
			{
				case RobotPostion.Home:
					break;
				case RobotPostion.LP1:
					LP1ToHome();
					return true;
				case RobotPostion.LP2:
					LP2ToHome();
					return true;
				case RobotPostion.Aligner:
					AlignerToHome();
					return true;
				default:
					break;
			}
			return false;
		}

		private void ExtendBlade1()
		{
			canvas3.Rotate(0, true, ms);
			Blade1Status = BladeStatus.Extend;
		}

		private void RetractBlade1()
		{
			canvas3.Rotate(180, true, ms);
			Blade1Status = BladeStatus.Retract;
		}

		private void ExtendBlade2()
		{
			canvas23.Rotate(0, true, ms);
			Blade2Status = BladeStatus.Extend;
		}

		private void RetractBlade2()
		{
			canvas23.Rotate(180, true, ms);
			Blade2Status = BladeStatus.Retract;
		}

		private void RetractBlade()
		{
			canvas3.Rotate(180, true);
			canvas23.Rotate(180, true);
		}

		private void ExtendBlade(int angle)
		{
			canvas3.Rotate(angle, true);
			canvas23.Rotate(angle, true);
		}

		private void HomeToLP1()
		{
			ToLP1Start(ToLP1End);
		}

		private void LP1ToAligner()
		{
			ToLP1Start(() => ToAlignerStart(ToAlignerEnd));
		}

		private void LP1ToLP2()
		{
			ToLP1Start(() => ToLP2Start(ToLP2End));
		}

		private void HomeToLP2()
		{
			ToLP2Start(ToLP2End);
		}

		private void LP2ToLP1()
		{
			ToLP2Start(() => ToLP1Start(ToLP1End));
		}

		private void LP2ToAligner()
		{
			ToLP2Start(() =>
			{
				RetractBlade();
				ToAlignerStart(() =>
				{
					ExtendBlade(0);
					ToAlignerEnd();
				});
			});
		}

		private void HomeToAligner()
		{
			ToAlignerStart(ToAlignerEnd);
		}

		private void AlignerToLP1()
		{
			ToAlignerStart(() => ToLP1Start(ToLP1End));
		}

		private void AlignerToLP2()
		{
			ToAlignerStart(
				() =>
				{
					RetractBlade();
					ToLP2Start(() =>
					{
						ExtendBlade(90);
						ToLP2End();
					});
				}
				);
		}

		private void LP1ToHome()
		{
			ToLP1Start(ToHome);
		}

		private void LP2ToHome()
		{
			ToLP2Start(ToHome);
		}

		private void AlignerToHome()
		{
			ToAlignerStart(ToHome);
		}

		private void ToLP1Start(Action action = null)
		{
			canvas1.Rotate(105, true, ms, 0, 0, () => { if (action != null) action(); });
			canvas2.Rotate(255, true, ms, 0.5);
			if (Blade1Status == BladeStatus.Extend)
				canvas3.Rotate(270, true, ms);
			if (Blade2Status == BladeStatus.Extend)
				canvas23.Rotate(270, true, ms);
			CurrentPosition = RobotPostion.LP1;
		}

		private void ToLP2Start(Action action = null)
		{
			canvas1.Rotate(70, true, ms, 0, 0, () => { if (action != null) action(); });
			canvas2.Rotate(110, true, ms, 0.5);
			if (Blade1Status == BladeStatus.Extend)
				canvas3.Rotate(90, true, ms);
			if (Blade2Status == BladeStatus.Extend)
				canvas23.Rotate(90, true, ms);
			CurrentPosition = RobotPostion.LP2;
		}

		private void ToAlignerStart(Action action = null)
		{
			canvas1.Rotate(45, true, ms, 0, 0, () => { if (action != null) action(); });
			canvas2.Rotate(135, true, ms, 0);
			if (Blade1Status == BladeStatus.Extend)
				canvas3.Rotate(0, true, ms);
			if (Blade2Status == BladeStatus.Extend)
				canvas23.Rotate(0, true, ms);
			CurrentPosition = RobotPostion.Aligner;
		}

		private void ToLP1End()
		{
			canvas1.Rotate(80, true, ms, 0);
			canvas2.Rotate(220, true, ms, 0);
			if (Blade1Status == BladeStatus.Extend) canvas3.Rotate(330, true, ms, 0);
			if (Blade2Status == BladeStatus.Extend) canvas23.Rotate(330, true, ms, 0);
			CurrentPosition = RobotPostion.LP1;
		}

		private void ToLP2End()
		{
			canvas1.Rotate(95, true, ms, 0);
			canvas2.Rotate(145, true, ms, 0);
			if (Blade1Status == BladeStatus.Extend) canvas3.Rotate(30, true, ms, 0);
			if (Blade2Status == BladeStatus.Extend) canvas23.Rotate(30, true, ms, 0);
			CurrentPosition = RobotPostion.LP2;
		}

		private void ToAlignerEnd()
		{
			canvas1.Rotate(115, true, ms, 0);
			canvas2.Rotate(80, true, ms, 0);
			if (Blade1Status == BladeStatus.Extend) canvas3.Rotate(-15, true, ms, 0);
			if (Blade2Status == BladeStatus.Extend) canvas23.Rotate(-15, true, ms, 0);
			CurrentPosition = RobotPostion.Aligner;
		}

		private void ToHome()
		{
			canvas1.Rotate(90, true, ms);
			canvas2.Rotate(180, true, ms);
			if (Blade1Status == BladeStatus.Extend) canvas3.Rotate(180, true, ms);
			if (Blade2Status == BladeStatus.Extend) canvas23.Rotate(180, true, ms);
			CurrentPosition = RobotPostion.Home;
		}

		private void mnuToLP1_Click(object sender, RoutedEventArgs e)
		{
			MoveToLP1();
		}

		private void mnuToLP2_Click(object sender, RoutedEventArgs e)
		{
			MoveToLP2();
		}

		private void mnuToHome_Click(object sender, RoutedEventArgs e)
		{
			MoveToHome();
		}

		private void mnuToAligner_Click(object sender, RoutedEventArgs e)
		{
			MoveToAligner();
		}
	}

	public enum RobotPostion
	{
		Home,
		LP1,
		LP2,
		Aligner
	}

	public enum BladeStatus
	{
		Extend,
		Retract
	}
}
