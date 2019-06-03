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
using static BambooEditor.UI.design.category.ObjectCategory;

namespace BambooEditor.UI.design.category
{
    /// <summary>
    /// Interaction logic for UcCategoryEditorMaterial.xaml
    /// </summary>
    public partial class UcCategoryEditorMaterial : UserControl, ICategoryEditor
    {
        Logic.ColorSheet curColorSheet;
        ObjectPackResult<MaterialModel> templateList;

        public UcCategoryEditorMaterial()
        {
            InitializeComponent();
            cmbFill.Items.Add("none");
            foreach (var item in Logic.BambooEditor.Instance.colorSheetSet.Sheets)
            {
                cmbFill.Items.Add(item.Name);
            }
            FreshInConstructor();
        }

        async void FreshInConstructor()
        {
            await FreshTemplateList();
        }

        public void SaveData()
        {
            if (target == null)
                return;
            string templateId = "";
            string colorSheet = "";

            var template = cmbTemplate.SelectedItem as EntityBase;
            if (template != null)
                templateId = template.id;

            colorSheet = cmbFill.SelectedItem as string;
            if (colorSheet == null)
                colorSheet = "";

            Logic.CategoryCustomData_Material data = new Logic.CategoryCustomData_Material();
            data.TemplateId = templateId;
            data.ColorSheet = colorSheet;
            data.ColorParamName = txtColorParamName.Text;
            data.MatPackageName = txtMatPackageName.Text;
            data.MatUrl = txtMatUrl.Text;
            target.customData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        CategoryModel target;
        public void SetTarget(CategoryModel Target)
        {
            target = Target;
            Logic.CategoryCustomData_Material customData = null;
            if(target != null && string.IsNullOrEmpty(target.customData) == false)
            {
                customData = Newtonsoft.Json.JsonConvert.DeserializeObject<Logic.CategoryCustomData_Material>(target.customData);
            }
            
            if (customData == null)
            {
                cmbTemplate.SelectedIndex = -1;
                cmbFill.SelectedIndex = 0;
                txtColorParamName.Text = "BaseColor";
                txtMatUrl.Text = "";
                txtMatPackageName.Text = "";
                return;
            }

            cmbFill.SelectedItem = customData.ColorSheet;
            txtColorParamName.Text = customData.ColorParamName;

            if (templateList == null || templateList.data == null)
                return;
            MaterialModel template = null;
            foreach (var item in templateList.data)
            {
                if(item.id == customData.TemplateId)
                {
                    template = item;
                    cmbTemplate.SelectedItem = item;
                    txtMatPackageName.Text = item.packageName;
                    txtMatUrl.Text = item.url;
                    break;
                }

            }
        }

        private void cmbFill_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string type = cmbFill.SelectedItem as string;
            if(string.IsNullOrEmpty(type) || type == "none")
            {
                listColors.ItemsSource = null;
                lblItemsCount.Text = "0";
                return;
            }

            foreach (var item in Logic.BambooEditor.Instance.colorSheetSet.Sheets)
            {
                if (item.Name == type)
                {
                    curColorSheet = item;
                    listColors.ItemsSource = curColorSheet.Colors;
                    lblItemsCount.Text = curColorSheet.Colors.Count.ToString();
                    return;
                }
            }
        }

        private async void btnRefreshTemplates_Click(object sender, RoutedEventArgs e)
        {
            await FreshTemplateList();
        }

        string matTemplateCatId = "";
        string findTemplateCateId()
        {
            if (string.IsNullOrEmpty(matTemplateCatId) == false)
                return matTemplateCatId;

            //find categoryid;
            foreach (var root in Logic.BambooEditor.Instance.rootCategories)
            {
                if (root.type == "hide" && root.children != null)
                {
                    foreach (var item in root.children)
                    {
                        if (item != null && item.name == "MaterialTemplate")
                        {
                            matTemplateCatId = item.id;
                            return matTemplateCatId;
                        }
                    }
                }
            }
            return "";
        }

