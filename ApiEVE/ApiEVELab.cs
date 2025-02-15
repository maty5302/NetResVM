using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiEVE
{
    public class ApiEVELab
    {
        public async Task<string?> GetLabs(ApiEVEHttpClient client)
        {
            var url = $"{client.Url}folders/";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        public async Task<string?> GetLabInfo(ApiEVEHttpClient client, string labName)
        {
            var url = $"{client.Url}labs/{labName.Trim()}";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
                return null;
        }

        public async Task<string?> ExportLab(ApiEVEHttpClient client, string jsonData)
        {
            var url = $"{client.Url}export";
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await client.Client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var downloadLink = await response.Content.ReadAsStringAsync();
                var downloadLinkJson = JsonDocument.Parse(downloadLink).RootElement.GetProperty("data");
                var downloadUrl = $"{client.Url.TrimEnd('/').Replace("/api", "", StringComparison.OrdinalIgnoreCase)}{downloadLinkJson}";
                var downloadResponse = await client.Client.GetAsync(downloadUrl);
                if (downloadResponse.IsSuccessStatusCode)
                {
                    return await downloadResponse.Content.ReadAsStringAsync();
                }
                else
                    return null;

            }
            else
                return null;
        }

        public async Task<bool> ImportLab(ApiEVEHttpClient user, byte[] file, string fileName)
        {
            var url = user.Url + "import";

            using (var content = new MultipartFormDataContent())
            {

                content.Add(new StringContent("/"), "path");
                var fileContent = new ByteArrayContent(file);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-zip-compressed");

                // Attach the file with the correct name
                content.Add(fileContent, "file", fileName);

                var response = await user.Client.PostAsync(url.Trim(), content);
                return response.IsSuccessStatusCode;
            }
        }
        
        public async Task<bool> DeleteLab(ApiEVEHttpClient user, string labName)
        {
            var url = $"{user.Url}labs/{labName.Trim()}";
            var response = await user.Client.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }

}
