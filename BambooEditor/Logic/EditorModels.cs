using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BambooEditor.Logic
{
    public class SlotPredefineModel
    {
        public string Type { get; set; }
        public int Diameter { get; set; }
        public int Depth { get; set; }
        public string ScrewType { get; set; }

        public override string ToString()
        {
            return $"{Type} {ScrewType} {Diameter}x{Depth}";
        }
    }
    public class SlotPredefines
    {
        public List<SlotPredefineModel> Slots { get; set; }
    }

    public class AccountProfile
    {
        public string organizationName { get; set; }
        public string organizationIcon { get; set; }
        public string icon { get; set; }
        public string mail { get; set; }
        public string location { get; set; }
        public string departmentId { get; set; }
        public string departmentName { get; set; }
        public object phone { get; set; }
        public bool frozened { get; set; }
        public string roleId { get; set; }
        public string roleName { get; set; }
        public string type { get; set; }
        public bool isAdmin { get; set; }
        public string iconAssetId { get; set; }
        public DateTime expireTime { get; set; }
        public DateTime activationTime { get; set; }
        public object[] additionRoles { get; set; }
        public string description { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime modifiedTime { get; set; }
        public string creator { get; set; }
        public string modifier { get; set; }
        public string organizationId { get; set; }
        public object categoryId { get; set; }
        public object creatorName { get; set; }
        public object modifierName { get; set; }
        public object folderName { get; set; }
        public object categoryName { get; set; }
        public int activeFlag { get; set; }
        public int resourceType { get; set; }
        public string id { get; set; }
        public string name { get; set; }
    }

    public class SlotFaceDefine
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public Brush Color { get; set; }
        public string Description { get; set; }
    }

    public class SlotFaceDefineSet
    {
        public List<SlotFaceDefine> Faces { get; set; }
    }

    public class ColorModel
    {
        public string Name { get; set; }
        public string Color { get; set; }

        [JsonIgnore]
        public Brush ColorBrush { get; set; }
    }

    public class ColorSheet
    {
        public string Name { get; set; }
        public List<ColorModel> Colors { get; set; }
    }

    public class ColorSheetSet
    {
        public List<ColorSheet> Sheets { get; set; }
    }

    public class CategoryCustomData_Material
    {
        public string TemplateId { get; set; }
        public string ColorSheet { get; set; }
        public string ColorParamName { get; set; }
        public string MatUrl { get; set; }
        public string MatPackageName { get; set; }
    }
}
