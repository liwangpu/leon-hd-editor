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

namespace BambooEditor.UI.design.category
{
    class ObjTypeModel
    {
        public string Name { get; set; }
        public string ApiName { get; set; }
        public Type TypeInfo { get; set; }
    }
    /// <summary>
    /// Interaction logic for ObjectCategory.xaml
    /// </summary>
    public partial class ObjectCategory : UserControl
    {
        CategoryModel curCategory;
        int curPage = 1;
        float pageSize = 20;
        int totalPages = 1;
        ObjTypeModel objType;
        ScrollViewer scrollViewer = null;
        System.Collections.ObjectModel.ObservableCollection<EntityBase> items = new System.Collections.ObjectModel.ObservableCollection<EntityBase>();
        List<ObjTypeModel> objTypeList = new List<ObjTypeModel>();

        public ObjectCategory()
        {
            InitializeComponent();
            cateSelector.SelectionChanged += CateSelector_SelectionChanged;

            objTypeList.AddRange(new ObjTypeModel[] 
            {
                new ObjTypeModel{ Name = "Products", ApiName = "Products", TypeInfo = typeof(ProductModel) },
                new ObjTypeModel{ Name = "Material", ApiName = "Material", TypeInfo = typeof(MaterialModel) },
                new ObjTypeModel{ Name = "ProductGroup", ApiName = "ProductGroup", TypeInfo = null },
                new ObjTypeModel{ Name = "Solution", ApiName = "Solution", TypeInfo = null },
                new ObjTypeModel{ Name = "Package", ApiName = "Package", TypeInfo = null },
                new ObjTypeModel{ Name = "StaticMesh", ApiName = "StaticMesh", TypeInfo = typeof(StaticMeshModel) },
                new ObjTypeModel{ Name = "Texture", ApiName = "Texture", TypeInfo = typeof(TextureModel) },
                new ObjTypeModel{ Name = "Map", ApiName = "Map", TypeInfo = typeof(MapModel) }
            });
            listObjTypes.ItemsSource = objTypeList;

            listObjs.ItemsSource = items;
        }

        private async void CateSelector_SelectionChanged(CategoryModel obj)
        {
            curCategory = obj;
            curPage = 1;
            items.Clear();
            if (objType == null)
            {
                MessageBox.Show("select object type first");
                return;
            }
            await FreshObjectList();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Decorator border = VisualTreeHelper.GetChild(listObjs, 0) as Decorator;
            if (border != null)
            {
                scrollViewer = border.Child as ScrollViewer;
            }
        }

        private void listObjs_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private async void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                curPage = 1;
                items.Clear();
                await FreshObjectList();
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
            if (objType == null)
                return 0;
            string search = txtSearch.Text.Trim();
            string categoryId = cateSelector.SelectedCategory == null ? "" : cateSelector.SelectedCategory.id;
            string classify = chkUnclassified.IsChecked == true ? "false" : "true";
            string query = $"/{objType.ApiName}?page={curPage}&pageSize={pageSize}&search={search}&categoryId={categoryId}&classify={classify}";
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

        private async void listObjs_ScrollChanged(object sender, ScrollChangedEventArgs e)
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

        private async void listObjTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            objType = listObjTypes.SelectedItem as ObjTypeModel;
            if (objType == null)
                return;

            curPage = 1;
            items.Clear();
            await FreshObjectList();
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            listObjs.SelectedIndex = -1;
        }

