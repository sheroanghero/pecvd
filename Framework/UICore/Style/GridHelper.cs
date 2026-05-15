using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MECF.Framework.UI.Core.Style
{
	public class GridHelper
	{
		public static Brush GetColumn0(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column0Property);
		}

		public static void SetColumn0(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column0Property, value);
		}

		// Using a DependencyProperty as the backing store for Column0.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column0Property =
			DependencyProperty.RegisterAttached("Column0", typeof(Brush), typeof(GridHelper), new FrameworkPropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn1(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column1Property);
		}

		public static void SetColumn1(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column1Property, value);
		}

		// Using a DependencyProperty as the backing store for Column1.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column1Property =
			DependencyProperty.RegisterAttached("Column1", typeof(Brush), typeof(GridHelper), new FrameworkPropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn2(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column2Property);
		}

		public static void SetColumn2(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column2Property, value);
		}

		// Using a DependencyProperty as the backing store for Column2.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column2Property =
			DependencyProperty.RegisterAttached("Column2", typeof(Brush), typeof(GridHelper), new FrameworkPropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn3(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column3Property);
		}

		public static void SetColumn3(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column3Property, value);
		}

		// Using a DependencyProperty as the backing store for Column3.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column3Property =
			DependencyProperty.RegisterAttached("Column3", typeof(Brush), typeof(GridHelper), new FrameworkPropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetStroke(DependencyObject obj)
		{
			return (Brush)obj.GetValue(StrokeProperty);
		}

		public static void SetStroke(DependencyObject obj, Brush value)
		{
			obj.SetValue(StrokeProperty, value);
		}

		// Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StrokeProperty =
			DependencyProperty.RegisterAttached("Stroke", typeof(Brush), typeof(GridHelper), new PropertyMetadata(Brushes.White));

		public static Brush GetColumn4(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column4Property);
		}

		public static void SetColumn4(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column4Property, value);
		}

		// Using a DependencyProperty as the backing store for Column4.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column4Property =
			DependencyProperty.RegisterAttached("Column4", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn5(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column5Property);
		}

		public static void SetColumn5(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column5Property, value);
		}

		// Using a DependencyProperty as the backing store for Column5.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column5Property =
			DependencyProperty.RegisterAttached("Column5", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn6(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column6Property);
		}

		public static void SetColumn6(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column6Property, value);
		}

		// Using a DependencyProperty as the backing store for Column6.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column6Property =
			DependencyProperty.RegisterAttached("Column6", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn7(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column7Property);
		}

		public static void SetColumn7(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column7Property, value);
		}

		// Using a DependencyProperty as the backing store for Column7.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column7Property =
			DependencyProperty.RegisterAttached("Column7", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn8(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column8Property);
		}

		public static void SetColumn8(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column8Property, value);
		}

		// Using a DependencyProperty as the backing store for Column8.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column8Property =
			DependencyProperty.RegisterAttached("Column8", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn9(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column9Property);
		}

		public static void SetColumn9(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column9Property, value);
		}

		// Using a DependencyProperty as the backing store for Column9.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column9Property =
			DependencyProperty.RegisterAttached("Column9", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn10(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column10Property);
		}

		public static void SetColumn10(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column10Property, value);
		}

		// Using a DependencyProperty as the backing store for Column10.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column10Property =
			DependencyProperty.RegisterAttached("Column10", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn11(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column11Property);
		}

		public static void SetColumn11(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column11Property, value);
		}

		// Using a DependencyProperty as the backing store for Column11.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column11Property =
			DependencyProperty.RegisterAttached("Column11", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn12(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column12Property);
		}

		public static void SetColumn12(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column12Property, value);
		}

		// Using a DependencyProperty as the backing store for Column12.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column12Property =
			DependencyProperty.RegisterAttached("Column12", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn13(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column13Property);
		}

		public static void SetColumn13(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column13Property, value);
		}

		// Using a DependencyProperty as the backing store for Column13.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column13Property =
			DependencyProperty.RegisterAttached("Column13", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn14(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column14Property);
		}

		public static void SetColumn14(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column14Property, value);
		}

		// Using a DependencyProperty as the backing store for Column14.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column14Property =
			DependencyProperty.RegisterAttached("Column14", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		public static Brush GetColumn15(DependencyObject obj)
		{
			return (Brush)obj.GetValue(Column15Property);
		}

		public static void SetColumn15(DependencyObject obj, Brush value)
		{
			obj.SetValue(Column15Property, value);
		}

		// Using a DependencyProperty as the backing store for Column15.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty Column15Property =
			DependencyProperty.RegisterAttached("Column15", typeof(Brush), typeof(GridHelper), new PropertyMetadata(null, PropertyChangedCallback));

		private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var grid = d as Grid;
			if (grid != null)
			{
				var col = int.Parse(e.Property.Name.Substring("Column".Length));
				RoutedEventHandler setup = null;
				setup = (s, x) =>
				{
					grid.Loaded -= setup;
					var count = VisualTreeHelper.GetChildrenCount(grid);
					for (int i = 0; i < count; i++)
					{
						var control = grid.Children[i];
						var columnSpan = Grid.GetColumnSpan(control);
						if (columnSpan > 1)
						{
							continue;
						}
						var column = Grid.GetColumn(control);
						if (col == column)
						{
							if (control is Label && e.NewValue == Brushes.Transparent)
							{
								//((Label)control).Style = (System.Windows.Style)Application.Current.TryFindResource("MiddleCenterLabel");
							}
							var rect = new Rectangle();
							rect.StrokeThickness = 1;
							rect.Stroke = GetStroke(grid);
							rect.Fill = e.NewValue as Brush;
							var row = Grid.GetRow(control);
							Grid.SetRow(rect, row);
							Grid.SetColumn(rect, col);
							grid.Children.Insert(i, rect);
							i++;
							count++;
						}
					}
				};

				grid.Loaded += setup;
			}
		}
	}
}
