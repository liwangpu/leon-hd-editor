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
    public class ColorModel
    {
        public string Name { get; set; }
        public Brush Color { get; set; }
    }
    /// <summary>
    /// Interaction logic for UcColors.xaml
    /// </summary>
    public partial class UcColors : UserControl
    {
        public List<ColorModel> colors = new List<ColorModel>();
        List<string> types = new List<string>();

        public UcColors()
        {
            InitializeComponent();

            types.Clear();
            foreach (var item in Logic.BambooEditor.Instance.colorSheetSet.Sheets)
            {
                types.Add(item.Name);
            }
            listTypes.ItemsSource = types;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (listTypes.Items.Count > 0)
                listTypes.SelectedIndex = 0;
        }

        private void listTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string type = listTypes.SelectedItem as string;
            if (type == null)
            {
                listColors.ItemsSource = null;
                return;
            }
            foreach (var item in Logic.BambooEditor.Instance.colorSheetSet.Sheets)
            {
                if(item.Name == type)
                {
                    listColors.ItemsSource = item.Colors;
                    return;
                }
            }
        }
    }
}
