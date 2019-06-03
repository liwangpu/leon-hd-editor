using BambooUploader.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.UI.design.material
{
    public class MaterialFactoryItem
    {
        public string Name { get; set; }
        public string Textures { get; set; }
        public List<MaterialTextureInfo> TextureList { get; set; }
        public string CategoryId { get; set; }
        public string CategoryPath { get; set; }
        public string MaterialTempateName { get; set; }

        public void PropObjToString()
        {
            if(TextureList == null)
            {
                Textures = "";
                return;
            }
            StringBuilder sb = new StringBuilder();
            foreach (var item in TextureList)
            {
                if (item == null)
                    continue;
                sb.Append(item.Name + "_" + item.ParamName + ";");
            }
            Textures = sb.ToString();
        }
    }
    

    public class MaterialTextureInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string ParamName { get; set; }
        public string RealSize { get; set; }
    }

    public class MaterialFactory
    {
        public static MaterialFactory Instance { get; } = new MaterialFactory();

        public ObservableCollection<MaterialFactoryItem> Items = new ObservableCollection<MaterialFactoryItem>();

        string materialRootDir = "";
        CategoryModel materialRoot = null;

        Dictionary<string, MaterialModel> categoryTemplateMap = new Dictionary<string, MaterialModel>();

        MaterialFactory()
        {

        }

        string workdir;
        public string WorkDir
        {
            get { return workdir; }
            set
            {
                workdir = value;
                if(Directory.Exists(workdir) == false)
                {
                    workdir = "";
                    return;
                }

                string[] files = Directory.GetFiles(workdir);
                bool hasUProject = false;
                foreach (var item in files)
                {
                    if(Path.GetExtension(item).ToLower() == ".uproject")
                    {
                        hasUProject = true;
                        break;
                    }
                }
                if(hasUProject)
                {
                    materialRootDir = workdir + "\\Content\\Materials\\";
                    Directory.CreateDirectory(materialRootDir);

                    foreach (var root in Logic.BambooEditor.Instance.rootCategories)
                    {
                        if (root?.name.IndexOf("material", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            materialRoot = root;
                            break;
                        }
                    }
                }
                else
                {
                    workdir = "";
                }
            }
        }

        string getCategoryPathName(CategoryModel cate)
        {
            string path = cate.name;
            while(string.IsNullOrEmpty(cate.parentId) == false)
            {
                cate = Logic.BambooEditor.Instance.GetCategoryById(cate.parentId);
                if (cate == null)
                    return path;
                if (string.IsNullOrEmpty(cate.parentId))
                    return "/" + path;
                path = cate.name + "/" + path;
            }
            return path;
        }

        void createCategoryDir(CategoryModel cate, string baseDir)
        {
            if (cate == null)
                return;

            baseDir = baseDir + "/" + cate.name;
            Directory.CreateDirectory(baseDir);

            if (cate.children == null)
                return;

            foreach (var item in cate.children)
            {
                createCategoryDir(item, baseDir);
            }
        }

        public void RecreateUploadDir()
        {
            if(Directory.Exists(workdir) == false)
                return;

            var editor = Logic.BambooEditor.Instance;
            if (editor.rootCategories == null)
                return;

            if (materialRoot == null)
                return;
            foreach (var item in materialRoot.children)
            {
                createCategoryDir(item, materialRootDir);
            }
        }

        void templog(string msg)
        {
            MaterialFactoryItem log = new MaterialFactoryItem();
            log.Name = msg;
            Items.Add(log);
        }

        async Task<int> parseCategoryDir(string dir, CategoryModel cate)
        {
            if (cate == null)
            {
                //templog("category is null");
                return 0;
            }

            var files = Directory.GetFiles(dir);

            Dictionary<string, List<MaterialTextureInfo>> textureMap = new Dictionary<string, List<MaterialTextureInfo>>();

            MaterialModel matTemplate = null;
            Logic.CategoryCustomData_Material matTemplateInfo = null;
            if(string.IsNullOrEmpty(cate.customData) == false)
            {
                try
                {
                    matTemplateInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Logic.CategoryCustomData_Material>(cate.customData);                    
                }
                catch { }
            }
            if(matTemplateInfo?.TemplateId != null)
            {
                var getres = await Logic.BambooEditor.Instance.api.GetAsync<MaterialModel>("/material/" + matTemplateInfo.TemplateId);
                matTemplate = getres.Content;
                categoryTemplateMap[cate.id] = matTemplate;
            }

            foreach (var file in files)
            {
                MaterialTextureInfo info = new MaterialTextureInfo();
                info.Path = Path.GetDirectoryName(file);
                string filename = Path.GetFileNameWithoutExtension(file);
                info.Name = "";
                info.ParamName = "";
                int tagIndex = filename.IndexOf('_');
                if(tagIndex > 0)
                {
                    info.Name = filename.Substring(0, tagIndex);
                    if(tagIndex < filename.Length - 1)
                        info.ParamName = filename.Substring(tagIndex + 1);
                }
                else
                {
                    info.Name = filename;
                }

                List<MaterialTextureInfo> textures = null;
                if(textureMap.ContainsKey(info.Name))
                {
                    textures = textureMap[info.Name];
                }
                else
                {
                    textures = new List<MaterialTextureInfo>();
                    textureMap[info.Name] = textures;
                }
                textures.Add(info);
            }

            //templog($"{dir} find {files.Length} files, get {textureMap.Count} materials");

            foreach (var item in textureMap)
            {
                MaterialFactoryItem mat = new MaterialFactoryItem();
                mat.TextureList = item.Value;
                mat.Name = item.Key;
                mat.CategoryId = cate.id;
                mat.CategoryPath = getCategoryPathName(cate);
                mat.MaterialTempateName = matTemplate?.name;

                mat.PropObjToString();
                Items.Add(mat);
            }

            var dirs = Directory.GetDirectories(dir);

            foreach (var d in dirs)
            {
                string dirname = Path.GetFileName(d);
                CategoryModel childcate = cate.children.Find(t => t.name == dirname);
                await parseCategoryDir(d, childcate);
            }

            return textureMap.Count;
        }

        public async Task<int> RefreshList()
        {
            Items.Clear();

            if (Directory.Exists(materialRootDir) == false)
                return 0;

            return await parseCategoryDir(materialRootDir, materialRoot);
        }

        public async Task<int> Upload()
        {
            await Task.Delay(100);
            return 0;
        }

        public string GetRules()
        {
            const string rules =
                @"材质规则
一一一一一一一一一一一一一一一一一一一一一一一一
* 此工具在项目/Content/Materials创建分类文件夹
* 把贴图导入对应文件夹后 点击【刷新列表】
* 点击【上传】按钮则会把这些贴图上传到服务器，

每组贴图会用分类的模板材质创建一个新的材质对象
每一个材质可以包含多张贴图，命名规则如下
材质名[_][参数名]
参数名，模板材质中使用此贴图的参数的名称
举例
Wood 只有一张漫反射贴图可以只有一个名字
      参数名会使用材质分类上设定的默认参数名
Wood_Diffuse, Wood_Normal 两张图
Wood_D1, Wood_D2, Wood_Normal 复杂情况

* 上面的文件是导入UE4的贴图.uasset，不是.jpg！
";
            return rules;
        }
    }
}
