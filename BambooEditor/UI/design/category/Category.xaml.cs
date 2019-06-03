using BambooUploader.Logic;
using FirstFloor.ModernUI.Windows.Controls;
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

namespace BambooEditor.UI.design
{
    /// <summary>
    /// Interaction logic for Category.xaml
    /// </summary>
    public partial class Category : UserControl
    {
        public Category()
        {
            InitializeComponent();
            cateTree.OnSelectionChanged = OnCatetorySelectionChanged;
            Logic.BambooEditor.Instance.CategoryUpdated += Instance_CategoryUpdated;
            RefreshData();
        }

        private void Instance_CategoryUpdated()
        {
            RefreshData();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void OnCatetorySelectionChanged(CategoryModel category)
        {
            editor.Target = category;
        }

        void RefreshData()
        {
            listTypes.ItemsSource = null;
            listTypes.ItemsSource = Logic.BambooEditor.Instance.rootCategories;
        }

        private void listTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var root = listTypes.SelectedItem as CategoryModel;
            cateTree.RootNode = root;
            editor.Target = root;
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await Logic.BambooEditor.Instance.LoadAllCategories();
        }

        private async void btnNewRoot_Click(object sender, RoutedEventArgs e)
        {
            var dlgResult = MessageBox.Show("are you sure?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dlgResult != MessageBoxResult.Yes)
                return;
            await Logic.BambooEditor.Instance.api.GetAsync("/category?type=newtype");
            await Logic.BambooEditor.Instance.LoadAllCategories();
        }

        private async void btnNewChild_Click(object sender, RoutedEventArgs e)
        {
            var root = listTypes.SelectedItem as CategoryModel;
            if(root == null)
            {
                MessageBox.Show("select a root type first.");
                return;
            }
            var dlgResult = MessageBox.Show("are you sure?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dlgResult != MessageBoxResult.Yes)
                return;

            CategoryModel newone = new CategoryModel();
            newone.name = "new-category";
            newone.parentId = root.id;
            newone.type = root.type;

            var result = await Logic.BambooEditor.Instance.api.PostAsync("/category", newone);
            await Logic.BambooEditor.Instance.LoadAllCategories();
            ModernDialog dialog = new ModernDialog();
            dialog.Title = "MESSAGE";
            dialog.Content = result.IsSuccess ? "OK" : result.StatusCode + " " + result.Content;
            dialog.ShowDialog();
        }
    }
}
