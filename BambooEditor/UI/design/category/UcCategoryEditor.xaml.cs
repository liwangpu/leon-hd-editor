using BambooCook.Logic;
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
    /// Interaction logic for UcCategoryEditor.xaml
    /// </summary>
    public partial class UcCategoryEditor : UserControl
    {
        CategoryModel target;
        category.ICategoryEditor advanceEditor;
        TextBlock txtNoTarget;
        TextBlock txtNoAdvanceEditor;

        Dictionary<string, category.ICategoryEditor> advanceEditors = new Dictionary<string, category.ICategoryEditor>();

        public UcCategoryEditor()
        {
            InitializeComponent();
            txtNoTarget = new TextBlock();
            txtNoAdvanceEditor = new TextBlock();
            txtNoTarget.Text = "no target";
            txtNoAdvanceEditor.Text = "no advance feature";

            advanceEditors["material"] = new category.UcCategoryEditorMaterial();
            tabAdvance.Content = txtNoTarget;
        }


        public CategoryModel Target
        {
            get { return target; }
            set
            {
                target = value;
                basic.SetTarget(target);
                if(target == null)
                {
                    tabAdvance.Content = txtNoTarget;
                    return;
                }
                if (target.type == null)
                    target.type = "";
                string type = target.type.ToLower();
                if(advanceEditors.ContainsKey(type))
                {
                    advanceEditor = advanceEditors[type];
                    advanceEditor.SetTarget(target);
                    tabAdvance.Content = advanceEditor;
                }
                else
                {
                    tabAdvance.Content = txtNoAdvanceEditor;
                }
            }
        }


        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (target == null)
                return;
            basic.SaveData();
            if (advanceEditor != null)
                advanceEditor.SaveData();

            var result = await Logic.BambooEditor.Instance.api.PutAsync("/category", target);
            ModernDialog dialog = new ModernDialog();
            dialog.Title = "MESSAGE";
            dialog.Content = result.IsSuccess ? "OK" : result.StatusCode + " " + result.Content;
            dialog.ShowDialog();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (target == null)
                return;
            var dlgResult = MessageBox.Show("are you sure?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dlgResult != MessageBoxResult.Yes)
                return;

            var result = await Logic.BambooEditor.Instance.api.DeleteAsync("/category/" + target.id);

            await Logic.BambooEditor.Instance.LoadAllCategories();

            ModernDialog dialog = new ModernDialog();
            dialog.Title = "MESSAGE";
            dialog.Content = result.IsSuccess ? "OK" : result.StatusCode + " " + result.Content;
            dialog.ShowDialog();

        }

        private async void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (target == null)
                return;

            if(string.IsNullOrEmpty(target.parentId))
            {
                MessageBox.Show("use [NewRoot] to create new Root category", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlgResult = MessageBox.Show("are you sure?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dlgResult != MessageBoxResult.Yes)
                return;

            CategoryModel newone = new CategoryModel();
            newone.activeFlag = target.activeFlag;
            newone.categoryId = target.categoryId;
            newone.categoryName = target.categoryName;
            newone.customData = target.customData;
            newone.description = target.description;
            newone.displayIndex = target.displayIndex;
            newone.folderName = target.folderName;
            newone.icon = target.icon;
            newone.isRoot = target.isRoot;
            newone.name = target.name;
            newone.organizationId = target.organizationId;
            newone.parentId = target.parentId;
            newone.resourceType = target.resourceType;
            newone.type = target.type;
            newone.value = target.value;

            var result = await Logic.BambooEditor.Instance.api.PostAsync("/category", newone);

            await Logic.BambooEditor.Instance.LoadAllCategories();

            ModernDialog dialog = new ModernDialog();
            dialog.Title = "MESSAGE";
            dialog.Content = result.IsSuccess ? "OK" : result.StatusCode + " " + result.Content;
            dialog.ShowDialog();
        }
    }
}
