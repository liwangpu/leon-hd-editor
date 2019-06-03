using BambooEditor.Logic;
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

namespace BambooEditor.UI.upload
{
    /// <summary>
    /// Interaction logic for Upload.xaml
    /// </summary>
    public partial class Upload : UserControl
    {
        AssetUploadMan man = new AssetUploadMan();
        CollectionView listView;

        public Upload()
        {
            InitializeComponent();

            listFiles.ItemsSource = man.AllFiles;
            listFiles.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Package", System.ComponentModel.ListSortDirection.Ascending));
            listUploadList.ItemsSource = man.UploadList;
            listProducts.ItemsSource = man.ProductList;

            listView = (CollectionView)CollectionViewSource.GetDefaultView(listFiles.ItemsSource);
            listView.Filter = ListFilter;

            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            man.StopMd5Thread();
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;
            if(Directory.Exists(files[0]))
            {
                txtProjectDir.Text = files[0];
                man.ProjDir = txtProjectDir.Text;
                Reload();
            }
        }

        private void TxtProjectDir_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                man.ProjDir = txtProjectDir.Text;
                Reload();
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            if(man.IsServerDataLoaded == false)
            {
                MessageBox.Show("还未加载服务器数据，请从服务器加载或者从缓存加载数据，进行比对后再开始上传", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (man.IsDownloading)
            {
                MessageBox.Show("正在下载中不能上传，请求已拒绝", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (man.IsCheckedForUpload == false)
            {
                MessageBox.Show("请先检查再上传", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var result = MessageBox.Show($"确认开始上传？", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            BindProgressObj(man.progress);
            await man.Upload();
            MessageBox.Show("ok", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void Reload()
        {
            BindProgressObj(man.progress);
            man.ReloadFileList(txtProjectDir.Text);
            lblLocalStatus.Text = man.LocalFiles.Count + "";
        }

        private async void BtnCheckOnServer_Click(object sender, RoutedEventArgs e)
        {
            BindProgressObj(man.svrdataMan.progress);

            bool pullFullData = chkPullFullData.IsChecked == true;
            int count = await man.LoadFromServer(pullFullData);
            lblServerDataStatus.Text = count + "";
            MessageBox.Show("ok. total " + man.svrdataMan.progress.Total, "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void BindProgressObj(ProgressModel p)
        {
            progressbar.DataContext = null;
            lblStatus.DataContext = null;
            lblProgCur.DataContext = null;
            lblProgTotal.DataContext = null;
            progressbar.DataContext = p;
            lblStatus.DataContext = p;
            lblProgCur.DataContext = p;
            lblProgTotal.DataContext = p;
        }

        private void BtnLoadServerDataFromCache_Click(object sender, RoutedEventArgs e)
        {
            int count = man.LoadFromCache();
            lblServerCacheStatus.Text = count + "";
            MessageBox.Show(count + " in cache", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnCheckServerDataCount_Click(object sender, RoutedEventArgs e)
        {
            string str = await man.svrdataMan.GetServerDataCountString();
            MessageBox.Show(str, "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listFiles.SelectedItems?.Count == 0)
            {
                MessageBox.Show("先选中一些资源再说", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确认删除这 {listFiles.SelectedItems.Count} 个数据不？", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            await DeleteSelectedItems();
            MessageBox.Show("ok", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<int> DeleteSelectedItems()
        {
            ProgressModel progress = new ProgressModel();
            BindProgressObj(progress);
            progress.Total = listFiles.SelectedItems.Count;

            UploadLoger loger = new UploadLoger();
            loger.BeginWrite();
            var api = Logic.BambooEditor.Instance.api;
            List<AssetFileModel> items = new List<AssetFileModel>();
            foreach (AssetFileModel item in listFiles.SelectedItems)
            {
                progress.CurPos++;
                items.Add(item);
                if (string.IsNullOrEmpty(item.ObjId))
                    continue;
                string apiname = SvrDataMan.GetApiNameByTypeName(item.Type);
                var res = await api.DeleteAsync($"/{apiname}/{item.ObjId}");
                string resstr = res.IsSuccess ? " ok" : res.StatusCode.ToString();
                progress.Status = item.ObjId + " " + resstr;
                loger.Write(item, "delete", resstr);
            }
            foreach (var item in items)
            {
                man.RemoveFile(item);
            }
            man.svrdataMan.SaveCache();

            progress.Status = "ok";
            loger.EndWrite();
            return (int)progress.Total;
        }

        private void RdoFilterAllRegion_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void RdoFilterLocal_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void RdoFilterServer_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterWorkDir_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterDependDir_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterMap_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterMaterial_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterTexture_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterStaticMesh_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterProduct_Checked(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private bool ListFilter(object itemfile)
        {
            AssetFileModel item = itemfile as AssetFileModel;
            if (item == null)
                return false;

            bool showLocal = rdoFilterLocal.IsChecked == true;
            bool showServer = rdoFilterServer.IsChecked == true;
            if(rdoFilterAllRegion.IsChecked == true)
            {
                showLocal = true;
                showServer = true;
            }

            if (!showLocal && man.LocalFiles.Contains(item))
                return false;
            if (!showServer && man.ServerFiles.Contains(item))
                return false;

            bool showWorkDir = chkFilterWorkDir.IsChecked == true;
            bool showDependDir = chkFilterDependDir.IsChecked == true;

            bool showTexture = chkFilterTexture.IsChecked == true;
            bool showMesh = chkFilterStaticMesh.IsChecked == true;
            bool showMaterial = chkFilterMaterial.IsChecked == true;
            bool showMap = chkFilterMap.IsChecked == true;
            //bool showProduct = chkFilterProduct.IsChecked == true;
            
            if (!showWorkDir && item.IsInWorkDir)
                return false;
            if (!showDependDir && !item.IsInWorkDir)
                return false;

            if (!showTexture && item.Class.StartsWith("Texture"))
                return false;
            if (!showMesh && item.Class == "StaticMesh")
                return false;
            if (!showMaterial && item.Class.StartsWith("Material"))
                return false;
            if (!showMap && item.Class == "Map")
                return false;
            //if (!showProduct && item.Class == "Product")
            //    return false;
            return true;
        }

        private void RefreshListByFilter()
        {
            if (IsLoaded == false)
                return; //ui not ready.
            
            listView.Refresh();
            
            updateListCountStatus();
        }

        private void TxtSearchKey_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Search();
            }
            else
            {
                searchIndex = 0;
                Search();
            }
        }
        int searchIndex = 0;
        private void Search()
        {
            string key = txtSearchKey.Text.Trim();
            listFiles.SelectedIndex = -1;
            for (int i = searchIndex; i < listFiles.Items.Count; i++)
            {
                AssetFileModel file = listFiles.Items[i] as AssetFileModel;
                if(file.Package?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.ObjId?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.FileId?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.Name?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    searchIndex = i + 1;
                    listFiles.SelectedIndex = i;
                    listFiles.ScrollIntoView(file);
                    return;
                }
            }

            //not found. start from begining next time.
            searchIndex = 0;
        }

        private void ListFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateListCountStatus();
        }

        private void updateListCountStatus()
        {
            lblListStatus.Text = $"{listFiles.Items.Count} / {man.AllFiles.Count}. selected {listFiles.SelectedItems?.Count}";
        }

        private void ChkFilterWorkDir_Click(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterDependDir_Click(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterMap_Click(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterMaterial_Click(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterTexture_Click(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterStaticMesh_Click(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterProduct_Click(object sender, RoutedEventArgs e)
        {
            RefreshListByFilter();
        }

        private void ChkFilterTypeAll_Click(object sender, RoutedEventArgs e)
        {
            if (chkFilterTypeAll.IsChecked == true)
            {
                chkFilterTexture.IsChecked = true;
                chkFilterStaticMesh.IsChecked = true;
                chkFilterMaterial.IsChecked = true;
                chkFilterMap.IsChecked = true;
                //chkFilterProduct.IsChecked = true;
            }
            else
            {
                chkFilterTexture.IsChecked = false;
                chkFilterStaticMesh.IsChecked = false;
                chkFilterMaterial.IsChecked = false;
                chkFilterMap.IsChecked = false;
                //chkFilterProduct.IsChecked = true;
            }
            RefreshListByFilter();
        }

        private void BtnDumpList_Click(object sender, RoutedEventArgs e)
        {
            man.DumpList();
            MessageBox.Show("ok", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnDownloadAllAssets_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(man.ProjDir) == false)
            {
                MessageBox.Show($"项目路径 [{man.ProjDir}] 不存在，无法下载", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (man.IsUploading)
            {
                MessageBox.Show("正在上传中不能下载，请求已拒绝", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var result = MessageBox.Show($"确认开始下载所有资源？这会耗费很长时间", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            BindProgressObj(man.progress);
            await man.DownloadAllAssets();
            MessageBox.Show("ok", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCheckForUpload_Click(object sender, RoutedEventArgs e)
        {
            string result = man.CheckForUpload();
            rdoUploadList.IsChecked = true;
            tabList.SelectedIndex = 1;
            MessageBox.Show(result);
        }

        private void BtnDumpAllFiles_Click(object sender, RoutedEventArgs e)
        {
            if((man.cache?.Parsed?.FileMap?.Values?.Count > 0) == false)
            {
                MessageBox.Show("还没有加载数据", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            StreamWriter sw = new StreamWriter("filelist.csv", false, Encoding.UTF8);
            sw.WriteLine("id, name, url, size, localpath, updatetime");
            foreach (var item in man.cache.Parsed.FileMap.Values)
            {
                sw.WriteLine($"{item.id}, {item.name}, {item.url}, {new FileSize(item.size)}, {item.localPath}, {item.modifiedTime}");
            }
            sw.Close();

            MessageBox.Show("ok", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExpandSecondToolbar_Click(object sender, RoutedEventArgs e)
        {
            dockSecondToolbar.Visibility = dockSecondToolbar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        class CopyItem
        {
            public string srcpath;
            public string dstpath;
            public DateTime updatetm;
        }
        private void BtnTransferFiles_Click(object sender, RoutedEventArgs e)
        {
            if ((man.cache?.Parsed?.FileMap?.Values?.Count > 0) == false)
            {
                MessageBox.Show("还没有加载数据", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string svrDir = txtServerFileDir.Text;
            string localDir = txtLocalFileDir.Text;
            if(Directory.Exists(svrDir) == false)
            {
                MessageBox.Show("服务器文件夹不存在", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (Directory.Exists(localDir) == false)
            {
                MessageBox.Show("接收文件夹不存在", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (svrDir.EndsWith("/") == false)
                svrDir = svrDir + "/";

            if (localDir.EndsWith("/") == false)
                localDir = localDir + "/";

            Dictionary<string, CopyItem> copyqueue = new Dictionary<string, CopyItem>();

            ProgressModel prog = new ProgressModel();
            BindProgressObj(prog);
            prog.Total = man.cache.Parsed.FileMap.Values.Count;
            long totalSize = 0;
            foreach (var item in man.cache.Parsed.FileMap.Values)
            {
                prog.Status = item.localPath;
                prog.CurPos++;
                if (string.IsNullOrEmpty(item.localPath) || string.IsNullOrEmpty(item.url) || item.url.StartsWith("/upload/", StringComparison.CurrentCultureIgnoreCase) == false)
                    continue;//数据不合法
                if (item.localPath.IndexOf("/Saved/") >= 0)
                    continue;//saved文件夹的一律跳过

                //只复制 content和 uploadIcons文件夹的

                int idindex = item.localPath.IndexOf("/Content/");
                if (idindex < 0)
                    idindex = item.localPath.IndexOf("/UploadIcons/");

                if (idindex < 0)
                    continue;//只复制 content和 uploadIcons文件夹的

                CopyItem cpitem = new CopyItem();
                cpitem.srcpath = svrDir + item.url.Substring(7);// /upload/abc.txt >> abc.txt
                cpitem.dstpath = localDir + item.localPath.Substring(idindex);
                cpitem.updatetm = item.modifiedTime;
                if(copyqueue.ContainsKey(cpitem.dstpath))
                {
                    var oldone = copyqueue[cpitem.dstpath];
                    if(cpitem.updatetm > oldone.updatetm)
                    {
                        copyqueue[cpitem.dstpath] = cpitem;
                    }
                }
                else
                {
                    copyqueue[cpitem.dstpath] = cpitem;
                    totalSize += item.size;
                }
            }

            MessageBox.Show($"总共 {copyqueue.Count} 文件拷贝， 尺寸约 {new FileSize((ulong)totalSize)}");

            prog.Reset();
            prog.Total = copyqueue.Values.Count;
            foreach (var item in copyqueue.Values)
            {
                Task.Run(() => {
                    string dstdir = System.IO.Path.GetDirectoryName(item.dstpath);
                    try
                    {
                        if (Directory.Exists(dstdir) == false)
                            Directory.CreateDirectory(dstdir);
                        File.Copy(item.srcpath, item.dstpath, true);
                        prog.Status = item.dstpath;
                        prog.CurPos++;
                    }
                    catch(Exception ex)
                    {
                        prog.Status = item.dstpath + " " + ex.Message;
                        prog.CurPos++;
                    }
                });
            }
        }

        private void BtnCopySelected_Click(object sender, RoutedEventArgs e)
        {
            if(listFiles.SelectedItem == null)
            {
                MessageBox.Show("没有选中项", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string text = Newtonsoft.Json.JsonConvert.SerializeObject(listFiles.SelectedItem, Newtonsoft.Json.Formatting.Indented);
            Clipboard.SetText(text);
            MessageBox.Show("已经复制到剪贴板", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RdoFileList_Checked(object sender, RoutedEventArgs e)
        {
            if (tabList == null)
                return;
            tabList.SelectedIndex = 0;
        }

        private void RdoUploadList_Checked(object sender, RoutedEventArgs e)
        {
            if (tabList == null)
                return;
            tabList.SelectedIndex = 1;
        }

        private void RdoProductList_Checked(object sender, RoutedEventArgs e)
        {
            if (tabList == null)
                return;
            tabList.SelectedIndex = 2;
        }

        int indexSearchUploadList = 0;
        private void TxtSearchUploadList_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                string key = txtSearchUploadList.Text.Trim();
                listUploadList.SelectedIndex = -1;
                for (int i = indexSearchUploadList; i < listUploadList.Items.Count; i++)
                {
                    AssetFileOperate file = listUploadList.Items[i] as AssetFileOperate;
                    if (file.File?.Package?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.File?.ObjId?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.File?.FileId?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.File?.Name?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        indexSearchUploadList = i + 1;
                        listUploadList.SelectedIndex = i;
                        listUploadList.ScrollIntoView(file);
                        return;
                    }
                }

                //not found. start from begining next time.
                indexSearchUploadList = 0;
            }
        }


        int indexSearchProductList = 0;
        private void TxtSearchProductList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string key = txtSearchProductList.Text.Trim();
                listProducts.SelectedIndex = -1;
                for (int i = indexSearchProductList; i < listProducts.Items.Count; i++)
                {
                    AssetFileModel file = listProducts.Items[i] as AssetFileModel;
                    if (file.Package?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.ObjId?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || file.Name?.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        indexSearchProductList = i + 1;
                        listProducts.SelectedIndex = i;
                        listProducts.ScrollIntoView(file);
                        return;
                    }
                }

                //not found. start from begining next time.
                indexSearchProductList = 0;
            }
        }

        private void BtnDownloadSizeCheck_Click(object sender, RoutedEventArgs e)
        {            
            MessageBox.Show(man.GetDownloadFileStat());
        }

        private void TxtDebugFind_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                string package = txtDebugFind.Text.Trim();
                var file = man.cache.Parsed.GetFileAssetsByPackageName(package);
                if(file == null)
                {
                    MessageBox.Show("not found");
                    return;
                }
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(file, Newtonsoft.Json.Formatting.Indented);
                MessageBox.Show(json);
            }
        }
    }
}
