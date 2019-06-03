using BambooUploader.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.Logic
{
    /// <summary>
    /// cache data from server. like a minimal database.
    /// </summary>
    public class SvrDataCache
    {
        public class DataCacheItemInfoModel
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
            public DateTime UpdateTm { get; set; }
        }

        public class DataCacheModel
        {
            public Dictionary<string, DataCacheItemInfoModel> IdTable { get; set; } = new Dictionary<string, DataCacheItemInfoModel>();
            public Dictionary<string, string> Kvp { get; set; } = new Dictionary<string, string>();
        }
        

        /// <summary>
        /// cache data
        /// </summary>
        public DataCacheModel Data = new DataCacheModel();

        /// <summary>
        /// parsed data from cached data file.
        /// </summary>
        public SvrParsedData Parsed = new SvrParsedData();

        public void Load(string filePath)
        {
            StreamReader sr = new StreamReader(filePath, Encoding.UTF8);
            string str = sr.ReadToEnd();
            sr.Close();

            Data = Newtonsoft.Json.JsonConvert.DeserializeObject<DataCacheModel>(str);
            if(Data == null)
            {
                Data = new DataCacheModel();
            }
            if (Data.IdTable == null)
                Data.IdTable = new Dictionary<string, DataCacheItemInfoModel>();
            if (Data.Kvp == null)
                Data.Kvp = new Dictionary<string, string>();

            ParseData();
        }

        public void Save(string filePath)
        {
            string str = Newtonsoft.Json.JsonConvert.SerializeObject(Data);
            StreamWriter sr = new StreamWriter(filePath, false, Encoding.UTF8);
            sr.Write(str);
            sr.Close();
        }

        public string Get(string id)
        {
            string value;
            Data.Kvp.TryGetValue(id, out value);
            return value;
        }

        public T Get<T>(string id)
        {
            string value;
            Data.Kvp.TryGetValue(id, out value);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
        }

        public bool Has(string id)
        {
            return Data.Kvp.ContainsKey(id);
        }

        public void New(string id, string type, string value)
        {
            if (string.IsNullOrEmpty(id))
                return;
            DataCacheItemInfoModel info = new DataCacheItemInfoModel();
            info.Id = id;
            info.Name = id;
            info.Type = type;
            info.UpdateTm = DateTime.Now;
            Data.IdTable[id] = info;
            Data.Kvp[id] = value;
        }

        public void New(string id, string type, object value)
        {
            if (string.IsNullOrEmpty(id) || value == null)
                return;
            New(id, type, Newtonsoft.Json.JsonConvert.SerializeObject(value));
            Parsed.New(id, type, value as EntityBase);
        }

        public void Update(string id, string value)
        {
            if (string.IsNullOrEmpty(id))
                return;
            Data.IdTable[id].UpdateTm = DateTime.Now;
            Data.Kvp[id] = value;
        }

        public void Update(string id, object value)
        {
            if (string.IsNullOrEmpty(id) || value == null)
                return;
            Update(id, Newtonsoft.Json.JsonConvert.SerializeObject(value));
            Parsed.Update(id, value as EntityBase);
        }

        public void Remove(string id)
        {
            Data.IdTable.Remove(id);
            Data.Kvp.Remove(id);
            Parsed.ObjectsIdMap.Remove(id);
        }

        public void ParseData()
        {
            Parsed.Parse(Data);
        }

        public void Clear()
        {
            Data.IdTable.Clear();
            Data.Kvp.Clear();
            Parsed.Clear();
        }
    }

    /// <summary>
    /// parsed objects data from SvrDataCache string version.
    /// </summary>
    public class SvrParsedData
    {
        public HashSet<string> Types = new HashSet<string>();
        public Dictionary<string, FileModel> FileMap = new Dictionary<string, FileModel>();
        public Dictionary<string, TextureModel> TextureMap = new Dictionary<string, TextureModel>();
        public Dictionary<string, MaterialModel> MaterialMap = new Dictionary<string, MaterialModel>();
        public Dictionary<string, MapModel> MapMap = new Dictionary<string, MapModel>();
        public Dictionary<string, StaticMeshModel> MeshMap = new Dictionary<string, StaticMeshModel>();
        public Dictionary<string, ProductModel> ProductMap = new Dictionary<string, ProductModel>();

        //package name vs item file asset
        public Dictionary<string, List<FileModel>> PackageFileMap = new Dictionary<string, List<FileModel>>();
        //object id vs object
        public Dictionary<string, EntityBase> ObjectsIdMap = new Dictionary<string, EntityBase>();
        //package name vs object
        public Dictionary<string, ClientAssetModel> PackageObjMap = new Dictionary<string, ClientAssetModel>();
        //package name vs product
        public Dictionary<string, ProductModel> PackageProductMap = new Dictionary<string, ProductModel>();
        //package name vs cooked file
        public Dictionary<string, FileModel> CookedPackageFileMap = new Dictionary<string, FileModel>();
        //package name vs uncooked file
        public Dictionary<string, FileModel> UnCookedPackageFileMap = new Dictionary<string, FileModel>();

        public void Clear()
        {
            Types.Clear();
            FileMap.Clear();
            TextureMap.Clear();
            MaterialMap.Clear();
            MeshMap.Clear();
            MapMap.Clear();

            PackageFileMap.Clear();
            ObjectsIdMap.Clear();
            PackageObjMap.Clear();
            PackageProductMap.Clear();
            CookedPackageFileMap.Clear();
            UnCookedPackageFileMap.Clear();
        }

        public void StaticsticsString()
        {
            StreamWriter sw = new StreamWriter("package_file_map_dump.csv", false, Encoding.UTF8);
            int meshobjs = MeshMap.Count;
            int packagesFileMap = PackageFileMap.Count;
            long totalsizeFileMap = 0;
            sw.WriteLine("package, files, size0, filelocation0, files");
            foreach (var kvp in PackageFileMap)
            {
                var item = kvp.Value;
                if (item == null || item.Count == 0)
                    continue;
                totalsizeFileMap += item[0].size;
                StringBuilder sb = new StringBuilder();
                foreach (var f in item)
                {
                    sb.Append($"{f.size} {f.localPath}; ");
                }
                sw.WriteLine($"{kvp.Key}, {item.Count}, {item[0].size}, {sb.ToString()}");
            }
            sw.Close();

            sw = new StreamWriter("package_obj_map_dump.csv", false, Encoding.UTF8);
            int packagesObjMap = PackageObjMap.Count;
            long totalsizeObjMap = 0;
            sw.WriteLine("package, id, name, size, filelocation");
            foreach (var kvp in PackageObjMap)
            {
                var item = kvp.Value;
                var m = item as ClientAssetModel;
                if (m == null)
                    continue;
                int? size = m?.fileAsset?.size;
                int nsize = size == null ? 0 : (int)size;
                totalsizeObjMap += nsize;
                sw.WriteLine($"{kvp.Key}, {item.id}, {item.name}, {nsize}, {m.fileAsset?.localPath}");
            }
            sw.Close();
        }

        void recordPackageEntity(string packageName, ClientAssetModel entityObj)
        {
            if(packageName == null)
                packageName = "";
            
            if(PackageObjMap.ContainsKey(packageName))
            {
                var existed = PackageObjMap[packageName];
                if (entityObj.modifiedTime > existed.modifiedTime)
                    PackageObjMap[packageName] = entityObj;
            }
            else
            {
                PackageObjMap[packageName] = entityObj;
            }
        }

        public EntityBase GetById(string id)
        {
            EntityBase value = null;
            ObjectsIdMap.TryGetValue(id, out value);
            return value;
        }
        
        public List<FileModel> GetFileAssetsByPackageName(string packageName)
        {
            List<FileModel> value = null;
            PackageFileMap.TryGetValue(packageName, out value);
            return value;
        }

        public ClientAssetModel GetNewestObjByPackageName(string packageName)
        {
            ClientAssetModel value = null;
            PackageObjMap.TryGetValue(packageName, out value);
            return value;
        }

        public ProductModel GetProductOnPackage(string packageName)
        {
            ProductModel value = null;
            PackageProductMap.TryGetValue(packageName, out value);
            return value;
        }

        public void New(string id, string type, EntityBase obj)
        {
            ObjectsIdMap[id] = obj;
            if (type == "Texture")
                TextureMap[id] = obj as TextureModel;
            else if (type == "Material")
                MaterialMap[id] = obj as MaterialModel;
            else if (type == "StaticMesh")
                MeshMap[id] = obj as StaticMeshModel;
            else if (type == "Map")
                MapMap[id] = obj as MapModel;
            else if (type == "File")
                FileMap[id] = obj as FileModel;
            else if (type == "Product")
                ProductMap[id] = obj as ProductModel;
        }

        public void Update(string id, EntityBase obj)
        {
            ObjectsIdMap[id] = obj;
        }

        void parseFileObj(FileModel file)
        {
            if (file == null || file.localPath == null)
                return;

            bool isCooked = false;
            string packageName = "";

            //cooked D:\Projects\AllAssets419-test\AZMJ\Saved\Cooked\WindowsNoEditor\AZMJ\Content\MatWk\CustomMaterial\DiffuseTextures\FF\heise.uasset
            //uncooked D:\Projects\AllAssets419-test\AZMJ\Content\MatWk\CustomMaterial\DiffuseTextures\FF\heise.uasset
            string path = file.localPath.Replace('\\', '/');
            string cookedTag = "/Saved/Cooked/WindowsNoEditor/";
            string contentTag = "/Content/";
            string uassetTag = ".uasset";
            int index = path.IndexOf(cookedTag);
            if (index > 0)
            {
                isCooked = true; //cooked
                index = path.IndexOf(contentTag, index + cookedTag.Length);
                if(index > 0)
                {
                    packageName = path.Substring(index + contentTag.Length);
                }
            }
            else
            {
                index = path.IndexOf(contentTag);
                if(index > 0)
                {
                    isCooked = false;
                    packageName = path.Substring(index + contentTag.Length);
                }
                else
                {
                    //this is not a unreal asset file.
                    return;
                }
            }
            packageName = "/Game/" + packageName.Replace('\\', '/');
            if(packageName.EndsWith(uassetTag, StringComparison.CurrentCultureIgnoreCase))
            {
                packageName = packageName.Substring(0, packageName.Length - uassetTag.Length);
            }
            if (isCooked)
                CookedPackageFileMap[packageName] = file;
            else
                UnCookedPackageFileMap[packageName] = file;
        }

        public void Parse(SvrDataCache.DataCacheModel Data)
        {
            Clear();

            foreach (var id in Data.IdTable.Values)
            {
                string value = "";
                Data.Kvp.TryGetValue(id.Id, out value);

                if (Types.Contains(id.Type) == false)
                    Types.Add(id.Type);

                if (id.Type == "Texture")
                {
                    TextureModel m = Newtonsoft.Json.JsonConvert.DeserializeObject<TextureModel>(value);
                    TextureMap[id.Id] = m;
                    ObjectsIdMap[id.Id] = m;
                    recordPackageEntity(m?.packageName, m);
                }
                else if (id.Type == "Material")
                {
                    MaterialModel m = Newtonsoft.Json.JsonConvert.DeserializeObject<MaterialModel>(value);
                    MaterialMap[id.Id] = m;
                    ObjectsIdMap[id.Id] = m;
                    recordPackageEntity(m?.packageName, m);
                }
                else if (id.Type == "StaticMesh")
                {
                    StaticMeshModel m = Newtonsoft.Json.JsonConvert.DeserializeObject<StaticMeshModel>(value);
                    MeshMap[id.Id] = m;
                    ObjectsIdMap[id.Id] = m;
                    recordPackageEntity(m?.packageName, m);
                }
                else if (id.Type == "Map")
                {
                    MapModel m = Newtonsoft.Json.JsonConvert.DeserializeObject<MapModel>(value);
                    MapMap[id.Id] = m;
                    ObjectsIdMap[id.Id] = m;
                    recordPackageEntity(m?.packageName, m);
                }
                else if (id.Type == "File")
                {
                    FileModel m = Newtonsoft.Json.JsonConvert.DeserializeObject<FileModel>(value);
                    FileMap[id.Id] = m;
                    ObjectsIdMap[id.Id] = m;
                    parseFileObj(m);
                }
                else if (id.Type == "Product")
                {
                    ProductModel m = Newtonsoft.Json.JsonConvert.DeserializeObject<ProductModel>(value);
                    ProductMap[id.Id] = m;
                    ObjectsIdMap[id.Id] = m;
                    if(m.specifications?.Count > 0)
                    {
                        var spec = m.specifications[0];
                        spec.PropStrToObj();
                        if(spec.staticMeshes?.Count > 0)
                        {
                            PackageProductMap[spec.staticMeshes[0].packageName] = m;
                        }
                        else if(spec.ComponentsObj?.Count > 0)
                        {
                            foreach (var comp in spec.ComponentsObj)
                            {
                                var meshcomp = comp as FProductStaticMeshComponent;
                                if (meshcomp != null)
                                {
                                    PackageProductMap[meshcomp.StaticMesh?.packageName] = m;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //fix file reference.
            foreach (var item in ObjectsIdMap.Values)
            {
                var asset = item as ClientAssetModel;
                if (asset == null)
                    continue;
                if(asset.fileAssetId != null && FileMap.ContainsKey(asset.fileAssetId))
                {
                    asset.fileAsset = FileMap[asset.fileAssetId];

                    string packageName = asset.packageName;
                    if (PackageFileMap.ContainsKey(packageName))
                    {
                        List<FileModel> list = PackageFileMap[packageName];
                        //do not add same file asset to list.
                        if (list.Find(obj => obj.id == asset.fileAssetId) == null)
                        {
                            list.Add(asset.fileAsset);
                        }
                    }
                    else
                    {
                        List<FileModel> list = new List<FileModel>();
                        PackageFileMap[packageName] = list;
                        list.Add(asset.fileAsset);
                    }
                }
            }
            StaticsticsString();
        }
    }
}