        private async void btnChangeCategory_Click(object sender, RoutedEventArgs e)
        {
            if (newCategorySelector.SelectedCategory == null || listSelectObjs.Items.Count == 0)
                return;
            if (objType == null)
                return;

            if (objType.TypeInfo == null)
            {
                MessageBox.Show("this type can not change category now. sorry");
                return;
            }

            string newCategoryId = newCategorySelector.SelectedCategory.id;

            string msg = $"{listSelectObjs.Items.Count} obj change category to {newCategorySelector.SelectedCategory.name}";
            var dlgResult = MessageBox.Show(msg, "are you sure?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dlgResult != MessageBoxResult.Yes)
                return;

            StringBuilder sb = new StringBuilder();
            foreach (var item in listObjs.SelectedItems)
            {
                EntityBase entity = item as EntityBase;
                if (entity == null)
                    continue;
                string id = entity.id;
                sb.Append(entity.id + " " + entity.name);

                // get object first.
                var result = await Logic.BambooEditor.Instance.api.GetAsync($"/{objType.ApiName}/{id}");
                if(result.IsSuccess)
                {
                    object obj = null;
                    string expstr = "";
                    try
                    {
                        obj = Newtonsoft.Json.JsonConvert.DeserializeObject(result.Content, objType.TypeInfo);
                    }catch(Exception ex)
                    {
                        expstr = ex.Message;
                    }
                    EntityBase objbase = obj as EntityBase;
                    if(objbase == null)
                    {
                        sb.Append(" get ok but not valid entity obj. " + expstr);
                        sb.AppendLine();
                        continue;
                    }
                    objbase.categoryId = newCategoryId;

                    //update back.
                    var updateresult = await Logic.BambooEditor.Instance.api.PutAsync($"/{objType.ApiName}", objbase);
                    if(updateresult.IsSuccess)
                    {
                        sb.Append(" ok");
                        sb.AppendLine();
                        continue;
                    }
                    else
                    {
                        sb.Append(" update failed. " + result.StatusCode + " " + updateresult.Content);
                        sb.AppendLine();
                        continue;
                    }
                }
                else
                {
                    sb.Append(" get failed. " + result.StatusCode + " " + result.Content);
                    sb.AppendLine();
                    continue;
                }
            }

            MessageBox.Show(sb.ToString());
        }

        private async void btnDeleteSeleted_Click(object sender, RoutedEventArgs e)
        {
            if (listSelectObjs.Items.Count == 0 || objType == null || objType.TypeInfo == null)
                return;

            string msg = $"continnue to DELETE {listSelectObjs.Items.Count} objects?";
            var dlgResult = MessageBox.Show(msg, "are you sure?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (dlgResult != MessageBoxResult.Yes)
                return;

            int total = listObjs.SelectedItems.Count;
            int success = 0;
            foreach (var item in listObjs.SelectedItems)
            {
                EntityBase entity = item as EntityBase;
                if (entity == null)
                    continue;
                // get object first.
                var result = await Logic.BambooEditor.Instance.api.DeleteAsync($"/{objType.ApiName}/{entity.id}");
                if (result.IsSuccess)
                {
                    success++;
                }
            }

            MessageBox.Show($"total {total}, success {success}");
        }

        private async void BtnCreateMaterials_Click(object sender, RoutedEventArgs e)
        {
            if (listSelectObjs.Items.Count == 0 || objType == null || objType.TypeInfo == null)
                return;
            if(objType.Name != "Texture")
            {
                MessageBox.Show($"select texture to create material");
                return;
            }

            int success = 0;
            foreach (var item in listObjs.SelectedItems)
            {
                EntityBase entity = item as EntityBase;
                if (entity == null)
                    continue;
                // get object first.
                var texRes = await Logic.BambooEditor.Instance.api.GetAsync<TextureModel>("/texture/" + entity.id);
                var texture = texRes.Content;
                MaterialModel material = new MaterialModel();
                material.iconFileAsset = texture.iconFileAsset;
                material.IconUrl = texture.IconUrl;
                material.name = texture.name;
                material.parameters = $"T&_ParamsName=ParamsName;ParamsNameId=Y6UK36GY8AA5P6;ParamsNameIcon={texture.IconUrl};ParamsNameP=;ParamsNameU=;";
                var result = await Logic.BambooEditor.Instance.api.PostAsync("/material", material);
                if (result.IsSuccess)
                {
                    success++;
                }
            }
        }

        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int hue = (int)(colorSlider.Value / 100 * 360);
            int r, g, b;
            HsvToRgb(hue, 1, 1, out r, out g, out b);
            Color c = Color.FromRgb((byte)r, (byte)g, (byte)b);
            btnColorPreview.Background = new SolidColorBrush(c);
            lblColorName.Text = c.ToString();
        }
        /// <summary>
        /// Convert HSV to RGB
        /// h is from 0-360
        /// s,v values are 0-1
        /// r,g,b values are 0-255
        /// Based upon http://ilab.usc.edu/wiki/index.php/HSV_And_H2SV_Color_Space#HSV_Transformation_C_.2F_C.2B.2B_Code_2
        /// </summary>
        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
