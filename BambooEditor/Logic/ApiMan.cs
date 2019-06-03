using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BambooCook.Logic
{
    public class ApiResponse<T>  where T : class, new()
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public T Content { get; set; }
    }

    public class ApiResponseStr
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Content { get; set; }
    }

    public class ApiMan
    {
        public HttpClient client;

        Newtonsoft.Json.JsonSerializerSettings jsonSettings = new Newtonsoft.Json.JsonSerializerSettings();

        string token;
        Uri serverBase;
        public ApiMan(Uri serverBase)
        {
            this.serverBase = serverBase;
            client = new HttpClient();
            client.BaseAddress = serverBase;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(300); //5 minutes

            jsonSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        }

        public enum HttpVerb
        {
            Get,
            Post,
            Put,
            Delete
        }

        public int TimeoutSeconds
        {
            get
            {
                return (int)client.Timeout.TotalSeconds;
            }
            set
            {
                client.Timeout = TimeSpan.FromSeconds(value);
            }
        }

        public void SetServerBase(Uri serverBase)
        {
            this.serverBase = serverBase;
            client.BaseAddress = serverBase;
        }
        public void SetToken(string token)
        {
            this.token = token;
            if(client.DefaultRequestHeaders.Contains("Authorization"))
            {
                client.DefaultRequestHeaders.Remove("Authorization");
            }
            client.DefaultRequestHeaders.Add("Authorization", "bearer " + token);
        }
        
        public async Task<ApiResponseStr> ActionAsync(HttpVerb verb, string path, object body = null)
        {
            HttpResponseMessage res = null;
            HttpContent content = null;
            if (body != null)
            {
                if(body is string)
                {
                    content = new StringContent(body as string, Encoding.UTF8, "application/json");
                }
                else
                {
                    content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                }
            }
            string resBody = "";
            try
            {
                switch (verb)
                {
                    case HttpVerb.Get: res = await client.GetAsync(path); break;
                    case HttpVerb.Post: res = await client.PostAsync(path, content); break;
                    case HttpVerb.Put: res = await client.PutAsync(path, content); break;
                    case HttpVerb.Delete: res = await client.DeleteAsync(path); break;
                }
                resBody = await res.Content.ReadAsStringAsync();
            }catch(Exception ex)
            {
                resBody = ex.Message;
            }
            ApiResponseStr result = new ApiResponseStr();
            result.StatusCode = res == null ? 0 : (int)res.StatusCode;
            result.IsSuccess = res == null ? false : res.IsSuccessStatusCode;
            result.Content = resBody;
            return result;
        }
        
        public async Task<ApiResponse<T>> ActionAsync<T>(HttpVerb verb, string path, object body = null) where T : class, new()
        {
            HttpResponseMessage res = null;
            HttpContent content = null;
            if (body != null)
            {
                if (body is string)
                {
                    content = new StringContent(body as string, Encoding.UTF8, "application/json");
                }
                else
                {
                    content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                }
            }
            string resBody = "";
            try
            {
                switch (verb)
                {
                    case HttpVerb.Get: res = await client.GetAsync(path); break;
                    case HttpVerb.Post: res = await client.PostAsync(path, content); break;
                    case HttpVerb.Put: res = await client.PutAsync(path, content); break;
                    case HttpVerb.Delete: res = await client.DeleteAsync(path); break;
                }
                resBody = await res.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                resBody = ex.Message;
            }
            ApiResponse<T> result = new ApiResponse<T>();
            result.StatusCode = (int)res.StatusCode;
            result.IsSuccess = res.IsSuccessStatusCode;
            try
            {
                result.Content = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(resBody, jsonSettings);
            }catch
            {
                result.IsSuccess = true;
                result.Content = null;
            }
            return result;
        }

        public async Task<ApiResponseStr> GetAsync(string path)
        {
            return await ActionAsync(HttpVerb.Get, path);
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string path) where T : class, new()
        {
            return await ActionAsync<T>(HttpVerb.Get, path);
        }

        public async Task<ApiResponseStr> GetAsync(string path, object content)
        {
            return await ActionAsync(HttpVerb.Get, path, content);
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string path, object content) where T : class, new()
        {
            return await ActionAsync<T>(HttpVerb.Get, path, content);
        }

        public async Task<ApiResponseStr> PostAsync(string path)
        {
            return await ActionAsync(HttpVerb.Get, path);
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string path) where T : class, new()
        {
            return await ActionAsync<T>(HttpVerb.Get, path);
        }

        public async Task<ApiResponseStr> PostAsync(string path, object content)
        {
            return await ActionAsync(HttpVerb.Post, path, content);
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string path, object content) where T : class, new()
        {
            return await ActionAsync<T>(HttpVerb.Post, path, content);
        }
        
        public async Task<ApiResponseStr> PutAsync(string path, object content)
        {
            return await ActionAsync(HttpVerb.Put, path, content);
        }

        public async Task<ApiResponse<T>> PutAsync<T>(string path, object content) where T : class, new()
        {
            return await ActionAsync<T>(HttpVerb.Put, path, content);
        }

        public async Task<ApiResponseStr> DeleteAsync(string path)
        {
            return await ActionAsync(HttpVerb.Delete, path);
        }

        public async Task<ApiResponse<T>> DeleteAsync<T>(string path) where T : class, new()
        {
            return await ActionAsync<T>(HttpVerb.Delete, path);
        }

        public async Task<bool> DownloadFile(string path, string localPath)
        {
            var res = await client.GetAsync(path);
            if(res.IsSuccessStatusCode)
            {
                var stream = await res.Content.ReadAsStreamAsync();
                string dir = System.IO.Path.GetDirectoryName(localPath);
                System.IO.Directory.CreateDirectory(dir);
                using (System.IO.FileStream fs = new System.IO.FileStream(localPath, System.IO.FileMode.OpenOrCreate))
                {
                    await stream.CopyToAsync(fs);
                }
            }
            return true;
        }
        public async Task<ApiResponseStr> UploadFile(string url, string localPath, int fileState = 0)
        {
            string urlEncodedPath = System.Net.WebUtility.UrlEncode(localPath);// for http header safe
            string fileExt = System.IO.Path.GetExtension(urlEncodedPath);
            HttpResponseMessage res = null;
            ApiResponseStr result = new ApiResponseStr();

            HttpClient c = new HttpClient();
            c.BaseAddress = serverBase;
            c.DefaultRequestHeaders.Add("Authorization", "bearer " + token);
            c.DefaultRequestHeaders.Add("fileExt", fileExt);
            c.DefaultRequestHeaders.Add("localPath", urlEncodedPath);
            c.DefaultRequestHeaders.Add("fileState", fileState.ToString());

            HttpContent content = null;
            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(localPath, System.IO.FileMode.Open))
                {
                    content = new StreamContent(fs);
                    res = await c.PostAsync(url, content);
                }
            }catch(Exception ex)
            {
                result.Content = ex.Message;
                result.IsSuccess = false;
                result.StatusCode = 400;
                return result;
            }
            string resBody = await res.Content.ReadAsStringAsync();
            result.StatusCode = (int)res.StatusCode;
            result.IsSuccess = res.IsSuccessStatusCode;
            result.Content = resBody;
            return result;
        }
    }
}
