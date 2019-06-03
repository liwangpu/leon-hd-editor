using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooUploader.Logic
{
    public class EntityBase
    {
        public string id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string description { get; set; }
        public string iconAssetId { get; set; }
        public string categoryId { get; set; }
        public string color { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime modifiedTime { get; set; }

        [JsonIgnore]
        public string IconUrl { get; set; }
    }
    public class UploadFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        string localPath;
        string id;
        string url;
        string status;
        string shortPath;

        public string LocalPath { get { return localPath; } set { localPath = value; notify("LocalPath"); } }
        public string Id { get { return id; } set { id = value; notify("Id"); } }
        public string Url { get { return url; } set { url = value; notify("Url"); } }
        public string Status { get { return status; } set { status = value; notify("Status"); } }
        public string ShortPath { get { return shortPath; } set { shortPath = value; notify("ShortPath"); } }

    }

    public class GetTokenModel
    {
        public string Account { get; set; }
        public string Password { get; set; }
    }
    public class GetTokenResModel
    {
        public string Token { get; set; }
        public string Message { get; set; }
        public DateTime Expires { get; set; }
    }

    public class ObjectPackResult<T>
    {
        public List<T> data { get; set; }
        public int page { get; set; }
        public int size { get; set; }
        public int total { get; set; }
    }

    public class FileListModel
    {
        public List<FileModel> data { get; set; }
        public int page { get; set; }
        public int size { get; set; }
        public int total { get; set; }
    }

    public enum FileAssetType
    {
        None,
        Mesh,
        Material,
        Texture,
        Map
    }
    public class FileModel : EntityBase
    {
        public string url { get; set; }
        public string md5 { get; set; }
        public int size { get; set; }
        public string fileExt { get; set; }
        public string localPath { get; set; }
        public int fileState { get; set; }
        public string iconFileAsset { get; set; }
        public string creator { get; set; }
        public string modifier { get; set; }
        public string organizationId { get; set; }
        public string creatorName { get; set; }
        public string modifierName { get; set; }
        public string folderName { get; set; }
        public string categoryName { get; set; }
        public int activeFlag { get; set; }
        public int resourceType { get; set; }

        //inner used transient properties

        [JsonIgnore]
        public bool NeedCook { get; set; }
        [JsonIgnore]
        public bool DownloadOk { get; set; }
        [JsonIgnore]
        public bool ImportOk { get; set; }
        [JsonIgnore]
        public bool CookedOk { get; set; }
        [JsonIgnore]
        public string UploadFileIds { get; set; }
        [JsonIgnore]
        public string CreatedObjIds { get; set; }
        [JsonIgnore]
        public FileAssetType AssetType { get; set; }
    }

    public class StaticMeshModel : ClientAssetModel
    {
        public string properties { get; set; }
    }


    public class ProductModel : EntityBase
    {
        public decimal price { get; set; }
        public decimal partnerPrice { get; set; }
        public decimal purchasePrice { get; set; }
        public List<ProductSpecModel> specifications { get; set; }
        public string unit { get; set; }
        public FileModel iconFileAsset { get; set; }
        public string creator { get; set; }
        public string modifier { get; set; }
        public string organizationId { get; set; }
        public string creatorName { get; set; }
        public string modifierName { get; set; }
        public string folderName { get; set; }
        public string categoryName { get; set; }
        public int activeFlag { get; set; }
        public int resourceType { get; set; }
    }

    public class ProductSpecModel : EntityBase
    {
        public decimal price { get; set; }
        public decimal partnerPrice { get; set; }
        public decimal purchasePrice { get; set; }
        public string tpid { get; set; }
        public string productId { get; set; }
        public FileModel iconAsset { get; set; }
        public List<StaticMeshModel> staticMeshes { get; set; }
        public string album { get; set; }
        public string iconFileAsset { get; set; }
        public string creator { get; set; }
        public string modifier { get; set; }
        public string organizationId { get; set; }
        public string creatorName { get; set; }
        public string modifierName { get; set; }
        public string folderName { get; set; }
        public string categoryName { get; set; }
        public int activeFlag { get; set; }
        public int resourceType { get; set; }
        public string slots { get; set; }
        public string components { get; set; }
        public string staticMeshIds { get; set; }

        [JsonIgnore]
        public SpecSlotsModel SlotsObj { get; set; }
        [JsonIgnore]
        public List<FProductComponentModel> ComponentsObj { get; set; }

        public void PropStrToObj()
        {
            if (slots == null)
            {
                slots = "";
            }
            try
            {
                SlotsObj = JsonConvert.DeserializeObject<SpecSlotsModel>(slots);
            }
            catch { }
            if (SlotsObj == null)
            {
                SlotsObj = new SpecSlotsModel();
            }
            SlotsObj.InitFaces();
            if (components == null)
                components = "";
            try
            {
                ComponentsObj = JsonConvert.DeserializeObject<List<FProductComponentModel>>(components);
            }
            catch { }
            if (ComponentsObj == null)
            {
                ComponentsObj = new List<FProductComponentModel>();
            }
        }

        public void PropObjToStr()
        {
            if (SlotsObj == null)
            {
                slots = "";
            }
            else
            {
                SlotsObj.SortSlots();
                SlotsObj.FaceSlotsTo3dSlots();
                slots = JsonConvert.SerializeObject(SlotsObj);
            }
            //do not change components. 
            if (ComponentsObj == null)
            {
                components = "";
            }
            else
            {
                components = JsonConvert.SerializeObject(ComponentsObj);
            }
        }
    }

    public class FProductComponentlMinimal
    {
        public string ClassName;
    };

    public class FProductComponentModel : FProductComponentlMinimal
    {
        public string Id;
        public string Name;
        public string Description;
        public string Icon;
        public string ParentName;

        public FProductComponentModel()
        {
            ClassName = "Component";
        }
    };
    public class FProductStaticMeshComponent : FProductComponentModel
    {
        public StaticMeshModel StaticMesh;

        public FProductStaticMeshComponent()
        {
            ClassName = "StaticMesh";
        }
    };

    public class SlotModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        int index;
        double posx, posy, posz, diameter, depth,dirx,diry,dirz;
        string type, screwtype;

        public int Index { get { return index; } set { index = value; notify("Index"); } }
        public double PosX { get { return posx; } set { posx = value; notify("PosX"); notify("Title"); } }
        public double PosY { get { return posy; } set { posy = value; notify("PosY"); notify("Title"); } }
        public double PosZ { get { return posz; } set { posz = value; notify("PosZ"); notify("Title"); } }
        public double DirX { get { return dirx; } set { dirx = value; notify("DirX"); notify("Title"); } }
        public double DirY { get { return diry; } set { diry = value; notify("DirY"); notify("Title"); } }
        public double DirZ { get { return dirz; } set { dirz = value; notify("DirZ"); notify("Title"); } }
        public string Type { get { return type; } set { type = value; notify("Type"); notify("Title"); } }
        public string ScrewType { get { return screwtype; } set { screwtype = value; notify("ScrewType"); notify("Title"); } }
        public double Diameter { get { return diameter; } set { diameter = value; notify("Diameter"); notify("Title"); } }
        public double Depth { get { return depth; } set { depth = value; notify("Depth"); notify("Title"); } }

        [JsonIgnore]
        public string Title { get { return ToString(); } }

        public override string ToString()
        {
            return $"{Index} {PosX},{PosY} {Type} {ScrewType}";
        }
    }

    public class SpecFaceSlotsModel
    {
        public int Index { get; set; }
        public System.Collections.ObjectModel.ObservableCollection<SlotModel> Slots { get; set; }
        [JsonIgnore]
        public SpecSlotsModel Parent;
    }

    public class SpecSlotsModel : INotifyPropertyChanged
    {
        double boundx, boundy, boundz;
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public double BoundX { get { return boundx; } set { boundx = value; notify("BoundX"); } }
        public double BoundY { get { return boundy; } set { boundy = value; notify("BoundY"); } }
        public double BoundZ { get { return boundz; } set { boundz = value; notify("BoundZ"); } }
        /// <summary>
        /// 按包围盒的6个面计算的插槽
        /// </summary>
        public List<SpecFaceSlotsModel> Faces { get; set; }
        /// <summary>
        /// 所有的插槽换算成3d空间的版本
        /// </summary>
        public List<SlotModel> Slots3d { get; set; }

        public void InitFaces()
        {
            if (Faces != null)
            {
                foreach (var face in Faces)
                {
                    face.Parent = this;
                }
                return;
            }
            Faces = new List<SpecFaceSlotsModel>();
            for (int i = 0; i < 6; i++)
            {
                Faces.Add(new SpecFaceSlotsModel() { Index = i, Slots = new System.Collections.ObjectModel.ObservableCollection<SlotModel>(), Parent = this });
            }
        }

        public void SortSlots()
        {
            int index = 0;
            if (Faces == null)
                return;
            foreach (var face in Faces)
            {
                if (face == null || face.Slots == null)
                    continue;
                foreach (var slot in face.Slots)
                {
                    slot.Index = index++;
                }
            }
        }

        public void FaceSlotsTo3dSlots()
        {
            if (Faces == null)
                return;
            if (Slots3d == null)
                Slots3d = new List<SlotModel>();

            foreach (var face in Faces)
            {
                if (face.Slots == null)
                    continue;
                foreach (var slot in face.Slots)
                {
                    var s3d = FaceSlotTo3dSlot(face.Index, slot);
                    Slots3d.Add(s3d);
                }
            }
        }

        SlotModel FaceSlotTo3dSlot(int faceIndex, SlotModel faceSlot)
        {
            SlotModel s3d = new SlotModel();
            s3d.Index = faceSlot.Index;
            s3d.Type = faceSlot.Type;
            s3d.ScrewType = faceSlot.ScrewType;
            s3d.Diameter = faceSlot.Diameter;
            s3d.Depth = faceSlot.Depth;

            double hbx = BoundX / 2;
            double hby = BoundY / 2;
            double hbz = BoundZ / 2;

            //       z  x
            //     __|_/__            1________2
            //    /__|/__/|          3/_______/4
            //   |       ||          
            //   |_______|/--Y        5________6
            //                       7/_______/8

            switch (faceIndex)
            {
                // 0 +x
                //   2 ____ 1
                //   6|____|5
                case 0:
                    {
                        s3d.PosX = +hbx;
                        s3d.PosY = +hby - faceSlot.PosX;
                        s3d.PosZ = +hbz - faceSlot.PosY;
                    }
                    break;
                // 1 -x
                //   3 ____ 4
                //   7|____|8
                case 1:
                    {
                        s3d.PosX = -hbx;
                        s3d.PosY = -hby + faceSlot.PosX;
                        s3d.PosZ = +hbz - faceSlot.PosY;
                    }
                    break;
                // 2 +y
                //   4 ____ 2
                //   8|____|6
                case 2:
                    {
                        s3d.PosX = -hbx + faceSlot.PosX;
                        s3d.PosY = +hby;
                        s3d.PosZ = +hbz - faceSlot.PosY;
                    }
                    break;
                // 3 -y
                //   1 ____ 3
                //   5|____|7
                case 3:
                    {
                        s3d.PosX = +hbx - faceSlot.PosX;
                        s3d.PosY = -hby;
                        s3d.PosZ = +hbz - faceSlot.PosY;
                    }
                    break;
                // 4 +z
                //   1 ____ 2
                //   3|____|4
                case 4:
                    {
                        s3d.PosX = +hbx - faceSlot.PosY;
                        s3d.PosY = -hby + faceSlot.PosX;
                        s3d.PosZ = +hbz;
                    }
                    break;
                // 5 -z
                //   7 ____ 8
                //   5|____|6
                case 5:
                    {
                        s3d.PosX = -hbx + faceSlot.PosY;
                        s3d.PosY = -hby + faceSlot.PosX;
                        s3d.PosZ = -hbz;
                    }
                    break;
                default:
                    s3d.PosX = faceSlot.PosX;
                    s3d.PosY = faceSlot.PosY;
                    break;
            }

            return s3d;
        }

        SlotModel Slot3dToFaceSlot(int faceIndex, SlotModel s3d)
        {
            SlotModel faceSlot = new SlotModel();
            faceSlot.Index = s3d.Index;
            faceSlot.Type = s3d.Type;
            faceSlot.ScrewType = s3d.ScrewType;
            faceSlot.Diameter = s3d.Diameter;
            faceSlot.Depth = s3d.Depth;
            faceSlot.PosZ = 0;

            double hbx = BoundX / 2;
            double hby = BoundY / 2;
            double hbz = BoundZ / 2;

            //       z  x
            //     __|_/__            1________2
            //    /__|/__/|          3/_______/4
            //   |       ||          
            //   |_______|/--Y        5________6
            //                       7/_______/8

            switch (faceIndex)
            {
                // 0 +x
                //   2 ____ 1
                //   6|____|5
                case 0:
                    {
                        faceSlot.PosX = +hby - s3d.PosY;
                        faceSlot.PosY = +hbz - s3d.PosZ;
                    }
                    break;
                // 1 -x
                //   3 ____ 4
                //   7|____|8
                case 1:
                    {
                        faceSlot.PosX = s3d.PosY - (-hby);
                        faceSlot.PosY = +hbz - s3d.PosZ;
                    }
                    break;
                // 2 +y
                //   4 ____ 2
                //   8|____|6
                case 2:
                    {
                        faceSlot.PosX = s3d.PosX - (-hbx);
                        faceSlot.PosY = +hbz - s3d.PosZ;
                    }
                    break;
                // 3 -y
                //   1 ____ 3
                //   5|____|7
                case 3:
                    {
                        faceSlot.PosX = +hbx - s3d.PosX;
                        faceSlot.PosY = +hbz - s3d.PosZ;
                    }
                    break;
                // 4 +z
                //   1 ____ 2
                //   3|____|4
                case 4:
                    {
                        faceSlot.PosX = s3d.PosY - (-hby);
                        faceSlot.PosY = +hbx - s3d.PosX;
                    }
                    break;
                // 5 -z
                //   7 ____ 8
                //   5|____|6
                case 5:
                    {
                        faceSlot.PosX = s3d.PosY - (-hby);
                        faceSlot.PosY = s3d.PosX - (-hbx);
                    }
                    break;
                default:
                    faceSlot.PosX = s3d.PosX;
                    faceSlot.PosY = s3d.PosY;
                    break;
            }

            return faceSlot;
        }
    }

    public class ClientAssetModel : EntityBase
    {
        public string fileAssetId { get; set; }
        public string dependencies { get; set; }
        public string parameters { get; set; }
        public string url { get; set; }
        public FileModel fileAsset { get; set; }
        public string packageName { get; set; }
        public string unCookedAssetId { get; set; }
        public string srcFileAssetId { get; set; }
        public string iconFileAsset { get; set; }
        public string creator { get; set; }
        public string modifier { get; set; }
        public string organizationId { get; set; }
        public string creatorName { get; set; }
        public string modifierName { get; set; }
        public string folderName { get; set; }
        public string categoryName { get; set; }
        public int activeFlag { get; set; }
        public int resourceType { get; set; }

        public bool IsDependenciesValid()
        {
            //dependencies = "url,package;url2,package2;"
            //check wether it lost url part.
            if (string.IsNullOrEmpty(dependencies))
                return true;
            char[] spliters =  { ';' };
            char[] spliters2 = { ',' };
            string[] deps = dependencies.Split(spliters);
            if (deps.Length == 0)
                return true;
            foreach (var depstr in deps)
            {
                string[] depss = depstr.Split(spliters2);
                if (depss.Length < 2 || string.IsNullOrEmpty(depss[0]))
                    return false;
            }
            return true;
        }
    }

    public class MaterialModel : ClientAssetModel
    {
    }

    public class TextureModel : ClientAssetModel
    {
    }

    public class MapModel : ClientAssetModel
    {
        public string properties { get; set; }
    }


    public class CategoryAllModel
    {
        public List<CategoryModel> categories { get; set; }
    }

    public class CategoryModel
    {
        public string value { get; set; }
        public string icon { get; set; }
        public string parentId { get; set; }
        public bool isRoot { get; set; }
        public string type { get; set; }
        public int displayIndex { get; set; }
        public List<CategoryModel> children { get; set; }
        public string description { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime modifiedTime { get; set; }
        public string creator { get; set; }
        public string modifier { get; set; }
        public string organizationId { get; set; }
        public string categoryId { get; set; }
        public string creatorName { get; set; }
        public string modifierName { get; set; }
        public string folderName { get; set; }
        public string categoryName { get; set; }
        public int activeFlag { get; set; }
        public int resourceType { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string customData { get; set; }
    }

    public class FileAsset
    {
        public string url { get; set; }
        public string md5 { get; set; }
        public int size { get; set; }
        public string fileExt { get; set; }
        public string localPath { get; set; }
        public object icon { get; set; }
        public int fileState { get; set; }
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

}
