using BambooUploader.Logic;
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
    /// Interaction logic for UcCategorySelector.xaml
    /// </summary>
    public partial class UcCategorySelector : UserControl
    {
        public UcCategorySelector()
        {
            InitializeComponent();
            cateTree.OnSelectionChanged = OnCatetorySelectionChanged;
            Logic.BambooEditor.Instance.CategoryUpdated += Instance_CategoryUpdated;
        }

        private void Instance_CategoryUpdated()
        {
            RefreshData();
        }

        public CategoryModel SelectedCategory { get; private set; }
        public event Action<CategoryModel> SelectionChanged;

        private void OnCatetorySelectionChanged(CategoryModel category)
        {
            SelectedCategory = category;
            popup.IsOpen = false;
            if (SelectedCategory == null)
                btn.Content = "EMPTY";
            else
                btn.Content = SelectedCategory.type + " " + SelectedCategory.name;
            
            SelectionChanged?.Invoke(SelectedCategory);
        }

        private void btn_Click(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        void RefreshData()
        {
            listTypes.ItemsSource = null;
            listTypes.ItemsSource = Logic.BambooEditor.Instance.rootCategories;
        }

        private void listTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cateTree.RootNode = listTypes.SelectedItem as CategoryModel;
        }
    }
}
