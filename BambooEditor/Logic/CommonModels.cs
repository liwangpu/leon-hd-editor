using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.Logic
{
    public class ProgressModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propName));
        }

        int percent;
        string status;
        long total, cur;

        public int Percent { get { return percent; } set { percent = value; notify("Percent"); } }
        public string Status { get { return status; } set { status = value; notify("Status"); } }

        public long Total { get { return total; } set { total = value; Percent = total == 0 ? 0 : (int)(cur * 100 / total); notify("Total"); } }
        public long CurPos { get { return cur; } set { cur = value; Percent = total == 0 ? 0 : (int)(cur * 100 / total); notify("CurPos"); } }

        public void Reset()
        {
            total = 0;
            cur = 0;
            Percent = 0;
            Status = "";
        }
    }
}
