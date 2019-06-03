using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.UI.design.category
{
    interface ICategoryEditor
    {
        void SetTarget(BambooUploader.Logic.CategoryModel target);
        void SaveData();
    }
}
