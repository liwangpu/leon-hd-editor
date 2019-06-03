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
    /// Interaction logic for UcTextureToMaterial.xaml
    /// </summary>
    public partial class UcTextureToMaterial : UserControl
    {
        int curPage = 1;
        float pageSize = 20;
        int totalPages = 1;

        ScrollViewer scrollViewer = null;
        System.Collections.ObjectModel.ObservableCollection<EntityBase> items = new System.Collections.ObjectModel.ObservableCollection<EntityBase>();

        public UcTextureToMaterial()
        {
            InitializeComponent();

            listObjs.ItemsSource = items;
        }

        private void ListObjs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listObjs.SelectedIndex < 0)
            {
                lblSelectedCount.Content = "0";
                listSelectObjs.ItemsSource = null;
                return;
            }
            List<string> selects = new List<string>();
            foreach (var item in listObjs.SelectedItems)
            {
                EntityBase entity = item as EntityBase;
                if (entity == null)
                    continue;
                selects.Add(entity.id + " " + entity.name);
            }
            listSelectObjs.ItemsSource = selects;
            lblSelectedCount.Content = selects.Count;
        }

        private async void ListObjs_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (scrollViewer == null)
                return;
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                lblInfo.Content = "end";
                if (items.Count > 0 && curPage < totalPages)
                {
                    curPage++;
                    await FreshObjectList();
                }
            }
            else
            {
                lblInfo.Content = $"{curPage} / {totalPages} {(int)(scrollViewer.VerticalOffset / scrollViewer.ScrollableHeight * 100)}%";
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            curPage = 1;
            items.Clear();
            await FreshObjectList();
        }

        private async void BtnCreateMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (cateSelector.SelectedCategory == null || cateSelector.SelectedCategory.type.Equals("material", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                MessageBox.Show($"select a material type");
                return;
            }
            string result = await CreateMaterials();
            MessageBox.Show(result);
        }

        async Task<string> CreateMaterials()
        {
            if (listSelectObjs.Items.Count == 0)
                return "nothing is selected";

            var category = cateSelector.SelectedCategory;
            Logic.CategoryCustomData_Material cateCustomData = null;
            if(string.IsNullOrEmpty(category.customData) == false)
            {
                cateCustomData = Newtonsoft.Json.JsonConvert.DeserializeObject<Logic.CategoryCustomData_Material>(category.customData);
            }
            if (cateCustomData == null || string.IsNullOrEmpty(cateCustomData.ColorParamName) || string.IsNullOrEmpty(cateCustomData.MatPackageName))
                return "selected category custom data invalid"; //custom data invalid.

            string ParamName = cateCustomData.ColorParamName;

            var tempMatRes = await Logic.BambooEditor.Instance.api.GetAsync<MaterialModel>("/material/" + cateCustomData.TemplateId);
            MaterialModel tempMat = tempMatRes.Content;
            if (tempMat == null)
                return "selected category template material is empty"; //template material is empty.

            StringBuilder sb = new StringBuilder();
            int success = 0;
            foreach (var item in listObjs.SelectedItems)
            {
                EntityBase entity = item as EntityBase;
                if (entity == null)
                {
                    sb.AppendLine("-- invalid entity");
                    continue;
                }
                // get object first.
                var texRes = await Logic.BambooEditor.Instance.api.GetAsync<TextureModel>("/texture/" + entity.id);
                var texture = texRes.Content;
                if (texture == null)
                {
                    sb.AppendLine($"{entity.id} get data failed {texRes.StatusCode}");
                    continue;
                }
                MaterialModel material = new MaterialModel();
                material.icon = texture.icon;
                material.IconUrl = texture.IconUrl;
                material.iconAssetId = texture.iconAssetId;
                material.name = texture.name;
                material.categoryId = category.id;
                material.categoryName = category.categoryName;
                material.fileAssetId = tempMat.fileAssetId;
                material.packageName = tempMat.packageName;
                material.dependencies = tempMat.dependencies;
                string textureFileAssetUrl = texture.fileAsset == null ? "" : texture.fileAsset.url;
                material.parameters = $"T&_{ParamName}={ParamName};{ParamName}Id={texture.id};{ParamName}Icon={texture.icon};{ParamName}P={texture.packageName};{ParamName}U={textureFileAssetUrl};";
                var result = await Logic.BambooEditor.Instance.api.PostAsync("/material", material);
                if (result.IsSuccess)
                {
                    EntityBase newMat = Newtonsoft.Json.JsonConvert.DeserializeObject<EntityBase>(result.Content);
                    string newId = newMat == null ? "-" : newMat.id;
                    sb.AppendLine($"{entity.id} {entity.name} create material {result.StatusCode} {newId}");
                    success++;
                }
            }
            return sb.ToString();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Decorator border = VisualTreeHelper.GetChild(listObjs, 0) as Decorator;
            if (border != null)
            {
                scrollViewer = border.Child as ScrollViewer;
            }
        }

        public class EntityListModel
        {
            public List<EntityBase> data { get; set; }
            public int page { get; set; }
            public int size { get; set; }
            public int total { get; set; }
        }

        async Task<int> FreshObjectList()
        {
            string search = txtSearch.Text.Trim();
            string categoryId = cateSelector.SelectedCategory == null ? "" : cateSelector.SelectedCategory.id;
            string classify = chkUnclassified.IsChecked == true ? "false" : "true";
            string query = $"/Texture?page={curPage}&pageSize={pageSize}&search={search}&categoryId={categoryId}&classify={classify}";
            var result = await Logic.BambooEditor.Instance.api.GetAsync(query);

            if (result == null || result.Content == null)
                return 0;

            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<EntityListModel>(result.Content);

            if (data == null || data.data == null || data.data.Count == 0)
            {
                totalPages = 1;
                listObjs.ItemsSource = null;
                return 0;
            }

            totalPages = (int)Math.Ceiling(data.total / pageSize);

            foreach (var p in data.data)
            {
                p.IconUrl = Logic.BambooEditor.Instance.serverBase + p.icon;
                items.Add(p);
            }

            listObjs.ItemsSource = items;

            return data.data.Count;
        }

        private async void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                curPage = 1;
                items.Clear();
                await FreshObjectList();
            }
        }
    }
}
