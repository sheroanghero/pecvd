using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Core.Style
{
	/// <summary>
	/// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件.
	///
	/// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件.
	/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根 
	/// 元素中: 
	///
	///     xmlns:MyNamespace="clr-namespace:BeanUI.Controls"
	///
	///
	/// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件.
	/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根 
	/// 元素中: 
	///
	///     xmlns:MyNamespace="clr-namespace:BeanUI.Controls;assembly=BeanUI.Controls"
	///
	/// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
	/// 并重新生成以避免编译错误: 
	///
	///     在解决方案资源管理器中右击目标项目，然后依次单击
	///     “添加引用”->“项目”->[浏览查找并选择此项目]
	///
	///
	/// 步骤 2)
	/// 继续操作并在 XAML 文件中使用控件.
	///
	///     <MyNamespace:HeaderPanel/>
	///
	/// </summary>
	public class HeaderPanel : HeaderedContentControl
	{
		static HeaderPanel()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderPanel), new FrameworkPropertyMetadata(typeof(HeaderPanel)));
		}

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register("Title", typeof(string), typeof(HeaderPanel), new PropertyMetadata(null));

	}
}
