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
using BambooUploader.Logic;

namespace BambooEditor.UI.design
{
    /// <summary>
    /// Interaction logic for UcCategoryTree.xaml
    /// </summary>
    public partial class UcCategoryTree : UserControl
    {
        public UcCategoryTree()
        {
            InitializeComponent();
            tree.SelectedItemChanged += Tree_SelectedItemChanged;
        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(tree.SelectedItem == null)
            {
                selectedNode = null;
                lblSelected.Text = "none";
            }
            else
            {
                selectedNode = (tree.SelectedItem as TreeViewItem).Tag as CategoryModel;
                lblSelected.Text = selectedNode.id + " " + selectedNode.name;
            }
            OnSelectionChanged?.Invoke(selectedNode);
        }

        CategoryModel root;
        CategoryModel selectedNode;

        public Action<CategoryModel> OnSelectionChanged;

        public CategoryModel RootNode
        {
            get { return root; }
            set { SetRootNode(value); }
        }
        public CategoryModel SelectedNode
        {
            get { return selectedNode; }
            set { SetSelectedNode(value); }
        }

        bool expandAll = false;
        public bool ExpandAll
        {
            get { return expandAll; }
            set
            {
                expandAll = value;
                expandall(tree, expandAll);
            }
        }
        private void expandall(ItemsControl items, bool expand)
        {
            foreach (var obj in items.Items)
            {
                ItemsControl childControl = items.ItemContainerGenerator.ContainerFromItem(obj) as ItemsControl;
                if (childControl != null)
                {
                    expandall(childControl, expand);
                }
                TreeViewItem item = childControl as TreeViewItem;
                if (item != null)
                    item.IsExpanded = true;
            }
        }

        void SetRootNode(CategoryModel node)
        {
            root = node;
            tree.Items.Clear();
            if(root != null)
            {
                //TreeViewItem item = new TreeViewItem();
                //item.Header = root.name;
                //item.ToolTip = root.id + " " + root.description;
                //item.Tag = root;
                //tree.Items.Add(item);
                //LoadItemChildren(item, root);
                LoadItemChildren(null, root);
            }
            ExpandAll = true;
        }
        void LoadItemChildren(TreeViewItem parent, CategoryModel node)
        {
            if (node == null)
                return;
            foreach(var child in node.children)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = child.name;
                item.ToolTip = child.id + " " + child.description;
                item.Tag = child;
                if (parent == null)
                    tree.Items.Add(item);
                else
                    parent.Items.Add(item);
                LoadItemChildren(item, child);
            }
        }

        void SetSelectedNode(CategoryModel node)
        {
            selectedNode = node;
            if (tree.SelectedItem != null)
            {
                (tree.SelectedItem as TreeViewItem).IsSelected = false;
            }

            SelectNode(tree.Items, node);
        }

        bool SelectNode(ItemCollection items, CategoryModel node)
        {
            foreach (var item in items)
            {
                TreeViewItem treeitem = item as TreeViewItem;
                if (treeitem == null)
                    continue;

                CategoryModel tagvalue = treeitem.Tag as CategoryModel;
                if (tagvalue == node)
                {
                    treeitem.IsSelected = true;
                    return true;
                }
                bool bChildFind = SelectNode(treeitem.Items, node);
                if (bChildFind)
                    return true;
            }
            return false;
        }
    }
}
