using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BambooEditor
{


    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : UserControl
    {
        UI.login.LoginHelper helper = new UI.login.LoginHelper();

        public LoginPage()
        {
            InitializeComponent();

            lblVerInfo.Content = "ver " + Logic.VersionInfo.Version;
            lblVerInfo.ToolTip = "build at " + Logic.VersionInfo.Date;
        }

        private void txtPwd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnLogin_Click(null, null);
            }
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";
            try
            {
                Uri serverbase = new Uri(txtServer.Text);
                string result = await Logic.BambooEditor.Instance.Login(serverbase, txtAcc.Text, txtPwd.Password);
                if (string.IsNullOrEmpty(result))
                {
                    Logic.BambooEditor.Instance.Logined = true;
                    helper.loginInfo.Account = txtAcc.Text;
                    helper.loginInfo.Pwd = txtPwd.Password;
                    Logic.VersionInfo.ServerInfo = helper.loginInfo.ServerName;
                    helper.Save();
                    CloseWindow();
                }
                else
                {
                    lblError.Text = result;
                }
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }
        void CloseWindow()
        {
            Window parentWindow = Window.GetWindow(Parent);
            if (parentWindow != null)
                parentWindow.Close();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            helper.Load();
            if (!(helper.list?.Items?.Length > 0))
                return;
            cmbServers.ItemsSource = helper.list.Items;
            foreach (var item in helper.list.Items)
            {
                if(item.Name == helper.loginInfo.ServerName)
                {
                    cmbServers.SelectedItem = item;
                    break;
                }
            }
            txtAcc.Text = helper.loginInfo.Account;
            txtPwd.Password = helper.loginInfo.Pwd;
        }

        private void CmbServers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = cmbServers.SelectedItem as UI.login.ServerItem;
            if (item == null)
                return;
            txtServer.Text = item.BaseUrl;
            txtAcc.Text = item.Account;
            txtPwd.Password = item.Pwd;
            helper.loginInfo.ServerName = item.Name;
        }
    }
}
