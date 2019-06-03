using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.UI.upload
{
    public class UploadLoger
    {
        StreamWriter writer = null;
        const string filename = "uploadlog.csv";

        public UploadLoger()
        {
        }

        public void BeginWrite()
        {
            bool bAppend = File.Exists(filename);

            writer = new StreamWriter(filename, bAppend, Encoding.UTF8);
            if(!bAppend)
            {
                writer.WriteLine("package,objid,name,class,updatetm,fileid,size,operate,result,optime");
            }
        }
        
        public void EndWrite()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }
        }

        public void Write(AssetFileModel file, string operate, string result)
        {
            if (writer == null)
                return;
            writer.WriteLine($"{file.Package}, {file.ObjId}, {file.Name}, {file.Class}, {file.UpdateTm}, {file.FileId}, {file.SizeStr}, {operate}, {result}, {DateTime.Now.ToString()}");
        }
        
    }
}
