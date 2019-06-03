using BambooCook.Logic;
using BambooUploader.Logic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooEditor.Logic
{
    public class SvrDataMan
    {
        SvrDataCache cache = null;
        ApiMan api = null;

        public ProgressModel progress = new ProgressModel();

        public SvrDataMan(SvrDataCache cache)
        {
            this.cache = cache;
            api = BambooEditor.Instance.api;
        }

        public int LoadFromCache()
        {
            cache.Load("datacache.dat");
            return cache.Data.Kvp.Count;
        }

        public void SaveCache()
        {
            cache.Save("datacache.dat");
        }

        public static string GetApiNameByTypeName(string type)
        {
            switch(type)
            {
                case "Texture": return "Texture";
                case "StaticMesh": return "StaticMesh";
                case "Material": return "Material";
                case "Map": return "Map";
                case "Product": return "Products";
                case "File": return "Files";
                default: return "";
            }
        }

        public async Task<int> LoadAllData(bool getFullData)
        {
            cache.Clear();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            progress.Reset();
            progress.Total = 3;
            await LoadAllCategories();  progress.CurPos++;

            progress.Status = "get files total";
            progress.Total += await GetObjCount("files"); progress.Status = "get Texture total";
            progress.Total += await GetObjCount("Texture"); progress.Status = "get static mesh total";
            progress.Total += await GetObjCount("StaticMesh"); progress.Status = "get Material total";
            progress.Total += await GetObjCount("Material"); progress.Status = "get Map total";
            progress.Total += await GetObjCount("Map"); progress.Status = "get Product total";
            progress.Total += await GetObjCount("Products");

            progress.Status = "loading objects";

            await LoadAllObjByCategory<FileModel>("File");
            await LoadAllObjByCategory<TextureModel>("Texture");
            await LoadAllObjByCategory<MaterialModel>("Material", getFullData);
            await LoadAllObjByCategory<StaticMeshModel>("StaticMesh", getFullData);
            await LoadAllObjByCategory<MapModel>("Map");
            await LoadAllObjByCategory<ProductModel>("Product", true); // product must use full data.

            var loadCost = watch.Elapsed;
            watch.Restart();
            progress.Status = "saving data cache file";
            cache.Save("datacache.dat"); progress.CurPos++;

            var saveCost = watch.Elapsed;
            watch.Restart();
            progress.Status = "finished total " + progress.Total;
            progress.Status = "parsing...";

            cache.ParseData(); progress.CurPos++;

            var parseCost = watch.Elapsed;
            progress.Percent = 100;
            progress.Status = $"load {progress.Total - 3} objs, get in {(int)loadCost.TotalSeconds}s, save in {(int)saveCost.TotalSeconds}s, parse in {(int)parseCost.TotalSeconds}s. total {(int)(loadCost + saveCost + parseCost).TotalSeconds}s";

            return cache.Data.Kvp.Count;
        }

        public async Task<int> GetServerAllDataCount()
        {
            int total = 0;
            total += await GetObjCount("StaticMesh");
            total += await GetObjCount("Products");
            return total;
        }

        public async Task<string> GetServerDataCountString()
        {
            int count = 0;
            StringBuilder sb = new StringBuilder();
            count = await GetObjCount("Texture");
            sb.Append("Texture: " + count + Environment.NewLine);
            count = await GetObjCount("Material");
            sb.Append("Materials: " + count + Environment.NewLine);
            count = await GetObjCount("Map");
            sb.Append("Map: " + count + Environment.NewLine);
            count = await GetObjCount("StaticMesh");
            sb.Append("StaticMesh: " + count + Environment.NewLine);
            count = await GetObjCount("files");
            sb.Append("Files: " + count + Environment.NewLine);
            count = await GetObjCount("Products");
            sb.Append("Products: " + count + Environment.NewLine);

            return sb.ToString();
        }

        public async Task<int> LoadAllCategories()
        {
            var result = await api.GetAsync("/category/all");
            if (result.IsSuccess)
            {
                cache.New("categoryall", "category", result.Content);
            }
            return 0;
        }

        public async Task<int> GetObjCount(string apiname)
        {
            var result = await api.GetAsync<ObjectPackResult<EntityBase>>($"/{apiname}?page=1&pageSize=1");
            int total = 0;
            if (result.IsSuccess)
            {
                total = result.Content.total;
            }
            return total;
        }

        public async Task<int> LoadAllObjByCategory<T>(string type, bool getFullData = false, int pageSize = 500) where T : EntityBase
        {
            string apiname = GetApiNameByTypeName(type);
            var result = await api.GetAsync<ObjectPackResult<EntityBase>>($"/{apiname}?page=1&pageSize=1");
            int total = 0;
            if (result.IsSuccess)
            {
                total = result.Content.total;
            }

            long count = 0;

            int pages = (int)Math.Ceiling(total / (float)pageSize);
            Stopwatch watch = new Stopwatch();
            for (int i = 1; i <= pages; i++)
            {
                watch.Restart();
                var listres = await api.GetAsync<ObjectPackResult<T>>($"/{apiname}?page={i}&pageSize={pageSize}");
                if (listres.IsSuccess == false || listres.Content == null || listres.Content.data == null)
                    continue;
                float pickCost = watch.ElapsedMilliseconds / 1000.0f;
                watch.Restart();
                foreach (var c in listres.Content.data)
                {
                    if(getFullData)
                    {
                        try
                        {
                            var obj = await api.GetAsync($"/{apiname}/{c.id}");
                            if (obj.IsSuccess)
                            {
                                cache.New(c.id, type, obj.Content);
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        //use data in list result directly.
                        cache.New(c.id, type, c);
                    }
                    count++;
                }
                float parseCost = watch.ElapsedMilliseconds / 1000.0f;
                progress.CurPos += listres.Content.data.Count;
                TimeSpan lasttime = new TimeSpan(0, 0, (int)((progress.Total - progress.CurPos) / pageSize * (pickCost + parseCost)));
                progress.Status = $"get {listres.Content.data.Count} in {pickCost}s, parse in {parseCost}s, rest time {lasttime.ToString(@"hh\:mm\:ss")}";
            }
            return 0;
        }
    }
}
