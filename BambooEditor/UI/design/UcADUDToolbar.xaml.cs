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
    /// Interaction logic for UcADUDToolbar.xaml
    /// </summary>
    public partial class UcADUDToolbar : UserControl
    {
        public UcADUDToolbar()
        {
            InitializeComponent();
        }
        public event Action AddClick;
        public event Action DeleteClick;
        public event Action UpClick;
        public event Action DownClick;

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            AddClick?.Invoke();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteClick?.Invoke();
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            UpClick?.Invoke();
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            DownClick?.Invoke();
        }
    }
}
