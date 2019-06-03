using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.Logic
{
    public class Md5Helper
    {
        public static string Md5String(string str)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static string Md5File(string path)
        {
            try
            {
                FileStream file = new FileStream(path, FileMode.Open);
                var md5 = System.Security.Cryptography.MD5.Create();
                var bytes = md5.ComputeHash(file);
                file.Close();
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
            catch
            {
                return "";
            }
        }
    }
}