        async Task<int> FreshTemplateList()
        {
            string categoryId = findTemplateCateId();
            
            string query = $"/material?page=1&pageSize=1000&search=&categoryId={categoryId}";
            var result = await Logic.BambooEditor.Instance.api.GetAsync(query);

            if (result == null || result.Content == null)
                return 0;

            templateList = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectPackResult<MaterialModel>>(result.Content);

            if (templateList == null || templateList.data == null || templateList.data.Count == 0)
            {
                return 0;
            }

            foreach (var p in templateList.data)
            {
                p.IconUrl = Logic.BambooEditor.Instance.serverBase + p.icon;
            }

            cmbTemplate.ItemsSource = templateList.data;

            return templateList.data.Count;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void btnFillCategory_Click(object sender, RoutedEventArgs e)
        {
            if (target == null || cmbFill.SelectedIndex < 0)
                return;

            if(cmbFill.SelectedIndex == 0)
            {
                string msg = $"delete all materials in category\r\n {target.id} {target.name} ?";
                var dlgResult = MessageBox.Show(msg, "are you sure?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (dlgResult != MessageBoxResult.Yes)
                    return;
                string result = await EmptyCatogry();
                MessageBox.Show(result);
            }
            else
            {
                string msg = $"create {curColorSheet.Colors.Count} new materials in category\r\n {target.id} {target.name} ?";
                var dlgResult = MessageBox.Show(msg, "are you sure?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (dlgResult != MessageBoxResult.Yes)
                    return;
                string result = await FillCatogry();
                MessageBox.Show(result);
            }
        }

        async Task<string> EmptyCatogry()
        {
            int pages = 0;
            float pageSize = 100;
            string categoryId = target.id;
            string query = $"/material?page=1&pageSize=1&search=&categoryId={categoryId}";
            var result = await Logic.BambooEditor.Instance.api.GetAsync(query);

            if (result == null || result.Content == null)
                return "invalid response";
            
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<EntityListModel>(result.Content);

            if (data == null)
            {
                return "invalid response message";
            }

            pages = (int)Math.Ceiling(data.total / pageSize);
            if (pages < 1)
                return "empty result";

            int total = 0;
            int success = 0;
            string lastError = "";
            for (int i = 1; i <= pages; i++)
            {
                query = $"/material?page={i}&pageSize={pageSize}&search=&categoryId={categoryId}";
                result = await Logic.BambooEditor.Instance.api.GetAsync(query);

                if (result == null || result.Content == null)
                    return "invalid response message in loop";

                data = Newtonsoft.Json.JsonConvert.DeserializeObject<EntityListModel>(result.Content);

                if (data == null || data.data == null || data.data.Count == 0)
                {
                    continue;
                }
                foreach (var item in data.data)
                {
                    total++;
                    if (item != null)
                    {
                        var subresult = await Logic.BambooEditor.Instance.api.DeleteAsync("/material/" + item.id);
                        if (subresult.IsSuccess)
                            success++;
                        else
                            lastError = subresult.StatusCode + " " + subresult.Content;
                    }
                }
            }

            return $"total {total} success {success}. lasterror {lastError}";
        }

        async Task<string> FillCatogry()
        {
            if (cmbFill.SelectedIndex < 0)
                return "file type not selected";

            var matTemplate = cmbTemplate.SelectedItem as MaterialModel;
            if(matTemplate == null)
            {
                return "material template not selected";
            }
            int total = 0;
            int success = 0;
            string lastError = "";
            string categoryId = target.id;
            foreach (var item in curColorSheet.Colors)
            {
                MaterialModel material = new MaterialModel();
                material.name = item.Name;
                material.categoryId = categoryId;
                material.color = item.Color;
                material.fileAssetId = matTemplate.fileAssetId;
                material.dependencies = matTemplate.dependencies;
                Color color = Logic.BambooEditor.StringToColor(item.Color);
                material.parameters = matTemplate.parameters + string.Format(";V&_{0}=(R={1},G={2},B={3},A={4})", txtColorParamName.Text, color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                material.packageName = matTemplate.packageName;
                material.url = matTemplate.url;
                material.unCookedAssetId = matTemplate.unCookedAssetId;
                material.description = matTemplate.description;

                total++;
                var subresult = await Logic.BambooEditor.Instance.api.PostAsync("/material", material);
                if (subresult.IsSuccess)
                    success++;
                else
                    lastError = subresult.StatusCode + " " + subresult.Content;
            }

            return $"total {total} success {success}. lasterror {lastError}";
        }

        private void cmbTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var temp = cmbTemplate.SelectedItem as MaterialModel;
            if(temp == null)
            {
                txtMatUrl.Text = "";
                txtMatPackageName.Text = "";
            }
            else
            {
                txtMatPackageName.Text = temp.packageName;
                txtMatUrl.Text = temp.url;
            }
        }
    }
}
