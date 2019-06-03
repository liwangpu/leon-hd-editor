using BambooCook.Logic;
using BambooEditor.Logic;
using BambooUploader.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace BambooEditor.UI.upload
{
    /// <summary>
    /// main logic for assets management like uploading, downloading, deleting.
    /// </summary>
    public class AssetUploadMan
    {
        public HashSet<AssetFileModel> LocalFiles = new HashSet<AssetFileModel>();
        public HashSet<AssetFileModel> ServerFiles = new HashSet<AssetFileModel>();
        public ObservableCollection<AssetFileModel> AllFiles = new ObservableCollection<AssetFileModel>();
        public ProgressModel progress = new ProgressModel();
        public SvrDataCache cache;
        public SvrDataMan svrdataMan;
        public bool IsDownloading { get; set; }
        public bool IsUploading { get; set; }
        public bool IsServerDataLoaded { get; set; }
        ApiMan api = null;
        public string ProjDir = "";
        public bool IsCheckedForUpload { get; set; }
        public bool IsMd5Checked { get; set; }
        Thread md5Thread = null;
        bool md5ThreadRunning = false;

        public ObservableCollection<AssetFileOperate> UploadList = new ObservableCollection<AssetFileOperate>();
        public ObservableCollection<AssetFileModel> ProductList = new ObservableCollection<AssetFileModel>();
        Dictionary<string, AssetFileModel> localFileMap = new Dictionary<string, AssetFileModel>();

        public AssetUploadMan()
        {
            cache = new SvrDataCache();
            svrdataMan = new SvrDataMan(cache);
            IsDownloading = false;
            IsUploading = false;
            api = Logic.BambooEditor.Instance.api;
            IsServerDataLoaded = false;
        }

        public AssetFileModel GetLocalFile(string packageName)
        {
            AssetFileModel loc = null;
            localFileMap.TryGetValue(packageName, out loc);
            return loc;
        }

        public int ReloadFileList(string projectPath)
        {
            IsMd5Checked = false;
            if (md5Thread != null && md5Thread.IsAlive)
            {
                StopMd5Thread();
            }
            LocalFiles.Clear();
            localFileMap.Clear();

            string filePath = $"{projectPath}\\Saved\\AssetMan\\assetlist.txt";
            if (File.Exists(filePath) == false)
                return 0;

            progress.Status = "loading file";
            string content = "";
            try
            {
                StreamReader sr = new StreamReader(filePath);
                content = sr.ReadToEnd();
                sr.Close();
            }catch
            {
                return 0;
            }

            progress.Status = "deserialize file";
            FAssetListData data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<FAssetListData>(content);
            }catch
            {

            }
            if (data == null)
                return 0;

            progress.Status = "recoding file";
            recordLocalFile(data.DataMap);
            recordLocalFile(data.Dependencies);

            MergeFiles();
            IsCheckedForUpload = false;
            progress.Reset();
            progress.Status = "start calc md5";
            progress.Total = data.DataMap.Count + data.Dependencies.Count;
            md5ThreadRunning = true;
            md5Thread = new Thread(new ThreadStart(CalcFileMd5Thread));
            md5Thread.Start();

            return LocalFiles.Count;
        }

        public void StopMd5Thread()
        {
            md5ThreadRunning = false;
            if(md5Thread != null)
                md5Thread.Abort();
            md5Thread = null;
        }

        class Md5CacheItem
        {
            public string file { get; set; }
            public long size { get; set; }
            public DateTime updatetm { get; set; }
            public string md5 { get; set; }
        }
        void CalcFileMd5Thread()
        {
            if(File.Exists("md5cache.txt"))
            {
                StreamReader sr = new StreamReader("md5cache.txt", Encoding.UTF8);
                string s = sr.ReadToEnd();
                sr.Close();
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Md5CacheItem>>(s);
                if(items != null)
                {
                    foreach (var file in LocalFiles)
                    {
                        if(items.ContainsKey(file.cookedpath))
                        {
                            var item = items[file.cookedpath];
                            file.SizeStr = new FileSize((ulong)item.size).ToString();
                            file.UpdateTm = item.updatetm.ToString();
                            file.Md5 = item.md5;
                        }
                        else
                        {
                            FileInfo fi = new FileInfo(file.cookedpath);
                            file.SizeStr = new FileSize((ulong)fi.Length).ToString();
                            file.UpdateTm = fi.LastWriteTime.ToString();
                            file.Md5 = Md5Helper.Md5File(file.cookedpath);
                        }
                        progress.Status = file.Package;
                        progress.CurPos++;
                    }
                }
                IsMd5Checked = true;
                progress.Status = "md5 thread done";
                progress.Percent = 100;
                return;
            }

            Dictionary<string, Md5CacheItem> md5cache = new Dictionary<string, Md5CacheItem>();
            foreach (var file in LocalFiles)
            {
                if (md5ThreadRunning == false)
                {
                    progress.Status = "md5 thread quit";
                    progress.Percent = 100;
                    return;
                }
                if (file.CookedFileState == "Y")
                {
                    FileInfo fi = new FileInfo(file.cookedpath);
                    file.SizeStr = new FileSize((ulong)fi.Length).ToString();
                    file.UpdateTm = fi.LastWriteTime.ToString();
                    file.Md5 = Md5Helper.Md5File(file.cookedpath);

                    Md5CacheItem ci = new Md5CacheItem();
                    ci.file = file.cookedpath;
                    ci.size = fi.Length;
                    ci.md5 = file.Md5;
                    ci.updatetm = fi.LastWriteTime;
                    md5cache[file.cookedpath] = ci;
                }
                progress.Status = file.Package;
                progress.CurPos++;
            }
            string cachestr = Newtonsoft.Json.JsonConvert.SerializeObject(md5cache);
            StreamWriter sw = new StreamWriter("md5cache.txt", false, Encoding.UTF8);
            sw.WriteLine(cachestr);
            sw.Close();
            IsMd5Checked = true;
            progress.Status = "md5 thread done";
            progress.Percent = 100;
        }

        public static string TagsToStr(Dictionary<string, string> tags)
        {
            if (tags?.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            foreach (var item in tags)
            {
                sb.Append($"{item.Key}={item.Value};");
            }
            return sb.ToString();
        }

        string GetTypeFromClass(string className)
        {
            if (className.StartsWith("Material", StringComparison.CurrentCultureIgnoreCase))
                return "Material";
            if (className.StartsWith("StaticMesh", StringComparison.CurrentCultureIgnoreCase))
                return "StaticMesh";
            if (className.StartsWith("Texture", StringComparison.CurrentCultureIgnoreCase))
                return "Texture";
            if (className.StartsWith("World", StringComparison.CurrentCultureIgnoreCase))
                return "Map";
            return "";
        }

        void recordLocalFile(Dictionary<string, FAssetInfo> map)
        {
            if (map == null)
                return;
            foreach(var info in map.Values)
            {
                if (info == null)
                    continue;
                AssetFileModel model = new AssetFileModel(info);
                model.CookedFileState = File.Exists(info.LocalPath) ? "Y" : "";
                model.UncookedFileState = File.Exists(info.UnCookedFile?.LocalPath) ? "Y" : "";
                model.SourceFileState = File.Exists(info.SrcFile?.LocalPath) ? "Y" : "";
                model.cookedpath = info.LocalPath;
                model.uncookedpath = info.UnCookedFile?.LocalPath;
                model.srcpath = info.SrcFile?.LocalPath;
                model.Dependencies = info.Dependencies;
                model.properties = TagsToStr(info.Tags);
                model.Type = GetTypeFromClass(info.Class);
                if(model.Dependencies?.Count > 0)
                {
                    var icondep = model.Dependencies.FirstOrDefault(t => t.Value.LocalPath?.IndexOf("UploadIcons/") > 0);
                    if (icondep.Value != null)
                        model.iconpath = icondep.Value.LocalPath;
                }
                LocalFiles.Add(model);
                localFileMap[model.Package] = model;
            }
        }

        public async Task<string> GetServerDataSizeStr()
        {
            return await svrdataMan.GetServerDataCountString();
        }

        public async Task<int> LoadFromServer(bool getFullData)
        {
            int count = await svrdataMan.LoadAllData(getFullData);
            ParseData();
            MergeFiles();
            IsServerDataLoaded = true;
            IsCheckedForUpload = false;
            return count;
        }

        public int LoadFromCache()
        {
            int count = svrdataMan.LoadFromCache();
            ParseData();
            MergeFiles();
            IsServerDataLoaded = true;
            IsCheckedForUpload = false;
            return count;
        }

        private void MergeFiles()
        {
            AllFiles.Clear();
            foreach(var file in LocalFiles)
            {
                AllFiles.Add(file);
            }
            foreach(var file in ServerFiles)
            {
                AllFiles.Add(file);
            }
        }

        public void RemoveFile(AssetFileModel file)
        {
            AllFiles.Remove(file);
            LocalFiles.Remove(file);
            ServerFiles.Remove(file);
            cache.Remove(file.ObjId);
        }

        List<AssetFileModel> localAdd = new List<AssetFileModel>();
        List<AssetFileModel> localNew = new List<AssetFileModel>();
        List<SvrDataCache.DataCacheItemInfoModel> deleteList = new List<SvrDataCache.DataCacheItemInfoModel>();

        public string CheckForUpload()
        {
            if(IsMd5Checked == false)
            {
                return "请等待md5计算结束";
            }

            UploadList.Clear();

            //check files to upload, create upload task first.
            foreach (var item in LocalFiles)
            {
                var svrObj = cache.Parsed.GetNewestObjByPackageName(item.Package);

                //upload it if need.
                if (svrObj == null  //server no this file
                    || string.Compare(svrObj.fileAsset?.md5, item.Md5, true) != 0  //or svr file is different
                    || string.IsNullOrEmpty(svrObj.fileAsset?.url) //or svr file download url is invalid.
                    )
                {
                    AssetFileOperate op = new AssetFileOperate();
                    op.File = item;
                    op.Operate = AssetFileOperateType.UploadFile;
                    UploadList.Add(op);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"本地资源 {LocalFiles.Count}, 服务器资源 {cache.Parsed.PackageObjMap.Count}, 服务器对象 {ServerFiles.Count}");
            foreach (var item in LocalFiles)
            {
                var assetFiles = cache.Parsed.GetFileAssetsByPackageName(item.Package);
                var objOnPackage = cache.Parsed.GetNewestObjByPackageName(item.Package);
                var productOnPackage = cache.Parsed.GetProductOnPackage(item.Package);
                ProductSpecModel specOnPackage = null;

                if(objOnPackage != null)
                {
                    item.ObjId = objOnPackage.id;
                }

                if (productOnPackage?.specifications?.Count > 0 && string.IsNullOrEmpty(productOnPackage.specifications[0].id) == false)
                {
                    specOnPackage = productOnPackage.specifications[0];
                }

                if (objOnPackage == null) // server no this asset file, no staticmesh/material/texture/map.
                {
                    if (item.IsInWorkDir == false)
                        continue;//only create objects for item in work dir.
                    localAdd.Add(item);
                    AssetFileOperate op = new AssetFileOperate();
                    op.File = item;
                    op.Operate = AssetFileOperateType.Create;

                    //no file asset but exist a product to reference it.
                    if (productOnPackage != null)
                    {
                        if(productOnPackage.specifications?.Count > 0 && string.IsNullOrEmpty(productOnPackage.specifications[0].id) == false)
                        {
                            op.Target = productOnPackage.specifications[0].id;
                            op.specId = productOnPackage.specifications[0].id;
                            op.spec = productOnPackage.specifications[0];
                            op.Operate = AssetFileOperateType.CreateAndUpdateSpec;
                        }
                        else
                        {
                            AssetFileOperate op2 = new AssetFileOperate();
                            op2.File = item;
                            op2.Operate = AssetFileOperateType.DeleteProduct;
                            op2.Target = productOnPackage.id;
                            op2.productId = productOnPackage.id;
                            UploadList.Add(op2);
                        }
                    }
                    
                    UploadList.Add(op);
                }
                else
                {
                    //file asset existed. 

                    bool isFileChanged = string.Equals(objOnPackage.fileAsset?.md5, item.Md5, StringComparison.CurrentCultureIgnoreCase) == false;
                    if(objOnPackage.IsDependenciesValid() == false)
                    {
                        isFileChanged = true;// force update invalid object.
                    }
                    if (isFileChanged)
                    {
                        //file changed.
                        AssetFileOperate op = new AssetFileOperate();
                        op.File = item;
                        op.Operate = AssetFileOperateType.Update;
                        op.Target = objOnPackage.id;
                        UploadList.Add(op);
                    }

                }


                if (item.Class == "StaticMesh")
                {
                    //product mising
                    if (specOnPackage == null)
                    {
                        AssetFileOperate op = new AssetFileOperate();
                        op.File = item;
                        op.Operate = AssetFileOperateType.CreateMissingProduct;
                        UploadList.Add(op);
                    }
                    else // product exist.
                    {
                        string specmeshid = "";
                        if (specOnPackage.staticMeshes?.Count > 0)
                            specmeshid = specOnPackage.staticMeshes[0].id;
                        if (specOnPackage.ComponentsObj?.Count > 0)
                        {
                            foreach (var comp in specOnPackage.ComponentsObj)
                            {
                                var meshcomp = comp as FProductStaticMeshComponent;
                                if (meshcomp?.StaticMesh != null)
                                {
                                    specmeshid = meshcomp.StaticMesh.id;
                                }
                            }
                        }
                        //product do not use latest mesh
                        if (string.Compare(objOnPackage.id, specmeshid, true) != 0)
                        {
                            AssetFileOperate op = new AssetFileOperate();
                            op.File = item;
                            op.Operate = AssetFileOperateType.ProductUseLatestMesh;
                            op.Target = specOnPackage.id;
                            op.spec = specOnPackage;
                            op.specId = specOnPackage.id;
                            UploadList.Add(op);
                        }
                    }
                } //if item is static mesh
            } //foreach
            sb.AppendLine($"检测到改动 {UploadList.Count}");

            IsCheckedForUpload = true;
            return sb.ToString();
        }

        void updateFileDependencies(AssetFileModel file)
        {
            if (file.Dependencies?.Count > 0)
            {
                StringBuilder depsb = new StringBuilder();
                foreach (var item in file.Dependencies)
                {
                    string package = item.Key;
                    var depfile = LocalFiles.FirstOrDefault(t => t.Package == package); // find dependency file by package.
                    string url = depfile?.cookedurl;
                    if (string.IsNullOrEmpty(url))
                    {
                        //local file may not upload this file as it not changed. so get url from svr obj.
                        var svrobj = cache.Parsed.GetNewestObjByPackageName(package);
                        url = svrobj?.fileAsset?.url;
                    }
                    //depsb.Append($"{depfile?.cookedurl},{item.Key},{depfile?.ObjId};");
                    depsb.Append($"{url},{package};");
                }
                file.dependencies = depsb.ToString();
            }
        }

        private async Task<string> CreateAssetObjForFile(AssetFileModel file, StreamWriter loger)
        {
            if (file == null)
                return "";
            string apiname = SvrDataMan.GetApiNameByTypeName(file.Type);

            if (apiname == "")
            {
                loger.WriteLine($"{DateTime.Now}, CREATE, {file.Package}, {file.Type}, apiname is empty. skip create object");
                return "";
            }

            //update dependencies after all files uploaded.
            if (file.Dependencies?.Count > 0)
            {
                updateFileDependencies(file);
            }

            ApiResponseStr res = null;
            StaticMeshModel mesh = null;

            //create
            var obj = new StaticMeshModel(); //all assets has same structure. but use staticmesh for safety for staticmesh case.
            obj.name = file.Name;
            obj.packageName = file.Package;

            obj.fileAssetId = file.cookedfid;
            obj.unCookedAssetId = file.uncookedfid;
            obj.srcFileAssetId = file.srcfid;
            obj.iconAssetId = file.iconfid;
            obj.dependencies = file.dependencies;
            obj.properties = file.properties;
            obj.parameters = file.properties;
            res = await api.PostAsync($"/{apiname}", obj);
            if (res.IsSuccess && string.IsNullOrEmpty(res.Content) == false)
            {
                ClientAssetModel resobj = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientAssetModel>(res.Content);
                file.ObjId = resobj?.id;
                obj.id = resobj?.id;
                loger.WriteLine($"{DateTime.Now}, CREATE, {file.Package}, {file.Type}, {file.ObjId}");
                cache.New(file.ObjId, file.Type, obj);
            }
            else
            {
                loger.WriteLine($"{DateTime.Now}, CREATE, {file.Package}, {file.Type}, create failed. {res.StatusCode}, {res.Content}");
            }
            mesh = obj as StaticMeshModel;
            return res.IsSuccess ? "ok" : "failed " + res.StatusCode;
        }


        private async Task<string> UpdateAssetObjForFile(AssetFileModel file, string assetObjid, StreamWriter loger)
        {
            if (file == null)
                return "";
            string apiname = SvrDataMan.GetApiNameByTypeName(file.Type);

            if (apiname == "")
            {
                loger.WriteLine($"{DateTime.Now}, CREATE, {file.Package}, {file.Type}, apiname is empty. skip create object");
                return "";
            }

            //update dependencies after all files uploaded.
            if (file.Dependencies?.Count > 0)
            {
                updateFileDependencies(file);
            }

            ApiResponseStr res = null;
            StaticMeshModel mesh = null;

            var obj = cache.Parsed.GetById(assetObjid) as ClientAssetModel;
            if (obj == null)
                return "";
            mesh = obj as StaticMeshModel;
            if (string.IsNullOrEmpty(file.cookedfid) == false)
                obj.fileAssetId = file.cookedfid;
            if (string.IsNullOrEmpty(file.uncookedfid) == false)
                obj.unCookedAssetId = file.uncookedfid;
            if (string.IsNullOrEmpty(file.srcfid) == false)
                obj.srcFileAssetId = file.srcfid;
            if (string.IsNullOrEmpty(file.iconfid) == false)
                obj.iconAssetId = file.iconfid;
            obj.id = assetObjid;
            obj.name = file.Name;
            obj.dependencies = file.dependencies;
            obj.parameters = file.properties;
            res = await api.PutAsync($"/{apiname}", obj); //现在的api版本，put 不需要id
                                                          //res = await api.PutAsync($"/{apiname}/{assetObjid}", obj);
            file.ObjId = assetObjid;

            if (res.IsSuccess && string.IsNullOrEmpty(res.Content) == false)
            {
                loger.WriteLine($"{DateTime.Now}, UPDATE, {file.Package}, {file.Type}, {assetObjid}, OK");
                cache.Update(assetObjid, obj);
            }
            else
            {
                loger.WriteLine($"{DateTime.Now}, UPDATE, {file.Package}, {file.Type}, {assetObjid}, failed {res.StatusCode} {res.Content}");
            }
            return res.IsSuccess ? "ok" : "failed " + res.StatusCode;
        }

        private async Task<string> CreateProductForMesh(AssetFileModel file, StreamWriter loger)
        {
            StaticMeshModel mesh = cache.Parsed.GetById(file.ObjId) as StaticMeshModel;

            if(mesh == null)
            {
                return "mesh obj not found " + file.ObjId;
            }

            ProductSpecModel spec = new ProductSpecModel();
            spec.name = file.Name;
            spec.iconAssetId = file.iconfid;
            spec.IconUrl = file.iconurl;
            spec.ComponentsObj = new List<FProductComponentModel>();
            FProductStaticMeshComponent meshcomp = new FProductStaticMeshComponent();
            meshcomp.StaticMesh = mesh;
            meshcomp.Id = mesh.id;
            meshcomp.Name = mesh.name;
            meshcomp.Icon = file.iconurl;
            spec.ComponentsObj.Add(meshcomp);
            spec.staticMeshIds = "{\"Items\":[{\"StaticMeshId\":\"" + mesh.id + "\",\"MaterialIds\":[]}]}";
            spec.PropObjToStr();

            ProductModel product = new ProductModel();
            product.name = file.Name;
            product.iconAssetId = file.iconfid;
            product.IconUrl = file.iconurl;
            var prodres = await api.PostAsync<ProductModel>("/products", product);
            string productId = "";
            string specId = "";
            if (prodres.IsSuccess && prodres.Content != null && prodres.Content.specifications?.Count > 0)
            {
                productId = prodres.Content.id;
                specId = prodres.Content.specifications[0]?.id;
                spec.id = specId;
                var specres = await api.PutAsync("/productspec", spec);//现在的api版本，put 不需要id
                if (specres.IsSuccess)
                {
                    loger.WriteLine($"{DateTime.Now}, CREATE, {file.Package}, {file.Type}, {file.ObjId}, productid {prodres.Content.id} SpecId {specId} update ok");
                }
                else
                {
                    loger.WriteLine($"{DateTime.Now}, CREATE, {file.Package}, {file.Type}, {file.ObjId}, productid {prodres.Content.id} SpecId {specId} update failed. {specres.StatusCode} {specres.Content}");
                }
            }
            else
            {
                loger.WriteLine($"{DateTime.Now}, CREATE, {file.Package}, {file.Type}, {file.ObjId}, create product failed. {prodres.StatusCode} {prodres.Content}");
            }
            string ids = $"product id {productId} specid {specId}";
            return prodres.IsSuccess ? "ok " + ids : "failed " + prodres.StatusCode;
        }

        private async Task<string> UpdateProductSpecMesh(AssetFileModel file, ProductSpecModel spec, StreamWriter loger)
        {
            StaticMeshModel mesh = cache.Parsed.GetById(file.ObjId) as StaticMeshModel;

            if (mesh == null)
            {
                return "mesh obj not found " + file.ObjId;
            }

            spec.name = file.Name;
            spec.iconAssetId = file.iconfid;
            spec.ComponentsObj = new List<FProductComponentModel>();
            FProductStaticMeshComponent meshcomp = new FProductStaticMeshComponent();
            meshcomp.StaticMesh = mesh;
            meshcomp.Id = mesh.id;
            meshcomp.Name = mesh.name;
            meshcomp.Icon = file.iconurl;
            spec.ComponentsObj.Add(meshcomp);
            spec.staticMeshIds = "{\"Items\":[{\"StaticMeshId\":\"" + mesh.id + "\",\"MaterialIds\":[]}]}";
            spec.PropObjToStr();

            var res = await api.PutAsync<ProductSpecModel>("/productspec", spec);
            return res.IsSuccess ? "ok" : "failed " + res.StatusCode;
        }
        
        public async Task<string> Upload()
        {
            if (IsCheckedForUpload == false)
                return "not checked before upload";

            progress.Reset();
            IsUploading = true;

            progress.Total = UploadList.Count;

            StringBuilder sb = new StringBuilder();
            StreamWriter loger = new StreamWriter("uploadlog.txt", true, Encoding.UTF8);
            loger.WriteLine(">>>>>>>>UPLOAD START>>>>>>>>");

            Logic.BambooEditor editor = Logic.BambooEditor.Instance;

            foreach (var item in UploadList)
            {
                //need to upload files.
                //if(item.Operate == AssetFileOperateType.Create || item.Operate == AssetFileOperateType.CreateAndUpdateSpec || item.Operate == AssetFileOperateType.Update)
                if(item.Operate == AssetFileOperateType.UploadFile)
                {
                    var file = item.File;
                    var upfile = await editor.UploadFile(file.cookedpath); cache.New(upfile?.id, "File", upfile);
                    file.cookedfid = upfile?.id; file.cookedurl = upfile?.url;
                    upfile = await editor.UploadFile(file.uncookedpath); cache.New(upfile?.id, "File", upfile);
                    file.uncookedfid = upfile?.id;
                    upfile = await editor.UploadFile(file.srcpath); cache.New(upfile?.id, "File", upfile);
                    file.srcfid = upfile?.id;
                    upfile = await editor.UploadFile(file.iconpath); cache.New(upfile?.id, "File", upfile);
                    file.iconfid = upfile?.id; file.iconurl = upfile?.url;
                    loger.WriteLine($"{DateTime.Now}, UPLOAD, {file.Package}, cf {file.cookedfid}, uf {file.uncookedfid}, sf {file.srcfid}, icon {file.iconfid}, cp {file.cookedpath}, up {file.uncookedpath}, sp {file.srcpath}, ip {file.iconpath}");
                    progress.CurPos++;
                    progress.Status = "upload " + file.Package;
                }

                var objOnPackage = cache.Parsed.GetNewestObjByPackageName(item.File.Package);
                if (item.Operate == AssetFileOperateType.Create)
                {
                    string res = await CreateAssetObjForFile(item.File, loger);
                    progress.CurPos++;
                    progress.Status = "create " + item.File.Package + " " + res;
                    loger.WriteLine($"{DateTime.Now}, CREATE, {item.File.Package}, {item.File.Name}, {item.File.Type}, objid {item.File.ObjId} {res}");
                }
                else if (item.Operate == AssetFileOperateType.CreateAndUpdateSpec)
                {
                    string res = await CreateAssetObjForFile(item.File, loger);
                    string res2 = await UpdateProductSpecMesh(item.File, item.spec, loger);
                    progress.CurPos++;
                    progress.Status = "create and update " + item.File.Package + " " + res;
                    loger.WriteLine($"{DateTime.Now}, CREATE and UPDATE, {item.File.Package}, {item.File.Name}, {item.File.Type}, objid {item.File.ObjId}, specid {item.spec?.id} create {res} update {res2}");
                }
                else if (item.Operate == AssetFileOperateType.CreateMissingProduct)
                {
                    string res = await CreateProductForMesh(item.File, loger);
                    progress.CurPos++;
                    progress.Status = "create product for " + item.File.Package + " " + res;
                    loger.WriteLine($"{DateTime.Now}, CREATE product for mesh, {item.File.Package}, {item.File.Name}, {item.File.Type}, objid {item.File.ObjId} {res}");
                }
                else if (item.Operate == AssetFileOperateType.DeleteProduct)
                {
                    string productid = item.productId;
                    progress.CurPos++;
                    progress.Status = "delete " + productid;
                    await api.DeleteAsync("products/" + productid);
                    loger.WriteLine($"{DateTime.Now}, DELETE, {item.File.Package}, {item.File.Name}");
                    cache.Remove(productid);
                }
                else if (item.Operate == AssetFileOperateType.ProductUseLatestMesh)
                {
                    string res = await UpdateProductSpecMesh(item.File, item.spec, loger);
                    loger.WriteLine($"{DateTime.Now}, UPDATE spec use mesh, {item.File.Package}, {item.File.Name}, {item.File.Type}, objid {item.File.ObjId}, specid {item.spec?.id} {res}");

                    progress.CurPos++;
                    progress.Status = "update spec mesh " + item.File.Package + " " + res;
                }
                else if (item.Operate == AssetFileOperateType.Update)
                {
                    string res = await UpdateAssetObjForFile(item.File, objOnPackage?.id, loger);
                    progress.CurPos++;
                    progress.Status = "update " + item.File.Package + " to " + objOnPackage?.id + " " + res;

                    loger.WriteLine($"{DateTime.Now}, UPDATE, {item.File.Package}, {item.File.Name}, {item.File.Type}, objid {item.File.ObjId} {res}");
                }
            }
            
            loger.WriteLine(">>>>>>>>UPLOAD END>>>>>>>>");

            loger.Close();
            cache.Save("datacache.dat");
            progress.Percent = 100;
            progress.Status = "finished";
            IsUploading = false;
            return sb.ToString();
        }
        
        AssetFileModel EntityToAssetFileModel(ClientAssetModel e, string type, string classs)
        {
            AssetFileModel model = new AssetFileModel();
            model.Type = type;
            model.Class = classs;
            model.Name = e.name;
            model.FileId = e.fileAssetId;
            model.ObjId = e.id;
            model.Package = e.packageName;
            model.SizeStr = new FileSize(e.fileAsset?.size).ToString();
            model.CookedFileState = string.IsNullOrEmpty(e.fileAssetId) ? "Y" : "";
            model.UncookedFileState = string.IsNullOrEmpty(e.unCookedAssetId) ? "Y" : "";
            model.SourceFileState = string.IsNullOrEmpty(e.srcFileAssetId) ? "Y" : "";
            model.UpdateTm = e.modifiedTime.ToString();
            return model;
        }

        private void ParseData()
        {
            ServerFiles.Clear();
            foreach (var e in cache.Parsed.TextureMap.Values)
            {
                ServerFiles.Add(EntityToAssetFileModel(e, "Texture", "Texture2D"));
            }
            foreach (var e in cache.Parsed.MeshMap.Values)
            {
                ServerFiles.Add(EntityToAssetFileModel(e, "StaticMesh", "StaticMesh"));
            }
            foreach (var e in cache.Parsed.MaterialMap.Values)
            {
                ServerFiles.Add(EntityToAssetFileModel(e, "Material", "Material"));
            }
            foreach (var e in cache.Parsed.MapMap.Values)
            {
                ServerFiles.Add(EntityToAssetFileModel(e, "Map", "Map"));
            }

            ProductList.Clear();
            foreach (var e in cache.Parsed.PackageProductMap)
            {
                AssetFileModel file = new AssetFileModel();
                file.Package = e.Key;
                file.ObjId = e.Value.id;
                file.Name = e.Value.name;
                file.UpdateTm = e.Value.modifiedTime.ToString();
                ProductList.Add(file);
            }
            //foreach (var e in cache.Parsed.ProductMap.Values)
            //{
            //    BambooUploader.Logic.StaticMeshModel mesh = null;
            //    if (e.specifications?.Count > 0 && e.specifications[0].staticMeshes?.Count > 0)
            //        mesh = e.specifications[0].staticMeshes[0];
            //    AssetFileModel model = EntityToAssetFileModel(mesh, "Product", "Product");
            //    model.ObjId = e.id;
            //    model.UpdateTm = e.modifiedTime.ToString();
            //    ServerFiles.Add(model);
            //}
        }


        public void DumpList()
        {
            StreamWriter writer = new StreamWriter("uploadlist-dump.csv", false, Encoding.UTF8);
            writer.WriteLine("type,package,class,fileid,objid,size,cookedfile,uncookedfile,sourcefile,progress,status,name,updatetm");
            foreach (var item in AllFiles)
            {
                writer.WriteLine($"{item.Type},{item.Package},{item.Class},{item.FileId},{item.ObjId},{item.SizeStr},{item.CookedFileState},{item.UncookedFileState},{item.SourceFileState},{item.Progress},{item.Status},{item.Name},{item.UpdateTm}");
            }
            writer.Close();
        }

        public string packageNameToLocalName(string packageName)
        {
            if(packageName.StartsWith("/Game/"))
            {
                string ext = ".uasset";
                if (packageName.StartsWith("/Game/Maps/", StringComparison.CurrentCultureIgnoreCase))
                    ext = ".umap";

                return Path.Combine(ProjDir, "Content", packageName.Substring(6) + ext);
            }
            return "";
        }

        public string getFileModelDownloadPath(FileModel file)
        {
            if (string.IsNullOrEmpty(file?.localPath))
                return "";
            int idindex = file.localPath.IndexOf("/Content/");
            if (idindex > 0)
            {
                return ProjDir + "/" + file.localPath.Substring(idindex);
            }
            return "";
        }

        public string getPackageNameFromLocalPath(string localPath)
        {
            int idindex = localPath.IndexOf("/Content/");
            if(idindex >= 0)
            {
                string packageName = "/Game/" + localPath.Substring(idindex + 9);
                idindex = packageName.LastIndexOf(".");
                if (idindex > 0)
                {
                    packageName = packageName.Substring(0, idindex);
                }
                return packageName;
            }
            return "";
        }
        public async Task<int> DownloadAsset(object obj, StreamWriter loger)
        {
            ClientAssetModel asset = obj as ClientAssetModel;
            
            if (asset == null || string.IsNullOrEmpty(asset?.unCookedAssetId))
            {
                progress.CurPos++;
                progress.Status = "";
                loger.WriteLine($"{progress.CurPos} invalid object. uncooked assetid {asset?.unCookedAssetId}");
                return 0;
            }

            //get file from cache.
            FileModel file = cache.Parsed.GetById(asset.unCookedAssetId) as FileModel;

            //get file from server it not found in cache.
            if(file == null)
            {
                var fileres = await api.GetAsync<FileModel>("/files/" + asset.unCookedAssetId);
                if (fileres.IsSuccess == false || fileres.Content == null)
                {
                    progress.CurPos++;
                    progress.Status = "";
                    loger.WriteLine($"{progress.CurPos} object {asset.id} {asset.name} get file asset failed.");
                    return 0;
                }
                file = fileres.Content;
            }
            string localPath = getFileModelDownloadPath(file);

            //download dependencies
            if(string.IsNullOrEmpty(asset?.dependencies) == false)
            {
                char[] spliter1 = { ';' };
                char[] spliter2 = { ',' };
                var deps = asset.dependencies.Split(spliter1);
                loger.WriteLine($"{progress.CurPos} object {asset.id} {asset.name} add {deps.Length} dependencies.");
                progress.Total += deps.Length;
                foreach (var dep in deps)
                {
                    var depinfo = dep.Split(spliter2);
                    if (depinfo == null || depinfo.Length < 2)
                    {
                        progress.CurPos++;
                        continue;
                    }
                    string url = depinfo[0];
                    string package = depinfo[1];
                    ClientAssetModel dependAsset = null;
                    if (cache.Parsed.PackageObjMap.ContainsKey(package))
                    {
                        dependAsset = cache.Parsed.PackageObjMap[package] as ClientAssetModel;
                    }
                    if(dependAsset != null)
                    {
                        await DownloadAsset(dependAsset, loger);
                    }
                }
            }

            if (File.Exists(localPath))
            {
                string md5 = Md5Helper.Md5File(localPath);
                if(md5 == file.md5)
                {
                    return 0;
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
            string message = "";
            try
            {
                await api.DownloadFile(file.url, localPath);
            }
            catch(Exception ex)
            {
                message = ex.Message;
                loger.WriteLine($"{progress.CurPos} object {asset.id} {asset.name} download failed. {ex.Message}");
            }
            progress.CurPos++;
            progress.Status = $"{asset.id} {asset.name} {message}";
            loger.WriteLine($"{progress.CurPos} object {asset.id} {asset.name} download success");
            loger.Flush();
            return 1;
        }

        public async Task<int> DownloadAllAssets()
        {
            progress.Reset();
            IsDownloading = true;
            progress.Total = cache.Parsed.UnCookedPackageFileMap.Count;

            StreamWriter loger = new StreamWriter("downloadlog.txt", true, Encoding.UTF8);
            loger.WriteLine("==================================" + DateTime.Now.ToString());
            loger.WriteLine("total " + progress.Total);

            foreach (var item in cache.Parsed.UnCookedPackageFileMap.Values)
            {
                string localPath = getFileModelDownloadPath(item);
                //ThreadPool.QueueUserWorkItem(DownloadAsset, item);
                try
                {
                    await api.DownloadFile(item.url, localPath);
                }
                catch (Exception ex)
                {
                    loger.WriteLine($"{progress.CurPos} {item.name} download failed. {ex.Message}");
                }
                progress.CurPos++;
            }
            //foreach (var item in cache.Parsed.PackageObjMap.Values)
            //{
            //    //ThreadPool.QueueUserWorkItem(DownloadAsset, item);
            //    await DownloadAsset(item, loger);
            //}
            loger.WriteLine(">>>>>>>>>>>>>>>>>>>>>>end");
            loger.Close();

            progress.Percent = 100;
            progress.Status = "ok";
            IsDownloading = false;
            return 0;
        }

        public string GetDownloadFileStat()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("total assets: " + cache.Parsed.PackageObjMap.Count);
            int cookedValid = 0;
            int uncookedValid = 0;
            int cookedInvalid = 0;
            int uncookedInvalid = 0;
            ulong totalsizeCooked = 0;
            ulong totalsizeUnCooked = 0;
            foreach (var item in cache.Parsed.PackageObjMap.Values)
            {
                if (item?.fileAssetId == null && item?.unCookedAssetId == null)
                {
                    continue;
                }

                if (item.fileAssetId != null)
                {
                    var file = cache.Parsed.GetById(item.fileAssetId) as FileModel;
                    if (file != null)
                    {
                        totalsizeCooked += (ulong)file.size;
                        cookedValid++;
                    }
                    else
                    {
                        cookedInvalid++;
                    }
                }

                if (item.unCookedAssetId != null)
                {
                    var file = cache.Parsed.GetById(item.unCookedAssetId) as FileModel;
                    if (file != null)
                    {
                        totalsizeUnCooked += (ulong)file.size;
                        uncookedValid++;
                    }
                    else
                    {
                        uncookedInvalid++;
                    }
                }
            }
            string filesizeCooked = new FileSize(totalsizeCooked).ToString();
            string filesizeUnCooked = new FileSize(totalsizeUnCooked).ToString();
            sb.AppendLine($"cooked valid {cookedValid} Invalid {cookedInvalid} size {filesizeCooked}");
            sb.AppendLine($"uncooked valid {uncookedValid} invalid {uncookedInvalid} size {filesizeUnCooked}");

            totalsizeCooked = 0;
            totalsizeUnCooked = 0;
            foreach (var item in cache.Parsed.CookedPackageFileMap.Values)
            {
                if (item == null)
                    continue;
                totalsizeCooked += (ulong)item.size;
            }
            foreach (var item in cache.Parsed.UnCookedPackageFileMap.Values)
            {
                if (item == null)
                    continue;
                totalsizeUnCooked += (ulong)item.size;
            }
            filesizeCooked = new FileSize(totalsizeCooked).ToString();
            filesizeUnCooked = new FileSize(totalsizeUnCooked).ToString();

            sb.AppendLine();
            sb.AppendLine("--- approach 2, direct from files ---");
            sb.AppendLine($"cooked files {cache.Parsed.CookedPackageFileMap.Count} size {filesizeCooked} uncooked files {cache.Parsed.UnCookedPackageFileMap.Count} size {filesizeUnCooked}");


            return sb.ToString();
        }
    }
}
