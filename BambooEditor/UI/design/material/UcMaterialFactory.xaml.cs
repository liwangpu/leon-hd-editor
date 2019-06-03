using BambooUploader.Logic;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace BambooEditor.UI.design.material
{
    /// <summary>
    /// Interaction logic for UcMaterialFactory.xaml
    /// </summary>
    public partial class UcMaterialFactory : UserControl
    {
        MaterialFactory factory = MaterialFactory.Instance;

        public UcMaterialFactory()
        {
            InitializeComponent();

            listFiles.ItemsSource = factory.Items;
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;
            if (Directory.Exists(files[0]))
            {
                txtProjectDir.Text = files[0];
                //man.ProjDir = txtProjectDir.Text;
                Reload();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void TxtProjectDir_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //man.ProjDir = txtProjectDir.Text;
                Reload();
            }
        }

        void Reload()
        {
            factory.WorkDir = txtProjectDir.Text;
            if(factory.WorkDir == "")
            {
                MessageBox.Show("路径 " + txtProjectDir.Text + " 不包含ue4 项目文件");
                return;
            }
        }

        private void BtnCreateDirs_Click(object sender, RoutedEventArgs e)
        {
            factory.RecreateUploadDir();
        }

        private void BtnOpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(factory.WorkDir))
            {
                MessageBox.Show("项目路径还未设置");
                return;
            }
            string targetPath = factory.WorkDir + "\\Content\\Materials";
            if(Directory.Exists(targetPath) == false)
            {
                MessageBox.Show("路径不存在 " + targetPath);
                return;
            }
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = targetPath;
            p.Start();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await factory.RefreshList();
        }

        int searchIndex = 0;
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchIndex = 0;
            searchNext();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                searchNext();
        }

        void searchNext()
        {
            string key = txtSearch.Text.Trim();
            for (int i = searchIndex; i < listFiles.Items.Count; i++)
            {
                var item = listFiles.Items[i] as FileModel;
                if (item == null)
                    continue;
                if(item.name.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) > 0 || item.id.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) > 0)
                {
                    searchIndex = i + 1;
                    listFiles.SelectedIndex = i;
                    return;
                }
            }
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            if(listFiles.Items.Count == 0)
            {
                MessageBox.Show("没有可以上传的数据", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("确认开始上传?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            //upload
            await factory.Upload();

            MessageBox.Show("ok");
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(factory.GetRules(), "help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
