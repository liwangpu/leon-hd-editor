using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.UI.login
{
    public class ServerList
    {
        public ServerItem[] Items { get; set; }
    }

    public class ServerItem
    {
        public string Name { get; set; }
        public string Account { get; set; }
        public string Pwd { get; set; }
        public string BaseUrl { get; set; }
    }

    public class LoginInfo
    {
        public string ServerName { get; set; }
        public string Account { get; set; }
        public string Pwd { get; set; }
    }

    public class LoginHelper
    {
        public ServerList list;
        public LoginInfo loginInfo;

        public void Load()
        {
            string filename = "servers.txt";
            if (File.Exists(filename))
            {
                StreamReader sr = new StreamReader(filename);
                string str = sr.ReadToEnd();
                sr.Close();
                try
                {
                    list = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerList>(str);
                }
                catch
                {
                }
            }
            if (list == null)
                list = new ServerList();

            filename = "logininfo.dat";
            if (File.Exists(filename))
            {
                StreamReader sr = new StreamReader(filename);
                string str = sr.ReadToEnd();
                sr.Close();
                try
                {
                    loginInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginInfo>(str);
                }
                catch
                {
                }
            }
            if (loginInfo == null)
                loginInfo = new LoginInfo();
        }

        public void Save()
        {
            StreamWriter sw = new StreamWriter("logininfo.dat");
            sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(loginInfo));
            sw.Close();
        }
    }

}
