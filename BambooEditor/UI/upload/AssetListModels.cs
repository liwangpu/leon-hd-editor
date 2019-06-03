using BambooUploader.Logic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.UI.upload
{
    public class FAssetListData
    {
        public Dictionary<string, FAssetInfo> DataMap { get; set; }
        public Dictionary<string, FAssetInfo> Dependencies { get; set; }
    };

    public class FAssetInfo
    {
        //包的路径， /Game/Meshes/Abc
        public string Package { get; set; }
        //对象名比如 Abc。 默认情况下对象名和包名是一样的，但引擎也允许不一样。 比如Content/Meshes/Abc.uasset文件对象路径为 StaticMesh"/Game/Meshes/Abc.Abc"

        public string Name { get; set; }
        //对象的类名，比如 StaticMesh
        public string Class { get; set; }
        //资源的标签，比如模型面数，uv数，是否有碰撞等等
        public Dictionary<string, string> Tags { get; set; }
        //资源的依赖项
        public Dictionary<string, FAssetDependency> Dependencies { get; set; }

        //上传相关的数据---------------------------------------

        //包的本地文件名， 比如 d:\Project\Content\Abc.uasset ,d:\Project\Content\Abc.umap
        public string LocalPath { get; set; }
        //此文件在服务器上的文件ID
        public string SvrFileID { get; set; }
        //服务器上此文件的下载路径
        public string SvrFileUrl { get; set; }
        //基于此文件在服务器上创建的对象的ID，比如基于此StaticMeshId, MaterialId, TextureId
        public string SvrAssetId { get; set; }
        //基于此文件在服务器上创建的二级对象的ID，比如基于StaticMeshId 创建的产品对象 ProductSpecId
        public string SvrAssetId2 { get; set; }
        //基于此文件在服务器上创建的三级对象的ID，比如基于StaticMeshId 创建的产品对象 ProductId
        public string SvrAssetId3 { get; set; }
        //上传此文件的时间
        public string SvrUploadTime { get; set; }

        //内部使用，图标文件在Dependencies里面的Package
        public string IconPackageName { get; set; }

        //未cook的uasset文件， FileAssetId指的是Cooked uasset文件。记录Uncooked版本用来方便升级引擎版本
        public FAssetDependency UnCookedFile { get; set; }

        //资源的原始文件
        public FAssetDependency SrcFile { get; set; }
    };

    public class FAssetDependency
    {
        public string Package { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public int Level { get; set; }

        //包的本地文件名， 比如 d:\Project\Content\Abc.uasset ,d:\Project\Content\Abc.umap
        public string LocalPath { get; set; }
        //file id on server
        public string FileAssetId { get; set; }
        //objid static mesh id, material id...
        public string ObjId { get; set; }
    };

    public class AssetFileModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public AssetFileModel() { }
        public AssetFileModel(FAssetInfo info)
        {
            name = info.Name;
            package = info.Package;
            classs = info.Class;
            fileid = info.SvrFileID;
            objid = info.SvrAssetId;
            localpath = info.LocalPath;
        }

        int progress;
        string status,sizestr,cfs,ucfs,sfs,name,package,classs,fileid,objid,updatetm,type,md5,localpath;

        public string Type { get { return type; } set { type = value; notify("Type"); } }
        public string Package { get { return package; } set { package = value; notify("Package"); } }
        public string Class { get { return classs; } set { classs = value; notify("Class"); } }
        public string FileId { get { return fileid; } set { fileid = value; notify("FileId"); } }
        public string ObjId { get { return objid; } set { objid = value; notify("ObjId"); } }
        public string SizeStr { get { return sizestr; } set { sizestr = value; notify("SizeStr"); } }
        public string CookedFileState { get { return cfs; } set { cfs = value; notify("CookedFileState"); } }
        public string UncookedFileState { get { return ucfs; } set { ucfs = value; notify("UncookedFileState"); } }
        public string SourceFileState { get { return sfs; } set { sfs = value; notify("SourceFileState"); } }
        public int Progress { get { return progress; } set { progress = value; notify("Progress"); } }
        public string Status { get { return status; } set { status = value; notify("Status"); } }
        public string Name { get { return name; } set { name = value; notify("Name"); } }
        public string UpdateTm { get { return updatetm; } set { updatetm = value; notify("UpdateTm"); } }
        public string Md5 { get { return md5; } set { md5 = value; notify("Md5"); } }

        public string cookedpath, uncookedpath, srcpath,iconpath, cookedfid, uncookedfid, srcfid, iconfid, dependencies, properties;
        public string cookedurl, iconurl;
        public Dictionary<string, FAssetDependency> Dependencies;

        public bool IsInWorkDir
        {
            get
            {
                if (string.IsNullOrEmpty(package))
                    return false;
                if (package.StartsWith("/Game/Textures", StringComparison.CurrentCultureIgnoreCase)
                    || package.StartsWith("/Game/Materials", StringComparison.CurrentCultureIgnoreCase) 
                    || package.StartsWith("/Game/Meshes", StringComparison.CurrentCultureIgnoreCase) 
                    || package.StartsWith("/Game/Maps", StringComparison.CurrentCultureIgnoreCase))
                    return true;
                return false;
            }
        }

        public override string ToString()
        {
            return package + " " + type;
        }
    }

    public enum AssetFileOperateType
    {
        Create,
        DeleteProduct,
        Update,
        CreateAndUpdateSpec,
        CreateMissingProduct,
        ProductUseLatestMesh,
        UploadFile
    }

    public class AssetFileOperate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        string target, status;
        AssetFileOperateType op;
        public AssetFileModel File { get; set; }
        public AssetFileOperateType Operate { get { return op; } set { op = value; notify("Operate"); } }
        public string Target { get { return target; } set { target = value; notify("Target"); } }
        public string Status { get { return status; } set { status = value; notify("Status"); } }

        public ProductSpecModel spec;
        public string productId;
        public string specId;
    }
}
