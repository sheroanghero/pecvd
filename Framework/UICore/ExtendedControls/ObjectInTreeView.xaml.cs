using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Core.ExtendedControls
{
    /// <summary>
    /// ObjectInTreeView.xaml 的交互逻辑
    /// </summary>
    public partial class ObjectInTreeView : UserControl
    {
        public ObjectInTreeView()
        {
            InitializeComponent();
        }

        public object ObjectToVisualize
        {
            get { return (object) GetValue(ObjectToVisualizeProperty); }
            set { SetValue(ObjectToVisualizeProperty, value); }
        }

        public static readonly DependencyProperty ObjectToVisualizeProperty =
            DependencyProperty.Register("ObjectToVisualize", typeof(object), typeof(ObjectInTreeView),
                new PropertyMetadata(null, OnObjectChanged));

        private static void OnObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ObjectTreeNode tree = ObjectTreeNode.CreateTree(e.NewValue);
            (d as ObjectInTreeView).TreeNodes = new List<ObjectTreeNode>() {tree};
        }

        public List<ObjectTreeNode> TreeNodes
        {
            get { return (List<ObjectTreeNode>) GetValue(TreeNodesProperty); }
            set { SetValue(TreeNodesProperty, value); }
        }

        public static readonly DependencyProperty TreeNodesProperty =
            DependencyProperty.Register("TreeNodes", typeof(List<ObjectTreeNode>), typeof(ObjectInTreeView),
                new PropertyMetadata(null));
    }
}